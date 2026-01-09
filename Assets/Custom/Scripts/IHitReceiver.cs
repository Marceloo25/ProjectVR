using UnityEngine;

/// <summary>
/// Implement on objects that can be hit (enemies, resource nodes, destructible props).
/// ReceiveHit should apply damage/state changes and return true if the hit was accepted.
/// </summary>
public interface IHitReceiver
{
    // Apply a hit coming from a tool/source. Return true if processed.
    // The handler will pass the source so the receiver can compute feedback dependent
    // on both the source and the receiver (e.g., different VFX/sound/haptics per weapon-target pair).
    bool ReceiveHit(in GenericCollisionHandler.HitInfo hit, IHitSource source);

    // Compute the damage amount for this hit. Use HitInfo data (speed, collider, etc).
    float ComputeDamage(in GenericCollisionHandler.HitInfo hit);

    // Classify the hit quality (Bad/Good/Perfect). Optional: return HitQuality.Good by default.
    HitQuality ClassifyHit(in GenericCollisionHandler.HitInfo hit);
}
