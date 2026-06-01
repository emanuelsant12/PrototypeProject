using UnityEngine;
using TMPro;

public class GamifiedFeedbackManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject feedbackPanel;
    public TMP_Text goalText;
    public TMP_Text progressText;
    public TMP_Text feedbackText;

    [Header("Progress Thresholds")]
    public int step1StrokePoints = 5;
    public int step2StrokePoints = 15;
    public int step3StrokePoints = 30;

    private bool gamifiedMode;
    private int strokePointCount;
    private int currentStep;

    public void SetGamifiedMode(bool enabled)
    {
        gamifiedMode = enabled;

        if (feedbackPanel != null)
            feedbackPanel.SetActive(enabled);

        RefreshUI();
    }

    public void ResetGamifiedProgress()
    {
        strokePointCount = 0;
        currentStep = 0;

        if (feedbackText != null)
            feedbackText.text = "Start drawing to build progress.";

        RefreshUI();
    }

    public void RegisterStrokePoint()
    {
        if (!gamifiedMode)
            return;

        strokePointCount++;

        int newStep = CalculateStep();

        if (newStep != currentStep)
        {
            currentStep = newStep;
            UpdateFeedbackForStep();
        }

        RefreshUI();
    }

    private int CalculateStep()
    {
        if (strokePointCount >= step3StrokePoints)
            return 3;

        if (strokePointCount >= step2StrokePoints)
            return 2;

        if (strokePointCount >= step1StrokePoints)
            return 1;

        return 0;
    }

    private void UpdateFeedbackForStep()
    {
        if (feedbackText == null)
            return;

        switch (currentStep)
        {
            case 1:
                feedbackText.text = "Good start. Build the main shaded area.";
                break;

            case 2:
                feedbackText.text = "Progress made. Now focus on smoother transitions.";
                break;

            case 3:
                feedbackText.text = "Final check: is the light direction clear?";
                break;
        }
    }

    private void RefreshUI()
    {
        if (!gamifiedMode)
            return;

        if (goalText != null)
            goalText.text = "Goal: recreate the reference using smooth shading and clear light direction.";

        if (progressText != null)
            progressText.text = $"Progress: {currentStep}/3";
    }
}