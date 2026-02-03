using UnityEngine;
using UnityEngine.UI;

namespace UltimateDungeon.UI
{
    /// <summary>
    /// InventoryGridLayoutUI (UI SETUP)
    /// ================================
    ///
    /// UO-style inventory grid container.
    ///
    /// FIX NOTE:
    /// ---------
    /// Unity does NOT allow creating or mutating RectOffset in field initializers
    /// or MonoBehaviour constructors. RectOffset values MUST be applied at runtime
    /// (Awake/Start/OnValidate).
    ///
    /// This version fixes that by storing padding as raw ints and applying them
    /// safely in Awake().
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InventoryGridLayoutUI : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private int columns = 8;
        [SerializeField] private int visibleRows = 5;

        [SerializeField] private Vector2 cellSize = new Vector2(48f, 48f);
        [SerializeField] private Vector2 spacing = new Vector2(6f, 6f);

        [Header("Padding (pixels)")]
        [SerializeField] private int paddingLeft = 8;
        [SerializeField] private int paddingRight = 8;
        [SerializeField] private int paddingTop = 8;
        [SerializeField] private int paddingBottom = 8;

        [Header("Scroll")]
        [SerializeField] private bool useScrollRect = true;
        [SerializeField] private bool verticalOnlyScroll = true;

        [Header("References")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform viewport;
        [SerializeField] private RectTransform content;
        [SerializeField] private GridLayoutGroup grid;

        [Header("Auto Size")]
        [SerializeField] private bool autoSizeViewport = true;

        private void Awake()
        {
            AutoFindRefs();

            if (content == null || grid == null)
            {
                Debug.LogError("[InventoryGridLayoutUI] Missing Content/Grid references. Check prefab setup.");
                return;
            }

            ApplyGridSettings();

            if (useScrollRect)
                ApplyScrollSettings();

            if (autoSizeViewport)
                ApplyViewportSize();
        }

        /// <summary>
        /// Public helper: where slots should be spawned.
        /// </summary>
        public RectTransform GetSlotParent()
        {
            return content;
        }

        private void AutoFindRefs()
        {
            if (scrollRect == null)
                scrollRect = GetComponentInChildren<ScrollRect>(includeInactive: true);

            if (viewport == null && scrollRect != null)
                viewport = scrollRect.viewport;

            if (content == null && scrollRect != null)
                content = scrollRect.content;

            if (content != null && grid == null)
                grid = content.GetComponent<GridLayoutGroup>();
        }

        private void ApplyGridSettings()
        {
            columns = Mathf.Max(1, columns);
            visibleRows = Mathf.Max(1, visibleRows);

            cellSize.x = Mathf.Max(8f, cellSize.x);
            cellSize.y = Mathf.Max(8f, cellSize.y);

            spacing.x = Mathf.Max(0f, spacing.x);
            spacing.y = Mathf.Max(0f, spacing.y);

            grid.cellSize = cellSize;
            grid.spacing = spacing;

            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;

            // SAFE padding assignment (runtime only)
            grid.padding.left = paddingLeft;
            grid.padding.right = paddingRight;
            grid.padding.top = paddingTop;
            grid.padding.bottom = paddingBottom;

            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
        }

        private void ApplyScrollSettings()
        {
            if (scrollRect == null)
            {
                Debug.LogError("[InventoryGridLayoutUI] useScrollRect is true but ScrollRect is missing.");
                return;
            }

            if (viewport != null)
                scrollRect.viewport = viewport;

            if (content != null)
                scrollRect.content = content;

            if (verticalOnlyScroll)
            {
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
            }

            // UO-style: keep movement clamped to content bounds.
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = true;

            // IMPORTANT: Content alignment for ScrollRect
            // ------------------------------------------
            // If Content anchors/pivot are not TOP-LEFT, the GridLayoutGroup can appear offset,
            // or spawn "outside" the visible viewport.
            //
            // Standard ScrollRect setup for top-left grids:
            // - Content anchorMin/anchorMax = (0,1) top-left
            // - Content pivot = (0,1) top-left
            // - Content anchoredPosition = (0,0)
            if (content != null)
            {
                content.anchorMin = new Vector2(0f, 1f);
                content.anchorMax = new Vector2(0f, 1f);
                content.pivot = new Vector2(0f, 1f);
                content.anchoredPosition = Vector2.zero;
            }

            // Also force the ScrollRect to start at the TOP.
            // (Unity uses 1 = top, 0 = bottom for verticalNormalizedPosition.)
            scrollRect.verticalNormalizedPosition = 1f;
        }

        private void ApplyViewportSize()
        {
            if (viewport == null) return;

            float width = paddingLeft + paddingRight
                          + (columns * cellSize.x)
                          + ((columns - 1) * spacing.x);

            float height = paddingTop + paddingBottom
                           + (visibleRows * cellSize.y)
                           + ((visibleRows - 1) * spacing.y);

            // Size the viewport to exactly show the configured visible rows/cols.
            // This assumes your viewport is NOT stretching.
            viewport.sizeDelta = new Vector2(width, height);

            // Helpful default: make the content start matching viewport width.
            // Height will expand via ContentSizeFitter (recommended) as rows increase.
            if (content != null)
            {
                var size = content.sizeDelta;
                size.x = width;
                content.sizeDelta = size;

                // Keep content pinned to top-left.
                content.anchoredPosition = Vector2.zero;
            }

            // Force scroll to top after sizing.
            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 1f;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            columns = Mathf.Max(1, columns);
            visibleRows = Mathf.Max(1, visibleRows);

            cellSize.x = Mathf.Max(8f, cellSize.x);
            cellSize.y = Mathf.Max(8f, cellSize.y);

            spacing.x = Mathf.Max(0f, spacing.x);
            spacing.y = Mathf.Max(0f, spacing.y);
        }
#endif
    }
}
