using UnityEngine;
using UnityEngine.EventSystems;

namespace UltimateDungeon.UI
{
    /// <summary>
    /// UIWindow
    /// ========
    /// Attach this to the ROOT of each Window_* prefab.
    ///
    /// Responsibilities:
    /// - Stores a unique WindowId (designer-friendly string)
    /// - Notifies UIWindowManager when the user clicks this window so it can be focused
    ///
    /// Why IPointerDownHandler?
    /// - It's the simplest "user clicked this UI element" signal.
    /// - It triggers even if you click on child UI elements (as long as raycasts hit within the window).
    ///
    /// Important:
    /// - Ensure your window root (or a full-size child Image) has a Graphic component
    ///   with Raycast Target enabled, otherwise clicks may not register.
    /// </summary>
    public sealed class UIWindow : MonoBehaviour, IPointerDownHandler
    {
        [Header("Window Identity")]
        [Tooltip("Unique id used by UIWindowManager to open/close/focus this window. Example: Inventory")]
        [SerializeField] private string windowId = "";

        [Tooltip("If true, this window will be opened automatically at startup.")]
        [SerializeField] private bool startOpen = false;

        // Assigned by UIWindowManager during RegisterWindow.
        private UIWindowManager _manager;

        public string WindowId => windowId;
        public bool StartOpen => startOpen;

        /// <summary>
        /// Called by UIWindowManager during registration.
        /// </summary>
        public void BindManager(UIWindowManager manager)
        {
            _manager = manager;
        }

        /// <summary>
        /// Called by Unity UI when the user presses down on this window.
        /// We use it to request focus, bringing this window to the front.
        /// </summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            if (_manager == null)
                return; // not registered yet

            _manager.Focus(windowId);
        }

        /// <summary>
        /// Optional helper for a Close button.
        /// You can wire a UI Button -> OnClick -> UIWindow.RequestClose.
        /// </summary>
        public void RequestClose()
        {
            if (_manager == null)
            {
                // Fallback behavior if not registered: just hide.
                gameObject.SetActive(false);
                return;
            }

            _manager.Close(windowId);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Normalize accidental whitespace so ids remain stable.
            if (!string.IsNullOrEmpty(windowId))
                windowId = windowId.Trim();
        }
#endif
    }
}
