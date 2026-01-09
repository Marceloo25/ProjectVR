using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Prototype ResourceNode: holds HP and basic TakeHit handling.
/// This is intentionally simple — advanced features (XP, networking, pooling,
/// respawn) are left as commented TODOs for later iterations.
/// </summary>
public class ResourceNode : NetworkBehaviour, IHitReceiver
{
    [Header("Debug")]
    public bool debugLogs = false;

    [Header("Resource")]
    public ResourceType resourceType;

    [Tooltip("Loot prefab to spawn on destroy. If empty, resourceType.lootPrefab will be used.")]
    public NetworkObject loot;

    [Tooltip("Spawn upwards impulse magnitude applied to spawned loot (prototype).")]
    public float lootSpawnForce = 2.5f;

    [Tooltip("If set, this overrides the ResourceType.defaultMaxHP for this instance.")]
    public float maxHP = 20f;

    private float m_CurrentHP = -1f;

    [Header("Hit Quality Settings (speed thresholds)")]
    public float speedBad = 3.0f;
    public float speedGood = 6.0f;
    public float speedPerfect = 10.0f;

    // These were NetworkVariables earlier but are not needed yet.
    // We can promote them back to replicated state later if required.
    //private HitQuality hitQuality = HitQuality.Invalid;
    //private float hitDamage = 0f;

