using UnityEngine;

public class VRPaintButtonActions : MonoBehaviour
{
    [Header("References")]
    public CanvasPainter canvasPainter;
    public BrushSettings brushSettings;
    public BrushPreviewUI brushPreviewUI;

    [Header("Brush Step Settings")]
    public int sizeStep = 8;
    public int minSize = 4;
    public int maxSize = 128;

    [Range(0.01f, 0.5f)]
    public float valueStep = 0.1f;

    private void Start()
    {
        if (brushPreviewUI != null)
            brushPreviewUI.ForceRefresh();
    }

    public void ClearCanvas()
    {
        if (canvasPainter == null)
        {
            Debug.LogError("[VRPaintButtonActions] CanvasPainter missing.");
            return;
        }

        canvasPainter.ClearCanvasToWhite();
    }

    public void SubmitPainting()
    {
        if (canvasPainter == null)
        {
            Debug.LogError("[VRPaintButtonActions] CanvasPainter missing.");
            return;
        }

        canvasPainter.Submit();
    }

    public void IncreaseBrushSize()
    {
        if (brushSettings == null)
            return;

        brushSettings.sizePx = Mathf.Clamp(brushSettings.sizePx + sizeStep, minSize, maxSize);
        Debug.Log($"[VRPaintButtonActions] Brush size: {brushSettings.sizePx}");

        if (brushPreviewUI != null)
            brushPreviewUI.ForceRefresh();
    }

    public void DecreaseBrushSize()
    {
        if (brushSettings == null)
            return;

        brushSettings.sizePx = Mathf.Clamp(brushSettings.sizePx - sizeStep, minSize, maxSize);
        Debug.Log($"[VRPaintButtonActions] Brush size: {brushSettings.sizePx}");

        if (brushPreviewUI != null)
            brushPreviewUI.ForceRefresh();
    }

    public void MakeDarker()
    {
        if (brushSettings == null)
            return;

        brushSettings.eraser = false;
        brushSettings.value = Mathf.Clamp01(brushSettings.value - valueStep);
        Debug.Log($"[VRPaintButtonActions] Brush value: {brushSettings.value}");

        if (brushPreviewUI != null)
            brushPreviewUI.ForceRefresh();
    }

    public void MakeLighter()
    {
        if (brushSettings == null)
            return;

        brushSettings.eraser = false;
        brushSettings.value = Mathf.Clamp01(brushSettings.value + valueStep);
        Debug.Log($"[VRPaintButtonActions] Brush value: {brushSettings.value}");

        if (brushPreviewUI != null)
            brushPreviewUI.ForceRefresh();
    }

    public void ToggleEraser()
    {
        if (brushSettings == null)
            return;

        brushSettings.eraser = !brushSettings.eraser;
        Debug.Log($"[VRPaintButtonActions] Eraser: {brushSettings.eraser}");

        if (brushPreviewUI != null)
            brushPreviewUI.ForceRefresh();
    }

    public void SetBrush()
    {
        if (brushSettings == null)
            return;

        brushSettings.eraser = false;

        if (brushPreviewUI != null)
            brushPreviewUI.ForceRefresh();
    }

    public void SetEraser()
    {
        if (brushSettings == null)
            return;

        brushSettings.eraser = true;

        if (brushPreviewUI != null)
            brushPreviewUI.ForceRefresh();
    }
}