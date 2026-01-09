using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// GameplayManager: server-authoritative entrypoint for submitted hits from clients.
/// - Clients call SubmitHitServerRpc to report compact hit claims.
/// - Server validates (basic) and invokes IHitReceiver on the target server-side.
/// This is a conservative skeleton — expand validation rules as needed.
/// </summary>
public class GameplayManager : NetworkBehaviour
{
    public static GameplayManager Instance { get; private set; }

    // Simple server-side tool damage table (toolId -> baseDamage). Extend as needed.
    Dictionary<int, float> m_ToolBaseDamage = new Dictionary<int, float>
    {
        { 0, 10f }, // default pickaxe/tool
        { 1, 10f }, // weapon default
    };

    void Awake()
    {
        UnityEngine.Debug.Log($"GameplayManager Awake on {gameObject.name}, IsServer={IsServer}");
        if (Instance != null && Instance != this) Destroy(this);
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        UnityEngine.Debug.Log($"GameplayManager OnNetworkSpawn on {gameObject.name}, IsServer={IsServer}, IsClient={IsClient}, IsSpawned={NetworkObject != null && NetworkObject.IsSpawned}");
    }

    public override void OnDestroy()
    {
        if (Instance == this) Instance = null;
        base.OnDestroy();
    }

    /// <summary>
    /// Clients call this to submit a compact hit claim. Runs on server only.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void SubmitHitServerRpc(ulong targetNetworkObjectId, Vector3 hitPoint, int toolId, float damage, HitQuality quality, ServerRpcParams rpcParams = default)
    {
        UnityEngine.Debug.Log($"Hit detected by {gameObject.name}, IsServer={IsServer}");

        // This method is executed on the server only.
        ulong attackerClientId = rpcParams.Receive.SenderClientId;

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkObjectId, out NetworkObject netObj))
        {
            UnityEngine.Debug.LogWarning($"GameplayManager: SubmitHitServerRpc - target {targetNetworkObjectId} not found.");
            return;
        }

        var receiver = netObj.GetComponentInParent<IHitReceiver>();
        if (receiver == null)
        {
            UnityEngine.Debug.LogWarning($"GameplayManager: SubmitHitServerRpc - target {netObj.name} has no IHitReceiver.");
            return;
        }

        // Basic server-side validation (expand as needed): ensure attacker is connected
        if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(attackerClientId))
        {
            UnityEngine.Debug.LogWarning($"GameplayManager: SubmitHitServerRpc - unknown attacker {attackerClientId}.");
            return;
        }

        // First, apply the hit on the server instance so HP and destruction
        // are driven from the server-side ResourceNode / IHitReceiver.
        var serverHit = new GenericCollisionHandler.HitInfo
        {
            hitter = null,
            localCollider = null,
            other = netObj.gameObject,
            otherCollider = null,
            point = hitPoint,
            normal = Vector3.zero,
        };

        IHitSource serverSource = new ReportedSource(toolId, damage, quality);
        try
        {
            bool accepted = receiver.ReceiveHit(serverHit, serverSource);
            if (!accepted)
            {
                UnityEngine.Debug.Log($"GameplayManager: Server-side hit not accepted by receiver '{netObj.name}'.");
            }
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError($"GameplayManager: exception while applying server-side hit on '{netObj.name}': {ex}");
        }

        // Minimal relay behavior: server broadcasts the hit claim to other clients so
        // they can play local feedback. Security/validation is intentionally omitted.
        if (GameplayManager.Instance != null)
        {
            UnityEngine.Debug.Log($"GameplayManager: Relaying hit on '{netObj.name}' from client {attackerClientId} (toolId={toolId}, dmg={damage:F1}, quality={quality}) to other clients.");
            // Build target client list excluding the original sender so the reporting client
            // doesn't re-apply the hit when it receives the broadcast (host too).
            var allIds = new System.Collections.Generic.List<ulong>(NetworkManager.Singleton.ConnectedClientsIds);
            allIds.Remove(attackerClientId);
            var clientRpcParams = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = allIds.ToArray() } };
            BroadcastHitClientRpc(netObj.NetworkObjectId, hitPoint, toolId, damage, quality, attackerClientId, clientRpcParams);
        }
    }

    /// <summary>
    /// Broadcast a compact hit claim to all clients. Clients will attempt to resolve
    /// the target and attacker-local source and call the local IHitReceiver implementation.
    /// Runs on clients.
    /// </summary>
    [ClientRpc]
    void BroadcastHitClientRpc(ulong targetNetworkObjectId, Vector3 hitPoint, int toolId, float damage, HitQuality quality, ulong attackerClientId, ClientRpcParams clientRpcParams = default)
    {
        if (NetworkManager.Singleton == null)
        {
            UnityEngine.Debug.LogWarning("GameplayManager: BroadcastHitClientRpc invoked but NetworkManager is null on client.");
            return;
        }

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkObjectId, out NetworkObject netObj))
        {
            UnityEngine.Debug.LogWarning($"GameplayManager: BroadcastHitClientRpc - target {targetNetworkObjectId} not found on client.");
            return;
        }

        var receiver = netObj.GetComponentInParent<IHitReceiver>();
        if (receiver == null)
        {
            UnityEngine.Debug.LogWarning($"GameplayManager: BroadcastHitClientRpc - target {netObj.name} has no IHitReceiver on client.");
            return;
        }

        // Reconstruct a minimal HitInfo for client-side processing
        var hit = new GenericCollisionHandler.HitInfo()
        {
            hitter = null,
            localCollider = null,
            other = netObj.gameObject,
            otherCollider = null,
            point = hitPoint,
            normal = Vector3.zero,
        };

        // Create a small local IHitSource that represents the reported values so
        // receivers can use the same ReceiveHit path without recomputing damage.
        IHitSource reportedSource = new ReportedSource(toolId, damage, quality);

        try
        {
            bool accepted = receiver.ReceiveHit(hit, reportedSource);
            if (!accepted)
                UnityEngine.Debug.Log($"GameplayManager: Broadcasted hit not accepted by local receiver '{netObj.name}' on client.");
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError($"GameplayManager: exception while applying broadcasted hit on client: {ex}");
        }
    }

    // Minimal server-side IHitSource for damage computation.
    class ServerSideSource : IHitSource
    {
        int m_ToolId;
        float m_BaseDamage;

        public ServerSideSource(int toolId, float baseDamage)
        {
            m_ToolId = toolId;
            m_BaseDamage = baseDamage;
        }

        public HitQuality ClassifyHit(in GenericCollisionHandler.HitInfo hit)
        {
            // Conservative: always return Good for server-side reported hits. Replace with real logic if sending more data.
            return HitQuality.Good;
        }

        public float ComputeDamage(in GenericCollisionHandler.HitInfo hit)
        {
            return m_BaseDamage;
        }
        public int ToolId => m_ToolId;
    }

    // A lightweight IHitSource implementation that carries concrete reported values
    // so clients can apply hits without re-running source logic.
    class ReportedSource : IHitSource
    {
        int m_ToolId;
        float m_Damage;
        HitQuality m_Quality;

        public ReportedSource(int toolId, float damage, HitQuality quality)
        {
            m_ToolId = toolId;
            m_Damage = damage;
            m_Quality = quality;
        }

        public int ToolId => m_ToolId;

        public HitQuality ClassifyHit(in GenericCollisionHandler.HitInfo hit)
        {
            return m_Quality;
        }

        public float ComputeDamage(in GenericCollisionHandler.HitInfo hit)
        {
            return m_Damage;
        }
    }
}
