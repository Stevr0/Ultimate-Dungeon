using System.Collections.Generic;
using UltimateDungeon.Items;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace UltimateDungeon.UI
{
    /// <summary>
    /// CorpseLootWindowUI
    /// ==================
    /// Client-only corpse loot grid that mirrors inventory/container look-and-feel.
    ///
    /// Editor wiring checklist:
    /// 1) Put this on your corpse loot window root under Canvas_Windows.
    /// 2) Assign rootPanel (window root to show/hide).
    /// 3) Assign gridRoot (RectTransform with GridLayoutGroup/ContentSizeFitter as desired).
    /// 4) Assign slotPrefab (prefab with CorpseLootSlotUI + icon/stack visuals).
    /// 5) Assign itemDefCatalog so ItemDefId -> iconAddress -> sprite lookup works.
    /// 6) Optional: assign closeButton (wired to hide window).
    ///
    /// Data/authority flow:
    /// - Snapshot events populate this read-only grid.
    /// - Slot drags publish loot payload via UIInventoryDragContext.
    /// - Valid drop targets call CorpseLootInteractable.RequestTakeItemServerRpc(instanceId).
    /// - We intentionally do NOT move local inventory/corpse UI state on drop.
    ///   Server authority + replication/snapshot refresh drives final state.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CorpseLootWindowUI : MonoBehaviour
    {
        [Header("Required UI")]
        [SerializeField] private GameObject rootPanel;
        [SerializeField] private RectTransform gridRoot;
        [SerializeField] private CorpseLootSlotUI slotPrefab;

        [Header("Data")]
        [SerializeField] private ItemDefCatalog itemDefCatalog;

        [Header("Optional UI")]
        [SerializeField] private Button closeButton;

        private ulong _currentCorpseId;
        private bool _hasCorpseSelection;

        private readonly List<CorpseLootSnapshotEntry> _currentEntries = new();
        private readonly List<CorpseLootSlotUI> _spawnedSlots = new();

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(CloseWindowAndClearSelection);

            SetPanelVisible(false);
        }

        private void OnEnable()
        {
            CorpseLootInteractable.ClientLootSnapshotReceived += OnClientLootSnapshotReceived;
            CorpseLootInteractable.ClientTakeItemResultReceived += OnClientTakeItemResultReceived;
        }

        private void OnDisable()
        {
            CorpseLootInteractable.ClientLootSnapshotReceived -= OnClientLootSnapshotReceived;
            CorpseLootInteractable.ClientTakeItemResultReceived -= OnClientTakeItemResultReceived;
            CloseWindowAndClearSelection();
        }

        private void Update()
        {
            if (_hasCorpseSelection && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseWindowAndClearSelection();
                return;
            }

            if (_hasCorpseSelection && !DoesCorpseStillExist(_currentCorpseId))
                CloseWindowAndClearSelection();
        }

        private void OnClientLootSnapshotReceived(ulong corpseId, List<CorpseLootSnapshotEntry> entries)
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient)
                return;

            _currentCorpseId = corpseId;
            _hasCorpseSelection = true;

            _currentEntries.Clear();
            if (entries != null)
                _currentEntries.AddRange(entries);

            // Empty snapshot means nothing to loot, so close immediately.
            if (_currentEntries.Count == 0)
            {
                CloseWindowAndClearSelection();
                return;
            }

            SetPanelVisible(true);
            RebuildGrid();
        }

        private void OnClientTakeItemResultReceived(ulong corpseId, LootTakeResultCode code)
        {
            if (!_hasCorpseSelection || corpseId != _currentCorpseId)
                return;

            // Refresh is snapshot-driven; this callback is informational only.
            Debug.Log($"[LootUI] Take result corpse={corpseId} code={code}");
        }

        private void RebuildGrid()
        {
            ClearSlots();

            if (gridRoot == null || slotPrefab == null)
                return;

            for (int i = 0; i < _currentEntries.Count; i++)
            {
                var entry = _currentEntries[i];

                var slot = Instantiate(slotPrefab, gridRoot);
                slot.name = $"LootSlot_{i:00}_{entry.ItemDefId}";

                // Snapshot -> ItemDef lookup -> iconAddress -> sprite.
                // We rely on ItemIconResolver to load from Resources by def.iconAddress.
                Sprite icon = ResolveIcon(entry);

                // Slot binds source corpse+instance metadata for drag payload.
                slot.Bind(_currentCorpseId, entry, icon);
                _spawnedSlots.Add(slot);
            }
        }

        private Sprite ResolveIcon(CorpseLootSnapshotEntry entry)
        {
            if (itemDefCatalog == null)
                return null;

            if (!itemDefCatalog.TryGet(entry.ItemDefId.ToString(), out var def) || def == null)
                return null;

            return ItemIconResolver.Resolve(def);
        }

        private bool DoesCorpseStillExist(ulong corpseId)
        {
            if (NetworkManager.Singleton == null || NetworkManager.Singleton.SpawnManager == null)
                return false;

            return NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(corpseId, out var netObj) && netObj != null;
        }

        private void CloseWindowAndClearSelection()
        {
            _hasCorpseSelection = false;
            _currentCorpseId = 0;
            _currentEntries.Clear();
            ClearSlots();
            SetPanelVisible(false);
        }

        private void SetPanelVisible(bool visible)
        {
            if (rootPanel != null)
                rootPanel.SetActive(visible);
        }

        private void ClearSlots()
        {
            for (int i = 0; i < _spawnedSlots.Count; i++)
            {
                if (_spawnedSlots[i] != null)
                    Destroy(_spawnedSlots[i].gameObject);
            }

            _spawnedSlots.Clear();
        }
    }
}
