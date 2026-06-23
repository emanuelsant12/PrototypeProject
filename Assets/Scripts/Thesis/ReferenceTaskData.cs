using UnityEngine;

public enum TaskType
{
    Shading,
    Perspective
}

[CreateAssetMenu(fileName = "ReferenceTaskData", menuName = "VR Painting/Reference Task Data")]
public class ReferenceTaskData : ScriptableObject
{
    [Header("Task Info")]
    public string taskName = "Shading Sphere";
    public TaskType taskType = TaskType.Shading;

    [TextArea(3, 8)]
    public string instructions;

    [Header("Reference Images")]
    public Texture2D referenceTexture;

    [Tooltip("For perspective, use a black-line-on-white-background mask. Black/dark pixels count as correct guide areas.")]
    public Texture2D perspectiveMask;

    [Header("Scoring")]
    [Range(0.01f, 1f)]
    public float shadingExcellentTolerance = 0.10f;

    [Range(0.01f, 1f)]
    public float shadingGoodTolerance = 0.20f;

    [Range(0.01f, 1f)]
    public float shadingOkayTolerance = 0.35f;

    [Range(0f, 1f)]
    public float perspectiveLineThreshold = 0.35f;
}