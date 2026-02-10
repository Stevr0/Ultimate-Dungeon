using System;
using System.Collections.Generic;
using UltimateDungeon.Items;
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
    [SerializeField] private string debugItemDefId = "mainhand_sword_shortsword";

    private readonly List<ItemInstance> _loot = new();

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

    private void SeedLootServer()
    {
        _loot.Clear();

        if (itemDefCatalog == null || itemInstanceFactory == null)
        {
            Debug.LogError("[CorpseLootInteractable] Missing required references.");
            return;
        }

        uint baseSeed = (uint)(NetworkObject.NetworkObjectId * 2654435761u) ^ 0xC0FFEEu;
        var rng = new DeterministicRng(unchecked((int)baseSeed));

        int clampedMin = Mathf.Max(0, minDrops);
        int clampedMax = Mathf.Max(clampedMin, maxDrops);
        int dropCount = RangeInclusive(rng, clampedMin, clampedMax);

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
