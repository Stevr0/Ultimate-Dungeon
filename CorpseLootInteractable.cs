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
    [Tooltip("ItemDefId to seed into the corpse when spawned (v0.1 hardcoded)")]
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

        // Create a tiny inventory (1 slot is enough for the spike)
        _inventory = new InventoryRuntimeModel(slotCount: 1);

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

        // Seed a single debug item (this proves loot transfer end-to-end)
        if (!string.IsNullOrWhiteSpace(debugItemDefId))
        {
            uint seed = BuildLootSeed(NetworkObjectId, debugItemDefId);
            if (itemInstanceFactory.TryCreateLootItem(debugItemDefId, seed, out var item))
            {
                var addResult = _inventory.TryAdd(item, itemDefCatalog, out _);
                if (addResult != InventoryOpResult.Success)
                    Debug.LogWarning($"[CorpseLootInteractable] Failed to seed loot '{debugItemDefId}': {addResult}");
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

    private static uint BuildLootSeed(ulong networkObjectId, string itemDefId)
    {
        const uint salt = 0x9E3779B9u;
        uint hash = StableStringHash(itemDefId);
        uint seed = (uint)networkObjectId;
        seed ^= hash;
        seed = (seed * 16777619u) ^ salt;
        return seed;
    }

    private static uint StableStringHash(string value)
    {
        if (string.IsNullOrEmpty(value))
            return 0u;

        unchecked
        {
            uint hash = 2166136261u;
            for (int i = 0; i < value.Length; i++)
            {
                hash ^= value[i];
                hash *= 16777619u;
            }
            return hash;
        }
    }

    #if UNITY_EDITOR
    private void OnValidate()
    {
        interactRange = Mathf.Max(0.1f, interactRange);
    }
#endif
}
