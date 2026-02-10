using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugManager;

namespace UltimateDungeon.UI
{
    /// <summary>
    /// UIModalManager
    /// =============
    ///
    /// Owns the *modal layer* (Canvas_Modals).
    ///
    /// Features:
    /// - Supports a STACK of modals (Loot -> Confirm on top, etc.)
    /// - Enables/disables a single ModalBlocker that blocks input behind modals
    /// - Provides APIs: Open(modal), Close(modal), CloseTop(), CloseAll()
    ///
    /// This is UI plumbing only.
    /// Gameplay systems later can request modals without knowing how they're rendered.
    /// </summary>
    public sealed class UIModalManager : MonoBehaviour
    {
        [Header("Scene References")]
        [Tooltip("All modal instances should live under this root so their z-order is controlled by sibling index.")]
        [SerializeField] private Transform modalRoot;

        [Tooltip("Full-screen blocker (Image) with Raycast Target ON. Enabled whenever ANY modal is open.")]
        [SerializeField] private Image modalBlocker;

        // Stack (in open order). Last element is topmost.
        private readonly List<UIModal> _stack = new List<UIModal>();

        public event Action<UIModal> OnModalOpened;
        public event Action<UIModal> OnModalClosed;

        public bool AnyModalOpen => _stack.Count > 0;

        private void Awake()
        {
            if (modalRoot == null)
                Debug.LogError("[UIModalManager] Missing ModalRoot reference. Assign it in the inspector.");

            if (modalBlocker == null)
                Debug.LogError("[UIModalManager] Missing ModalBlocker reference. Assign it in the inspector.");

            RefreshBlocker();
        }

        /// <summary>
        /// Open a modal instance.
        /// - Ensures it is parented under modalRoot
        /// - Activates it
        /// - Pushes onto the stack
        /// - Brings it to the top visually
        /// </summary>
        public void Open(UIModal modal)
        {
            if (modal == null) return;
            if (modalRoot == null) return;

            // Ensure correct parent.
            if (modal.transform.parent != modalRoot)
                modal.transform.SetParent(modalRoot, worldPositionStays: false);

            // If it's already open, just bring to top.
            if (_stack.Contains(modal))
            {
                BringToTop(modal);
                return;
            }

            // Activate and push onto stack.
            modal.gameObject.SetActive(true);
            _stack.Add(modal);

            // Highest sibling = renders above other modals.
            modal.transform.SetAsLastSibling();

            // Back-link so modal can request close.
            modal.BindManager(this);

            RefreshBlocker();
            OnModalOpened?.Invoke(modal);
        }

        /// <summary>
        /// Close a specific modal instance.
        /// Safe even if not topmost.
        /// </summary>
        public void Close(UIModal modal)
        {
            if (modal == null) return;

            if (_stack.Remove(modal))
            {
                modal.gameObject.SetActive(false);
                RefreshBlocker();
                OnModalClosed?.Invoke(modal);
            }
        }

        /// <summary>
        /// Close the topmost modal (the last opened).
        /// This is what an ESC handler should call.
        /// </summary>
        public void CloseTop()
        {
            if (_stack.Count == 0) return;

            var top = _stack[_stack.Count - 1];
            Close(top);
        }

        /// <summary>
        /// Close all modals immediately.
        /// Useful for scene transitions or safety resets.
        /// </summary>
        public void CloseAll()
        {
            // Hide all modal GameObjects.
            for (int i = _stack.Count - 1; i >= 0; i--)
            {
                var m = _stack[i];
                if (m != null)
                    m.gameObject.SetActive(false);
            }

            _stack.Clear();
            RefreshBlocker();
        }

        /// <summary>
        /// Bring an already-open modal to the top.
        /// Updates both visual order (sibling) and logical order (stack).
        /// </summary>
        private void BringToTop(UIModal modal)
        {
            if (modal == null) return;

            modal.transform.SetAsLastSibling();

            // Move it to the end of the stack.
            _stack.Remove(modal);
            _stack.Add(modal);

            RefreshBlocker();
        }

        /// <summary>
        /// Enable blocker when any modal is open.
        /// The blocker should sit behind modals, but above windows/world.
        /// </summary>
        private void RefreshBlocker()
        {
            if (modalBlocker == null) return;

            modalBlocker.gameObject.SetActive(AnyModalOpen);

            if (AnyModalOpen)
            {
                // Ensure blocker is behind the modals within Canvas_Modals.
                // This assumes blocker + modalRoot are siblings under Canvas_Modals.
                modalBlocker.transform.SetAsFirstSibling();
            }
        }
    }
}
