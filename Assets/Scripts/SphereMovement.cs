using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SphereMovement : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Max ground speed in units/second.")]
    public float moveSpeed = 6f;

    [Tooltip("How strong the jump is (impulse).")]
    public float jumpForce = 5f;

    [Header("Ground Check")]
    [Tooltip("Extra distance below the sphere bottom to consider as grounded.")]
    public float groundCheckDistance = 0.05f;

    [Tooltip("Layers considered as ground.")]
    public LayerMask groundLayers = ~0; // Everything by default

    [Header("Collision")]
    [Tooltip("Extra distance beyond the sphere radius to check for walls in front.")]
    public float wallCheckDistance = 0.05f;

    [Header("Camera")]
    [Tooltip("Optional: reference to the camera used for movement direction. If null, Camera.main is used.")]
    public Transform cameraTransform;

    Rigidbody _rb;
    float _h, _v, _jumpAxis;
    bool _isGrounded;
    float _sphereRadius = 0.5f;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.constraints = RigidbodyConstraints.FreezeRotation; // Prevent unwanted tipping

        // Try to get an accurate radius from a SphereCollider if present
        var sphere = GetComponent<SphereCollider>();
        if (sphere != null)
        {
            var maxScale = Mathf.Max(transform.localScale.x, Mathf.Max(transform.localScale.y, transform.localScale.z));
            _sphereRadius = sphere.radius * maxScale;
        }

        // Fallback to main camera if not assigned in Inspector
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        // Read inputs every frame for responsiveness
        _h = Input.GetAxis("Horizontal");
        _v = Input.GetAxis("Vertical");
        _jumpAxis = Input.GetAxis("Jump");
    }

    void FixedUpdate()
    {
        UpdateGrounded();
        HandleMove();
        HandleJump();
    }

    void UpdateGrounded()
    {
        // Cast from the center downward to slightly below the bottom of the sphere
        float checkDistance = _sphereRadius + groundCheckDistance;
        _isGrounded = Physics.Raycast(transform.position, Vector3.down, checkDistance, groundLayers, QueryTriggerInteraction.Ignore);
    }

    void HandleMove()
    {
        // Compute movement relative to camera on the XZ plane
        Vector3 forward = Vector3.forward;
        Vector3 right = Vector3.right;
        if (cameraTransform != null)
        {
            forward = cameraTransform.forward; forward.y = 0f; forward = forward.sqrMagnitude > 0f ? forward.normalized : Vector3.forward;
            right = cameraTransform.right; right.y = 0f; right = right.sqrMagnitude > 0f ? right.normalized : Vector3.right;
        }

        Vector3 desiredDir = right * _h + forward * _v;
        if (desiredDir.sqrMagnitude > 1f) desiredDir.Normalize();

        // Prevent "latching" to walls while airborne by removing into-wall component of input
        if (!_isGrounded && desiredDir.sqrMagnitude > 0.0001f)
        {
            RaycastHit hit;
            Vector3 castDir = desiredDir.normalized;
            float castRadius = _sphereRadius * 0.98f; // small tolerance
            float castDist = Mathf.Max(0.01f, wallCheckDistance);
            if (Physics.SphereCast(transform.position, castRadius, castDir, out hit, castDist, groundLayers, QueryTriggerInteraction.Ignore))
            {
                // If pushing into the surface, slide along it instead of into it
                if (Vector3.Dot(desiredDir, hit.normal) < 0f)
                {
                    desiredDir = Vector3.ProjectOnPlane(desiredDir, hit.normal);
                }
            }
        }

        // Desired horizontal velocity
        Vector3 desiredVel = desiredDir * moveSpeed;
        Vector3 currentVel = _rb.velocity;
        Vector3 deltaVel = new Vector3(desiredVel.x - currentVel.x, 0f, desiredVel.z - currentVel.z);

        // Apply instantaneous horizontal change (keeps gravity unaffected)
        _rb.AddForce(deltaVel, ForceMode.VelocityChange);
    }

    void HandleJump()
    {
        // Jump only when grounded and button is pressed (GetAxis > 0)
        if (_isGrounded && _jumpAxis > 0f)
        {
            // Reset any downward velocity to make jump consistent
            Vector3 vel = _rb.velocity;
            if (vel.y < 0f) vel.y = 0f;
            _rb.velocity = vel;

            _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    void OnDrawGizmosSelected()
    {
        // Visualize ground check in the editor
        Gizmos.color = Color.yellow;
        float radius = _sphereRadius;
        var sphere = GetComponent<SphereCollider>();
        if (sphere != null)
        {
            var maxScale = Mathf.Max(transform.localScale.x, Mathf.Max(transform.localScale.y, transform.localScale.z));
            radius = sphere.radius * maxScale;
        }
        Vector3 start = transform.position;
        Vector3 end = start + Vector3.down * (radius + groundCheckDistance);
        Gizmos.DrawLine(start, end);
        Gizmos.DrawWireSphere(start + Vector3.down * (radius + groundCheckDistance), 0.02f);
    }
}
