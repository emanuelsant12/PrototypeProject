using UnityEngine;
using UnityEngine.InputSystem;

public class CanvasPainter_Input : CanvasPainter
{
    [Header("Input")]
    public InputActionReference drawAction; // bind to RightHand Activate/Select

    void OnEnable()
    {
        if (drawAction != null) drawAction.action.Enable();
    }

    void OnDisable()
    {
        if (drawAction != null) drawAction.action.Disable();
    }
    /*
    new void Update()
    {
        bool pressed = drawAction != null && drawAction.action.ReadValue<float>() > 0.5f;

        if (pressed && !isDrawing) StartDrawing();
        if (!pressed && isDrawing) StopDrawing();

        if (isDrawing) TryPaint();
    }
    */
}