   public override void OnNetworkSpawn()
    {
        // Initialize HP on the server only (clients rely on server-driven despawn)
        if (IsServer)
        {
            if (resourceType != null && maxHP <= 0f)
                maxHP = resourceType.defaultMaxHP;

            if (m_CurrentHP < 0f)
                m_CurrentHP = maxHP;
        }

        if (debugLogs)
        {
            Debug.Log($"[ResourceNode.OnNetworkSpawn] '{name}' NetId={NetworkObjectId} HP={m_CurrentHP}/{maxHP} IsServer={IsServer} IsClient={IsClient} Owner={OwnerClientId} IsSpawned={IsSpawned}");
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (debugLogs)
        {
            Debug.Log($"[ResourceNode.OnNetworkDespawn] '{name}' NetId={NetworkObjectId} IsServer={IsServer} IsClient={IsClient} Owner={OwnerClientId}");
        }

        // As a safety net, explicitly disable the GameObject locally when
        // it despawns. Netcode should already handle visibility, but if
        // there are any pooling or template quirks, this guarantees the
        // renderer disappears on both host and clients.
        gameObject.SetActive(false);
    }

    /// <summary>
    /// New API: receive a HitInfo created by the GenericCollisionHandler and a reference
    /// to the IHitSource that produced the hit. Returns true if the hit was accepted.
    /// </summary>
    public bool ReceiveHit(in GenericCollisionHandler.HitInfo info, IHitSource source)
    {
        if (info.other == null) return false;

        //Client vs Server handling
        var quality = ClassifyHit(info);
        var damage = ComputeDamage(info);
        if (debugLogs)
        {
            Debug.Log($"[ResourceNode.ReceiveHit] '{name}' NetId={NetworkObjectId} from={info.hitter.name} quality={quality} dmg={damage:F2} IsServer={IsServer} IsOwner={IsOwner}");
        }

        // Always route damage application through the ServerRpc so the
        // server is the single entry point for HP/destruction logic.
        HitServerRpc(info.point, damage, quality);

        return true;
    }

    public HitQuality ClassifyHit(in GenericCollisionHandler.HitInfo hit)
    {
        /*
        // Determine a hit reference point (contact point if available, else tip transform or head)
        Vector3 hitPoint = Vector3.zero;
        Vector3 motionDir = Vector3.zero;
        if (motionDir == Vector3.zero)
        {
            // fallback to tip->hit direction
            motionDir = (hitPoint - (hit.localCollider != null ? hit.localCollider.transform.position : transform.position)).normalized;
        }

        // Project sampled velocity onto the motion direction to avoid counting lateral/separating motion
        float speedAlongDir = 0f;
        float speed = Mathf.Max(0f, speedAlongDir);


        if (speed >= speedPerfect) return HitQuality.Perfect;
        if (speed >= speedGood) return HitQuality.Good;
        if (speed >= speedBad) return HitQuality.Bad;*/
        return HitQuality.Good;
    }

    public float ComputeDamage(in GenericCollisionHandler.HitInfo hit)
    {
        /*
        float multiplier = 0f;
        var quality = hitQuality.Value;
        switch (quality)
        {
            case HitQuality.Invalid: multiplier = 0f; break;
            case HitQuality.Bad: multiplier = 0.5f; break;
            case HitQuality.Good: multiplier = 1.0f; break;
            case HitQuality.Perfect: multiplier = 2.5f; break;
        }
        float baseDamage = 0f; //need to get from tool
        float dmg = baseDamage * multiplier;*/
        return 10f;
    }

    [ServerRpc(RequireOwnership = false)]
    void HitServerRpc(Vector3 hitpoint, float damage, HitQuality quality)
    {
        if (debugLogs)
        {
            Debug.Log($"[ResourceNode.HitServerRpc] '{name}' NetId={NetworkObjectId} hitpoint={hitpoint} dmg={damage:F2} quality={quality} IsServer={IsServer} Owner={OwnerClientId}");
        }
        ApplyHitServer(hitpoint, damage, quality);
    }

    void ApplyHitServer(Vector3 hitpoint, float damage, HitQuality quality)
    {
        if (!IsServer)
            return;

        float oldHP = m_CurrentHP;
        m_CurrentHP -= damage;

        if (debugLogs)
        {
            Debug.Log($"[ResourceNode.ApplyHitServer] '{name}' NetId={NetworkObjectId} HP {oldHP}->{m_CurrentHP} dmg={damage:F2} quality={quality} Owner={OwnerClientId}");
        }

        // Broadcast hit feedback to all clients
        HitClientRpc(hitpoint, quality);

        if (m_CurrentHP <= 0f)
        {
            if (debugLogs)
            {
                Debug.Log($"[ResourceNode.ApplyHitServer] '{name}' NetId={NetworkObjectId} destroyed on server. Despawning and spawning loot.");
            }
            NetworkObject.Despawn();
            SpawnLoot(hitpoint);
        }
    }

    [ClientRpc]
    void HitClientRpc(Vector3 hitPoint, HitQuality quality)
    {
        if (debugLogs)
        {
            Debug.Log($"[ResourceNode.HitClientRpc] '{name}' NetId={NetworkObjectId} received hit at {hitPoint} quality={quality} IsServer={IsServer} IsClient={IsClient} Owner={OwnerClientId}");
        }

        // Play hit feedback
        AudioClip clip = null;
        GameObject vfx = null;
        switch (quality)
        {
            case HitQuality.Bad:
                clip = resourceType.hitSfxBad;
                vfx = resourceType.hitVfxBad;
                break;
            case HitQuality.Good:
                clip = resourceType.hitSfxGood;
                vfx = resourceType.hitVfxGood;
                break;
            case HitQuality.Perfect:
                clip = resourceType.hitSfxPerfect;
                vfx = resourceType.hitVfxPerfect;
                break;
        }

        if (clip != null)
            AudioSource.PlayClipAtPoint(clip, hitPoint);
        if (vfx != null)
            Instantiate(vfx, hitPoint, Quaternion.identity);
        if (resourceType != null)
        {
            if (resourceType.breakSfx != null)
                AudioSource.PlayClipAtPoint(resourceType.breakSfx, hitPoint);
            if (resourceType.breakVfx != null)
                Instantiate(resourceType.breakVfx, hitPoint, Quaternion.identity);
        }
    }

    void SpawnLoot(Vector3 hitPoint)
    {
        var spawnPos = hitPoint + Vector3.up;

        var go = Instantiate(loot, spawnPos, Quaternion.identity);
        var no = go.GetComponent<NetworkObject>();
        no.Spawn();

        var rb = go.GetComponent<Rigidbody>();
        Vector3 initialVel = new(
            UnityEngine.Random.Range(-0.5f, 0.5f),
            UnityEngine.Random.Range(1.5f, 2.5f),
            UnityEngine.Random.Range(-0.5f, 0.5f)
        );
        rb.linearVelocity = initialVel * lootSpawnForce;
    }
}