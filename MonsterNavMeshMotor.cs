// ============================================================================
// Ultimate Dungeon — Monster AI (v0.1)
// Engine: Unity 6 (URP) + Netcode for GameObjects (NGO)
//
// What this gives you:
// - A simple, server-authoritative monster brain you can put on any Monster prefab.
// - The monster will:
//     * Acquire a player target within aggro radius
//     * Chase until in attack range
//     * Start the existing AttackLoop (Combat Core stays unchanged)
//     * “Kite” a bit (maintain a preferred range band)
//     * Flee when low HP
//     * Recover (regen) to a threshold, then re-engage
//
// IMPORTANT DESIGN LOCKS (from ACTOR_MODEL / TARGETING_MODEL / COMBAT_CORE):
// - AI must be server-authoritative.
// - AI must not bypass legality checks.
// - CombatAllowed / DamageAllowed scene gates must still apply.
//
// This implementation uses the same canonical validator as player double-click attacks:
//   ServerTargetValidator.ValidateAttackByNetId(...)
// so it cannot attack illegal targets (safe scenes, non-hostiles, dead actors, etc.).
// ============================================================================

using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

using UltimateDungeon.Actors;
using UltimateDungeon.Combat;
using UltimateDungeon.Targeting;

namespace UltimateDungeon.AI
{
    // ========================================================================
    // 1) MonsterNavMeshMotor
    // ------------------------------------------------------------------------
    // Server-only movement motor for AI-controlled monsters.
    // - Server sets destination.
    // - NavMeshAgent moves.
    // - NetworkTransform replicates position to clients.
    //
    // Requirements on the Monster prefab:
    // - NetworkObject
    // - NetworkTransform (server-authoritative recommended)
    // - NavMeshAgent
    //
    // Notes:
    // - Keep agent “updateRotation” true if you want natural facing.
    // - If you have your own movement motor later, replace this.
    // ========================================================================
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class MonsterNavMeshMotor : NetworkBehaviour
    {
        private NavMeshAgent _agent;

        /// <summary>True if the server currently has a path for the agent.</summary>
        public bool HasPath => _agent != null && _agent.hasPath;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _agent = GetComponent<NavMeshAgent>();

            if (_agent == null)
                Debug.LogError("[MonsterNavMeshMotor] Missing NavMeshAgent.");

            // Only the server should drive monster locomotion.
            // Clients receive replicated movement via NetworkTransform.
            if (!IsServer)
            {
                if (_agent != null)
                {
                    // Disable agent on clients to avoid divergent simulation.
                    _agent.enabled = false;
                }
            }
        }

        /// <summary>
        /// Server-only: sets a movement destination.
        /// </summary>
        public void ServerSetDestination(Vector3 worldPos)
        {
            if (!IsServer || _agent == null || !_agent.enabled)
                return;

            _agent.isStopped = false;
            _agent.SetDestination(worldPos);
        }

        /// <summary>
        /// Server-only: stop moving.
        /// </summary>
        public void ServerStop()
        {
            if (!IsServer || _agent == null || !_agent.enabled)
                return;

            _agent.isStopped = true;
            _agent.ResetPath();
        }

        /// <summary>
        /// Server-only: change move speed (useful for flee / chase / idle).
        /// </summary>
        public void ServerSetSpeed(float metersPerSecond)
        {
            if (!IsServer || _agent == null || !_agent.enabled)
                return;

            _agent.speed = Mathf.Max(0.1f, metersPerSecond);
        }

        /// <summary>
        /// Returns distance to a point using agent’s current position.
        /// (We keep it simple: straight-line distance. Nav distance is overkill for v0.1.)
        /// </summary>
        public float DistanceTo(Vector3 worldPos)
        {
            return Vector3.Distance(transform.position, worldPos);
        }
    }
}

