using UnityEngine;
using UnityEngine.UI;
using TMPro;

// -----------------------------------------------------------------------------
// HudVitalsUI.cs (ActorVitals Only)
// -----------------------------------------------------------------------------
// SINGLE vitals source:
// - This HUD binds ONLY to ActorVitals.
// - ActorVitals is the one-and-only replicated vitals component for Players/Monsters.
//
// Attach:
// - HUD canvas
//
// Bind:
// - LocalPlayerUIBinder should call hudVitals.Bind(actorVitals)
// -----------------------------------------------------------------------------

namespace UltimateDungeon.UI.HUD
{
    [DisallowMultipleComponent]
    public sealed class HudVitalsUI : MonoBehaviour
    {
        [Header("Bars")]
        [SerializeField] private Slider hpSlider;
        [SerializeField] private Slider staminaSlider;
        [SerializeField] private Slider manaSlider;

        [Header("Text")]
        [SerializeField] private TMP_Text hpText;
        [SerializeField] private TMP_Text staminaText;
        [SerializeField] private TMP_Text manaText;

        private UltimateDungeon.Combat.ActorVitals _vitals;

        private void OnDisable()
        {
            Unsubscribe();
        }

        /// <summary>
        /// Bind to the local player's replicated vitals.
        /// </summary>
        public void Bind(UltimateDungeon.Combat.ActorVitals vitals)
        {
            Unsubscribe();

            _vitals = vitals;

            if (_vitals == null)
            {
                Debug.LogWarning("[HudVitalsUI] Bind failed: vitals is null.");
                RefreshAll();
                return;
            }

            Subscribe();
            RefreshAll();
        }

        private void Subscribe()
        {
            if (_vitals == null) return;

            // Subscribe to changes so UI updates immediately.
            _vitals.CurrentHPNet.OnValueChanged += OnHpChanged;
            _vitals.MaxHPNet.OnValueChanged += OnHpChanged;

            _vitals.CurrentStaminaNet.OnValueChanged += OnStaminaChanged;
            _vitals.MaxStaminaNet.OnValueChanged += OnStaminaChanged;

            _vitals.CurrentManaNet.OnValueChanged += OnManaChanged;
            _vitals.MaxManaNet.OnValueChanged += OnManaChanged;
        }

        private void Unsubscribe()
        {
            if (_vitals == null) return;

            _vitals.CurrentHPNet.OnValueChanged -= OnHpChanged;
            _vitals.MaxHPNet.OnValueChanged -= OnHpChanged;

            _vitals.CurrentStaminaNet.OnValueChanged -= OnStaminaChanged;
            _vitals.MaxStaminaNet.OnValueChanged -= OnStaminaChanged;

            _vitals.CurrentManaNet.OnValueChanged -= OnManaChanged;
            _vitals.MaxManaNet.OnValueChanged -= OnManaChanged;
        }

        private void OnHpChanged(int previous, int current) => RefreshHp();
        private void OnStaminaChanged(int previous, int current) => RefreshStamina();
        private void OnManaChanged(int previous, int current) => RefreshMana();

        private void RefreshAll()
        {
            RefreshHp();
            RefreshStamina();
            RefreshMana();
        }

        private void RefreshHp()
        {
            int cur = _vitals != null ? _vitals.CurrentHPNet.Value : 0;
            int max = _vitals != null ? _vitals.MaxHPNet.Value : 0;

            SetSliderSafe(hpSlider, cur, max);
            SetTextSafe(hpText, cur, max);
        }

        private void RefreshStamina()
        {
            int cur = _vitals != null ? _vitals.CurrentStaminaNet.Value : 0;
            int max = _vitals != null ? _vitals.MaxStaminaNet.Value : 0;

            SetSliderSafe(staminaSlider, cur, max);
            SetTextSafe(staminaText, cur, max);
        }

        private void RefreshMana()
        {
            int cur = _vitals != null ? _vitals.CurrentManaNet.Value : 0;
            int max = _vitals != null ? _vitals.MaxManaNet.Value : 0;

            SetSliderSafe(manaSlider, cur, max);
            SetTextSafe(manaText, cur, max);
        }

        private static void SetSliderSafe(Slider slider, int current, int max)
        {
            if (slider == null) return;

            slider.minValue = 0;
            slider.maxValue = Mathf.Max(1, max);
            slider.value = Mathf.Clamp(current, 0, Mathf.Max(0, max));
        }

        private static void SetTextSafe(TMP_Text text, int current, int max)
        {
            if (text == null) return;

            text.text = $"{current}/{max}";
        }
    }
}
