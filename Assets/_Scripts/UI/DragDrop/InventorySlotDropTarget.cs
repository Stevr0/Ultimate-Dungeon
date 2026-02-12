// ============================================================================
// InventorySlotDropTarget.cs (UI) - v3
// ----------------------------------------------------------------------------
// Purpose:
// - Accept drops onto an inventory slot.
// - Supports:
//   1) Inventory -> Inventory (move or swap into the exact target slot)
//   2) Equipment -> Inventory (unequip into the exact target slot)
//   3) Corpse loot -> Inventory (request server-authoritative take)
//
// Corpse loot flow:
// - We DO NOT move the item locally.
// - On valid drop, we call CorpseLootInteractable.RequestTakeItemServerRpc(instanceId).
// - Inventory/corpse UI then refreshes from replicated/server snapshot state.
// ============================================================================

using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UltimateDungeon.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(InventorySlotViewUI))]
    public sealed class InventorySlotDropTarget : MonoBehaviour, IDropHandler
    {
        private InventorySlotViewUI _view;

        private void Awake()
        {
            _view = GetComponent<InventorySlotViewUI>();
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (_view == null)
                return;

            // ----------------------------------------------------------------
            // Corpse Loot -> Inventory (request take, no local move)
            // ----------------------------------------------------------------
            if (UIInventoryDragContext.Kind == UIInventoryDragContext.DragKind.LootEntry)
            {
                TryRequestLootTake();
                return;
            }

            // ----------------------------------------------------------------
            // Inventory -> Inventory (move/swap)
            // ----------------------------------------------------------------
            if (UIInventoryDragContext.Kind == UIInventoryDragContext.DragKind.InventorySlot)
            {
                int fromSlot = UIInventoryDragContext.SourceInventorySlot;
                int toSlot = _view.SlotIndex;

                if (fromSlot < 0)
                    return;

                // Dropping onto itself = no-op.
                if (fromSlot == toSlot)
                    return;

                var inv = FindFirstObjectByType<UltimateDungeon.Items.PlayerInventoryComponent>();
                if (inv == null)
                {
                    Debug.LogWarning("[InventorySlotDropTarget] No PlayerInventoryComponent found in scene.");
                    return;
                }

                inv.RequestMoveOrSwap(fromSlot, toSlot);
                return;
            }

            // ----------------------------------------------------------------
            // Equipment -> Inventory (targeted unequip)
            // ----------------------------------------------------------------
            if (UIInventoryDragContext.Kind == UIInventoryDragContext.DragKind.EquipmentSlot)
            {
                var equipSlot = UIInventoryDragContext.SourceEquipmentSlot;
                if ((int)equipSlot < 0)
                    return;

                var equip = FindFirstObjectByType<UltimateDungeon.Items.PlayerEquipmentComponent>();
                if (equip == null)
                {
                    Debug.LogWarning("[InventorySlotDropTarget] No PlayerEquipmentComponent found in scene.");
                    return;
                }

                // Targeted unequip into the exact slot we dropped on.
                equip.RequestUnequipToInventory(equipSlot, _view.SlotIndex);
                return;
            }
        }

        private static void TryRequestLootTake()
        {
            if (NetworkManager.Singleton == null || NetworkManager.Singleton.SpawnManager == null)
                return;

            ulong corpseNetId = UIInventoryDragContext.SourceCorpseNetId;
            string instance = UIInventoryDragContext.SourceLootInstanceId;
            if (corpseNetId == 0 || string.IsNullOrWhiteSpace(instance))
                return;

            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(corpseNetId, out var corpseObj) || corpseObj == null)
                return;

            if (!corpseObj.TryGetComponent(out CorpseLootInteractable corpse))
                return;

            corpse.RequestTakeItemServerRpc(new FixedString64Bytes(instance));
        }
    }
}
