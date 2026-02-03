using System;
using UltimateDungeon.Actors;
using UltimateDungeon.AI;
using UltimateDungeon.Combat;
using UltimateDungeon.Targeting;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;


// ========================================================================
// 2) MonsterAiController
// ------------------------------------------------------------------------
// “Basic monster stuff” state machine:
// - Idle: waits, periodically scans for targets
// - Chase: moves toward target
// - Attack: starts AttackLoop; maintains range band; stops if target invalid
// - Flee: runs away when HP low
// - Recover: regens HP; when healthy enough, re-engages
//
// This is intentionally conservative and deterministic:
// - No random wandering (yet)
// - No complex LOS checks
// - No flanking
//
// You can add personality later by varying the tuning fields per monster prefab.
// ========================================================================
[DisallowMultipleComponent]
[RequireComponent(typeof(ActorComponent))]
[RequireComponent(typeof(ActorVitals))]
[RequireComponent(typeof(AttackLoop))]
[RequireComponent(typeof(MonsterNavMeshMotor))]
public sealed class MonsterAiController : NetworkBehaviour
{
    private enum AiState
    {
        Idle,
        Chase,
        Attack,
        Flee,
        Recover,
    }

    [Header("Sensing")]
    [Tooltip("How far the monster can initially acquire a player target.")]
    [SerializeField] private float aggroRadiusMeters = 10f;

    [Tooltip("If the target gets farther than this, monster gives up and returns to Idle.")]
    [SerializeField] private float leashRadiusMeters = 18f;

    [Tooltip("How often (seconds) the server scans for a new target while Idle.")]
    [SerializeField] private float idleScanIntervalSeconds = 0.50f;

    [Header("Attack / Spacing")]
    [Tooltip("Preferred distance band center while fighting. (For kiting / spacing)")]
    [SerializeField] private float preferredCombatRangeMeters = 2.25f;

    [Tooltip("Allowed +/- band around preferred range before we reposition.")]
    [SerializeField] private float preferredRangeToleranceMeters = 0.75f;

    [Tooltip("Hard minimum distance. If closer than this, monster backs off.")]
    [SerializeField] private float tooCloseMeters = 1.25f;

    [Header("Movement Speeds")]
    [SerializeField] private float idleSpeed = 0f;
    [SerializeField] private float chaseSpeed = 3.5f;
    [SerializeField] private float fightRepositionSpeed = 3.0f;
    [SerializeField] private float fleeSpeed = 5.0f;

    [Header("Flee / Recover")]
    [Tooltip("When CurrentHP / MaxHP <= this, monster enters Flee state.")]
    [Range(0.05f, 0.95f)]
    [SerializeField] private float fleeAtHpPct = 0.25f;

    [Tooltip("When CurrentHP / MaxHP >= this, monster can leave Recover and re-engage.")]
    [Range(0.05f, 0.95f)]
    [SerializeField] private float reengageAtHpPct = 0.65f;

    [Tooltip("How much HP per second the monster regenerates while Recovering.")]
    [SerializeField] private float recoverHpPerSecond = 2.0f;

    [Tooltip("How far (meters) the monster tries to run away from the target when fleeing.")]
    [SerializeField] private float fleeDistanceMeters = 8f;

    [Header("Debug")]
    [SerializeField] private bool debugLogStateChanges = false;

    // ---- Cached components ----
    private ActorComponent _selfActor;
    private ActorVitals _selfVitals;
    private AttackLoop _attackLoop;
    private MonsterNavMeshMotor _motor;

    // ---- State ----
    private AiState _state = AiState.Idle;
    private ulong _currentTargetNetId;
    private ICombatActor _currentTargetCombatActor;

    private float _nextIdleScanTime;

    // We need a stable max HP reference for percent checks.
    // ActorVitals currently stores maxHP privately, so we approximate via “initial HP on spawn”.
    // If you later expose MaxHP, replace this.
    private int _approxMaxHp = 1;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        _selfActor = GetComponent<ActorComponent>();
        _selfVitals = GetComponent<ActorVitals>();
        _attackLoop = GetComponent<AttackLoop>();
        _motor = GetComponent<MonsterNavMeshMotor>();

        if (_selfActor == null) Debug.LogError("[MonsterAiController] Missing ActorComponent.");
        if (_selfVitals == null) Debug.LogError("[MonsterAiController] Missing ActorVitals.");
        if (_attackLoop == null) Debug.LogError("[MonsterAiController] Missing AttackLoop.");
        if (_motor == null) Debug.LogError("[MonsterAiController] Missing MonsterNavMeshMotor.");

