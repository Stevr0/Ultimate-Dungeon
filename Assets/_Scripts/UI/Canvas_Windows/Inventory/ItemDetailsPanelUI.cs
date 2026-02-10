// ============================================================================
// ItemDetailsPanelUI.cs
// ----------------------------------------------------------------------------
// Purpose:
// - Displays detailed information about a clicked inventory/equipment item.
// - Albion-style "inspect panel" (first step):
//     * Icon
//     * Name
//     * Family / slot
//     * Weight
//     * Durability
//     * Basic stats (from ItemDef / ItemInstance)
//     * (NEW) Invokes the optional ItemGrantedAbilitiesPanelUI (if present)
//
// IMPORTANT:
// - This panel is UI-only.
// - It does NOT directly mutate game state; equipped-item changes are sent to the server.
// - It does NOT decide what abilities are allowed; it only passes ItemDef/ItemInstance
//   to the child "granted abilities" panel if you wired one.
//
// Why the "optional invoke" approach?
// - You may still be iterating on ItemGrantedAbilitiesPanelUI's public API.
// - To avoid compile churn, this script calls common method names via reflection.
// - Once the API is locked, you can replace the reflection call with a strongly typed
//   reference (recommended).
//
// Setup:
// - Create a Panel prefab (right side of inventory is ideal).
// - Wire the serialized fields below.
// - (Optional) Drag your ItemGrantedAbilitiesPanelUI component into
//   "Granted Abilities Panel".
// - Panel starts hidden.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UltimateDungeon.Actors;
using UltimateDungeon.Items;
using UltimateDungeon.Spells;
using UltimateDungeon.UI.Hotbar;

namespace UltimateDungeon.UI
{
    [DisallowMultipleComponent]
    public sealed class ItemDetailsPanelUI : MonoBehaviour
    {
        [Header("Core UI")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text familyText;

        [Header("Meta")]
        [SerializeField] private TMP_Text weightText;
        [SerializeField] private TMP_Text durabilityText;

        [Header("Stats (simple list)")]
        [SerializeField] private TMP_Text statsText;

        [Header("Affixes")]
        [SerializeField] private AffixCatalog affixCatalog;
        [SerializeField] private TMP_Text affixesHeaderText;
        [SerializeField] private Transform affixesContainer;
        [SerializeField] private TMP_Text affixRowPrefab;

        [Header("Granted Abilities (Item -> Spell selection)")]
        [Tooltip(
            "Optional. If assigned, this script will try to call a method on the component\n" +
            "to render item-granted abilities (Primary/Secondary/Utility etc).\n\n" +
            "Supported method names (first match wins):\n" +
            "- Bind(ItemDef, ItemInstance)\n" +
            "- Show(ItemDef, ItemInstance)\n" +
            "- SetItem(ItemDef, ItemInstance)\n" +
            "- Render(ItemDef, ItemInstance)\n\n" +
            "If your component uses a different signature, either add one of the above\n" +
            "methods, or later replace the reflection with a typed call.")]
        [SerializeField] private MonoBehaviour ItemGrantedAbilitiesPanelUI;

        [Header("Window Controls")]
        [Tooltip("Optional cancel/close button that closes the parent Window_ItemDetails.")]
        [SerializeField] private Button cancelButton;

        [Tooltip("Optional explicit UIWindow reference. If omitted, we will search in parents.")]
        [SerializeField] private UIWindow targetWindow;

        private ItemDefCatalog _catalog;

        // Current item being shown (so we can persist UI changes)
        private ItemDef _currentDef;
        private ItemInstance _currentInstance;
        private EquipmentSlotId? _currentEquipmentSlot;
        private readonly List<TMP_Text> _affixRows = new List<TMP_Text>();

        // Typed reference to the granted abilities panel (if you can)
        // If you're still using MonoBehaviour + reflection, keep that,
        // but ALSO add this typed field so we can subscribe to events.
        [SerializeField] private ItemGrantedAbilitiesPanelUI grantedAbilitiesPanelTyped;

        [Header("Hotbar Icon Updates (Optional)")]
        [SerializeField] private HotbarSpellIconBinder hotbarIconBinder;

        private PlayerEquipmentComponent _equipment;
        private ActorComponent _actor;

        private void Awake()
        {
            if (targetWindow == null)
                targetWindow = GetComponentInParent<UIWindow>();

            Hide();
        }

        private void OnEnable()
        {
            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveListener(HandleCancelClicked);
                cancelButton.onClick.AddListener(HandleCancelClicked);
            }

            if (grantedAbilitiesPanelTyped != null)
            {
                grantedAbilitiesPanelTyped.OnSelectionChanged += HandleAbilitySelectionChanged;
                grantedAbilitiesPanelTyped.OnActiveSlotRequested += HandleActiveSlotRequested;
            }

            if (hotbarIconBinder == null)
                hotbarIconBinder = FindFirstObjectByType<HotbarSpellIconBinder>(FindObjectsInactive.Include);

            PlayerNetIdentity.LocalPlayerSpawned += HandleLocalPlayerSpawned;
            if (PlayerNetIdentity.Local != null)
                HandleLocalPlayerSpawned(PlayerNetIdentity.Local);
        }

