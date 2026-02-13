using System;
using System.Collections.Generic;
using UltimateDungeon.Items;
using UltimateDungeon.Loot;
using UltimateDungeon.Progression;
using UltimateDungeon.SceneRules;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkObject))]
public sealed class CorpseLootInteractable : NetworkBehaviour, IInteractable
{
    [Header("Identity")]
    [SerializeField] private string displayName = "Corpse";

    [Header("Interaction")]
    [SerializeField] private float interactRange = 2.0f;

    [Header("Required")]
    [SerializeField] private ItemDefCatalog itemDefCatalog;
    [SerializeField] private ItemInstanceFactory itemInstanceFactory;

    [Header("Loot")]
    [SerializeField] private int minDrops = 1;
    [SerializeField] private int maxDrops = 3;
    [SerializeField] private List<string> lootPoolItemDefIds = new();
    // Optional: when assigned, this corpse uses the DropTableResolver pipeline
    // instead of legacy lootPoolItemDefIds random-pick behavior.
    [SerializeField] private DropTableDef dropTable;
    [SerializeField] private string debugItemDefId = "mainhand_sword_shortsword";
    [SerializeField] private bool debugLogs;

    private readonly List<ItemInstance> _loot = new();

    // Server-side seed resolved from death context (via CorpseLootSeedNet).
    // We keep this cached so SeedLootServer can remain focused on loot generation
    // while using the externally provided deterministic context.
    private uint _serverLootSeed;
    private bool _hasServerLootSeed;

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public float InteractRange => Mathf.Max(0.1f, interactRange);
    public ulong NetworkObjectId => NetworkObject.NetworkObjectId;

