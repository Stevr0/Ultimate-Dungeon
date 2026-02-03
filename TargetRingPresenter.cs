using UnityEngine;
using Unity.Netcode;

namespace UltimateDungeon.UI.Targeting
{
    /// <summary>
    /// TargetRingPresenter
    /// -------------------
    /// LOCAL-ONLY visual feedback for target selection + combat engagement.
    ///
    /// Listens to:
    /// - PlayerTargeting: "what I'm currently targeting" (normal ring color)
    /// - PlayerCombatController: "server confirmed I'm attacking X" (hostile ring color)
    ///
    /// Notes:
    /// - This script does NOT decide gameplay.
    /// - It only changes a material color for the local player's UI ring.
    /// </summary>
    [DisallowMultipleComponent]
    public class TargetRingPresenter : MonoBehaviour
    {
        [Header("Ring Visual")]
        [SerializeField] private Renderer ringRenderer;

        [Tooltip("Material property name used for color (commonly _Color or _BaseColor).")]
        [SerializeField] private string colorProperty = "_Color";

        [Header("Colors")]
        [SerializeField] private Color normalTargetColor = Color.yellow;
        [SerializeField] private Color hostileTargetColor = Color.red;

        // We grab these from the player root (parent), because the ring is usually a child object.
        private UltimateDungeon.Players.PlayerTargeting _targeting;
        private UltimateDungeon.Combat.PlayerCombatController _combat;

        // NetId of the target the SERVER confirmed we are currently attacking.
        private ulong _currentEngagedTargetNetId;

        private void Awake()
        {
            // Find components on the player root. The ring is usually under the player hierarchy.
            _targeting = GetComponentInParent<UltimateDungeon.Players.PlayerTargeting>();
            _combat = GetComponentInParent<UltimateDungeon.Combat.PlayerCombatController>();

            if (ringRenderer == null)
                Debug.LogError("[TargetRingPresenter] Missing ringRenderer reference.");

            if (_targeting == null)
                Debug.LogError("[TargetRingPresenter] Could not find PlayerTargeting in parents.");

            if (_combat == null)
                Debug.LogError("[TargetRingPresenter] Could not find PlayerCombatController in parents.");
        }

        private void OnEnable()
        {
            if (_targeting != null)
            {
                _targeting.OnTargetSet += HandleTargetSet;
                _targeting.OnTargetCleared += HandleTargetCleared;
            }

            if (_combat != null)
            {
                _combat.OnAttackConfirmedTargetNetId += HandleAttackConfirmed;
                _combat.OnAttackStopped += HandleAttackStopped;
            }
        }

        private void OnDisable()
        {
            if (_targeting != null)
            {
                _targeting.OnTargetSet -= HandleTargetSet;
                _targeting.OnTargetCleared -= HandleTargetCleared;
            }

            if (_combat != null)
            {
                _combat.OnAttackConfirmedTargetNetId -= HandleAttackConfirmed;
                _combat.OnAttackStopped -= HandleAttackStopped;
            }
        }

        private void HandleTargetSet(GameObject target)
        {
            // Selecting a target shows "normal" targeting color by default.
            // If this selected target is also the engaged combat target, show hostile red.
            SetRingColor(IsSelectedTargetEngaged() ? hostileTargetColor : normalTargetColor);
        }

        private void HandleTargetCleared()
        {
            // When you clear target, you generally want to reset the ring.
            // (If you prefer hiding the ring, you can disable the renderer instead.)
            _currentEngagedTargetNetId = 0;
            SetRingColor(normalTargetColor);
        }

        private void HandleAttackConfirmed(ulong targetNetId)
        {
            // Server accepted attack start for THIS local player.
            _currentEngagedTargetNetId = targetNetId;

            // If our currently selected target is that same object, show hostile red.
            SetRingColor(IsSelectedTargetEngaged() ? hostileTargetColor : normalTargetColor);
        }

        private void HandleAttackStopped()
        {
            _currentEngagedTargetNetId = 0;
            SetRingColor(normalTargetColor);
        }

        private bool IsSelectedTargetEngaged()
        {
            if (_targeting == null || _targeting.CurrentTarget == null)
                return false;

            var netObj = _targeting.CurrentTarget.GetComponent<NetworkObject>();
            if (netObj == null)
                return false;

            return netObj.NetworkObjectId == _currentEngagedTargetNetId;
        }

        private void SetRingColor(Color c)
        {
            if (ringRenderer == null)
                return;

            // Important: ringRenderer.material creates an instance at runtime.
            // That’s fine for per-player UI, but don’t do this on 1000 objects.
            var mat = ringRenderer.material;
            if (mat == null)
                return;

            if (mat.HasProperty(colorProperty))
                mat.SetColor(colorProperty, c);
        }
    }
}
