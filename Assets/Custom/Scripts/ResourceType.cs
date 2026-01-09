using UnityEngine;

[CreateAssetMenu(menuName = "Mining/ResourceType", fileName = "ResourceType_")]
public class ResourceType : ScriptableObject
{
    public string resourceName = "NewResource";

    public enum Rarity { Common, Rare }
    public Rarity rarity = Rarity.Common;

    [Tooltip("Default max HP for this resource type. Individual nodes can override.")]
    public float defaultMaxHP = 20f;

    [Tooltip("Loot prefab spawned when destroyed (optional).")]
    public GameObject lootPrefab;

    [Header("Feedback (optional)")]
    public GameObject hitVfxBad;
    public GameObject hitVfxGood;
    public GameObject hitVfxPerfect;
    public GameObject breakVfx;

    public AudioClip hitSfxBad;
    public AudioClip hitSfxGood;
    public AudioClip hitSfxPerfect;
    public AudioClip breakSfx;

    [Header("Gameplay (prototyping)")]
    [Tooltip("XP reward on destroy (placeholder) -- XP system not implemented yet")]
    public int xpOnDestroy = 0; // TODO: XP system integration

    [Tooltip("Should this resource respawn after being harvested? (respawn logic not implemented in prototype)")]
    public bool respawn = false; // TODO: respawn logic

    public float respawnTime = 30f; // TODO: used by respawn logic when implemented
}
