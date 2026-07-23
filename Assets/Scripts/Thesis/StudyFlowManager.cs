using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StudyFlowManager : MonoBehaviour
{
    [Header("Study Settings")]
    public string participantId = "P01";

    [Header("Current Phase")]
    public StudyPhase currentPhase = StudyPhase.Baseline;

    [Header("Task Assets")]
    public ReferenceTaskData shadingTask;
    public ReferenceTaskData perspectiveTask;

    [Header("References")]
    public CanvasPainter canvasPainter;
    public SessionLogger sessionLogger;
    public ReferenceScoringManager referenceScoringManager;

    [Header("UI Panels")]
    public GameObject baselineInstructionsPanel;
    public GameObject gamifiedInstructionsPanel;
    public GameObject drawingToolsPanel;
    public GameObject gamifiedHudPanel;
    public GameObject endPanel;

    [Header("Reference UI")]
    public RawImage referenceImageDisplay;
    public TMP_Text taskInstructionText;

    [Header("Instruction Text")]
    public TMP_Text baselineInstructionText;
    public TMP_Text gamifiedInstructionText;
    public TMP_Text endText;

    [Header("Transition UI")]
    public GameObject transitionPanel;
    public TMP_Text transitionText;
    public float transitionDelaySeconds = 2f;

    [Header("Participant ID")]
    public ParticipantIdManager participantIdManager;

    private int currentTaskIndex = 0;
    private bool isTransitioning = false;

    private float currentTaskStartTime;

    private void Start()
    {
        StartBaselineIntro();
    }

    public void StartBaselineIntro()
    {
        isTransitioning = false;
        currentPhase = StudyPhase.Baseline;
        currentTaskIndex = 0;

        SetPanel(baselineInstructionsPanel, true);
        SetPanel(gamifiedInstructionsPanel, false);
        SetPanel(drawingToolsPanel, false);
        SetPanel(gamifiedHudPanel, false);
        SetPanel(endPanel, false);
        SetPanel(transitionPanel, false);

        ClearReferenceUI();

        if (baselineInstructionText != null)
        {
            baselineInstructionText.text =
                "Baseline Task\n\n" +
                "You will complete two drawing tasks:\n" +
                "1. Shading\n" +
                "2. Perspective\n\n" +
                "Controls:\n" +
                "Point at a button, dropdown, or slider.\n" +
                "Press the trigger to select it.\n" +
                "Hold the trigger on the white canvas to draw.\n\n" +
                "Use the sliders to change brush shade and size.\n" +
                "Use the dropdown to switch between brush and eraser.\n\n" +
                "Use the reference image as your guide.\n\n" +
                "Press this panel when ready.";
        }

        Debug.Log("[StudyFlowManager] Baseline intro opened.");
    }

    public void StartBaselineTask()
    {
        if (isTransitioning)
            return;

        currentPhase = StudyPhase.Baseline;
        currentTaskIndex = 0;

        if (sessionLogger != null)
            sessionLogger.StartNewSession(GetParticipantId(), "baseline");

        SetPanel(baselineInstructionsPanel, false);
        SetPanel(gamifiedInstructionsPanel, false);
        SetPanel(drawingToolsPanel, true);
        SetPanel(gamifiedHudPanel, false);
        SetPanel(endPanel, false);
        SetPanel(transitionPanel, false);

        StartCurrentTask(false);

        Debug.Log("[StudyFlowManager] Baseline task started.");
    }

    public void StartGamifiedIntro()
    {
        isTransitioning = false;
        currentPhase = StudyPhase.Gamified;
        currentTaskIndex = 0;

        SetPanel(baselineInstructionsPanel, false);
        SetPanel(gamifiedInstructionsPanel, true);
        SetPanel(drawingToolsPanel, false);
        SetPanel(gamifiedHudPanel, false);
        SetPanel(endPanel, false);
        SetPanel(transitionPanel, false);

        ClearReferenceUI();

        if (gamifiedInstructionText != null)
        {
            gamifiedInstructionText.text =
                "Gamified Task\n\n" +
                "You will complete the same two drawing tasks again:\n" +
                "1. Shading\n" +
                "2. Perspective\n\n" +
                "This version shows score, progress, and feedback.\n\n" +
                "Controls:\n" +
                "Point at a button, dropdown, or slider.\n" +
                "Press the trigger to select it.\n" +
                "Hold the trigger on the white canvas to draw.\n\n" +
                "Use the sliders to change brush shade and size.\n" +
                "Use the dropdown to switch between brush and eraser.\n\n" +
                "Feedback updates after each completed brush stroke.\n\n" +
                "Press this panel when ready.";
        }

        Debug.Log("[StudyFlowManager] Gamified intro opened.");
    }

    public void StartGamifiedTask()
    {
        if (isTransitioning)
            return;

        currentPhase = StudyPhase.Gamified;
        currentTaskIndex = 0;

        if (sessionLogger != null)
            sessionLogger.StartNewSession(participantId, "gamified");

        SetPanel(baselineInstructionsPanel, false);
        SetPanel(gamifiedInstructionsPanel, false);
        SetPanel(drawingToolsPanel, true);
        SetPanel(gamifiedHudPanel, true);
        SetPanel(endPanel, false);
        SetPanel(transitionPanel, false);

        StartCurrentTask(true);

        Debug.Log("[StudyFlowManager] Gamified task started.");
    }

    public void SubmitCurrentTask()
    {
        if (isTransitioning)
        {
            Debug.Log("[StudyFlowManager] Submit ignored because transition is already running.");
            return;
        }

        StartCoroutine(SubmitAndAdvanceAfterDelay());
    }

    private IEnumerator SubmitAndAdvanceAfterDelay()
    {
        isTransitioning = true;

        if (canvasPainter == null)
        {
            Debug.LogError("[StudyFlowManager] CanvasPainter missing.");
            isTransitioning = false;
            yield break;
        }

        string taskFileName = currentTaskIndex == 0 ? "shading.png" : "perspective.png";

        string savedPath = canvasPainter.SubmitWithCustomFileName(taskFileName);

        LogCurrentTaskSummary(savedPath);

        SetPanel(transitionPanel, true);

        if (transitionText != null)
        {
            if (currentPhase == StudyPhase.Baseline && currentTaskIndex == 0)
                transitionText.text = "Shading saved.\n\nPerspective task starting...";
            else if (currentPhase == StudyPhase.Baseline && currentTaskIndex == 1)
                transitionText.text = "Baseline saved.\n\nGamified version starting...";
            else if (currentPhase == StudyPhase.Gamified && currentTaskIndex == 0)
                transitionText.text = "Shading saved.\n\nPerspective task starting...";
            else
                transitionText.text = "Final task saved.\n\nFinishing...";
        }

        yield return new WaitForSeconds(transitionDelaySeconds);

        SetPanel(transitionPanel, false);

        AdvanceFlow();

        isTransitioning = false;
    }

    private void AdvanceFlow()
    {
        if (currentPhase == StudyPhase.Baseline)
        {
            if (currentTaskIndex == 0)
            {
                currentTaskIndex = 1;
                StartCurrentTask(false);
            }
            else
            {
                StartGamifiedIntro();
            }
        }
        else if (currentPhase == StudyPhase.Gamified)
        {
            if (currentTaskIndex == 0)
            {
                currentTaskIndex = 1;
                StartCurrentTask(true);
            }
            else
            {
                FinishStudy();
            }
        }
    }

    private void StartCurrentTask(bool gamified)
    {
        currentTaskStartTime = Time.time;

        ReferenceTaskData task = GetCurrentTask();

        if (task == null)
        {
            Debug.LogError("[StudyFlowManager] Current task is missing. Assign ShadingTaskData and PerspectiveTaskData.");
            return;
        }

        if (referenceScoringManager != null)
        {
            referenceScoringManager.StartTask(task, gamified);
        }

        if (referenceImageDisplay != null)
        {
            referenceImageDisplay.texture = task.referenceTexture;
            referenceImageDisplay.color = Color.white;

            Debug.Log($"[StudyFlowManager] Reference image set to: {task.referenceTexture}");
        }
        else
        {
            Debug.LogError("[StudyFlowManager] Reference Image Display is NOT assigned.");
        }

        if (taskInstructionText != null)
        {
            taskInstructionText.text = task.instructions;
        }

        if (sessionLogger != null)
        {
            sessionLogger.LogTaskStarted(
                task.taskName,
                task.taskType.ToString(),
                gamified
            );
        }

        if (canvasPainter != null)
            canvasPainter.ClearCanvas();

        SetPanel(drawingToolsPanel, true);
        SetPanel(gamifiedHudPanel, gamified);

        Debug.Log($"[StudyFlowManager] Started task: {task.taskName}, gamified: {gamified}");
    }

    private ReferenceTaskData GetCurrentTask()
    {
        if (currentTaskIndex == 0)
            return shadingTask;

        return perspectiveTask;
    }

    private void FinishStudy()
    {
        currentPhase = StudyPhase.Complete;

        SetPanel(baselineInstructionsPanel, false);
        SetPanel(gamifiedInstructionsPanel, false);
        SetPanel(drawingToolsPanel, false);
        SetPanel(gamifiedHudPanel, false);
        SetPanel(endPanel, true);
        SetPanel(transitionPanel, false);

        ClearReferenceUI();

        if (endText != null)
        {
            endText.text =
                "Task Complete\n\n" +
                "Thank you. Please remove the headset and inform the researcher.";
        }

        if (sessionLogger != null)
            sessionLogger.LogSessionComplete();

        Debug.Log("[StudyFlowManager] Study complete.");
    }

    private void ClearReferenceUI()
    {
        if (referenceImageDisplay != null)
            referenceImageDisplay.texture = null;

        if (taskInstructionText != null)
            taskInstructionText.text = "";
    }

    private void SetPanel(GameObject panel, bool active)
    {
        if (panel != null)
            panel.SetActive(active);
    }

    private string GetParticipantId()
    {
        if (participantIdManager != null && !string.IsNullOrEmpty(participantIdManager.CurrentParticipantId))
            return participantIdManager.CurrentParticipantId;

        return participantId;
    }

    private void LogCurrentTaskSummary(string savedPath)
    {
        if (sessionLogger == null || referenceScoringManager == null)
            return;

        ReferenceTaskData task = GetCurrentTask();

        if (task == null)
            return;

        float elapsed = Time.time - currentTaskStartTime;

        sessionLogger.LogTaskSubmitted(
            task.taskName,
            task.taskType.ToString(),
            currentPhase.ToString(),
            currentPhase == StudyPhase.Gamified,
            elapsed,
            referenceScoringManager.totalStrokePoints,
            referenceScoringManager.excellentPoints,
            referenceScoringManager.goodPoints,
            referenceScoringManager.okayPoints,
            referenceScoringManager.inaccuratePoints,
            referenceScoringManager.score,
            referenceScoringManager.GetProgressPercent(),
            savedPath
        );
    }
}