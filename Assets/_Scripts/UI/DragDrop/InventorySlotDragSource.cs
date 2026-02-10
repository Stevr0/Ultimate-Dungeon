// ============================================================================
// InventorySlotDragSource.cs (UI) — v2
// ----------------------------------------------------------------------------
// Fixes common Unity drag/drop issue:
// - If the dragged UI element continues to block raycasts, the drop target
//   never receives OnDrop().
//
// Solution:
// - Add / use a CanvasGroup and set blocksRaycasts = false while dragging.
//
// Also:
// - Compatible with the new Input System (drag handlers are UGUI-driven).
// ============================================================================

using UnityEngine;
using UnityEngine.EventSystems;
using UltimateDungeon.Items;

namespace UltimateDungeon.UI
{
    [DisallowMultipleComponent]
    public sealed class InventorySlotDragSource : MonoBehaviour,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        [Header("Required")]
        [SerializeField] private ItemDefCatalog itemDefCatalog;

        private InventorySlotViewUI _slotView;
        private PlayerInventoryComponent _playerInventory;

        // This is the key for drop targets: while dragging, we must NOT block raycasts.
        private CanvasGroup _canvasGroup;
        private bool _prevBlocksRaycasts;

        private void Awake()
        {
            _slotView = GetComponent<InventorySlotViewUI>();
            if (_slotView == null)
                Debug.LogError("[InventorySlotDragSource] Missing InventorySlotViewUI on the same GameObject.", this);

            // Add a CanvasGroup if none exists so we can toggle raycast blocking.
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData == null) return;
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (_slotView == null) return;

            // Ensure local inventory model.
            EnsureLocalInventory();
            if (_playerInventory == null || _playerInventory.Inventory == null)
                return;

            int slotIndex = _slotView.SlotIndex;
            var slot = _playerInventory.Inventory.GetSlot(slotIndex);

            // No item? Nothing to drag.
            if (slot.IsEmpty || slot.item == null)
                return;

            // Resolve icon for drag ghost.
            Sprite icon = null;
            if (itemDefCatalog != null && itemDefCatalog.TryGet(slot.item.itemDefId, out var def))
                icon = ItemIconResolver.Resolve(def);

            // Begin drag context.
            UIInventoryDragContext.BeginInventorySlotDrag(slotIndex);

            // Show drag ghost.
            if (UIDragGhost.Instance != null)
                UIDragGhost.Instance.Show(icon);

            // *** CRITICAL ***
            // Allow drop targets to receive raycasts while we drag.
            _prevBlocksRaycasts = _canvasGroup.blocksRaycasts;
            _canvasGroup.blocksRaycasts = false;

            Debug.Log($"[Drag] BeginDrag slot={slotIndex} item={slot.item.itemDefId}", this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            // No per-frame work needed.
            // UIDragGhost follows the cursor in its Update().
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            // Restore raycast blocking.
            if (_canvasGroup != null)
                _canvasGroup.blocksRaycasts = _prevBlocksRaycasts;

            // End drag visual + context.
            if (UIDragGhost.Instance != null)
                UIDragGhost.Instance.Hide();

            UIInventoryDragContext.Clear();

            Debug.Log("[Drag] EndDrag", this);
        }

        private void EnsureLocalInventory()
        {
            if (_playerInventory != null)
                return;

            // MVP: find the first PlayerInventoryComponent (local player).
            // If you have multiple players in-scene, we will switch this to
            // a proper local-player binder just like InventoryUIBinder.
            _playerInventory = FindFirstObjectByType<PlayerInventoryComponent>();
        }
    }
}
