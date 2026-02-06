using System;
using System.Collections.Generic;

namespace UltimateDungeon.StatusEffects
{
    /// <summary>
    /// Lightweight snapshot of an active status effect.
    /// </summary>
    public readonly struct ActiveStatusEffect
    {
        public readonly StatusEffectId Id;
        public readonly float RemainingSeconds;
        public readonly int Stacks;

        public ActiveStatusEffect(StatusEffectId id, float remainingSeconds, int stacks)
        {
            Id = id;
            RemainingSeconds = remainingSeconds;
            Stacks = stacks;
        }
    }

    /// <summary>
    /// Runtime provider for active status effects.
    /// </summary>
    public interface IStatusEffectRuntime
    {
        event Action StatusesChanged;

        /// <summary>
        /// Populate the supplied list with active status effects.
        /// Returns the number of entries written.
        /// </summary>
        int GetActiveStatuses(List<ActiveStatusEffect> buffer);
    }
}
