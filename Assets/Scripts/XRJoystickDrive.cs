using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;
using UnityEngine.Events;

public class XRJoystickDrive : MonoBehaviour
{
    [Header("Refs")]
    public XRGrabInteractable grabHandle; // The part of the joystick the user physically grabs.
    public Transform rotationPart;        // the cylinder that tilts

    [Header("Tuning")]
    public float maxDisplacement = 0.09f; // Maximum distance the handle can move from its starting position before hitting full tilt.
    public float maxTiltAngle = 25f;     // Maximum angle (in degrees) to visually tilt the rotationPart.
    public float deadZone = 0.01f;   // Threshold to ignore tiny accidental movements or jitter.

    [Header("Output")]
    [Tooltip("Normalized joystick value in [-1,1] (x=right/left, y=forward/back)")]
    public Vector2 value;// Current joystick position normalized to range -1..1.


    // Stores the original position and rotation of the grab handle
    // (used for resetting when the joystick is released).
    Vector3 startPos;
    Quaternion startRot;

    void Awake()
    {
        // Ensure grabHandle reference exists; fallback to this object if missing.
        if (!grabHandle) grabHandle = GetComponent<XRGrabInteractable>();
        // Listen for the "select exited" event (when user releases the grab).
        grabHandle.selectExited.AddListener(OnSelectExited);
    }

    private void Start()
    {
        // Save the initial position and rotation of the handle at startup.
        // This is the "rest" pose where the joystick will return when released.
        startPos = grabHandle.transform.position;
        startRot = grabHandle.transform.rotation;

        SetValue(Vector2.zero);

    }

    void OnDestroy()
    {
        // Clean up the event listener to avoid memory leaks or dangling references.
        grabHandle.selectExited.RemoveListener(OnSelectExited);
    }


    void OnSelectExited(SelectExitEventArgs _)
    {
        // Reset the visual tilt back to neutral.
        if (rotationPart) rotationPart.localRotation = Quaternion.identity;
        value = Vector2.zero;    // Reset the output value to zero (center position).

        // Start coroutine to snap the grab handle back to its original position.
        // Done after physics finishes to avoid fighting XR/physics updates.
        StartCoroutine(SnapBackRoutine());
    }

    IEnumerator SnapBackRoutine()
    {
        // Wait until physics step completes before applying the reset position/rotation.
        yield return new WaitForFixedUpdate();
        // Instantly move the grab handle back to its original rest position and rotation.
        grabHandle.transform.SetPositionAndRotation(startPos, startRot);
        
    }

    void Update()
    {
        // If not currently selected, nothing to compute
        if (grabHandle.interactorsSelecting.Count == 0) return;

        // Calculate how far the handle has moved in world space from the rest position.
        Vector3 delta = grabHandle.transform.position - startPos;

        // Convert that world-space movement into the joystick's local space axes:
        // dx = movement along local X (left/right), dz = movement along local Z (forward/back).
        Transform t = transform;
        float dx = Vector3.Dot(delta, t.right);
        float dz = Vector3.Dot(delta, t.forward);

        // Apply dead zone: ignore tiny movements below the threshold.
        if (Mathf.Abs(dx) < deadZone) dx = 0f;
        if (Mathf.Abs(dz) < deadZone) dz = 0f;

        // Normalize movement to range [-1,1] by dividing by maxDisplacement and clamping.
        float nx = Mathf.Clamp(dx / maxDisplacement, -1f, 1f);
        float ny = Mathf.Clamp(dz / maxDisplacement, -1f, 1f);
        SetValue(new Vector2(nx, ny));         // Update the output value with the normalized coordinates.

        // Tilt the visual rotationPart to simulate the joystick angle:
        // - Forward/back movement rotates around X-axis.
        // - Left/right movement rotates around Z-axis (negative sign makes it feel natural).
        if (rotationPart)
        {
            float tiltX = ny * maxTiltAngle;
            float tiltZ = -nx * maxTiltAngle; // negative for intuitive right tilt
            rotationPart.localRotation = Quaternion.Euler(tiltX, 0f, tiltZ);
        }
    }

    void SetValue(Vector2 v)
    {
        value = v;     // Store the normalized joystick value for other scripts to use.
    }

}


