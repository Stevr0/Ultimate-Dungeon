// TargetingResolver.cs
// Ultimate Dungeon
//
// Purpose (ACTOR_MODEL.md + TARGETING_MODEL.md):
// - Pure rules layer used by server-side targeting validation and combat pre-checks.
// - Computes:
//   1) Eligibility: can the viewer even target this thing (alive, visible, optional range gate)
//   2) Disposition: Self / Friendly / Neutral / Hostile / Invalid (in LOCKED order)
//   3) Attack legality: hard scene gates + status gates + hostility requirement
//
// Design locks:
// - NO physics, NO scene lookups, NO networking side effects.
// - Scene gates come from SceneRuleRegistry.Current.Flags (passed in by caller).
// - Faction matrix / flagging is owned by FactionService (this class calls it; does not replace it).
//
// Notes:
// - Range / LoS are passed in as booleans computed by higher layers (physics / nav / etc.).
// - Visibility (invisible/reveal) is also passed in as a boolean.
// - This file is intentionally “boring” and deterministic so you can unit test it.

using System;
using UltimateDungeon.Actors;
using UltimateDungeon.Factions;
using UltimateDungeon.SceneRules;

namespace UltimateDungeon.Targeting
{
    /// <summary>
    /// UI-facing disposition result for viewer -> target.
    /// </summary>
    public enum TargetingDisposition : byte
    {
        Self = 0,
        Friendly = 1,
        Neutral = 2,
        Hostile = 3,
        Invalid = 4,
    }

    /// <summary>
    /// Why targeting/attacking was refused (small, deterministic set).
    ///
    /// IMPORTANT:
    /// - Keep these stable once UI starts using them.
    /// - TargetIntentValidator may later map/expand these to the richer deny reasons in TARGETING_MODEL.md.
    /// </summary>
    public enum TargetingDenyReason : byte
    {
        None = 0,

        // Eligibility / identity
        Denied_NullActor = 1,
        Denied_TargetDead = 2,
        Denied_AttackerDead = 3,
        Denied_TargetNotVisible = 4,

        // Scene gates
        Denied_SceneDisallowsHostileActors = 10,
        Denied_SceneDisallowsCombat = 11,
        Denied_SceneDisallowsDamage = 12,
        Denied_PvPNotAllowed = 13,

        // Mechanics gates
        Denied_NotHostile = 20,
        Denied_RangeOrLoS = 21,
        Denied_StatusGated = 22,
    }

    /// <summary>
    /// Bundle of common inputs for disposition resolution.
    /// </summary>
    public readonly struct DispositionQuery
    {
        public readonly ActorComponent Viewer;
        public readonly ActorComponent Target;

        // Eligibility gates (computed by caller):
        public readonly bool RequireRangeGate;
        public readonly bool IsInRange;           // only used if RequireRangeGate == true
        public readonly bool ViewerCanSeeTarget;  // false when target is invisible and viewer lacks reveal

        // Scene gates:
        public readonly SceneRuleFlags SceneFlags;

        public DispositionQuery(
            ActorComponent viewer,
            ActorComponent target,
            SceneRuleFlags sceneFlags,
            bool viewerCanSeeTarget,
            bool requireRangeGate,
            bool isInRange)
        {
            Viewer = viewer;
            Target = target;
            SceneFlags = sceneFlags;
            ViewerCanSeeTarget = viewerCanSeeTarget;
            RequireRangeGate = requireRangeGate;
            IsInRange = isInRange;
        }
    }

    /// <summary>
    /// Disposition result.
    /// </summary>
    public readonly struct DispositionResult
    {
        public readonly bool IsEligible;
        public readonly TargetingDisposition Disposition;
        public readonly TargetingDenyReason DenyReason;

        public DispositionResult(bool isEligible, TargetingDisposition disposition, TargetingDenyReason denyReason)
        {
            IsEligible = isEligible;
            Disposition = disposition;
            DenyReason = denyReason;
        }
    }

    /// <summary>
    /// Inputs for an attack legality evaluation.
    /// </summary>
    public readonly struct AttackQuery
    {
        public readonly ActorComponent Attacker;
        public readonly ActorComponent Target;

        // Mechanics gates (computed elsewhere):
        public readonly bool IsInRangeAndLoS;
        public readonly bool AttackerCanAttack;   // e.g., not stunned/paralyzed/silenced depending on your rules
        public readonly bool AttackerCanSeeTarget;

        // Scene gates:
        public readonly SceneRuleFlags SceneFlags;

        public AttackQuery(
            ActorComponent attacker,
            ActorComponent target,
            SceneRuleFlags sceneFlags,
            bool isInRangeAndLoS,
            bool attackerCanAttack,
            bool attackerCanSeeTarget)
        {
            Attacker = attacker;
            Target = target;
            SceneFlags = sceneFlags;
            IsInRangeAndLoS = isInRangeAndLoS;
            AttackerCanAttack = attackerCanAttack;
            AttackerCanSeeTarget = attackerCanSeeTarget;
        }
    }

