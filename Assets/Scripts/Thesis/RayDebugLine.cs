using UnityEngine;

public class RayDebugLine : MonoBehaviour
{
    public Transform origin;
    public float length = 2f;
    public LayerMask mask;
    public Color hitColor = Color.green;
    public Color missColor = Color.red;

    LineRenderer lr;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
    }

    void Update()
    {
        if (!origin) return;

        Vector3 a = origin.position;
        Vector3 b = origin.position + origin.forward * length;

        bool hit = Physics.Raycast(a, origin.forward, out RaycastHit rh, length, mask);
        if (hit) b = rh.point;

        lr.positionCount = 2;
        lr.SetPosition(0, a);
        lr.SetPosition(1, b);
        lr.startColor = lr.endColor = hit ? hitColor : missColor;
    }
}