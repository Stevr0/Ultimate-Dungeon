using System;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateDungeon.Items
{
    /// <summary>
    /// AffixPool (DESIGN DATA)
    /// =======================
    ///
    /// A curated list of AffixIds that are allowed to be selected for a given context.
    ///
    /// Examples:
    /// - WeaponPool (Hit Spells, Leaches, combat bonuses)
    /// - ArmorPool (Resists, vitals)
    /// - JewelryPool (Faster Casting, combat bonuses, vitals)
    ///
    /// IMPORTANT:
    /// - Pools do NOT roll magnitudes and do NOT decide affix count.
    /// - Pools simply constrain which AffixIds are eligible to be picked.
    /// - Eligibility (Weapon/Armor/Shield/Jewelry) is still enforced by AffixDef.
    ///
    /// Determinism:
    /// - Pools are just data.
    /// - Deterministic selection is done by AffixPicker using a seeded System.Random.
    /// </summary>
    [CreateAssetMenu(menuName = "Ultimate Dungeon/Items/Affix Pool", fileName = "AffixPool_")]
    public sealed class AffixPool : ScriptableObject
    {
        [Header("Pool Constraints")]
        [Tooltip("Optional: the item eligibility this pool is intended for. Used as an extra safety filter.")]
        public AffixEligibility intendedEligibility = AffixEligibility.Any;

        [Header("Entries")]
        [Tooltip("AffixIds allowed in this pool. Keep this list curated; do not just dump 'everything'.")]
        public List<Entry> entries = new List<Entry>();

        [Serializable]
        public struct Entry
        {
            public AffixId id;

            [Tooltip("Optional weighting for future use. 1 = normal weight. Currently unused unless picker is set to weighted.")]
            [Min(0f)]
            public float weight;

            public Entry(AffixId id, float weight = 1f)
            {
                this.id = id;
                this.weight = weight;
            }
        }

        /// <summary>
        /// Returns the raw ids in this pool (may contain duplicates if the asset is mis-edited).
        /// Validation will catch duplicates later.
        /// </summary>
        public IEnumerable<AffixId> GetIds()
        {
            for (int i = 0; i < entries.Count; i++)
                yield return entries[i].id;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Small authoring nicety: default weights.
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].weight <= 0f)
                {
                    var e = entries[i];
                    e.weight = 1f;
                    entries[i] = e;
                }
            }
        }
#endif
    }
}
