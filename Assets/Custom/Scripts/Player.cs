using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Simple Player combat behaviour for prototype.
/// - Now network-aware: implements IHitReceiver so players can be damaged by tools/weapons.
/// - Applies damage locally and reports concrete damage to the server for relay to other clients.
/// TODO: hook into XR input, animations, XP/leveling, and server-authoritative HP if desired.
/// </summary>
public class Player : NetworkBehaviour, IHitReceiver
{
    [Header("Health")]
    public float maxHealth = 100f;
    private float m_CurrentHealth;

    [Header("Behavior")]
    public bool debugLogs = true;

    [Header("Feedback")]
    public AudioClip hitSfx;
    public GameObject hitVfx;
    [Tooltip("Alternate sound clip to use; Player will alternate between the two clips each hit if assigned.")]
    public AudioClip hitSfxAlt;
    [Tooltip("Sound to play when this hit kills the player. The normal hit sounds are skipped on the killing blow.")]
    public AudioClip deathSfx;

    // internal toggle to alternate between primary and alternate clip
    bool m_PlayAltNext = false;

    void Awake()
    {
        m_CurrentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f) return;

        m_CurrentHealth -= amount;
        if (debugLogs) Debug.Log($"Player {name}: Took {amount} damage. HP={m_CurrentHealth}/{maxHealth}");

        if (m_CurrentHealth <= 0f)
        {
            m_CurrentHealth = 0f;
            Die();
        }
    }

    // IHitReceiver implementation
    public bool ReceiveHit(in GenericCollisionHandler.HitInfo hit, IHitSource source)
    {
        if (source == null) return false;

        float dmg = ComputeDamage(hit);
        if (dmg <= 0f) return false;

        // Apply locally for immediate feedback (allow self-hit for testing)
        TakeDamage(dmg);

        // Play hit feedback locally
        Vector3 feedbackPos = hit.point != Vector3.zero ? hit.point : transform.position;

        // Alternate between primary and alternate clip when both are present.
            // Determine whether this hit will kill the player (so we can play deathSfx instead)
            bool willDie = (m_CurrentHealth - dmg) <= 0f;

            if (willDie)
            {
                if (deathSfx != null)
                    AudioSource.PlayClipAtPoint(deathSfx, feedbackPos);
            }
            else
            {
                AudioClip clipToPlay = null;
                if (hitSfx != null && hitSfxAlt != null)
                {
                    clipToPlay = m_PlayAltNext ? hitSfxAlt : hitSfx;
                    m_PlayAltNext = !m_PlayAltNext;
                }
                else if (hitSfx != null)
                {
                    clipToPlay = hitSfx;
                }

                if (clipToPlay != null)
                {
                    AudioSource.PlayClipAtPoint(clipToPlay, feedbackPos);
                }
            }

        if (hitVfx != null) Instantiate(hitVfx, feedbackPos, Quaternion.identity);

        // If the hit came from a local Component IHitSource, report concrete numbers to server
        if (source is IHitSource && source is UnityEngine.Component)
        {
            if (NetworkManager.Singleton != null && GameplayManager.Instance != null)
            {
                int toolId = source.ToolId;
                ulong targetId = 0ul;
                if (this is NetworkBehaviour nb && nb.NetworkObject != null)
                    targetId = nb.NetworkObject.NetworkObjectId;

                HitQuality quality = HitQuality.Invalid;
                try { quality = ClassifyHit(hit); } catch { quality = HitQuality.Invalid; }

                GameplayManager.Instance.SubmitHitServerRpc(targetId, hit.point, toolId, dmg, quality);
            }
        }

        return true;
    }

    void Die()
    {
        if (debugLogs) Debug.Log($"Player {name} died.");
        // TODO: handle player death/respawn properly. For now just disable object.
        gameObject.SetActive(false);
    }

    // Optional helper to reset health
    public void ResetHealth()
    {
        m_CurrentHealth = maxHealth;
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
