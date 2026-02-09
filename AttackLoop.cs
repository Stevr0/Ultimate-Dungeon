using System;
using System.Collections;
using UnityEngine;
using Unity.Netcode;

namespace UltimateDungeon.Combat
{
    /// <summary>
    /// AttackLoop (Server)
    /// ------------------
    /// CHANGE: "Chase" semantics.
    /// - Being out of range does NOT end the engagement.
    /// - The loop stays alive and simply waits until the target comes back in range.
    ///
    /// This aligns with the clarified design:
    /// - Target intent is not range-gated.
    /// - Range gates whether a swing can resolve, not whether combat is ongoing.
    ///
    /// NOTE:
    /// - This script does NOT move the attacker.
    ///   Movement/chasing is owned by click-to-move / steering.
    /// - This script only decides whether to attempt swings.
    /// </summary>
    [DisallowMultipleComponent]
    public class AttackLoop : NetworkBehaviour, CombatStateTracker.IServerCombatCancelable
    {
        private ICombatActor _attacker;
        private ICombatActor _target;

        [SerializeField] private float _maxRangeMeters = 2.25f;

        private Coroutine _loopRoutine;

        public event Action OnLoopStoppedServer;

        public bool IsRunning => _loopRoutine != null;
        public ulong CurrentTargetNetId => _target != null ? _target.NetId : 0UL;

        public bool IsAttackingTarget(ulong targetNetId)
        {
            return IsRunning && CurrentTargetNetId == targetNetId;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _attacker = GetComponent<ICombatActor>();
            if (_attacker == null)
                Debug.LogError($"[AttackLoop] No ICombatActor found on '{name}'. Add CombatActorFacade.");
        }

        public override void OnNetworkDespawn()
        {
            StopLoopServer();
            base.OnNetworkDespawn();
        }

        // -------------------------
        // Server API
        // -------------------------

        public void StartLoopServer(ICombatActor target)
        {
            if (!IsServer)
                return;

            if (_attacker == null)
            {
                Debug.LogError("[AttackLoop] Cannot start loop: _attacker is null.");
                return;
            }

            if (target == null)
            {
                Debug.LogWarning("[AttackLoop] Cannot start loop: target is null.");
                return;
            }

            if (target.NetObject == null || !target.NetObject.IsSpawned)
            {
                Debug.LogWarning("[AttackLoop] Cannot start loop: target NetObject missing or not spawned.");
                return;
            }

            if (!target.IsAlive)
            {
                Debug.Log("[AttackLoop] Cannot start loop: target already dead.");
                return;
            }

            _target = target;

            if (_loopRoutine == null)
            {
                _loopRoutine = StartCoroutine(LoopCoroutine());
                Debug.Log($"[AttackLoop] Started engagement: attacker={_attacker.NetId} target={_target.NetId}");
            }
            else
            {
                Debug.Log($"[AttackLoop] Updated engagement target: attacker={_attacker.NetId} target={_target.NetId}");
            }
        }

        public void StopLoopServer()
        {
            if (!IsServer)
                return;

            if (_loopRoutine != null)
            {
                StopCoroutine(_loopRoutine);
                _loopRoutine = null;
            }

            _target = null;
            OnLoopStoppedServer?.Invoke();
        }

        public void ServerCancelCombat()
        {
            StopLoopServer();
        }

        // -------------------------
        // Loop
        // -------------------------

        private IEnumerator LoopCoroutine()
        {
            while (true)
            {
                // Attacker invalid/dead ends engagement.
                if (_attacker == null || !_attacker.IsAlive)
                {
                    Debug.Log("[AttackLoop] Engagement ended: attacker invalid/dead.");
                    break;
                }

                // Target invalid/dead ends engagement.
                if (_target == null || !_target.IsAlive)
                {
                    Debug.Log("[AttackLoop] Engagement ended: target invalid/dead.");
                    break;
                }

                // Status gate: if cannot attack, wait and re-check.
                if (!_attacker.CanAttack)
                {
                    yield return new WaitForSeconds(0.25f);
                    continue;
                }

                // Range gate:
                // - Out of range does NOT end the engagement.
                // - We simply wait briefly and keep checking.
                float dist = Vector3.Distance(_attacker.Transform.position, _target.Transform.position);
                if (dist > _maxRangeMeters)
                {
                    // We intentionally do not spam logs here; it can get noisy.
                    yield return new WaitForSeconds(0.15f);
                    continue;
                }

                // Determine swing time.
                float swingTime = Mathf.Max(0.1f, _attacker.GetBaseSwingTimeSeconds());

                // Wait for swing completion.
                yield return new WaitForSeconds(swingTime);

                // Re-validate after waiting.
                if (_attacker == null || _target == null)
                    break;

                if (!_attacker.IsAlive || !_target.IsAlive)
                    break;

                // Re-check range right before resolving.
                dist = Vector3.Distance(_attacker.Transform.position, _target.Transform.position);
                if (dist > _maxRangeMeters)
                {
                    // Missed the window; keep engagement alive.
                    yield return new WaitForSeconds(0.05f);
                    continue;
                }

                int staminaCost = Mathf.Max(0, _attacker.GetStaminaCostPerSwing());
                if (staminaCost > 0 && !_attacker.TrySpendStamina(staminaCost))
                {
                    // Not enough stamina: deny this swing, briefly wait, then re-check.
                    yield return new WaitForSeconds(0.25f);
                    continue;
                }

                // Resolve swing (server-only).
                CombatResolver.ResolveSwing(_attacker, _target);
            }

            _loopRoutine = null;
            _target = null;

            OnLoopStoppedServer?.Invoke();
        }
    }
}
