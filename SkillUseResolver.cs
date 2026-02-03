// ============================================================================
// SkillUseResolver.cs
// ----------------------------------------------------------------------------
// Server-authoritative bridge between gameplay actions and progression.
//
// Responsibilities:
// 1) Resolve a skill check result (success/fail) deterministically
// 2) Attempt skill gain via SkillGainSystem (cap/lock aware)
// 3) Attempt stat gain via StatGainSystem (optional; UO-like)
// 4) Recompute vitals once if stats changed
//
// This is scaffolding — exact chance curves are NOT locked yet.
// ============================================================================


using UnityEngine;
using UltimateDungeon.Players;
using UltimateDungeon.Skills;


namespace UltimateDungeon.Progression
{
    /// <summary>
    /// SkillUseOutcome
    /// --------------
    /// Caller reports the semantic outcome of an action.
    ///
    /// Why not just bool?
    /// - Some actions can be "partial" (e.g., resisted spell, glancing hit).
    /// - We can decide whether partial outcomes can still grant gains.
    /// </summary>
    public enum SkillUseOutcome
    {
        Fail = 0,
        Partial = 1,
        Success = 2
    }


    /// <summary>
    /// SkillUseContext
    /// --------------
    /// Purely informational for logs/tuning.
    /// This can help you later apply different gain throttles per context.
    /// </summary>
    public enum SkillUseContext
    {
        Combat = 0,
        Magic = 1,
        Crafting = 2,
        Utility = 3
    }


    /// <summary>
    /// SkillUseRequest
    /// ---------------
    /// Input to the resolver.
    ///
    /// Caller supplies:
    /// - which skill was used
    /// - difficulty/opposition value (0..100 recommended)
    /// - outcome (success/fail)
    /// - a stable nonce (optional) so repeated actions don't share a seed
    /// </summary>
    public struct SkillUseRequest
    {
        public SkillId skillId;
    }
}