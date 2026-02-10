using UnityEngine;

namespace UltimateDungeon.UI
{
    /// <summary>
    /// UIWindowClampToCanvas
    /// =====================
    ///
    /// Optional helper that keeps a window on-screen.
    ///
    /// Attach this to the WINDOW ROOT (RectTransform) you want to clamp.
    ///
    /// Recommended usage:
    /// - Add UIWindowDrag to a header bar (so you can drag)
    /// - Add UIWindowClampToCanvas to the window root (so it stays visible)
    ///
    /// How it works:
    /// - Every LateUpdate, clamps anchoredPosition so the window rect stays inside the canvas rect.
    /// - Includes padding so you can allow a small edge margin.
    ///
    /// Notes:
    /// - This assumes your canvas is Screen Space Overlay or uses a matching reference resolution.
    /// - If you later add safe-area support, clamp can be updated to clamp to a "safe rect" instead.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIWindowClampToCanvas : MonoBehaviour
    {
        [Header("Clamp Settings")]
        [Tooltip("Padding (in pixels) to keep between the window and canvas edges.")]
        [SerializeField] private Vector2 padding = new Vector2(8f, 8f);

        [Tooltip("If true, clamps every frame in LateUpdate. If false, you can call ClampNow() manually.")]
        [SerializeField] private bool clampEveryFrame = true;

        private RectTransform _window;
        private RectTransform _canvasRect;

        private void Awake()
        {
            _window = transform as RectTransform;
            if (_window == null)
            {
                Debug.LogError("[UIWindowClampToCanvas] Must be on a RectTransform (UI object).");
                enabled = false;
                return;
            }

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[UIWindowClampToCanvas] No Canvas found in parents.");
                enabled = false;
                return;
            }

            _canvasRect = canvas.transform as RectTransform;
            if (_canvasRect == null)
            {
                Debug.LogError("[UIWindowClampToCanvas] Canvas transform is not a RectTransform (unexpected).");
                enabled = false;
            }
        }

        private void LateUpdate()
        {
            if (!clampEveryFrame) return;
            ClampNow();
        }

        /// <summary>
        /// Clamp the window anchoredPosition so it remains within the canvas.
        /// Call this after opening a window, after resolution changes, etc.
        /// </summary>
        public void ClampNow()
        {
            if (_window == null || _canvasRect == null) return;

            // Get rect sizes in local space.
            Vector2 canvasSize = _canvasRect.rect.size;
            Vector2 windowSize = _window.rect.size;

            // Our anchoredPosition is relative to the window's anchor.
            // This clamp assumes your windows use a stable anchor setup (commonly center).
            // If you anchor differently, clamping still works but edges may not be perfect.
            Vector2 pos = _window.anchoredPosition;

            // Compute half extents.
            float halfCanvasW = canvasSize.x * 0.5f;
            float halfCanvasH = canvasSize.y * 0.5f;

            float halfWindowW = windowSize.x * 0.5f;
            float halfWindowH = windowSize.y * 0.5f;

            // Clamp so the window bounds stay inside canvas bounds.
            float minX = -halfCanvasW + halfWindowW + padding.x;
            float maxX = halfCanvasW - halfWindowW - padding.x;

            float minY = -halfCanvasH + halfWindowH + padding.y;
            float maxY = halfCanvasH - halfWindowH - padding.y;

            // If the window is bigger than the canvas, min can exceed max.
            // In that case, force it to 0 so it stays centered.
            if (minX > maxX) pos.x = 0f;
            else pos.x = Mathf.Clamp(pos.x, minX, maxX);

            if (minY > maxY) pos.y = 0f;
            else pos.y = Mathf.Clamp(pos.y, minY, maxY);

            _window.anchoredPosition = pos;
        }
    }
}
