// ============================================================================
// SkillGainSystem.cs
// ----------------------------------------------------------------------------
// Server-authoritative system that applies skill gains and enforces:
// - Total Skill Cap = 700
// - Manual cap behavior (+ / - / lock)
// - 0.1 step increments (proposed default)
//
// This is intentionally "math-only" and can be called by combat/crafting/etc.
// ============================================================================

using UnityEngine;
using UltimateDungeon.Skills;

namespace UltimateDungeon.Players
{
    /// <summary>
    /// SkillGainSystem
    /// --------------
    /// Applies a skill gain attempt to a player's skill book.
    ///
    /// NOTE:
    /// - This does NOT decide when to roll gains.
    /// - Other systems call TryApplyGain() when an action meaningfully used a skill.
    /// </summary>
    public static class SkillGainSystem
    {
        // UO-like step size. Locked as "float support" by docs; actual step can be tuned.
        // If you later want 1.0 increments, change this constant.
        public const float GainStep = 0.1f;

        // Total skill cap is locked to 700 by PlayerDefinition.
        // We pass it in explicitly so you can later move caps to WorldRules.

        /// <summary>
        /// Attempts to apply a gain to the given skill.
        ///
        /// Returns true if the skill increased.
        /// Returns false if blocked by lock state or cap rules.
        ///
        /// CAP RULE (LOCKED):
        /// - If total skills < cap: gain is allowed.
        /// - If total skills == cap: gain is only allowed if at least one skill is set to Decrease (-).
        ///   If none are set to Decrease, gain is blocked.
        /// - When gaining at cap, we reduce Decrease skill(s) deterministically.
        /// </summary>
        public static bool TryApplyGain(
            PlayerSkillBook book,
            SkillId skillToGain,
            float totalSkillCap
        )
        {
            if (book == null)
            {
                Debug.LogError("[SkillGainSystem] book is null.");
                return false;
            }

            // Rule 1: Skill must be set to Increase (+)
            if (book.GetLockState(skillToGain) != SkillLockState.Increase)
                return false;

            float total = book.GetTotalSkills();

            // Not at cap: simply gain.
            if (total + GainStep <= totalSkillCap)
            {
                book.AddValue(skillToGain, GainStep);
                return true;
            }

            // At/over cap: manual rule requires at least one Decrease skill.
            if (!book.HasAnyDecreaseSkills())
            {
                // Player did not choose a skill to decrease -> no gain.
                return false;
            }

            // We need to make room by decreasing skills.
            // Determine how much room we need to gain one step.
            float requiredRoom = (total + GainStep) - totalSkillCap;

            // Reduce decrement candidates until we've made enough room.
            // Deterministic: always choose the best candidate each time.
            float remaining = requiredRoom;

            int safety = 1000; // prevents infinite loops if something goes wrong

            while (remaining > 0f && safety-- > 0)
            {
                if (!book.TryGetDecreaseCandidate(out SkillId candidate))
                {
                    // No valid candidate found; cannot make room.
                    return false;
                }

                float current = book.GetValue(candidate);

                // Reduce by the same step size. Clamp at 0.
                float newValue = Mathf.Max(0f, current - GainStep);

                // Compute how much room we actually created.
                float created = current - newValue;

                book.SetValue(candidate, newValue);
                remaining -= created;

                // If created is 0 (skill already 0), we could loop forever.
                // But safety counter protects us.
            }

            if (safety <= 0)
            {
                Debug.LogError("[SkillGainSystem] Safety break hit while decreasing skills.");
                return false;
            }

            // Now we have room: apply the gain.
            book.AddValue(skillToGain, GainStep);
            return true;
        }
    }
}