    /// <summary>
    /// TargetingResolver (Pure Rules)
    /// </summary>
    public static class TargetingResolver
    {
        // ----------------------------
        // Eligibility (shared)
        // ----------------------------

        /// <summary>
        /// Returns true if the target is eligible for targeting.
        ///
        /// LOCKED order intent:
        /// - If dead -> not eligible
        /// - If not visible -> not eligible
        /// - If range gate required and out of range -> not eligible
        /// </summary>
        public static bool IsEligible(in DispositionQuery q, out TargetingDenyReason denyReason)
        {
            denyReason = TargetingDenyReason.None;

            if (q.Viewer == null || q.Target == null)
            {
                denyReason = TargetingDenyReason.Denied_NullActor;
                return false;
            }

            // Self is always eligible for disposition resolution.
            if (ReferenceEquals(q.Viewer, q.Target))
                return true;

            if (!q.Target.IsAlive)
            {
                denyReason = TargetingDenyReason.Denied_TargetDead;
                return false;
            }

            if (!q.ViewerCanSeeTarget)
            {
                denyReason = TargetingDenyReason.Denied_TargetNotVisible;
                return false;
            }

            if (q.RequireRangeGate && !q.IsInRange)
            {
                // We re-use RangeOrLoS deny; for targeting it's just “range”.
                denyReason = TargetingDenyReason.Denied_RangeOrLoS;
                return false;
            }

            return true;
        }

        // ----------------------------
        // Disposition (Self -> Eligibility -> Faction -> Scene Overrides)
        // ----------------------------

        /// <summary>
        /// Resolves disposition for UI and for downstream legality checks.
        ///
        /// LOCKED order:
        /// 1) Self
        /// 2) Eligibility
        /// 3) FactionService relationship
        /// 4) Scene overrides (safe scenes cannot produce Hostile)
        /// 5) PvP override (players vs players require PvPAllowed to ever be Hostile)
        /// </summary>
        public static DispositionResult ResolveDisposition(in DispositionQuery q)
        {
            // 1) Null guards
            if (q.Viewer == null || q.Target == null)
                return new DispositionResult(false, TargetingDisposition.Invalid, TargetingDenyReason.Denied_NullActor);

            // 2) Self
            if (ReferenceEquals(q.Viewer, q.Target))
                return new DispositionResult(true, TargetingDisposition.Self, TargetingDenyReason.None);

            // 3) Eligibility
            if (!IsEligible(q, out TargetingDenyReason eligibilityDeny))
                return new DispositionResult(false, TargetingDisposition.Invalid, eligibilityDeny);

            // 4) Base relation from faction service (includes flagging overrides like murderer/criminal)
            FactionRelation rel = FactionService.GetRelation(q.Viewer, q.Target);

            // 5) Scene override: if hostile actors are disallowed, we must never treat anything as Hostile.
            //    (This prevents the UI and validators from thinking attacks are possible in safe scenes.)
            if (!q.SceneFlags.HasFlag(SceneRuleFlags.HostileActorsAllowed) && rel == FactionRelation.Hostile)
            {
                // We intentionally degrade to Neutral (not Friendly), because "safe" scenes can still contain
                // things you don't want to encourage players to attack.
                rel = FactionRelation.Neutral;
            }

            // 6) PvP override: even if a future faction table ever marks player vs player as hostile,
            //    we clamp it unless PvPAllowed is true.
            bool bothPlayers = q.Viewer.Type == ActorType.Player && q.Target.Type == ActorType.Player;
            if (bothPlayers && !q.SceneFlags.HasFlag(SceneRuleFlags.PvPAllowed))
            {
                if (rel == FactionRelation.Hostile)
                    rel = FactionRelation.Neutral;
            }

            // 7) Map relation -> disposition
            TargetingDisposition disp = rel switch
            {
                FactionRelation.Friendly => TargetingDisposition.Friendly,
                FactionRelation.Neutral => TargetingDisposition.Neutral,
                FactionRelation.Hostile => TargetingDisposition.Hostile,
                _ => TargetingDisposition.Neutral,
            };

            return new DispositionResult(true, disp, TargetingDenyReason.None);
        }

        // ----------------------------
        // Attack legality
        // ----------------------------

