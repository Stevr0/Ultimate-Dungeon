namespace UltimateDungeon.Items
{
    /// <summary>
    /// AffixId (AUTHORITATIVE, APPEND-ONLY)
    /// ===================================
    ///
    /// Stable identifiers for item affixes.
    ///
    /// Source of truth:
    /// - ITEM_AFFIX_CATALOG.md (append-only)
    ///
    /// CRITICAL RULES:
    /// - Never reorder values once shipped.
    /// - Never rename an existing id once shipped.
    /// - Only append new entries at the end.
    ///
    /// Why?
    /// - These ids may appear in saved ItemInstances (and potentially network messages).
    /// - Reordering/renaming would corrupt save data and desync clients.
    /// </summary>
    public enum AffixId
    {
        // --------------------------------------------------------------------
        // COMBAT — CHANCE & DAMAGE
        // --------------------------------------------------------------------
        Combat_HitChance = 0,
        Combat_DefenseChance = 1,
        Combat_DamageIncrease = 2,
        Combat_SwingSpeed = 3,

        // --------------------------------------------------------------------
        // RESISTS
        // --------------------------------------------------------------------
        Resist_Physical = 4,
        Resist_Fire = 5,
        Resist_Cold = 6,
        Resist_Poison = 7,
        Resist_Energy = 8,

        // --------------------------------------------------------------------
        // MAGIC — CASTING MODIFIERS
        // --------------------------------------------------------------------
        Magic_FasterCasting = 9,

        // --------------------------------------------------------------------
        // VITAL MODIFIERS
        // --------------------------------------------------------------------
        Vital_MaxHP = 10,
        Vital_MaxStamina = 11,
        Vital_MaxMana = 12,

        // --------------------------------------------------------------------
        // WEAPON PROCS — HIT SPELLS
        // --------------------------------------------------------------------
        Hit_Lightning = 13,
        Hit_Fireball = 14,
        Hit_Harm = 15,

        // --------------------------------------------------------------------
        // WEAPON PROCS — HIT LEACHES
        // --------------------------------------------------------------------
        Leech_Life = 16,
        Leech_Mana = 17,
        Leech_Stamina = 18,

        // --------------------------------------------------------------------
        // STAT MODIFIERS
        // --------------------------------------------------------------------
        Stat_MaxStrength = 19,
        Stat_MaxDexterity = 20,
        Stat_MaxInteligence = 21,

        // --------------------------------------------------------------------
        // REGENERATION MODIFIERS
        // --------------------------------------------------------------------
        Regenerate_Health = 22,
        Regenerate_Stamina = 23,
        Regenerate_Mana = 24,

        // --------------------------------------------------------------------
        // MOVE - SPEED MODIFIERS
        // --------------------------------------------------------------------
        Move_Speed = 25,


        // --------------------------------------------------------------------
        // APPEND NEW AFFIX IDS BELOW THIS LINE ONLY.
        // --------------------------------------------------------------------
    }
}
