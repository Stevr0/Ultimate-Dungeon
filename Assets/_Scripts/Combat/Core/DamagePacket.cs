using System;

namespace UltimateDungeon.Combat
{
    /// <summary>
    /// DamagePacket
    /// ------------
    /// A single, atomic request to apply damage to an Actor.
    ///
    /// CORE DESIGN RULE:
    /// -----------------
    /// - ALL damage in the game flows through this model.
    ///   Weapon hits, spells, DoTs, traps, hit-spell procs, etc.
    ///
    /// Why this matters:
    /// - Centralizes damage handling
    /// - Makes logging, replay, and debugging possible
    /// - Keeps Combat Core deterministic and auditable
    ///
    /// DamagePacket is DATA ONLY.
    /// - No logic
    /// - No side effects
    /// - Resolved and applied by CombatResolver on the server
    /// </summary>
    [Serializable]
    public struct DamagePacket
    {
        // -------------------------
        // Identity
        // -------------------------

        /// <summary>
        /// NetworkObjectId of the actor that caused the damage.
        /// Can be 0 for environmental or system damage.
        /// </summary>
        public ulong sourceActorNetId;

        /// <summary>
        /// NetworkObjectId of the actor receiving the damage.
        /// </summary>
        public ulong targetActorNetId;

        // -------------------------
        // Damage definition
        // -------------------------

        /// <summary>
        /// Final damage amount AFTER all rolls and modifiers.
        /// CombatResolver must ensure this is >= 0.
        /// </summary>
        public int finalDamageAmount;

        /// <summary>
        /// Damage channel used for resist calculations.
        ///
        /// v0.1:
        /// - Resists are not applied yet
        /// - This is carried forward for future versions
        /// </summary>
        public DamageType damageType;

        // -------------------------
        // Context / Metadata
        // -------------------------

        /// <summary>
        /// Deterministic seed used for this damage event.
        ///
        /// Why store it?
        /// - Allows debugging and replay of combat events
        /// - Lets downstream systems (logging, analytics) reproduce rolls
        ///
        /// v0.1:
        /// - Optional; can be 0 if not used yet
        /// </summary>
        public int seed;

        /// <summary>
        /// Bitfield describing the source of damage.
        /// Used for:
        /// - Proc rules (on-hit effects)
        /// - UI ("Spell", "Weapon", "DoT")
        /// - Logging/analytics
        /// </summary>
        public DamageTags tags;

        // -------------------------
        // Convenience
        // -------------------------

        /// <summary>
        /// Returns true if this packet represents zero damage.
        /// CombatResolver may skip application in that case.
        /// </summary>
        public bool IsZeroDamage => finalDamageAmount <= 0;

        /// <summary>
        /// Helper constructor for common weapon-hit cases.
        /// </summary>
        public static DamagePacket CreateWeaponHit(
            ulong sourceNetId,
            ulong targetNetId,
            int finalDamage,
            DamageType type,
            int seed = 0)
        {
            return new DamagePacket
            {
                sourceActorNetId = sourceNetId,
                targetActorNetId = targetNetId,
                finalDamageAmount = finalDamage,
                damageType = type,
                seed = seed,
                tags = DamageTags.Weapon
            };
        }
    }

    /// <summary>
    /// DamageTags
    /// ----------
    /// Describes the origin/category of damage.
    ///
    /// Flags allow combination (e.g. Weapon | Proc).
    /// </summary>
    [Flags]
    public enum DamageTags
    {
        None = 0,

        // Primary sources
        Weapon = 1 << 0,
        Spell = 1 << 1,
        Dot = 1 << 2,
        Trap = 1 << 3,
        Environment = 1 << 4,

        // Modifiers / sub-sources
        Proc = 1 << 8,
        Reflect = 1 << 9,
        Leech = 1 << 10
    }
}
