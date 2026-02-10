// MinimalDebugHud.cs
// Ultimate Dungeon
//
// Purpose:
// - Option B: a tiny, always-on debug HUD that shows the local player's
//   targeting + combat state (and last deny reason).
//
// Design goals:
// - ZERO inspector wiring to runtime-spawned player objects.
//   (Players spawn via NGO, so we auto-bind to the local player on spawn.)
// - Inspector wiring ONLY for UI Text references (TMP).
// - Safe in Host/Client. Does nothing on dedicated server.
//
// What it displays:
// - Local player NetId / OwnerClientId
// - Current target name + net id
// - Disposition (Self/Friendly/Neutral/Hostile/Invalid) using TargetingResolver
// - AttackLoop running + engaged target net id (from PlayerCombatController events)
// - Last deny reason (from PlayerCombatController deny callback)
//
// Requirements in scene:
// - A Canvas + TextMeshProUGUI fields assigned in the inspector.
//
// Optional (recommended) hookup:
// - A PlayerNetIdentity event that fires when the local player spawns.
//   If you don't have that yet, this script also has a fallback
//   "lazy find" loop that tries to locate the local player for a few seconds.

using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UltimateDungeon.Actors;
using UltimateDungeon.Targeting;

namespace UltimateDungeon.DebugUI
{
    [DisallowMultipleComponent]
    public class MinimalDebugHud : MonoBehaviour
    {
        // ----------------------------
        // Inspector (UI references)
        // ----------------------------
        [Header("Text References (TMP)")]
        [SerializeField] private TextMeshProUGUI _linePlayer;
        [SerializeField] private TextMeshProUGUI _lineTarget;
        [SerializeField] private TextMeshProUGUI _lineDisposition;
        [SerializeField] private TextMeshProUGUI _lineCombat;
        [SerializeField] private TextMeshProUGUI _lineDeny;

        [Header("Options")]
        [Tooltip("How often to refresh the HUD (seconds). 0.1 is plenty.")]
        [SerializeField] private float _refreshInterval = 0.10f;

        [Tooltip("If true, will attempt to resolve disposition via TargetingResolver using current scene flags.")]
        [SerializeField] private bool _computeDisposition = true;

        // ----------------------------
        // Runtime bindings (local player)
        // ----------------------------
        private NetworkObject _localPlayerNetObj;
        private ActorComponent _localActor;
        private UltimateDungeon.Players.PlayerTargeting _playerTargeting;
        private UltimateDungeon.Combat.PlayerCombatController _playerCombat;
        private UltimateDungeon.Combat.AttackLoop _attackLoop;

        // Cached info from combat events
        private ulong _lastConfirmedTargetNetId;
        private TargetingDenyReason _lastDenyReason = TargetingDenyReason.None;

        // Refresh timer
        private float _nextRefreshTime;

        // Fallback bind attempts (in case you don't have a spawn event wired yet)
        private float _fallbackBindDeadline;

        // ----------------------------
        // Unity
        // ----------------------------

        private void Awake()
        {
            // In case you forget to assign TMP refs, don't explode.
            ValidateUiRefs();

            // We'll try to auto-bind for a few seconds after start.
            _fallbackBindDeadline = Time.time + 8f;

            WriteLine(_linePlayer, "Player: (waiting for local player...)");
            WriteLine(_lineTarget, "Target: (none)");
            WriteLine(_lineDisposition, "Disposition: (n/a)");
            WriteLine(_lineCombat, "Combat: (n/a)");
            WriteLine(_lineDeny, "Last deny: None");
        }

        private void OnEnable()
        {
            // If you implement a local-player-spawn event later, hook it here.
            //
            // Example (recommended):
            // PlayerNetIdentity.LocalPlayerSpawned += HandleLocalPlayerSpawned;
            //
            // For now, we rely on fallback binding.
        }

        private void OnDisable()
        {
            UnbindCombatEvents();

            // If you implement the event later:
            // PlayerNetIdentity.LocalPlayerSpawned -= HandleLocalPlayerSpawned;
        }

        private void Update()
        {
            // Don't do any work on a dedicated server.
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsClient)
                return;

            // Fallback: try to bind if we haven't yet.
            if (_localPlayerNetObj == null && Time.time <= _fallbackBindDeadline)
                TryFallbackBindToLocalPlayer();

            // Periodic refresh (avoid spamming string allocs every frame).
            if (Time.time < _nextRefreshTime)
                return;

            _nextRefreshTime = Time.time + Mathf.Max(0.02f, _refreshInterval);

            RefreshHud();
        }

        // ----------------------------
        // Binding
        // ----------------------------

        /// <summary>
        /// If you add a local-player-spawn event, call this from the event handler.
        /// </summary>
        public void BindToLocalPlayer(NetworkObject localPlayer)
        {
            if (localPlayer == null)
                return;

            // Clear any previous bindings.
            UnbindCombatEvents();

            _localPlayerNetObj = localPlayer;

            // ActorComponent is the universal identity surface.
            _localActor = localPlayer.GetComponent<ActorComponent>();

            // Targeting + combat components live on the player prefab (per your inspector screenshot).
            _playerTargeting = localPlayer.GetComponent<UltimateDungeon.Players.PlayerTargeting>();
            _playerCombat = localPlayer.GetComponent<UltimateDungeon.Combat.PlayerCombatController>();
            _attackLoop = localPlayer.GetComponent<UltimateDungeon.Combat.AttackLoop>();

            BindCombatEvents();

            // Immediate refresh so you see it snap in.
            RefreshHud();
        }

        private void TryFallbackBindToLocalPlayer()
        {
            // If NGO isn't running yet, nothing to do.
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient)
                return;

