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

    [Header("Dynamic Feedback Settings")]
    [Tooltip("Minimum stroke points needed before feedback is shown.")]
    public int minStrokePointsForFeedback = 3;

    [Tooltip("How many good/excellent points are needed before praising a stroke.")]
    public int goodStrokePointThreshold = 3;

    [Tooltip("Average value difference needed before saying the stroke is too dark or too light.")]
    [Range(0.05f, 0.5f)]
    public float shadingValueDifferenceThreshold = 0.15f;

    [Tooltip("If this percentage of a shading stroke hits the white background, warn the user.")]
    [Range(0.1f, 1f)]
    public float backgroundWarningPercent = 0.60f;

    [Tooltip("Progress below this is treated as early task progress.")]
    [Range(0f, 1f)]
    public float lowProgressThreshold = 0.25f;

    [Tooltip("Progress below this is treated as middle task progress.")]
    [Range(0f, 1f)]
    public float midProgressThreshold = 0.65f;

    [Tooltip("Stops the exact same feedback message from repeating immediately.")]
    public bool avoidRepeatingSameFeedback = true;

    private HashSet<int> relevantReferenceCells = new HashSet<int>();
    private HashSet<int> completedCorrectCells = new HashSet<int>();

    private int requiredCellsForCompletion = 1;

    private int currentStrokeExcellent;
    private int currentStrokeGood;
    private int currentStrokeOkay;
    private int currentStrokeInaccurate;
    private int currentStrokeOffGuide;
    private int currentStrokePoints;

    private float currentStrokeReferenceValueTotal;
    private float currentStrokeBrushValueTotal;
    private int currentStrokeValueSamples;

    private int currentStrokeBackgroundHits;
    private int currentStrokeRelevantHits;

    private string lastFeedbackMessage = "";

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
                currentStrokeRelevantHits++;
                break;

            case "good":
            case "on_perspective_guide":
                currentStrokeGood++;
                currentStrokeRelevantHits++;
                break;

            case "okay":
            case "near_perspective_guide":
                currentStrokeOkay++;
                currentStrokeRelevantHits++;
                break;

            case "inaccurate":
                currentStrokeInaccurate++;
                break;

            case "off_guide":
                currentStrokeOffGuide++;
                break;
        }

        if (currentTask != null && currentTask.taskType == TaskType.Shading)
        {
            currentStrokeReferenceValueTotal += result.referenceValue;
            currentStrokeBrushValueTotal += result.brushValue;
            currentStrokeValueSamples++;

            if (result.referenceValue >= shadingRelevantValueThreshold)
                currentStrokeBackgroundHits++;
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

        float averageReferenceValue = 0f;
        float averageBrushValue = 0f;

        if (currentStrokeValueSamples > 0)
        {
            averageReferenceValue = currentStrokeReferenceValueTotal / currentStrokeValueSamples;
            averageBrushValue = currentStrokeBrushValueTotal / currentStrokeValueSamples;
        }

        float valueDifference = averageBrushValue - averageReferenceValue;

        float backgroundRatio = currentStrokePoints > 0
        ? currentStrokeBackgroundHits / (float)currentStrokePoints
        : 0f;

        float goodRatio = currentStrokePoints > 0
            ? (currentStrokeExcellent + currentStrokeGood) / (float)currentStrokePoints
            : 0f;

        float weakRatio = currentStrokePoints > 0
            ? currentStrokeInaccurate / (float)currentStrokePoints
            : 0f;

        if (currentStrokePoints < minStrokePointsForFeedback)
        {
            SetFeedbackSmart(PickRandom(
                "Make a slightly longer stroke so the system can give useful feedback.",
                "Try drawing a longer mark before checking the feedback.",
                "Use a more complete stroke so the system can compare it properly."
            ));
            return;
        }

        if (backgroundRatio >= backgroundWarningPercent)
        {
            SetFeedbackSmart(PickRandom(
                "Most of this stroke is on the white background. Focus on the sphere and cast shadow.",
                "You are drawing mostly outside the shaded area. Aim for the sphere or shadow.",
                "Move closer to the reference shape. The white background does not count much toward progress."
            ));
            return;
        }

        if (currentStrokeExcellent >= goodStrokePointThreshold)
        {
            SetFeedbackSmart(PickRandom(
                "Strong value match. Now smooth the transition between dark and light areas.",
                "This stroke matches the reference well. Continue refining nearby tones.",
                "Good accuracy. Keep building the shading gradually instead of overworking one area.",
                "Nice value control. Now focus on making the sphere feel smoother and rounder."
            ));
            return;
        }

        if (currentStrokeGood >= goodStrokePointThreshold)
        {
            SetFeedbackSmart(PickRandom(
                "Good value match. Try using smaller strokes for smoother blending.",
                "You are close to the reference. Refine the edges of the shaded area.",
                "Good progress. Keep adjusting the brush shade as the reference changes.",
                "The value is close. Now try to make the transition less patchy."
            ));
            return;
        }

        if (valueDifference < -shadingValueDifferenceThreshold)
        {
            SetFeedbackSmart(PickRandom(
                "Your stroke is too dark for this area. Move the shade slider lighter.",
                "This part of the reference is lighter than your brush. Use a lighter value.",
                "Try reducing the darkness. Build the shadow gradually instead of going too dark.",
                "The brush value is too strong here. Lighten it and blend into the mid-tone."
            ));
            return;
        }

        if (valueDifference > shadingValueDifferenceThreshold)
        {
            SetFeedbackSmart(PickRandom(
                "Your stroke is too light for this area. Move the shade slider darker.",
                "The reference is darker here. Use a darker brush value.",
                "Try darkening the brush before continuing in this shadow area.",
                "This area needs a stronger value. Move the shade slider darker."
            ));
            return;
        }

        if (currentStrokeOkay >= goodStrokePointThreshold)
        {
            SetFeedbackSmart(PickRandom(
                "The value is close, but needs more control. Use shorter strokes and compare with the reference.",
                "You are near the correct tone. Adjust the shade slightly before adding more.",
                "Close attempt. Focus on smoother transitions between the tones.",
                "This is nearly correct. Slow down and blend the value more carefully."
            ));
            return;
        }

        if (weakRatio > 0.7f)
        {
            SetFeedbackSmart(PickRandom(
                "This stroke does not match the reference well. Check the shade before continuing.",
                "Try comparing the area under your brush with the same area on the reference.",
                "The mark is not helping the form yet. Adjust the shade and aim for a relevant area."
            ));
            return;
        }

        if (progress01 < lowProgressThreshold)
        {
            SetFeedbackSmart(PickRandom(
                "Start by covering the main shadow and mid-tone areas of the sphere.",
                "Begin with the largest shaded areas before refining details.",
                "Focus first on the main form shadow, then build lighter tones around it."
            ));
        }
        else if (progress01 < midProgressThreshold)
        {
            SetFeedbackSmart(PickRandom(
                "Good start. Add more controlled shading where the reference is darker.",
                "You have covered some key areas. Now connect the tones more smoothly.",
                "Keep building the sphere shape by blending from dark to light."
            ));
        }
        else
        {
            SetFeedbackSmart(PickRandom(
                "Most key areas are covered. Now refine smoothness and clean rough marks.",
                "You have good coverage. Focus on polishing the gradient.",
                "The main shading is complete. Use small adjustments to improve the final result."
            ));
        }
    }

    private void UpdatePerspectiveFeedback()
    {
        if (feedbackText == null)
            return;

        float progress01 = GetProgress01();

        float goodRatio = currentStrokePoints > 0
       ? currentStrokeGood / (float)currentStrokePoints
       : 0f;

        float nearRatio = currentStrokePoints > 0
            ? currentStrokeOkay / (float)currentStrokePoints
            : 0f;

        float offGuideRatio = currentStrokePoints > 0
            ? currentStrokeOffGuide / (float)currentStrokePoints
            : 0f;

        if (currentStrokePoints < minStrokePointsForFeedback)
        {
            SetFeedbackSmart(PickRandom(
                "Make a longer line so the system can check the perspective direction.",
                "Try drawing a more complete line toward the vanishing point.",
                "Use a longer stroke so the system can judge the alignment properly."
            ));
            return;
        }

        if (currentStrokeGood >= goodStrokePointThreshold || goodRatio >= 0.6f)
        {
            SetFeedbackSmart(PickRandom(
                "Good alignment. Keep following the guide lines toward the vanishing point.",
                "This line is following the perspective structure well.",
                "Good stroke. Continue adding lines that converge toward the same point.",
                "Your line direction is strong. Keep the next lines consistent."
            ));
            return;
        }

        if (currentStrokeOkay >= goodStrokePointThreshold || nearRatio >= 0.5f)
        {
            SetFeedbackSmart(PickRandom(
                "Close. Your line is near the guide, but try keeping it straighter.",
                "You are close to the correct guide line. Slow down and aim toward the vanishing point.",
                "Almost aligned. Adjust the direction slightly so the line follows the reference.",
                "This line is near the structure. Try to place it more directly on the guide."
            ));
            return;
        }

        if (currentStrokeOffGuide >= goodStrokePointThreshold || offGuideRatio >= 0.6f)
        {
            SetFeedbackSmart(PickRandom(
                "This line is away from the perspective guide. Aim it toward the vanishing point.",
                "Your stroke is drifting from the structure. Use the reference lines as a guide.",
                "Try starting from the edge and drawing toward the centre vanishing point.",
                "Avoid random lines. Perspective lines should converge toward the same point."
            ));
            return;
        }

        if (progress01 < lowProgressThreshold)
        {
            SetFeedbackSmart(PickRandom(
                "Start with the main lines that lead toward the vanishing point.",
                "Begin with the biggest edges first, then add smaller details.",
                "Focus on the horizon and the main converging lines."
            ));
        }
        else if (progress01 < midProgressThreshold)
        {
            SetFeedbackSmart(PickRandom(
                "The structure is forming. Add the remaining converging lines.",
                "Good start. Keep the next lines aiming toward the same point.",
                "Continue building the main wall, floor, or object edges."
            ));
        }
        else
        {
            SetFeedbackSmart(PickRandom(
                "Most guide areas are covered. Refine line straightness and remove messy marks.",
                "The perspective layout is mostly complete. Clean up the main lines.",
                "Good coverage. Now focus on making the structure clear and readable."
            ));
        }
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

    public void ResetCurrentStrokeFeedbackCounters()
    {
        currentStrokeExcellent = 0;
        currentStrokeGood = 0;
        currentStrokeOkay = 0;
        currentStrokeInaccurate = 0;
        currentStrokeOffGuide = 0;
        currentStrokePoints = 0;

        currentStrokeReferenceValueTotal = 0f;
        currentStrokeBrushValueTotal = 0f;
        currentStrokeValueSamples = 0;

        currentStrokeBackgroundHits = 0;
        currentStrokeRelevantHits = 0;
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

    private void SetFeedbackSmart(string message)
    {
        if (avoidRepeatingSameFeedback && message == lastFeedbackMessage)
        {
            message = AddSmallVariation(message);
        }

        lastFeedbackMessage = message;
        SetFeedback(message);
    }

    private string AddSmallVariation(string message)
    {
        return message + "\nTry one careful stroke at a time.";
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