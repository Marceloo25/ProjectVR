using System;
using UnityEngine;

/// <summary>
/// Pickaxe now implements IHitSource and uses the GenericCollisionHandler +
/// LocalColliderForwarder on tip colliders. Hit classification and damage are
/// computed on-demand by the source (this) when a receiver asks.
///
/// The script uses the `GenericCollisionHandler` + `LocalColliderForwarder`
/// pipeline. Legacy fallbacks have been removed; ensure tip colliders have
/// `LocalColliderForwarder` and the owner has `GenericCollisionHandler`.
/// </summary>
public class Pickaxe : MonoBehaviour, IHitSource
{
    [Header("Debug")]
    public bool debugLogs = false;

    [Tooltip("Tool identifier used when relaying hits to other clients via server.")]
    public int ToolId => 0;

    //[Header("Damage")]
    //[Tooltip("Base damage applied by this tool (prototype)")]
    //public float baseDamage = 10f;

    // IHitSource typed ToolId implementation
    //public int ToolId => toolId;

    // Cached cube transform and velocity for evaluating hit angle and speed
    //public Transform headTransform;


    //System.Collections.Generic.Dictionary<int, Vector3> m_PrevTipPos = new System.Collections.Generic.Dictionary<int, Vector3>();
    //System.Collections.Generic.Dictionary<int, Vector3> m_TipVelocity = new System.Collections.Generic.Dictionary<int, Vector3>();

    /*
    void Start()
    {
        // Initialize previous position so first FixedUpdate doesnt create a huge spike
        if (headTransform != null)
        {
            headPrevPos = headTransform.position;
            headVelocity = Vector3.zero;
        }
    } 

   
    void FixedUpdate()
    {
        // Update cube velocity if we have a cube transform
        if (headTransform != null)
        {
            float dt = Mathf.Max(0.0001f, Time.fixedDeltaTime);
            Vector3 pos = headTransform.position;
            headVelocity = (pos - headPrevPos) / dt;
            headPrevPos = pos;
        }
    }
    // IHitSource implementation — called by receivers to classify hits and compute damage
    public HitQuality ClassifyHit(in GenericCollisionHandler.HitInfo hit)
    {

        // Determine a hit reference point (contact point if available, else tip transform or head)
        Vector3 hitPoint = hit.point != Vector3.zero ? hit.point : (hit.localCollider != null ? hit.localCollider.transform.position : (headTransform != null ? headTransform.position : transform.position));

        // Compute motion direction from the previous sampled head position toward the hit point.
        Vector3 motionDir = (hitPoint - headPrevPos).normalized;
        if (motionDir == Vector3.zero)
        {
            // fallback to tip->hit direction
            motionDir = (hitPoint - (hit.localCollider != null ? hit.localCollider.transform.position : transform.position)).normalized;
        }

        // Project sampled velocity onto the motion direction to avoid counting lateral/separating motion
        float speedAlongDir = Vector3.Dot(headVelocity, motionDir);
        float speed = Mathf.Max(0f, speedAlongDir);

        if (debugLogs)
        {
            string localName = hit.localCollider != null ? hit.localCollider.name : "<null>";
            Debug.Log($"Pickaxe.ClassifyHit: local='{localName}' rawVel={headVelocity:F3} projected={speed:F2} motionDir={motionDir}");
        }

        if (speed >= speedPerfect) return HitQuality.Perfect;
        if (speed >= speedGood) return HitQuality.Good;
        if (speed >= speedBad) return HitQuality.Bad;
        return HitQuality.Invalid;
    }

    public float ComputeDamage(in GenericCollisionHandler.HitInfo hit)
    {
        var quality = ClassifyHit(hit);
        float multiplier = 1f;
        switch (quality)
        {
            case HitQuality.Invalid: multiplier = 0f; break;
            case HitQuality.Bad: multiplier = 0.5f; break;
            case HitQuality.Good: multiplier = 1.0f; break;
            case HitQuality.Perfect: multiplier = 2.5f; break;
        }
        float dmg = baseDamage * multiplier;
        if (debugLogs) Debug.Log($"Pickaxe.ComputeDamage: quality={quality} dmg={dmg:F2}");
        return dmg;
    }*/
}
