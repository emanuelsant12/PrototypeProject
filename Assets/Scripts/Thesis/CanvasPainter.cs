using UnityEngine;
using UnityEngine.InputSystem;

public class CanvasPainter : MonoBehaviour
{
    public GamificationManager gamificationManager;

    [Header("References")]
    public Transform brushTip;
    public RenderTexture canvasRT;
    public Texture2D brushStamp;
    public Material stampMaterial;
    public BrushSettings brushSettings;
    public SessionLogger sessionLogger;

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

    [Header("Gamified Feedback")]
    public GamifiedFeedbackManager gamifiedFeedback;

    [Header("Debug")]
    public bool debugLogs = false;

    private Vector2 lastLoggedUv;
    private bool hasLastLoggedUv;

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

        ClearCanvasToWhite();
    }

    private void Update()
    {
        if (WasResetPressed())
        {
            ClearCanvasToWhite();
        }

        if (WasSubmitPressed())
        {
            Submit();
        }

        if (IsDrawPressed())
        {
            TryPaint();
        }
        else
        {
            hasLastLoggedUv = false;
        }
    }

    private void TryPaint()
    {
        if (brushTip == null || canvasRT == null || stampMaterial == null || brushStamp == null)
            return;

        Ray ray = new Ray(brushTip.position, brushTip.forward);

        if (!Physics.Raycast(ray, out RaycastHit hit, rayDistance, canvasLayer))
            return;

        Vector2 uv = hit.textureCoord;

        PaintAtUV(uv);

        if (brushSettings != null && sessionLogger != null)
        {
            bool shouldLog = !hasLastLoggedUv || Vector2.Distance(uv, lastLoggedUv) >= minLogDistanceUV;

            if (shouldLog)
            {
                sessionLogger.LogStrokePoint(
                    uv,
                    hit.point,
                    brushSettings.sizePx,
                    brushSettings.CurrentColor,
                    brushSettings.eraser
                );

                if (gamificationManager != null)
                {
                    gamificationManager.RegisterStrokePoint(
                        brushSettings.value,
                        brushSettings.eraser
                    );
                }

                lastLoggedUv = uv;
                hasLastLoggedUv = true;
            }
        }
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

        stampMaterial.SetTexture(brushTextureProperty, brushStamp);
        stampMaterial.SetColor(brushColorProperty, brushColor);
        stampMaterial.SetVector(brushUvProperty, new Vector4(uv.x, uv.y, 0f, 0f));
        stampMaterial.SetFloat(brushSizeProperty, brushSizeNormalized);

        Graphics.Blit(temp, canvasRT, stampMaterial);

        RenderTexture.ReleaseTemporary(temp);
    }

    public void ClearCanvasToWhite()
    {
        if (canvasRT == null)
            return;

        RenderTexture previous = RenderTexture.active;

        if (!canvasRT.IsCreated())
            canvasRT.Create();

        RenderTexture.active = canvasRT;
        GL.Clear(true, true, Color.white);
        RenderTexture.active = previous;

        hasLastLoggedUv = false;

        if (sessionLogger != null)
            sessionLogger.LogCanvasCleared();

        if (debugLogs)
            Debug.Log("[CanvasPainter] Canvas cleared to white.");
    }

    public void Submit()
    {
        SubmitWithCustomFileName("final.png");
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

    public void SubmitWithCustomFileName(string fileName)
    {
        Debug.Log($"[CanvasPainter] SubmitWithCustomFileName called: {fileName}");

        if (sessionLogger == null)
        {
            Debug.LogError("[CanvasPainter] Cannot submit because SessionLogger is missing.");
            return;
        }

        if (canvasRT == null)
        {
            Debug.LogError("[CanvasPainter] Cannot submit because CanvasRT is missing.");
            return;
        }

        string savedPath = sessionLogger.SaveRenderTextureAsPng(canvasRT, fileName);

        if (string.IsNullOrEmpty(savedPath))
        {
            Debug.LogError("[CanvasPainter] Submit failed.");
        }
        else
        {
            Debug.Log($"[CanvasPainter] Saved: {savedPath}");
        }
    }
}