using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

// -----------------------------------------------------------------------------
// CharacterStatsPanelUI.cs
// -----------------------------------------------------------------------------
// Displays a single consolidated list of player-facing stats.
// -----------------------------------------------------------------------------

namespace UltimateDungeon.UI.Panels
{
    [DisallowMultipleComponent]
    public sealed class CharacterStatsPanelUI : MonoBehaviour
    {
        [Header("Single List UI")]
        [SerializeField] private RectTransform listContent;
        [SerializeField] private TextMeshProUGUI rowPrefab;
        [SerializeField] private bool showIndividualSkills = true;

        [Header("Optional Catalogs")]
        [SerializeField] private UltimateDungeon.Items.AffixCatalog affixCatalog;
        [SerializeField] private UltimateDungeon.StatusEffects.StatusEffectCatalog statusEffectCatalog;

        [Header("Legacy Text (Optional)")]
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

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs;

        private UltimateDungeon.Players.Networking.PlayerStatsNet _statsNet;
        private UltimateDungeon.Players.Networking.PlayerCombatStatsNet _combatStatsNet;
        private UltimateDungeon.Combat.ActorVitals _vitals;
        private UltimateDungeon.Players.Networking.PlayerSkillBookNet _skillBookNet;
        private UltimateDungeon.Players.PlayerCore _playerCore;
        private UltimateDungeon.StatusEffects.IStatusEffectRuntime _statusRuntime;
        private UltimateDungeon.Economy.IPlayerCurrencyWallet _currencyWallet;

        private readonly List<TextMeshProUGUI> _rows = new List<TextMeshProUGUI>();
        private readonly List<UltimateDungeon.StatusEffects.ActiveStatusEffect> _statusBuffer =
            new List<UltimateDungeon.StatusEffects.ActiveStatusEffect>();

        private void OnDisable()
        {
            Unsubscribe();
        }

        /// <summary>
        /// Bind to replicated player stats + combat stats.
        /// </summary>
        public void Bind(
            UltimateDungeon.Players.Networking.PlayerStatsNet statsNet,
            UltimateDungeon.Players.Networking.PlayerCombatStatsNet combatStatsNet,
            UltimateDungeon.Combat.ActorVitals vitals,
            UltimateDungeon.Players.Networking.PlayerSkillBookNet skillBookNet,
            UltimateDungeon.Players.PlayerCore playerCore,
            UltimateDungeon.StatusEffects.IStatusEffectRuntime statusRuntime,
            UltimateDungeon.Economy.IPlayerCurrencyWallet currencyWallet)
        {
            Unsubscribe();

            _statsNet = statsNet;
            _combatStatsNet = combatStatsNet;
            _vitals = vitals;
            _skillBookNet = skillBookNet;
            _playerCore = playerCore;
            _statusRuntime = statusRuntime;
            _currencyWallet = currencyWallet;

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
                _statsNet.BaseSTR.OnValueChanged += OnStatChanged;
                _statsNet.BaseDEX.OnValueChanged += OnStatChanged;
                _statsNet.BaseINT.OnValueChanged += OnStatChanged;
                _statsNet.MaxHP.OnValueChanged += OnStatChanged;
                _statsNet.MaxStamina.OnValueChanged += OnStatChanged;
                _statsNet.MaxMana.OnValueChanged += OnStatChanged;
                _statsNet.HpRegenPerSec.OnValueChanged += OnRegenChanged;
                _statsNet.StaminaRegenPerSec.OnValueChanged += OnRegenChanged;
                _statsNet.ManaRegenPerSec.OnValueChanged += OnRegenChanged;
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

            if (_vitals != null)
            {
                _vitals.CurrentHPNet.OnValueChanged += OnVitalChanged;
                _vitals.MaxHPNet.OnValueChanged += OnVitalChanged;
                _vitals.CurrentStaminaNet.OnValueChanged += OnVitalChanged;
                _vitals.MaxStaminaNet.OnValueChanged += OnVitalChanged;
                _vitals.CurrentManaNet.OnValueChanged += OnVitalChanged;
                _vitals.MaxManaNet.OnValueChanged += OnVitalChanged;
            }

            if (_skillBookNet != null)
                _skillBookNet.Skills.OnListChanged += OnSkillListChanged;

            if (_statusRuntime != null)
                _statusRuntime.StatusesChanged += OnStatusesChanged;

            if (_currencyWallet != null)
                _currencyWallet.CurrencyChanged += OnCurrencyChanged;
        }

