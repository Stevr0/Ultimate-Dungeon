// ============================================================================
// AttackLoop.cs — Ultimate Dungeon
// ----------------------------------------------------------------------------
// CHANGE (2026-02-12): Melee swing animation trigger.
//
// Goal:
// - When the server schedules a melee swing, also trigger a client-visible
//   animation on the attacker.
//
// Why this is safe:
// - Combat remains server-authoritative.
// - Animation is VISUAL ONLY (does not change combat outcome).
// - We use NetworkAnimator so the SERVER triggers once and all clients see it.
//
// Setup checklist (Unity):
// 1) On the Player prefab, ensure there is a NetworkAnimator component.
// 2) In the Animator Controller:
//    - Add a Trigger parameter: "meleeSwing" (or rename in inspector).
//    - Add an Attack state/clip and transition into it on the trigger.
//
// Notes:
// - This is intentionally generic: later you can swap the trigger name based
//   on weapon type (sword vs mace vs unarmed), or drive it from ItemDef.
// ============================================================================

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

        [Header("Melee")]
        [SerializeField] private float _maxRangeMeters = 2.25f;

        [Header("Animation (Visual Only)")]
        [Tooltip("If true, the server will trigger a melee swing animation when a swing is scheduled.")]
        [SerializeField] private bool triggerMeleeSwingAnimation = true;

        [Tooltip("Animator Trigger parameter name used for melee swings.")]
        [SerializeField] private string meleeSwingTrigger = "meleeSwing";

        [Tooltip("If true, the attacker will rotate to face the target when scheduling a swing.")]
        [SerializeField] private bool faceTargetOnSwing = true;

        [Tooltip("Rotation speed (degrees/sec) when facing the target.")]
        [SerializeField] private float faceTargetTurnSpeed = 720f;

        // NetworkAnimator lets the server set animator triggers and have them replicate.
        // This avoids writing our own ClientRPC just for animation.
        private NetworkAnimator _networkAnimator;

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

            // Optional: only required if we want swing animation replication.
            _networkAnimator = GetComponent<NetworkAnimator>();

            if (triggerMeleeSwingAnimation && _networkAnimator == null)
            {
                Debug.LogWarning(
                    $"[AttackLoop] triggerMeleeSwingAnimation is enabled but no NetworkAnimator exists on '{name}'. " +
                    "Add NetworkAnimator to replicate swing triggers to clients.");
            }
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

                // ----------------------------------------------------------------
                // VISUAL: trigger the melee swing animation when we START the swing.
                // ----------------------------------------------------------------
                // IMPORTANT:
                // - We trigger BEFORE the swingTime wait, so the animation starts
                //   immediately, then the hit/miss resolves after swingTime.
                // - This keeps the visual timing aligned with combat resolution.
                TriggerMeleeSwingAnimationServer();

                // Optional: face the target at swing start.
                // (This is purely cosmetic; do not make gameplay depend on it.)
                if (faceTargetOnSwing)
                    FaceTargetServer(_target.Transform.position);

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

        // --------------------------------------------------------------------
        // Animation helpers (Server)
        // --------------------------------------------------------------------

        private void TriggerMeleeSwingAnimationServer()
        {
            if (!IsServer)
                return;

            if (!triggerMeleeSwingAnimation)
                return;

            // NetworkAnimator is optional, but without it you won't see the trigger
            // replicate to other clients.
            if (_networkAnimator == null)
                return;

            // NetworkAnimator.SetTrigger will replicate the trigger to clients.
            // Make sure your Animator Controller has a Trigger parameter that
            // matches meleeSwingTrigger.
            _networkAnimator.SetTrigger(meleeSwingTrigger);
        }

        private void FaceTargetServer(Vector3 worldTargetPos)
        {
            if (_attacker == null)
                return;

            Transform t = _attacker.Transform;
            Vector3 to = worldTargetPos - t.position;
            to.y = 0f;
            if (to.sqrMagnitude < 0.0001f)
                return;

            Quaternion targetRot = Quaternion.LookRotation(to.normalized, Vector3.up);

            // Use RotateTowards for deterministic-ish, stable behavior.
            // (We are using Time.deltaTime on the server here; it is cosmetic only.)
            t.rotation = Quaternion.RotateTowards(t.rotation, targetRot, faceTargetTurnSpeed * Time.deltaTime);
        }
    }
}
