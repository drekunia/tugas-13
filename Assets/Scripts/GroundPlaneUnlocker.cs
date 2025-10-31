using UnityEngine;

/// <summary>
/// Attach this script to the ground plane (the lowest plane in the level).
/// When the player touches the ground plane (via collision or trigger),
/// it unlocks the ability for finish objects to flash again.
/// </summary>
[RequireComponent(typeof(Collider))]
public class GroundPlaneUnlocker : MonoBehaviour
{
    [Tooltip("Tag used by the player GameObject")] public string playerTag = "Player";

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.collider.CompareTag(playerTag)) return;
        FinishObject.UnlockFlash();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        FinishObject.UnlockFlash();
    }
}
