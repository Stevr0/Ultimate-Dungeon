// FactionService.cs
// Ultimate Dungeon
//
// Purpose (ACTOR_MODEL.md):
// - Pure rules: given two factions (and optional per-actor flags), determine relationship.
// - This is used by TargetingResolver and ServerTargetValidator.
//
// Design locks:
// - Deterministic and server-authoritative.
// - No scene knowledge here (SceneRuleFlags handled elsewhere).
// - Keep "what faction is hostile" separate from targeting legality (range/LOS/etc.).
//
// Notes:
// - This is a v1 implementation using an enum FactionId.
// - Later, you can data-drive faction relationships from ScriptableObjects.

using System;
using UltimateDungeon.Actors;

namespace UltimateDungeon.Factions
{
    /// <summary>
    /// High-level relationship from the perspective of an observer.
    /// </summary>
    public enum FactionRelation : byte
    {
        Self = 0,
        Friendly = 1,
        Neutral = 2,
        Hostile = 3,
    }

    /// <summary>
    /// FactionService (Pure Rules)
    /// --------------------------
    /// This class has no Unity dependencies and can be unit-tested.
    /// </summary>
    public static class FactionService
    {
        /// <summary>
        /// Determines how <paramref name="viewer"/> should perceive <paramref name="target"/>.
        /// 
        /// IMPORTANT:
        /// - This does NOT decide whether an action is allowed (that's TargetingResolver).
        /// - This does NOT check scene gates (SceneRuleFlags) or geometry.
        /// </summary>
        public static FactionRelation GetRelation(ActorComponent viewer, ActorComponent target)
        {
            if (viewer == null || target == null)
                throw new ArgumentNullException("viewer/target");

            if (ReferenceEquals(viewer, target))
                return FactionRelation.Self;

            // If either actor is dead, we still return a relation for UI (corpse label colors etc.).
            // Legality for interacting/attacking corpses is handled elsewhere.

            // Base relation from faction IDs.
            FactionRelation baseRelation = GetRelation(viewer.Faction, target.Faction);

            // Player flagging (v1): Criminal/Murderer flips to Hostile against Players/Guards.
            // This mirrors the ACTOR_MODEL intent: players can become hostile based on flagging,
            // not by changing their faction to "Monsters".
            if (target.Type == ActorType.Player)
            {
                if (target.IsMurderer)
                {
                    // Murderers are hostile to Players and Guards.
                    if (viewer.Faction == FactionId.Players || viewer.Faction == FactionId.Guards || viewer.Faction == FactionId.Village)
                        return FactionRelation.Hostile;
                }

                if (target.IsCriminal)
                {
                    // Criminals are hostile to Guards (and optionally village).
                    if (viewer.Faction == FactionId.Guards)
                        return FactionRelation.Hostile;
                }
            }

            // Controller inheritance: a pet/summon inherits hostility from its controller.
            // For v1 we keep this as an "advisory" rule; TargetingResolver can choose
            // to consult controller relation if needed.
            // (Actual controller lookup is done elsewhere; here we just keep the hook.)

            return baseRelation;
        }

        /// <summary>
        /// Basic faction-to-faction relationship table.
        /// Keep this small and deterministic for v1.
        /// </summary>
        public static FactionRelation GetRelation(FactionId viewerFaction, FactionId targetFaction)
        {
            // Neutral baseline FIRST (even if equal)
            if (viewerFaction == FactionId.Neutral || targetFaction == FactionId.Neutral)
                return FactionRelation.Neutral;

            if (viewerFaction == targetFaction)
                return FactionRelation.Friendly;

            // Players vs Monsters
            if (viewerFaction == FactionId.Players && targetFaction == FactionId.Monsters)
                return FactionRelation.Hostile;

            if (viewerFaction == FactionId.Monsters && targetFaction == FactionId.Players)
                return FactionRelation.Hostile;

            // Guards vs Monsters
            if (viewerFaction == FactionId.Guards && targetFaction == FactionId.Monsters)
                return FactionRelation.Hostile;

            if (viewerFaction == FactionId.Monsters && targetFaction == FactionId.Guards)
                return FactionRelation.Hostile;

            // Village friendliness
            if (viewerFaction == FactionId.Village &&
                (targetFaction == FactionId.Players || targetFaction == FactionId.Guards))
                return FactionRelation.Friendly;

            if ((viewerFaction == FactionId.Players || viewerFaction == FactionId.Guards) &&
                targetFaction == FactionId.Village)
                return FactionRelation.Friendly;

            return FactionRelation.Neutral;
        }


        /// <summary>
        /// Convenience helper.
        /// </summary>
        public static bool IsHostile(ActorComponent viewer, ActorComponent target)
        {
            return GetRelation(viewer, target) == FactionRelation.Hostile;
        }
    }
}
