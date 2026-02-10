using System;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateDungeon.UI
{
    /// <summary>
    /// UIWindowManager
    /// ==============
    ///
    /// UO-style window layer manager.
    ///
    /// What this solves:
    /// - Multiple windows can be open at the same time (Inventory + Paperdoll + Skills, etc.)
    /// - Clicking any window brings it to the front (z-order via sibling index)
    /// - Simple API for later wiring: Open/Close/Toggle/Focus
    ///
    /// What this does NOT do (by design):
    /// - No gameplay logic
    /// - No networking logic
    /// - No drag/resize behavior (can be added later without changing this manager)
    ///
    /// Scene/Prefab expectations:
    /// - This component lives on Canvas_Windows (or a child under it)
    /// - A "WindowDock" Transform exists under Canvas_Windows
    /// - All Window_* prefabs are children of WindowDock
    /// - Each Window_* prefab root has a UIWindow component with a unique WindowId
    /// </summary>
    public sealed class UIWindowManager : MonoBehaviour
    {
        [Header("Scene References")]
        [Tooltip("All windows must live under this transform so we can control z-order by sibling index.")]
        [SerializeField] private Transform windowDock;

        // Map of WindowId -> UIWindow. WindowId is a designer-friendly string (e.g., "Inventory").
        // We use Ordinal (case-sensitive) so ids are predictable and fast.
        private readonly Dictionary<string, UIWindow> _windowsById =
            new Dictionary<string, UIWindow>(StringComparer.Ordinal);

        // Optional events: useful later if you add a taskbar, window buttons, etc.
        public event Action<UIWindow> OnWindowOpened;
        public event Action<UIWindow> OnWindowClosed;
        public event Action<UIWindow> OnWindowFocused;

        public Transform WindowDock => windowDock;

        private void Awake()
        {
            if (windowDock == null)
            {
                Debug.LogError("[UIWindowManager] Missing WindowDock reference. Assign it in the inspector.");
                return;
            }

            // Auto-register windows already present under the dock.
            // This matches your current setup where windows exist in the prefab hierarchy.
            var existingWindows = windowDock.GetComponentsInChildren<UIWindow>(includeInactive: true);
            foreach (var w in existingWindows)
            {
                RegisterWindow(w);
            }

            // Apply StartOpen flags.
            // Note: We iterate over the dictionary values. This is safe because we are not mutating it here.
            foreach (var pair in _windowsById)
            {
                var window = pair.Value;

                if (window.StartOpen)
                    Open(window.WindowId);
                else
                    Close(window.WindowId);
            }
        }

        /// <summary>
        /// Register a UIWindow so it can be controlled by WindowId.
        ///
        /// Typical usage:
        /// - Automatically from Awake() for windows already in the scene
        /// - Manually if you instantiate/spawn windows at runtime later
        /// </summary>
        public void RegisterWindow(UIWindow window)
        {
            if (window == null) return;
            if (windowDock == null) return; // already logged in Awake

            // Ensure the window lives under the dock.
            // This is important: our z-order is purely based on sibling index under this dock.
            if (window.transform.parent != windowDock)
            {
                window.transform.SetParent(windowDock, worldPositionStays: false);
            }

            if (string.IsNullOrWhiteSpace(window.WindowId))
            {
                Debug.LogError($"[UIWindowManager] UIWindow on '{window.name}' is missing WindowId.");
                return;
            }

            // Prevent accidental duplicates (two windows both called "Inventory").
            if (_windowsById.ContainsKey(window.WindowId))
            {
                Debug.LogError($"[UIWindowManager] Duplicate WindowId '{window.WindowId}'. Each window must be unique.");
                return;
            }

            _windowsById.Add(window.WindowId, window);

            // Give the window a back-reference so it can request focus on click.
            window.BindManager(this);
        }

        /// <summary>
        /// Returns true if the window exists AND is currently active.
        /// </summary>
        public bool IsOpen(string windowId)
        {
            return TryGetWindow(windowId, out var w) && w.gameObject.activeSelf;
        }

        /// <summary>
        /// Open a window and bring it to front.
        /// Safe to call even if already open.
        /// </summary>
        public void Open(string windowId)
        {
            if (!TryGetWindow(windowId, out var w))
                return;

            if (!w.gameObject.activeSelf)
            {
                w.gameObject.SetActive(true);
                OnWindowOpened?.Invoke(w);
            }

            // UO-style: opening a window also focuses it.
            Focus(windowId);
        }

        /// <summary>
        /// Close a window.
        /// Safe to call even if already closed.
        /// </summary>
        public void Close(string windowId)
        {
            if (!TryGetWindow(windowId, out var w))
                return;

            if (w.gameObject.activeSelf)
            {
                w.gameObject.SetActive(false);
                OnWindowClosed?.Invoke(w);
            }
        }

        /// <summary>
        /// Toggle a window open/closed.
        /// </summary>
        public void Toggle(string windowId)
        {
            if (IsOpen(windowId)) Close(windowId);
            else Open(windowId);
        }

        /// <summary>
        /// Bring a window to the front by placing it as the last sibling under WindowDock.
        ///
        /// This is the core of UO-style window focus.
        /// </summary>
        public void Focus(string windowId)
        {
            if (!TryGetWindow(windowId, out var w))
                return;

            // Can't focus a closed window.
            if (!w.gameObject.activeSelf)
                return;

            // Render above all other windows.
            w.transform.SetAsLastSibling();

            OnWindowFocused?.Invoke(w);
        }

        /// <summary>
        /// Helper: fetch window by id with a friendly warning.
        /// </summary>
        private bool TryGetWindow(string windowId, out UIWindow window)
        {
            window = null;

            if (string.IsNullOrWhiteSpace(windowId))
                return false;

            if (_windowsById.TryGetValue(windowId, out window))
                return true;

            Debug.LogWarning($"[UIWindowManager] Unknown WindowId '{windowId}'. Did you forget to add UIWindow and set WindowId?");
            return false;
        }
    }
}
