using System;
using UnityEngine;

namespace UltimateDungeon.Players
{
    /// <summary>
    /// PlayerTargeting
    /// --------------
    /// Holds the local player's currently selected target.
    ///
    /// FIX:
    /// - Selection must NOT keep combat engagement alive.
    /// - Player must be able to STOP attacking WITHOUT clearing the target.
    ///
    /// Why this bug showed up:
    /// - After we changed AttackLoop to "pause out of range" (chase semantics),
    ///   the engagement can stay alive indefinitely until the player explicitly cancels it.
    /// - Previously, you only stopped attacking when you cleared the target.
    /// - Result: if you keep a monster targeted, you never send StopAttack -> AttackLoop stays running ->
    ///   CombatStateTracker correctly keeps you InCombat.
    ///
    /// Design rule (locked):
    /// - Targeting/selection is independent from engagement.
    /// - Cancelling engagement should NOT require clearing selection.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerTargeting : MonoBehaviour
    {
        public event Action<GameObject> OnTargetSet;
        public event Action OnTargetCleared;

        public GameObject CurrentTarget { get; private set; }

        /// <summary>
        /// Set current selection.
        ///
        /// IMPORTANT:
        /// - Changing selection should stop any existing engagement.
        ///   (If the player wants to attack the newly selected target, they must double-click again.)
        /// </summary>
        public void SetTarget(GameObject target)
        {
            if (target == null)
            {
                ClearTarget();
                return;
            }

            // If the player is switching targets, stop attacking the previous one,
            // but KEEP selection behavior separate.
            if (CurrentTarget != null && CurrentTarget != target)
            {
                StopAttackButKeepTarget();
            }

            CurrentTarget = target;
            Debug.Log($"[PlayerTargeting] Target set to: {target.name}");

            OnTargetSet?.Invoke(target);
        }

        /// <summary>
        /// Clears selection.
        /// Clearing selection SHOULD also cancel engagement.
        /// </summary>
        public void ClearTarget()
        {
            CurrentTarget = null;
            Debug.Log("[PlayerTargeting] Target cleared");

            // Stop attacking when target is cleared.
            StopAttackButKeepTarget();

            OnTargetCleared?.Invoke();
        }

        /// <summary>
        /// Cancels combat engagement WITHOUT clearing the current target.
        ///
        /// Hook this from:
        /// - Right click move (UO behavior: moving cancels attack intent)
        /// - ESC key
        /// - Any UI "Stop" button
        /// </summary>
        public void StopAttackButKeepTarget()
        {
            // Only the local player should send combat RPCs.
            if (TryGetComponent(out UltimateDungeon.Combat.PlayerCombatController combat))
            {
                combat.RequestStopAttack(); // server will ignore if nothing is running
            }
        }
    }
}
