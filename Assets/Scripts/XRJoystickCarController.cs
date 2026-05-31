using UnityEngine;

public class XRJoystickCarController : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Reference to the joystick script in your scene.")]
    public XRJoystickDrive joystick;

    [Header("Tuning")]
    [Tooltip("Meters per second at full forward.")]
    public float moveSpeed = 4f;

    [Tooltip("Degrees per second at full steer.")]
    public float turnSpeed = 120f;

    [Tooltip("If true, car only turns while moving.")]
    public bool onlyTurnWhileMoving = true;

    void Update()
    {
        if (!joystick) return;

        // Read normalized joystick input
        Vector2 input = joystick.value; // x: left/right, y: forward/back in [-1,1]

        // Forward/back movement (local Z)
        float move = input.y * moveSpeed * Time.deltaTime;

        // Yaw (turn) around up axis
        float turn = input.x * turnSpeed * Time.deltaTime;
        if (onlyTurnWhileMoving) turn *= Mathf.Abs(input.y); //Mathf.Abs() ignores the sign and uses only the magnitude of the forward/backward movement to scale the turning.

        // Apply movement
        transform.Rotate(0f, turn, 0f);         // rotate in place
        transform.Translate(0f, 0f, move);      // move along local forward
    }
}
