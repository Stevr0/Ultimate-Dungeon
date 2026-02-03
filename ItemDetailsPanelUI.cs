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
// - It does NOT mutate game state.
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
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UltimateDungeon.Items;
using UltimateDungeon.Spells;

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

        // Typed reference to the granted abilities panel (if you can)
        // If you're still using MonoBehaviour + reflection, keep that,
        // but ALSO add this typed field so we can subscribe to events.
        [SerializeField] private ItemGrantedAbilitiesPanelUI grantedAbilitiesPanelTyped;


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
                grantedAbilitiesPanelTyped.OnSelectionChanged += HandleAbilitySelectionChanged;
        }

        private void OnDisable()
        {
            if (cancelButton != null)
                cancelButton.onClick.RemoveListener(HandleCancelClicked);

            if (grantedAbilitiesPanelTyped != null)
                grantedAbilitiesPanelTyped.OnSelectionChanged -= HandleAbilitySelectionChanged;
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

            Show(def, instance);
        }

        public void ShowFromEquipment(ItemDef def, ItemInstance instance = null)
        {
            // Equipment path: the binder already knows the ItemDef.
            if (def == null)
                return;

            Show(def, instance);
        }

        public void Hide()
        {
            // Hide the whole details panel
            gameObject.SetActive(false);

            // Also hide the granted-abilities panel (if present)
            TryInvokeHide(ItemGrantedAbilitiesPanelUI);


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

        private void Show(ItemDef def, ItemInstance instance)
        {
            _currentDef = def;
            _currentInstance = instance;

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


            // NEW: Hand off to the optional "granted abilities" panel.
            // This is intentionally *UI-only*. It just passes the same def + instance
            // the details panel is already using.
            TryInvokeGrantedAbilitiesPanel(ItemGrantedAbilitiesPanelUI, def, instance);
        }

        private void HandleAbilitySelectionChanged(AbilityGrantSlot slot, SpellId chosen)
        {
            // We can only persist if we have a live instance to write to.
            // Inventory items should always have an instance.
            // Equipment can be def-only in some cases; if so, ignore.
            if (_currentDef == null || _currentInstance == null)
                return;

            // Use the instances validation rules (allowedSpellIds, slot exists, etc.)
            bool ok = _currentInstance.TrySetSelectedSpellId(_currentDef, slot, chosen);

            if (!ok)
                Debug.LogWarning($"[ItemDetailsPanelUI] Rejecting selection {chosen} for {slot} (not allowed by def?)");
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
