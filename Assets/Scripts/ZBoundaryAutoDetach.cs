using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UIElements;

public class ZBoundaryAutoDetach : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("How far (in world Z) the object is allowed to move from its start Z before we detach.")]
    public float zThreshold = 1.5f;

    [Tooltip("When detaching, snap back to StartZ +/- this boundary (world space).")]
    public float zBoundary = 1.4f;

    private XRGrabInteractable grab;     // Reference to the XR Grab Interactable on this object
    private XRInteractionManager manager; // Reference to the interaction manager (handles attach/detach events)

    // World-space start Z captured at play start
    private float startWorldZ;

    // Track whether we're currently selected (grabbed)
    private bool isSelected = false;

    private void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        manager = grab.interactionManager; // set by XRI
    }

    private void Start()
    {
        // Capture initial world Z at the start of the game
        startWorldZ = transform.position.z;
    }

    private void OnEnable()
    {
        // Subscribe to grab and release events
        grab.selectEntered.AddListener(OnSelectEntered);
        grab.selectExited.AddListener(OnSelectExited);
    }

    private void OnDisable()
    {
        // Unsubscribe when the object is disabled or destroyed
        grab.selectEntered.RemoveListener(OnSelectEntered);
        grab.selectExited.RemoveListener(OnSelectExited);
    }
    // Called when the object is grabbed
    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        isSelected = true;
    }

    // Called when the object is released
    private void OnSelectExited(SelectExitEventArgs args)
    {
        isSelected = false;
    }

    private void Update()
    {
        // Only check while the object is being held
        if (!isSelected) return;

        float currentZ = transform.position.z;
        float deltaZ = currentZ - startWorldZ;

        if (Mathf.Abs(deltaZ) >= zThreshold)   // If the object has moved too far in the Z direction; Mathf.Abs() returns the absolute value of a number (ignores the sign).
        {
            // 1) Force a detach from the current interactor 
            if (grab.isSelected && manager != null)
            {
                var interactor = grab.interactorsSelecting.Count > 0 ? grab.interactorsSelecting[0] : null; //Checks if there’s at least one interactor (hand/controller) grabbing the object.
                if (interactor != null)
                {
                    manager.SelectExit(interactor, grab);
                }
            }

            // 2) Snap back to boundary (keep X/Y, clamp Z to start +/- zBoundary)
            var p = transform.position;
            float clampedZ = startWorldZ + Mathf.Sign(deltaZ) * zBoundary;// Mathf.Sign() returns + 1 if the number is positive and - 1 if the number is negative. This keeps track of which side the object crossed the threshold on.
            transform.position = new Vector3(p.x, p.y, clampedZ);

            // 3) Clear flag so we don't re-run this 
            isSelected = false;
        }
    }
}
