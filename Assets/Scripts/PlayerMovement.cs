using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Speeds")]
    [Tooltip("Walking speed in meters/second")] public float walkSpeed = 5f;
    [Tooltip("Running speed in meters/second (hold Run key)")] public float runSpeed = 10f;
    [Tooltip("Acceleration towards target speed")] public float acceleration = 20f;
    [Tooltip("Deceleration when no input")] public float deceleration = 30f;

    [Header("Jumping & Gravity")]
    [Tooltip("Desired jump height in meters")] public float jumpHeight = 1.5f;
    [Tooltip("Gravity magnitude (negative)")] public float gravity = -9.81f;
    [Range(0f, 89f), Tooltip("Maximum slope angle CharacterController can walk up")]
    public float maxSlopeAngle = 45f;

    [Header("Camera Alignment")]
    [Tooltip("If true, movement input is relative to the main camera's facing direction")]
    public bool cameraRelative = true;

    [Header("Run Key")]
    [Tooltip("Key to hold for running")] public KeyCode runKey = KeyCode.LeftShift;

    private CharacterController controller;
    private Vector3 velocity;           // vertical velocity stored in y; xz only from move
    private Vector3 planarVelocity;     // xz velocity we control

    Animator animator;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        controller.slopeLimit = maxSlopeAngle;
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector2 input = new Vector2(h, v);
        input = Vector2.ClampMagnitude(input, 1f);

        Vector3 moveDir = new Vector3(input.x, 0f, input.y);
        if (cameraRelative)
        {
            Transform cam = Camera.main ? Camera.main.transform : null;
            if (cam != null)
            {
                Vector3 camForward = cam.forward; camForward.y = 0f; camForward.Normalize();
                Vector3 camRight = cam.right; camRight.y = 0f; camRight.Normalize();
                moveDir = camForward * input.y + camRight * input.x;
            }
        }
        moveDir.Normalize();

        float targetSpeed = Input.GetKey(runKey) ? runSpeed : walkSpeed;
        Vector3 targetPlanar = moveDir * targetSpeed;

        float accel = (moveDir.sqrMagnitude > 0.001f) ? acceleration : deceleration;
        planarVelocity = Vector3.MoveTowards(planarVelocity, targetPlanar, accel * Time.deltaTime);

        velocity.y += gravity * Time.deltaTime;

        if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space))
        {
            velocity.y = Mathf.Sqrt(2f * jumpHeight * -gravity);
            animator.SetTrigger("jump");
        }

        Vector3 motion = new Vector3(planarVelocity.x, 0f, planarVelocity.z);
        motion += Vector3.up * velocity.y;

        controller.Move(motion * Time.deltaTime);

        if ((controller.collisionFlags & CollisionFlags.Above) != 0 && velocity.y > 0f)
        {
            velocity.y = 0f;
        }

        if (moveDir.sqrMagnitude > 0.0001f)
        {
            Vector3 lookDir = new Vector3(moveDir.x, 0f, moveDir.z);
            if (lookDir.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), 10f * Time.deltaTime);
            }
        }

        UpdateAnimation();
    }

    void UpdateAnimation()
    {
        animator.SetFloat("speed", planarVelocity.magnitude);
    }
}
