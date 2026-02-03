using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UltimateDungeon.UI
{
    /// <summary>
    /// UIWindowDrag_Fixed
    /// ==================
    ///
    /// Drop-in replacement for UIWindowDrag when dragging "doesn't work".
    ///
    /// The 3 most common causes:
    /// 1) No raycast target on the header/root you are trying to drag.
    /// 2) A LayoutGroup (on WindowDock or parents) is repositioning the window each frame.
    /// 3) The script is dragging the wrong RectTransform (e.g., WindowDock instead of the window root).
    ///
    /// This version is more explicit + defensive:
    /// - You can assign the DragTarget (window root RectTransform) directly.
    /// - If left null, it auto-finds the nearest UIWindow root and drags THAT.
    /// - It warns you if a LayoutGroup is in the parent chain (likely overriding position).
    /// - It can auto-add a transparent Image raycast target to your header if you want.
    ///
    /// Recommended setup:
    /// - Add this script to the HeaderBar object of your window.
    /// - Assign DragTarget to the Window_* root RectTransform.
    /// - Ensure the HeaderBar has an Image with Raycast Target ON.
    /// </summary>
    public sealed class UIWindowDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IPointerDownHandler
    {
        [Header("Drag Target")]
        [Tooltip("The RectTransform to move when dragging (usually the Window_* root).")]
        [SerializeField] private RectTransform dragTarget;

        [Header("Raycast Helper (Optional)")]
        [Tooltip("If true, ensures this object has a Graphic (Image) so it can receive drag events.")]
        [SerializeField] private bool ensureRaycastTargetOnThisObject = true;

        [Header("Focus")]
        [Tooltip("If true, clicking/dragging will bring the window to front (via UIWindow focus).")]
        [SerializeField] private bool focusWindowOnPointerDown = true;

        private Canvas _canvas;
        private RectTransform _canvasRect;
        private Vector2 _pointerOffset;

        private void Awake()
        {
            // 1) Ensure we can receive pointer events.
            if (ensureRaycastTargetOnThisObject)
            {
                // If there's no Graphic, Unity UI won't send pointer/drag events.
                var graphic = GetComponent<Graphic>();
                if (graphic == null)
                {
                    // Add a transparent Image as a raycast target.
                    var img = gameObject.AddComponent<Image>();
                    img.color = new Color(1f, 1f, 1f, 0f); // fully transparent
                    img.raycastTarget = true;
                }
                else
                {
                    graphic.raycastTarget = true;
                }
            }

            // 2) Find the Canvas.
            _canvas = GetComponentInParent<Canvas>();
            if (_canvas == null)
            {
                Debug.LogError("[UIWindowDrag_Fixed] No Canvas found in parents. Dragging requires a Canvas.");
                enabled = false;
                return;
            }

            _canvasRect = _canvas.transform as RectTransform;
            if (_canvasRect == null)
            {
                Debug.LogError("[UIWindowDrag_Fixed] Canvas transform is not a RectTransform (unexpected).");
                enabled = false;
                return;
            }

            // 3) Resolve drag target.
            if (dragTarget == null)
            {
                // Best: find UIWindow root and drag that.
                var window = GetComponentInParent<UIWindow>();
                if (window != null)
                    dragTarget = window.transform as RectTransform;
            }

            if (dragTarget == null)
            {
                Debug.LogError("[UIWindowDrag_Fixed] DragTarget is not set and no UIWindow was found in parents. Assign DragTarget in inspector.");
                enabled = false;
                return;
            }

            // 4) Warn if a LayoutGroup might be overriding movement.
            if (HasLayoutGroupInParents(dragTarget))
            {
                Debug.LogWarning($"[UIWindowDrag_Fixed] '{dragTarget.name}' is under a LayoutGroup. " +
                                 "LayoutGroups can override position every frame. " +
                                 "If drag snaps back, REMOVE the LayoutGroup from WindowDock or parents.");
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!focusWindowOnPointerDown) return;

            // Focusing is handled by UIWindow (if click hits a raycast target within the window).
            // This method exists mainly so you can debug focus issues if needed.
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (dragTarget == null) return;

            // Convert pointer to canvas-local.
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasRect,
                    eventData.position,
                    eventData.pressEventCamera,
                    out var localPointer))
            {
                _pointerOffset = dragTarget.anchoredPosition - localPointer;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (dragTarget == null) return;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasRect,
                    eventData.position,
                    eventData.pressEventCamera,
                    out var localPointer))
            {
                dragTarget.anchoredPosition = localPointer + _pointerOffset;
            }
        }

        private static bool HasLayoutGroupInParents(RectTransform target)
        {
            if (target == null) return false;

            Transform t = target.parent;
            while (t != null)
            {
                if (t.GetComponent<LayoutGroup>() != null)
                    return true;
                t = t.parent;
            }

            return false;
        }
    }
}
