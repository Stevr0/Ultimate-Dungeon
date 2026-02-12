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
    /// Key behavior:
    /// - Read-only source (we only drag OUT from corpse loot).
    /// - Never receives/accepts drops itself.
    /// - Creates a visual drag ghost while dragging.
    /// - Temporarily disables raycast blocking on both:
    ///   1) The source slot (so inventory drop targets can receive OnDrop)
    ///   2) The ghost object (so it never intercepts raycasts)
    ///
    /// Editor wiring checklist:
    /// 1) Add this script to your corpse loot slot prefab.
    /// 2) Assign Icon Image and Stack Count TMP Text.
    /// 3) Assign Drag Ghost Prefab:
    ///    - Should be a UI prefab (RectTransform) with an Image for icon display.
    ///    - Add a CanvasGroup on the prefab root (recommended).
    ///    - Set all Image/TMP components on the ghost to Raycast Target = false.
    /// 4) Ensure the slot GameObject has a CanvasGroup.
    ///    - If missing, this script auto-adds one in Awake.
    ///
    /// How this integrates with InventorySlotDropTarget:
    /// - OnBeginDrag publishes loot payload into UIInventoryDragContext.
    /// - InventorySlotDropTarget.OnDrop reads that payload and sends the
    ///   server-authoritative loot-take request.
    /// - We do not mutate inventory/corpse state locally in this slot script.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CorpseLootSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Slot UI")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text stackCountText;

        [Header("Drag Ghost")]
        [Tooltip("UI prefab instantiated during drag. Should contain an Image used as icon display.")]
        [SerializeField] private GameObject dragGhostPrefab;

        private CanvasGroup _slotCanvasGroup;
        private bool _slotPrevBlocksRaycasts;

        private ulong _corpseNetId;
        private CorpseLootSnapshotEntry _entry;
        private Sprite _icon;

        // Runtime drag ghost state
        private RectTransform _ghostRect;
        private Image _ghostIconImage;
        private CanvasGroup _ghostCanvasGroup;
        private bool _ghostPrevBlocksRaycasts;
        private Canvas _rootCanvas;

        private void Awake()
        {
            _slotCanvasGroup = GetComponent<CanvasGroup>();
            if (_slotCanvasGroup == null)
            {
                // Required so we can toggle blocksRaycasts during drag.
                _slotCanvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            // Root canvas is used to parent the runtime ghost and convert pointer coords.
            _rootCanvas = GetComponentInParent<Canvas>();
            if (_rootCanvas != null)
                _rootCanvas = _rootCanvas.rootCanvas;
        }

        /// <summary>
        /// Binds this slot to a specific corpse snapshot entry.
        /// Called by CorpseLootWindowUI when rebuilding the corpse grid.
        /// </summary>
        public void Bind(ulong corpseNetId, CorpseLootSnapshotEntry entry, Sprite icon)
        {
            _corpseNetId = corpseNetId;
            _entry = entry;
            _icon = icon;

            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
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
            if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
                return;

            if (_corpseNetId == 0 || _entry.InstanceId.Length == 0)
                return;

            // Required drag context call for corpse loot entries.
            UIInventoryDragContext.BeginLootEntry(
                sourceCorpseNetId: _corpseNetId,
                instanceId: _entry.InstanceId.ToString(),
                itemDefId: _entry.ItemDefId.ToString());

            CreateAndShowGhost(eventData);

            // Critical: disable source-slot raycast blocking while dragging so
            // InventorySlotDropTarget can be hit.
            _slotPrevBlocksRaycasts = _slotCanvasGroup.blocksRaycasts;
            _slotCanvasGroup.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            UpdateGhostPosition(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            // Restore slot raycast behavior.
            if (_slotCanvasGroup != null)
                _slotCanvasGroup.blocksRaycasts = _slotPrevBlocksRaycasts;

            // Restore ghost raycast behavior before destroy (good hygiene if pooled later).
            if (_ghostCanvasGroup != null)
                _ghostCanvasGroup.blocksRaycasts = _ghostPrevBlocksRaycasts;

            DestroyGhost();

            // Only clear if drop did not land on a valid inventory drop target.
            // If accepted, InventorySlotDropTarget has already consumed context in OnDrop.
            if (!WasDropAccepted(eventData))
                UIInventoryDragContext.Clear();
        }

        // Minimal hover hooks (intentional stub for tooltip systems).
        public void OnPointerEnterHint() { }
        public void OnPointerExitHint() { }

        private void CreateAndShowGhost(PointerEventData eventData)
        {
            DestroyGhost();

            if (dragGhostPrefab == null)
                return;

            Transform parent = _rootCanvas != null ? _rootCanvas.transform : transform.root;
            var ghostGo = Instantiate(dragGhostPrefab, parent);
            ghostGo.name = $"{name}_DragGhost";

            _ghostRect = ghostGo.transform as RectTransform;
            _ghostIconImage = ghostGo.GetComponentInChildren<Image>(includeInactive: true);
            if (_ghostIconImage != null)
                _ghostIconImage.sprite = _icon;

            _ghostCanvasGroup = ghostGo.GetComponent<CanvasGroup>();
            if (_ghostCanvasGroup == null)
                _ghostCanvasGroup = ghostGo.AddComponent<CanvasGroup>();

            _ghostPrevBlocksRaycasts = _ghostCanvasGroup.blocksRaycasts;
            _ghostCanvasGroup.blocksRaycasts = false;

            UpdateGhostPosition(eventData);
        }

        private void UpdateGhostPosition(PointerEventData eventData)
        {
            if (_ghostRect == null || eventData == null)
                return;

            Vector2 screenPos = eventData.position;

            if (_rootCanvas != null && _rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rootCanvas.transform as RectTransform,
                    screenPos,
                    _rootCanvas.worldCamera,
                    out Vector2 localPos);

                _ghostRect.anchoredPosition = localPos;
            }
            else
            {
                _ghostRect.position = screenPos;
            }
        }

        private void DestroyGhost()
        {
            if (_ghostRect != null)
                Destroy(_ghostRect.gameObject);

            _ghostRect = null;
            _ghostIconImage = null;
            _ghostCanvasGroup = null;
        }

        private static bool WasDropAccepted(PointerEventData eventData)
        {
            if (eventData == null)
                return false;

            // If pointer is over an inventory drop target at end-drag, treat as accepted.
            var hovered = eventData.pointerCurrentRaycast.gameObject;
            if (hovered == null)
                hovered = eventData.pointerEnter;

            if (hovered == null)
                return false;

            return hovered.GetComponentInParent<InventorySlotDropTarget>() != null;
        }
    }
}
