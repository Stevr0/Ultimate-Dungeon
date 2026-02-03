using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UltimateDungeon.UI
{
    /// <summary>
    /// InventoryWeightPanelUI (UI-ONLY)
    /// ================================
    ///
    /// Displays the player's current inventory weight vs carrying capacity.
    ///
    /// SETUP PHASE (now):
    /// - Pure UI presentation: text + fill bar
    /// - Overweight state visuals
    /// - No gameplay rules / no calculations enforced here
    ///
    /// WIRING PHASE (later):
    /// - A binder/controller calls SetWeight(current, capacity)
    /// - Capacity may include mounts/bags/food buffs depending on authoritative rules
    ///
    /// Notes:
    /// - Albion-style: weight pertains to INVENTORY items, not equipped.
    /// - Movement penalty rules are NOT implemented here.
    ///   This panel can *display* an overweight warning once rules exist.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InventoryWeightPanelUI : MonoBehaviour
    {
        [Header("Text")]
        [Tooltip("Displays something like: 'Weight 35.2 / 85.0' or '35 / 85'.")]
        [SerializeField] private TextMeshProUGUI weightText;

        [Tooltip("Optional extra line for status like 'Overweight' or movement penalty.")]
        [SerializeField] private TextMeshProUGUI statusText;

        [Header("Bar")]
        [Tooltip("Fill Image (Image Type = Filled).")]
        [SerializeField] private Image fillImage;

        [Tooltip("Optional background image, useful if you want to tint it during overweight.")]
        [SerializeField] private Image backgroundImage;

        [Header("Formatting")]
        [Tooltip("If true, rounds values to 0 decimals. If false, shows 1 decimal.")]
        [SerializeField] private bool roundToInt = false;

        [Tooltip("If true, shows percent like '42%'.")]
        [SerializeField] private bool showPercent = true;

        [Header("Overweight Display")]
        [Tooltip("When weight exceeds capacity, show an overweight warning.")]
        [SerializeField] private bool showOverweightWarning = true;

        [Tooltip("Text shown when overweight (purely visual).")]
        [SerializeField] private string overweightLabel = "Overweight";

        [Header("Debug (Setup Only)")]
        [SerializeField] private bool debugSetExampleOnStart = false;

        [SerializeField] private float debugWeight = 65f;
        [SerializeField] private float debugCapacity = 80f;

        // Cached numbers (UI state)
        private float _weight;
        private float _capacity;

        private void Start()
        {
            if (debugSetExampleOnStart)
                SetWeight(debugWeight, debugCapacity);
            else
                Refresh();
        }

        /// <summary>
        /// Set the displayed weight and capacity.
        ///
        /// IMPORTANT:
        /// - This method does NOT compute weight.
        /// - It assumes the caller is authoritative (server/binder).
        /// </summary>
        public void SetWeight(float currentWeight, float capacity)
        {
            _weight = Mathf.Max(0f, currentWeight);
            _capacity = Mathf.Max(0f, capacity);
            Refresh();
        }

        /// <summary>
        /// Convenience: clears the panel to a "no data" state.
        /// </summary>
        public void Clear()
        {
            _weight = 0f;
            _capacity = 0f;
            Refresh();
        }

        private void Refresh()
        {
            // Calculate percent safely.
            float percent = 0f;
            if (_capacity > 0f)
                percent = _weight / _capacity;

            // Clamp the bar fill to [0..1] so it doesn't overflow the UI.
            if (fillImage != null)
                fillImage.fillAmount = Mathf.Clamp01(percent);

            bool overweight = (_capacity > 0f) && (_weight > _capacity);

            // Update weight text.
            if (weightText != null)
            {
                string w = FormatNumber(_weight);
                string c = FormatNumber(_capacity);

                if (showPercent && _capacity > 0f)
                {
                    int pct = Mathf.RoundToInt(percent * 100f);
                    weightText.text = $"Weight {w} / {c}  ({pct}%)";
                }
                else
                {
                    weightText.text = $"Weight {w} / {c}";
                }
            }

            // Update status text.
            if (statusText != null)
            {
                if (showOverweightWarning && overweight)
                {
                    // We only show a label for now.
                    // Later you can display "-15% Move Speed" once rules exist.
                    statusText.enabled = true;
                    statusText.text = overweightLabel;
                }
                else
                {
                    statusText.enabled = false;
                    statusText.text = string.Empty;
                }
            }

            // Optional: you can tint background/fill for overweight.
            // We avoid hardcoding colors here. Keep styling in prefabs/themes.
            // If you want tinting later, we can add a small theme struct.
        }

        private string FormatNumber(float value)
        {
            if (roundToInt)
                return Mathf.RoundToInt(value).ToString();

            // 1 decimal is a nice balance for weight.
            return value.ToString("0.0");
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            debugWeight = Mathf.Max(0f, debugWeight);
            debugCapacity = Mathf.Max(0.001f, debugCapacity);
        }
#endif
    }
}
