using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace UltimateDungeon.UI
{
    /// <summary>
    /// CorpseLootWindowUI
    /// ==================
    ///
    /// Minimal client-side loot window for corpse interaction.
    ///
    /// What this script does:
    /// - Listens for loot snapshot events raised by CorpseLootInteractable.ClientRpc calls.
    /// - Opens a small panel and shows one clickable row per item.
    /// - Clicking a row sends RequestTakeItemServerRpc(instanceId) to the correct corpse NetworkObject.
    /// - Closes automatically if the loot list becomes empty or the corpse despawns.
    /// - Allows manual closing through a close button or the ESC key.
    ///
    /// Quick scene/prefab setup checklist:
    /// 1) Add this component to a UI GameObject under your Canvas_Windows hierarchy.
    /// 2) Assign rootPanel to the panel root that should be shown/hidden.
    /// 3) Assign listRoot to a child RectTransform with a VerticalLayoutGroup.
    /// 4) Create a row prefab with a Button + TMP_Text (on root or child), then assign to rowPrefab.
    /// 5) Optional: assign closeButton.
    ///
    /// Notes:
    /// - This script intentionally does not touch inventory UI. It only sends "take item" requests.
    /// - On successful take, CorpseLootInteractable sends a fresh snapshot, which naturally refreshes this window.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CorpseLootWindowUI : MonoBehaviour
    {
        [Header("Required UI")]
        [Tooltip("Panel root that is enabled/disabled when loot is available.")]
        [SerializeField] private GameObject rootPanel;

        [Tooltip("Container for instantiated rows (typically has VerticalLayoutGroup).")]
        [SerializeField] private RectTransform listRoot;

        [Tooltip("Button prefab used for each loot row. Should include TMP_Text on self or children.")]
        [SerializeField] private Button rowPrefab;

        [Header("Optional UI")]
        [Tooltip("Optional close button that hides the panel.")]
        [SerializeField] private Button closeButton;

        // Tracks the currently displayed corpse network object id.
        // Value is only meaningful while _hasCorpseSelection is true.
        private ulong _currentCorpseId;
        private bool _hasCorpseSelection;

        // Cache of latest loot entries for the selected corpse.
        // This is useful if later we want sorting/filtering without requerying the server.
        private readonly List<CorpseLootSnapshotEntry> _currentEntries = new();

        // Keep references to spawned row instances so we can cleanly rebuild.
        private readonly List<Button> _spawnedRows = new();

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(CloseWindowAndClearSelection);

            // Start hidden to avoid stale content appearing before first snapshot arrives.
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

            // If this component is disabled, hide UI and release references.
            CloseWindowAndClearSelection();
        }

        private void Update()
        {
            // ESC closes the loot window for convenience.
            if (_hasCorpseSelection && Input.GetKeyDown(KeyCode.Escape))
            {
                CloseWindowAndClearSelection();
                return;
            }

            // If current corpse no longer exists in SpawnManager (despawned), close the panel.
            if (_hasCorpseSelection && !DoesCorpseStillExist(_currentCorpseId))
                CloseWindowAndClearSelection();
        }

        private void OnClientLootSnapshotReceived(ulong corpseId, List<CorpseLootSnapshotEntry> entries)
        {
            // This callback is client-facing by design because it is raised from a ClientRpc.
            // We still defensively guard against running with no client network context.
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient)
                return;

            _currentCorpseId = corpseId;
            _hasCorpseSelection = true;

            _currentEntries.Clear();
            if (entries != null)
                _currentEntries.AddRange(entries);

            Debug.Log($"[LootUI] Snapshot received corpse={corpseId} entries={_currentEntries.Count}");

            // When empty, close immediately. Server may despawn corpse right after this snapshot.
            if (_currentEntries.Count == 0)
            {
                CloseWindowAndClearSelection();
                return;
            }

            SetPanelVisible(true);
            RebuildListRows();
        }

        private void OnClientTakeItemResultReceived(ulong corpseId, LootTakeResultCode code)
        {
            // Keep logs verbose while learning/wiring this flow.
            Debug.Log($"[LootUI] Take result corpse={corpseId} code={code}");

            // Success path is intentionally passive.
            // CorpseLootInteractable already sends a fresh snapshot after successful take,
            // so this UI will refresh itself when the next snapshot event arrives.
        }

        private void RebuildListRows()
        {
            ClearRows();

            if (listRoot == null)
            {
                Debug.LogWarning("[LootUI] listRoot is not assigned.");
                return;
            }

            if (rowPrefab == null)
            {
                Debug.LogWarning("[LootUI] rowPrefab is not assigned.");
                return;
            }

            for (int i = 0; i < _currentEntries.Count; i++)
            {
                var entry = _currentEntries[i];
                var row = Instantiate(rowPrefab, listRoot);
                row.name = $"LootRow_{i:00}_{entry.DisplayName}";

                // Build a simple label: "DisplayName - Affix Summary" when affixes exist.
                string rowText = BuildRowText(entry);
                var text = row.GetComponent<TMP_Text>();
                if (text == null)
                    text = row.GetComponentInChildren<TMP_Text>(includeInactive: true);

                if (text != null)
                    text.text = rowText;

                // Capture the instance id value for this specific row button.
                FixedString64Bytes instanceId = entry.InstanceId;
                row.onClick.AddListener(() => TakeItem(instanceId));

                _spawnedRows.Add(row);
            }
        }

        private static string BuildRowText(CorpseLootSnapshotEntry entry)
        {
            string display = entry.DisplayName.ToString();
            if (string.IsNullOrWhiteSpace(display))
                display = entry.ItemDefId.ToString();

            string affixSummary = entry.AffixSummary.ToString();
            if (string.IsNullOrWhiteSpace(affixSummary))
                return display;

            return $"{display} ({affixSummary})";
        }

        private void TakeItem(FixedString64Bytes instanceId)
        {
            if (!_hasCorpseSelection)
            {
                Debug.LogWarning("[LootUI] Cannot take item: no corpse selected.");
                return;
            }

            if (NetworkManager.Singleton == null)
            {
                Debug.LogWarning("[LootUI] Cannot take item: NetworkManager.Singleton is null.");
                return;
            }

            var spawnManager = NetworkManager.Singleton.SpawnManager;
            if (spawnManager == null)
            {
                Debug.LogWarning("[LootUI] Cannot take item: SpawnManager is null.");
                CloseWindowAndClearSelection();
                return;
            }

            if (!spawnManager.SpawnedObjects.TryGetValue(_currentCorpseId, out var netObj) || netObj == null)
            {
                Debug.LogWarning($"[LootUI] Cannot take item: corpse {_currentCorpseId} no longer exists.");
                CloseWindowAndClearSelection();
                return;
            }

            if (!netObj.TryGetComponent(out CorpseLootInteractable corpse))
            {
                Debug.LogWarning($"[LootUI] NetworkObject {_currentCorpseId} has no CorpseLootInteractable.");
                CloseWindowAndClearSelection();
                return;
            }

            corpse.RequestTakeItemServerRpc(instanceId);
        }

        private bool DoesCorpseStillExist(ulong corpseId)
        {
            if (NetworkManager.Singleton == null)
                return false;

            var spawnManager = NetworkManager.Singleton.SpawnManager;
            if (spawnManager == null)
                return false;

            return spawnManager.SpawnedObjects.TryGetValue(corpseId, out var netObj) && netObj != null;
        }

        private void CloseWindowAndClearSelection()
        {
            _hasCorpseSelection = false;
            _currentCorpseId = 0;
            _currentEntries.Clear();
            ClearRows();
            SetPanelVisible(false);
        }

        private void SetPanelVisible(bool visible)
        {
            if (rootPanel != null)
                rootPanel.SetActive(visible);
        }

        private void ClearRows()
        {
            for (int i = 0; i < _spawnedRows.Count; i++)
            {
                var row = _spawnedRows[i];
                if (row != null)
                    Destroy(row.gameObject);
            }

            _spawnedRows.Clear();
        }
    }
}
