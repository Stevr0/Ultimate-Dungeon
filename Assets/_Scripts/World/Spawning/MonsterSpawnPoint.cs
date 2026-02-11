using UnityEngine;

/// <summary>
/// Marks a location that a <see cref="MonsterSpawner"/> can use when spawning networked monsters.
/// 
/// Why this is a separate component:
/// - Designers can place many points in a scene and move them visually.
/// - The spawner can auto-discover points without hard-coding transforms.
/// </summary>
[DisallowMultipleComponent]
public sealed class MonsterSpawnPoint : MonoBehaviour
{
    [Header("Optional Grouping")]
    [Tooltip("Optional logical group id for designer organization. Not currently used by runtime logic.")]
    [SerializeField] private string spawnGroupId = string.Empty;

    /// <summary>
    /// Optional grouping id used only for organization/debug right now.
    /// </summary>
    public string SpawnGroupId => spawnGroupId;

    /// <summary>
    /// Spawn position/orientation source.
    /// We intentionally use this component's transform so scene editing stays simple.
    /// </summary>
    public Transform SpawnTransform => transform;
}
