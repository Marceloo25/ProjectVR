using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Forwards trigger/collision events from a specific child collider to the
/// nearest parent GenericCollisionHandler, supplying the exact local Collider.
/// Attach to any damage collider (tips, blades, heads) so the handler receives
/// which sub-collider caused the contact.
/// </summary>
[RequireComponent(typeof(Collider))]
public class LocalColliderForwarder : MonoBehaviour
{
    Collider m_Local;
    GenericCollisionHandler m_Handler;

    void Awake()
    {
        m_Local = GetComponent<Collider>();
        m_Handler = GetComponentInParent<GenericCollisionHandler>();
        if (m_Handler == null)
            Debug.LogWarning($"LocalColliderForwarder: no GenericCollisionHandler found in parents of '{gameObject.name}'");
    }

    void OnTriggerEnter(Collider other)
    {
        if (m_Handler == null) m_Handler = GetComponentInParent<GenericCollisionHandler>();
        if (m_Handler == null)
        {
            Debug.LogWarning($"LocalColliderForwarder '{gameObject.name}': OnTriggerEnter with '{other.name}' but no GenericCollisionHandler found in parents.");
            return;
        }

        if (!ShouldForward())
        {
            if (m_Handler.debugLogs) Debug.Log($"LocalColliderForwarder '{gameObject.name}': Skipping forward (not owner) for '{other.gameObject.name}'");
            return;
        }

        if (m_Handler.debugLogs) Debug.Log($"LocalColliderForwarder '{gameObject.name}': Forwarding Trigger -> handler='{m_Handler.gameObject.name}' local='{m_Local.name}' other='{other.gameObject.name}'");
        m_Handler.ForwardTriggerEnter(m_Local, other);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (m_Handler == null) m_Handler = GetComponentInParent<GenericCollisionHandler>();
        if (m_Handler == null)
        {
            Debug.LogWarning($"LocalColliderForwarder '{gameObject.name}': OnCollisionEnter with '{collision.collider.name}' but no GenericCollisionHandler found in parents.");
            return;
        }

        if (!ShouldForward())
        {
            if (m_Handler.debugLogs) Debug.Log($"LocalColliderForwarder '{gameObject.name}': Skipping forward (not owner) for collision with '{collision.collider.gameObject.name}'");
            return;
        }

        if (m_Handler.debugLogs) Debug.Log($"LocalColliderForwarder '{gameObject.name}': Forwarding Collision -> handler='{m_Handler.gameObject.name}' local='{m_Local.name}' other='{collision.collider.gameObject.name}' contacts={collision.contactCount}");
        m_Handler.ForwardCollisionEnter(m_Local, collision);
    }

    bool ShouldForward()
    {
        // If there's no NetworkManager or this object isn't networked, forward normally.
        if (NetworkManager.Singleton == null) return true;

        var netObj = GetComponentInParent<NetworkObject>();
        if (netObj == null) return true; // not a networked object

        // If the object isn't spawned yet, allow forwarding (fallback).
        if (!netObj.IsSpawned) return true;

        // Owner should forward. If ownerless and we're the server, allow.
        if (netObj.OwnerClientId == NetworkManager.Singleton.LocalClientId) return true;
        if (netObj.OwnerClientId == 0 && NetworkManager.Singleton.IsServer) return true;

        return false;
    }
}
