using UnityEngine;
using TMPro;

public class StudyFlowManager : MonoBehaviour
{
    [Header("Study Settings")]
    public string participantId = "P01";

    [Header("Current Phase")]
    public StudyPhase currentPhase = StudyPhase.Baseline;

    [Header("References")]
    public CanvasPainter canvasPainter;
    public SessionLogger sessionLogger;
    public GamificationManager gamificationManager;

    [Header("UI Panels")]
    public GameObject baselineInstructionsPanel;
    public GameObject gamifiedInstructionsPanel;
    public GameObject drawingToolsPanel;
    public GameObject gamifiedHudPanel;
    public GameObject endPanel;

    [Header("Instruction Text")]
    public TMP_Text baselineInstructionText;
    public TMP_Text gamifiedInstructionText;
    public TMP_Text endText;

    private void Start()
    {
        StartBaselineIntro();
    }

    public void StartBaselineIntro()
    {
        currentPhase = StudyPhase.Baseline;

        SetPanel(baselineInstructionsPanel, true);
        SetPanel(gamifiedInstructionsPanel, false);
        SetPanel(drawingToolsPanel, false);
        SetPanel(gamifiedHudPanel, false);
        SetPanel(endPanel, false);

        if (baselineInstructionText != null)
        {
            baselineInstructionText.text =
                "Baseline Task\n\n" +
                "Use the brush tools to complete the shading task.\n\n" +
                "Try to match the reference image as accurately as possible.\n\n" +
                "Press Start when ready.";
        }
    }

    public void StartBaselineTask()
    {
        currentPhase = StudyPhase.Baseline;

        if (sessionLogger != null)
            sessionLogger.StartNewSession(participantId, "baseline");

        if (gamificationManager != null)
            gamificationManager.SetGamifiedMode(false);

        if (canvasPainter != null)
            canvasPainter.ClearCanvasToWhite();

        SetPanel(baselineInstructionsPanel, false);
        SetPanel(gamifiedInstructionsPanel, false);
        SetPanel(drawingToolsPanel, true);
        SetPanel(gamifiedHudPanel, false);
        SetPanel(endPanel, false);

        Debug.Log("[StudyFlowManager] Baseline task started.");
    }

    public void SubmitCurrentTask()
    {
        if (canvasPainter == null)
        {
            Debug.LogError("[StudyFlowManager] CanvasPainter missing.");
            return;
        }

        canvasPainter.Submit();

        if (currentPhase == StudyPhase.Baseline)
        {
            StartGamifiedIntro();
        }
        else if (currentPhase == StudyPhase.Gamified)
        {
            FinishStudy();
        }
    }

    public void StartGamifiedIntro()
    {
        currentPhase = StudyPhase.Gamified;

        SetPanel(baselineInstructionsPanel, false);
        SetPanel(gamifiedInstructionsPanel, true);
        SetPanel(drawingToolsPanel, false);
        SetPanel(gamifiedHudPanel, false);
        SetPanel(endPanel, false);

        if (gamifiedInstructionText != null)
        {
            gamifiedInstructionText.text =
                "Gamified Task\n\n" +
                "Complete the same type of shading task again.\n\n" +
                "This version includes points, progress, and feedback.\n\n" +
                "Press Start when ready.";
        }

        Debug.Log("[StudyFlowManager] Gamified intro opened.");
    }

    public void StartGamifiedTask()
    {
        currentPhase = StudyPhase.Gamified;

        if (sessionLogger != null)
            sessionLogger.StartNewSession(participantId, "gamified");

        if (gamificationManager != null)
            gamificationManager.SetGamifiedMode(true);

        if (canvasPainter != null)
            canvasPainter.ClearCanvasToWhite();

        SetPanel(baselineInstructionsPanel, false);
        SetPanel(gamifiedInstructionsPanel, false);
        SetPanel(drawingToolsPanel, true);
        SetPanel(gamifiedHudPanel, true);
        SetPanel(endPanel, false);

        Debug.Log("[StudyFlowManager] Gamified task started.");
    }

    public void FinishStudy()
    {
        currentPhase = StudyPhase.Complete;

        SetPanel(baselineInstructionsPanel, false);
        SetPanel(gamifiedInstructionsPanel, false);
        SetPanel(drawingToolsPanel, false);
        SetPanel(gamifiedHudPanel, false);
        SetPanel(endPanel, true);

        if (endText != null)
        {
            endText.text =
                "Task Complete\n\n" +
                "Thank you. Please remove the headset and inform the researcher.";
        }

        Debug.Log("[StudyFlowManager] Study complete.");
    }

    private void SetPanel(GameObject panel, bool active)
    {
        if (panel != null)
            panel.SetActive(active);
    }
}