using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InstantXRSlider : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    public Slider slider;
    public RectTransform hitArea;

    private void Awake()
    {
        if (slider == null)
            slider = GetComponent<Slider>();

        if (hitArea == null)
            hitArea = GetComponent<RectTransform>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        SetSliderFromPointer(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        SetSliderFromPointer(eventData);
    }

    private void SetSliderFromPointer(PointerEventData eventData)
    {
        if (slider == null || hitArea == null)
            return;

        Vector2 localPoint;

        if (eventData.pointerCurrentRaycast.gameObject != null)
        {
            Vector3 worldPoint = eventData.pointerCurrentRaycast.worldPosition;
            Vector3 localPoint3D = hitArea.InverseTransformPoint(worldPoint);
            localPoint = new Vector2(localPoint3D.x, localPoint3D.y);
        }
        else
        {
            bool gotPoint = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                hitArea,
                eventData.position,
                eventData.pressEventCamera,
                out localPoint
            );

            if (!gotPoint)
                return;
        }

        Rect rect = hitArea.rect;

        float normalizedValue;

        if (slider.direction == Slider.Direction.LeftToRight ||
            slider.direction == Slider.Direction.RightToLeft)
        {
            normalizedValue = Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x);
        }
        else
        {
            normalizedValue = Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y);
        }

        if (slider.direction == Slider.Direction.RightToLeft ||
            slider.direction == Slider.Direction.TopToBottom)
        {
            normalizedValue = 1f - normalizedValue;
        }

        slider.normalizedValue = Mathf.Clamp01(normalizedValue);
    }
}