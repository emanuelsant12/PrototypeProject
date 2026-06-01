using UnityEngine;
using TMPro;

public class GamificationManager : MonoBehaviour
{
    [Header("Mode")]
    public bool gamifiedMode = false;

    [Header("Scoring")]
    public int score = 0;
    public int strokePoints = 0;
    public int targetStrokePoints = 100;

    [Header("Timer")]
    public float taskTimeSeconds = 180f;
    public bool timerRunning = false;
    private float remainingTime;

    [Header("UI")]
    public TMP_Text scoreText;
    public TMP_Text progressText;
    public TMP_Text feedbackText;
    public TMP_Text timerText;

    public void SetGamifiedMode(bool enabled)
    {
        gamifiedMode = enabled;

        score = 0;
        strokePoints = 0;
        remainingTime = taskTimeSeconds;
        timerRunning = enabled;

        RefreshUI();

        if (feedbackText != null)
        {
            feedbackText.text = enabled
                ? "Start shading. Build up smooth values."
                : "";
        }
    }

    private void Update()
    {
        if (!gamifiedMode || !timerRunning)
            return;

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            timerRunning = false;

            if (feedbackText != null)
                feedbackText.text = "Time complete. Submit your result.";
        }

        RefreshUI();
    }

    public void RegisterStrokePoint(float brushValue, bool eraser)
    {
        if (!gamifiedMode)
            return;

        strokePoints++;

        if (!eraser)
        {
            score += 1;
        }

        if (strokePoints % 20 == 0)
        {
            GiveFeedback(brushValue);
        }

        RefreshUI();
    }

    private void GiveFeedback(float brushValue)
    {
        if (feedbackText == null)
            return;

        if (brushValue < 0.25f)
        {
            feedbackText.text = "Good dark values. Now try smoother transitions.";
        }
        else if (brushValue > 0.75f)
        {
            feedbackText.text = "Light value selected. Use this for highlights or corrections.";
        }
        else
        {
            feedbackText.text = "Mid value selected. Good for soft shading transitions.";
        }
    }

    private void RefreshUI()
    {
        if (scoreText != null)
            scoreText.text = $"Score: {score}";

        if (progressText != null)
        {
            float progress = Mathf.Clamp01(strokePoints / (float)targetStrokePoints);
            progressText.text = $"Progress: {Mathf.RoundToInt(progress * 100f)}%";
        }

        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(remainingTime / 60f);
            int seconds = Mathf.FloorToInt(remainingTime % 60f);
            timerText.text = $"{minutes:00}:{seconds:00}";
        }
    }
}