using UnityEngine;
using Unity.Netcode;
using System;
using UltimateDungeon.Players;
using UltimateDungeon.Actors;
using UltimateDungeon.SceneRules;
using UltimateDungeon.Targeting;

namespace UltimateDungeon.Combat
{
    /// <summary>
    /// PlayerCombatController (Server-authoritative execution)
    /// -----------------------------------------------------
    /// Fix:
    /// - Attack intent is NOT range-gated.
    /// - Range only gates swing resolution (AttackLoop / CombatResolver).
    ///
    /// Why?
    /// - Matches the desired UO-style feel: you can double-click from far away,
    ///   enter InCombat immediately, and swings begin once in range.
    ///
    /// IMPORTANT:
    /// - This controller should be called ONLY on explicit player action (double click / cast).
    /// - Do NOT call RequestStartAttack every frame just because a target is selected.
    ///   Selection != engagement.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerCombatController : NetworkBehaviour
    {
        // ------------------------------------------------------------
        // Server-side safety constants
        // ------------------------------------------------------------
        // NOTE: Melee range still belongs to weapon data later.
        private const float SERVER_MELEE_RANGE_METERS = 2.0f;
        private const float SERVER_RANGE_BUFFER_METERS = 0.25f;

        private AttackLoop _attackLoop;
        private CombatActorFacade _combatActor;
        private PlayerTargeting _playerTargeting;
        private ActorComponent _selfActor;

        public event Action<ulong> OnAttackConfirmedTargetNetId;
        public event Action<TargetingDenyReason> OnAttackDenied;
        public event Action OnAttackStopped;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _attackLoop = GetComponent<AttackLoop>();
            _combatActor = GetComponent<CombatActorFacade>();
            _playerTargeting = GetComponent<PlayerTargeting>();
            _selfActor = GetComponent<ActorComponent>();

            if (_attackLoop == null)
                Debug.LogError("[PlayerCombatController] Missing AttackLoop component.");
            if (_combatActor == null)
                Debug.LogError("[PlayerCombatController] Missing CombatActorFacade component.");
            if (_playerTargeting == null)
                Debug.LogError("[PlayerCombatController] Missing PlayerTargeting component.");
            if (_selfActor == null)
                Debug.LogError("[PlayerCombatController] Missing ActorComponent (required on ALL actors).");
        }

        // --------------------------------------------------------------------
        // Client-side request API
        // --------------------------------------------------------------------

        /// <summary>
        /// Called by local input/UI on DOUBLE CLICK.
        /// Client asks the server to start attacking the currently selected target.
        /// </summary>
        public void RequestStartAttack()
        {

            if (!IsOwner)
                return;

            if (_playerTargeting == null)
                return;

            var target = _playerTargeting.CurrentTarget;
            if (target == null)
                return;

            NetworkObject targetNetObj = target.GetComponent<NetworkObject>();
            if (targetNetObj == null)
            {
                Debug.Log("[PlayerCombatController] Target has no NetworkObject.");
                return;
            }

            RequestStartAttackServerRpc(targetNetObj.NetworkObjectId);
        }

