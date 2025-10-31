using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FinishObject : MonoBehaviour
{
    [Header("Detection")]
    [Tooltip("Tag used by the player GameObject")] public string playerTag = "Player";
    [Tooltip("How close a contact normal must be to Vector3.up to count as 'top'.")]
    [Range(0f, 1f)] public float topNormalThreshold = 0.8f;

    [Header("UI")] public FinishUIFeedback uiFeedback;
    [Tooltip("Optional sound to play when touched")] public AudioClip feedbackSound;
    [Tooltip("Optional particle system to play when touched")] public ParticleSystem feedbackEffect;

    private AudioSource _audio;

    private void Awake()
    {
        _audio = GetComponent<AudioSource>();
        if (_audio == null && feedbackSound != null)
            _audio = gameObject.AddComponent<AudioSource>();
        // Supports both collision and trigger setups.
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.collider.CompareTag(playerTag)) return;

        if (IsTopContact(collision))
        {
            TriggerFeedback();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        TriggerFeedback();
    }

    private bool IsTopContact(Collision collision)
    {
        // For this collider, the top face normal points up; if any contact normal is close to up, it's a top touch
        for (int i = 0; i < collision.contactCount; i++)
        {
            var n = collision.GetContact(i).normal;
            if (Vector3.Dot(n, Vector3.up) >= topNormalThreshold)
                return true;
        }
        return false;
    }

    private void TriggerFeedback()
    {
        // Sound
        if (_audio != null && feedbackSound != null)
        {
            _audio.PlayOneShot(feedbackSound);
        }

        // Particle
        if (feedbackEffect != null)
        {
            feedbackEffect.Play(true);
        }

        uiFeedback?.Flash();

        Debug.Log("[FinishObject] Touched by player — feedback triggered.", this);
    }
}
