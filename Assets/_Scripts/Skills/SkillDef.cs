// ============================================================================
// SkillDef.cs
// ----------------------------------------------------------------------------
// ScriptableObject definition for a skill.
//
// You will create one SkillDef asset per SkillId.
// Example asset names:
//   - SkillDef_Swords.asset
//   - SkillDef_Magery.asset
//
// These assets are "designer-editable" but server rules are still enforced in
// runtime systems (SkillGainSystem, combat resolver, etc.).
// ============================================================================

using UnityEngine;

namespace UltimateDungeon.Skills
{
    /// <summary>
    /// Pure data describing a skill.
    ///
    /// This DOES NOT store a player's current value.
    /// Player values live in PlayerSkillBook (runtime/networked).
    /// </summary>
    [CreateAssetMenu(menuName = "Ultimate Dungeon/Skills/Skill Definition", fileName = "SkillDef_")]
    public class SkillDef : ScriptableObject
    {
        // --------------------------------------------------------------------
        // Authoritative Identity
        // --------------------------------------------------------------------

        [Header("Identity")]
        [Tooltip("Must match a value in the SkillId enum.")]
        public SkillId skillId;

        [Tooltip("Human-readable name shown in UI (e.g., 'Evaluating Intelligence').")]
        public string displayName;

        [TextArea(2, 6)]
        [Tooltip("Short description for tooltips / help panels.")]
        public string description;

        // --------------------------------------------------------------------
        // Category (for UI grouping only)
        // --------------------------------------------------------------------

        [Header("Category (UI only)")]
        public SkillCategory category = SkillCategory.Combat;

        // --------------------------------------------------------------------
        // Optional: Governing Attribute
        // --------------------------------------------------------------------
        // This is NOT locked by the docs yet, but it is a very common pattern:
        // - STR-aligned skills (e.g., Wrestling)
        // - DEX-aligned skills (e.g., Archery)
        // - INT-aligned skills (e.g., Magery)
        //
        // If you don't want this, you can remove it safely later.

        [Header("Optional: Governing Attribute")]
        public GoverningAttribute governingAttribute = GoverningAttribute.None;

        // --------------------------------------------------------------------
        // Optional: Skill Gain Settings
        // --------------------------------------------------------------------
        // The docs lock the *concept* of use-based gains and the manual cap
        // management model, but they do not lock exact per-skill gain chances.
        //
        // These values give us a place to tune the game without hardcoding.

        [Header("Optional: Gain Tuning")]
        [Tooltip("Multiplier applied to the base gain chance for this skill.")]
        [Range(0.0f, 5.0f)]
        public float gainChanceMultiplier = 1.0f;

        [Tooltip("If true, this skill can be used to modify mana regen (e.g., Meditation).")]
        public bool affectsRegen;

        // --------------------------------------------------------------------
        // Validation Helpers
        // --------------------------------------------------------------------

        private void OnValidate()
        {
            // Keep displayName sane for new assets.
            // If the designer hasn't typed one yet, we auto-fill from enum.
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = skillId.ToString();
            }
        }
    }

    /// <summary>
    /// Category for UI grouping only.
    /// (No gameplay meaning, per SKILLS.md)
    /// </summary>
    public enum SkillCategory
    {
        Combat,
        Magic,
        Utility,
        Crafting
    }

    /// <summary>
    /// Optional attribute alignment for future formulas.
    /// Not currently required by the docs, but useful.
    /// </summary>
    public enum GoverningAttribute
    {
        None,
        Strength,
        Dexterity,
        Intelligence
    }
}
