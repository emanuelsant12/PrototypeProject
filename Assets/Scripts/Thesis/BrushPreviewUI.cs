using UnityEngine;
using UnityEngine.UI;

public class BrushPreviewUI : MonoBehaviour
{
    [Header("References")]
    public BrushSettings brushSettings;
    public RawImage previewImage;

    [Header("Preview Settings")]
    public int textureSize = 128;
    public int maxBrushSizeForPreview = 60;

    private Texture2D previewTexture;

    private int lastSize = -1;
    private float lastValue = -1f;
    private bool lastEraser;

    private void Start()
    {
        if (previewImage == null)
            previewImage = GetComponent<RawImage>();

        if (brushSettings == null)
        {
            Debug.LogError("[BrushPreviewUI] BrushSettings is missing.");
            return;
        }

        if (previewImage == null)
        {
            Debug.LogError("[BrushPreviewUI] Preview RawImage is missing.");
            return;
        }

        previewTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        previewTexture.wrapMode = TextureWrapMode.Clamp;
        previewTexture.filterMode = FilterMode.Point;

        previewImage.texture = previewTexture;

        // Important: preview should not block slider/button clicks.
        previewImage.raycastTarget = false;

        ForceRefresh();
    }

    private void Update()
    {
        if (brushSettings == null || previewImage == null)
            return;

        if (brushSettings.sizePx != lastSize ||
            Mathf.Abs(brushSettings.value - lastValue) > 0.001f ||
            brushSettings.eraser != lastEraser)
        {
            ForceRefresh();
        }
    }

    public void ForceRefresh()
    {
        if (brushSettings == null || previewImage == null)
            return;

        Color background = new Color(0.85f, 0.85f, 0.85f, 1f);

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                previewTexture.SetPixel(x, y, background);
            }
        }

        float value = brushSettings.eraser ? 1f : Mathf.Clamp01(brushSettings.value);
        Color brushColor = new Color(value, value, value, 1f);

        int previewRadius = Mathf.RoundToInt(
            Mathf.Lerp(6f, textureSize * 0.42f, brushSettings.sizePx / (float)maxBrushSizeForPreview)
        );

        previewRadius = Mathf.Clamp(previewRadius, 4, textureSize / 2 - 4);

        Vector2 center = new Vector2(textureSize / 2f, textureSize / 2f);

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);

                if (distance <= previewRadius)
                {
                    previewTexture.SetPixel(x, y, brushColor);
                }
            }
        }

        previewTexture.Apply();

        lastSize = brushSettings.sizePx;
        lastValue = brushSettings.value;
        lastEraser = brushSettings.eraser;

        Debug.Log("[BrushPreviewUI] Preview updated.");
    }
}
