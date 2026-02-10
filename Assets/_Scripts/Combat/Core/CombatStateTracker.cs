using System;
using Unity.Netcode;
using UnityEngine;
using UltimateDungeon.Actors;

namespace UltimateDungeon.Combat
{
    /// <summary>
    /// CombatStateTracker (Server-authoritative)
    /// ----------------------------------------
    /// Fix3:
    /// - InCombat is owned by the disengage timer (combatUntilTime), NOT by AttackLoop.IsRunning.
    ///
    /// Why this matters:
    /// - With "chase semantics" AttackLoop may remain running while the target is out of range.
    /// - If we force InCombat whenever AttackLoop is running, combat never ends while a target is kept engaged.
    /// - Your desired rule: combat ends after 10s with no combat-extending events (hit/miss/damage),
    ///   EVEN IF the player is still chasing.
    ///
    /// Result:
    /// - Double click -> refresh timer -> immediately InCombat.
    /// - While chasing out of range -> no resolutions -> timer expires -> combat ends -> loop cancelled.
    /// - While in range and swinging -> CombatResolver refreshes timer -> stays InCombat.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ActorComponent))]
    public sealed class CombatStateTracker : NetworkBehaviour
    {
        [Header("Combat Timing (Server)")]
        [Tooltip("How long after the last combat-extending event the actor remains InCombat.")]
        [SerializeField] private float _disengageSeconds = 10f;

        [Tooltip("Throttle state writes to avoid spamming NetworkVariables.")]
        [SerializeField] private float _minStateWriteIntervalSeconds = 0.25f;

        private ActorComponent _actor;
        private IServerCombatCancelable _combatCancelable;

        // Server-only state
        private double _serverCombatUntilTime = 0;
        private ulong _serverEngagedTargetNetId = 0;
        private double _serverLastStateWriteTime = -999;

        public event Action<CombatState, CombatState> ServerCombatStateChanged;
        public event Action ServerCombatEnded;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _actor = GetComponent<ActorComponent>();
            _combatCancelable = GetComponent<IServerCombatCancelable>();

            if (!IsServer)
                return;

            if (_actor != null && !_actor.IsAlive)
                SafeWriteStateServer(CombatState.Dead);
            else
                SafeWriteStateServer(CombatState.Peaceful);

            _serverCombatUntilTime = 0;
            _serverEngagedTargetNetId = 0;
        }

        private void Update()
        {
            if (!IsServer)
                return;

            if (_actor == null)
                return;

            if (!_actor.IsAlive)
            {
                SafeWriteStateServer(CombatState.Dead);
                return;
            }

            double now = GetServerNow();
            bool stillInCombat = now < _serverCombatUntilTime;

            if (stillInCombat)
            {
                SafeWriteStateServer(CombatState.InCombat);
            }
            else
            {
                // Window expired: leave combat.
                if (_actor.State == CombatState.InCombat)
                {
                    // Cancel any outstanding combat scheduling (AttackLoop etc.)
                    // so chasing/auto-attack intent ends with combat.
                    _combatCancelable?.ServerCancelCombat();

                    _serverEngagedTargetNetId = 0;
                    ServerCombatEnded?.Invoke();
                }

                SafeWriteStateServer(CombatState.Peaceful);
            }
        }

        // --------------------------------------------------------------------
        // Server API
        // --------------------------------------------------------------------

        /// <summary>
        /// Refresh the disengage window.
        /// Call ONLY from combat-extending events:
        /// - Validated hostile intent (attacker)
        /// - Hostile resolution (attacker + victim)
        /// </summary>
        public void ServerRefreshCombatWindow(ulong engagedTargetNetId)
        {
            if (!IsServer)
                return;

            if (_actor == null || !_actor.IsAlive)
                return;

            double now = GetServerNow();
            double desiredUntil = now + Math.Max(0.0, _disengageSeconds);
            _serverCombatUntilTime = Math.Max(_serverCombatUntilTime, desiredUntil);

            _serverEngagedTargetNetId = engagedTargetNetId;

            SafeWriteStateServer(CombatState.InCombat);
        }

        public void ServerNotifyStartedHostileAction(ulong targetNetId) => ServerRefreshCombatWindow(targetNetId);
        public void ServerNotifyReceivedHostileAction(ulong attackerNetId) => ServerRefreshCombatWindow(attackerNetId);

        public void ServerForcePeaceful()
        {
            if (!IsServer)
                return;

            _serverEngagedTargetNetId = 0;
            _serverCombatUntilTime = 0;

            _combatCancelable?.ServerCancelCombat();

            if (_actor != null && _actor.IsAlive)
                SafeWriteStateServer(CombatState.Peaceful);
        }

        public void ServerForceClearCombat() => ServerForcePeaceful();
        public ulong ServerGetEngagedTargetNetId() => _serverEngagedTargetNetId;

        private void SafeWriteStateServer(CombatState desired)
        {
            if (!IsServer || _actor == null)
                return;

            CombatState current = _actor.State;
            if (current == desired)
                return;

            double now = GetServerNow();
            if ((now - _serverLastStateWriteTime) < _minStateWriteIntervalSeconds)
                return;

            _serverLastStateWriteTime = now;

            _actor.ServerSetCombatState(desired);
            ServerCombatStateChanged?.Invoke(current, desired);
        }

        private double GetServerNow()
        {
            if (NetworkManager != null && NetworkManager.IsServer)
                return NetworkManager.ServerTime.Time;

            return Time.timeAsDouble;
        }

        /// <summary>
        /// Optional interface for components that schedule combat actions (AttackLoop).
        /// CombatStateTracker will call this when combat ends.
        /// </summary>
        public interface IServerCombatCancelable
        {
            void ServerCancelCombat();
        }
    }
}
