using UnityEngine;
using XRMultiplayer;

/// <summary>
/// Auto-wires a NetworkBaseInteractable activation event to call a parent DeckScript.Draw().
/// Attach this to the deck root or the draw-handle. It will search for a DeckScript in parents
/// and for a NetworkBaseInteractable on the same GameObject or in children.
/// </summary>
[DisallowMultipleComponent]
public class DeckActivationAutoWiring : MonoBehaviour
{
    [Tooltip("Optional: explicitly assign the GameObject that has the DeckScript. If null, searches parents.")]
    public GameObject deckRoot;

    // Resolved at Awake via reflection to avoid compile-time assembly dependency on DeckScript.
    System.Reflection.MethodInfo m_deckDrawMethod;
    Component m_deckInstance;

    [Tooltip("Optional: explicitly assign the NetworkBaseInteractable to listen to. If null, searches this GameObject and children.")]
    public NetworkBaseInteractable interactable;

    void Awake()
    {
        if (deckRoot != null)
        {
            // Try to find a component on the assigned GameObject that has a Draw() method
            foreach (var comp in deckRoot.GetComponents<Component>())
            {
                if (comp == null) continue;
                var mi = comp.GetType().GetMethod("Draw", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic, null, System.Type.EmptyTypes, null);
                if (mi != null)
                {
                    m_deckDrawMethod = mi;
                    m_deckInstance = comp;
                    break;
                }
            }
        }

        if (m_deckDrawMethod == null)
        {
            // Search parents for any component that exposes a parameterless Draw() method
            Transform t = transform.parent;
            while (t != null)
            {
                foreach (var comp in t.GetComponents<Component>())
                {
                    if (comp == null) continue;
                    var mi = comp.GetType().GetMethod("Draw", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic, null, System.Type.EmptyTypes, null);
                    if (mi != null)
                    {
                        m_deckDrawMethod = mi;
                        m_deckInstance = comp;
                        break;
                    }
                }

                if (m_deckDrawMethod != null)
                    break;

                t = t.parent;
            }
        }

        if (interactable == null)
        {
            interactable = GetComponent<NetworkBaseInteractable>();
            if (interactable == null)
                interactable = GetComponentInChildren<NetworkBaseInteractable>(true);
        }

        if (m_deckDrawMethod == null)
        {
            Debug.LogWarning($"[{nameof(DeckActivationAutoWiring)}] No component with Draw() found in parents of '{gameObject.name}'. Auto-wiring skipped.");
            return;
        }

        if (interactable == null)
        {
            Debug.LogWarning($"[{nameof(DeckActivationAutoWiring)}] No NetworkBaseInteractable found on '{gameObject.name}' or children. Auto-wiring skipped.");
            return;
        }

        // Add listener to the networked activation event
        interactable.ActivateNetworkedEventAll.AddListener(OnActivateNetworkedAll);
    }

    void OnDestroy()
    {
        if (interactable != null)
            interactable.ActivateNetworkedEventAll.RemoveListener(OnActivateNetworkedAll);
    }

    void OnActivateNetworkedAll(bool activate)
    {
        if (!activate)
            return;

        if (m_deckDrawMethod != null && m_deckInstance != null)
            m_deckDrawMethod.Invoke(m_deckInstance, null);
    }
}
