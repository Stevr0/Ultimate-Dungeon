// ============================================================================
// PlayerDefinition.cs
// ----------------------------------------------------------------------------
// Authoritative player template (ScriptableObject).
//
// This asset defines:
// - Starting primary attributes (STR/DEX/INT)
// - Global caps (Total Skill Cap = 700, Vital Cap = 150)
// - Starting skills (values + default lock state)
//
// IMPORTANT:
// - This is DESIGN DATA, not runtime state.
// - Runtime state lives in PlayerCore + PlayerStats + PlayerVitals + PlayerSkillBook.
// - The SERVER is authoritative for all values derived from this.
//
// Aligns with:
// - PLAYER_MODEL.md
// - PLAYER_DEFINITION.md
// - SKILLS.md
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateDungeon.Players
{
    /// <summary>
    /// PlayerDefinition
    /// ----------------
    /// ScriptableObject that acts like a "character rules template".
    ///
    /// You will typically have at least one default asset:
    /// - PlayerDef_Default
    ///
    /// Later you might add more (e.g., different starter experiences),
    /// but remember: this game is classless. These are NOT "classes".
    /// They are configuration profiles.
    /// </summary>
    [CreateAssetMenu(menuName = "Ultimate Dungeon/Players/Player Definition", fileName = "PlayerDef_")]
    public sealed class PlayerDefinition : ScriptableObject
    {
        // --------------------------------------------------------------------
        // Identity
        // --------------------------------------------------------------------

        [Header("Identity")]
        [Tooltip("Stable identifier for save data / migrations (e.g., 'player_default').")]
        public string definitionId = "player_default";

        [Tooltip("Editor-friendly label only (not used for save integrity).")]
        public string displayName = "Default Player";

        // --------------------------------------------------------------------
        // Primary Attributes (LOCKED)
        // --------------------------------------------------------------------
        // Locked by your decisions:
        // - Starting stats fixed at 10 each.

        [Header("Starting Attributes (LOCKED)")]
        [Min(1)]
        public int baseSTR = 10;

        [Min(1)]
        public int baseDEX = 10;

        [Min(1)]
        public int baseINT = 10;

        // --------------------------------------------------------------------
        // Vital Regen (Authoritative)
        // --------------------------------------------------------------------


        [Header("Vital Regen (Authoritative)")]
        [Tooltip("HP regen per second while alive. Server applies this over time.")]
        [Min(0f)]
        public float hpRegenPerSec = 1.0f;


        [Tooltip("Stamina regen per second while alive. Server applies this over time.")]
        [Min(0f)]
        public float staminaRegenPerSec = 2.0f;


        [Tooltip("Mana regen per second while alive. Server applies this over time.")]
        [Min(0f)]
        public float manaRegenPerSec = 1.5f;

        // --------------------------------------------------------------------
        // Global Caps (LOCKED)
        // --------------------------------------------------------------------

        [Header("Caps (LOCKED)")]
        [Tooltip("Total skill cap (UO-style). Locked to 700.")]
        public float totalSkillCap = 700f;

        [Tooltip("Hard cap for any max vital (HP/Stamina/Mana). Locked to 150.")]
        public int vitalCap = 150;

        [Tooltip("Hard cap for any (Resistances). Locked to 70.")]
        public int resistanceCap = 70;

        // --------------------------------------------------------------------
        // Vital Derivation (LOCKED RULE)
        // --------------------------------------------------------------------
        // Locked rule:
        // - MaxHPBase = STR
        // - MaxStaminaBase = DEX
        // - MaxManaBase = INT
        // - Items/Statuses can push each up to vitalCap.
        //
        // We DO NOT store base max vitals here because they are derived from STR/DEX/INT.
        // However, we DO store the cap because it is a world rule.

        // --------------------------------------------------------------------
        // Starting Skills
        // --------------------------------------------------------------------
        // IMPORTANT (your design decision):
        // - Players start with ALL skills available from the beginning.
        // - Therefore we do NOT need to list every skill in this asset.
        //
        // This list is treated as OVERRIDES ONLY.
        // If a SkillId is not present here, it is assumed to exist with:
        //   startValue = 0.0
        //   lockState  = defaultSkillLockState
        //
        // This keeps the asset small and avoids tedious manual setup.

        [Header("Starting Skills (Overrides Only)")]
        [Tooltip("Optional overrides for starting skill values/lock states. Missing skills default to 0.0 and defaultSkillLockState.")]
        public List<StartingSkill> startingSkills = new List<StartingSkill>();

        // --------------------------------------------------------------------
        // Default Skill Lock Behavior (LOCKED MODEL)
        // --------------------------------------------------------------------
        // Locked model: each skill has + / - / Lock.
        // We store the *default* state for newly created characters.

        [Header("Default Skill Lock State (Applied to All Skills by Default)")]
        [Tooltip("If a skill is missing from startingSkills, this state is used.")]
        public SkillLockState defaultSkillLockState = SkillLockState.Increase;

        // --------------------------------------------------------------------
        // Validation
        // --------------------------------------------------------------------

        private void OnValidate()
        {
            // Make sure caps remain locked to the design values.
            // (If you later decide to make them configurable per shard/world,
            // you can remove these clamps and move caps into a WorldRules asset.)

            // Keep totalSkillCap at exactly 700.
            if (!Mathf.Approximately(totalSkillCap, 700f))
                totalSkillCap = 700f;

            // Keep vitalCap at exactly 150.
            if (vitalCap != 150)
                vitalCap = 150;

            // Keep resistanceCap at exactly 70.
            if (resistanceCap != 70)
                resistanceCap = 70;

            // Ensure starting attributes are fixed to 10 each.
            // If you later add character creation point-buy, remove these clamps.
            if (baseSTR != 10) baseSTR = 10;
            if (baseDEX != 10) baseDEX = 10;
            if (baseINT != 10) baseINT = 10;

            if (hpRegenPerSec < 0f) hpRegenPerSec = 0f;
            if (staminaRegenPerSec < 0f) staminaRegenPerSec = 0f;
            if (manaRegenPerSec < 0f) manaRegenPerSec = 0f;

            // Sanity: prevent null list.
            if (startingSkills == null)
                startingSkills = new List<StartingSkill>();

            // Optional: remove duplicate SkillIds (keeps Inspector mistakes harmless).
            DeduplicateStartingSkills();
        }

        /// <summary>
        /// Removes duplicate SkillIds in the startingSkills list.
        /// Keeps the first occurrence and deletes subsequent duplicates.
        /// </summary>
        private void DeduplicateStartingSkills()
        {
            var seen = new HashSet<int>();

            // Iterate backwards so we can safely remove.
            for (int i = startingSkills.Count - 1; i >= 0; i--)
            {
                int idInt = (int)startingSkills[i].skillId;

                if (seen.Contains(idInt))
                {
                    startingSkills.RemoveAt(i);
                    continue;
                }

                seen.Add(idInt);
            }
        }

        /// <summary>
        /// Convenience lookup used by character creation and by server validators.
        ///
        /// IMPORTANT:
        /// - We assume every SkillId exists for every player.
        /// - This method returns an override if present, otherwise defaults.
        /// </summary>
        public StartingSkill GetStartingSkill(UltimateDungeon.Skills.SkillId skillId)
        {
            // Linear search is fine for tiny lists.
            // If you later add 100+ skills, you can build a cached dictionary.
            for (int i = 0; i < startingSkills.Count; i++)
            {
                if (startingSkills[i].skillId == skillId)
                    return startingSkills[i];
            }

            // Missing: return defaults.
            return new StartingSkill
            {
                skillId = skillId,
                startValue = 0f,
                lockState = defaultSkillLockState
            };
        }
    }

    /// <summary>
    /// SkillLockState
    /// --------------
    /// Locked by design:
    /// - Increase (+)
    /// - Decrease (-)
    /// - Locked (🔒)
    ///
    /// Stored per character at runtime, but PlayerDefinition provides defaults.
    /// </summary>
    public enum SkillLockState
    {
        Increase = 0,
        Decrease = 1,
        Locked = 2
    }

    /// <summary>
    /// StartingSkill
    /// ------------
    /// A small serializable struct for initial skill values.
    ///
    /// NOTES:
    /// - We use float to support UO-like 0.1 increments.
    /// - Lock state controls whether this skill can gain or lose.
    /// </summary>
    [Serializable]
    public struct StartingSkill
    {
        public UltimateDungeon.Skills.SkillId skillId;

        [Tooltip("Starting value (recommended 0.0; supports 0.1 increments).")]
        public float startValue;

        [Tooltip("Default lock state for this skill on new characters.")]
        public SkillLockState lockState;
    }
}
