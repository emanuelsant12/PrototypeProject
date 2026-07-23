using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BrushSliderControls : MonoBehaviour
{
    [Header("Brush Settings")]
    public BrushSettings brushSettings;

    [Header("Sliders")]
    public Slider brushValueSlider;
    public Slider brushSizeSlider;

    [Header("Optional Text Labels")]
    public TMP_Text brushValueText;
    public TMP_Text brushSizeText;

    private void Start()
    {
        if (brushSettings == null)
        {
            Debug.LogError("[BrushSliderControls] BrushSettings is missing.");
            return;
        }

        SetupValueSlider();
        SetupSizeSlider();
        RefreshText();
    }

    private void SetupValueSlider()
    {
        if (brushValueSlider == null)
            return;

        brushValueSlider.minValue = 0f;
        brushValueSlider.maxValue = 1f;
        brushValueSlider.wholeNumbers = false;
        brushValueSlider.value = brushSettings.value;

        brushValueSlider.onValueChanged.RemoveListener(SetBrushValue);
        brushValueSlider.onValueChanged.AddListener(SetBrushValue);
    }

    private void SetupSizeSlider()
    {
        if (brushSizeSlider == null)
            return;

        brushSizeSlider.minValue = 8f;
        brushSizeSlider.maxValue = 60f;
        brushSizeSlider.wholeNumbers = true;
        brushSizeSlider.value = brushSettings.sizePx;

        brushSizeSlider.onValueChanged.RemoveListener(SetBrushSize);
        brushSizeSlider.onValueChanged.AddListener(SetBrushSize);
    }

    public void SetBrushValue(float value)
    {
        if (brushSettings == null)
            return;

        brushSettings.value = Mathf.Clamp01(value);

        // Moving the shade slider should return to brush mode.
        brushSettings.eraser = false;

        RefreshText();

        Debug.Log("[BrushSliderControls] Brush value set to: " + brushSettings.value);
    }

    public void SetBrushSize(float size)
    {
        if (brushSettings == null)
            return;

        brushSettings.sizePx = Mathf.RoundToInt(size);

        RefreshText();

        Debug.Log("[BrushSliderControls] Brush size set to: " + brushSettings.sizePx);
    }

    private void RefreshText()
    {
        if (brushSettings == null)
            return;

        if (brushValueText != null)
        {
            int percent = Mathf.RoundToInt(brushSettings.value * 100f);
            brushValueText.text = "Value: " + percent + "%";
        }

        if (brushSizeText != null)
        {
            brushSizeText.text = "Size: " + brushSettings.sizePx + "px";
        }
    }
}