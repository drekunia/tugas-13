using UnityEngine;

// Third-person orbit camera that follows a target (the sphere) and can be
// controlled with the mouse. Attach to a Camera object.
public class CameraMovement : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Target to follow. If not set, will try to find an object with SphereMovement in the scene on Awake.")]
    public Transform target;

    [Tooltip("Offset from target's position to look at (e.g., raise to look at head).")]
    public Vector3 focusOffset = new Vector3(0f, 0.5f, 0f);

    [Header("Orbit")]
    [Tooltip("Starting distance from the target.")]
    public float distance = 6f;

    [Tooltip("Minimum and maximum zoom distances.")]
    public float minDistance = 2f;
    public float maxDistance = 12f;

    [Tooltip("Horizontal (yaw) mouse sensitivity.")]
    public float yawSensitivity = 200f;

    [Tooltip("Vertical (pitch) mouse sensitivity.")]
    public float pitchSensitivity = 120f;

    [Tooltip("Invert vertical mouse movement.")]
    public bool invertY = false;

    [Tooltip("Clamp for the vertical angle (in degrees).")]
    public float minPitch = -20f;
    public float maxPitch = 80f;

    [Header("Input")]
    [Tooltip("If true, rotation occurs only while the specified mouse button is held (0=LMB,1=RMB,2=MMB).")]
    public bool requireMouseButtonForRotate = false;
    public int rotateMouseButton = 1; // Right mouse by default

    [Tooltip("Zoom speed when using mouse scroll wheel.")]
    public float zoomSensitivity = 4f;

    [Header("Smoothing")]
    [Tooltip("Smoothing factor for following target (0 = snap).")]
    [Range(0f, 1f)] public float followSmoothing = 0f;

    float _yaw;
    float _pitch;
    Vector3 _currentFollowPoint;

    void Awake()
    {
        if (target == null)
        {
            // Try to find the sphere automatically
            var sphere = FindObjectOfType<SphereMovement>();
            if (sphere != null) target = sphere.transform;
        }

        // Initialize angular values from current transform, if possible
        if (target != null)
        {
            Vector3 toCam = (transform.position - (target.position + focusOffset)).normalized;
            if (toCam.sqrMagnitude > 0.0001f)
            {
                // Convert toCam to yaw/pitch
                _pitch = Mathf.Asin(toCam.y) * Mathf.Rad2Deg; // approximate
                float yOnPlane = Mathf.Sqrt(Mathf.Max(0f, 1f - toCam.y * toCam.y));
                float x = toCam.x / Mathf.Max(yOnPlane, 1e-5f);
                float z = toCam.z / Mathf.Max(yOnPlane, 1e-5f);
                _yaw = Mathf.Atan2(x, z) * Mathf.Rad2Deg; // note: forward is +z
            }
            else
            {
                _yaw = transform.eulerAngles.y;
                _pitch = transform.eulerAngles.x;
            }
            _pitch = Mathf.Clamp(NormalizeAngle(_pitch), minPitch, maxPitch);
            _yaw = NormalizeAngle(_yaw);

            _currentFollowPoint = target.position + focusOffset;
        }
    }

    void OnValidate()
    {
        minDistance = Mathf.Max(0.01f, minDistance);
        maxDistance = Mathf.Max(minDistance, maxDistance);
        distance = Mathf.Clamp(distance, minDistance, maxDistance);
        maxPitch = Mathf.Max(minPitch + 0.01f, maxPitch);
        rotateMouseButton = Mathf.Clamp(rotateMouseButton, 0, 2);
        followSmoothing = Mathf.Clamp01(followSmoothing);
    }

    void Update()
    {
        if (target == null) return;

        // Mouse rotation
        bool canRotate = !requireMouseButtonForRotate || Input.GetMouseButton(rotateMouseButton);
        if (canRotate)
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            _yaw += mouseX * yawSensitivity * Time.unscaledDeltaTime;
            float ySign = invertY ? 1f : -1f;
            _pitch += mouseY * pitchSensitivity * ySign * Time.unscaledDeltaTime;
            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
            _yaw = NormalizeAngle(_yaw);
        }

        // Mouse wheel zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            float targetDist = distance - scroll * zoomSensitivity;
            distance = Mathf.Clamp(targetDist, minDistance, maxDistance);
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Follow point (optionally smoothed)
        Vector3 desiredFollowPoint = target.position + focusOffset;
        if (followSmoothing > 0f)
        {
            _currentFollowPoint = Vector3.Lerp(_currentFollowPoint, desiredFollowPoint, 1f - Mathf.Pow(1f - followSmoothing, Time.deltaTime * 60f));
        }
        else
        {
            _currentFollowPoint = desiredFollowPoint;
        }

        Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);
        Vector3 camOffset = rot * new Vector3(0f, 0f, -distance);
        transform.position = _currentFollowPoint + camOffset;
        transform.rotation = rot;

        // Keep looking at the follow point
        transform.LookAt(_currentFollowPoint);
    }

    static float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }
}
