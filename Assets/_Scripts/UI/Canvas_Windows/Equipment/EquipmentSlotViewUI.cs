using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UltimateDungeon.UI
{
    /// <summary>
    /// EquipmentSlotViewUI (UI-ONLY VIEW)
    /// =================================
    ///
    /// Represents ONE equipment slot in the Inventory window.
    ///
    /// SETUP PHASE (now):
    /// - Visual state: empty vs filled
    /// - Selection highlight
    /// - Pointer events: left click, double click, right click, hover
    /// - Slot label (optional)
    /// - No gameplay wiring
    ///
    /// WIRING PHASE (later):
    /// - Bind to an equipped item model
    /// - Enforce equip rules elsewhere (server-authoritative)
    /// - Support drag/drop between equipment and inventory grid
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EquipmentSlotViewUI : MonoBehaviour,
        IPointerClickHandler,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        // ----------------------------
        // Events (UI-only)
        // ----------------------------
        public event Action<EquipmentSlotViewUI> OnLeftClick;
        public event Action<EquipmentSlotViewUI> OnRightClick;
        public event Action<EquipmentSlotViewUI> OnDoubleClick;
        public event Action<EquipmentSlotViewUI> OnHoverEnter;
        public event Action<EquipmentSlotViewUI> OnHoverExit;

        [Header("Identity")]
        [SerializeField] private EquipmentSlotId slotId;

        [Header("Visual References")]
        [Tooltip("Optional background image for the slot.")]
        [SerializeField] private Image background;

        [Tooltip("Item icon. Disabled when empty.")]
        [SerializeField] private Image icon;

        [Tooltip("Selection highlight overlay (Image). Enabled when selected.")]
        [SerializeField] private Image selectionHighlight;

        [Tooltip("Optional text label shown on the slot (e.g., 'Head', 'Bag').")]
        [SerializeField] private TextMeshProUGUI slotLabel;

        // ----------------------------
        // Public API
        // ----------------------------
        public EquipmentSlotId SlotId => slotId;

        public void SetSlotId(EquipmentSlotId id)
        {
            slotId = id;
            UpdateLabelFromId();
        }

        public void SetSelected(bool selected)
        {
            if (selectionHighlight != null)
                selectionHighlight.enabled = selected;
        }

        public void SetEmpty()
        {
            if (icon != null)
                icon.enabled = false;
        }

        public void SetFilled(Sprite itemIcon)
        {
            if (icon == null) return;

            icon.sprite = itemIcon;
            icon.enabled = (itemIcon != null);
        }

        // ----------------------------
        // Pointer events
        // ----------------------------
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData == null) return;

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                if (eventData.clickCount >= 2)
                    OnDoubleClick?.Invoke(this);
                else
                    OnLeftClick?.Invoke(this);
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                OnRightClick?.Invoke(this);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            OnHoverEnter?.Invoke(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            OnHoverExit?.Invoke(this);
        }

        private void Awake()
        {
            // Setup-time nicety: show labels automatically if provided.
            UpdateLabelFromId();

            // Selection highlight should start off.
            if (selectionHighlight != null)
                selectionHighlight.enabled = false;
        }

        private void UpdateLabelFromId()
        {
            if (slotLabel == null) return;

            // Keep it readable.
            slotLabel.text = slotId switch
            {
                EquipmentSlotId.Mainhand => "Main\nHand",
                EquipmentSlotId.Offhand => "Off-Hand",
                _ => slotId.ToString()
            };
        }
    }
}
