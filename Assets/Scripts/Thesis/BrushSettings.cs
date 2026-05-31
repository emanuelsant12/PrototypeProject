using UnityEngine;

public class BrushSettings : MonoBehaviour
{
    [Header("Brush")]
    [Min(1)]
    public int sizePx = 32;

    [Range(0f, 1f)]
    public float value = 0f; // 0 = black, 1 = white

    [Header("Tools")]
    public bool eraser = false;

    public Color CurrentColor
    {
        get
        {
            float v = eraser ? 1f : value;
            return new Color(v, v, v, 1f);
        }
    }

    public void SetSize(float newSize)
    {
        sizePx = Mathf.Max(1, Mathf.RoundToInt(newSize));
    }

    public void SetValue(float newValue)
    {
        value = Mathf.Clamp01(newValue);
    }

    public void SetEraser(bool isEraser)
    {
        eraser = isEraser;
    }
}