    public static event Action<ulong, List<CorpseLootSnapshotEntry>> ClientLootSnapshotReceived;
    public static event Action<ulong, LootTakeResultCode> ClientTakeItemResultReceived;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsServer)
            return;

        ResolveSeedContextServer();
        SeedLootServer();
    }

    public void ServerInteract(NetworkBehaviour interactor)
    {
        if (!IsServer || interactor == null)
            return;

        if (!TryResolvePlayer(interactor, out var playerInventory, out var playerObject))
            return;

        if (!IsPlayerInRange(playerObject.transform.position))
            return;

        if (!IsLootAllowedInCurrentScene())
            return;

        SendSnapshotToClient(playerInventory.OwnerClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestLootSnapshotServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsServer)
            return;

        ulong senderClientId = rpcParams.Receive.SenderClientId;
        if (!TryGetPlayerInventory(senderClientId, out _))
            return;

        SendSnapshotToClient(senderClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestTakeItemServerRpc(FixedString64Bytes instanceId, ServerRpcParams rpcParams = default)
    {
        if (!IsServer)
            return;

        ulong senderClientId = rpcParams.Receive.SenderClientId;
        if (!TryGetPlayerInventory(senderClientId, out var playerInventory))
        {
            SendTakeItemResult(senderClientId, LootTakeResultCode.InvalidPlayer);
            return;
        }

        if (!IsPlayerInRange(playerInventory.transform.position))
        {
            SendTakeItemResult(senderClientId, LootTakeResultCode.OutOfRange);
            return;
        }

        if (!IsLootAllowedInCurrentScene())
        {
            SendTakeItemResult(senderClientId, LootTakeResultCode.SceneRulesBlocked);
            return;
        }

        int itemIndex = FindLootIndex(instanceId.ToString());
        if (itemIndex < 0)
        {
            SendTakeItemResult(senderClientId, LootTakeResultCode.ItemNotFound);
            return;
        }

        ItemInstance item = _loot[itemIndex];
        var addResult = playerInventory.ServerTryAdd(item, out _);
        if (addResult != InventoryOpResult.Success)
        {
            SendTakeItemResult(senderClientId, LootTakeResultCode.InventoryFull);
            return;
        }

        _loot.RemoveAt(itemIndex);
        playerInventory.ServerPersistNow();

        SendTakeItemResult(senderClientId, LootTakeResultCode.Success);
        SendSnapshotToClient(senderClientId);

        if (_loot.Count == 0 && NetworkObject != null && NetworkObject.IsSpawned)
            NetworkObject.Despawn(destroy: true);
    }

    /// <summary>
    /// Server-only seed resolution for corpse loot generation.
    ///
    /// Preferred path:
    /// - Read deterministic death-context seed handed off by CombatResolver via
    ///   CorpseLootSeedNet before corpse spawn.
    ///
    /// Fallback path:
    /// - If no handoff is present, keep old behavior (seed from corpse NetworkObjectId)
    ///   and warn so playtests continue instead of hard-failing.
    /// </summary>
    private void ResolveSeedContextServer()
    {
        _hasServerLootSeed = false;
        _serverLootSeed = 0u;

        if (TryGetComponent(out CorpseLootSeedNet corpseLootSeedNet) && corpseLootSeedNet.HasServerAssignedSeed)
        {
            _serverLootSeed = corpseLootSeedNet.LootSeed.Value;
            _hasServerLootSeed = true;
            return;
        }

        // Acceptance fallback: preserve previous seed behavior so existing playtests
        // remain functional even when the corpse seed bridge is not wired yet.
        _serverLootSeed = (uint)(NetworkObject.NetworkObjectId * 2654435761u) ^ 0xC0FFEEu;
        Debug.LogWarning($"[CorpseLootInteractable] No death-context loot seed provided for corpse {NetworkObject.NetworkObjectId}. Falling back to corpse NetworkObjectId seeding.");
    }

    /// <summary>
    /// Server-only drop table resolution with explicit backward compatibility rules.
    ///
    /// Resolution order:
    /// 1) CorpseLootSeedNet.LootTableId when present and non-empty (monster-driven).
    ///    - We resolve via Resources/DropTables/{id} for minimal integration cost.
    ///    - Missing asset logs warning, then falls back.
    /// 2) Serialized dropTable on this corpse (existing Step 2 behavior).
    /// 3) Null => caller will use legacy lootPoolItemDefIds behavior.
    /// </summary>
    private DropTableDef ResolveDropTableServer()
    {
        if (TryGetComponent(out CorpseLootSeedNet corpseLootSeedNet) && corpseLootSeedNet.HasServerAssignedLootTableId)
        {
            string lootTableId = corpseLootSeedNet.LootTableId.Value.ToString();
            if (!string.IsNullOrWhiteSpace(lootTableId))
            {
                string normalizedId = lootTableId.Trim();
                DropTableDef tableFromId = Resources.Load<DropTableDef>($"DropTables/{normalizedId}");
                if (tableFromId != null)
                    return tableFromId;

                Debug.LogWarning($"[CorpseLootInteractable] LootTableId '{normalizedId}' was provided but no DropTableDef exists at Resources/DropTables/{normalizedId}. Falling back to serialized dropTable/legacy lootPool.");
            }
        }

        // Existing behavior continuation (Step 2): use serialized corpse table.
        if (dropTable != null)
            return dropTable;

        // Null here intentionally preserves legacy pool path in SeedLootServer.
        return null;
    }

    private void SeedLootServer()
    {
        _loot.Clear();

        if (itemDefCatalog == null || itemInstanceFactory == null)
        {
            Debug.LogError("[CorpseLootInteractable] Missing required references.");
            return;
        }

        // IMPORTANT deterministic seed behavior:
        // 1) Preferred path (new): if the corpse was handed an explicit server seed
        //    from combat death context, always use that so all loot generation is
        //    deterministic across server-side callsites/replays.
        // 2) Back-compat fallback (old): if that bridge is missing, continue using
        //    NetworkObjectId-derived seed so previous behavior remains intact.
        //    ResolveSeedContextServer already emits the warning for this fallback.
        uint baseSeed = _hasServerLootSeed
            ? _serverLootSeed
            : (uint)(NetworkObject.NetworkObjectId * 2654435761u) ^ 0xC0FFEEu;
        var rng = new DeterministicRng(unchecked((int)baseSeed));

        int clampedMin = Mathf.Max(0, minDrops);
        int clampedMax = Mathf.Max(clampedMin, maxDrops);
        int dropCount = RangeInclusive(rng, clampedMin, clampedMax);

        // Resolve effective drop table with backward-compatible precedence:
        // 1) Monster-driven loot table id from CorpseLootSeedNet (if non-empty and valid)
        // 2) Serialized corpse dropTable reference (existing Step 2 behavior)
        // 3) Legacy loot-pool path below
        DropTableDef resolvedDropTable = ResolveDropTableServer();
        if (resolvedDropTable != null)
        {
            var outputs = DropTableResolver.Roll(resolvedDropTable, baseSeed, baseRolls: dropCount, skillValue: float.PositiveInfinity);
            int createdIndex = 0;

            if (outputs != null)
            {
                for (int i = 0; i < outputs.Count; i++)
                {
                    var output = outputs[i];
                    int quantity = Mathf.Max(0, output.Quantity);

                    // We intentionally create one instance per quantity unit.
                    // This is the safest compatibility choice because it preserves
                    // exact quantity semantics even if stackability rules differ
                    // between item defs or runtime consumers.
                    for (int q = 0; q < quantity; q++)
                    {
                        uint itemSeed = baseSeed + (uint)createdIndex * 1013904223u;
                        createdIndex++;

                        if (!itemInstanceFactory.TryCreateLootItem(output.ItemId, itemSeed, out var item) || item == null)
                            continue;

                        item.EnsureInstanceId();
                        _loot.Add(item);
                    }
                }
            }

            if (debugLogs)
            {
                Debug.Log($"[CorpseLootInteractable] SeedLootServer used dropTable path on corpse {NetworkObject.NetworkObjectId}. baseSeed={baseSeed}, dropRolls={dropCount}, generated={_loot.Count}.");
            }

            return;
        }

        // Legacy behavior path (backward compatibility):
        // draw item def ids from lootPoolItemDefIds (or fallback debug item), then
        // create one instance per selected item exactly as before.
        List<string> selectedItemDefIds = new(dropCount);
        if (lootPoolItemDefIds != null && lootPoolItemDefIds.Count > 0)
        {
            SelectLootFromPool(rng, lootPoolItemDefIds, dropCount, selectedItemDefIds);
        }
        else if (!string.IsNullOrWhiteSpace(debugItemDefId))
        {
            selectedItemDefIds.Add(debugItemDefId);
        }

        for (int i = 0; i < selectedItemDefIds.Count; i++)
        {
            string itemDefId = selectedItemDefIds[i];
            uint itemSeed = baseSeed + (uint)i * 1013904223u;
            if (!itemInstanceFactory.TryCreateLootItem(itemDefId, itemSeed, out var item) || item == null)
                continue;

            item.EnsureInstanceId();
            _loot.Add(item);
        }

        if (debugLogs)
        {
            Debug.Log($"[CorpseLootInteractable] SeedLootServer used lootPool path on corpse {NetworkObject.NetworkObjectId}. baseSeed={baseSeed}, requestedDrops={dropCount}, selected={selectedItemDefIds.Count}, generated={_loot.Count}.");
        }
    }

    private bool TryResolvePlayer(NetworkBehaviour interactor, out PlayerInventoryComponent playerInventory, out NetworkObject playerObject)
    {
        playerInventory = null;
        playerObject = null;

        if (!interactor.TryGetComponent(out playerInventory))
            return false;

        playerObject = playerInventory.NetworkObject;
        return playerObject != null;
    }

    private bool TryGetPlayerInventory(ulong clientId, out PlayerInventoryComponent inventory)
    {
        inventory = null;

        if (NetworkManager == null)
            return false;

        if (!NetworkManager.ConnectedClients.TryGetValue(clientId, out var client) || client.PlayerObject == null)
            return false;

        return client.PlayerObject.TryGetComponent(out inventory);
    }

    private bool IsPlayerInRange(Vector3 playerPosition)
    {
        float sqrDistance = (playerPosition - transform.position).sqrMagnitude;
        return sqrDistance <= InteractRange * InteractRange;
    }

    private static bool IsLootAllowedInCurrentScene()
    {
        // TODO: Wire this to a dedicated loot/inventory interaction scene gate when one exists.
        // ResourceGatheringAllowed is not semantically correct for corpse looting.
        return true;
    }

    private void SendSnapshotToClient(ulong clientId)
    {
        var snapshot = BuildSnapshot();
        ReceiveLootSnapshotClientRpc(snapshot, new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
        });
    }

    private LootSnapshotPayload BuildSnapshot()
    {
        var payload = new LootSnapshotPayload { Entries = new CorpseLootSnapshotEntry[_loot.Count] };

        for (int i = 0; i < _loot.Count; i++)
        {
            ItemInstance item = _loot[i];
            item.EnsureInstanceId();

            string display = item.itemDefId;
            string iconAddress = string.Empty;
            if (itemDefCatalog != null && itemDefCatalog.TryGet(item.itemDefId, out var def) && def != null)
            {
                display = string.IsNullOrWhiteSpace(def.displayName) ? item.itemDefId : def.displayName;
                iconAddress = def.iconAddress ?? string.Empty;
            }

            string affixSummary = BuildAffixSummary(item);
            payload.Entries[i] = new CorpseLootSnapshotEntry
            {
                InstanceId = new FixedString64Bytes(item.instanceId),
                ItemDefId = new FixedString64Bytes(item.itemDefId ?? string.Empty),
                DisplayName = new FixedString64Bytes(display),
                IconAddress = new FixedString128Bytes(iconAddress),
                StackCount = item.stackCount,
                DurabilityCurrent = item.durabilityCurrent,
                DurabilityMax = item.durabilityMax,
                AffixSummary = new FixedString128Bytes(affixSummary)
            };
        }

        return payload;
    }

    private static string BuildAffixSummary(ItemInstance item)
    {
        if (item?.affixes == null || item.affixes.Count == 0)
            return string.Empty;

        int count = Mathf.Min(3, item.affixes.Count);
        var names = new string[count];
        for (int i = 0; i < count; i++)
            names[i] = item.affixes[i].id.ToString();

        return string.Join(", ", names);
    }

    private int FindLootIndex(string instanceId)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
            return -1;

        for (int i = 0; i < _loot.Count; i++)
        {
            if (string.Equals(_loot[i].instanceId, instanceId, StringComparison.Ordinal))
                return i;
        }

        return -1;
    }

    private void SendTakeItemResult(ulong clientId, LootTakeResultCode code)
    {
        ReceiveTakeItemResultClientRpc((byte)code, new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
        });
    }

    [ClientRpc]
    private void ReceiveLootSnapshotClientRpc(LootSnapshotPayload payload, ClientRpcParams clientRpcParams = default)
    {
        var list = new List<CorpseLootSnapshotEntry>(payload.Entries?.Length ?? 0);
        if (payload.Entries != null)
            list.AddRange(payload.Entries);

        ClientLootSnapshotReceived?.Invoke(NetworkObjectId, list);
    }

    [ClientRpc]
    private void ReceiveTakeItemResultClientRpc(byte resultCode, ClientRpcParams clientRpcParams = default)
    {
        ClientTakeItemResultReceived?.Invoke(NetworkObjectId, (LootTakeResultCode)resultCode);
    }

    private static int RangeInclusive(DeterministicRng rng, int min, int max)
    {
        if (max <= min)
            return min;

        return rng.NextInt(min, max + 1);
    }

    private static void SelectLootFromPool(DeterministicRng rng, List<string> pool, int dropCount, List<string> results)
    {
        if (dropCount <= 0 || pool == null || pool.Count == 0)
            return;

        int poolCount = pool.Count;
        if (poolCount >= dropCount)
        {
            var indices = new List<int>(poolCount);
            for (int i = 0; i < poolCount; i++)
                indices.Add(i);

            for (int i = 0; i < dropCount; i++)
            {
                int pickIndex = rng.NextInt(0, indices.Count);
                int poolIndex = indices[pickIndex];
                indices.RemoveAt(pickIndex);
                results.Add(pool[poolIndex]);
            }
        }
        else
        {
            for (int i = 0; i < dropCount; i++)
            {
                int poolIndex = rng.NextInt(0, poolCount);
                results.Add(pool[poolIndex]);
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        interactRange = Mathf.Max(0.1f, interactRange);
    }
#endif
}

public enum LootTakeResultCode : byte
{
    Success = 0,
    InvalidPlayer = 1,
    OutOfRange = 2,
    ItemNotFound = 3,
    InventoryFull = 4,
    SceneRulesBlocked = 5
}

public struct LootSnapshotPayload : INetworkSerializable
{
    public CorpseLootSnapshotEntry[] Entries;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        int count = Entries?.Length ?? 0;
        serializer.SerializeValue(ref count);

        if (serializer.IsReader)
            Entries = new CorpseLootSnapshotEntry[count];

        for (int i = 0; i < count; i++)
            serializer.SerializeValue(ref Entries[i]);
    }
}

public struct CorpseLootSnapshotEntry : INetworkSerializable
{
    public FixedString64Bytes InstanceId;
    public FixedString64Bytes ItemDefId;
    public FixedString64Bytes DisplayName;
    public FixedString128Bytes IconAddress;
    public int StackCount;
    public float DurabilityCurrent;
    public float DurabilityMax;
    public FixedString128Bytes AffixSummary;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref InstanceId);
        serializer.SerializeValue(ref ItemDefId);
        serializer.SerializeValue(ref DisplayName);
        serializer.SerializeValue(ref IconAddress);
        serializer.SerializeValue(ref StackCount);
        serializer.SerializeValue(ref DurabilityCurrent);
        serializer.SerializeValue(ref DurabilityMax);
        serializer.SerializeValue(ref AffixSummary);
    }
}
