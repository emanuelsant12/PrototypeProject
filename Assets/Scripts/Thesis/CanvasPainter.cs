using UnityEngine;
using UnityEngine.InputSystem;

public class CanvasPainter : MonoBehaviour
{
    [Header("References")]
    public Transform brushTip;
    public RenderTexture canvasRT;
    public Texture2D brushStamp;
    public Material stampMaterial;
    public BrushSettings brushSettings;
    public SessionLogger sessionLogger;
    public ReferenceScoringManager referenceScoringManager;

    [Header("Raycast")]
    public LayerMask canvasLayer;
    public float rayDistance = 2f;

    [Header("Input Actions")]
    public InputActionReference drawAction;
    public InputActionReference resetAction;
    public InputActionReference submitAction;

    [Header("Keyboard Testing")]
    public Key resetKey = Key.C;
    public Key submitKey = Key.Enter;

    [Header("Stamp Shader Property Names")]
    public string brushTextureProperty = "_BrushTex";
    public string brushColorProperty = "_BrushColor";
    public string brushUvProperty = "_BrushUV";
    public string brushSizeProperty = "_BrushSize";

    [Header("Logging")]
    public float minLogDistanceUV = 0.0025f;

    [Header("Debug")]
    public bool debugLogs = false;

    private Vector2 lastLoggedUv;
    private bool hasLastLoggedUv;

    private Vector2 lastPaintUv;
    private bool hasLastPaintUv;
    private bool wasDrawingLastFrame;

    private void OnEnable()
    {
        EnableAction(drawAction);
        EnableAction(resetAction);
        EnableAction(submitAction);
    }

    private void OnDisable()
    {
        DisableAction(drawAction);
        DisableAction(resetAction);
        DisableAction(submitAction);
    }

    private void Start()
    {
        if (canvasRT != null && !canvasRT.IsCreated())
            canvasRT.Create();

        ClearCanvas();
    }

    private void Update()
    {
        if (WasResetPressed())
        {
            ClearCanvas();
        }

        if (WasSubmitPressed())
        {
            Submit();
        }

        bool isDrawingNow = IsDrawPressed();

        if (isDrawingNow)
        {
            TryPaint();
        }
        else
        {
            if (wasDrawingLastFrame && referenceScoringManager != null)
            {
                referenceScoringManager.EndCurrentBrushStroke();
            }

            hasLastLoggedUv = false;
            hasLastPaintUv = false;
        }

        wasDrawingLastFrame = isDrawingNow;
    }

    private void TryPaint()
    {
        if (brushTip == null || canvasRT == null || stampMaterial == null || brushStamp == null)
            return;

        Ray ray = new Ray(brushTip.position, brushTip.forward);

        if (!Physics.Raycast(ray, out RaycastHit hit, rayDistance, canvasLayer))
            return;

        Vector2 uv = hit.textureCoord;

        PaintSmoothStroke(uv);

        if (brushSettings == null || sessionLogger == null)
            return;

        bool shouldLog = !hasLastLoggedUv || Vector2.Distance(uv, lastLoggedUv) >= minLogDistanceUV;

        if (!shouldLog)
            return;

        StrokeScoreResult scoreResult = default;

        if (referenceScoringManager != null)
        {
            scoreResult = referenceScoringManager.RegisterStroke(
                uv,
                brushSettings.value,
                brushSettings.eraser
            );
        }

        sessionLogger.LogStrokePoint(
            uv,
            hit.point,
            brushSettings.sizePx,
            brushSettings.CurrentColor,
            brushSettings.eraser,
            scoreResult
        );

        lastLoggedUv = uv;
        hasLastLoggedUv = true;
    }

    private void PaintSmoothStroke(Vector2 currentUv)
    {
        int brushSizePx = brushSettings != null ? brushSettings.sizePx : 32;

        float brushSizeNormalized = brushSizePx / (float)Mathf.Max(canvasRT.width, canvasRT.height);

        // Smaller spacing = smoother stroke, but more expensive.
        float spacing = brushSizeNormalized * 0.35f;

        if (!hasLastPaintUv)
        {
            PaintAtUV(currentUv);
            lastPaintUv = currentUv;
            hasLastPaintUv = true;
            return;
        }

        float distance = Vector2.Distance(lastPaintUv, currentUv);

        int steps = Mathf.Max(1, Mathf.CeilToInt(distance / spacing));

        for (int i = 1; i <= steps; i++)
        {
            float t = i / (float)steps;
            Vector2 interpolatedUv = Vector2.Lerp(lastPaintUv, currentUv, t);
            PaintAtUV(interpolatedUv);
        }

        lastPaintUv = currentUv;
    }

