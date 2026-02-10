// ============================================================================
// InventorySlotDropTarget.cs (UI) — v2
// ----------------------------------------------------------------------------
// Purpose:
// - Accept drops onto an inventory slot.
// - Supports:
//   1) Inventory -> Inventory (move or swap into the *exact* target slot)
//   2) Equipment -> Inventory (unequip into the *exact* target slot)
//
// Design Notes:
// - Inventory->Inventory uses move/swap.
// - Equipment->Inventory is intentionally conservative:
//     - We REQUIRE the target slot to be empty (server-enforced).
//     - This avoids illegal swaps (e.g., swapping a random inventory item into
//       an equipment slot that it can't legally occupy).
//
// Setup:
// - Add this component to the same GameObject as InventorySlotViewUI.
// ============================================================================

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
    }
}
