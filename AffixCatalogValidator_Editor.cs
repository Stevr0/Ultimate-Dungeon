#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UltimateDungeon.Items
{
    /// <summary>
    /// AffixCatalogValidator_Editor
    /// ============================
    ///
    /// Editor-only validation for AffixCatalog.
    ///
    /// Enforces rules from ITEM_AFFIX_CATALOG.md:
    /// - No duplicate AffixIds
    /// - Ranges are within authored min/max and generally 0..N
    /// - Stacking policies make sense
    /// - Eligibility expectations (weapon-only procs, jewelry-only faster casting, etc.)
    ///
    /// IMPORTANT:
    /// - This does NOT validate ItemInstances (runtime). That's a runtime sanitization concern.
    /// - This only validates the catalog asset + affix defs.
    /// </summary>
    public static class AffixCatalogValidator_Editor
    {
        [MenuItem("Ultimate Dungeon/Validate/Affix Catalog (Selected)")]
        public static void ValidateSelected()
        {
            var catalog = Selection.activeObject as AffixCatalog;
            if (catalog == null)
            {
                EditorUtility.DisplayDialog("Validate Affix Catalog", "Select an AffixCatalog asset in the Project window first.", "OK");
                return;
            }

            Validate(catalog);
        }

        /// <summary>
        /// Validates a catalog and prints errors/warnings to the Console.
        /// </summary>
        public static void Validate(AffixCatalog catalog)
        {
            if (catalog == null)
            {
                Debug.LogError("[AffixCatalogValidator] Catalog is null.");
                return;
            }

            int errorCount = 0;
            int warnCount = 0;

            // 1) Null entries
            for (int i = 0; i < catalog.defs.Count; i++)
            {
                if (catalog.defs[i] == null)
                {
                    warnCount++;
                    Debug.LogWarning($"[AffixCatalogValidator] Catalog contains a NULL entry at index {i}.", catalog);
                }
            }

            // 2) Duplicate ids
            var seen = new HashSet<AffixId>();
            var dupes = new List<AffixId>();

            foreach (var def in catalog.defs)
            {
                if (def == null) continue;

                if (!seen.Add(def.id))
                    dupes.Add(def.id);
            }

            if (dupes.Count > 0)
            {
                errorCount++;
                Debug.LogError($"[AffixCatalogValidator] Duplicate AffixIds found: {string.Join(", ", dupes)}", catalog);
            }

            // 3) Missing ids (optional but very useful)
            // We expect one AffixDef per enum value.
            // If you are mid-authoring, this will warn rather than error.
            var allIds = (AffixId[])Enum.GetValues(typeof(AffixId));
            foreach (var id in allIds)
            {
                if (!seen.Contains(id))
                {
                    warnCount++;
                    Debug.LogWarning($"[AffixCatalogValidator] Missing AffixDef for id '{id}'.", catalog);
                }
            }

            // 4) Per-def validation
            foreach (var def in catalog.defs)
            {
                if (def == null) continue;

                ValidateDef(def, ref errorCount, ref warnCount);
            }

            // Summary
            string msg = $"AffixCatalog validation complete. Errors: {errorCount}, Warnings: {warnCount}.";
            if (errorCount > 0)
                Debug.LogError($"[AffixCatalogValidator] {msg}", catalog);
            else if (warnCount > 0)
                Debug.LogWarning($"[AffixCatalogValidator] {msg}", catalog);
            else
                Debug.Log($"[AffixCatalogValidator] {msg}", catalog);
        }

        private static void ValidateDef(AffixDef def, ref int errorCount, ref int warnCount)
        {
            // Range sanity
            if (def.min < 0f)
            {
                errorCount++;
                Debug.LogError($"[AffixCatalogValidator] {def.name} ({def.id}) has min < 0 (min={def.min}). Ranges should be 0..N.", def);
            }

            if (def.max < def.min)
            {
                errorCount++;
                Debug.LogError($"[AffixCatalogValidator] {def.name} ({def.id}) has max < min (min={def.min}, max={def.max}).", def);
            }

            // Doc convention: most ranges are 0..N.
            if (!Mathf.Approximately(def.min, 0f))
            {
                warnCount++;
                Debug.LogWarning($"[AffixCatalogValidator] {def.name} ({def.id}) has min != 0. Ensure this is intentional and documented.", def);
            }

            // Stacking policy sanity
            if (def.stackingPolicy == AffixStackingPolicy.NoStack)
            {
                // NoStack affixes should usually be procs or leeches.
                if (def.valueType != AffixValueType.ProcChance && def.valueType != AffixValueType.ProcPercent)
                {
                    warnCount++;
                    Debug.LogWarning($"[AffixCatalogValidator] {def.name} ({def.id}) is NoStack but valueType is {def.valueType}. Is that intentional?", def);
                }
            }

            // Eligibility locks from the pasted catalog
            // - Hit Spells and Leaches are weapon-only
            if (IsWeaponProc(def.id))
            {
                if ((def.eligibility & AffixEligibility.Weapon) == 0 || def.eligibility != AffixEligibility.Weapon)
                {
                    errorCount++;
                    Debug.LogError($"[AffixCatalogValidator] {def.name} ({def.id}) must be Weapon-only per catalog, but eligibility is {def.eligibility}.", def);
                }

                // Proc types must match.
                if (def.valueType != AffixValueType.ProcChance && def.valueType != AffixValueType.ProcPercent)
                {
                    warnCount++;
                    Debug.LogWarning($"[AffixCatalogValidator] {def.name} ({def.id}) looks like a proc/leech but valueType is {def.valueType}.", def);
                }
            }

            // - Faster Casting is jewelry-only (v1)
            if (def.id == AffixId.Magic_FasterCasting)
            {
                if (def.eligibility != AffixEligibility.Jewelry)
                {
                    errorCount++;
                    Debug.LogError($"[AffixCatalogValidator] Faster Casting must be Jewelry-only per catalog, but eligibility is {def.eligibility}.", def);
                }
            }

            // - Resist affixes are armor/shield-only (v1)
            if (IsResist(def.id))
            {
                var allowed = AffixEligibility.Armor | AffixEligibility.Shield;
                if ((def.eligibility & allowed) == 0)
                {
                    errorCount++;
                    Debug.LogError($"[AffixCatalogValidator] Resist affix {def.id} must be Armor/Shield eligible per catalog, but eligibility is {def.eligibility}.", def);
                }

                // If designer accidentally includes Weapon/Jewelry, warn (not error) since future may expand.
                if ((def.eligibility & (AffixEligibility.Weapon | AffixEligibility.Jewelry)) != 0)
                {
                    warnCount++;
                    Debug.LogWarning($"[AffixCatalogValidator] Resist affix {def.id} includes Weapon/Jewelry eligibility. Catalog says Armor/Shield-only (v1). Ensure intentional.", def);
                }
            }

            // - Percent-like types should usually have integer magnitudes (authoring convenience)
            if (def.valueType == AffixValueType.Percent || def.valueType == AffixValueType.ProcChance || def.valueType == AffixValueType.ProcPercent)
            {
                if (!def.integerMagnitude)
                {
                    warnCount++;
                    Debug.LogWarning($"[AffixCatalogValidator] {def.name} ({def.id}) is {def.valueType} but integerMagnitude is false. Is that intentional?", def);
                }
            }
        }

        private static bool IsResist(AffixId id)
        {
            return id == AffixId.Resist_Physical
                || id == AffixId.Resist_Fire
                || id == AffixId.Resist_Cold
                || id == AffixId.Resist_Poison
                || id == AffixId.Resist_Energy;
        }

        private static bool IsWeaponProc(AffixId id)
        {
            return id == AffixId.Hit_Lightning
                || id == AffixId.Hit_Fireball
                || id == AffixId.Hit_Harm
                || id == AffixId.Leech_Life
                || id == AffixId.Leech_Mana
                || id == AffixId.Leech_Stamina;
        }
    }
}
#endif
