using System;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateDungeon.UI
{
    /// <summary>
    /// InventoryEquipmentPanelUI (UI-ONLY CONTROLLER)
    /// =============================================
    ///
    /// Controls the equipment slots shown at the top of the Inventory window.
    ///
    /// SETUP PHASE (now):
    /// - Holds references to the 10 equipment slot views
    /// - Handles selection highlighting
    /// - Forwards UI events (left/right/double click) as slot ids
    /// - No gameplay wiring
    ///
    /// WIRING PHASE (later):
    /// - Bind equipped item icons per slot
    /// - Listen for equipment change events from an authoritative model
    /// - Trigger equip/unequip requests through a UI binder (not directly here)
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InventoryEquipmentPanelUI : MonoBehaviour
    {
        // ----------------------------
        // Events (UI-only)
        // ----------------------------
        public event Action<EquipmentSlotId> OnSlotSelected;
        public event Action<EquipmentSlotId> OnSlotRightClicked;
        public event Action<EquipmentSlotId> OnSlotDoubleClicked;
        public event Action<EquipmentSlotId> OnSlotHoverEnter;
        public event Action<EquipmentSlotId> OnSlotHoverExit;

        [Header("Slots (Assign in Inspector)")]
        [SerializeField] private EquipmentSlotViewUI slotBag;
        [SerializeField] private EquipmentSlotViewUI slotHead;
        [SerializeField] private EquipmentSlotViewUI slotCape;
        [SerializeField] private EquipmentSlotViewUI slotMainHand;
        [SerializeField] private EquipmentSlotViewUI slotOffHand;
        [SerializeField] private EquipmentSlotViewUI slotChest;
        [SerializeField] private EquipmentSlotViewUI slotBoots;
        [SerializeField] private EquipmentSlotViewUI slotPotion;
        [SerializeField] private EquipmentSlotViewUI slotFood;
        [SerializeField] private EquipmentSlotViewUI slotMount;

        [Header("Selection")]
        [SerializeField] private bool highlightSelection = true;

        private readonly List<EquipmentSlotViewUI> _slots = new List<EquipmentSlotViewUI>();
        private EquipmentSlotId? _selectedSlot;

        public EquipmentSlotId? SelectedSlot => _selectedSlot;

        private void Awake()
        {
            // Build a list so we can iterate.
            _slots.Clear();
            AddIfNotNull(slotBag, EquipmentSlotId.Bag);
            AddIfNotNull(slotHead, EquipmentSlotId.Head);
            AddIfNotNull(slotCape, EquipmentSlotId.Neck);
            AddIfNotNull(slotMainHand, EquipmentSlotId.Mainhand);
            AddIfNotNull(slotOffHand, EquipmentSlotId.Offhand);
            AddIfNotNull(slotChest, EquipmentSlotId.Chest);
            AddIfNotNull(slotBoots, EquipmentSlotId.Foot);
            AddIfNotNull(slotPotion, EquipmentSlotId.BeltA);
            AddIfNotNull(slotFood, EquipmentSlotId.BeltB);
            AddIfNotNull(slotMount, EquipmentSlotId.Mount);

            // Subscribe to events.
            foreach (var s in _slots)
            {
                s.OnLeftClick += HandleLeftClick;
                s.OnRightClick += HandleRightClick;
                s.OnDoubleClick += HandleDoubleClick;
                s.OnHoverEnter += HandleHoverEnter;
                s.OnHoverExit += HandleHoverExit;

                // Make sure selection highlights are off by default.
                s.SetSelected(false);
            }

            _selectedSlot = null;
        }

        /// <summary>
        /// UI-only: select a slot.
        /// </summary>
        public void Select(EquipmentSlotId slotId)
        {
            _selectedSlot = slotId;

            if (highlightSelection)
            {
                foreach (var s in _slots)
                    s.SetSelected(s.SlotId == slotId);
            }

            OnSlotSelected?.Invoke(slotId);
        }

        /// <summary>
        /// UI-only: clear selection.
        /// </summary>
        public void ClearSelection()
        {
            _selectedSlot = null;

            if (highlightSelection)
            {
                foreach (var s in _slots)
                    s.SetSelected(false);
            }

            // Null selection signals "none selected".
            // If you prefer, you can add a separate event for selection cleared.
        }

        /// <summary>
        /// UI-only convenience: set an equipment icon for a slot.
        /// In the real wiring phase, a binder will call this.
        /// </summary>
        public void SetSlotIcon(EquipmentSlotId id, Sprite icon)
        {
            var view = FindView(id);
            if (view == null) return;

            if (icon == null)
                view.SetEmpty();
            else
                view.SetFilled(icon);
        }

        // ----------------------------
        // Internal handlers
        // ----------------------------

        private void HandleLeftClick(EquipmentSlotViewUI slot)
        {
            if (slot == null) return;
            Select(slot.SlotId);
        }

        private void HandleRightClick(EquipmentSlotViewUI slot)
        {
            if (slot == null) return;

            // UO feel: right click selects first.
            Select(slot.SlotId);
            OnSlotRightClicked?.Invoke(slot.SlotId);
        }

        private void HandleDoubleClick(EquipmentSlotViewUI slot)
        {
            if (slot == null) return;
            Select(slot.SlotId);
            OnSlotDoubleClicked?.Invoke(slot.SlotId);
        }

        private void HandleHoverEnter(EquipmentSlotViewUI slot)
        {
            if (slot == null) return;
            OnSlotHoverEnter?.Invoke(slot.SlotId);
        }

        private void HandleHoverExit(EquipmentSlotViewUI slot)
        {
            if (slot == null) return;
            OnSlotHoverExit?.Invoke(slot.SlotId);
        }

        private void AddIfNotNull(EquipmentSlotViewUI view, EquipmentSlotId id)
        {
            if (view == null) return;

            // Ensure the view has the correct id.
            view.SetSlotId(id);

            _slots.Add(view);
        }

        private EquipmentSlotViewUI FindView(EquipmentSlotId id)
        {
            foreach (var s in _slots)
            {
                if (s.SlotId == id)
                    return s;
            }
            return null;
        }
    }
}
