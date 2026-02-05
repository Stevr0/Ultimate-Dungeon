using System;
using System.Globalization;
using System.Reflection;
using UnityEngine;
using TMPro;

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

        public void Refresh(UltimateDungeon.Players.PlayerStats stats, UltimateDungeon.Combat.PlayerCombatStats combatStats)
        {
            string strValue = stats != null ? stats.STR.ToString() : "-";
            string dexValue = stats != null ? stats.DEX.ToString() : "-";
            string intValue = stats != null ? stats.INT.ToString() : "-";

            SetText(strText, $"STR: {strValue}");
            SetText(dexText, $"DEX: {dexValue}");
            SetText(intText, $"INT: {intValue}");

            string hciValue = FormatPercent(GetFloat(combatStats, "attackerHciPct"));
            string dciValue = FormatPercent(GetFloat(combatStats, "defenderDciPct"));
            string diValue = FormatPercent(GetFloat(combatStats, "damageIncreasePct"));

            SetText(hciText, $"HCI: {hciValue}");
            SetText(dciText, $"DCI: {dciValue}");
            SetText(diText, $"DI: {diValue}");

            string resistPhysical = FormatInt(GetInt(combatStats, "resistPhysical"));
            string resistFire = FormatInt(GetInt(combatStats, "resistFire"));
            string resistCold = FormatInt(GetInt(combatStats, "resistCold"));
            string resistPoison = FormatInt(GetInt(combatStats, "resistPoison"));
            string resistEnergy = FormatInt(GetInt(combatStats, "resistEnergy"));

            SetText(resistsText,
                $"Resists: {resistPhysical} / {resistFire} / {resistCold} / {resistPoison} / {resistEnergy}");

            SetText(weaponText, BuildWeaponLine(combatStats));

            string canAttack = FormatBool(GetBool(combatStats, "canAttack"));
            string canCast = FormatBool(GetBool(combatStats, "canCast"));
            string canMove = FormatBool(GetBool(combatStats, "canMove"));
            string canBandage = FormatBool(GetBool(combatStats, "canBandage"));

            SetText(actionGatesText,
                $"Gates: canAttack={canAttack} | canCast={canCast} | canMove={canMove} | canBandage={canBandage}");

            string source = combatStats != null ? "Server (authoritative)" : "Local/Unknown";
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

        private static float? GetFloat(object source, string memberName)
        {
            if (!TryGetMemberValue(source, memberName, out var raw) || raw == null)
                return null;

            if (raw is float floatValue) return floatValue;
            if (raw is double doubleValue) return (float)doubleValue;
            if (raw is int intValue) return intValue;
            if (raw is long longValue) return longValue;
            if (raw is short shortValue) return shortValue;

            if (float.TryParse(raw.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
                return parsed;

            return null;
        }

        private static int? GetInt(object source, string memberName)
        {
            if (!TryGetMemberValue(source, memberName, out var raw) || raw == null)
                return null;

            if (raw is int intValue) return intValue;
            if (raw is short shortValue) return shortValue;
            if (raw is long longValue) return (int)longValue;
            if (raw is float floatValue) return Mathf.RoundToInt(floatValue);
            if (raw is double doubleValue) return Mathf.RoundToInt((float)doubleValue);

            if (int.TryParse(raw.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
                return parsed;

            return null;
        }

        private static bool? GetBool(object source, string memberName)
        {
            if (!TryGetMemberValue(source, memberName, out var raw) || raw == null)
                return null;

            if (raw is bool boolValue) return boolValue;

            if (bool.TryParse(raw.ToString(), out bool parsed))
                return parsed;

            return null;
        }

        private static string BuildWeaponLine(object combatStats)
        {
            object weapon = GetMemberValue(combatStats, "weapon");
            if (weapon == null)
                return "Weapon: Unarmed";

            string name = GetFirstStringMember(weapon, "displayName", "name", "weaponName", "itemName");
            string minDamage = FormatInt(GetInt(weapon, "minDamage"));
            string maxDamage = FormatInt(GetInt(weapon, "maxDamage"));
            string swingSpeed = FormatSwingSpeed(GetFloat(weapon, "swingSpeedSeconds"));

            string displayName = string.IsNullOrWhiteSpace(name) ? "Unarmed" : name;
            string damageRange = minDamage == "-" || maxDamage == "-" ? "-" : $"{minDamage}-{maxDamage}";

            return $"Weapon: {displayName} | DMG {damageRange} | Swing {swingSpeed}";
        }

        private static string FormatSwingSpeed(float? value)
        {
            if (!value.HasValue) return "-";
            return $"{value.Value:0.##}s";
        }

        private static bool TryGetMemberValue(object source, string memberName, out object value)
        {
            value = null;
            if (source == null) return false;

            Type type = source.GetType();
            PropertyInfo prop = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.Instance);
            if (prop != null)
            {
                value = prop.GetValue(source);
                return true;
            }

            FieldInfo field = type.GetField(memberName, BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
            {
                value = field.GetValue(source);
                return true;
            }

            return false;
        }

        private static object GetMemberValue(object source, string memberName)
        {
            return TryGetMemberValue(source, memberName, out var value) ? value : null;
        }

        private static string GetFirstStringMember(object source, params string[] memberNames)
        {
            if (source == null) return null;

            foreach (string memberName in memberNames)
            {
                if (TryGetMemberValue(source, memberName, out var raw) && raw != null)
                {
                    string text = raw.ToString();
                    if (!string.IsNullOrWhiteSpace(text))
                        return text;
                }
            }

            return null;
        }
    }
}
