using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// -----------------------------------------------------------------------------
// CharacterStatsPanelUI.cs
// -----------------------------------------------------------------------------
// Displays STR/DEX/INT from PlayerStatsNet.
// -----------------------------------------------------------------------------

namespace UltimateDungeon.UI.Panels
{
    [DisallowMultipleComponent]
    public sealed class CharacterStatsPanelUI : MonoBehaviour
    {
        [Header("Text")]
        [SerializeField] private TMP_Text strText;
        [SerializeField] private TMP_Text dexText;
        [SerializeField] private TMP_Text intText;

        [Header("Optional: base stats shown (small text)")]
        [SerializeField] private TMP_Text baseStrText;
        [SerializeField] private TMP_Text baseDexText;
        [SerializeField] private TMP_Text baseIntText;

        private UltimateDungeon.Players.Networking.PlayerStatsNet _stats;

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Bind(UltimateDungeon.Players.Networking.PlayerStatsNet stats)
        {
            Unsubscribe();

            _stats = stats;

            if (_stats == null)
            {
                Debug.LogWarning("[CharacterStatsPanelUI] Bind failed: stats is null.");
                RefreshAll();
                return;
            }

            Subscribe();
            RefreshAll();
        }

        private void Subscribe()
        {
            if (_stats == null) return;

            _stats.STR.OnValueChanged += OnEffectiveChanged;
            _stats.DEX.OnValueChanged += OnEffectiveChanged;
            _stats.INT.OnValueChanged += OnEffectiveChanged;

            _stats.BaseSTR.OnValueChanged += OnBaseChanged;
            _stats.BaseDEX.OnValueChanged += OnBaseChanged;
            _stats.BaseINT.OnValueChanged += OnBaseChanged;
        }

        private void Unsubscribe()
        {
            if (_stats == null) return;

            _stats.STR.OnValueChanged -= OnEffectiveChanged;
            _stats.DEX.OnValueChanged -= OnEffectiveChanged;
            _stats.INT.OnValueChanged -= OnEffectiveChanged;

            _stats.BaseSTR.OnValueChanged -= OnBaseChanged;
            _stats.BaseDEX.OnValueChanged -= OnBaseChanged;
            _stats.BaseINT.OnValueChanged -= OnBaseChanged;
        }

        private void OnEffectiveChanged(int prev, int cur)
        {
            RefreshEffective();
        }

        private void OnBaseChanged(int prev, int cur)
        {
            RefreshBase();
        }

        private void RefreshAll()
        {
            RefreshEffective();
            RefreshBase();
        }

        private void RefreshEffective()
        {
            int str = _stats != null ? _stats.STR.Value : 0;
            int dex = _stats != null ? _stats.DEX.Value : 0;
            int intel = _stats != null ? _stats.INT.Value : 0;

            if (strText != null) strText.text = str.ToString();
            if (dexText != null) dexText.text = dex.ToString();
            if (intText != null) intText.text = intel.ToString();
        }

        private void RefreshBase()
        {
            if (baseStrText == null && baseDexText == null && baseIntText == null)
                return;

            int str = _stats != null ? _stats.BaseSTR.Value : 0;
            int dex = _stats != null ? _stats.BaseDEX.Value : 0;
            int intel = _stats != null ? _stats.BaseINT.Value : 0;

            if (baseStrText != null) baseStrText.text = $"Base {str}";
            if (baseDexText != null) baseDexText.text = $"Base {dex}";
            if (baseIntText != null) baseIntText.text = $"Base {intel}";
        }
    }
}
