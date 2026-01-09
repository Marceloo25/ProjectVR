using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// GenericCollisionHandler
/// - Detects collisions (trigger & physics) and notifies listeners with a compact HitInfo.
/// - Filters by LayerMask and optionally debounces repeated hits on the same target.
/// - Intended as a minimal, generic starting point for grabbables, weapons, tools and thrown items.
///
/// Usage:
/// - Attach to the root of a grabbable/weapon prefab that has a Collider and (optionally) a Rigidbody.
/// - Set `detectLayers` to restrict which layers will produce events.
/// - Subscribe to the `OnDetected` C# event to receive full `HitInfo` or use the Inspector `onDetected` UnityEvent.
/// </summary>
public class GenericCollisionHandler : MonoBehaviour
{
    [Header("Debug")]
    public bool debugLogs = false;

    [Header("Filter")]
    [Tooltip("Layers which will be considered for collision detection.")]
    public LayerMask detectLayers = ~0;

    [Tooltip("Minimum seconds between registering hits on the same target (by InstanceID). Helps debounce multiple callbacks).")]
    public float hitCooldown = 0.5f;

    // C# event with full HitInfo for runtime subscribers (code-only preferred)
    public event Action<HitInfo> OnDetected;

    // Internal map to debounce per target instance
    float lastHitTime = 0f;

    public struct HitInfo
    {
        public GameObject hitter;        // the object that holds this handler
        public Collider localCollider;   // the local collider on the hitter that caused the event (may be null)
        public GameObject other;         // the collided object
        public Collider otherCollider;   // the collider on the target that was hit (may be null for some collisions)
        public Vector3 point;            // approximate contact point
        public Vector3 normal;           // approximate contact normal
        public HitQuality quality;       // quality of the hit (to be filled by IHitSource/IHitReceiver)
        public float damage;             // damage amount of the hit (to be filled by IHitSource/IHitReceiver)
    }

    void Awake()
    {   
        if (debugLogs)
        {
            // Report on child colliders and forwarders at startup to help debugging.
            var allColliders = GetComponentsInChildren<Collider>(true);
            var forwarders = GetComponentsInChildren<LocalColliderForwarder>(true);
            Debug.Log($"GenericCollisionHandler.Awake('{name}'): found {allColliders.Length} child colliders and {forwarders.Length} LocalColliderForwarder(s).");
        }
    }

    // NOTE: This handler expects child colliders to forward events explicitly using
    // `ForwardTriggerEnter` / `ForwardCollisionEnter` so `localCollider` is always defined.
    // Root-level OnTrigger/OnCollision handlers are intentionally omitted to avoid
    // accidental double-processing or handling non-damaging colliders.

    /// <summary>
    /// Public forwarder used by LocalColliderForwarder attached to child colliders.
    /// Call this from a collider-specific forwarder to indicate which local collider caused the hit.
    /// </summary>
    public void ForwardTriggerEnter(Collider localCollider, Collider other)
    {
        if (localCollider == null)
        {
            if (debugLogs) Debug.LogError($"GenericCollisionHandler.ForwardTriggerEnter called with null localCollider on '{gameObject.name}'");
            return;
        }
        if (other == null) return;
        if (((1 << other.gameObject.layer) & detectLayers) == 0) return;
        if (debugLogs) Debug.Log($"GenericCollisionHandler.ForwardTriggerEnter: handler='{name}' local='{localCollider.name}' other='{other.gameObject.name}'");
        HandleDetected(localCollider, other.gameObject, other, null);
    }

    public void ForwardCollisionEnter(Collider localCollider, Collision collision)
    {
        if (localCollider == null)
        {
            if (debugLogs) Debug.LogError($"GenericCollisionHandler.ForwardCollisionEnter called with null localCollider on '{gameObject.name}'");
            return;
        }
        if (collision == null || collision.collider == null) return;
        var other = collision.collider.gameObject;
        if (((1 << other.layer) & detectLayers) == 0) return;
        if (debugLogs) Debug.Log($"GenericCollisionHandler.ForwardCollisionEnter: handler='{name}' local='{localCollider.name}' other='{other.name}' contacts={collision.contactCount}");
        HandleDetected(localCollider, other, collision.collider, collision);
    }

    void HandleDetected(Collider localCollider, GameObject other, Collider otherCollider, Collision collision)
    {
        if (other == null) return;

        float now = Time.time;

        if (now - lastHitTime < hitCooldown)
        {
            // debounced
            if (debugLogs) Debug.Log($"GenericCollisionHandler: Ignored hit on {other.name}");
            return;
        }

        lastHitTime = now;
        // Compute an approximate contact point and normal
        Vector3 point = transform.position;
        Vector3 normal = (other.transform.position - transform.position).normalized;

        if (collision != null && collision.contactCount > 0)
        {
            point = collision.GetContact(0).point;
            normal = collision.GetContact(0).normal;
        }
        else if (otherCollider != null)
        {
            // Use ClosestPoint which works for most collider types
            point = otherCollider.ClosestPoint(transform.position);
            normal = (point - transform.position).normalized;
        }

        var info = new HitInfo()
        {
            hitter = this.gameObject,
            localCollider = localCollider,
            other = other,
            otherCollider = otherCollider,
            point = point,
            normal = normal,
        };

        if (debugLogs)
        {
            var localName = localCollider != null ? localCollider.name : "<root>";
            Debug.Log($"GenericCollisionHandler: Detected collision between '{gameObject.name}'(local='{localName}') and '{other.name}' at {point}");
        }

        // Dispatch to IHitSource (tool) and IHitReceiver (target) if present. This keeps game logic modular.
        // Find a source (search from the local collider up to parents first so child-mounted scripts are found).
        IHitSource source = null;
        if (localCollider != null) source = localCollider.GetComponentInParent<IHitSource>();
        if (source == null) source = GetComponentInParent<IHitSource>();

        // Gameplay computations (damage, quality, feedback) are the responsibility
        // of the IHitSource / IHitReceiver implementations. The handler only
        // provides a consistent HitInfo snapshot and the source reference.

        // Find a receiver on the other object
        IHitReceiver receiver = null;
        if (other != null) receiver = other.GetComponentInParent<IHitReceiver>();

        // Always call the receiver locally so the hit is applied immediately on the local client/host.
        if (receiver != null)
        {
            try
            {
                bool accepted = receiver.ReceiveHit(info, source);
                if (debugLogs && !accepted) Debug.Log($"GenericCollisionHandler: receiver rejected local hit on {other.name}.");
            }
            catch (System.Exception ex)
            {
                if (debugLogs) Debug.LogError($"IHitReceiver threw exception (local): {ex}");
            }
        }
        else
        {
            if (debugLogs) Debug.LogWarning($"GenericCollisionHandler: target '{other.name}' has no IHitReceiver.");
        }

        // Finally invoke the C# event (code-first approach). Create HitInfo as a struct to avoid allocations.
        OnDetected?.Invoke(info);
    }
}
