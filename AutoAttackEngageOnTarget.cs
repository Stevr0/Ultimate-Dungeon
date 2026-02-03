using UnityEngine;
using Unity.Netcode;
using UltimateDungeon.Players;

namespace UltimateDungeon.Combat
{
    /// <summary>
    /// AutoAttackEngageOnTarget
    /// ------------------------
    /// Uses your existing left-click target selection to automatically start/stop attacking.
    ///
    /// FIX:
    /// - This version uses your FactionTag to decide hostility.
    /// - If the target has FactionTag.Value == Hostile, we engage when in range.
    ///
    /// Requirements on the Player:
    /// - PlayerCombatController
    /// - PlayerTargeting (must expose GameObject CurrentTarget)
    /// - (Optional) this script for auto-engage
    ///
    /// Requirements on the Target:
    /// - NetworkObject
    /// - CombatActorFacade (implements ICombatActor)
    /// - ActorVitals (or your vitals adapter)
    /// - FactionTag set to Hostile
    /// </summary>
    [DisallowMultipleComponent]
    public class AutoAttackEngageOnTarget : NetworkBehaviour
    {
        [Header("Engage Rules")]
        [SerializeField] private float meleeEngageRangeMeters = 2.0f;
        [SerializeField] private float disengageBufferMeters = 0.25f;
        [SerializeField] private float pollIntervalSeconds = 0.10f;

        [Header("Debug")]
        [SerializeField] private bool debugLogs = false;

        private PlayerCombatController _combatController;
        private PlayerTargeting _targeting;

        private float _nextPollTime;
        private ulong _lastTargetNetId;
        private bool _attackRequested;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _combatController = GetComponent<PlayerCombatController>();
            _targeting = GetComponent<PlayerTargeting>();

            if (_combatController == null)
                Debug.LogError("[AutoAttackEngageOnTarget] Missing PlayerCombatController.");

            if (_targeting == null)
                Debug.LogError("[AutoAttackEngageOnTarget] Missing PlayerTargeting.");

            _nextPollTime = 0f;
            _lastTargetNetId = 0;
            _attackRequested = false;
        }

        private void Update()
        {
            // Owner-only.
            if (!IsOwner)
                return;

            if (Time.time < _nextPollTime)
                return;

            _nextPollTime = Time.time + pollIntervalSeconds;
            Evaluate();
        }

        private void Evaluate()
        {
            if (_combatController == null || _targeting == null)
                return;

            GameObject targetGo = _targeting.CurrentTarget;

            // No target => stop.
            if (targetGo == null)
            {
                StopIfRunning("No target");
                _lastTargetNetId = 0;
                return;
            }

            // Must be networked.
            var targetNetObj = targetGo.GetComponent<NetworkObject>();
            if (targetNetObj == null)
            {
                StopIfRunning("Target has no NetworkObject");
                _lastTargetNetId = 0;
                return;
            }

            ulong targetId = targetNetObj.NetworkObjectId;
            if (targetId != _lastTargetNetId)
            {
                if (debugLogs) Debug.Log($"[AutoAttackEngageOnTarget] Target changed: {_lastTargetNetId} -> {targetId}");
                _lastTargetNetId = targetId;
                _attackRequested = false;
            }

            // Hostility gate: must be Hostile.
            if (!IsHostileByFactionTag(targetGo))
            {
                StopIfRunning("Target not hostile (FactionTag)");
                return;
            }

            float dist = Vector3.Distance(transform.position, targetGo.transform.position);
            float engageRange = Mathf.Max(0.1f, meleeEngageRangeMeters);
            float disengageRange = engageRange + Mathf.Max(0f, disengageBufferMeters);

            if (dist <= engageRange)
            {
                if (!_attackRequested)
                {
                    if (debugLogs) Debug.Log($"[AutoAttackEngageOnTarget] In range ({dist:0.00}m). RequestStartAttack().");
                    _combatController.RequestStartAttack();
                    _attackRequested = true;
                }
                return;
            }

            if (dist > disengageRange)
            {
                StopIfRunning($"Out of range ({dist:0.00}m)");
                return;
            }
        }

        private void StopIfRunning(string reason)
        {
            if (_attackRequested)
            {
                if (debugLogs) Debug.Log($"[AutoAttackEngageOnTarget] RequestStopAttack() — {reason}");
                _combatController.RequestStopAttack();
                _attackRequested = false;
            }
        }

        private bool IsHostileByFactionTag(GameObject target)
        {
            // Your script is currently local-only, which is fine for client engagement.
            // The SERVER still validates range + combat actor existence.
            // (Later we can make factions authoritative.)
            var tag = target.GetComponent<FactionTag>();
            if (tag == null) return false;
            return tag.Value == FactionTag.Faction.Hostile;
        }
    }


}
