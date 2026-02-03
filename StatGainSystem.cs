// ============================================================================
// StatGainSystem.cs
// ----------------------------------------------------------------------------
// Server-authoritative base stat gain logic.
//
// PURPOSE:
// - Provide UO-like stat gains as a byproduct of skill usage.
// - Keep ALL progression server-authoritative.
// - Keep tuning isolated so combat/crafting/magic math never needs refactors.
//
// DESIGN NOTES:
// - No levels, no XP.
// - Stats start from PlayerDefinition (10/10/10).
// - Items and status effects modify stats separately.
// - This system ONLY handles permanent base stat progression.
//
// OPEN (NOT LOCKED YET):
// - Total stat cap (e.g. UO-style 225)
// - Individual stat caps (e.g. 100 per stat)
// - Gain chance curves
// ============================================================================

using UnityEngine;
using UltimateDungeon.Players;
using UltimateDungeon.Skills;

namespace UltimateDungeon.Progression
{
    /// <summary>
    /// StatGainSystem
    /// -------------
    /// Central authority for permanent base stat gains (STR / DEX / INT).
    ///
    /// IMPORTANT:
    /// - Only the SERVER should ever call this.
    /// - This system does NOT recompute vitals.
    ///   Callers must do that once per event if a stat gain occurred.
    /// </summary>
    public static class StatGainSystem
    {
        // --------------------------------------------------------------------
        // Tunables (PROPOSED DEFAULTS)
        // --------------------------------------------------------------------

        /// <summary>
        /// Chance per eligible event to gain +1 to a base stat.
        /// Keep LOW — stats affect many downstream systems.
        /// </summary>
        public const float DefaultStatGainChance = 0.02f; // 2%

        /// <summary>
        /// Amount added to a base stat when a gain succeeds.
        /// UO-like behavior is +1.
        /// </summary>
        public const int StatGainStep = 1;

        // --------------------------------------------------------------------
        // Public API
        // --------------------------------------------------------------------

        /// <summary>
        /// Attempts to apply a permanent base stat gain.
        ///
        /// Returns:
        /// - true  => stat increased
        /// - false => no increase (chance failed, caps blocked, invalid input)
        ///
        /// Determinism:
        /// - Uses the provided DeterministicRng (seeded by caller).
        /// </summary>
        public static bool TryApplyStatGain(
            PlayerStats stats,
            PlayerDefinition def,
            StatId statToGain,
            float gainChance,
            ref DeterministicRng rng
        )
        {
            // Always return a value on every path.
            if (stats == null || def == null)
                return false;

            // Clamp chance to a safe range.
            gainChance = Mathf.Clamp01(gainChance);

            // Roll chance.
            float roll = rng.NextFloat01();
            if (roll >= gainChance)
                return false;

            // --------------------------------------------------------------
            // CAP RULES (NOT LOCKED YET)
            // --------------------------------------------------------------
            // Hooks left here intentionally so caps can be locked later
            // without touching any callers.

            // Example UO-style rules (DO NOT ENABLE YET):
            // int totalCap = 225;
            // int individualCap = 100;
            //
            // if (stats.GetTotalBaseStats() >= totalCap)
            //     return false;
            //
            // switch (statToGain)
            // {
            //     case StatId.STR: if (stats.BaseSTR >= individualCap) return false; break;
            //     case StatId.DEX: if (stats.BaseDEX >= individualCap) return false; break;
            //     case StatId.INT: if (stats.BaseINT >= individualCap) return false; break;
            // }

            // Apply +1 to the chosen base stat.
            // Requires PlayerStats.IncreaseBaseStat(...)
            stats.IncreaseBaseStat(statToGain, StatGainStep);

            // NOTE:
            // Vitals recompute is NOT done here.
            // SkillUseResolver (or caller) must recompute vitals once per event
            // if any stat gain occurred.

            return true;
        }

        // --------------------------------------------------------------------
        // Skill -> Stat Mapping Helper (Scaffolding)
        // --------------------------------------------------------------------
        // Long-term: move this into data (SkillDef.GoverningAttribute or
        // a ProgressionRules ScriptableObject).

        public static StatId GuessStatForSkill(SkillId skillId)
        {
            switch (skillId)
            {
                // -----------------
                // STR leaning
                // -----------------
                case SkillId.Swords:
                case SkillId.Macing:
                case SkillId.Wrestling:
                case SkillId.Tactics:
                case SkillId.Anatomy:
                case SkillId.Parrying:
                    return StatId.STR;

                // -----------------
                // DEX leaning
                // -----------------
                case SkillId.Fencing:
                case SkillId.Archery:
                case SkillId.Healing:
                case SkillId.Hiding:
                case SkillId.Stealth:
                case SkillId.Lockpicking:
                    return StatId.DEX;

                // -----------------
                // INT leaning
                // -----------------
                case SkillId.Magery:
                case SkillId.Meditation:
                case SkillId.EvaluatingIntelligence:
                case SkillId.ResistSpells:
                case SkillId.Alchemy:
                    return StatId.INT;

                // Crafting (placeholder defaults)
                case SkillId.Blacksmithing:
                case SkillId.Tailoring:
                case SkillId.Carpentry:
                    return StatId.DEX;

                // Safe default
                default:
                    return StatId.STR;
            }
        }
    }
}
