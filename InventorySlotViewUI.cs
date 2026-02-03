// ============================================================================
// InventorySlotViewUI.cs
// ----------------------------------------------------------------------------
// A single inventory slot view.
//
// Fixes:
// - Restores API that InventoryUIBinder expects: SlotIndex, SetEmpty, SetFilled
// - Restores API that InventoryGridViewUI expects: SetIndex + click/hover events
// - Removes dependency on missing InventoryUIEvents (slot emits events itself)
// ============================================================================

using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UltimateDungeon.UI
{
    [DisallowMultipleComponent]
    public sealed class InventorySlotViewUI : MonoBehaviour,
        IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        // --------------------------------------------------------------------
        // Events (UI-only)
        // InventoryGridViewUI subscribes to these.
        // --------------------------------------------------------------------

        public event Action<InventorySlotViewUI> OnLeftClick;
        public event Action<InventorySlotViewUI> OnRightClick;
        public event Action<InventorySlotViewUI> OnDoubleClick;
        public event Action<InventorySlotViewUI> OnHoverEnter;
        public event Action<InventorySlotViewUI> OnHoverExit;

        [Header("Identity")]
        [SerializeField] private int slotIndex;

        [Header("Visual References")]
        [SerializeField] private Image background;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text stackCountText;
        [SerializeField] private Image selectionHighlight;

        [Header("Debug (Optional)")]
        [SerializeField] private TMP_Text debugIndexText;

        /// <summary>
        /// Slot index used by InventoryUIBinder to map UI → model.
        /// </summary>
        public int SlotIndex => slotIndex;

        // Used to detect double-click. (UO style)
        [Header("Input")]
        [SerializeField] private float doubleClickSeconds = 0.30f;
        private float _lastClickTime = -999f;

        private void Awake()
        {
            // Safety auto-wire common child names if you forgot to assign refs.
            if (icon == null) icon = transform.Find("Icon")?.GetComponent<Image>();
            if (stackCountText == null) stackCountText = transform.Find("StackCount")?.GetComponent<TMP_Text>();
            if (selectionHighlight == null) selectionHighlight = transform.Find("SelectionHighlight")?.GetComponent<Image>();
        }

        // --------------------------------------------------------------------
        // Identity
        // --------------------------------------------------------------------

        /// <summary>
        /// InventoryGridViewUI calls this when spawning slots.
        /// </summary>
        public void SetIndex(int index)
        {
            slotIndex = index;

            if (debugIndexText != null)
                debugIndexText.text = index.ToString();
        }

        /// <summary>
        /// Backwards-compatible alias if older code used SetSlotIdentity.
        /// </summary>
        public void SetSlotIdentity(int index) => SetIndex(index);

        // --------------------------------------------------------------------
        // Visuals
        // --------------------------------------------------------------------

        public void SetEmpty()
        {
            if (icon != null)
            {
                icon.sprite = null;
                icon.enabled = false;
            }

            if (stackCountText != null)
            {
                stackCountText.text = string.Empty;
                stackCountText.enabled = false;
            }

            if (selectionHighlight != null)
                selectionHighlight.enabled = false;
        }

        /// <summary>
        /// Sets visuals for a filled slot.
        ///
        /// NOTE:
        /// - itemIcon can be null in v1 (icon loading not implemented).
        /// - If icon is null, we show the stack count even when it is 1,
        ///   so you can still SEE that the slot has something.
        /// </summary>
        public void SetFilled(Sprite itemIcon, int stackCount)
        {
            stackCount = Mathf.Max(1, stackCount);

            if (icon != null)
            {
                icon.sprite = itemIcon;
                icon.enabled = itemIcon != null;
            }

            if (stackCountText != null)
            {
                bool showCount = (stackCount > 1) || (itemIcon == null);
                stackCountText.enabled = showCount;
                stackCountText.text = showCount ? stackCount.ToString() : string.Empty;
            }
        }

        public void SetSelected(bool isSelected)
        {
            if (selectionHighlight != null)
                selectionHighlight.enabled = isSelected;
        }

        // --------------------------------------------------------------------
        // Pointer events
        // --------------------------------------------------------------------

        public void OnPointerEnter(PointerEventData eventData)
        {
            OnHoverEnter?.Invoke(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            OnHoverExit?.Invoke(this);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData == null) return;

            // Double click detection (left button only)
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                float now = Time.unscaledTime;
                bool isDouble = (now - _lastClickTime) <= Mathf.Max(0.05f, doubleClickSeconds);
                _lastClickTime = now;

                if (isDouble)
                {
                    OnDoubleClick?.Invoke(this);
                    return;
                }

                OnLeftClick?.Invoke(this);
                return;
            }

            if (eventData.button == PointerEventData.InputButton.Right)
            {
                OnRightClick?.Invoke(this);
                return;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (slotIndex < 0) slotIndex = 0;
            if (doubleClickSeconds < 0.05f) doubleClickSeconds = 0.05f;
        }
#endif
    }
}
