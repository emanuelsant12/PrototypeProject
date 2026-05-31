using UnityEngine;
using UnityEngine.InputSystem;

public class SimpleGripCurl : MonoBehaviour
{
    [Header("Input (XRI Select Value)")]
    public InputActionProperty gripValue; // XRI Left/Right Interaction / Select Value
    public InputActionProperty triggerValue;    // XRI Left/Right Interaction / Activate Value
    public InputActionProperty stickClickValue; // XRI Left/Right Interaction / Stick Click

    [Header("Middle finger joints (base -> tip)")]
    public Transform[] middleJoints;

    [Header("Ring finger joints (base -> tip)")]
    public Transform[] ringJoints;

    [Header("Little finger joints (base -> tip)")]
    public Transform[] littleJoints;

    [Header("Index finger joints (base -> tip)")]
    public Transform[] indexJoints;

    [Header("Thumb joints (base -> tip)")]
    public Transform[] thumbJoints;


    [Header("Rotation around local X when fully pressed (degrees)")]
    public float middleMax = -65f;
    public float ringMax = -65f;
    public float littleMax = -60f;
    public float indexMax = -65f;
    public float thumbMax = -30f;

    // cache initial rotations
    Quaternion[] mid0, ring0, lit0 ,index0, thumb0;

    void Awake()
    {
        mid0 = Cache(middleJoints);
        ring0 = Cache(ringJoints);
        lit0 = Cache(littleJoints);
        index0 = Cache(indexJoints);
        thumb0 = Cache(thumbJoints);
    }

    void OnEnable()
    {
        // Check if the grip action is assigned in the Inspector
        if (gripValue.action != null)
        {
            // Enable the input action so we can use ReadValue<float>()
            gripValue.action.Enable();
        }

        if (triggerValue.action != null)
        {
            triggerValue.action.Enable();
        }

        if (stickClickValue.action != null)
        {
            stickClickValue.action.Enable();
        }
    }


    void OnDisable()
    {
        // Check if the grip action is assigned in the Inspector
        if (gripValue.action != null)
        {
            // Disable the input action to free up resources
            gripValue.action.Disable();
        }

        if (triggerValue.action != null)
        {
            triggerValue.action.Disable();
        }

        if (stickClickValue.action != null)
        {
            stickClickValue.action.Disable();
        }
    }

    void Update()
    {


        // -----------------------------------------------------------
        // Read the current value of the grip input (0.0 to 1.0)
        // -----------------------------------------------------------
        // - First, check if the grip action is assigned (not null)
        // - If yes, read its current value from the controller
        // - Clamp it between 0 and 1 to avoid any invalid values
        // - If the grip action is not assigned, default to 0
       // Debug.Log(gripValue.action != null ? gripValue.action.ReadValue<float>() : 0f);
        float g = gripValue.action != null ? Mathf.Clamp01(gripValue.action.ReadValue<float>()) : 0f;

        // -----------------------------------------------------------
        // Apply the rotation to the middle finger joints
        // -----------------------------------------------------------
        // - Pass the array of middle finger joints (middleJoints)
        // - Pass the cached "original" rotations (mid0)
        // - Multiply the maximum curl angle (middleMax) by the grip
        //   value (g) to get a dynamic rotation based on how much
        //   the button is pressed
        Apply(middleJoints, mid0, middleMax * g);
        Apply(ringJoints, ring0, ringMax * g);
        Apply(littleJoints, lit0, littleMax * g);


        // -----------------------------------------------------------
        // Index finger: curl dynamically based on trigger pressure
        // -----------------------------------------------------------
        float trigger = triggerValue.action != null
            ? Mathf.Clamp01(triggerValue.action.ReadValue<float>())
            : 0f;

        //debug the trigger
        Debug.Log("Trigger Value: " + trigger);

        Apply(indexJoints, index0, indexMax * trigger);


        // -----------------------------------------------------------
        // Thumb: snaps when stick is pressed (no smoothing)
        // -----------------------------------------------------------
        bool stickPressed = stickClickValue.action != null
            && stickClickValue.action.IsPressed();

        //debug stickpressed
        //Debug.Log("Stick Pressed: " + stickPressed);

        float thumbAngle = stickPressed ? thumbMax : 0f;

        Apply(thumbJoints, thumb0, thumbAngle);

    }


    // -----------------------------------------------------------
    // Cache
    // -----------------------------------------------------------
    // This method stores the "initial" local rotation of each joint
    // in an array. We use this later so that our rotations are
    // applied relative to the original pose of the hand.
    //
    // Parameters:
    //    t - the array of joints (Transform[]) for a finger
    //
    // Returns:
    //    An array of Quaternion values, one for each joint's
    //    starting local rotation.
    //
    Quaternion[] Cache(Transform[] t)
    {
        if (t == null) return null;
        // Create a new Quaternion array with the same size as the joints array
        var q = new Quaternion[t.Length];

        // Loop through each joint
        for (int i = 0; i < t.Length; i++)
        {
            // If the joint exists (is not null)
            if (t[i])
            {
                // Store that joint's current local rotation
                q[i] = t[i].localRotation;
            }
        }

        // Return the array of initial rotations
        return q;
    }


    // -----------------------------------------------------------
    // Apply
    // -----------------------------------------------------------
    // This method applies a new rotation to each joint of a finger
    // based on the input angle. The rotation is calculated by
    // combining the original rotation (cached) with an additional
    // rotation around the X axis.
    //
    // Parameters:
    //    t        - the array of joints (Transform[]) for a finger
    //    baseRot  - the cached "initial" rotations for those joints
    //    angleX   - how much to rotate (degrees) around the local X axis
    void Apply(Transform[] t, Quaternion[] baseRot, float angleX)
    {
        if (t == null || baseRot == null) return;
        // Loop through each joint of the finger
        for (int i = 0; i < t.Length; i++)
        {
            // If the joint exists
            if (t[i])
            {
                // Apply the rotation:
                // 1. Start with the original local rotation (baseRot[i])
                // 2. Multiply by a new rotation on the X axis
                t[i].localRotation = baseRot[i] * Quaternion.Euler(angleX, 0f, 0f);
            }
        }
    }
}
