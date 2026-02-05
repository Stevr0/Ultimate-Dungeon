using System.Globalization;
using UnityEngine;
using TMPro;
using Unity.Collections;

// -----------------------------------------------------------------------------
// CharacterStatsPanelUI.cs
// -----------------------------------------------------------------------------
// Displays STR/DEX/INT and combat debug stats.
// -----------------------------------------------------------------------------

namespace UltimateDungeon.UI.Panels
{
    [DisallowMultipleComponent]
    public sealed class CharacterStatsPanelUI : MonoBehaviour
    {
        [Header("Text")]
        [SerializeField] private TextMeshProUGUI strText;
        [SerializeField] private TextMeshProUGUI dexText;
        [SerializeField] private TextMeshProUGUI intText;
        [SerializeField] private TextMeshProUGUI hciText;
        [SerializeField] private TextMeshProUGUI dciText;
        [SerializeField] private TextMeshProUGUI diText;
        [SerializeField] private TextMeshProUGUI resistsText;
        [SerializeField] private TextMeshProUGUI weaponText;
        [SerializeField] private TextMeshProUGUI actionGatesText;
        [SerializeField] private TextMeshProUGUI debugFooterText;

        private UltimateDungeon.Players.Networking.PlayerStatsNet _statsNet;
        private UltimateDungeon.Players.Networking.PlayerCombatStatsNet _combatStatsNet;

        private void OnDisable()
        {
            Unsubscribe();
        }

        /// <summary>
        /// Bind to replicated player stats + combat stats.
        /// </summary>
        public void Bind(
            UltimateDungeon.Players.Networking.PlayerStatsNet statsNet,
            UltimateDungeon.Players.Networking.PlayerCombatStatsNet combatStatsNet)
        {
            Unsubscribe();

            _statsNet = statsNet;
            _combatStatsNet = combatStatsNet;

            Subscribe();
            Refresh();
        }

        private void Subscribe()
        {
            if (_statsNet != null)
            {
                _statsNet.STR.OnValueChanged += OnStatChanged;
                _statsNet.DEX.OnValueChanged += OnStatChanged;
                _statsNet.INT.OnValueChanged += OnStatChanged;
            }

            if (_combatStatsNet != null)
            {
                _combatStatsNet.AttackerHciPct.OnValueChanged += OnCombatFloatChanged;
                _combatStatsNet.DefenderDciPct.OnValueChanged += OnCombatFloatChanged;
                _combatStatsNet.DamageIncreasePct.OnValueChanged += OnCombatFloatChanged;

                _combatStatsNet.ResistPhysical.OnValueChanged += OnCombatIntChanged;
                _combatStatsNet.ResistFire.OnValueChanged += OnCombatIntChanged;
                _combatStatsNet.ResistCold.OnValueChanged += OnCombatIntChanged;
                _combatStatsNet.ResistPoison.OnValueChanged += OnCombatIntChanged;
                _combatStatsNet.ResistEnergy.OnValueChanged += OnCombatIntChanged;

                _combatStatsNet.WeaponName.OnValueChanged += OnWeaponNameChanged;
                _combatStatsNet.WeaponMinDamage.OnValueChanged += OnCombatIntChanged;
                _combatStatsNet.WeaponMaxDamage.OnValueChanged += OnCombatIntChanged;
                _combatStatsNet.WeaponSwingSpeedSeconds.OnValueChanged += OnCombatFloatChanged;

                _combatStatsNet.CanAttack.OnValueChanged += OnCombatBoolChanged;
                _combatStatsNet.CanCast.OnValueChanged += OnCombatBoolChanged;
                _combatStatsNet.CanMove.OnValueChanged += OnCombatBoolChanged;
                _combatStatsNet.CanBandage.OnValueChanged += OnCombatBoolChanged;
            }
        }

        private void Unsubscribe()
        {
            if (_statsNet != null)
            {
                _statsNet.STR.OnValueChanged -= OnStatChanged;
                _statsNet.DEX.OnValueChanged -= OnStatChanged;
                _statsNet.INT.OnValueChanged -= OnStatChanged;
            }

            if (_combatStatsNet != null)
            {
                _combatStatsNet.AttackerHciPct.OnValueChanged -= OnCombatFloatChanged;
                _combatStatsNet.DefenderDciPct.OnValueChanged -= OnCombatFloatChanged;
                _combatStatsNet.DamageIncreasePct.OnValueChanged -= OnCombatFloatChanged;

                _combatStatsNet.ResistPhysical.OnValueChanged -= OnCombatIntChanged;
                _combatStatsNet.ResistFire.OnValueChanged -= OnCombatIntChanged;
                _combatStatsNet.ResistCold.OnValueChanged -= OnCombatIntChanged;
                _combatStatsNet.ResistPoison.OnValueChanged -= OnCombatIntChanged;
                _combatStatsNet.ResistEnergy.OnValueChanged -= OnCombatIntChanged;

                _combatStatsNet.WeaponName.OnValueChanged -= OnWeaponNameChanged;
                _combatStatsNet.WeaponMinDamage.OnValueChanged -= OnCombatIntChanged;
                _combatStatsNet.WeaponMaxDamage.OnValueChanged -= OnCombatIntChanged;
                _combatStatsNet.WeaponSwingSpeedSeconds.OnValueChanged -= OnCombatFloatChanged;

                _combatStatsNet.CanAttack.OnValueChanged -= OnCombatBoolChanged;
                _combatStatsNet.CanCast.OnValueChanged -= OnCombatBoolChanged;
                _combatStatsNet.CanMove.OnValueChanged -= OnCombatBoolChanged;
                _combatStatsNet.CanBandage.OnValueChanged -= OnCombatBoolChanged;
            }
        }

