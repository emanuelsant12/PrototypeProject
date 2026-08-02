using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class GreyscalePickerUI : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [Header("References")]
    public BrushSettings brushSettings;
    public RawImage pickerImage;
    public RectTransform pickerHandle;

    [Header("Optional Text")]
    public TMP_Text valueText;

    [Header("Texture")]
    public int textureWidth = 256;
    public int textureHeight = 32;

    private RectTransform pickerRect;
    private Texture2D gradientTexture;

    private void Awake()
    {
        if (pickerImage == null)
            pickerImage = GetComponent<RawImage>();

        pickerRect = GetComponent<RectTransform>();
    }

    private void Start()
    {
        if (brushSettings == null)
        {
            Debug.LogError("[GreyscalePickerUI] BrushSettings is missing.");
            return;
        }

        if (pickerImage == null)
        {
            Debug.LogError("[GreyscalePickerUI] Picker RawImage is missing.");
            return;
        }

        GenerateGradientTexture();

        pickerImage.raycastTarget = true;

        RefreshVisuals();

        Debug.Log("[GreyscalePickerUI] Ready.");
    }

    private void GenerateGradientTexture()
    {
        gradientTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        gradientTexture.wrapMode = TextureWrapMode.Clamp;
        gradientTexture.filterMode = FilterMode.Bilinear;

        for (int x = 0; x < textureWidth; x++)
        {
            float value = x / (float)(textureWidth - 1);
            Color color = new Color(value, value, value, 1f);

            for (int y = 0; y < textureHeight; y++)
            {
                gradientTexture.SetPixel(x, y, color);
            }
        }

        gradientTexture.Apply();

        pickerImage.texture = gradientTexture;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        SetValueFromPointer(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        SetValueFromPointer(eventData);
    }

    private void SetValueFromPointer(PointerEventData eventData)
    {
        if (brushSettings == null || pickerRect == null)
            return;

        bool gotPoint = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            pickerRect,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint
        );

        if (!gotPoint)
            return;

        Rect rect = pickerRect.rect;

        float normalizedX = Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x);
        normalizedX = Mathf.Clamp01(normalizedX);

        brushSettings.value = normalizedX;
        brushSettings.eraser = false;

        RefreshVisuals();

        Debug.Log("[GreyscalePickerUI] Brush shade set to: " + brushSettings.value);
    }

    public void RefreshVisuals()
    {
        if (brushSettings == null)
            return;

        float value = Mathf.Clamp01(brushSettings.value);

        if (pickerHandle != null && pickerRect != null)
        {
            Rect rect = pickerRect.rect;

            float x = Mathf.Lerp(rect.xMin, rect.xMax, value);

            pickerHandle.anchoredPosition = new Vector2(
                x,
                pickerHandle.anchoredPosition.y
            );
        }

        if (valueText != null)
        {
            int percent = Mathf.RoundToInt(value * 100f);
            valueText.text = "Shade: " + percent + "%";
        }
    }
}