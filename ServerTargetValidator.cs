// ServerTargetValidator.cs
// Ultimate Dungeon
//
// Purpose:
// - Server-side "glue" for authoritative target validation.
// - Converts network IDs -> ActorComponent
// - Gathers scene flags from SceneRuleRegistry
// - Applies mechanic gates (range/LoS, visibility, status)
// - Delegates final legality to TargetingResolver (pure rules)
//
// Why this exists (clean layering):
// - TargetingResolver is pure rules (no Unity physics, no networking).
// - ServerTargetValidator is where networking + scene registry + physics gates meet.
// - Combat/interaction code calls this to avoid duplicating validation logic everywhere.
//
// Notes:
// - Minimal for the first playable slice.
// - Later extensions: LoS raycasts, safe-zone exceptions, corpse rules, stealth/reveal, etc.

using UltimateDungeon.Actors;
using UltimateDungeon.Combat;
using UltimateDungeon.SceneRules;
using Unity.Netcode;
using UnityEngine;

namespace UltimateDungeon.Targeting
{
    /// <summary>
    /// Standard result object for server validation.
    /// Keeps all callers consistent.
    /// </summary>
    public readonly struct ServerTargetValidationResult
    {
        public readonly bool Allowed;
        public readonly TargetingDenyReason DenyReason;
        public readonly ActorComponent TargetActor;

        public ServerTargetValidationResult(bool allowed, TargetingDenyReason denyReason, ActorComponent targetActor)
        {
            Allowed = allowed;
            DenyReason = denyReason;
            TargetActor = targetActor;
        }

        public static ServerTargetValidationResult Denied(TargetingDenyReason reason)
            => new ServerTargetValidationResult(false, reason, null);

        public static ServerTargetValidationResult AllowedWith(ActorComponent target)
            => new ServerTargetValidationResult(true, TargetingDenyReason.None, target);
    }

    /// <summary>
    /// ServerTargetValidator
    /// --------------------
    /// Server-only helper. Do not call on clients.
    /// </summary>
    public static class ServerTargetValidator
    {
        /// <summary>
        /// Validates an attack intent from <paramref name="attacker"/> to a target NetworkObjectId.
        ///
        /// This is the shared, canonical server validation for "start attack" requests.
        ///
        /// Inputs:
        /// - attacker: the attacker's ActorComponent (identity surface)
        /// - targetNetId: NetworkObjectId of the intended target
        /// - maxRangeMeters/rangeBufferMeters: simple v1 melee range gate
        /// - attackerCanAttack: status gate (stun/paralyze/etc.) computed by caller
        /// - attackerCanSeeTarget: visibility gate computed by caller
        ///
        /// Output:
        /// - targetCombatActor: resolved ICombatActor (needed to start AttackLoop)
        /// </summary>
        public static ServerTargetValidationResult ValidateAttackByNetId(
            ActorComponent attacker,
            ulong targetNetId,
            float maxRangeMeters,
            float rangeBufferMeters,
            bool attackerCanAttack,
            bool attackerCanSeeTarget,
            out ICombatActor targetCombatActor)
        {
            targetCombatActor = null;

            // -----------------------------
            // Sanity / identity checks
            // -----------------------------
            if (attacker == null)
                return ServerTargetValidationResult.Denied(TargetingDenyReason.Denied_NullActor);

            if (!attacker.IsAlive)
                return ServerTargetValidationResult.Denied(TargetingDenyReason.Denied_AttackerDead);

            // NetworkManager can be null during shutdown / scene transitions.
            if (NetworkManager.Singleton == null)
                return ServerTargetValidationResult.Denied(TargetingDenyReason.Denied_NullActor);

            // Resolve target NetworkObject.
            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetId, out NetworkObject targetNetObj))
                return ServerTargetValidationResult.Denied(TargetingDenyReason.Denied_NullActor);

            // Target must have ActorComponent (your universal identity surface).
            if (!targetNetObj.TryGetComponent(out ActorComponent targetActor))
                return ServerTargetValidationResult.Denied(TargetingDenyReason.Denied_NullActor);

            if (!targetActor.IsAlive)
                return ServerTargetValidationResult.Denied(TargetingDenyReason.Denied_TargetDead);

            // Target must be something combat can attack.
            // (If you later allow attacking "Objects" like doors/chests, you can relax this.)
            if (!targetNetObj.TryGetComponent(out targetCombatActor))
                return ServerTargetValidationResult.Denied(TargetingDenyReason.Denied_NullActor);

            // -----------------------------
            // Mechanics gates (range / LoS)
            // -----------------------------
            // v1: range-only. Later: add LoS raycast and incorporate it into inRangeAndLos.
            Vector3 attackerPos = attacker.transform.position;
            Vector3 targetPos = targetNetObj.transform.position;
            float dist = Vector3.Distance(attackerPos, targetPos);

            float maxAllowed = maxRangeMeters + rangeBufferMeters;
            bool inRangeAndLos = dist <= maxAllowed; // TODO: line of sight raycast

            // -----------------------------
            // Scene gates (authoritative)
            // -----------------------------
            // If no provider registered yet, this defaults to None (safe) and combat will be refused.
            SceneRuleFlags flags = SceneRuleRegistry.HasCurrent
                ? SceneRuleRegistry.Current.Flags
                : SceneRuleFlags.None;

            // -----------------------------
            // Delegate to pure legality
            // -----------------------------
            AttackQuery q = new AttackQuery(
                attacker: attacker,
                target: targetActor,
                sceneFlags: flags,
                isInRangeAndLoS: inRangeAndLos,
                attackerCanAttack: attackerCanAttack,
                attackerCanSeeTarget: attackerCanSeeTarget);

            if (!TargetingResolver.CanAttack(q, out TargetingDenyReason deny))
                return ServerTargetValidationResult.Denied(deny);

            return ServerTargetValidationResult.AllowedWith(targetActor);
        }
    }
}
