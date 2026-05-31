using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class HammerWhackDetector : MonoBehaviour
{

    [Tooltip("Minimum downward speed to count as a whack.")]
    public float minDownSpeed = 2f;

    // Position from the previous Update frame (used to calculate frame-to-frame velocity)
    Vector3 prevPosUpdate;
    // Instantaneous velocity calculated every Update frame (m/s)
    Vector3 velUpdate;
    // Snapshot of velocity to use during physics step for collision checks
    Vector3 preImpactVel;

    void OnEnable()
    {
        // Reset tracking values when the component is enabled
        prevPosUpdate = transform.position;
        velUpdate = preImpactVel = Vector3.zero;
    }

    void Update()
    {
        // Calculate the hammer's velocity based on position change per frame
        float dt = Time.deltaTime;
        if (dt > 0f)
        {
            Vector3 p = transform.position;
            velUpdate = (p - prevPosUpdate) / dt; // velocity = delta position / delta time
            prevPosUpdate = p;  // store current position for next frame
        }
    }

    void FixedUpdate()
    {
        // Store the latest velocity to be used during the physics step.
        // This ensures OnTriggerEnter reads a stable value consistent with physics timing.
        preImpactVel = velUpdate;
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Collision detected: " + other.name);
        // Check if the object we collided with has a BottleWhack component
        var bottle = other.GetComponent<BottleWhack>();
        if (!bottle) return;

        // Define "down" direction 
        Vector3 downDir = Vector3.down;

        // Project the hammer's pre-impact velocity onto the "down" direction
        // Positive value = moving downward, Negative value = moving upward
        float down = Vector3.Dot(preImpactVel, downDir);

        Debug.Log($"Whack: v={preImpactVel} down={down:F2}");

        // Only trigger the bottle if downward speed is at least the threshold
        if (down >= minDownSpeed)
            bottle.ForceDown();
    }

}
