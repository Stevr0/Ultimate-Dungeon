using System;
using UnityEngine;

namespace UltimateDungeon.Items
{
    /// <summary>
    /// AffixDef (AUTHORITATIVE DATA)
    /// =============================
    ///
    /// ScriptableObject definition of a single affix type.
    ///
    /// Source of truth:
    /// - ITEM_AFFIX_CATALOG.md
    ///
    /// This is DESIGN DATA (author-time).
    /// Runtime rolled values live in an AffixInstance (future).
    ///
    /// Notes on "percent":
    /// - For Percent / ProcChance / ProcPercent, we store values as *0..N in percent units*.
    ///   Example: 15 means "15%".
    /// - This keeps authoring simple and matches the doc tables.
    /// - When applying math, you convert to fraction: percent * 0.01f.
    /// </summary>
    [CreateAssetMenu(menuName = "Ultimate Dungeon/Items/Affix Def", fileName = "AffixDef_")]
    public sealed class AffixDef : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable affix identifier (append-only).")]
        public AffixId id;

        [Tooltip("UI display name (can change without breaking saves).")]
        public string displayName;

        [Header("Magnitude")]
        [Tooltip("How the magnitude should be interpreted.")]
        public AffixValueType valueType = AffixValueType.Flat;

        [Tooltip("Minimum allowed magnitude (authoritative).")]
        public float min = 0f;

        [Tooltip("Maximum allowed magnitude (authoritative).")]
        public float max = 0f;

        [Tooltip("Whether the rolled magnitude should be treated as whole numbers.")]
        public bool integerMagnitude = true;

        [Header("Stacking")]
        [Tooltip("How multiple instances of this affix should combine.")]
        public AffixStackingPolicy stackingPolicy = AffixStackingPolicy.Sum;

        [Header("Eligibility")]
        [Tooltip("Which item families are allowed to roll this affix.")]
        public AffixEligibility eligibility = AffixEligibility.Any;

        [Header("Notes")]
        [TextArea(2, 6)]
        [Tooltip("Designer notes (non-authoritative).")]
        public string notes;

        /// <summary>
        /// Convenience: clamps a magnitude to this affix's authoritative range.
        /// Use this in validators and when sanitizing network/save data.
        /// </summary>
        public float Clamp(float value)
        {
            if (max < min)
                return min;

            return Mathf.Clamp(value, min, max);
        }

        /// <summary>
        /// Convenience: validates magnitude against range.
        /// </summary>
        public bool IsInRange(float value)
        {
            if (max < min)
                return false;

            return value >= min && value <= max;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Keep values sane while authoring.
            if (min < 0f) min = 0f;
            if (max < 0f) max = 0f;

            // If a designer accidentally swaps them, keep them consistent.
            if (max < min)
                max = min;

            // Safety: if you forget a display name, default to enum name.
            if (string.IsNullOrWhiteSpace(displayName))
                displayName = id.ToString();

            // Doc lock: ranges are always 0..N unless explicitly stated.
            // (If you ever add a special-case affix with non-0 min, do it intentionally.)
            if (min != 0f)
            {
                // We don't hard-error here; we just make it visible.
                // The proper enforcement will live in an editor validator later.
            }

            // If integerMagnitude is true, ensure min/max are "integer-like".
            // Again, not a hard error, just a nudge. Validators will enforce later.
        }
#endif
    }

    /// <summary>
    /// AffixValueType
    /// ==============
    /// Matches the catalog's "Type" column.
    /// </summary>
    public enum AffixValueType
    {
        /// <summary>Flat additive value (e.g., +10 Max HP, +12 Physical Resist)</summary>
        Flat = 0,

        /// <summary>Percent modifier (e.g., +25% Damage Increase)</summary>
        Percent = 1,

        /// <summary>Chance to proc something (e.g., 10% Hit Lightning)</summary>
        ProcChance = 2,

        /// <summary>Percent of a payload to return (e.g., 15% Life Leech)</summary>
        ProcPercent = 3,
    }

    /// <summary>
    /// AffixStackingPolicy
    /// ===================
    /// Matches the catalog's stacking policies.
    /// </summary>
    public enum AffixStackingPolicy
    {
        /// <summary>Sum magnitudes together.</summary>
        Sum = 0,

        /// <summary>Take the highest magnitude only.</summary>
        HighestOnly = 1,

        /// <summary>At most one instance may exist.</summary>
        NoStack = 2,
    }

    /// <summary>
    /// AffixEligibility (FLAGS)
    /// ========================
    /// Which kinds of items can roll this affix.
    ///
    /// IMPORTANT:
    /// - This is intentionally coarse-grained (Weapon/Armor/Shield/Jewelry).
    /// - The exact mapping from ItemFamily -> Eligibility should be handled
    ///   by the ItemDef schema / item families later.
    /// </summary>
    [Flags]
    public enum AffixEligibility
    {
        None = 0,

        Weapon = 1 << 0,
        Armor = 1 << 1,
        Shield = 1 << 2,
        Jewelry = 1 << 3,

        Any = Weapon | Armor | Shield | Jewelry,
    }
}
