using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [Header("Follow Target")]
    public Transform target;                 // Character to follow
    [Tooltip("Automatically find the player at startup if target is not set")]
    public bool autoAssignTarget = true;
    [Tooltip("Tag to search if Player component isn't found")]
    public string playerTag = "Player";
    [Tooltip("Vertical offset of the camera pivot relative to target position (e.g., character head height).")]
    public float targetHeight = 1.6f;

    [Header("Orbit Settings")]
    public float mouseSensitivityX = 180f;   // Degrees per second for horizontal rotation per mouse unit
    public float mouseSensitivityY = 120f;   // Degrees per second for vertical rotation per mouse unit
    [Tooltip("Minimum vertical angle (looking down is negative).")]
    public float minPitch = -35f;
    [Tooltip("Maximum vertical angle (looking up).")]
    public float maxPitch = 75f;

    [Header("Zoom")]
    public float distance = 4.5f;            // Current distance to target
    public float minDistance = 1.5f;
    public float maxDistance = 7.5f;
    public float zoomSpeed = 5f;             // How fast the zoom changes per scroll unit

    [Header("Smoothing")]
    public bool smooth = true;
    public float positionSmoothTime = 0.06f; // Lower = snappier, higher = smoother
    public float rotationSmoothTime = 0.04f;

    [Header("Collision")]
    public bool avoidClipping = true;
    public LayerMask collisionLayers = ~0;   // Collide with everything by default
    public float collisionRadius = 0.2f;     // Spherecast radius
    public float collisionBuffer = 0.1f;     // Keep some space from obstacles

    [Header("Cursor")]
    public bool lockAndHideCursor = true;    // Lock/hide cursor for mouse-look

    private float _yaw;   // Horizontal angle around Y axis
    private float _pitch; // Vertical angle around X axis

    private Vector3 _currentVelocity;        // For SmoothDamp position
    private Quaternion _currentRot;          // For rotation smoothing
    private float _rotVelocity;              // Not used by Quaternion.Slerp, kept for potential future use

    void Start()
    {
        if (target == null && autoAssignTarget)
        {
            var player = FindObjectOfType<PlayerMovement>();
            if (player != null)
            {
                target = player.transform;
            }
            else
            {
                GameObject go = null;
                try { go = GameObject.FindGameObjectWithTag(playerTag); } catch {} // ignore if tag doesn't exist
                if (go != null)
                    target = go.transform;
            }

            if (target == null)
            {
                Debug.LogWarning($"CameraMovement: No target assigned and auto-assign failed. Assign the Target in Inspector, add {nameof(PlayerMovement)} to your player, or tag the player as \"{playerTag}\".");
            }
        }
        else if (target == null)
        {
            Debug.LogWarning("CameraMovement: No target assigned. Please set the target Transform.");
        }

        Vector3 e = transform.eulerAngles;
        _yaw = e.y;
        float rawPitch = e.x;
        if (rawPitch > 180f) rawPitch -= 360f;
        _pitch = Mathf.Clamp(rawPitch, minPitch, maxPitch);

        _currentRot = Quaternion.Euler(_pitch, _yaw, 0f);

        if (lockAndHideCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        _yaw += mouseX * mouseSensitivityX * Time.deltaTime;
        _pitch -= mouseY * mouseSensitivityY * Time.deltaTime;
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            distance -= scroll * zoomSpeed; // positive scroll zooms in with default settings
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }

        Quaternion desiredRot = Quaternion.Euler(_pitch, _yaw, 0f);
        Vector3 pivot = target.position + Vector3.up * targetHeight;
        Vector3 desiredPos = pivot - (desiredRot * Vector3.forward) * distance;

        if (avoidClipping)
        {
            Vector3 dir = (desiredPos - pivot);
            float desiredDist = dir.magnitude;
            if (desiredDist > 0.0001f)
            {
                dir /= desiredDist; // normalize
                if (Physics.SphereCast(pivot, collisionRadius, dir, out RaycastHit hit, desiredDist, collisionLayers, QueryTriggerInteraction.Ignore))
                {
                    float safeDist = Mathf.Max(hit.distance - collisionBuffer, 0.0f);
                    desiredPos = pivot + dir * safeDist;
                }
            }
        }

        if (smooth)
        {
            transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref _currentVelocity, positionSmoothTime);
            _currentRot = Quaternion.Slerp(_currentRot, desiredRot, 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.0001f, rotationSmoothTime)));
            transform.rotation = _currentRot;
        }
        else
        {
            transform.position = desiredPos;
            transform.rotation = desiredRot;
        }
    }

    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