        private void Unsubscribe()
        {
            if (_statsNet != null)
            {
                _statsNet.STR.OnValueChanged -= OnStatChanged;
                _statsNet.DEX.OnValueChanged -= OnStatChanged;
                _statsNet.INT.OnValueChanged -= OnStatChanged;
                _statsNet.BaseSTR.OnValueChanged -= OnStatChanged;
                _statsNet.BaseDEX.OnValueChanged -= OnStatChanged;
                _statsNet.BaseINT.OnValueChanged -= OnStatChanged;
                _statsNet.MaxHP.OnValueChanged -= OnStatChanged;
                _statsNet.MaxStamina.OnValueChanged -= OnStatChanged;
                _statsNet.MaxMana.OnValueChanged -= OnStatChanged;
                _statsNet.HpRegenPerSec.OnValueChanged -= OnRegenChanged;
                _statsNet.StaminaRegenPerSec.OnValueChanged -= OnRegenChanged;
                _statsNet.ManaRegenPerSec.OnValueChanged -= OnRegenChanged;
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

            if (_vitals != null)
            {
                _vitals.CurrentHPNet.OnValueChanged -= OnVitalChanged;
                _vitals.MaxHPNet.OnValueChanged -= OnVitalChanged;
                _vitals.CurrentStaminaNet.OnValueChanged -= OnVitalChanged;
                _vitals.MaxStaminaNet.OnValueChanged -= OnVitalChanged;
                _vitals.CurrentManaNet.OnValueChanged -= OnVitalChanged;
                _vitals.MaxManaNet.OnValueChanged -= OnVitalChanged;
            }

            if (_skillBookNet != null)
                _skillBookNet.Skills.OnListChanged -= OnSkillListChanged;

            if (_statusRuntime != null)
                _statusRuntime.StatusesChanged -= OnStatusesChanged;

            if (_currencyWallet != null)
                _currencyWallet.CurrencyChanged -= OnCurrencyChanged;
        }

        private void OnStatChanged(int previous, int current)
        {
            if (enableDebugLogs)
                Debug.Log($"[CharacterStatsPanelUI] Stat changed: {previous} -> {current}");

            Refresh();
        }

        private void OnRegenChanged(float previous, float current)
        {
            if (enableDebugLogs)
                Debug.Log($"[CharacterStatsPanelUI] Regen changed: {previous:0.##} -> {current:0.##}");

            Refresh();
        }
        private void OnCombatIntChanged(int previous, int current) => Refresh();
        private void OnCombatFloatChanged(float previous, float current) => Refresh();
        private void OnCombatBoolChanged(bool previous, bool current) => Refresh();
        private void OnWeaponNameChanged(FixedString64Bytes previous, FixedString64Bytes current) => Refresh();
        private void OnVitalChanged(int previous, int current) => Refresh();
        private void OnSkillListChanged(NetworkListEvent<UltimateDungeon.Players.Networking.SkillNetState> changeEvent) => Refresh();
        private void OnStatusesChanged() => Refresh();
        private void OnCurrencyChanged() => Refresh();

        public void Refresh()
        {
            if (listContent != null && rowPrefab != null)
            {
                RefreshSingleList();
                if (enableDebugLogs)
                    Debug.Log("[CharacterStatsPanelUI] Refreshed after stats change.");
                return;
            }

            RefreshLegacy();
        }

        private void RefreshSingleList()
        {
            ClearRows();

            var definition = _playerCore != null ? _playerCore.definition : null;

            AddHeader("Attributes");
            AddRow(BuildAttributeLine("Strength", _statsNet != null ? _statsNet.BaseSTR.Value : (int?)null, _statsNet != null ? _statsNet.STR.Value : (int?)null));
            AddRow(BuildAttributeLine("Dexterity", _statsNet != null ? _statsNet.BaseDEX.Value : (int?)null, _statsNet != null ? _statsNet.DEX.Value : (int?)null));
            AddRow(BuildAttributeLine("Intelligence", _statsNet != null ? _statsNet.BaseINT.Value : (int?)null, _statsNet != null ? _statsNet.INT.Value : (int?)null));

            AddHeader("Vitals");
            int? maxHp = _statsNet != null ? _statsNet.MaxHP.Value : (_vitals != null ? _vitals.MaxHPNet.Value : (int?)null);
            int? maxStamina = _statsNet != null ? _statsNet.MaxStamina.Value : (_vitals != null ? _vitals.MaxStaminaNet.Value : (int?)null);
            int? maxMana = _statsNet != null ? _statsNet.MaxMana.Value : (_vitals != null ? _vitals.MaxManaNet.Value : (int?)null);

            AddRow(BuildVitalLine("Health", _vitals != null ? _vitals.CurrentHPNet.Value : (int?)null, maxHp));
            AddRow(BuildVitalLine("Stamina", _vitals != null ? _vitals.CurrentStaminaNet.Value : (int?)null, maxStamina));
            AddRow(BuildVitalLine("Mana", _vitals != null ? _vitals.CurrentManaNet.Value : (int?)null, maxMana));

            AddHeader("Regeneration");
            AddRow(BuildRegenLine("Health", _statsNet != null ? _statsNet.HpRegenPerSec.Value : (float?)null));
            AddRow(BuildRegenLine("Stamina", _statsNet != null ? _statsNet.StaminaRegenPerSec.Value : (float?)null));
            AddRow(BuildRegenLine("Mana", _statsNet != null ? _statsNet.ManaRegenPerSec.Value : (float?)null));

            AddHeader("Resistances");
            int? resistCap = definition != null ? definition.resistanceCap : (int?)null;
            AddRow(BuildResistLine("Physical", _combatStatsNet != null ? _combatStatsNet.ResistPhysical.Value : (int?)null, resistCap));
            AddRow(BuildResistLine("Fire", _combatStatsNet != null ? _combatStatsNet.ResistFire.Value : (int?)null, resistCap));
            AddRow(BuildResistLine("Cold", _combatStatsNet != null ? _combatStatsNet.ResistCold.Value : (int?)null, resistCap));
            AddRow(BuildResistLine("Poison", _combatStatsNet != null ? _combatStatsNet.ResistPoison.Value : (int?)null, resistCap));
            AddRow(BuildResistLine("Energy", _combatStatsNet != null ? _combatStatsNet.ResistEnergy.Value : (int?)null, resistCap));

            AddHeader("Combat");
            AddRow(BuildPercentLine("Hit Chance Increase", _combatStatsNet != null ? _combatStatsNet.AttackerHciPct.Value : (float?)null));
            AddRow(BuildPercentLine("Defense Chance Increase", _combatStatsNet != null ? _combatStatsNet.DefenderDciPct.Value : (float?)null));
            AddRow(BuildPercentLine("Damage Increase", _combatStatsNet != null ? _combatStatsNet.DamageIncreasePct.Value : (float?)null));
            AddRow(BuildWeaponLine());

            AddHeader("Skills");
            AddRow(BuildSkillSummaryLine(definition));
            if (showIndividualSkills)
                AddSkillRows();

            AddHeader("Currency");
            AddRow(BuildCurrencyLine("Held Coins", _currencyWallet != null ? _currencyWallet.HeldCoins : (int?)null));
            AddRow(BuildCurrencyLine("Banked Coins", _currencyWallet != null ? _currencyWallet.BankedCoins : (int?)null));

            AddHeader("Active Status Effects");
            AddStatusRows();

            AddHeader("Possible Affixes");
            AddAffixRows();
        }

        private void RefreshLegacy()
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

        private void ClearRows()
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                if (_rows[i] != null)
                    Destroy(_rows[i].gameObject);
            }