    private void PaintAtUV(Vector2 uv)
    {
        Color brushColor = Color.black;
        int brushSizePx = 32;

        if (brushSettings != null)
        {
            brushColor = brushSettings.CurrentColor;
            brushSizePx = brushSettings.sizePx;
        }

        float brushSizeNormalized = brushSizePx / (float)Mathf.Max(canvasRT.width, canvasRT.height);

        RenderTexture temp = RenderTexture.GetTemporary(canvasRT.width, canvasRT.height, 0, canvasRT.format);

        Graphics.Blit(canvasRT, temp);

        stampMaterial.SetTexture("_MainTex", temp);
        stampMaterial.SetTexture(brushTextureProperty, brushStamp);
        stampMaterial.SetColor(brushColorProperty, brushColor);
        stampMaterial.SetVector(brushUvProperty, new Vector4(uv.x, uv.y, 0f, 0f));
        stampMaterial.SetFloat(brushSizeProperty, brushSizeNormalized);

        Graphics.Blit(temp, canvasRT, stampMaterial);

        RenderTexture.ReleaseTemporary(temp);
    }

    public void ClearCanvas()
    {
        if (canvasRT == null)
        {
            Debug.LogError("[CanvasPainter] Cannot clear. CanvasRT is missing.");
            return;
        }

        RenderTexture previous = RenderTexture.active;

        RenderTexture.active = canvasRT;
        GL.Clear(true, true, Color.white);

        RenderTexture.active = previous;

        hasLastLoggedUv = false;
        hasLastPaintUv = false;

        if (sessionLogger != null)
            sessionLogger.LogCanvasCleared();

        Debug.Log("[CanvasPainter] Canvas cleared.");
    }

    public void Submit()
    {
        SubmitWithCustomFileName("final.png");
    }

    public string SubmitWithCustomFileName(string fileName)
    {
        Debug.Log($"[CanvasPainter] SubmitWithCustomFileName called: {fileName}");

        if (sessionLogger == null)
        {
            Debug.LogError("[CanvasPainter] Cannot submit because SessionLogger is missing.");
            return null;
        }

        if (canvasRT == null)
        {
            Debug.LogError("[CanvasPainter] Cannot submit because CanvasRT is missing.");
            return null;
        }

        string savedPath = sessionLogger.SaveRenderTextureAsPng(canvasRT, fileName);

        if (string.IsNullOrEmpty(savedPath))
        {
            Debug.LogError("[CanvasPainter] Submit failed.");
        }
        
        Debug.Log($"[CanvasPainter] Saved: {savedPath}");
        return savedPath;
        

        
    }

    private bool IsDrawPressed()
    {
        if (drawAction == null || drawAction.action == null)
            return false;

        return drawAction.action.ReadValue<float>() > 0.5f;
    }

    private bool WasResetPressed()
    {
        if (resetAction != null && resetAction.action != null && resetAction.action.WasPressedThisFrame())
            return true;

        return Keyboard.current != null && Keyboard.current[resetKey].wasPressedThisFrame;
    }

    private bool WasSubmitPressed()
    {
        if (submitAction != null && submitAction.action != null && submitAction.action.WasPressedThisFrame())
        {
            Debug.Log("[CanvasPainter] Submit action pressed.");
            return true;
        }

        if (Keyboard.current != null && Keyboard.current[submitKey].wasPressedThisFrame)
        {
            Debug.Log("[CanvasPainter] Submit keyboard key pressed.");
            return true;
        }

        return false;
    }

    private static void EnableAction(InputActionReference actionReference)
    {
        if (actionReference != null && actionReference.action != null)
            actionReference.action.Enable();
    }

    private static void DisableAction(InputActionReference actionReference)
    {
        if (actionReference != null && actionReference.action != null)
            actionReference.action.Disable();
    }
}