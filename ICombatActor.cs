using UnityEngine;
using Unity.Netcode;

namespace UltimateDungeon.Combat
{
    /// <summary>
    /// ICombatActor
    /// -----------
    /// Minimal, combat-facing contract for anything that can participate in combat.
    ///
    /// Why an interface?
    /// - Keeps Combat Core decoupled from "Player" vs "Enemy" implementation details.
    /// - Lets us plug in NPCs/Summons later without rewriting CombatResolver.
    ///
    /// Design rules this enforces:
    /// - Server authoritative: Combat systems read state; only server applies outcomes.
    /// - One damage path: CombatResolver will use this contract to apply DamagePackets.
    ///
    /// IMPORTANT:
    /// - This interface is intentionally small.
    /// - It should NOT expose inventory, equipment, UI, or other game-specific data.
    /// - Add methods/fields only when Combat Core truly needs them.
    /// </summary>
    public interface ICombatActor
    {
        // -------------------------
        // Identity / Networking
        // -------------------------

        /// <summary>
        /// The NetworkObject for this actor.
        /// Combat uses this for stable identity (NetworkObjectId) and server checks.
        /// </summary>
        NetworkObject NetObject { get; }

        /// <summary>
        /// Convenience stable ID for dictionaries/maps.
        /// Equivalent to NetObject.NetworkObjectId.
        /// </summary>
        ulong NetId { get; }

        /// <summary>
        /// World transform (position) used for range checks.
        /// </summary>
        Transform Transform { get; }

        // -------------------------
        // Life state
        // -------------------------

        /// <summary>
        /// True if this actor is alive.
        /// Combat must not resolve swings against dead targets.
        /// </summary>
        bool IsAlive { get; }

        // -------------------------
        // Action gates
        // -------------------------

        /// <summary>
        /// True if this actor is currently allowed to perform weapon attacks.
        ///
        /// In full implementation this comes from StatusEffectSystem via
        /// PlayerCombatStats.canAttack.
        ///
        /// For v0.1 skeleton, you can return true always.
        /// </summary>
        bool CanAttack { get; }

        // -------------------------
        // Core timing inputs
        // -------------------------

        /// <summary>
        /// Base swing time for this actor's current attack context.
        ///
        /// In full implementation:
        /// - weapon swing speed comes from equipped weapon profile (ItemDef)
        /// - if unarmed, use PlayerDefinition.unarmedBaseSwingSpeedSeconds
        /// - Combat Core then applies DEX bonus, swing-speed affix, and status multipliers
        ///
        /// For v0.1 skeleton:
        /// - return a constant like 2.0f so we can test the loop.
        /// </summary>
        float GetBaseSwingTimeSeconds();

        // -------------------------
        // Weapon + combat stats
        // -------------------------

        /// <summary>
        /// Minimum weapon damage (snapshot-derived).
        /// </summary>
        int GetWeaponMinDamage();

        /// <summary>
        /// Maximum weapon damage (snapshot-derived).
        /// </summary>
        int GetWeaponMaxDamage();

        /// <summary>
        /// Damage increase percentage (e.g., 0.15 = +15%).
        /// </summary>
        float GetDamageIncreasePct();

        /// <summary>
        /// Weapon damage type for this actor.
        /// </summary>
        DamageType GetWeaponDamageType();

        /// <summary>
        /// Attacker hit chance increase (HCI), returned as a percentage (0.10 = +10%).
        /// </summary>
        float GetAttackerHitChancePct();

        /// <summary>
        /// Defender chance to defend (DCI), returned as a percentage (0.10 = +10%).
        /// </summary>
        float GetDefenderDefenseChancePct();

        /// <summary>
        /// Stamina cost per swing for this actor's weapon.
        /// </summary>
        int GetStaminaCostPerSwing();

        /// <summary>
        /// Attempts to spend stamina on the server.
        /// </summary>
        bool TrySpendStamina(int amount);

        // -------------------------
        // Damage application
        // -------------------------

        /// <summary>
        /// Apply damage to this actor.
        ///
        /// RULE:
        /// - Must only be called on the server.
        /// - Clients must never mutate HP.
        ///
        /// v0.1 expectation:
        /// - You can implement this by calling your existing PlayerVitals.TakeDamage(int)
        ///   (or equivalent) on the server.
        /// </summary>
        /// <param name="amount">Final damage amount (already resolved).</param>
        /// <param name="damageType">Damage channel for later resist logic (v0.1 can ignore).</param>
        /// <param name="source">Optional source actor (can be null).</param>
        void ApplyDamageServer(int amount, DamageType damageType, ICombatActor source);
    }

    /// <summary>
    /// DamageType
    /// ---------
    /// Shared damage channels used across Combat, Magic, Status DoTs, etc.
    ///
    /// We define it here in Combat for v0.1 convenience.
    /// If you already have a shared enum elsewhere, move this and update references.
    /// </summary>
    public enum DamageType
    {
        Physical = 0,
        Fire = 1,
        Cold = 2,
        Poison = 3,
        Energy = 4
    }
}
