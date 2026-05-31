using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit;


//you have to press the Trigger and Primary button at the same time to move the object

[RequireComponent(typeof(XRSimpleInteractable))]
public class MoveOnActivate : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("How high the object moves when activated.")]
    public float moveUpDistance = 3.5f;

    [Tooltip("Speed of the lerp movement.")]
    public float lerpSpeed = 5f;

    private Vector3 startPosition;
    private Vector3 targetPosition;

    private XRSimpleInteractable interactable;

    private void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
    }

    private void Start()
    {
        // Store the starting position
        startPosition = transform.localPosition;
        targetPosition = startPosition;

        // Register to Activate and Deactivate events
        interactable.activated.AddListener(OnActivated);
        interactable.deactivated.AddListener(OnDeactivated);
        interactable.selectExited.AddListener(OnSelectExit); // Also handle select exit to reset position

    }

    private void OnDestroy()
    {
        // Unregister to prevent memory leaks
        interactable.activated.RemoveListener(OnActivated);
        interactable.deactivated.RemoveListener(OnDeactivated);
        interactable.selectExited.RemoveListener(OnSelectExit);
    }

    private void Update()
    {
        // Smoothly interpolate position towards the target
        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            targetPosition,
            Time.deltaTime * lerpSpeed
        );
    }



    private void OnActivated(ActivateEventArgs args)
    {
        Debug.Log("Activated: Moving object up");
        targetPosition = startPosition + Vector3.up * moveUpDistance;
    }

    private void OnDeactivated(DeactivateEventArgs args)
    {
        Debug.Log("OnDeactivated: Moving object down");
        targetPosition = startPosition;
    }

    private void OnSelectExit(SelectExitEventArgs args)
    {
        Debug.Log("OnSelectExit: Resetting position");
        targetPosition = startPosition; // Reset to start position when selection exits
    }
}