            _rows.Clear();
        }

        private void AddHeader(string title)
        {
            AddRow($"<b>{title}</b>");
        }

        private void AddRow(string text)
        {
            if (rowPrefab == null || listContent == null)
                return;

            var row = Instantiate(rowPrefab, listContent);
            row.gameObject.SetActive(true);
            row.text = text;
            _rows.Add(row);
        }

        private void AddSkillRows()
        {
            if (_skillBookNet == null)
            {
                AddRow("(No skill data)");
                return;
            }

            var skills = new List<UltimateDungeon.Players.Networking.SkillNetState>();
            foreach (var skill in _skillBookNet.Skills)
                skills.Add(skill);

            skills.Sort((a, b) => a.skillId.CompareTo(b.skillId));

            for (int i = 0; i < skills.Count; i++)
            {
                var state = skills[i];
                var id = (UltimateDungeon.Skills.SkillId)state.skillId;
                string name = id.ToString();
                string value = state.value.ToString("0.0", CultureInfo.InvariantCulture);
                AddRow($"{name}: {value}");
            }
        }

        private void AddStatusRows()
        {
            if (_statusRuntime == null)
            {
                AddRow("None");
                return;
            }

            _statusBuffer.Clear();
            int count = _statusRuntime.GetActiveStatuses(_statusBuffer);

            if (count <= 0)
            {
                AddRow("None");
                return;
            }

            for (int i = 0; i < _statusBuffer.Count; i++)
            {
                var status = _statusBuffer[i];
                string name = ResolveStatusName(status.Id);
                string duration = status.RemainingSeconds >= 0f
                    ? $"{status.RemainingSeconds:0.#}s"
                    : "-";
                string stacks = status.Stacks > 1 ? $" (x{status.Stacks})" : string.Empty;
                AddRow($"{name}: {duration}{stacks}");
            }
        }

