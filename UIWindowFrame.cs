using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UltimateDungeon.UI
{
    /// <summary>
    /// UIWindowFrame
    /// =============
    ///
    /// A reusable "chrome" component for all UO-style windows.
    ///
    /// This is UI-only scaffolding:
    /// - Title text
    /// - Close button
    /// - Header bar reference (for drag)
    /// - ContentRoot reference (where your real UI content goes)
    ///
    /// Why do this?
    /// - Every window (Inventory/Paperdoll/Skills/Magic/Character) shares the same structure.
    /// - You wire references ONCE per prefab and stop fighting hierarchy inconsistencies.
    ///
    /// Dependencies:
    /// - TextMeshProUGUI for the title
    /// - A Unity UI Button for close
    /// </summary>
    public sealed class UIWindowFrame : MonoBehaviour
    {
        [Header("Frame Parts")]
        [Tooltip("Drag handle / header bar area. Put UIWindowDrag_Fixed on this object.")]
        [SerializeField] private RectTransform headerBar;

        [Tooltip("Title text displayed in the header.")]
        [SerializeField] private TextMeshProUGUI titleText;

        [Tooltip("Close button in the header.")]
        [SerializeField] private Button closeButton;

        [Tooltip("Where the window's unique content lives. Your Inventory grid etc. will be inside this.")]
        [SerializeField] private RectTransform contentRoot;

        public RectTransform HeaderBar => headerBar;
        public RectTransform ContentRoot => contentRoot;

        /// <summary>
        /// Set the title text at runtime.
        /// This is safe even if titleText is not assigned (it will do nothing).
        /// </summary>
        public void SetTitle(string title)
        {
            if (titleText == null) return;
            titleText.text = title;
        }

        /// <summary>
        /// Wire the close button to a specific UIWindow.
        /// We keep this as a method so each window can bind it cleanly.
        /// </summary>
        public void BindCloseTo(UIWindow window)
        {
            if (closeButton == null) return;

            // Clean old listeners to prevent duplicate calls if you rebind.
            closeButton.onClick.RemoveAllListeners();

            if (window == null) return;

            // UIWindow already knows how to close itself via its manager.
            closeButton.onClick.AddListener(window.RequestClose);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Try to auto-find common parts to reduce setup friction.
            // This is editor-only convenience.

            if (headerBar == null)
            {
                var header = transform.Find("HeaderBar");
                if (header != null) headerBar = header as RectTransform;
            }

            if (contentRoot == null)
            {
                var content = transform.Find("ContentRoot");
                if (content != null) contentRoot = content as RectTransform;
            }
        }
#endif
    }
}