        /// <summary>
        /// Pure legality check: "Can attacker attack target right now?"
        ///
        /// Required (v1):
        /// - attacker alive
        /// - target alive
        /// - scene CombatAllowed + DamageAllowed
        /// - range/LoS ok
        /// - attacker not status-gated
        /// - attacker can see target
        /// - disposition resolves to Hostile
        /// - if both are players => PvPAllowed
        /// </summary>
        public static bool CanAttack(in AttackQuery q, out TargetingDenyReason denyReason)
        {
            denyReason = TargetingDenyReason.None;

            if (q.Attacker == null || q.Target == null)
            {
                denyReason = TargetingDenyReason.Denied_NullActor;
                return false;
            }

            if (!q.Attacker.IsAlive)
            {
                denyReason = TargetingDenyReason.Denied_AttackerDead;
                return false;
            }

            if (!q.Target.IsAlive)
            {
                denyReason = TargetingDenyReason.Denied_TargetDead;
                return false;
            }

            // Scene gates
            if (!q.SceneFlags.HasFlag(SceneRuleFlags.CombatAllowed))
            {
                denyReason = TargetingDenyReason.Denied_SceneDisallowsCombat;
                return false;
            }

            if (!q.SceneFlags.HasFlag(SceneRuleFlags.DamageAllowed))
            {
                denyReason = TargetingDenyReason.Denied_SceneDisallowsDamage;
                return false;
            }

            // PvP gate
            bool bothPlayers = q.Attacker.Type == ActorType.Player && q.Target.Type == ActorType.Player;
            if (bothPlayers && !q.SceneFlags.HasFlag(SceneRuleFlags.PvPAllowed))
            {
                denyReason = TargetingDenyReason.Denied_PvPNotAllowed;
                return false;
            }

            // Mechanics gates
            if (!q.IsInRangeAndLoS)
            {
                denyReason = TargetingDenyReason.Denied_RangeOrLoS;
                return false;
            }

            if (!q.AttackerCanAttack)
            {
                denyReason = TargetingDenyReason.Denied_StatusGated;
                return false;
            }

            if (!q.AttackerCanSeeTarget)
            {
                denyReason = TargetingDenyReason.Denied_TargetNotVisible;
                return false;
            }

            // Hostility requirement
            // NOTE: For attack we require range eligibility already, so RequireRangeGate=false here.
            DispositionQuery dq = new DispositionQuery(
                viewer: q.Attacker,
                target: q.Target,
                sceneFlags: q.SceneFlags,
                viewerCanSeeTarget: q.AttackerCanSeeTarget,
                requireRangeGate: false,
                isInRange: true);

            DispositionResult disp = ResolveDisposition(dq);

            if (!disp.IsEligible)
            {
                denyReason = disp.DenyReason;
                return false;
            }

            if (disp.Disposition != TargetingDisposition.Hostile)
            {
                denyReason = TargetingDenyReason.Denied_NotHostile;
                return false;
            }

            denyReason = TargetingDenyReason.None;
            return true;
        }

        // ----------------------------
        // Convenience helpers for callers
        // ----------------------------

        /// <summary>
        /// Helper for the common case: resolve disposition using the currently active scene flags.
        ///
        /// Use this in UI/debug code. Server validators should pass explicit flags for determinism.
        /// </summary>
        public static DispositionResult ResolveDispositionUsingCurrentScene(
            ActorComponent viewer,
            ActorComponent target,
            bool viewerCanSeeTarget,
            bool requireRangeGate,
            bool isInRange)
        {
            SceneRuleFlags flags = SceneRuleRegistry.HasCurrent ? SceneRuleRegistry.Current.Flags : SceneRuleFlags.None;

            DispositionQuery q = new DispositionQuery(
                viewer,
                target,
                flags,
                viewerCanSeeTarget,
                requireRangeGate,
                isInRange);

            return ResolveDisposition(q);
        }

        /// <summary>
        /// Helper for the common case: check attack legality using current scene flags.
        /// </summary>
        public static bool CanAttackUsingCurrentScene(
            ActorComponent attacker,
            ActorComponent target,
            bool isInRangeAndLoS,
            bool attackerCanAttack,
            bool attackerCanSeeTarget,
            out TargetingDenyReason denyReason)
        {
            SceneRuleFlags flags = SceneRuleRegistry.HasCurrent ? SceneRuleRegistry.Current.Flags : SceneRuleFlags.None;

            AttackQuery q = new AttackQuery(
                attacker,
                target,
                flags,
                isInRangeAndLoS,
                attackerCanAttack,
                attackerCanSeeTarget);

            return CanAttack(q, out denyReason);
        }
    }
}
