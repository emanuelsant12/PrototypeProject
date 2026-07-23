using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BrushPreviewSimple : MonoBehaviour
{
    [Header("References")]
    public BrushSettings brushSettings;
    public Image previewDot;
    public TMP_Text previewLabel;

    [Header("Preview Size")]
    public float minPreviewSize = 12f;
    public float maxPreviewSize = 70f;
    public int maxBrushSize = 60;

    private void Update()
    {
        RefreshPreview();
    }

    private void RefreshPreview()
    {
        if (brushSettings == null)
        {
            Debug.LogError("[BrushPreviewSimple] BrushSettings is missing.");
            return;
        }

        if (previewDot == null)
        {
            Debug.LogError("[BrushPreviewSimple] Preview Dot Image is missing.");
            return;
        }

        float value = brushSettings.eraser ? 1f : Mathf.Clamp01(brushSettings.value);

        previewDot.color = new Color(value, value, value, 1f);

        float size01 = Mathf.Clamp01(brushSettings.sizePx / (float)maxBrushSize);
        float previewSize = Mathf.Lerp(minPreviewSize, maxPreviewSize, size01);

        RectTransform dotRect = previewDot.GetComponent<RectTransform>();
        dotRect.sizeDelta = new Vector2(previewSize, previewSize);

        if (previewLabel != null)
        {
            string tool = brushSettings.eraser ? "Eraser" : "Brush";
            int percent = Mathf.RoundToInt(value * 100f);
            previewLabel.text = $"{tool}\nValue: {percent}%\nSize: {brushSettings.sizePx}px";
        }
    }
}