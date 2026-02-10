// ============================================================================
// StatusEffectId.cs
// ----------------------------------------------------------------------------
// Authoritative enum generated from STATUS_EFFECT_CATALOG.md (v1.0, 2026-01-28)
//
// IMPORTANT RULES:
// - Do NOT reorder existing entries once you ship/save data.
// - Only append new statuses to the end.
// ============================================================================

namespace UltimateDungeon.StatusEffects
{
    /// <summary>
    /// Stable identifiers for all status effects.
    /// </summary>
    public enum StatusEffectId
    {
        None = 0,

        // --------------------------------------------------------------------
        // Control / Debuff
        // --------------------------------------------------------------------
        Control_Stunned = 1,
        Control_Paralyzed = 2,
        Control_Rooted = 3,
        Debuff_Silenced = 4,
        Debuff_Disarmed = 5,

        // --------------------------------------------------------------------
        // Damage over time
        // --------------------------------------------------------------------
        Dot_Poisoned = 6,
        Dot_Bleeding = 7,

        // --------------------------------------------------------------------
        // Movement / Speed
        // --------------------------------------------------------------------
        Debuff_Slowed = 8,
        Buff_Hasted = 9,

        // --------------------------------------------------------------------
        // Utility / State
        // --------------------------------------------------------------------
        Utility_Invisible = 10,
        Utility_Revealed = 11,

        // --------------------------------------------------------------------
        // Buffs
        // --------------------------------------------------------------------
        Buff_ReactiveArmor = 12,
        Buff_MagicReflection = 13
    }
}
