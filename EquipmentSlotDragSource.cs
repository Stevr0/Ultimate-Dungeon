// ============================================================================
// EquipmentSlotDragSource.cs (UI)
// ----------------------------------------------------------------------------
// Purpose:
// - Enables left-click drag FROM an equipment slot back to inventory.
// - Shows drag ghost and disables raycast blocking while dragging.
//
// Requirements:
// - EquipmentSlotViewUI exists and exposes:
//     EquipmentSlotId SlotId { get; }
//
// Setup:
// - Add this component to each equipment slot UI element
//   (same GameObject as EquipmentSlotViewUI).
// - Assign ItemDefCatalog (same asset used elsewhere).
//
// Notes:
// - This is UI-only. Actual unequip happens in PlayerEquipmentComponent via ServerRpc.
// ============================================================================

using UnityEngine;
using UnityEngine.EventSystems;
using UltimateDungeon.Items;

namespace UltimateDungeon.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EquipmentSlotViewUI))]
    public sealed class EquipmentSlotDragSource : MonoBehaviour,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        [Header("Required")]
        [SerializeField] private ItemDefCatalog itemDefCatalog;

        private EquipmentSlotViewUI _slotView;
        private CanvasGroup _canvasGroup;
        private bool _prevBlocksRaycasts;
        private float _prevAlpha;

        private PlayerEquipmentComponent _equipment;

        private void Awake()
        {
            _slotView = GetComponent<EquipmentSlotViewUI>();

            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData == null) return;
            if (eventData.button != PointerEventData.InputButton.Left) return;

            EnsureEquipment();
            if (_equipment == null)
                return;

            // Read current equipped item for this slot.
            var equipped = _equipment.GetEquippedForUI(_slotView.SlotId);
            if (equipped.IsEmpty)
                return; // Nothing equipped to drag.

            // Resolve icon sprite.
            Sprite icon = null;
            if (itemDefCatalog != null)
            {
                var id = equipped.itemDefId.ToString();
                if (itemDefCatalog.TryGet(id, out var def) && def != null)
                    icon = ItemIconResolver.Resolve(def);
            }

            UIInventoryDragContext.BeginEquipmentSlotDrag(_slotView.SlotId);

            if (UIDragGhost.Instance != null)
                UIDragGhost.Instance.Show(icon);

            // Allow drop targets to receive raycasts.
            _prevBlocksRaycasts = _canvasGroup.blocksRaycasts;
            _canvasGroup.blocksRaycasts = false;

            // Visual feedback: fade slot while dragging.
            _prevAlpha = _canvasGroup.alpha;
            _canvasGroup.alpha = 0.35f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Ghost follows cursor in UIDragGhost.Update().
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            // Restore visuals.
            if (_canvasGroup != null)
            {
                _canvasGroup.blocksRaycasts = _prevBlocksRaycasts;
                _canvasGroup.alpha = _prevAlpha;
            }

            if (UIDragGhost.Instance != null)
                UIDragGhost.Instance.Hide();

            UIInventoryDragContext.Clear();
        }

        private void EnsureEquipment()
        {
            if (_equipment != null)
                return;

            _equipment = FindFirstObjectByType<PlayerEquipmentComponent>();
        }
    }
}
