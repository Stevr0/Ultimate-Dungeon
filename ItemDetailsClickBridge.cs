// ============================================================================
// ItemDetailsClickBridge.cs
// ----------------------------------------------------------------------------
// UI WIRING SCRIPT (MVP)
//
// Goal:
// - Clicking an InventorySlotViewUI or EquipmentSlotViewUI opens the Item Details
//   window and renders the clicked item's details.
//
// Rendering path:
// - Inventory click -> InventoryUIBinder.ShowItemDetailsFromInventory(slotIndex)
// - Equipment click -> InventoryUIBinder.ShowItemDetailsFromEquipment(slotId)
//
// Why this is separate from InventoryUIBinder:
// - InventoryUIBinder is responsible for *data binding / painting* slots.
// - This class is responsible for *input -> open window -> show details*.
//   (Keeps responsibilities clean and avoids circular references.)
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateDungeon.UI
{
    [DisallowMultipleComponent]
    public sealed class ItemDetailsClickBridge : MonoBehaviour
    {
        [Header("Window")]
        [Tooltip("Reference to the UIWindowManager in your Canvas_Windows hierarchy.")]
        [SerializeField] private UIWindowManager windowManager;

        [Tooltip("The WindowId string on the Window_ItemDetails root (UIWindow.WindowId).")]
        [SerializeField] private string itemDetailsWindowId = "ItemDetails";

        [Header("Details Rendering")]
        [Tooltip("We delegate rendering to InventoryUIBinder because it already knows how to resolve inventory/equipment items into ItemDetailsPanelUI.")]
        [SerializeField] private InventoryUIBinder inventoryBinder;

        [Header("Slot Roots (Optional) - used to auto-subscribe")]
        [Tooltip("Root containing InventorySlotViewUI children. If empty, we try to use InventoryUIBinder's slotsRoot via Find().")]
        [SerializeField] private Transform inventorySlotsRoot;

        [Tooltip("Root containing EquipmentSlotViewUI children.")]
        [SerializeField] private Transform equipmentSlotsRoot;

        [Header("Selection Highlight (Optional)")]
        [Tooltip("If true, we highlight the last clicked slot (inventory OR equipment).")]
        [SerializeField] private bool showSelectionHighlight = true;

        private readonly List<InventorySlotViewUI> _invSlots = new List<InventorySlotViewUI>(64);
        private readonly List<EquipmentSlotViewUI> _eqSlots = new List<EquipmentSlotViewUI>(16);

        private InventorySlotViewUI _selectedInv;
        private EquipmentSlotViewUI _selectedEq;

        private void Awake()
        {
            // Auto-find references to reduce inspector pain.
            if (windowManager == null)
                windowManager = FindFirstObjectByType<UIWindowManager>();

            if (inventoryBinder == null)
                inventoryBinder = FindFirstObjectByType<InventoryUIBinder>();

            // If you didn't assign roots, try to infer them.
            if (inventorySlotsRoot == null && inventoryBinder != null)
            {
                // InventoryUIBinder stores its slots root privately, so we can't read it directly.
                // Best effort: search under the binder for slot views.
                inventorySlotsRoot = inventoryBinder.transform;
            }
        }

        private void OnEnable()
        {
            RebuildSlotLists();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        // --------------------------------------------------------------------
        // Slot discovery
        // --------------------------------------------------------------------

        private void RebuildSlotLists()
        {
            _invSlots.Clear();
            _eqSlots.Clear();

            if (inventorySlotsRoot != null)
                inventorySlotsRoot.GetComponentsInChildren(includeInactive: true, result: _invSlots);

            if (equipmentSlotsRoot != null)
                equipmentSlotsRoot.GetComponentsInChildren(includeInactive: true, result: _eqSlots);
        }

        private void Subscribe()
        {
            foreach (var s in _invSlots)
            {
                if (s == null) continue;
                s.OnLeftClick += HandleInventoryLeftClick;
                // You can extend later:
                // s.OnRightClick += ... (context menu)
                // s.OnDoubleClick += ... (equip/use)
            }

            foreach (var s in _eqSlots)
            {
                if (s == null) continue;
                s.OnLeftClick += HandleEquipmentLeftClick;
            }
        }

        private void Unsubscribe()
        {
            foreach (var s in _invSlots)
            {
                if (s == null) continue;
                s.OnLeftClick -= HandleInventoryLeftClick;
            }

            foreach (var s in _eqSlots)
            {
                if (s == null) continue;
                s.OnLeftClick -= HandleEquipmentLeftClick;
            }
        }

        // --------------------------------------------------------------------
        // Click handlers
        // --------------------------------------------------------------------

        private void HandleInventoryLeftClick(InventorySlotViewUI slotView)
        {
            if (slotView == null) return;
            if (inventoryBinder == null) return;

            ApplySelection(slotView, null);

            // IMPORTANT ORDER:
            // Open the window first so the details panel (likely a child) can be activated.
            OpenItemDetailsWindow();

            // Render the clicked inventory item.
            inventoryBinder.ShowItemDetailsFromInventory(slotView.SlotIndex);
        }

        private void HandleEquipmentLeftClick(EquipmentSlotViewUI slotView)
        {
            if (slotView == null) return;
            if (inventoryBinder == null) return;

            ApplySelection(null, slotView);

            OpenItemDetailsWindow();

            // Render the equipped item for this slot (if equipment is wired).
            inventoryBinder.ShowItemDetailsFromEquipment(slotView.SlotId);
        }

        private void OpenItemDetailsWindow()
        {
            if (windowManager == null) return;
            if (string.IsNullOrWhiteSpace(itemDetailsWindowId)) return;

            windowManager.Open(itemDetailsWindowId);
        }

        // --------------------------------------------------------------------
        // Selection highlight
        // --------------------------------------------------------------------

        private void ApplySelection(InventorySlotViewUI inv, EquipmentSlotViewUI eq)
        {
            if (!showSelectionHighlight)
                return;

            if (_selectedInv != null)
                _selectedInv.SetSelected(false);

            if (_selectedEq != null)
                _selectedEq.SetSelected(false);

            _selectedInv = inv;
            _selectedEq = eq;

            if (_selectedInv != null)
                _selectedInv.SetSelected(true);

            if (_selectedEq != null)
                _selectedEq.SetSelected(true);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!string.IsNullOrEmpty(itemDetailsWindowId))
                itemDetailsWindowId = itemDetailsWindowId.Trim();
        }
#endif
    }
}
