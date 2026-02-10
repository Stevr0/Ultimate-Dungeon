using UltimateDungeon.Players;
using UltimateDungeon.Targeting;
using UnityEngine;

namespace UltimateDungeon.Combat
{
    /// <summary>
    /// CombatEngageIntent (Local)
    /// -------------------------
    /// FIX: Selection must NOT keep you "engaged".
    ///
    /// What this fixes:
    /// - Player remains InCombat as long as a monster is merely targeted/selected.
    ///
    /// Root cause (typical):
    /// - Some update loop treats "CurrentTarget != null" as "keep attacking".
    /// - Or this intent stays armed even after the player stops attacking, so the
    ///   system keeps (re)starting the AttackLoop.
    ///
    /// Design rule:
    /// - Selection = looking at / highlighting.
    /// - Engagement (IsArmed) = player explicitly initiated attack via double click.
    ///
    /// This script enforces:
    /// - ArmedTarget must MATCH current selection, otherwise Disarm.
    /// - You can keep a selection without being armed.
    /// </summary>
    [DisallowMultipleComponent]
    public class CombatEngageIntent : MonoBehaviour
    {
        [Header("Debug (read-only)")]
        [SerializeField] private bool _debugIsArmed;
        [SerializeField] private string _debugArmedTargetName;

        /// <summary>
        /// True if the player explicitly initiated an attack (double-click behavior).
        /// </summary>
        public bool IsArmed { get; private set; }

        /// <summary>
        /// Local target GameObject the player intends to attack.
        /// </summary>
        public GameObject ArmedTarget { get; private set; }

        /// <summary>
        /// Arm intent to attack the provided target.
        /// Call ONLY from the double-click / cast input path.
        /// </summary>
        public void Arm(GameObject target)
        {
            if (target == null)
            {
                Disarm();
                return;
            }

            IsArmed = true;
            ArmedTarget = target;

            RefreshDebug();
        }

        /// <summary>
        /// Clears the intent to attack.
        /// Call when:
        /// - player explicitly stops attack
        /// - player cancels (ESC)
        /// - target dies/despawns
        /// - selection changes away from the armed target
        /// </summary>
        public void Disarm()
        {
            IsArmed = false;
            ArmedTarget = null;

            RefreshDebug();
        }

        /// <summary>
        /// Validates local intent vs current selection.
        ///
        /// IMPORTANT:
        /// - If the player changes selection, we disarm.
        ///   (Selection != engagement, and engagement is tied to a specific target.)
        /// </summary>
        public void ValidateAgainstSelection(PlayerTargeting playerTargeting)
        {
            if (!IsArmed)
                return;

            if (playerTargeting == null)
            {
                Disarm();
                return;
            }

            // If target destroyed -> disarm
            if (ArmedTarget == null)
            {
                Disarm();
                return;
            }

            // Enforce: armed target must equal current selection.
            // If the player is merely targeting something else, they must double-click again.
            GameObject current = playerTargeting.CurrentTarget;
            if (current != ArmedTarget)
            {
                Disarm();
                return;
            }

            RefreshDebug();
        }

        private void RefreshDebug()
        {
            _debugIsArmed = IsArmed;
            _debugArmedTargetName = ArmedTarget != null ? ArmedTarget.name : "(none)";
        }
    }
}
