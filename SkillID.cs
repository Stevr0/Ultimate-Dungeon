// ============================================================================
// SkillId.cs
// ----------------------------------------------------------------------------
// Authoritative enum generated from SKILLS.md (v0.1, 2026-01-27)
//
// IMPORTANT RULES:
// - Do NOT reorder existing entries once you ship/save data.
// Reordering changes the underlying int values and can break save files.
// - Only append new skills to the end.
// - If you ever need to deprecate a skill, keep the enum value and mark it
// as deprecated in documentation + SkillDef assets.
// ============================================================================


namespace UltimateDungeon.Skills
{
    /// <summary>
    /// Stable identifiers for all player skills.
    ///
    /// This is the code representation of SKILLS.md and should match it exactly.
    ///
    /// Why an enum?
    /// - Fast lookups
    /// - Easy serialization
    /// - Prevents typo bugs that happen with string keys
    /// </summary>
    public enum SkillId
    {
        // --------------------------------------------------------------------
        // Combat
        // --------------------------------------------------------------------
        Swords = 0,
        Macing = 1,
        Fencing = 2,
        Wrestling = 3,
        Archery = 4,
        Tactics = 5,
        Anatomy = 6,
        Parrying = 7,


        // --------------------------------------------------------------------
        // Magic
        // --------------------------------------------------------------------
        Magery = 100,
        Meditation = 101,
        EvaluatingIntelligence = 102,
        ResistSpells = 103,


        // --------------------------------------------------------------------
        // Utility
        // --------------------------------------------------------------------
        Healing = 200,
        Hiding = 201,
        Stealth = 202,
        Lockpicking = 203,


        // --------------------------------------------------------------------
        // Crafting
        // --------------------------------------------------------------------
        Blacksmithing = 300,
        Tailoring = 301,
        Carpentry = 302,
        Alchemy = 303,
    }
}