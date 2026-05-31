using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Rigidbody))]
public class PCHammerController : MonoBehaviour
{
 
    [Header("Move")]
    public float moveSpeed = 3f;
    public float sprintMultiplier = 2f; // hold Left Shift to sprint

    Rigidbody rb;
  

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

    }

    void OnEnable()
    {
        // mimic XR grab
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.angularDamping = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    void Update()
    {
        var k = Keyboard.current;
        if (k == null) return;

        float speed = moveSpeed * (k.leftShiftKey.isPressed ? sprintMultiplier : 1f);

        Vector3 wish = Vector3.zero;
        if (k.sKey.isPressed) wish += Vector3.forward; 
        if (k.wKey.isPressed) wish += Vector3.back;    
        if (k.dKey.isPressed) wish += Vector3.left;    
        if (k.aKey.isPressed) wish += Vector3.right;   
        if (k.eKey.isPressed) wish += Vector3.down;    
        if (k.qKey.isPressed) wish += Vector3.up;

        // If the combined input direction is longer than 1 (e.g., moving diagonally with W+A),
        // normalize it so diagonal movement isn't faster than straight movement.
        // This keeps movement speed consistent in all directions.
        if (wish.sqrMagnitude > 1f) wish.Normalize();

        rb.MovePosition(rb.position + wish * speed * Time.deltaTime);
    }

 
}