        private void OnDisable()
        {
            if (cancelButton != null)
                cancelButton.onClick.RemoveListener(HandleCancelClicked);

            if (grantedAbilitiesPanelTyped != null)
            {
                grantedAbilitiesPanelTyped.OnSelectionChanged -= HandleAbilitySelectionChanged;
                grantedAbilitiesPanelTyped.OnActiveSlotRequested -= HandleActiveSlotRequested;
            }

            PlayerNetIdentity.LocalPlayerSpawned -= HandleLocalPlayerSpawned;
            UnbindEquipment();
        }

        public void BindCatalog(ItemDefCatalog catalog)
        {
            _catalog = catalog;
        }

        // --------------------------------------------------------------------
        // Public API
        // --------------------------------------------------------------------

        public void ShowFromInventory(ItemInstance instance)
        {
            // Inventory path: resolve ItemDef via the catalog.
            if (instance == null || _catalog == null)
                return;

            if (!_catalog.TryGet(instance.itemDefId, out var def) || def == null)
                return;

            Show(def, instance, equipmentSlot: null);
        }

        public void ShowFromEquipment(ItemDef def, ItemInstance instance = null)
        {
            // Equipment path: the binder already knows the ItemDef.
            if (def == null)
                return;

            Show(def, instance, equipmentSlot: null);
        }

        public void ShowFromEquipment(EquipmentSlotId slotId, ItemDef def, ItemInstance instance = null)
        {
            // Equipment path with known slot (used for hotbar icon refresh).
            if (def == null)
                return;

            Show(def, instance, slotId);
        }

        public void Hide()
        {
            // Hide the whole details panel
            gameObject.SetActive(false);
            _currentEquipmentSlot = null;

            // Also hide the granted-abilities panel (if present)
            TryInvokeHide(ItemGrantedAbilitiesPanelUI);

            if (grantedAbilitiesPanelTyped != null)
                grantedAbilitiesPanelTyped.Hide();
        }

        private void HandleCancelClicked()
        {
            if (targetWindow != null)
            {
                targetWindow.RequestClose();
                return;
            }

            gameObject.SetActive(false);
        }


        // --------------------------------------------------------------------
        // Internal rendering
        // --------------------------------------------------------------------