        private void OnStatChanged(int previous, int current) => Refresh();
        private void OnCombatIntChanged(int previous, int current) => Refresh();
        private void OnCombatFloatChanged(float previous, float current) => Refresh();
        private void OnCombatBoolChanged(bool previous, bool current) => Refresh();
        private void OnWeaponNameChanged(FixedString64Bytes previous, FixedString64Bytes current) => Refresh();

        public void Refresh()
        {
            string strValue = _statsNet != null ? _statsNet.STR.Value.ToString() : "-";
            string dexValue = _statsNet != null ? _statsNet.DEX.Value.ToString() : "-";
            string intValue = _statsNet != null ? _statsNet.INT.Value.ToString() : "-";

            SetText(strText, $"STR: {strValue}");
            SetText(dexText, $"DEX: {dexValue}");
            SetText(intText, $"INT: {intValue}");

            string hciValue = FormatPercent(_combatStatsNet != null ? _combatStatsNet.AttackerHciPct.Value : (float?)null);
            string dciValue = FormatPercent(_combatStatsNet != null ? _combatStatsNet.DefenderDciPct.Value : (float?)null);
            string diValue = FormatPercent(_combatStatsNet != null ? _combatStatsNet.DamageIncreasePct.Value : (float?)null);

            SetText(hciText, $"HCI: {hciValue}");
            SetText(dciText, $"DCI: {dciValue}");
            SetText(diText, $"DI: {diValue}");

            string resistPhysical = FormatInt(_combatStatsNet != null ? _combatStatsNet.ResistPhysical.Value : (int?)null);
            string resistFire = FormatInt(_combatStatsNet != null ? _combatStatsNet.ResistFire.Value : (int?)null);
            string resistCold = FormatInt(_combatStatsNet != null ? _combatStatsNet.ResistCold.Value : (int?)null);
            string resistPoison = FormatInt(_combatStatsNet != null ? _combatStatsNet.ResistPoison.Value : (int?)null);
            string resistEnergy = FormatInt(_combatStatsNet != null ? _combatStatsNet.ResistEnergy.Value : (int?)null);

            SetText(resistsText,
                $"Resists: {resistPhysical} / {resistFire} / {resistCold} / {resistPoison} / {resistEnergy}");

            SetText(weaponText, BuildWeaponLine());

            string canAttack = FormatBool(_combatStatsNet != null ? _combatStatsNet.CanAttack.Value : (bool?)null);
            string canCast = FormatBool(_combatStatsNet != null ? _combatStatsNet.CanCast.Value : (bool?)null);
            string canMove = FormatBool(_combatStatsNet != null ? _combatStatsNet.CanMove.Value : (bool?)null);
            string canBandage = FormatBool(_combatStatsNet != null ? _combatStatsNet.CanBandage.Value : (bool?)null);

            SetText(actionGatesText,
                $"Gates: canAttack={canAttack} | canCast={canCast} | canMove={canMove} | canBandage={canBandage}");

            string source = _combatStatsNet != null ? "Net Snapshot (Server)" : "-";
            SetText(debugFooterText, $"Stats Source: {source}");
        }

        private static void SetText(TextMeshProUGUI label, string value)
        {
            if (label != null) label.text = value;
        }

        private static string FormatPercent(float? value)
        {
            if (!value.HasValue) return "-";
            int percent = Mathf.RoundToInt(value.Value * 100f);
            return $"{percent}%";
        }

        private static string FormatInt(int? value)
        {
            return value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "-";
        }

        private static string FormatBool(bool? value)
        {
            if (!value.HasValue) return "-";
            return value.Value ? "Y" : "N";
        }

        private string BuildWeaponLine()
        {
            if (_combatStatsNet == null)
                return "Weapon: -";

            string weaponName = _combatStatsNet.WeaponName.Value.ToString();
            string displayName = string.IsNullOrWhiteSpace(weaponName) ? "Unarmed" : weaponName;

            int minDamage = _combatStatsNet.WeaponMinDamage.Value;
            int maxDamage = _combatStatsNet.WeaponMaxDamage.Value;
            float swingSpeed = _combatStatsNet.WeaponSwingSpeedSeconds.Value;

            string damageRange = (minDamage == 0 && maxDamage == 0) ? "-" : $"{minDamage}-{maxDamage}";
            string swingText = swingSpeed > 0f ? $"{swingSpeed:0.##}s" : "-";

            return $"Weapon: {displayName} | DMG {damageRange} | Swing {swingText}";
        }
    }
}