        private void AddAffixRows()
        {
            if (affixCatalog == null || affixCatalog.All.Count == 0)
            {
                AddRow("(No affix catalog)");
                return;
            }

            var defs = affixCatalog.All;
            for (int i = 0; i < defs.Count; i++)
            {
                var def = defs[i];
                if (def == null)
                    continue;

                string displayName = string.IsNullOrWhiteSpace(def.displayName) ? def.id.ToString() : def.displayName;
                AddRow($"{displayName} ({def.id})");
            }
        }

        private string BuildAttributeLine(string label, int? baseValue, int? effectiveValue)
        {
            string baseText = FormatInt(baseValue);
            string effectiveText = FormatInt(effectiveValue);

            if (baseValue.HasValue && effectiveValue.HasValue)
                return $"{label}: {baseText} (Effective {effectiveText})";

            if (baseValue.HasValue)
                return $"{label}: {baseText}";

            if (effectiveValue.HasValue)
                return $"{label}: {effectiveText}";

            return $"{label}: -";
        }

        private static string BuildVitalLine(string label, int? current, int? max)
        {
            string currentText = FormatInt(current);
            string maxText = FormatInt(max);
            return $"{label}: {currentText} / {maxText}";
        }

        private static string BuildRegenLine(string label, float? value)
        {
            string regenText = value.HasValue
                ? value.Value.ToString("0.##", CultureInfo.InvariantCulture)
                : "-";
            return $"{label} Regen: {regenText} / sec";
        }

        private static string BuildResistLine(string label, int? value, int? cap)
        {
            string valueText = FormatInt(value);
            string capText = FormatInt(cap ?? 70);
            return $"{label} Resist: {valueText} / {capText}";
        }

        private static string BuildPercentLine(string label, float? value)
        {
            string percent = FormatPercent(value);
            return $"{label}: {percent}";
        }

        private static string BuildCurrencyLine(string label, int? value)
        {
            string amount = FormatInt(value);
            return $"{label}: {amount}";
        }

        private string BuildSkillSummaryLine(UltimateDungeon.Players.PlayerDefinition definition)
        {
            if (_skillBookNet == null)
                return "Total Skills: -";

            float total = 0f;
            foreach (var skill in _skillBookNet.Skills)
                total += skill.value;

            string totalText = total.ToString("0.0", CultureInfo.InvariantCulture);
            string capText = definition != null
                ? definition.totalSkillCap.ToString(CultureInfo.InvariantCulture)
                : "-";

            return $"Total Skills: {totalText} / {capText}";
        }

        private string ResolveStatusName(UltimateDungeon.StatusEffects.StatusEffectId id)
        {
            if (statusEffectCatalog != null && statusEffectCatalog.TryGet(id, out var def) && def != null)
                return string.IsNullOrWhiteSpace(def.displayName) ? id.ToString() : def.displayName;

            return id.ToString().Replace('_', ' ');
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
