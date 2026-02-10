// ============================================================================
// EquipmentUIBinder.cs (UI Binder)
// ----------------------------------------------------------------------------
// Purpose:
// - Reads PlayerEquipmentComponent's replicated list
// - Sets slot icons in InventoryEquipmentPanelUI
//
// Setup:
// - Add this to your Inventory window root (same object as InventoryUIBinder is fine).
// - Assign:
//   - ItemDefCatalog
//   - InventoryEquipmentPanelUI
//
// Notes:
// - This is display-only. All equipment changes happen via server RPC.
// ============================================================================

using UnityEngine;
using UltimateDungeon.Items;

namespace UltimateDungeon.UI
{
    [DisallowMultipleComponent]
    public sealed class EquipmentUIBinder : MonoBehaviour
    {
        [Header("Required")]
        [SerializeField] private ItemDefCatalog itemDefCatalog;

        [Header("Required")]
        [SerializeField] private InventoryEquipmentPanelUI equipmentPanel;

        private PlayerEquipmentComponent _equipment;

        private void OnEnable()
        {
            TryBind();
        }

        private void OnDisable()
        {
            Unbind();
        }

        private void Update()
        {
            // Simple retry loop (like InventoryUIBinder) so it binds after spawn.
            if (_equipment == null)
                TryBind();
        }

        private void TryBind()
        {
            if (_equipment != null)
                return;

            _equipment = FindFirstObjectByType<PlayerEquipmentComponent>();
            if (_equipment == null)
                return;

            _equipment.OnEquipmentChanged += RefreshAll;
            RefreshAll();
        }

        private void Unbind()
        {
            if (_equipment != null)
                _equipment.OnEquipmentChanged -= RefreshAll;

            _equipment = null;
        }

        private void RefreshAll()
        {
            if (_equipment == null || equipmentPanel == null)
                return;

            // Clear all first (keeps UI deterministic).
            equipmentPanel.SetSlotIcon(EquipmentSlotId.Bag, null);
            equipmentPanel.SetSlotIcon(EquipmentSlotId.Head, null);
            equipmentPanel.SetSlotIcon(EquipmentSlotId.Neck, null);
            equipmentPanel.SetSlotIcon(EquipmentSlotId.Mainhand, null);
            equipmentPanel.SetSlotIcon(EquipmentSlotId.Chest, null);
            equipmentPanel.SetSlotIcon(EquipmentSlotId.Offhand, null);
            equipmentPanel.SetSlotIcon(EquipmentSlotId.BeltA, null);
            equipmentPanel.SetSlotIcon(EquipmentSlotId.BeltB, null);
            equipmentPanel.SetSlotIcon(EquipmentSlotId.Foot, null);
            equipmentPanel.SetSlotIcon(EquipmentSlotId.Mount, null);

            if (itemDefCatalog == null)
                return;

            // Paint equipped items.
            var list = _equipment.EquippedNet;
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                if (e.IsEmpty)
                    continue;

                var itemId = e.itemDefId.ToString();
                if (!itemDefCatalog.TryGet(itemId, out var def) || def == null)
                    continue;

                var icon = ItemIconResolver.Resolve(def);
                equipmentPanel.SetSlotIcon(e.slot, icon);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (equipmentPanel == null)
                equipmentPanel = GetComponentInChildren<InventoryEquipmentPanelUI>(includeInactive: true);
        }
#endif
    }
}
