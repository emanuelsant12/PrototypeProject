using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ReferenceScoringManager : MonoBehaviour
{
    [Header("Current Task")]
    public ReferenceTaskData currentTask;

    [Header("Mode")]
    public bool showGamifiedUI = false;

    [Header("Stats")]
    public int totalStrokePoints;
    public int excellentPoints;
    public int goodPoints;
    public int okayPoints;
    public int inaccuratePoints;
    public int score;

    [Header("Progress Settings")]
    [Tooltip("Higher = more accurate progress tracking but slightly more expensive.")]
    public int progressGridSize = 32;

    [Tooltip("For shading, ignore almost-white background areas when calculating required progress.")]
    [Range(0f, 1f)]
    public float shadingRelevantValueThreshold = 0.95f;

    [Tooltip("Player does not need to cover 100% of the reference. This is the percentage of relevant cells needed to reach 100% progress.")]
    [Range(0.1f, 1f)]
    public float requiredCoveragePercent = 0.45f;

    [Header("UI")]
    public TMP_Text taskTitleText;
    public TMP_Text scoreText;
    public TMP_Text progressText;
    public TMP_Text feedbackText;

    [Header("Logging")]
    public SessionLogger sessionLogger;

    private HashSet<int> relevantReferenceCells = new HashSet<int>();
    private HashSet<int> completedCorrectCells = new HashSet<int>();

    private int requiredCellsForCompletion = 1;

    private int currentStrokeExcellent;
    private int currentStrokeGood;
    private int currentStrokeOkay;
    private int currentStrokeInaccurate;
    private int currentStrokeOffGuide;
    private int currentStrokePoints;

    public void StartTask(ReferenceTaskData task, bool showUI)
    {
        currentTask = task;
        showGamifiedUI = showUI;

        totalStrokePoints = 0;
        excellentPoints = 0;
        goodPoints = 0;
        okayPoints = 0;
        inaccuratePoints = 0;
        score = 0;

        ResetCurrentStrokeFeedbackCounters();

        completedCorrectCells.Clear();

        BuildRelevantReferenceCells();

        RefreshUI();

        if (feedbackText != null)
        {
            feedbackText.text = showUI
                ? "Start drawing. Try to match the reference as closely as possible."
                : "";
        }

        Debug.Log($"[ReferenceScoringManager] Started task: {task.taskName}, UI shown: {showUI}, Required Cells: {requiredCellsForCompletion}");
    }

    public StrokeScoreResult RegisterStroke(Vector2 uv, float brushValue, bool eraser)
    {
        StrokeScoreResult result = new StrokeScoreResult();

        if (currentTask == null)
            return result;

        totalStrokePoints++;

        if (eraser)
        {
            result.taskType = currentTask.taskType.ToString();
            result.category = "eraser";
            result.pointsAwarded = 0;
            RefreshUI();
            return result;
        }

        switch (currentTask.taskType)
        {
            case TaskType.Shading:
                result = ScoreShadingStroke(uv, brushValue);
                break;

            case TaskType.Perspective:
                result = ScorePerspectiveStroke(uv);
                break;
        }

        ApplyScoreResult(result);
        TryAddProgressCell(uv, result);
        TrackCurrentStrokeFeedback(result);

        RefreshUI();

        return result;
    }

    private void BuildRelevantReferenceCells()
    {
        relevantReferenceCells.Clear();

        if (currentTask == null)
        {
            requiredCellsForCompletion = 1;
            return;
        }

        Texture2D textureToCheck = null;

        if (currentTask.taskType == TaskType.Shading)
        {
            textureToCheck = currentTask.referenceTexture;
        }
        else if (currentTask.taskType == TaskType.Perspective)
        {
            textureToCheck = currentTask.perspectiveMask != null
                ? currentTask.perspectiveMask
                : currentTask.referenceTexture;
        }

        if (textureToCheck == null)
        {
            requiredCellsForCompletion = 1;
            return;
        }

        for (int y = 0; y < progressGridSize; y++)
        {
            for (int x = 0; x < progressGridSize; x++)
            {
                float u = (x + 0.5f) / progressGridSize;
                float v = (y + 0.5f) / progressGridSize;

                float value = textureToCheck.GetPixelBilinear(u, v).grayscale;

                bool isRelevant = false;

                if (currentTask.taskType == TaskType.Shading)
                {
                    // Counts the sphere/shadow areas, ignores mostly white background.
                    isRelevant = value < shadingRelevantValueThreshold;
                }
                else if (currentTask.taskType == TaskType.Perspective)
                {
                    // Counts dark guide-line cells.
                    isRelevant = value <= currentTask.perspectiveLineThreshold;
                }

                if (isRelevant)
                {
                    int cellId = GetCellId(x, y);
                    relevantReferenceCells.Add(cellId);
                }
            }
        }

        int relevantCount = Mathf.Max(1, relevantReferenceCells.Count);
        requiredCellsForCompletion = Mathf.Max(1, Mathf.RoundToInt(relevantCount * requiredCoveragePercent));

        Debug.Log($"[ReferenceScoringManager] Relevant cells: {relevantCount}, Required for 100%: {requiredCellsForCompletion}");
    }

    private StrokeScoreResult ScoreShadingStroke(Vector2 uv, float brushValue)
    {
        StrokeScoreResult result = new StrokeScoreResult();
        result.taskType = "shading";

        if (currentTask.referenceTexture == null)
        {
            result.category = "no_reference";
            return result;
        }

        Color referenceColor = currentTask.referenceTexture.GetPixelBilinear(uv.x, uv.y);
        float referenceValue = referenceColor.grayscale;

        float difference = Mathf.Abs(brushValue - referenceValue);

        result.referenceValue = referenceValue;
        result.brushValue = brushValue;
        result.difference = difference;

        if (difference <= currentTask.shadingExcellentTolerance)
        {
            result.category = "excellent";
            result.pointsAwarded = 3;
        }
        else if (difference <= currentTask.shadingGoodTolerance)
        {
            result.category = "good";
            result.pointsAwarded = 2;
        }
        else if (difference <= currentTask.shadingOkayTolerance)
        {
            result.category = "okay";
            result.pointsAwarded = 1;
        }
        else
        {
            result.category = "inaccurate";
            result.pointsAwarded = 0;
        }

        return result;
    }

    private StrokeScoreResult ScorePerspectiveStroke(Vector2 uv)
    {
        StrokeScoreResult result = new StrokeScoreResult();
        result.taskType = "perspective";

        Texture2D mask = currentTask.perspectiveMask != null
            ? currentTask.perspectiveMask
            : currentTask.referenceTexture;

        if (mask == null)
        {
            result.category = "no_mask";
            return result;
        }

        Color maskColor = mask.GetPixelBilinear(uv.x, uv.y);
        float maskValue = maskColor.grayscale;

        result.referenceValue = maskValue;
        result.brushValue = 0f;
        result.difference = maskValue;

        if (maskValue <= currentTask.perspectiveLineThreshold)
        {
            result.category = "on_perspective_guide";
            result.pointsAwarded = 3;
        }
        else if (IsNearPerspectiveLine(mask, uv, 4))
        {
            result.category = "near_perspective_guide";
            result.pointsAwarded = 1;
        }
        else
        {
            result.category = "off_guide";
            result.pointsAwarded = 0;
        }

        return result;
    }

    private void TryAddProgressCell(Vector2 uv, StrokeScoreResult result)
    {
        int x = Mathf.Clamp(Mathf.FloorToInt(uv.x * progressGridSize), 0, progressGridSize - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt(uv.y * progressGridSize), 0, progressGridSize - 1);

        int cellId = GetCellId(x, y);

        if (!relevantReferenceCells.Contains(cellId))
            return;

        bool countsAsProgress = false;

        if (currentTask.taskType == TaskType.Shading)
        {
            countsAsProgress =
                result.category == "excellent" ||
                result.category == "good";

            // Removed "okay" from progress.
            // Okay strokes can still get score, but they should not fill progress.
        }
        else if (currentTask.taskType == TaskType.Perspective)
        {
            countsAsProgress =
                result.category == "on_perspective_guide";

            // IMPORTANT:
            // near_perspective_guide no longer counts for progress.
            // It can still give feedback/score, but not completion.
        }

        if (countsAsProgress)
        {
            completedCorrectCells.Add(cellId);
        }
    }

    private bool IsNearPerspectiveLine(Texture2D mask, Vector2 uv, int radiusPixels)
    {
        int centerX = Mathf.RoundToInt(uv.x * mask.width);
        int centerY = Mathf.RoundToInt(uv.y * mask.height);

        for (int y = -radiusPixels; y <= radiusPixels; y++)
        {
            for (int x = -radiusPixels; x <= radiusPixels; x++)
            {
                int px = Mathf.Clamp(centerX + x, 0, mask.width - 1);
                int py = Mathf.Clamp(centerY + y, 0, mask.height - 1);

                float value = mask.GetPixel(px, py).grayscale;

                if (value <= currentTask.perspectiveLineThreshold)
                    return true;
            }
        }

        return false;
    }

    private void ApplyScoreResult(StrokeScoreResult result)
    {
        score += result.pointsAwarded;

        switch (result.category)
        {
            case "excellent":
                excellentPoints++;
                break;

            case "good":
            case "on_perspective_guide":
                goodPoints++;
                break;

            case "okay":
            case "near_perspective_guide":
                okayPoints++;
                break;

            case "inaccurate":
            case "off_guide":
                inaccuratePoints++;
                break;
        }
    }

    private void TrackCurrentStrokeFeedback(StrokeScoreResult result)
    {
        currentStrokePoints++;

        switch (result.category)
        {
            case "excellent":
                currentStrokeExcellent++;
                break;

            case "good":
            case "on_perspective_guide":
                currentStrokeGood++;
                break;

            case "okay":
            case "near_perspective_guide":
                currentStrokeOkay++;
                break;

            case "inaccurate":
                currentStrokeInaccurate++;
                break;

            case "off_guide":
                currentStrokeOffGuide++;
                break;
        }
    }

    private void UpdateConstructiveFeedback()
    {
        if (feedbackText == null || currentTask == null)
            return;

        if (currentTask.taskType == TaskType.Shading)
        {
            UpdateShadingFeedback();
        }
        else if (currentTask.taskType == TaskType.Perspective)
        {
            UpdatePerspectiveFeedback();
        }
    }

    private string PickRandom(params string[] options)
    {
        if (options == null || options.Length == 0)
            return "";

        int index = Random.Range(0, options.Length);
        return options[index];
    }

    private void UpdateShadingFeedback()
    {
        if (feedbackText == null)
            return;

        float progress01 = GetProgress01();

        if (currentStrokeExcellent >= 3)
        {
            feedbackText.text = PickRandom(
                "Strong value matching. Now focus on smoothing the transition between tones.",
                "Good accuracy. Try blending the edges so the shading feels less patchy.",
                "You are matching the reference well. Refine the softer areas next.",
                "Good value control. Keep building the form gradually instead of pressing too dark too quickly."
            );
        }
        else if (currentStrokeGood >= 3)
        {
            feedbackText.text = PickRandom(
                "Good progress. Try using slightly smaller strokes to control the gradient.",
                "Your values are close. Focus on making the shadow flow smoothly into the mid-tone.",
                "Good match overall. Look at where the reference changes from light to dark and copy that transition.",
                "You are close to the correct values. Keep adjusting the brush darkness as the reference changes."
            );
        }
        else if (currentStrokeOkay >= 3)
        {
            feedbackText.text = PickRandom(
                "You are close, but compare the brush darkness with the reference before drawing more.",
                "The value is nearly there. Try making small adjustments instead of large dark strokes.",
                "Close attempt. Use lighter strokes first, then build darker areas gradually.",
                "Your marks are in the right area, but the tone needs more control."
            );
        }
        else if (currentStrokeInaccurate >= 3)
        {
            feedbackText.text = PickRandom(
                "Try adjusting the brush lighter or darker to better match the reference.",
                "Check the reference before continuing. Your current value does not match this area well.",
                "Slow down and compare the area you are painting with the reference value.",
                "Use the Lighten and Darken buttons before continuing. The brush value needs adjusting.",
                "Avoid filling large areas with one value. The reference uses gradual changes."
            );
        }
        else if (progress01 < 0.20f)
        {
            feedbackText.text = PickRandom(
                "Start by blocking in the main shadow and mid-tone areas.",
                "Focus first on the largest shaded area of the sphere.",
                "Begin with the main form shadow before adding small details.",
                "Try covering the important shaded areas before refining texture."
            );
        }
        else if (progress01 < 0.50f)
        {
            feedbackText.text = PickRandom(
                "Good start. Add more controlled strokes around the main shaded areas.",
                "You have started covering the form. Now build the mid-tones more carefully.",
                "Keep going. Try to connect the shadow areas instead of leaving isolated marks.",
                "Focus on the transition between the dark shadow and the lighter side."
            );
        }
        else if (progress01 < 0.80f)
        {
            feedbackText.text = PickRandom(
                "You are covering the important areas. Refine the edges and transitions.",
                "Good coverage. Now smooth the patchy areas and avoid unnecessary marks.",
                "The main shading is forming. Use lighter values to soften harsh edges.",
                "Try to make the sphere feel rounded by blending from dark to light."
            );
        }
        else
        {
            feedbackText.text = PickRandom(
                "Strong progress. Spend the remaining time refining smoothness and accuracy.",
                "Good completion. Now polish the transitions and clean any rough marks.",
                "The main task is mostly covered. Focus on improving the gradient quality.",
                "You have covered the key areas. Refine the shading so it looks less noisy."
            );
        }
    }

    private void UpdatePerspectiveFeedback()
    {
        if (feedbackText == null)
            return;

        float progress01 = GetProgress01();

        if (currentStrokeGood >= 3)
        {
            SetFeedback(PickRandom(
                "Good alignment. Keep following the perspective guide lines.",
                "Your strokes are lining up well. Continue building the main structure.",
                "Good direction. Keep aiming your lines toward the vanishing point.",
                "The perspective structure is becoming clearer. Keep the lines consistent."
            ));
        }
        else if (currentStrokeOkay >= 3)
        {
            SetFeedback(PickRandom(
                "Close. Try keeping your strokes more directly on the guide lines.",
                "You are near the correct direction. Adjust slightly toward the reference lines.",
                "Your line is close, but it is drifting. Re-align it with the vanishing point.",
                "Almost there. Use slower strokes to keep the line straighter."
            ));
        }
        else if (currentStrokeOffGuide >= 3)
        {
            SetFeedback(PickRandom(
                "Your strokes are drifting away from the perspective structure. Aim toward the guide lines.",
                "Check the reference before drawing more. Your lines should converge toward the vanishing point.",
                "Try starting from the edge and drawing toward the centre vanishing point.",
                "Avoid random lines. Perspective lines need to follow the same direction.",
                "Use the reference as a guide. The important lines should meet near the same point."
            ));
        }
        else if (progress01 < 0.20f)
        {
            SetFeedback(PickRandom(
                "Start with the main lines that lead toward the vanishing point.",
                "Begin by drawing the largest perspective edges first.",
                "Focus on the horizon and the main converging lines before adding details.",
                "Draw fewer lines, but make sure they aim toward the centre point."
            ));
        }
        else if (progress01 < 0.50f)
        {
            SetFeedback(PickRandom(
                "Good start. Add the remaining edges that converge toward the centre.",
                "The structure is starting to form. Keep the next lines aligned with the same vanishing point.",
                "Continue adding the main wall, floor, or ceiling edges.",
                "Try to keep spacing consistent as the lines move toward the distance."
            ));
        }
        else if (progress01 < 0.80f)
        {
            SetFeedback(PickRandom(
                "The structure is forming. Refine the lines and keep them consistent.",
                "Good coverage. Now clean up any lines that do not point toward the vanishing point.",
                "Most of the guide structure is present. Focus on straightness and alignment.",
                "Refine the perspective by making the major lines clearer and less shaky."
            ));
        }
        else
        {
            SetFeedback(PickRandom(
                "Strong progress. Use the remaining time to clean and straighten the main lines.",
                "The perspective layout is mostly complete. Refine accuracy and remove messy areas if needed.",
                "Good completion. Focus on making the final structure clearer and more readable.",
                "You have covered the important perspective areas. Now polish line quality."
            ));
        }
    }

    private void ResetRecentFeedbackCounters()
    {
        currentStrokeExcellent = 0;
        currentStrokeGood = 0;
        currentStrokeOkay = 0;
        currentStrokeInaccurate = 0;
        currentStrokeOffGuide = 0;
    }

    private void RefreshUI()
    {
        bool visible = showGamifiedUI;

        if (taskTitleText != null)
            taskTitleText.text = visible && currentTask != null ? currentTask.taskName : "";

        if (scoreText != null)
            scoreText.text = visible ? $"Score: {score}" : "";

        if (progressText != null)
        {
            float progress = GetProgress01();
            progressText.text = visible ? $"Progress: {Mathf.RoundToInt(progress * 100f)}%" : "";
        }

        if (!visible && feedbackText != null)
            feedbackText.text = "";
    }

    private float GetProgress01()
    {
        if (requiredCellsForCompletion <= 0)
            return 0f;

        return Mathf.Clamp01(completedCorrectCells.Count / (float)requiredCellsForCompletion);
    }

    private int GetCellId(int x, int y)
    {
        return y * progressGridSize + x;
    }

    public void  Stroke()
    {
        if (!showGamifiedUI)
            return;

        if (currentStrokePoints <= 0)
            return;

        UpdateConstructiveFeedback();
        ResetCurrentStrokeFeedbackCounters();
    }

    public void ResetCurrentStrokeFeedbackCounters()
    {
        currentStrokeExcellent = 0;
        currentStrokeGood = 0;
        currentStrokeOkay = 0;
        currentStrokeInaccurate = 0;
        currentStrokeOffGuide = 0;
        currentStrokePoints = 0;
    }

    public void EndCurrentBrushStroke()
    {
        if (!showGamifiedUI)
            return;

        if (currentStrokePoints <= 0)
            return;

        UpdateConstructiveFeedback();
        ResetCurrentStrokeFeedbackCounters();
    }

    public float GetProgressPercent()
    {
        return GetProgress01() * 100f;
    }

    private void SetFeedback(string message)
    {
        if (feedbackText != null)
            feedbackText.text = message;

        if (sessionLogger != null && currentTask != null && showGamifiedUI)
        {
            sessionLogger.LogFeedbackShown(
                currentTask.taskName,
                currentTask.taskType.ToString(),
                message
            );
        }
    }
}

[System.Serializable]
public struct StrokeScoreResult
{
    public string taskType;
    public string category;
    public int pointsAwarded;
    public float referenceValue;
    public float brushValue;
    public float difference;
}