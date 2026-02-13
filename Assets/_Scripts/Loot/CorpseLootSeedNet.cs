using Unity.Netcode;
using UnityEngine;
using Unity.Collections;

/// <summary>
/// CorpseLootSeedNet
/// -----------------
/// Minimal network bridge for passing a deterministic loot seed from death resolution
/// (CombatResolver) into the spawned corpse loot container.
///
/// Why this exists:
/// - CorpseLootInteractable previously derived loot seed from *corpse* NetworkObjectId.
/// - Corpse spawn order can vary, so that made loot depend on spawn timing.
/// - We now seed from the death event and hand it to the corpse before Spawn().
///
/// Server-authoritative contract:
/// - Server writes the seed exactly once via SetSeed before the corpse is spawned.
/// - Clients can read the NetworkVariable value, but never write.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkObject))]
public sealed class CorpseLootSeedNet : NetworkBehaviour
{
    // Static guard so seed handoff logging can be enabled quickly without noisy always-on logs.
    private const bool DebugSeedLogs = false;

    /// <summary>
    /// Death-derived loot seed replicated to everyone (read-only on clients).
    ///
    /// We keep this as a NetworkVariable so the value is also visible/inspectable
    /// after spawn and for late-joining clients if needed.
    /// </summary>
    public NetworkVariable<uint> LootSeed { get; } = new NetworkVariable<uint>(
        value: 0u,
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Server);

    /// <summary>
    /// Optional death-context loot table identifier supplied by the victim/monster.
    ///
    /// Backward compatibility rule:
    /// - Empty string means "no monster-driven override", so corpse-side logic should
    ///   continue to use its serialized dropTable or legacy pool path.
    /// </summary>
    public NetworkVariable<FixedString64Bytes> LootTableId { get; } = new NetworkVariable<FixedString64Bytes>(
        value: default,
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Server);

    // Server-only marker indicating that CombatResolver explicitly provided a seed.
    // This lets CorpseLootInteractable distinguish "real seed value 0" from
    // "no seed was ever provided" during server-side fallback handling.
    private bool _hasServerAssignedSeed;
    private bool _hasServerAssignedLootTableId;

    /// <summary>
    /// True only when the server explicitly assigned a seed through SetSeed.
    /// Consumers should use this when deciding whether to use fallback behavior.
    /// </summary>
    public bool HasServerAssignedSeed => _hasServerAssignedSeed;
    public bool HasServerAssignedLootTableId => _hasServerAssignedLootTableId;

    /// <summary>
    /// Server-only assignment used during corpse instantiation, before Spawn().
    /// </summary>
    public void SetSeed(uint seed)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("[CorpseLootSeedNet] SetSeed called on non-server. Ignored.");
            return;
        }

        LootSeed.Value = seed;
        _hasServerAssignedSeed = true;

        if (DebugSeedLogs)
            Debug.Log($"[CorpseLootSeedNet][SeedTrace] corpseSeedAssigned seed={seed} netObjectId={NetworkObjectId}");
    }

    /// <summary>
    /// Server-only assignment for the optional monster-driven loot table id.
    /// Must be called before corpse Spawn() so OnNetworkSpawn server logic can use it.
    /// </summary>
    public void SetLootTableId(string id)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("[CorpseLootSeedNet] SetLootTableId called on non-server. Ignored.");
            return;
        }

        string normalizedId = string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
        LootTableId.Value = normalizedId;
        _hasServerAssignedLootTableId = !string.IsNullOrEmpty(normalizedId);

        if (DebugSeedLogs)
            Debug.Log($"[CorpseLootSeedNet][SeedTrace] lootTableAssigned id={normalizedId} netObjectId={NetworkObjectId}");
    }
}
