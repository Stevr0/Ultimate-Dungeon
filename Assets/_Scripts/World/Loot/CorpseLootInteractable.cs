// ============================================================================
// CorpseLootInteractable.cs (SPIKE)
// ----------------------------------------------------------------------------
// Goal:
// - Represent a dead monster's corpse as an interactable world object
// - Allow a player to Interact() to loot items
// - Transfer items server-authoritatively into the player's inventory
// - Despawn the corpse when empty (v0.1: immediately after looting)
//
// Spike Goal:
// - Enable Spawn → Kill → Loot vertical slice
//
// Success Criteria:
// - Monster dies → corpse appears
// - Player interacts → receives item
// - Corpse despawns
//
// Delete When:
// - Replaced by full Corpse / Loot / Persistence system
//
// Dependencies:
// - IInteractable
// - PlayerInventoryComponent
// - InventoryRuntimeModel
// - ItemInstance
// ============================================================================

using System;
using System.Collections.Generic;
using UltimateDungeon.Progression;
using UltimateDungeon.Items;
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
    [Tooltip("ItemDefCatalog asset with all ItemDefs. Needed so the corpse can validate/stack items.")]
    [SerializeField] private ItemDefCatalog itemDefCatalog;

    [Tooltip("Factory used to create server-authoritative ItemInstances.")]
    [SerializeField] private ItemInstanceFactory itemInstanceFactory;

    [Header("Loot (DEBUG / SPIKE)")]
    [SerializeField] private int minDrops = 1;
    [SerializeField] private int maxDrops = 3;

    [Tooltip("ItemDefIds to randomly pick from. Populate this list on the corpse prefab (e.g. 10–30 entries).")]
    [SerializeField] private List<string> lootPoolItemDefIds = new List<string>();

    [Tooltip("ItemDefId to seed into the corpse when spawned (fallback when loot pool is empty).")]
    [SerializeField] private string debugItemDefId = "mainhand_sword_shortsword";

    // Runtime-only inventory for the corpse
    private InventoryRuntimeModel _inventory;

    // --------------------------------------------------------------------
    // IInteractable
    // --------------------------------------------------------------------

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public float InteractRange => Mathf.Max(0.1f, interactRange);
    public ulong NetworkObjectId => NetworkObject.NetworkObjectId;

    // --------------------------------------------------------------------
    // Lifecycle
    // --------------------------------------------------------------------

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsServer)
            return;

        int slotCount = Mathf.Max(1, maxDrops);
        _inventory = new InventoryRuntimeModel(slotCount);

        // Validate we can resolve ItemDefs (required for stacking rules, etc.)
        if (itemDefCatalog == null)
        {
            Debug.LogError("[CorpseLootInteractable] Missing ItemDefCatalog reference on corpse prefab.");
            return;
        }

        if (itemInstanceFactory == null)
        {
            Debug.LogError("[CorpseLootInteractable] Missing ItemInstanceFactory reference on corpse prefab.");
            return;
        }

        uint baseSeed = (uint)(NetworkObject.NetworkObjectId * 2654435761u) ^ 0xC0FFEEu;
        var rng = new DeterministicRng(unchecked((int)baseSeed));

        int clampedMin = Mathf.Max(0, minDrops);
        int clampedMax = Mathf.Max(clampedMin, maxDrops);
        int dropCount = RangeInclusive(rng, clampedMin, clampedMax);
        dropCount = Mathf.Clamp(dropCount, 0, clampedMax);

        List<string> selectedItemDefIds = new List<string>(dropCount);
        if (lootPoolItemDefIds != null && lootPoolItemDefIds.Count > 0)
        {
            SelectLootFromPool(rng, lootPoolItemDefIds, dropCount, selectedItemDefIds);
        }
        else if (!string.IsNullOrWhiteSpace(debugItemDefId))
        {
            selectedItemDefIds.Add(debugItemDefId);
        }
        else
        {
            Debug.LogWarning("[CorpseLootInteractable] Loot pool is empty and no debug item is configured; corpse will spawn empty.");
        }

        for (int i = 0; i < selectedItemDefIds.Count; i++)
        {
            string itemDefId = selectedItemDefIds[i];
            uint itemSeed = baseSeed + (uint)i * 1013904223u;
            if (itemInstanceFactory.TryCreateLootItem(itemDefId, itemSeed, out var item))
            {
                var addResult = _inventory.TryAdd(item, itemDefCatalog, out _);
                if (addResult != InventoryOpResult.Success)
                {
                    Debug.LogWarning($"[CorpseLootInteractable] Failed to seed loot '{itemDefId}': {addResult}");
                }
            }
            else
            {
                Debug.LogWarning($"[CorpseLootInteractable] Failed to create loot item '{itemDefId}'.");
            }
        }
    }


    // --------------------------------------------------------------------
    // Interaction
    // --------------------------------------------------------------------

    public void ServerInteract(NetworkBehaviour interactor)
    {
        if (!IsServer)
            return;

        if (interactor == null)
            return;

        if (_inventory == null)
        {
            Debug.LogWarning("[CorpseLootInteractable] Corpse inventory is missing; aborting loot transfer.");
            return;
        }

        // We only allow players to loot
        if (!interactor.TryGetComponent(out PlayerInventoryComponent playerInventory))
        {
            Debug.LogWarning("[CorpseLootInteractable] Interactor has no PlayerInventoryComponent.");
            return;
        }

        // Transfer all items from corpse to player
        for (int i = 0; i < _inventory.SlotCount; i++)
        {
            var slot = _inventory.GetSlot(i);
            if (slot.IsEmpty || slot.item == null)
                continue;

            // Try to add to player inventory (PlayerInventoryComponent already validates ItemDefId)
            var result = playerInventory.ServerTryAdd(slot.item, out _);
            if (result == InventoryOpResult.Success)
            {
                _inventory.TryRemoveAt(i, out _);
            }
            else
            {
                // If the player's inventory is full (or item unknown), we keep the item in the corpse.
                Debug.LogWarning($"[CorpseLootInteractable] Player inventory add failed: {result}");
            }
        }

        // v0.1: Despawn if empty, otherwise stay so the player can try again later.
        bool anyRemaining = false;
        for (int i = 0; i < _inventory.SlotCount; i++)
        {
            var s = _inventory.GetSlot(i);
            if (!s.IsEmpty && s.item != null) { anyRemaining = true; break; }
        }

        if (!anyRemaining)
            NetworkObject.Despawn(destroy: true);
    }

    private static int RangeInclusive(DeterministicRng rng, int min, int max)
    {
        if (max <= min)
            return min;

        return rng.NextInt(min, max + 1);
    }

    private static void SelectLootFromPool(
        DeterministicRng rng,
        List<string> pool,
        int dropCount,
        List<string> results)
    {
        if (dropCount <= 0)
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
