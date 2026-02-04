// ============================================================================
// ItemGrantedAbilitiesPanelUI.cs
// ----------------------------------------------------------------------------
// Purpose:
// - UI widget used inside Window_ItemDetails to show "item-granted ability" slots.
// - For each AbilityGrantSlot (Primary / Secondary / Utility) defined on ItemDef,
//   this panel shows a dropdown of allowed SpellIds.
// - Emits an event when the player changes a selection.
//
// IMPORTANT DESIGN CHOICE (locked for MVP):
// - This component does NOT directly mutate ItemInstance.
//   It renders using provided selections and emits OnSelectionChanged.
//
// MVP Compatibility:
// - ItemDetailsPanelUI currently invokes the granted-abilities panel via reflection
//   looking for one of the following method signatures:
//     Bind(ItemDef, ItemInstance)
//     Show(ItemDef, ItemInstance)
//     SetItem(ItemDef, ItemInstance)
//     Render(ItemDef, ItemInstance)
//
// This script therefore provides ALL of those signatures as adapters.
// - The "real" render method remains:
//     Show(ItemDef, IReadOnlyList<SlotSelection>, bool canEdit)
// - The adapter methods simply translate an ItemInstance into SlotSelection[]
//   and call the real render method.
//
// Unity setup:
// - Place this component under Window_ItemDetails.
// - Create up to 3 rows (Primary/Secondary/Utility), each containing:
//     * TMP_Text label
//     * TMP_Dropdown dropdown
// - Wire rows into the serialized fields.
// ============================================================================

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace UltimateDungeon.UI
{
    using UltimateDungeon.Items;
    using UltimateDungeon.Spells;

    [DisallowMultipleComponent]
    public sealed class ItemGrantedAbilitiesPanelUI : MonoBehaviour
    {
        // --------------------------------------------------------------------
        // Public data helpers
        // --------------------------------------------------------------------

        /// <summary>
        /// Passed in by the caller (usually ItemDetailsPanelUI) so we can display
        /// which SpellId is currently selected per slot.
        /// </summary>
        [Serializable]
        public struct SlotSelection
        {
            public AbilityGrantSlot slot;
            public SpellId selectedSpellId;
        }

        /// <summary>
        /// Fired when the user changes a selection.
        /// The caller should persist this to ItemInstance (and later send to server).
        /// </summary>
        public event Action<AbilityGrantSlot, SpellId> OnSelectionChanged;

        /// <summary>
        /// Fired when the user clicks a row to request an active hotbar slot.
        /// </summary>
        public event Action<AbilityGrantSlot> OnActiveSlotRequested;

        // --------------------------------------------------------------------
        // Row references (3 max, because schema is capped at 3)
        // --------------------------------------------------------------------

        [Header("Row: Primary")]
        [SerializeField] private GameObject primaryRowRoot;
        [SerializeField] private TMP_Text primaryLabel;
        [SerializeField] private TMP_Dropdown primaryDropdown;
        [SerializeField] private GameObject primaryActiveHighlight;

        [Header("Row: Secondary")]
        [SerializeField] private GameObject secondaryRowRoot;
        [SerializeField] private TMP_Text secondaryLabel;
        [SerializeField] private TMP_Dropdown secondaryDropdown;
        [SerializeField] private GameObject secondaryActiveHighlight;

        [Header("Row: Utility")]
        [SerializeField] private GameObject utilityRowRoot;
        [SerializeField] private TMP_Text utilityLabel;
        [SerializeField] private TMP_Dropdown utilityDropdown;
        [SerializeField] private GameObject utilityActiveHighlight;

        // --------------------------------------------------------------------
        // Internal state
        // --------------------------------------------------------------------

        /// <summary>
        /// Guard flag to prevent dropdown callbacks from firing while we are
        /// programmatically binding values.
        /// </summary>
        private bool _isBinding;

        // Cache current allowed lists so we can map dropdown indices -> SpellId
        private SpellId[] _primaryAllowed;
        private SpellId[] _secondaryAllowed;
        private SpellId[] _utilityAllowed;

        private void Awake()
        {
            // Start hidden until an item that grants abilities is shown.
            gameObject.SetActive(false);

            // Hook dropdown events once.
            HookDropdown(primaryDropdown, AbilityGrantSlot.Primary);
            HookDropdown(secondaryDropdown, AbilityGrantSlot.Secondary);
            HookDropdown(utilityDropdown, AbilityGrantSlot.Utility);

            HookRowClick(primaryRowRoot, AbilityGrantSlot.Primary);
            HookRowClick(secondaryRowRoot, AbilityGrantSlot.Secondary);
            HookRowClick(utilityRowRoot, AbilityGrantSlot.Utility);
        }

        private void HookDropdown(TMP_Dropdown dd, AbilityGrantSlot slot)
        {
            if (dd == null)
                return;

            // Clear any prior listeners (defensive in case prefab had them).
            dd.onValueChanged.RemoveAllListeners();

            // Add our listener.
            dd.onValueChanged.AddListener(index => HandleDropdownChanged(slot, index));
        }

        private void HookRowClick(GameObject rowRoot, AbilityGrantSlot slot)
        {
            if (rowRoot == null)
                return;

            var handler = rowRoot.GetComponent<AbilityGrantSlotRowClickHandler>();
            if (handler == null)
                handler = rowRoot.AddComponent<AbilityGrantSlotRowClickHandler>();

            handler.Configure(slot, HandleRowClicked);
        }

        // --------------------------------------------------------------------
        // Public API (REAL render method)
        // --------------------------------------------------------------------

        /// <summary>
        /// Shows and populates rows for the given item.
        ///
        /// canEdit:
        /// - Pass false when in combat (selection is locked per design).
        ///
        /// currentSelections:
        /// - The caller provides the currently chosen SpellId per slot
        ///   (usually from ItemInstance + def validation).
        /// - If a slot is missing from currentSelections, we fall back to:
        ///     * def.defaultSpellId (if valid)
        ///     * SpellId.None
        /// </summary>
        public void Show(ItemDef def, IReadOnlyList<SlotSelection> currentSelections, bool canEdit)
        {
            if (def == null)
            {
                Hide();
                return;
            }

            // If item grants no slots, hide the whole panel.
            var slots = def.grantedAbilities.grantedAbilitySlots;
            if (slots == null || slots.Length == 0)
            {
                Hide();
                return;
            }

            gameObject.SetActive(true);

            // Reset all rows to hidden; we enable only the ones present.
            SetRowVisible(AbilityGrantSlot.Primary, false);
            SetRowVisible(AbilityGrantSlot.Secondary, false);
            SetRowVisible(AbilityGrantSlot.Utility, false);

            // Populate rows based on the def.
            for (int i = 0; i < slots.Length; i++)
            {
                var s = slots[i];
                var slot = s.slot;

                // Allowed list (authoring says 1..3, but we handle any length safely).
                var allowed = s.allowedSpellIds ?? Array.Empty<SpellId>();

                // Determine initial selection: caller selection -> default -> None
                SpellId initial = GetCurrentSelection(slot, currentSelections, fallback: SpellId.None);

                // If caller selection isn't allowed, try def default, else None.
                if (!IsAllowed(initial, allowed))
                {
                    if (IsAllowed(s.defaultSpellId, allowed))
                        initial = s.defaultSpellId;
                    else
                        initial = SpellId.None;
                }

                ApplyToRow(slot, allowed, initial, canEdit);
            }
        }

        /// <summary>
        /// UI-only: highlight the active grant slot used for the hotbar.
        /// </summary>
        public void SetActiveGrantSlot(AbilityGrantSlot activeSlot)
        {
            SetActiveHighlight(primaryActiveHighlight, activeSlot == AbilityGrantSlot.Primary);
            SetActiveHighlight(secondaryActiveHighlight, activeSlot == AbilityGrantSlot.Secondary);
            SetActiveHighlight(utilityActiveHighlight, activeSlot == AbilityGrantSlot.Utility);
        }

        /// <summary>
        /// Hides the panel completely.
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        // --------------------------------------------------------------------
        // Public API (ADAPTER METHODS for ItemDetailsPanelUI reflection)
        // --------------------------------------------------------------------

        /// <summary>
        /// Adapter method expected by ItemDetailsPanelUI reflection.
        ///
        /// Converts ItemInstance selections into SlotSelection list and calls
        /// the real Show(def, selections, canEdit).
        /// </summary>
        public void Bind(ItemDef def, ItemInstance instance)
        {
            // If no def, we cannot render.
            if (def == null)
            {
                Hide();
                return;
            }

            // Build a compact selections list.
            // We keep only slots that exist on the def.
            var selections = BuildSelectionsFromInstance(def, instance);

            // MVP: We do not yet have a combat gate wired into UI.
            // For now, allow editing. Later ItemDetailsPanelUI will pass canEdit.
            const bool canEdit = true;

            Show(def, selections, canEdit);
        }

        /// <summary>
        /// Compatibility adapter (same signature expected by reflection).
        /// </summary>
        public void Show(ItemDef def, ItemInstance instance)
        {
            Bind(def, instance);
        }

        /// <summary>
        /// Compatibility adapter (same signature expected by reflection).
        /// </summary>
        public void SetItem(ItemDef def, ItemInstance instance)
        {
            Bind(def, instance);
        }

        /// <summary>
        /// Compatibility adapter (same signature expected by reflection).
        /// </summary>
        public void Render(ItemDef def, ItemInstance instance)
        {
            Bind(def, instance);
        }

        // --------------------------------------------------------------------
        // Row helpers
        // --------------------------------------------------------------------

        private void ApplyToRow(AbilityGrantSlot slot, SpellId[] allowed, SpellId initial, bool canEdit)
        {
            SetRowVisible(slot, true);

            TMP_Text label = null;
            TMP_Dropdown dd = null;

            switch (slot)
            {
                case AbilityGrantSlot.Primary:
                    label = primaryLabel;
                    dd = primaryDropdown;
                    _primaryAllowed = allowed;
                    break;

                case AbilityGrantSlot.Secondary:
                    label = secondaryLabel;
                    dd = secondaryDropdown;
                    _secondaryAllowed = allowed;
                    break;

                case AbilityGrantSlot.Utility:
                    label = utilityLabel;
                    dd = utilityDropdown;
                    _utilityAllowed = allowed;
                    break;
            }

            if (label != null)
                label.text = slot.ToString();

            if (dd == null)
                return;

            _isBinding = true;

            // Build dropdown options.
            // Option 0 is always "None".
            dd.ClearOptions();

            var options = new List<TMP_Dropdown.OptionData>(1 + allowed.Length);
            options.Add(new TMP_Dropdown.OptionData("None"));

            for (int i = 0; i < allowed.Length; i++)
            {
                // MVP: show enum name. Later we can resolve to SpellDef displayName.
                options.Add(new TMP_Dropdown.OptionData(allowed[i].ToString()));
            }

            dd.AddOptions(options);

            // Choose dropdown index matching initial selection.
            int initialIndex = 0; // None
            if (initial != SpellId.None)
            {
                for (int i = 0; i < allowed.Length; i++)
                {
                    if (allowed[i].Equals(initial))
                    {
                        initialIndex = i + 1; // +1 because 0 is None
                        break;
                    }
                }
            }

            dd.value = initialIndex;
            dd.RefreshShownValue();

            // Lock if in combat.
            dd.interactable = canEdit;

            _isBinding = false;
        }

        private void SetRowVisible(AbilityGrantSlot slot, bool visible)
        {
            GameObject root = null;
            GameObject highlight = null;
            switch (slot)
            {
                case AbilityGrantSlot.Primary:
                    root = primaryRowRoot;
                    highlight = primaryActiveHighlight;
                    break;
                case AbilityGrantSlot.Secondary:
                    root = secondaryRowRoot;
                    highlight = secondaryActiveHighlight;
                    break;
                case AbilityGrantSlot.Utility:
                    root = utilityRowRoot;
                    highlight = utilityActiveHighlight;
                    break;
            }

            if (root != null)
                root.SetActive(visible);

            if (!visible)
                SetActiveHighlight(highlight, false);
        }

        // --------------------------------------------------------------------
        // Selection logic
        // --------------------------------------------------------------------

        private void HandleDropdownChanged(AbilityGrantSlot slot, int dropdownIndex)
        {
            if (_isBinding)
                return;

            // Index 0 is None.
            if (dropdownIndex <= 0)
            {
                OnSelectionChanged?.Invoke(slot, SpellId.None);
                return;
            }

            int allowedIndex = dropdownIndex - 1;

            SpellId chosen = SpellId.None;
            switch (slot)
            {
                case AbilityGrantSlot.Primary:
                    chosen = TryGetAllowed(_primaryAllowed, allowedIndex);
                    break;
                case AbilityGrantSlot.Secondary:
                    chosen = TryGetAllowed(_secondaryAllowed, allowedIndex);
                    break;
                case AbilityGrantSlot.Utility:
                    chosen = TryGetAllowed(_utilityAllowed, allowedIndex);
                    break;
            }

            OnSelectionChanged?.Invoke(slot, chosen);
        }

        private void HandleRowClicked(AbilityGrantSlot slot)
        {
            if (_isBinding)
                return;

            Debug.Log($"[AbilityUI] Active slot requested: {slot}");
            OnActiveSlotRequested?.Invoke(slot);
        }

        private static void SetActiveHighlight(GameObject highlight, bool enabled)
        {
            if (highlight == null)
                return;

            highlight.SetActive(enabled);
        }

        private static SpellId TryGetAllowed(SpellId[] allowed, int index)
        {
            if (allowed == null) return SpellId.None;
            if (index < 0 || index >= allowed.Length) return SpellId.None;
            return allowed[index];
        }

        private static bool IsAllowed(SpellId id, SpellId[] allowed)
        {
            if (id == SpellId.None) return true; // Always allow "None"
            if (allowed == null) return false;

            for (int i = 0; i < allowed.Length; i++)
            {
                if (allowed[i].Equals(id))
                    return true;
            }

            return false;
        }

        private static SpellId GetCurrentSelection(AbilityGrantSlot slot, IReadOnlyList<SlotSelection> current, SpellId fallback)
        {
            if (current != null)
            {
                for (int i = 0; i < current.Count; i++)
                {
                    if (current[i].slot == slot)
                        return current[i].selectedSpellId;
                }
            }

            return fallback;
        }

        // --------------------------------------------------------------------
        // Instance -> UI selection adapter
        // --------------------------------------------------------------------

        /// <summary>
        /// Builds a SlotSelection list by reading ItemInstance.grantedAbilitySelections.
        ///
        /// We intentionally call ItemInstance.GetSelectedSpellId(def, slot) so that:
        /// - defaults are respected
        /// - invalid values fall back cleanly
        ///
        /// NOTE:
        /// - This only reads values; it does NOT mutate instance.
        /// </summary>
        private static List<SlotSelection> BuildSelectionsFromInstance(ItemDef def, ItemInstance instance)
        {
            var list = new List<SlotSelection>(3);

            var slots = def.grantedAbilities.grantedAbilitySlots;
            if (slots == null || slots.Length == 0)
                return list;

            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i].slot;

                SpellId selected;
                if (instance != null)
                    selected = instance.GetSelectedSpellId(def, slot);
                else
                    selected = slots[i].defaultSpellId;

                list.Add(new SlotSelection
                {
                    slot = slot,
                    selectedSpellId = selected
                });
            }

            return list;
        }
    }
}