            // Try to find any spawned NetworkObject that is owned by this client.
            // This is a generic "good enough" fallback.
            ulong localClientId = NetworkManager.Singleton.LocalClientId;

            foreach (var kvp in NetworkManager.Singleton.SpawnManager.SpawnedObjects)
            {
                NetworkObject no = kvp.Value;
                if (no == null)
                    continue;

                if (no.OwnerClientId != localClientId)
                    continue;

                // Must have PlayerCombatController or PlayerTargeting to be "our player".
                if (no.GetComponent<UltimateDungeon.Combat.PlayerCombatController>() == null &&
                    no.GetComponent<UltimateDungeon.Players.PlayerTargeting>() == null)
                    continue;

                BindToLocalPlayer(no);
                return;
            }
        }

        private void BindCombatEvents()
        {
            if (_playerCombat == null)
                return;

            // These events are owner-only in your controller, so it's safe.
            _playerCombat.OnAttackConfirmedTargetNetId += HandleAttackConfirmed;
            _playerCombat.OnAttackStopped += HandleAttackStopped;

            // OPTIONAL (if you have a deny event in your controller)
            // If you don't have it, we still show the last deny reason as "None".
            //
            // To support this cleanly, add to PlayerCombatController:
            //   public event Action<TargetingDenyReason> OnAttackDenied;
            // and invoke it when you send the deny client rpc.
            //
            // Then uncomment the lines below:
            //
            // _playerCombat.OnAttackDenied += HandleAttackDenied;
        }

        private void UnbindCombatEvents()
        {
            if (_playerCombat != null)
            {
                _playerCombat.OnAttackConfirmedTargetNetId -= HandleAttackConfirmed;
                _playerCombat.OnAttackStopped -= HandleAttackStopped;

                // _playerCombat.OnAttackDenied -= HandleAttackDenied;
            }
        }

        // ----------------------------
        // Combat event handlers
        // ----------------------------

        private void HandleAttackConfirmed(ulong targetNetId)
        {
            _lastConfirmedTargetNetId = targetNetId;
        }

        private void HandleAttackStopped()
        {
            _lastConfirmedTargetNetId = 0;
        }

        // If you wire up an OnAttackDenied event later:
        // private void HandleAttackDenied(TargetingDenyReason reason)
        // {
        //     _lastDenyReason = reason;
        // }

        // ----------------------------
        // HUD rendering
        // ----------------------------

        private void RefreshHud()
        {
            // PLAYER LINE
            if (_localPlayerNetObj == null)
            {
                WriteLine(_linePlayer, "Player: (waiting for local player...)");
            }
            else
            {
                WriteLine(_linePlayer,
                    $"Player: NetId={_localPlayerNetObj.NetworkObjectId} OwnerClientId={_localPlayerNetObj.OwnerClientId}");
            }

            // TARGET LINE
            GameObject targetGo = _playerTargeting != null ? _playerTargeting.CurrentTarget : null;

            if (targetGo == null)
            {
                WriteLine(_lineTarget, "Target: (none)");
            }
            else
            {
                NetworkObject targetNo = targetGo.GetComponent<NetworkObject>();
                ulong targetId = targetNo != null ? targetNo.NetworkObjectId : 0;
                WriteLine(_lineTarget, $"Target: {targetGo.name} NetId={targetId}");
            }

            // DISPOSITION LINE
            if (!_computeDisposition || _localActor == null || targetGo == null)
            {
                WriteLine(_lineDisposition, "Disposition: (n/a)");
            }
            else
            {
                ActorComponent targetActor = targetGo.GetComponent<ActorComponent>();

                if (targetActor == null)
                {
                    WriteLine(_lineDisposition, "Disposition: (target has no ActorComponent)");
                }
                else
                {
                    // v1 visibility/range inputs: true/no range gate.
                    var result = TargetingResolver.ResolveDispositionUsingCurrentScene(
                        viewer: _localActor,
                        target: targetActor,
                        viewerCanSeeTarget: true,
                        requireRangeGate: false,
                        isInRange: true);

                    if (!result.IsEligible)
                        WriteLine(_lineDisposition, $"Disposition: Invalid ({result.DenyReason})");
                    else
                        WriteLine(_lineDisposition, $"Disposition: {result.Disposition}");
                }
            }

            // COMBAT LINE
            if (_attackLoop == null)
            {
                WriteLine(_lineCombat, "Combat: (no AttackLoop on player)");
            }
            else
            {
                string running = _attackLoop.IsRunning ? "RUNNING" : "stopped";
                string engaged = _lastConfirmedTargetNetId != 0 ? $"engagedTargetNetId={_lastConfirmedTargetNetId}" : "engagedTargetNetId=(none)";
                WriteLine(_lineCombat, $"Combat: {running} | {engaged}");
            }

            // DENY LINE
            WriteLine(_lineDeny, $"Last deny: {_lastDenyReason}");
        }

        // ----------------------------
        // Helpers
        // ----------------------------

        private void ValidateUiRefs()
        {
            // No hard errors; HUD can still run partially.
            if (_linePlayer == null) Debug.LogWarning("[MinimalDebugHud] Missing _linePlayer TMP reference.");
            if (_lineTarget == null) Debug.LogWarning("[MinimalDebugHud] Missing _lineTarget TMP reference.");
            if (_lineDisposition == null) Debug.LogWarning("[MinimalDebugHud] Missing _lineDisposition TMP reference.");
            if (_lineCombat == null) Debug.LogWarning("[MinimalDebugHud] Missing _lineCombat TMP reference.");
            if (_lineDeny == null) Debug.LogWarning("[MinimalDebugHud] Missing _lineDeny TMP reference.");
        }

        private static void WriteLine(TextMeshProUGUI tmp, string text)
        {
            if (tmp != null)
                tmp.text = text;
        }
    }
}
