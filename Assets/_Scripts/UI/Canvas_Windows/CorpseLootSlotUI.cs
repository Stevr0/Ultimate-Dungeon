using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UltimateDungeon.UI
{
    /// <summary>
    /// CorpseLootSlotUI
    /// ----------------
    /// Read-only corpse loot slot view that supports drag-out only.
    ///
    /// Behavior:
    /// - Shows icon + optional stack count.
    /// - Starts a drag payload via UIInventoryDragContext as DragKind.LootEntry.
    /// - Never accepts drops itself (corpse is take-only; no put/reorder).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CorpseLootSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("UI")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text stackText; // optional

        private CanvasGroup _canvasGroup;
        private bool _prevBlocksRaycasts;
        private float _prevAlpha;

        private ulong _sourceCorpseId;
        private FixedString64Bytes _instanceId;
        private string _itemDefId;
        private Sprite _icon;
        private int _stackCount;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        /// <summary>
        /// Binds snapshot data to this slot.
        /// Called by CorpseLootWindowUI when rebuilding its loot grid.
        /// </summary>
        public void Bind(ulong sourceCorpseId, CorpseLootSnapshotEntry entry, Sprite icon)
        {
            _sourceCorpseId = sourceCorpseId;
            _instanceId = entry.InstanceId;
            _itemDefId = entry.ItemDefId.ToString();
            _icon = icon;
            _stackCount = Mathf.Max(1, entry.StackCount);

            SetVisuals(_icon, _stackCount, showStackWhenOne: _icon == null);
        }

        private void SetVisuals(Sprite icon, int stackCount, bool showStackWhenOne)
        {
            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }

            if (stackText != null)
            {
                bool show = stackCount > 1 || showStackWhenOne;
                stackText.gameObject.SetActive(show);
                if (show)
                    stackText.text = stackCount.ToString();
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
                return;

            if (_sourceCorpseId == 0 || _instanceId.Length == 0)
                return;

            // Publish loot drag payload for existing inventory drop targets.
            UIInventoryDragContext.BeginLootEntryDrag(_sourceCorpseId, _instanceId.ToString(), _itemDefId);

            if (UIDragGhost.Instance != null)
                UIDragGhost.Instance.Show(_icon);

            _prevBlocksRaycasts = _canvasGroup.blocksRaycasts;
            _canvasGroup.blocksRaycasts = false;

            _prevAlpha = _canvasGroup.alpha;
            _canvasGroup.alpha = 0.35f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Drag ghost follows pointer in UIDragGhost.Update().
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.blocksRaycasts = _prevBlocksRaycasts;
                _canvasGroup.alpha = _prevAlpha;
            }

            if (UIDragGhost.Instance != null)
                UIDragGhost.Instance.Hide();

            UIInventoryDragContext.Clear();
        }
    }
}