        public void RequestStopAttack()
        {
            if (!IsOwner)
                return;

            RequestStopAttackServerRpc();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void RequestStartAttackServerRpc(ulong targetNetId, RpcParams rpcParams = default)
        {
            // Defensive sender validation
            if (rpcParams.Receive.SenderClientId != OwnerClientId)
                return;

            if (!IsServer)
                return;

            if (NetworkManager.Singleton == null)
                return;

            if (_attackLoop == null || _selfActor == null)
                return;

            // Ignore invalid requests
            if (targetNetId == 0UL)
                return;

            // Attacker must be alive.
            if (!_selfActor.IsAlive)
                return;

            // Guard: if already engaged with this target (even if currently out of range), do nothing.
            // This prevents spam-clicking from constantly refreshing combat.
            if (_attackLoop.IsAttackingTarget(targetNetId))
                return;

            // v1 stubs
            bool attackerCanAttack = true;
            bool attackerCanSeeTarget = true;

            // ----------------------------------------------------------------
            // Canonical server validation (INTENT VALIDATION)
            // ----------------------------------------------------------------
            // DESIGN LOCK:
            // - Attack intent is NOT range-gated.
            // - Therefore we pass an intentionally huge range to the validator so it does not deny
            //   solely due to distance.
            //
            // Range will be enforced by AttackLoop/CombatResolver during swing resolution.
            float intentValidationMaxRange = float.MaxValue;
            float intentValidationBuffer = 0f;

            var result = ServerTargetValidator.ValidateAttackByNetId(
                attacker: _selfActor,
                targetNetId: targetNetId,
                maxRangeMeters: intentValidationMaxRange,
                rangeBufferMeters: intentValidationBuffer,
                attackerCanAttack: attackerCanAttack,
                attackerCanSeeTarget: attackerCanSeeTarget,
                out ICombatActor targetCombatActor);

            if (!result.Allowed || targetCombatActor == null)
            {
                // Tell owner why it failed
                SendAttackDeniedToOwnerClientRpc(
                    (byte)result.DenyReason,
                    new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams
                        {
                            TargetClientIds = new[] { OwnerClientId }
                        }
                    });

                OnAttackDenied?.Invoke((TargetingDenyReason)(byte)result.DenyReason);
                return;
            }

            // ----------------------------------------------------------------
            // COMMIT: begin engagement (server authoritative)
            // ----------------------------------------------------------------

            // 1) Start/retarget attack loop (engagement)
            _attackLoop.StartLoopServer(targetCombatActor);

            // 2) Immediately enter combat on intent (attacker)
            // This makes "double click from far away" flip to InCombat instantly.
            if (TryGetComponent(out CombatStateTracker tracker))
            {
                tracker.ServerRefreshCombatWindow(targetNetId);
            }
            // Defender becomes aware of hostility immediately
            if (targetCombatActor is Component targetComponent &&
                targetComponent.TryGetComponent(out CombatStateTracker defenderTracker))
            {
                defenderTracker.ServerRefreshCombatWindow(_selfActor.NetworkObjectId);
            }


            // 3) Owner-only UI confirmation
            SendAttackConfirmedToOwnerClientRpc(
                targetNetId,
                new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new[] { OwnerClientId }
                    }
                });
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void RequestStopAttackServerRpc(RpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != OwnerClientId)
                return;

            if (!IsServer)
                return;

            if (NetworkManager.Singleton == null)
                return;

            if (_attackLoop == null)
                return;

            if (!_attackLoop.IsRunning)
                return;

            _attackLoop.StopLoopServer();

            // NOTE:
            // We do NOT force-clear combat here.
            // Disengage rules: combat decays naturally after DISENGAGE_SECONDS.

            SendAttackStoppedToOwnerClientRpc(
                new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new[] { OwnerClientId }
                    }
                });
        }

        // --------------------------------------------------------------------
        // Owner-only UI callbacks
        // --------------------------------------------------------------------

        [ClientRpc]
        private void SendAttackConfirmedToOwnerClientRpc(ulong targetNetId, ClientRpcParams clientRpcParams = default)
        {
            if (!IsOwner)
                return;

            OnAttackConfirmedTargetNetId?.Invoke(targetNetId);
        }

        [ClientRpc]
        private void SendAttackStoppedToOwnerClientRpc(ClientRpcParams clientRpcParams = default)
        {
            if (!IsOwner)
                return;

            OnAttackStopped?.Invoke();
        }

        [ClientRpc]
        private void SendAttackDeniedToOwnerClientRpc(byte denyReason, ClientRpcParams clientRpcParams = default)
        {
            if (!IsOwner)
                return;

            Debug.Log($"[PlayerCombatController] Attack denied. Reason={(TargetingDenyReason)denyReason}");
        }
    }
}
