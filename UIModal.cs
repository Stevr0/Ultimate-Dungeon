using UnityEngine;

namespace UltimateDungeon.UI
{
    /// <summary>
    /// UIModal
    /// =======
    /// Attach this to the ROOT of each Modal_* prefab.
    ///
    /// Responsibilities:
    /// - Optional designer-friendly ModalId (string)
    /// - Can request that the UIModalManager close it
    ///
    /// This keeps each modal prefab self-contained:
    /// - Its Close button can call RequestClose()
    /// - Confirm/Cancel buttons can close the modal after doing UI-only work
    ///
    /// Gameplay wiring later:
    /// - A "Confirm" modal can raise an event, then close itself
    /// - A Loot modal can display items, and request server actions via a separate binder/controller
    /// </summary>
    public sealed class UIModal : MonoBehaviour
    {
        [Header("Modal Identity")]
        [Tooltip("Optional id. Useful later if you want to open modals by name.")]
        [SerializeField] private string modalId = "";

        private UIModalManager _manager;

        public string ModalId => modalId;

        /// <summary>
        /// Called by UIModalManager when the modal is opened.
        /// </summary>
        public void BindManager(UIModalManager manager)
        {
            _manager = manager;
        }

        /// <summary>
        /// Call this from a UI Button (OnClick) to close the modal.
        /// </summary>
        public void RequestClose()
        {
            if (_manager == null)
            {
                // Fallback: if no manager is present, at least hide.
                gameObject.SetActive(false);
                return;
            }

            _manager.Close(this);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!string.IsNullOrEmpty(modalId))
                modalId = modalId.Trim();
        }
#endif
    }
}
