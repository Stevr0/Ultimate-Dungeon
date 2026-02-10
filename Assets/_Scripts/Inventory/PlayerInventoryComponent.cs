// ============================================================================
// PlayerInventoryComponent.cs — v2 (UI drag/drop RPC)
// ----------------------------------------------------------------------------
// Adds client->server requests for inventory rearranging.
//
// New client API:
// - RequestMoveOrSwap(fromSlot, toSlot)
//
// Server enforcement:
// - Only server mutates InventoryRuntimeModel.
// - Only the owning client can request moves.
// ============================================================================

using Unity.Netcode;
using UnityEngine;

namespace UltimateDungeon.Items
{
    [DisallowMultipleComponent]
    public sealed class PlayerInventoryComponent : NetworkBehaviour
    {
        [Header("Required")]
        [Tooltip("ItemDefCatalog asset with all ItemDefs.")]
        [SerializeField] private ItemDefCatalog itemDefCatalog;

        [Header("Inventory")]
        [SerializeField] private int slotCount = InventoryRuntimeModel.DefaultSlotCount;

        [System.NonSerialized] private InventoryRuntimeModel _inventory;

        public InventoryRuntimeModel Inventory => _inventory;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _inventory = new InventoryRuntimeModel(slotCount);
        }

        // --------------------------------------------------------------------
        // Client-side requests (UI)
        // --------------------------------------------------------------------

        /// <summary>
        /// Called by UI when user drops an inventory item onto another slot.
        /// </summary>
        public void RequestMoveOrSwap(int fromSlot, int toSlot)
        {
            if (!IsOwner)
                return;

            // Client -> Server request.
            MoveOrSwapServerRpc(fromSlot, toSlot);
        }

        [ServerRpc]
        private void MoveOrSwapServerRpc(int fromSlot, int toSlot)
        {
            if (!IsServer)
                return;

            ServerTryMoveOrSwap(fromSlot, toSlot);
        }

        // --------------------------------------------------------------------
        // Server-only API (gameplay systems)
        // --------------------------------------------------------------------

        public InventoryOpResult ServerTryAdd(ItemInstance item, out int placedSlot)
        {
            placedSlot = -1;

            if (!IsServer)
            {
                Debug.LogWarning("[PlayerInventoryComponent] ServerTryAdd called on a client. Ignored.");
                return InventoryOpResult.Failed;
            }

            if (itemDefCatalog == null)
            {
                Debug.LogError("[PlayerInventoryComponent] Missing ItemDefCatalog reference.");
                return InventoryOpResult.UnknownItemDef;
            }

            return _inventory.TryAdd(item, itemDefCatalog, out placedSlot);
        }

        /// <summary>
        /// Targeted placement into a specific empty slot (no auto-find).
        /// Used by targeted unequip.
        /// </summary>
        public InventoryOpResult ServerTryPlaceIntoEmptySlot(int toSlot, ItemInstance item)
        {
            if (!IsServer)
            {
                Debug.LogWarning("[PlayerInventoryComponent] ServerTryPlaceIntoEmptySlot called on a client. Ignored.");
                return InventoryOpResult.Failed;
            }

            if (itemDefCatalog == null)
            {
                Debug.LogError("[PlayerInventoryComponent] Missing ItemDefCatalog reference.");
                return InventoryOpResult.UnknownItemDef;
            }

            return _inventory.TryPlaceIntoEmptySlot(toSlot, item, itemDefCatalog);
        }

        public InventoryOpResult ServerTryMoveOrSwap(int fromSlot, int toSlot)
        {
            if (!IsServer)
            {
                Debug.LogWarning("[PlayerInventoryComponent] ServerTryMoveOrSwap called on a client. Ignored.");
                return InventoryOpResult.Failed;
            }

            if (itemDefCatalog == null)
            {
                Debug.LogError("[PlayerInventoryComponent] Missing ItemDefCatalog reference.");
                return InventoryOpResult.UnknownItemDef;
            }

            return _inventory.TryMoveOrSwap(fromSlot, toSlot, itemDefCatalog);
        }

        public InventoryOpResult ServerTrySplit(int fromSlot, int toSlot, int splitAmount)
        {
            if (!IsServer)
            {
                Debug.LogWarning("[PlayerInventoryComponent] ServerTrySplit called on a client. Ignored.");
                return InventoryOpResult.Failed;
            }

            if (itemDefCatalog == null)
            {
                Debug.LogError("[PlayerInventoryComponent] Missing ItemDefCatalog reference.");
                return InventoryOpResult.UnknownItemDef;
            }

            return _inventory.TrySplit(fromSlot, toSlot, splitAmount, itemDefCatalog);
        }

        public InventoryOpResult ServerTryRemoveAt(int slot, out ItemInstance removed)
        {
            removed = null;

            if (!IsServer)
            {
                Debug.LogWarning("[PlayerInventoryComponent] ServerTryRemoveAt called on a client. Ignored.");
                return InventoryOpResult.Failed;
            }

            return _inventory.TryRemoveAt(slot, out removed);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (slotCount < 0) slotCount = 0;
            if (slotCount > 300) slotCount = 300;
        }
#endif
    }
}
