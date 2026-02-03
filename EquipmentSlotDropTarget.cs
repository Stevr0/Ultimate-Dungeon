// ============================================================================
// EquipmentSlotDropTarget.cs (UI)
// ----------------------------------------------------------------------------
// Purpose:
// - Accept drops from Inventory slots onto equipment slots.
// - Sends a server-authoritative equip request.
//
// Setup:
// - Add this component to the same GameObject as EquipmentSlotViewUI.
// - No inspector wiring needed.
//
// Notes:
// - For MVP we only support Inventory → Equipment.
// - Equipment → Inventory drag can be added later by adding a drag source.
// ============================================================================

using UnityEngine;
using UnityEngine.EventSystems;
using UltimateDungeon.Items;

namespace UltimateDungeon.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EquipmentSlotViewUI))]
    public sealed class EquipmentSlotDropTarget : MonoBehaviour, IDropHandler
    {
        private EquipmentSlotViewUI _view;

        private void Awake()
        {
            _view = GetComponent<EquipmentSlotViewUI>();
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (_view == null)
                return;

            // Only accept inventory slot drags.
            if (UIInventoryDragContext.Kind != UIInventoryDragContext.DragKind.InventorySlot)
                return;

            int sourceInvSlot = UIInventoryDragContext.SourceInventorySlot;
            if (sourceInvSlot < 0)
                return;

            // Find local equipment component and request equip.
            // (Server authoritative: this will run a ServerRpc.)
            var equip = FindFirstObjectByType<PlayerEquipmentComponent>();
            if (equip == null)
            {
                Debug.LogWarning("[EquipmentSlotDropTarget] No PlayerEquipmentComponent found in scene.");
                return;
            }

            equip.RequestEquipFromInventory(sourceInvSlot, _view.SlotId);
        }
    }
}