        if (!IsServer)
            return;

        // Approximate max HP: at spawn vitals are set to max.
        _approxMaxHp = Mathf.Max(1, _selfVitals.CurrentHP);

        SetStateServer(AiState.Idle);
    }

    private void Update()
    {
        if (!IsServer)
            return;

        if (_selfActor == null || _selfVitals == null || _attackLoop == null || _motor == null)
            return;

        // Dead actors do nothing.
        if (!_selfActor.IsAlive)
        {
            StopCombatAndMovementServer();
            return;
        }

        // Basic HP-based flee / recover transitions.
        // (We keep this check global so any state can react.)
        float hpPct = GetHpPct();
        if (_state != AiState.Flee && _state != AiState.Recover)
        {
            if (hpPct <= fleeAtHpPct && HasValidTarget())
            {
                SetStateServer(AiState.Flee);
            }
        }

        switch (_state)
        {
            case AiState.Idle:
                TickIdleServer();
                break;
            case AiState.Chase:
                TickChaseServer();
                break;
            case AiState.Attack:
                TickAttackServer();
                break;
            case AiState.Flee:
                TickFleeServer();
                break;
            case AiState.Recover:
                TickRecoverServer();
                break;
        }
    }

    // ====================================================================
    // State ticks
    // ====================================================================

    private void TickIdleServer()
    {
        _motor.ServerSetSpeed(idleSpeed);
        _motor.ServerStop();

        if (Time.time < _nextIdleScanTime)
            return;

        _nextIdleScanTime = Time.time + Mathf.Max(0.1f, idleScanIntervalSeconds);

        // Acquire a player.
        var candidate = FindNearestPlayerActorWithin(aggroRadiusMeters);
        if (candidate == null)
            return;

        if (TrySetTargetServer(candidate))
        {
            SetStateServer(AiState.Chase);
        }
    }

    private void TickChaseServer()
    {
        if (!HasValidTarget())
        {
            ClearTargetServer();
            SetStateServer(AiState.Idle);
            return;
        }

        var targetActor = ResolveTargetActorComponent();
        if (targetActor == null || !targetActor.IsAlive)
        {
            ClearTargetServer();
            SetStateServer(AiState.Idle);
            return;
        }

        float dist = _motor.DistanceTo(targetActor.transform.position);

        // Leash: give up if too far.
        if (dist > leashRadiusMeters)
        {
            StopCombatAndMovementServer();
            ClearTargetServer();
            SetStateServer(AiState.Idle);
            return;
        }

        // Move toward the target.
        _motor.ServerSetSpeed(chaseSpeed);
        _motor.ServerSetDestination(targetActor.transform.position);

        // If we are close-ish to our preferred band, start attacking.
        if (dist <= preferredCombatRangeMeters + preferredRangeToleranceMeters)
        {
            // Start attack loop (server authoritative) — same pipeline as player.
            if (!_attackLoop.IsRunning)
            {
                _attackLoop.StartLoopServer(_currentTargetCombatActor);

                // Also refresh combat state windows so both are “in combat” quickly.
                if (TryGetComponent(out CombatStateTracker tracker))
                    tracker.ServerRefreshCombatWindow(_currentTargetNetId);

                if (_currentTargetCombatActor is Component targetComp &&
                    targetComp.TryGetComponent(out CombatStateTracker defenderTracker))
                {
                    defenderTracker.ServerRefreshCombatWindow(_selfActor.NetworkObjectId);
                }
            }

            SetStateServer(AiState.Attack);
        }
    }

    private void TickAttackServer()
    {
        if (!HasValidTarget())
        {
            StopCombatAndMovementServer();
            ClearTargetServer();
            SetStateServer(AiState.Idle);
            return;
        }

        var targetActor = ResolveTargetActorComponent();
        if (targetActor == null || !targetActor.IsAlive)
        {
            StopCombatAndMovementServer();
            ClearTargetServer();
            SetStateServer(AiState.Idle);
            return;
        }

        float dist = _motor.DistanceTo(targetActor.transform.position);

        // Leash: if target drags us too far, give up.
        if (dist > leashRadiusMeters)
        {
            StopCombatAndMovementServer();
            ClearTargetServer();
            SetStateServer(AiState.Idle);
            return;
        }

        // If we’re low, flee (handled globally too, but this keeps it snappy).
        if (GetHpPct() <= fleeAtHpPct)
        {
            SetStateServer(AiState.Flee);
            return;
        }

        // Ensure the attack loop is targeting the correct actor.
        // (If your AttackLoop supports retargeting, you can add that here.)
        if (!_attackLoop.IsRunning)
        {
            _attackLoop.StartLoopServer(_currentTargetCombatActor);
        }

        // “Kite” / spacing:
        // - Too close -> step away
        // - Too far from preferred band -> step toward
        // - In band -> stop moving
        _motor.ServerSetSpeed(fightRepositionSpeed);

        float minBand = preferredCombatRangeMeters - preferredRangeToleranceMeters;
        float maxBand = preferredCombatRangeMeters + preferredRangeToleranceMeters;

        if (dist < tooCloseMeters)
        {
            // Back away from target.
            Vector3 fleeDir = (transform.position - targetActor.transform.position);
            fleeDir.y = 0f;
            if (fleeDir.sqrMagnitude < 0.0001f)
                fleeDir = transform.forward;

            Vector3 desired = transform.position + fleeDir.normalized * 2.0f;
            _motor.ServerSetDestination(SampleNavPosition(desired, 2.5f));
        }
        else if (dist < minBand)
        {
            // Slightly too close -> small backstep
            Vector3 away = (transform.position - targetActor.transform.position);
            away.y = 0f;
            Vector3 desired = transform.position + away.normalized * 1.5f;
            _motor.ServerSetDestination(SampleNavPosition(desired, 2.0f));
        }
        else if (dist > maxBand)
        {
            // Too far -> move closer
            _motor.ServerSetDestination(targetActor.transform.position);
        }
        else
        {
            // In the sweet spot -> hold position
            _motor.ServerStop();
        }
    }

    private void TickFleeServer()
    {
        if (!HasValidTarget())
        {
            StopCombatAndMovementServer();
            SetStateServer(AiState.Recover);
            return;
        }

        var targetActor = ResolveTargetActorComponent();
        if (targetActor == null)
        {
            StopCombatAndMovementServer();
            SetStateServer(AiState.Recover);
            return;
        }

        // Stop attacking while fleeing.
        if (_attackLoop.IsRunning)
            _attackLoop.StopLoopServer();

        _motor.ServerSetSpeed(fleeSpeed);

        Vector3 away = (transform.position - targetActor.transform.position);
        away.y = 0f;
        if (away.sqrMagnitude < 0.0001f)
            away = transform.forward;

        Vector3 desired = transform.position + away.normalized * fleeDistanceMeters;
        _motor.ServerSetDestination(SampleNavPosition(desired, 4.0f));

        // Once we’ve opened distance, start recovering.
        float dist = _motor.DistanceTo(targetActor.transform.position);
        if (dist >= fleeDistanceMeters * 0.8f)
        {
            SetStateServer(AiState.Recover);
        }
    }

    private void TickRecoverServer()
    {
        // Passive regen (server-only).
        if (recoverHpPerSecond > 0f)
        {
            float healThisFrame = recoverHpPerSecond * Time.deltaTime;

            // ActorVitals only accepts ints, so we accumulate fractional heal.
            // (Small helper keeps regen smooth without spamming tiny heals.)
            AccumulateHealServer(healThisFrame);
        }

        // If we have a target and we are healthy enough, re-engage.
        if (HasValidTarget() && GetHpPct() >= reengageAtHpPct)
        {
            SetStateServer(AiState.Chase);
            return;
        }

        // If we don’t have a target, just idle once we’re “okay”.
        if (!HasValidTarget() && GetHpPct() >= reengageAtHpPct)
        {
            SetStateServer(AiState.Idle);
            return;
        }

        // Otherwise: keep holding position.
        _motor.ServerStop();
    }

    // ====================================================================
    // Target acquisition / validation
    // ====================================================================

    private ActorComponent FindNearestPlayerActorWithin(float radius)
    {
        // NOTE: This is a simple Physics scan.
        // If you later have an ActorRegistry, switch to that for performance.
        Collider[] hits = Physics.OverlapSphere(transform.position, radius);

        ActorComponent best = null;
        float bestDist = float.MaxValue;

        foreach (var h in hits)
        {
            if (h == null)
                continue;

            if (!h.TryGetComponent(out ActorComponent actor))
                continue;

            if (actor.Type != ActorType.Player)
                continue;

            if (!actor.IsAlive)
                continue;

            // Quick distance check
            float d = Vector3.Distance(transform.position, actor.transform.position);
            if (d < bestDist)
            {
                // Ensure a NetworkObject exists so we can call the validator.
                if (!actor.TryGetComponent(out NetworkObject netObj))
                    continue;

                bestDist = d;
                best = actor;
            }
        }

        return best;
    }

    private bool TrySetTargetServer(ActorComponent candidate)
    {
        if (!IsServer || candidate == null)
            return false;

        if (!candidate.TryGetComponent(out NetworkObject targetNetObj))
            return false;

        // IMPORTANT:
        // - We do not want range to deny the intent (same as player double click).
        // - We still want all other legality gates (scene, hostility, dead, etc.).
        float intentValidationMaxRange = float.MaxValue;
        float intentValidationBuffer = 0f;

        // v1 stubs; later these come from status aggregation.
        bool attackerCanAttack = true;
        bool attackerCanSeeTarget = true;

        var result = ServerTargetValidator.ValidateAttackByNetId(
            attacker: _selfActor,
            targetNetId: targetNetObj.NetworkObjectId,
            maxRangeMeters: intentValidationMaxRange,
            rangeBufferMeters: intentValidationBuffer,
            attackerCanAttack: attackerCanAttack,
            attackerCanSeeTarget: attackerCanSeeTarget,
            out ICombatActor targetCombatActor);

        if (!result.Allowed || targetCombatActor == null)
            return false;

        _currentTargetNetId = targetNetObj.NetworkObjectId;
        _currentTargetCombatActor = targetCombatActor;

        return true;
    }

    private bool HasValidTarget()
    {
        return _currentTargetNetId != 0UL && _currentTargetCombatActor != null;
    }

    private ActorComponent ResolveTargetActorComponent()
    {
        if (!HasValidTarget())
            return null;

        // Since we only stored NetId, resolve via SpawnManager.
        if (NetworkManager.Singleton == null || NetworkManager.Singleton.SpawnManager == null)
            return null;

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(_currentTargetNetId, out var netObj))
            return null;

        if (netObj == null)
            return null;

        return netObj.GetComponent<ActorComponent>();
    }

    private void ClearTargetServer()
    {
        _currentTargetNetId = 0UL;
        _currentTargetCombatActor = null;
    }

    // ====================================================================
    // Utilities
    // ====================================================================

    private void StopCombatAndMovementServer()
    {
        if (_attackLoop != null && _attackLoop.IsRunning)
            _attackLoop.StopLoopServer();

        if (_motor != null)
            _motor.ServerStop();
    }

    private float GetHpPct()
    {
        if (_approxMaxHp <= 0)
            _approxMaxHp = 1;

        return Mathf.Clamp01((float)_selfVitals.CurrentHP / _approxMaxHp);
    }

    // --- fractional healing accumulator ---
    private float _healAccumulator;

    private void AccumulateHealServer(float healAmount)
    {
        if (!IsServer)
            return;

        _healAccumulator += Mathf.Max(0f, healAmount);

        // Apply in whole HP chunks to avoid calling Heal(0) repeatedly.
        int whole = Mathf.FloorToInt(_healAccumulator);
        if (whole <= 0)
            return;

        _healAccumulator -= whole;
        _selfVitals.Heal(whole);
    }

    private void SetStateServer(AiState newState)
    {
        if (!IsServer)
            return;

        if (_state == newState)
            return;

        if (debugLogStateChanges)
            Debug.Log($"[MonsterAiController] {name} state {_state} -> {newState}");

        _state = newState;

        // Minimal entry actions.
        switch (_state)
        {
            case AiState.Idle:
                StopCombatAndMovementServer();
                break;
            case AiState.Chase:
                _motor.ServerSetSpeed(chaseSpeed);
                break;
            case AiState.Attack:
                _motor.ServerSetSpeed(fightRepositionSpeed);
                break;
            case AiState.Flee:
                _motor.ServerSetSpeed(fleeSpeed);
                break;
            case AiState.Recover:
                StopCombatAndMovementServer();
                break;
        }
    }

    /// <summary>
    /// Returns a nearby valid NavMesh position so we don't request unreachable destinations.
    /// </summary>
    private static Vector3 SampleNavPosition(Vector3 desired, float maxDistance)
    {
        if (NavMesh.SamplePosition(desired, out var hit, Mathf.Max(0.5f, maxDistance), NavMesh.AllAreas))
            return hit.position;

        return desired; // fallback: agent will fail gracefully
    }

    // Optional: visualize aggro/leash in the editor.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRadiusMeters);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, leashRadiusMeters);
    }
}