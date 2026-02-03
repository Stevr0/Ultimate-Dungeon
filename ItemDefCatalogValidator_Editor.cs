// ============================================================================
// ItemDefCatalogValidator_Editor.cs — Ultimate Dungeon (Editor-only)
// ----------------------------------------------------------------------------
// Validates ItemDef assets against:
// - ITEM_CATALOG.md (stable id existence)
// - ITEM_DEF_SCHEMA.md (authoring legality rules)
//
// This is intentionally "fail fast" so content errors are found early.
// ============================================================================

#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace UltimateDungeon.Items
{
    public static class ItemDefCatalogValidator_Editor
    {
        [MenuItem("Ultimate Dungeon/Validate/ItemDef Catalog (Selected)")]
        public static void ValidateSelected()
        {
            var catalog = Selection.activeObject as ItemDefCatalog;
            if (catalog == null)
            {
                EditorUtility.DisplayDialog("Validate ItemDef Catalog",
                    "Select an ItemDefCatalog asset.", "OK");
                return;
            }

            Validate(catalog);
        }

        public static void Validate(ItemDefCatalog catalog)
        {
            if (catalog == null)
                return;

            var allowedIds = catalog.CatalogMarkdown != null
                ? ParseBacktickedIds(catalog.CatalogMarkdown.text)
                : null;

            var seen = new HashSet<string>();

            foreach (var def in catalog.AllDefs)
            {
                if (def == null)
                {
                    Debug.LogWarning("Null ItemDef in catalog", catalog);
                    continue;
                }

                // -------------------------
                // Stable id checks
                // -------------------------
                if (string.IsNullOrWhiteSpace(def.itemDefId))
                {
                    Debug.LogError("ItemDef has empty itemDefId", def);
                    continue;
                }

                if (!seen.Add(def.itemDefId))
                    Debug.LogError($"Duplicate ItemDefId: {def.itemDefId}", def);

                if (allowedIds != null && !allowedIds.Contains(def.itemDefId))
                    Debug.LogWarning($"ItemDefId not found in ITEM_CATALOG.md: {def.itemDefId}", def);

                // -------------------------
                // Schema legality checks (subset)
                // -------------------------

                // Stacking rule: stackable => stackMax > 1
                if (def.isStackable && def.stackMax <= 1)
                    Debug.LogError($"Stackable item must have stackMax > 1: {def.itemDefId}", def);

                // Durability rule: usesDurability => durabilityMax > 0
                if (def.usesDurability && def.durabilityMax <= 0f)
                    Debug.LogError($"usesDurability requires durabilityMax > 0: {def.itemDefId}", def);

                // Equip legality
                if (def.equipment.isEquippable)
                {
                    if (def.family == ItemFamily.UtilityItem)
                    {
                        if (def.equipment.equipSlot != EquipSlot.BeltA && def.equipment.equipSlot != EquipSlot.BeltB)
                            Debug.LogError($"UtilityItem must equip to BeltA or BeltB: {def.itemDefId}", def);
                    }
                    else
                    {
                        // For all other equippable families, equipSlot must match the family.
                        var expected = ExpectedSlot(def.family);
                        if (expected != EquipSlot.None && def.equipment.equipSlot != expected)
                            Debug.LogError($"EquipSlot mismatch. Expected {expected} for {def.family}: {def.itemDefId}", def);
                    }

                    // Backpack lock
                    if (def.equipment.equipSlot == EquipSlot.Bag)
                    {
                        if (def.container.capacitySlots != 48)
                            Debug.LogError($"Backpacks must have exactly 48 slots: {def.itemDefId}", def);
                    }
                }

                // Icon authoring sanity
                if (!string.IsNullOrWhiteSpace(def.iconAddress))
                {
                    if (def.iconAddress.Contains("Assets/") || def.iconAddress.EndsWith(".png"))
                        Debug.LogWarning($"iconAddress should be Resources path without extension: {def.itemDefId} => '{def.iconAddress}'", def);
                }
            }
        }

        private static EquipSlot ExpectedSlot(ItemFamily family)
        {
            return family switch
            {
                ItemFamily.Bag => EquipSlot.Bag,
                ItemFamily.Head => EquipSlot.Head,
                ItemFamily.Neck => EquipSlot.Neck,
                ItemFamily.Mainhand => EquipSlot.Mainhand,
                ItemFamily.Chest => EquipSlot.Chest,
                ItemFamily.Offhand => EquipSlot.Offhand,
                ItemFamily.Foot => EquipSlot.Foot,
                ItemFamily.Mount => EquipSlot.Mount,
                _ => EquipSlot.None,
            };
        }

        private static HashSet<string> ParseBacktickedIds(string markdown)
        {
            var set = new HashSet<string>();
            var rx = new Regex("`([^`]+)`");
            foreach (Match m in rx.Matches(markdown))
                set.Add(m.Groups[1].Value.Trim());
            return set;
        }
    }
}
#endif