        private void Show(ItemDef def, ItemInstance instance, EquipmentSlotId? equipmentSlot)
        {
            _currentDef = def;
            _currentInstance = instance;
            _currentEquipmentSlot = equipmentSlot;

            gameObject.SetActive(true);

            // Icon
            if (iconImage != null)
                iconImage.sprite = ItemIconResolver.Resolve(def);

            // Identity
            if (nameText != null)
                nameText.text = def.displayName;

            if (familyText != null)
                familyText.text = def.family.ToString();

            // Meta
            if (weightText != null)
                weightText.text = $"Weight: {def.weight:0.##}";

            if (durabilityText != null)
            {
                if (def.usesDurability && instance != null)
                    durabilityText.text = $"Durability: {instance.durabilityCurrent}/{instance.durabilityMax}";
                else if (def.usesDurability)
                    durabilityText.text = $"Durability: {def.durabilityMax}";
                else
                    durabilityText.text = string.Empty;
            }

            // Stats (very simple first pass)
            if (statsText != null)
                statsText.text = BuildStatsBlock(def);

            RenderAffixes(instance);

            // NEW: Hand off to the optional "granted abilities" panel.
            // This is intentionally *UI-only*. It just passes the same def + instance
            // the details panel is already using.
            RenderGrantedAbilitiesPanel(def, instance, equipmentSlot);

            UpdateActiveGrantSlotHighlight();
        }

        private void HandleLocalPlayerSpawned(PlayerNetIdentity identity)
        {
            if (identity == null)
                return;

            var equipment = identity.GetComponent<PlayerEquipmentComponent>();
            var actor = identity.GetComponent<ActorComponent>();

            if (_equipment == equipment && _actor == actor)
                return;

            UnbindEquipment();

            _equipment = equipment;
            _actor = actor;

            if (_equipment != null)
                _equipment.OnEquipmentChanged += HandleEquipmentChanged;
        }

        private void UnbindEquipment()
        {
            if (_equipment != null)
                _equipment.OnEquipmentChanged -= HandleEquipmentChanged;

            _equipment = null;
            _actor = null;
        }

        private void HandleEquipmentChanged()
        {
            if (!_currentEquipmentSlot.HasValue || _currentDef == null)
                return;

            RefreshGrantedAbilitiesFromSnapshot();
            UpdateActiveGrantSlotHighlight();
        }

        private void RenderGrantedAbilitiesPanel(ItemDef def, ItemInstance instance, EquipmentSlotId? equipmentSlot)
        {
            bool canEdit = CanEditAbilitySelections();

            if (grantedAbilitiesPanelTyped != null)
            {
                var selections = BuildSelectionsForPanel(def, instance, equipmentSlot);
                grantedAbilitiesPanelTyped.Show(def, selections, canEdit);
                return;
            }

            TryInvokeGrantedAbilitiesPanel(ItemGrantedAbilitiesPanelUI, def, instance);
        }

        private bool CanEditAbilitySelections()
        {
            return _actor == null || _actor.State != CombatState.InCombat;
        }

        private List<ItemGrantedAbilitiesPanelUI.SlotSelection> BuildSelectionsForPanel(
            ItemDef def,
            ItemInstance instance,
            EquipmentSlotId? equipmentSlot)
        {
            if (equipmentSlot.HasValue && instance == null)
                return BuildSelectionsFromSnapshot(def, equipmentSlot.Value);

            if (instance != null)
                return BuildSelectionsFromInstance(def, instance);

            return BuildSelectionsFromDef(def);
        }

        private List<ItemGrantedAbilitiesPanelUI.SlotSelection> BuildSelectionsFromSnapshot(ItemDef def, EquipmentSlotId slotId)
        {
            var list = new List<ItemGrantedAbilitiesPanelUI.SlotSelection>(3);

            if (_equipment == null)
                return list;

            var equipped = _equipment.GetEquippedForUI(slotId);
            var slots = def.grantedAbilities.grantedAbilitySlots;
            if (slots == null)
                return list;

            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i].slot;
                list.Add(new ItemGrantedAbilitiesPanelUI.SlotSelection
                {
                    slot = slot,
                    selectedSpellId = GetSnapshotSpellId(equipped, slot)
                });
            }

