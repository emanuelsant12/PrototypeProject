using UnityEngine;
using UnityEngine.UI;

public class BrushPreviewUI : MonoBehaviour
{
    [Header("References")]
    public BrushSettings brushSettings;
    public RawImage previewImage;

    [Header("Preview Settings")]
    public int textureWidth = 256;
    public int textureHeight = 128;

    [Tooltip("How much smaller the preview brush is compared to the real brush size.")]
    public float previewScale = 1.5f;

    [Header("Background")]
    public Color backgroundColor = Color.white;

    private Texture2D previewTexture;
    private int lastSize;
    private float lastValue;
    private bool lastEraser;

    private void Start()
    {
        CreatePreviewTexture();
        ForceRefresh();
    }

    private void Update()
    {
        if (brushSettings == null)
            return;

        if (HasBrushChanged())
        {
            RefreshPreview();
        }
    }

    private void CreatePreviewTexture()
    {
        previewTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        previewTexture.filterMode = FilterMode.Point;

        if (previewImage != null)
            previewImage.texture = previewTexture;
    }

    public void ForceRefresh()
    {
        lastSize = -1;
        lastValue = -1f;
        lastEraser = !GetCurrentEraser();
        RefreshPreview();
    }

    private bool HasBrushChanged()
    {
        return lastSize != brushSettings.sizePx ||
               !Mathf.Approximately(lastValue, brushSettings.value) ||
               lastEraser != brushSettings.eraser;
    }

    private void RefreshPreview()
    {
        if (previewTexture == null)
            CreatePreviewTexture();

        ClearTexture();

        DrawPreviewStroke();

        previewTexture.Apply();

        lastSize = brushSettings.sizePx;
        lastValue = brushSettings.value;
        lastEraser = brushSettings.eraser;
    }

    private void ClearTexture()
    {
        Color32 bg = backgroundColor;

        Color32[] pixels = new Color32[textureWidth * textureHeight];

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = bg;

        previewTexture.SetPixels32(pixels);
    }

    private void DrawPreviewStroke()
    {
        if (brushSettings == null)
            return;

        Color brushColor = brushSettings.CurrentColor;

        // If eraser is active, draw a grey outline so the player can see the eraser size.
        bool isEraser = brushSettings.eraser;

        int previewSize = Mathf.RoundToInt(brushSettings.sizePx / previewScale);
        previewSize = Mathf.Clamp(previewSize, 4, 96);

        Vector2 start = new Vector2(textureWidth * 0.25f, textureHeight * 0.5f);
        Vector2 end = new Vector2(textureWidth * 0.75f, textureHeight * 0.5f);

        int steps = 32;

        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;

            Vector2 pos = Vector2.Lerp(start, end, t);

            // Slight curve so it looks like a stroke, not just a boring straight line.
            pos.y += Mathf.Sin(t * Mathf.PI) * 18f;

            DrawCircle(
                Mathf.RoundToInt(pos.x),
                Mathf.RoundToInt(pos.y),
                previewSize,
                brushColor
            );
        }

        if (isEraser)
        {
            DrawCircleOutline(
                Mathf.RoundToInt(textureWidth * 0.5f),
                Mathf.RoundToInt(textureHeight * 0.5f),
                previewSize,
                Color.gray
            );
        }
    }

    private void DrawCircle(int centerX, int centerY, int radius, Color color)
    {
        int radiusSquared = radius * radius;

        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                int distanceSquared = x * x + y * y;

                if (distanceSquared > radiusSquared)
                    continue;

                int px = centerX + x;
                int py = centerY + y;

                if (px < 0 || px >= textureWidth || py < 0 || py >= textureHeight)
                    continue;

                float distance = Mathf.Sqrt(distanceSquared);
                float edgeFade = Mathf.Clamp01(1f - (distance / radius));

                // Soft edge, otherwise the preview looks ugly and fake.
                float alpha = Mathf.SmoothStep(0f, 1f, edgeFade);

                Color existing = previewTexture.GetPixel(px, py);
                Color blended = Color.Lerp(existing, color, alpha);

                previewTexture.SetPixel(px, py, blended);
            }
        }
    }

    private void DrawCircleOutline(int centerX, int centerY, int radius, Color color)
    {
        int thickness = 2;

        for (int y = -radius - thickness; y <= radius + thickness; y++)
        {
            for (int x = -radius - thickness; x <= radius + thickness; x++)
            {
                float distance = Mathf.Sqrt(x * x + y * y);

                if (distance < radius - thickness || distance > radius + thickness)
                    continue;

                int px = centerX + x;
                int py = centerY + y;

                if (px < 0 || px >= textureWidth || py < 0 || py >= textureHeight)
                    continue;

                previewTexture.SetPixel(px, py, color);
            }
        }
    }

    private bool GetCurrentEraser()
    {
        return brushSettings != null && brushSettings.eraser;
    }
}