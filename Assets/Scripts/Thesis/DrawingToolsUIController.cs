using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DrawingToolsUIController : MonoBehaviour
{
    [Header("Core References")]
    public BrushSettings brushSettings;
    public CanvasPainter canvasPainter;
    public StudyFlowManager studyFlowManager;

    [Header("Sliders")]
    public Slider brushShadeSlider;
    public Slider brushSizeSlider;

    [Header("Tool Dropdown")]
    public Dropdown legacyToolDropdown;
    public TMP_Dropdown tmpToolDropdown;

    [Header("Buttons")]
    public Button submitButton;
    public Button clearButton;

    [Header("Optional Text Labels")]
    public TMP_Text shadeValueText;
    public TMP_Text sizeValueText;
    public TMP_Text currentToolText;

    

    private void Start()
    {
        if (brushSettings == null)
        {
            Debug.LogError("[DrawingToolsUIController] BrushSettings is missing.");
            return;
        }

        SetupShadeSlider();
        SetupSizeSlider();
        SetupDropdown();
        SetupButtons();

        RefreshLabels();

        Debug.Log("[DrawingToolsUIController] Drawing tools connected.");
    }

    private void SetupShadeSlider()
    {
        if (brushShadeSlider == null)
        {
            Debug.LogWarning("[DrawingToolsUIController] Brush Shade Slider is not assigned.");
            return;
        }

        brushShadeSlider.minValue = 0f;
        brushShadeSlider.maxValue = 1f;
        brushShadeSlider.wholeNumbers = false;
        brushShadeSlider.value = brushSettings.value;

        brushShadeSlider.onValueChanged.RemoveListener(SetBrushShade);
        brushShadeSlider.onValueChanged.AddListener(SetBrushShade);
    }

    private void SetupSizeSlider()
    {
        if (brushSizeSlider == null)
        {
            Debug.LogWarning("[DrawingToolsUIController] Brush Size Slider is not assigned.");
            return;
        }

        brushSizeSlider.minValue = 8f;
        brushSizeSlider.maxValue = 60f;
        brushSizeSlider.wholeNumbers = true;
        brushSizeSlider.value = brushSettings.sizePx;

        brushSizeSlider.onValueChanged.RemoveListener(SetBrushSize);
        brushSizeSlider.onValueChanged.AddListener(SetBrushSize);
    }

    private void SetupDropdown()
    {
        if (legacyToolDropdown != null)
        {
            legacyToolDropdown.ClearOptions();
            legacyToolDropdown.AddOptions(new System.Collections.Generic.List<string>
            {
                "Brush",
                "Eraser"
            });

            legacyToolDropdown.value = brushSettings.eraser ? 1 : 0;

            legacyToolDropdown.onValueChanged.RemoveListener(SetToolFromDropdown);
            legacyToolDropdown.onValueChanged.AddListener(SetToolFromDropdown);
        }

        if (tmpToolDropdown != null)
        {
            tmpToolDropdown.ClearOptions();
            tmpToolDropdown.AddOptions(new System.Collections.Generic.List<string>
            {
                "Brush",
                "Eraser"
            });

            tmpToolDropdown.value = brushSettings.eraser ? 1 : 0;

            tmpToolDropdown.onValueChanged.RemoveListener(SetToolFromDropdown);
            tmpToolDropdown.onValueChanged.AddListener(SetToolFromDropdown);
        }
    }

    private void SetupButtons()
    {
        if (submitButton != null)
        {
            submitButton.onClick.RemoveListener(SubmitCurrentTask);
            submitButton.onClick.AddListener(SubmitCurrentTask);
        }
        else
        {
            Debug.LogWarning("[DrawingToolsUIController] Submit Button is not assigned.");
        }

        if (clearButton != null)
        {
            clearButton.onClick.RemoveListener(ClearCanvas);
            clearButton.onClick.AddListener(ClearCanvas);
        }
        else
        {
            Debug.LogWarning("[DrawingToolsUIController] Clear Button is not assigned.");
        }
    }

    public void SetBrushShade(float value)
    {
        if (brushSettings == null)
            return;

        brushSettings.value = Mathf.Clamp01(value);

        // Moving the shade slider should automatically return to brush mode.
        brushSettings.eraser = false;

        SetDropdownValueWithoutNotify(0);
        RefreshLabels();

        Debug.Log("[DrawingToolsUIController] Brush shade set to: " + brushSettings.value);
    }

    public void SetBrushSize(float value)
    {
        if (brushSettings == null)
            return;

        brushSettings.sizePx = Mathf.RoundToInt(Mathf.Clamp(value, 8f, 60f));

        RefreshLabels();

        Debug.Log("[DrawingToolsUIController] Brush size set to: " + brushSettings.sizePx);
    }

    public void SetToolFromDropdown(int index)
    {
        if (brushSettings == null)
            return;

        if (index == 0)
        {
            brushSettings.eraser = false;
            Debug.Log("[DrawingToolsUIController] Tool selected: Brush");
        }
        else if (index == 1)
        {
            brushSettings.eraser = true;
            Debug.Log("[DrawingToolsUIController] Tool selected: Eraser");
        }

        RefreshLabels();
    }

    public void SubmitCurrentTask()
    {
        if (studyFlowManager == null)
        {
            Debug.LogError("[DrawingToolsUIController] Cannot submit. StudyFlowManager is missing.");
            return;
        }

        studyFlowManager.SubmitCurrentTask();

        Debug.Log("[DrawingToolsUIController] Submit pressed.");
    }

    public void ClearCanvas()
    {
        if (canvasPainter == null)
        {
            Debug.LogError("[DrawingToolsUIController] Cannot clear. CanvasPainter is missing.");
            return;
        }

        canvasPainter.ClearCanvas();

        Debug.Log("[DrawingToolsUIController] Clear pressed.");
    }

    private void SetDropdownValueWithoutNotify(int value)
    {
        if (legacyToolDropdown != null)
            legacyToolDropdown.SetValueWithoutNotify(value);

        if (tmpToolDropdown != null)
            tmpToolDropdown.SetValueWithoutNotify(value);
    }

    private void RefreshLabels()
    {
        if (brushSettings == null)
            return;

        if (shadeValueText != null)
        {
            int percent = Mathf.RoundToInt(brushSettings.value * 100f);
            shadeValueText.text = "Shade: " + percent + "%";
        }

        if (sizeValueText != null)
        {
            sizeValueText.text = "Size: " + brushSettings.sizePx + "px";
        }

        if (currentToolText != null)
        {
            currentToolText.text = brushSettings.eraser ? "Tool: Eraser" : "Tool: Brush";
        }
    }
}