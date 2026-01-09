using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Simple Enemy prototype for local combat testing.
/// - Now network-aware: inherits from NetworkBehaviour so hits can be relayed to server like ResourceNode.
/// - Supports taking damage and dying.
/// - Calls Die() when HP reaches zero.
/// TODO: add animations, hit effects, XP, and server-side authority/validation.
/// </summary>
public class Enemy : NetworkBehaviour, IHitReceiver
{
    [Header("Stats")]
    public float maxHealth = 50f;
    private float m_CurrentHealth;

    [Header("Behavior")]
    public bool debugLogs = true;

    [Header("Feedback")]
    [Tooltip("Sound to play when this enemy is hit (same for all qualities).")]
    public AudioClip hitSfx;
    [Tooltip("VFX prefab to instantiate at hit point when this enemy is hit (same for all qualities).")]
    public GameObject hitVfx;

    void Awake()
    {
        m_CurrentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f) return;

        m_CurrentHealth -= amount;
        if (debugLogs) Debug.Log($"Enemy {name}: Took {amount} damage. HP={m_CurrentHealth}/{maxHealth}");

        if (m_CurrentHealth <= 0f)
        {
            m_CurrentHealth = 0f;
            Die();
        }
    }

    // IHitReceiver implementation: called by GenericCollisionHandler when this object is hit.
    // Returns true if the hit was accepted and applied.
    public bool ReceiveHit(in GenericCollisionHandler.HitInfo hit, IHitSource source)
    {
        if (source == null) return false;

        // Determine damage (source is responsible for computing damage)
        float dmg = ComputeDamage(hit);
        if (dmg <= 0f) return false;

        // Apply locally
        TakeDamage(dmg);

        // Play hit feedback (same for all qualities) on whoever applies the hit locally.
        Vector3 feedbackPos = hit.point != Vector3.zero ? hit.point : transform.position;
        if (hitSfx != null)
        {
            AudioSource.PlayClipAtPoint(hitSfx, feedbackPos);
        }
        if (hitVfx != null)
        {
            Instantiate(hitVfx, feedbackPos, Quaternion.identity);
        }

        // If this hit was produced by a local IHitSource Component (owner/client-side calculation),
        // submit the concrete damage/quality to the GameplayManager so the server can broadcast it to others.
        if (source is IHitSource && source is UnityEngine.Component)
        {
            if (Unity.Netcode.NetworkManager.Singleton != null && GameplayManager.Instance != null)
            {
                int toolId = source.ToolId;
                ulong targetId = 0ul;
                if (this is NetworkBehaviour nb && nb.NetworkObject != null)
                    targetId = nb.NetworkObject.NetworkObjectId;

                // Classify hit quality if available (we allow sources to not care about quality)
                HitQuality quality = HitQuality.Invalid;
                try { quality = ClassifyHit(hit); } catch { quality = HitQuality.Invalid; }

                GameplayManager.Instance.SubmitHitServerRpc(targetId, hit.point, toolId, dmg, quality);
            }
        }

        return true;
    }

    void Die()
    {
        if (debugLogs) Debug.Log($"Enemy {name} died.");
        // TODO: play death VFX/SFX, drop loot, award XP, notify game manager, and support respawn.
        Destroy(gameObject);
    }

    public float ComputeDamage(in GenericCollisionHandler.HitInfo hit)
    {
        return 10f;
    }

    public HitQuality ClassifyHit(in GenericCollisionHandler.HitInfo hit)
    {
        return HitQuality.Good;
    }
}
