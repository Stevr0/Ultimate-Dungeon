// ============================================================================
// InventoryGridViewUI.cs
// ----------------------------------------------------------------------------
// UI-only controller for a grid of InventorySlotViewUI.
//
// This version matches the refactored InventorySlotViewUI:
// - Uses SetIndex()
// - Subscribes to slot events (left/right/double/hover)
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateDungeon.UI
{
    [DisallowMultipleComponent]
    public sealed class InventoryGridViewUI : MonoBehaviour
    {
        // UI-only events
        public event Action<int> OnSlotSelected;
        public event Action<int> OnSlotRightClicked;
        public event Action<int> OnSlotDoubleClicked;
        public event Action<int> OnSlotHoverEnter;
        public event Action<int> OnSlotHoverExit;

        [Header("Required")]
        [SerializeField] private InventoryGridLayoutUI gridLayout;
        [SerializeField] private RectTransform slotPrefab;

        [Header("Setup Only")]
        [SerializeField] private int slotCount = 40;
        [SerializeField] private bool buildOnAwake = true;

        [Header("Selection")]
        [SerializeField] private bool highlightSelection = true;

        private readonly List<InventorySlotViewUI> _slots = new();
        private int _selectedIndex = -1;

        public int SelectedIndex => _selectedIndex;

        private void Awake()
        {
            if (gridLayout == null)
                gridLayout = GetComponentInChildren<InventoryGridLayoutUI>(includeInactive: true);

            if (buildOnAwake)
                Build();
        }

        public void Build()
        {
            if (gridLayout == null)
            {
                Debug.LogError("[InventoryGridViewUI] Missing InventoryGridLayoutUI reference.");
                return;
            }

            if (slotPrefab == null)
            {
                Debug.LogError("[InventoryGridViewUI] Missing slotPrefab reference.");
                return;
            }

            var parent = gridLayout.GetSlotParent();
            if (parent == null)
            {
                Debug.LogError("[InventoryGridViewUI] Grid layout returned null slot parent (Content).");
                return;
            }

            ClearExisting(parent);
            _slots.Clear();
            _selectedIndex = -1;

            slotCount = Mathf.Clamp(slotCount, 0, 300);

            for (int i = 0; i < slotCount; i++)
            {
                var rt = Instantiate(slotPrefab, parent);
                rt.name = $"Slot_{i:00}";

                var view = rt.GetComponent<InventorySlotViewUI>();
                if (view == null)
                {
                    Debug.LogError($"[InventoryGridViewUI] Slot prefab '{slotPrefab.name}' is missing InventorySlotViewUI.");
                    Destroy(rt.gameObject);
                    continue;
                }

                view.SetIndex(i);
                view.SetSelected(false);
                view.SetEmpty();

                view.OnLeftClick += HandleLeftClick;
                view.OnRightClick += HandleRightClick;
                view.OnDoubleClick += HandleDoubleClick;
                view.OnHoverEnter += HandleHoverEnter;
                view.OnHoverExit += HandleHoverExit;

                _slots.Add(view);
            }
        }

        public void Select(int index)
        {
            if (index < 0 || index >= _slots.Count)
                return;

            _selectedIndex = index;

            if (highlightSelection)
            {
                for (int i = 0; i < _slots.Count; i++)
                    _slots[i].SetSelected(i == _selectedIndex);
            }

            OnSlotSelected?.Invoke(_selectedIndex);
        }

        public void ClearSelection()
        {
            _selectedIndex = -1;

            if (highlightSelection)
            {
                for (int i = 0; i < _slots.Count; i++)
                    _slots[i].SetSelected(false);
            }

            OnSlotSelected?.Invoke(_selectedIndex);
        }

        // ---- Slot event handlers ----

        private void HandleLeftClick(InventorySlotViewUI slot)
        {
            if (slot == null) return;
            Select(slot.SlotIndex);
        }

        private void HandleRightClick(InventorySlotViewUI slot)
        {
            if (slot == null) return;
            Select(slot.SlotIndex);
            OnSlotRightClicked?.Invoke(slot.SlotIndex);
        }

        private void HandleDoubleClick(InventorySlotViewUI slot)
        {
            if (slot == null) return;
            Select(slot.SlotIndex);
            OnSlotDoubleClicked?.Invoke(slot.SlotIndex);
        }

        private void HandleHoverEnter(InventorySlotViewUI slot)
        {
            if (slot == null) return;
            OnSlotHoverEnter?.Invoke(slot.SlotIndex);
        }

        private void HandleHoverExit(InventorySlotViewUI slot)
        {
            if (slot == null) return;
            OnSlotHoverExit?.Invoke(slot.SlotIndex);
        }

        private static void ClearExisting(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (child != null)
                    Destroy(child.gameObject);
            }
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
