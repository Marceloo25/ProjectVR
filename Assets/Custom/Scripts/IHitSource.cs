using UnityEngine;

/// <summary>
/// Implement on items that produce hits (weapons, tools, pickaxes, thrown objects).
/// The handler will call into these methods to compute damage/classify hits and play local feedback.
/// </summary>
public interface IHitSource
{
    // Compute the damage amount for this hit. Use HitInfo data (speed, collider, etc).
    //float ComputeDamage(in GenericCollisionHandler.HitInfo hit);

    // Classify the hit quality (Bad/Good/Perfect). Optional: return HitQuality.Good by default.
    //HitQuality ClassifyHit(in GenericCollisionHandler.HitInfo hit);

    // Typed tool identifier. Prefer a type id (e.g., 0=bronze pickaxe, 1=steel pickaxe).
    // Implementations should return a stable identifier for the tool type.
    int ToolId { get; }

    // Note: feedback (VFX/haptics) is now handled by the receiver so it can
    // decide how to present feedback given both the source and the receiver.
}
