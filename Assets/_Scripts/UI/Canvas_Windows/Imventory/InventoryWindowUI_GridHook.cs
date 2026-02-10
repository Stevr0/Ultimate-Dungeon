using TMPro;
using UnityEngine;

namespace UltimateDungeon.UI
{
    /// <summary>
    /// InventoryWindowUI (Grid Hook version)
    /// ====================================
    ///
    /// Same idea as InventoryWindowUI, but it prefers to spawn placeholder slots
    /// under an InventoryGridLayoutUI content parent.
    ///
    /// Use this if you want the Inventory window to automatically use the grid container.
    ///
    /// NOTE:
    /// - This is still SETUP ONLY. No gameplay wiring.
    /// - When you wire real inventory, you'll replace the placeholder spawning.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InventoryWindowUI_GridHook : MonoBehaviour
    {
        [Header("Required")]
        [SerializeField] private UIWindowFrame frame;
        [SerializeField] private UIWindow window;

        [Header("Optional")]
        [SerializeField] private TextMeshProUGUI headerLabel;

        [Header("Grid Container")]
        [Tooltip("If assigned, placeholders will spawn under this grid's Content.")]
        [SerializeField] private InventoryGridLayoutUI gridLayout;

        [Header("Placeholder Layout (Setup Only)")]
        [SerializeField] private bool spawnPlaceholders = true;
        [SerializeField] private int placeholderSlotCount = 40;
        [SerializeField] private RectTransform placeholderSlotPrefab;

        private void Awake()
        {
            if (window == null)
                window = GetComponent<UIWindow>();

            if (frame == null)
                frame = GetComponentInChildren<UIWindowFrame>(includeInactive: true);

            if (gridLayout == null)
                gridLayout = GetComponentInChildren<InventoryGridLayoutUI>(includeInactive: true);

            if (window == null)
                Debug.LogError("[InventoryWindowUI_GridHook] Missing UIWindow on the window root.");

            if (frame == null)
                Debug.LogError("[InventoryWindowUI_GridHook] Missing UIWindowFrame in children.");

            if (frame != null)
            {
                frame.SetTitle("Backpack");
                frame.BindCloseTo(window);
            }

            if (headerLabel != null)
                headerLabel.text = "Backpack";

            if (spawnPlaceholders)
                TrySpawnPlaceholders();
        }

        private void TrySpawnPlaceholders()
        {
            if (placeholderSlotPrefab == null)
            {
                Debug.LogWarning("[InventoryWindowUI_GridHook] spawnPlaceholders is true but placeholderSlotPrefab is not assigned.");
                return;
            }

            RectTransform parent = null;

            if (gridLayout != null)
                parent = gridLayout.GetSlotParent();

            if (parent == null && frame != null)
                parent = frame.ContentRoot;

            if (parent == null)
            {
                Debug.LogWarning("[InventoryWindowUI_GridHook] Cannot spawn placeholders: no parent found.");
                return;
            }

            if (parent.childCount > 0)
                return;

            for (int i = 0; i < placeholderSlotCount; i++)
            {
                var slot = Instantiate(placeholderSlotPrefab, parent);
                slot.name = $"Slot_{i:00}";

                // If the slot prefab has the placeholder script, label it.
                var label = slot.GetComponent<InventorySlotPlaceholderUI>();
                if (label != null)
                    label.SetIndex(i);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (placeholderSlotCount < 0) placeholderSlotCount = 0;
            if (placeholderSlotCount > 200) placeholderSlotCount = 200;
        }
#endif
    }
}
