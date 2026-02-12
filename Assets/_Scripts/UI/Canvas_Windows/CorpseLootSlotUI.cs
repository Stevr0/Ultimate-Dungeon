using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UltimateDungeon.UI
{
    /// <summary>
    /// CorpseLootSlotUI
    /// ----------------
    /// Tiny view/controller for a single loot slot:
    /// - shows icon + optional stack count
    /// - fires a click callback when pressed
    /// </summary>
    public sealed class CorpseLootSlotUI : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Button button;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text stackText; // optional

        private System.Action _onClick;

        private void Awake()
        {
            // Defensive: allow prefab to work even if you forgot to wire button.
            if (button == null)
                button = GetComponent<Button>();

            if (button != null)
                button.onClick.AddListener(() => _onClick?.Invoke());
        }

        /// <summary>
        /// Called by the loot window to populate the slot visuals.
        /// </summary>
        public void SetVisuals(Sprite icon, int stackCount, bool showStackWhenOne = false)
        {
            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null; // hide if missing
            }

            if (stackText != null)
            {
                // Only show stack count when relevant (typical inventory behavior).
                bool show = stackCount > 1 || showStackWhenOne;
                stackText.gameObject.SetActive(show);
                if (show)
                    stackText.text = stackCount.ToString();
            }
        }

        /// <summary>
        /// Called by the loot window to set the click action for this slot.
        /// </summary>
        public void SetOnClick(System.Action onClick)
        {
            _onClick = onClick;
        }
    }
}
