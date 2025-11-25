using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Collider))]
public class CapsuleEvent : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Optional: Assign a UI Text to show the pickup message. If not set, the script will try to find one in the scene.")]
    public Text messageText;

    [Tooltip("Message to show when the capsule is picked up.")]
    public string pickupMessage = "Capsule taken!";

    [Tooltip("If true, the message will be cleared after a short delay.")]
    public bool autoClearMessage = true;

    [Tooltip("How long to wait before clearing the message (seconds).")]
    public float clearDelay = 2f;

    Collider _collider;
    bool _consumed;

    void Awake()
    {
        _collider = GetComponent<Collider>();
        // Ensure the capsule is a trigger so the sphere can pass through it
        _collider.isTrigger = true;
    }

    void Reset()
    {
        // Also enforce trigger when the component is added in the editor
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_consumed) return;

        // Check if the thing that touched us is the player sphere (has SphereMovement)
        if (other != null && other.GetComponent<SphereMovement>() != null)
        {
            _consumed = true;
            ShowMessage();
            ConsumeCapsule();
        }
    }

    void ShowMessage()
    {
        // Print the pickup message to the Unity Console instead of updating UI
        Debug.Log(pickupMessage);
    }

    void ClearMessage()
    {
        if (messageText != null)
        {
            messageText.text = string.Empty;
        }
    }

    void ConsumeCapsule()
    {
        // Hide visuals immediately
        var renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers) r.enabled = false;

        // Disable collider to avoid further triggers
        if (_collider != null) _collider.enabled = false;

        // Destroy the capsule GameObject
        Destroy(gameObject);
    }
}
