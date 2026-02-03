using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UltimateDungeon.UI
{
    /// <summary>
    /// InventorySlotPlaceholderUI (SETUP ONLY)
    /// ======================================
    ///
    /// Tiny helper for your placeholder slot prefab.
    ///
    /// Why have this?
    /// - Lets you quickly see slot indices during layout testing.
    /// - Gives you a place to later expand into a real InventorySlotView
    ///   without changing prefab structure.
    ///
    /// IMPORTANT:
    /// - This does NOT represent an item.
    /// - No gameplay logic.
    /// </summary>
    public sealed class InventorySlotPlaceholderUI : MonoBehaviour
    {
        [Header("Optional Visuals")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI indexLabel;

        /// <summary>
        /// Set the slot index for debug/layout visibility.
        /// </summary>
        public void SetIndex(int index)
        {
            if (indexLabel != null)
                indexLabel.text = index.ToString();
        }

        /// <summary>
        /// Setup helper: tint the slot background or icon.
        /// </summary>
        public void SetTint(Color tint)
        {
            if (iconImage != null)
                iconImage.color = tint;
        }
    }
}
