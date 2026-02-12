using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UltimateDungeon.UI
{
    /// <summary>
    /// CorpseLootSlotUI
    /// -----------------------------------------------------------------------
    /// Drag source for ONE corpse-loot slot.
    ///
    /// Uses the shared/global UIDragGhost (same as inventory/equipment) so we
    /// don't need to spawn per-slot ghost prefabs.
    ///
    /// Important:
    /// - This is a READ-ONLY loot source. We only drag OUT to request a take.
    /// - We DO NOT accept drops on corpse slots.
    /// - While dragging, we disable this slot's raycasts so underlying drop
    ///   targets (inventory slots) can receive OnDrop.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CorpseLootSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Slot UI")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text stackCountText;

        private CanvasGroup _slotCanvasGroup;
        private bool _slotPrevBlocksRaycasts;

        private ulong _corpseNetId;
        private CorpseLootSnapshotEntry _entry;
        private Sprite _icon;

        private void Awake()
        {
            // We need a CanvasGroup so we can toggle blocksRaycasts during drag.
            _slotCanvasGroup = GetComponent<CanvasGroup>();
            if (_slotCanvasGroup == null)
                _slotCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        /// <summary>
        /// Binds this slot to a snapshot entry (called by CorpseLootWindowUI).
        /// </summary>
        public void Bind(ulong corpseNetId, CorpseLootSnapshotEntry entry, Sprite icon)
        {
            _corpseNetId = corpseNetId;
            _entry = entry;
            _icon = icon;

            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = (icon != null);
            }

            if (stackCountText != null)
            {
                int stackCount = Mathf.Max(1, entry.StackCount);
                stackCountText.text = stackCount.ToString();
                stackCountText.gameObject.SetActive(stackCount > 1);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            // Only left-drag
            if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
                return;

            // Must have valid loot metadata
            if (_corpseNetId == 0 || _entry.InstanceId.Length == 0)
                return;

            // Publish the loot drag payload (InventorySlotDropTarget reads this)
            UIInventoryDragContext.BeginLootEntry(
                sourceCorpseNetId: _corpseNetId,
                instanceId: _entry.InstanceId.ToString(),
                itemDefId: _entry.ItemDefId.ToString());

            // Show shared/global ghost (MUST exist in scene)
            if (UIDragGhost.Instance != null)
                UIDragGhost.Instance.Show(_icon);
            else
                Debug.LogWarning("[CorpseLootSlotUI] UIDragGhost.Instance is null (no drag ghost in scene).");

            // Critical: let drop targets receive raycasts while dragging
            _slotPrevBlocksRaycasts = _slotCanvasGroup.blocksRaycasts;
            _slotCanvasGroup.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Nothing needed here when using UIDragGhost (it follows cursor itself).
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            // Restore raycast behavior on the source slot
            if (_slotCanvasGroup != null)
                _slotCanvasGroup.blocksRaycasts = _slotPrevBlocksRaycasts;

            // Hide shared/global ghost
            if (UIDragGhost.Instance != null)
                UIDragGhost.Instance.Hide();

            // If we didn't drop on an InventorySlotDropTarget, clear drag context.
            // If we DID drop successfully, OnDrop will have consumed it.
            if (!WasDropAccepted(eventData))
                UIInventoryDragContext.Clear();
        }

        private static bool WasDropAccepted(PointerEventData eventData)
        {
            if (eventData == null)
                return false;

            var hovered = eventData.pointerCurrentRaycast.gameObject;
            if (hovered == null)
                hovered = eventData.pointerEnter;

            if (hovered == null)
                return false;

            return hovered.GetComponentInParent<InventorySlotDropTarget>() != null;
        }
    }
}
