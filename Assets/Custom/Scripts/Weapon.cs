using UnityEngine;

/// <summary>
/// VR Weapon prototype.
/// - Detects hits using its collider (trigger or non-trigger) and applies damage to objects on `hitLayerMask`.
/// - Intentionally minimal and unconstrained for VR: no "must be held" gating — collisions are damage sources.
/// - Designed to be similar to the pickaxe tip approach: attach this script to the weapon GameObject, give it
///   an appropriately sized collider (trigger or not) and a Rigidbody (kinematic is fine).
///
/// TODO: add per-swing debouncing, velocity-based hit quality, haptics, and network authority.
/// </summary>
public class Weapon : MonoBehaviour, IHitSource
{
    [Header("Weapon")]
    public float damage = 10f;
    [Tooltip("Tool type identifier used for broadcasts (optional)")]
    public int toolId = 1;
    public float cooldown = 0.5f;
    [Tooltip("Layer mask for objects that should receive damage from this weapon.")]
    public LayerMask hitLayerMask = 0;
    public bool debugLogs = true;

    private float m_LastAttackTime = -999f;
    public bool CanAttack => Time.time >= m_LastAttackTime + cooldown;

    void Reset()
    {
        cooldown = 0.1f;
        damage = 10f;
        hitLayerMask = ~0; // default to all layers
    }
    
    // IHitSource implementation: simple weapon that always returns fixed damage when available.
    public HitQuality ClassifyHit(in GenericCollisionHandler.HitInfo hit)
    {
        // This weapon doesn't classify based on velocity; always a standard hit quality.
        return HitQuality.Good;
    }

    public float ComputeDamage(in GenericCollisionHandler.HitInfo hit)
    {
        // Enforce local cooldown so weapons don't spam hits too fast (optional redundancy with handler cooldown)
        if (!CanAttack) return 0f;
        m_LastAttackTime = Time.time;

        if (debugLogs)
        {
            string target = hit.other != null ? hit.other.name : "<unknown>";
            Debug.Log($"Weapon: Hit {target} for {damage} damage");
        }

        return damage;
    }

    // IHitSource ToolId
    public int ToolId => toolId;
}

