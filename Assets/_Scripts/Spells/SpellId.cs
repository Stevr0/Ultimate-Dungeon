// ============================================================================
// SpellId.cs
// ----------------------------------------------------------------------------
// Authoritative enum generated from SPELL_ID_CATALOG.md (v1.0, 2026-01-28)
//
// IMPORTANT RULES:
// - Do NOT reorder existing entries once you ship/save data.
// - Only append new spells to the end of a circle/range.
// - If you ever need to deprecate a spell, keep the enum value and mark it
//   as deprecated in documentation + SpellDef assets.
// ============================================================================

namespace UltimateDungeon.Spells
{
    /// <summary>
    /// Stable identifiers for all Magery spells (Circles 1–8).
    ///
    /// This is the code representation of SPELL_ID_CATALOG.md and should match it exactly.
    /// </summary>
    public enum SpellId
    {
        None = 0,

        // --------------------------------------------------------------------
        // Circle 1 — First Circle (1–8)
        // --------------------------------------------------------------------
        Clumsy = 1,
        CreateFood = 2,
        Feeblemind = 3,
        Heal = 4,
        MagicArrow = 5,
        NightSight = 6,
        ReactiveArmor = 7,
        Weaken = 8,

        // --------------------------------------------------------------------
        // Circle 2 — Second Circle (101–108)
        // --------------------------------------------------------------------
        Agility = 101,
        Cunning = 102,
        Cure = 103,
        Harm = 104,
        MagicTrap = 105,
        RemoveTrap = 106,
        Protection = 107,
        Strength = 108,

        // --------------------------------------------------------------------
        // Circle 3 — Third Circle (201–208)
        // --------------------------------------------------------------------
        Bless = 201,
        Fireball = 202,
        MagicLock = 203,
        Poison = 204,
        Telekinesis = 205,
        Teleport = 206,
        Unlock = 207,
        WallOfStone = 208,

        // --------------------------------------------------------------------
        // Circle 4 — Fourth Circle (301–308)
        // --------------------------------------------------------------------
        ArchCure = 301,
        ArchProtection = 302,
        Curse = 303,
        FireField = 304,
        GreaterHeal = 305,
        Lightning = 306,
        ManaDrain = 307,
        Recall = 308,

        // --------------------------------------------------------------------
        // Circle 5 — Fifth Circle (401–408)
        // --------------------------------------------------------------------
        BladeSpirits = 401,
        DispelField = 402,
        Incognito = 403,
        MagicReflection = 404,
        MindBlast = 405,
        Paralyze = 406,
        PoisonField = 407,
        SummonCreature = 408,

        // --------------------------------------------------------------------
        // Circle 6 — Sixth Circle (501–508)
        // --------------------------------------------------------------------
        Dispel = 501,
        EnergyBolt = 502,
        Explosion = 503,
        Invisibility = 504,
        Mark = 505,
        MassCurse = 506,
        ParalyzeField = 507,
        Reveal = 508,

        // --------------------------------------------------------------------
        // Circle 7 — Seventh Circle (601–608)
        // --------------------------------------------------------------------
        ChainLightning = 601,
        EnergyField = 602,
        Flamestrike = 603,
        GateTravel = 604,
        ManaVampire = 605,
        MassDispel = 606,
        MeteorSwarm = 607,
        Polymorph = 608,

        // --------------------------------------------------------------------
        // Circle 8 — Eighth Circle (701–708)
        // --------------------------------------------------------------------
        Earthquake = 701,
        EnergyVortex = 702,
        Resurrection = 703,
        SummonAirElemental = 704,
        SummonDaemon = 705,
        SummonEarthElemental = 706,
        SummonFireElemental = 707,
        SummonWaterElemental = 708
    }
}
