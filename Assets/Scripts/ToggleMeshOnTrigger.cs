using UnityEngine;

public class ToggleMeshOnTrigger : MonoBehaviour
{
    [Tooltip("The object whose MeshRenderer will be toggled. Leave empty to use this object.")]
    public GameObject targetObject;

    private MeshRenderer meshRenderer;

    private void Start()
    {
        // Use the assigned object or fallback to this object
        if (targetObject == null)
            targetObject = gameObject;

        // Get the MeshRenderer component
        meshRenderer = targetObject.GetComponent<MeshRenderer>();
        meshRenderer.enabled = false; // Start with the mesh turned OFF
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Trigger entered by: {other.gameObject.name}");
        if (meshRenderer != null)
        {
            meshRenderer.enabled = true; // Turn ON
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (meshRenderer != null)
        {
            meshRenderer.enabled = false; // Turn OFF
        }
    }
}
