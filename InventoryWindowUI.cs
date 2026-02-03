using TMPro;
using UnityEngine;

namespace UltimateDungeon.UI
{
    /// <summary>
    /// InventoryWindowUI
    /// ================
    ///
    /// UI-only window controller for the Inventory/Backpack window.
    ///
    /// Right now (SETUP PHASE):
    /// - Owns references to the window frame
    /// - Sets the title
    /// - Binds the close button
    /// - (Optional) spawns placeholder slots so you can validate layout/scaling
    ///
    /// Later (WIRING PHASE):
    /// - This will bind to an Inventory model/binder and populate real items.
    /// - DO NOT put gameplay rules here. This script is purely presentation + UI events.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InventoryWindowUI : MonoBehaviour
    {
        [Header("Required")]
        [Tooltip("Reusable frame component (HeaderBar/Title/Close/ContentRoot).")]
        [SerializeField] private UIWindowFrame frame;

        [Tooltip("UIWindow component on the root of this window prefab.")]
        [SerializeField] private UIWindow window;

        [Header("Inventory Labels")]
        [Tooltip("Optional label inside the content area. Not required if you prefer title-only.")]
        [SerializeField] private TextMeshProUGUI headerLabel;

        [Header("Placeholder Layout (Setup Only)")]
        [Tooltip("If true, spawns placeholder slot visuals under ContentRoot so you can test sizing.")]
        [SerializeField] private bool spawnPlaceholders = true;

        [Tooltip("How many placeholder slots to spawn. Typical UO backpacks feel like 5x4, 8x5, etc.")]
        [SerializeField] private int placeholderSlotCount = 40;

        [Tooltip("A simple slot prefab (UI only). Should be a RectTransform with an Image.")]
        [SerializeField] private RectTransform placeholderSlotPrefab;

        [Tooltip("Parent under ContentRoot where slots should be spawned. If null, uses ContentRoot.")]
        [SerializeField] private RectTransform slotParent;

        private void Awake()
        {
            // Auto-find references to reduce inspector friction.
            if (window == null)
                window = GetComponent<UIWindow>();

            if (frame == null)
                frame = GetComponentInChildren<UIWindowFrame>(includeInactive: true);

            // Fail loudly during setup.
            if (window == null)
                Debug.LogError("[InventoryWindowUI] Missing UIWindow on the window root.");

            if (frame == null)
                Debug.LogError("[InventoryWindowUI] Missing UIWindowFrame in children.");

            // Apply basic window chrome.
            if (frame != null)
            {
                frame.SetTitle("Backpack");
                frame.BindCloseTo(window);
            }

            if (headerLabel != null)
                headerLabel.text = "Backpack";

            // Setup-only: spawn placeholder slots.
            if (spawnPlaceholders)
            {
                TrySpawnPlaceholders();
            }
        }

        /// <summary>
        /// Spawns a grid of placeholder slots so you can validate:
        /// - window size
        /// - scaling across resolutions
        /// - scroll view / grid layout group behavior
        ///
        /// This should be disabled when you wire real inventory data.
        /// </summary>
        private void TrySpawnPlaceholders()
        {
            if (placeholderSlotPrefab == null)
            {
                Debug.LogWarning("[InventoryWindowUI] spawnPlaceholders is true but placeholderSlotPrefab is not assigned.");
                return;
            }

            // Choose a parent.
            RectTransform parent = slotParent != null
                ? slotParent
                : (frame != null ? frame.ContentRoot : null);

            if (parent == null)
            {
                Debug.LogWarning("[InventoryWindowUI] Cannot spawn placeholders: no parent found (slotParent/contentRoot missing).");
                return;
            }

            // Avoid double-spawning if this window is toggled.
            // If you want a rebuild, delete children manually or add a 'Rebuild' button later.
            if (parent.childCount > 0)
                return;

            // Spawn N placeholder slots.
            for (int i = 0; i < placeholderSlotCount; i++)
            {
                var slot = Instantiate(placeholderSlotPrefab, parent);
                slot.name = $"Slot_{i:00}";
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Keep placeholder count sane.
            if (placeholderSlotCount < 0) placeholderSlotCount = 0;
            if (placeholderSlotCount > 200) placeholderSlotCount = 200;
        }
#endif
    }
}
