using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class TVRotator : MonoBehaviour
{
    [Tooltip("Reference to the TV object to rotate.")]
    public Transform tvObject;

    [Tooltip("Degrees to rotate each interaction.")]
    public float rotationStep = 90f;

    [Tooltip("Material to show when the switch is not hovered.")]
    public Material defaultMaterial;

    [Tooltip("Material to show when the switch is hovered.")]
    public Material hoveredMaterial;

    [Header("Visual Feedback")]
    [Tooltip("Renderer of the switch object (the mesh to change materials on hover).")]
    public Renderer switchRenderer;

    public void RotateTV(SelectEnterEventArgs args)
    {
        if (tvObject != null)
        {
            tvObject.Rotate(0f, rotationStep, 0f, Space.Self);
        }
    }

    // Called when the interactor starts hovering over the switch
    public void OnHoverEntered(HoverEnterEventArgs args)
    {
        if (switchRenderer != null && hoveredMaterial != null)
        {
            switchRenderer.material = hoveredMaterial;
        }
    }

    // Called when the interactor stops hovering over the switch
    public void OnHoverExited(HoverExitEventArgs args)
    {
        if (switchRenderer != null && defaultMaterial != null)
        {
            switchRenderer.material = defaultMaterial;
        }
    }
}