            return list;
        }

        private static List<ItemGrantedAbilitiesPanelUI.SlotSelection> BuildSelectionsFromInstance(ItemDef def, ItemInstance instance)
        {
            var list = new List<ItemGrantedAbilitiesPanelUI.SlotSelection>(3);

            var slots = def.grantedAbilities.grantedAbilitySlots;
            if (slots == null)
                return list;

            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i].slot;
                var selected = instance != null ? instance.GetSelectedSpellId(def, slot) : slots[i].defaultSpellId;
                list.Add(new ItemGrantedAbilitiesPanelUI.SlotSelection
                {
                    slot = slot,
                    selectedSpellId = selected
                });
            }

            return list;
        }

        private static List<ItemGrantedAbilitiesPanelUI.SlotSelection> BuildSelectionsFromDef(ItemDef def)
        {
            var list = new List<ItemGrantedAbilitiesPanelUI.SlotSelection>(3);
            var slots = def.grantedAbilities.grantedAbilitySlots;
            if (slots == null)
                return list;

            for (int i = 0; i < slots.Length; i++)
            {
                list.Add(new ItemGrantedAbilitiesPanelUI.SlotSelection
                {
                    slot = slots[i].slot,
                    selectedSpellId = slots[i].defaultSpellId
                });
            }

            return list;
        }

        private void RefreshGrantedAbilitiesFromSnapshot()
        {
            if (!_currentEquipmentSlot.HasValue || grantedAbilitiesPanelTyped == null || _currentDef == null)
                return;

            var selections = BuildSelectionsFromSnapshot(_currentDef, _currentEquipmentSlot.Value);
            grantedAbilitiesPanelTyped.Show(_currentDef, selections, CanEditAbilitySelections());
        }

        private static SpellId GetSnapshotSpellId(EquippedSlotNet equipped, AbilityGrantSlot slot)
        {
            return slot switch
            {
                AbilityGrantSlot.Primary => (SpellId)equipped.selectedSpellPrimary,
                AbilityGrantSlot.Secondary => (SpellId)equipped.selectedSpellSecondary,
                AbilityGrantSlot.Utility => (SpellId)equipped.selectedSpellUtility,
                _ => SpellId.None
            };
        }

        private void HandleAbilitySelectionChanged(AbilityGrantSlot slot, SpellId chosen)
        {
            // We can only persist if we have a live instance to write to.
            // Inventory items should always have an instance.
            // Equipment can be def-only in some cases; if so, ignore.
            if (_currentDef == null)
                return;

            if (_currentEquipmentSlot.HasValue && _equipment != null)
            {
                Debug.Log(
                    $"[ItemDetailsPanelUI] Requesting ability selection change for equipment slot {_currentEquipmentSlot.Value} (grant slot: {slot}, spell: {chosen}).");

                if (grantedAbilitiesPanelTyped != null)
                    grantedAbilitiesPanelTyped.SetActiveGrantSlot(slot);

                _equipment.RequestSetAbilitySelection(_currentEquipmentSlot.Value, slot, chosen);

                if (hotbarIconBinder != null)
                    hotbarIconBinder.RefreshSlotForEquipmentSlot(_currentEquipmentSlot.Value);

                return;
            }

            if (_currentInstance == null)
                return;

            // Use the instances validation rules (allowedSpellIds, slot exists, etc.)
            bool ok = _currentInstance.TrySetSelectedSpellId(_currentDef, slot, chosen);

            if (!ok)
                Debug.LogWarning($"[ItemDetailsPanelUI] Rejecting selection {chosen} for {slot} (not allowed by def?)");

            UpdateActiveGrantSlotHighlight();

            if (ok && _currentEquipmentSlot.HasValue && hotbarIconBinder != null)
                hotbarIconBinder.RefreshSlotForEquipmentSlot(_currentEquipmentSlot.Value);
        }

        private void HandleActiveSlotRequested(AbilityGrantSlot slot)
        {
            if (_currentDef == null)
                return;

            if (grantedAbilitiesPanelTyped != null)
                grantedAbilitiesPanelTyped.SetActiveGrantSlot(slot);

            if (_currentEquipmentSlot.HasValue && _equipment != null)
            {
                _equipment.RequestSetActiveGrantSlot(_currentEquipmentSlot.Value, slot);

                if (hotbarIconBinder != null)
                    hotbarIconBinder.RefreshSlotForEquipmentSlot(_currentEquipmentSlot.Value);

                return;
            }

            if (_currentInstance == null)
                return;

            _currentInstance.TrySetActiveGrantSlotForHotbar(_currentDef, slot);
            UpdateActiveGrantSlotHighlight();
        }

        private void UpdateActiveGrantSlotHighlight()
        {
            if (grantedAbilitiesPanelTyped == null || _currentDef == null)
                return;

            AbilityGrantSlot activeSlot = AbilityGrantSlot.Primary;

            if (_currentInstance != null &&
                _currentInstance.TryResolveActiveGrantSlot(_currentDef, allowUpdate: false, out var resolved))
            {
                activeSlot = resolved;
            }
            else if (_currentEquipmentSlot.HasValue && _equipment != null)
            {
                var equipped = _equipment.GetEquippedForUI(_currentEquipmentSlot.Value);
                activeSlot = (AbilityGrantSlot)equipped.activeGrantSlotForHotbar;
            }
            else
            {
                var granted = _currentDef.grantedAbilities.grantedAbilitySlots;
                if (granted != null && granted.Length > 0)
                    activeSlot = granted[0].slot;
            }

            grantedAbilitiesPanelTyped.SetActiveGrantSlot(activeSlot);
        }


        private string BuildStatsBlock(ItemDef def)
        {
            // MVP: show a readable subset based on family
            switch (def.family)
            {
                case ItemFamily.Mainhand:
                    return $"Damage: {def.weapon.minDamage} - {def.weapon.maxDamage}\n" +
                           $"Swing Speed: {def.weapon.swingSpeedSeconds:0.##}s\n" +
                           $"Stamina Cost: {def.weapon.staminaCostPerSwing}";

                case ItemFamily.Chest:
                    return $"Physical Resist: {def.armor.resistPhysical}\n" +
                           $"Fire Resist: {def.armor.resistFire}\n" +
                           $"Cold Resist: {def.armor.resistCold}\n" +
                           $"Poison Resist: {def.armor.resistPoison}\n" +
                           $"Energy Resist: {def.armor.resistEnergy}";

                case ItemFamily.Offhand:
                    return $"Block Bonus: {def.shield.blockType}";

                default:
                    return string.Empty;
            }
        }

        // --------------------------------------------------------------------
        // Affix rendering
        // --------------------------------------------------------------------

        private void RenderAffixes(ItemInstance instance)
        {
            if (affixesContainer == null || affixRowPrefab == null)
            {
                if (affixesHeaderText != null)
                {
                    bool hasAffixes = instance != null && instance.affixes != null && instance.affixes.Count > 0;
                    affixesHeaderText.text = hasAffixes ? "Affixes" : "Affixes: None";
                }

                if (affixesContainer == null || affixRowPrefab == null)
                    return;
            }

            ClearAffixRows();

            if (instance == null || instance.affixes == null || instance.affixes.Count == 0)
            {
                if (affixesHeaderText != null)
                    affixesHeaderText.text = "Affixes: None";
                else
                    AddAffixRow("None");

                return;
            }

            if (affixesHeaderText != null)
                affixesHeaderText.text = "Affixes";

            if (affixCatalog == null)
            {
                Debug.LogWarning("[ItemDetailsPanelUI] AffixCatalog is not assigned; cannot resolve affix names.");
                foreach (var affix in instance.affixes)
                    AddAffixRow($"{affix.id}: {FormatMagnitudeRaw(affix.magnitude)}");
                return;
            }

            for (int i = 0; i < instance.affixes.Count; i++)
            {
                var affix = instance.affixes[i];
                if (!affixCatalog.TryGet(affix.id, out var def) || def == null)
                {
                    AddAffixRow($"{affix.id}: {FormatMagnitudeRaw(affix.magnitude)}");
                    continue;
                }

                AddAffixRow($"{def.displayName}: {FormatAffixMagnitude(def, affix.magnitude)}");
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            LogAffixDebug(instance);
#endif
        }

        private void ClearAffixRows()
        {
            for (int i = 0; i < _affixRows.Count; i++)
            {
                if (_affixRows[i] != null)
                    Destroy(_affixRows[i].gameObject);
            }

            _affixRows.Clear();
        }

        private void AddAffixRow(string text)
        {
            var row = Instantiate(affixRowPrefab, affixesContainer);
            row.text = text;
            _affixRows.Add(row);
        }

        private static string FormatAffixMagnitude(AffixDef def, float magnitude)
        {
            string number = def.integerMagnitude
                ? Mathf.RoundToInt(magnitude).ToString()
                : magnitude.ToString("0.##");

            string prefix = magnitude >= 0 ? "+" : string.Empty;

            switch (def.valueType)
            {
                case AffixValueType.Percent:
                case AffixValueType.ProcChance:
                case AffixValueType.ProcPercent:
                    return $"{prefix}{number}%";
                default:
                    return $"{prefix}{number}";
            }
        }

        private static string FormatMagnitudeRaw(float magnitude)
        {
            string prefix = magnitude >= 0 ? "+" : string.Empty;
            return $"{prefix}{magnitude:0.##}";
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void LogAffixDebug(ItemInstance instance)
        {
            if (instance == null)
                return;

            int count = instance.affixes?.Count ?? 0;
            Debug.Log($"[ItemDetailsPanelUI] ItemInstance {instance.itemDefId} affixes={count}");

            if (instance.affixes == null)
                return;

            for (int i = 0; i < instance.affixes.Count; i++)
            {
                var affix = instance.affixes[i];
                Debug.Log($"[ItemDetailsPanelUI]  - {affix.id} ({affix.magnitude})");
            }
        }
#endif

        // --------------------------------------------------------------------
        // Optional panel invocation (reflection)
        // --------------------------------------------------------------------

        private static void TryInvokeGrantedAbilitiesPanel(MonoBehaviour panel, ItemDef def, ItemInstance instance)
        {
            if (panel == null)
                return;

            // Common method names you might choose for the panel.
            // We attempt them in a sane priority order.
            //
            // Expected signature:
            //   void MethodName(ItemDef def, ItemInstance instance)
            //
            // IMPORTANT:
            // - Reflection is slower than direct calls, but this is UI and called only on clicks.
            // - This keeps compilation stable while you iterate on the panel API.
            string[] methodNames = { "Bind", "Show", "SetItem", "Render" };

            Type type = panel.GetType();

            foreach (string name in methodNames)
            {
                // Look for: (ItemDef, ItemInstance)
                MethodInfo mi = type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    binder: null,
                    types: new[] { typeof(ItemDef), typeof(ItemInstance) },
                    modifiers: null);

                if (mi == null)
                    continue;

                try
                {
                    mi.Invoke(panel, new object[] { def, instance });
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ItemDetailsPanelUI] Failed to invoke {type.Name}.{name}(ItemDef, ItemInstance): {ex}");
                }

                return; // First match wins.
            }

            // If we got here, the panel exists but doesn't expose a compatible method.
            Debug.LogWarning(
                $"[ItemDetailsPanelUI] Granted Abilities Panel '{type.Name}' is assigned, but no compatible method was found.\n" +
                "Add one of: Bind/Show/SetItem/Render with signature (ItemDef, ItemInstance), or replace reflection with a typed call.");
        }

        private static void TryInvokeHide(MonoBehaviour panel)
        {
            if (panel == null)
                return;

            Type type = panel.GetType();

            // Optional Hide() method.
            MethodInfo mi = type.GetMethod("Hide", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                types: Type.EmptyTypes,
                modifiers: null);

            if (mi == null)
                return;

            try
            {
                mi.Invoke(panel, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ItemDetailsPanelUI] Failed to invoke {type.Name}.Hide(): {ex}");
            }
        }
    }
}
