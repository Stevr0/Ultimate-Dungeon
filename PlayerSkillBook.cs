// ============================================================================
// PlayerSkillBook.cs
// ----------------------------------------------------------------------------
// Server-authoritative runtime storage for a player's skills.
//
// Responsibilities:
// - Holds current skill values for ALL SkillIds
// - Holds lock state (+ / - / lock) for ALL SkillIds
// - Provides safe getters/setters
// - Initializes from PlayerDefinition (defaults + overrides)
//
// Networking:
// - For NGO, you can replicate values using NetworkVariables or custom sync.
// - For v1, keep it simple: server owns, clients read via replication.
//
// This file aligns with:
// - SKILLS.md
// - PLAYER_DEFINITION.md
// - PlayerDefinition.cs
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using UltimateDungeon.Skills;
using UltimateDungeon.Players;

namespace UltimateDungeon.Players
{
    /// <summary>
    /// Runtime skill state for a single skill.
    /// Stored for every SkillId.
    /// </summary>
    [Serializable]
    public struct SkillState
    {
        public SkillId skillId;

        [Tooltip("Current skill value. Supports UO-like 0.1 increments.")]
        public float value;

        [Tooltip("Increase (+), Decrease (-), or Locked (🔒).")]
        public SkillLockState lockState;
    }

    /// <summary>
    /// PlayerSkillBook
    /// --------------
    /// Holds all skills for a player.
    ///
    /// IMPORTANT:
    /// - This is runtime state.
    /// - The server should be the only writer.
    /// - Clients should treat it as read-only.
    /// </summary>
    public sealed class PlayerSkillBook : MonoBehaviour
    {
        // In v1 we store skills in a dictionary for fast lookup.
        // SkillId is an enum so we can safely use it as the key.
        private readonly Dictionary<SkillId, SkillState> _skills = new Dictionary<SkillId, SkillState>();

        // Cache of enum values to avoid GC allocations when iterating.
        private static SkillId[] _allSkillIdsCache;

        /// <summary>
        /// Initialize the skill book for a newly spawned player.
        /// This must run on the SERVER.
        /// </summary>
        public void InitializeFromDefinition(PlayerDefinition def)
        {
            if (def == null)
            {
                Debug.LogError("[PlayerSkillBook] InitializeFromDefinition failed: PlayerDefinition is null.");
                return;
            }

            // Ensure our cache exists.
            CacheAllSkillIdsIfNeeded();

            _skills.Clear();

            // Create a SkillState for EVERY SkillId.
            // Missing skills in def.startingSkills are handled by def.GetStartingSkill().
            for (int i = 0; i < _allSkillIdsCache.Length; i++)
            {
                SkillId id = _allSkillIdsCache[i];

                StartingSkill start = def.GetStartingSkill(id);

                SkillState state = new SkillState
                {
                    skillId = id,
                    value = Mathf.Max(0f, start.startValue),
                    lockState = start.lockState
                };

                _skills[id] = state;
            }

            Debug.Log($"[PlayerSkillBook] Initialized {_skills.Count} skills from PlayerDefinition '{def.definitionId}'.");
        }

        /// <summary>
        /// Get current value of a skill.
        /// Returns 0 if the skill is missing (should not happen).
        /// </summary>
        public float GetValue(SkillId id)
        {
            if (_skills.TryGetValue(id, out SkillState state))
                return state.value;

            // Missing should not happen if initialized correctly.
            Debug.LogWarning($"[PlayerSkillBook] Missing SkillId '{id}' in skill book.");
            return 0f;
        }

        /// <summary>
        /// Get current lock state of a skill.
        /// Returns Locked if missing.
        /// </summary>
        public SkillLockState GetLockState(SkillId id)
        {
            if (_skills.TryGetValue(id, out SkillState state))
                return state.lockState;

            Debug.LogWarning($"[PlayerSkillBook] Missing SkillId '{id}' in skill book.");
            return SkillLockState.Locked;
        }

        /// <summary>
        /// Server-side: set lock state (+ / - / lock).
        /// </summary>
        public void SetLockState(SkillId id, SkillLockState newState)
        {
            if (!_skills.TryGetValue(id, out SkillState state))
            {
                Debug.LogWarning($"[PlayerSkillBook] Cannot SetLockState; missing SkillId '{id}'.");
                return;
            }

            state.lockState = newState;
            _skills[id] = state;
        }

        /// <summary>
        /// Server-side: set skill value.
        /// This does NOT enforce cap rules; SkillGainSystem does.
        /// </summary>
        public void SetValue(SkillId id, float newValue)
        {
            if (!_skills.TryGetValue(id, out SkillState state))
            {
                Debug.LogWarning($"[PlayerSkillBook] Cannot SetValue; missing SkillId '{id}'.");
                return;
            }

            state.value = Mathf.Max(0f, newValue);
            _skills[id] = state;
        }

        /// <summary>
        /// Server-side: add delta to a skill.
        /// This does NOT enforce cap rules; SkillGainSystem does.
        /// </summary>
        public void AddValue(SkillId id, float delta)
        {
            if (!_skills.TryGetValue(id, out SkillState state))
            {
                Debug.LogWarning($"[PlayerSkillBook] Cannot AddValue; missing SkillId '{id}'.");
                return;
            }

            state.value = Mathf.Max(0f, state.value + delta);
            _skills[id] = state;
        }

        /// <summary>
        /// Returns the total of all skill values.
        /// This is used to enforce the 700 total skill cap.
        /// </summary>
        public float GetTotalSkills()
        {
            float total = 0f;

            foreach (var kvp in _skills)
                total += kvp.Value.value;

            return total;
        }

        /// <summary>
        /// Returns true if ANY skill is set to Decrease (-).
        /// Used by SkillGainSystem for manual cap behavior.
        /// </summary>
        public bool HasAnyDecreaseSkills()
        {
            foreach (var kvp in _skills)
            {
                if (kvp.Value.lockState == SkillLockState.Decrease)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Finds the best skill to decrease (deterministic):
        /// - highest value among Decrease skills
        /// - tie-breaker: lowest SkillId
        ///
        /// Returns true if found.
        /// </summary>
        public bool TryGetDecreaseCandidate(out SkillId candidate)
        {
            candidate = default;
            bool found = false;

            float bestValue = float.MinValue;
            int bestIdInt = int.MaxValue;

            foreach (var kvp in _skills)
            {
                SkillState s = kvp.Value;

                if (s.lockState != SkillLockState.Decrease)
                    continue;

                int idInt = (int)s.skillId;

                // Primary: highest value
                if (s.value > bestValue)
                {
                    bestValue = s.value;
                    bestIdInt = idInt;
                    candidate = s.skillId;
                    found = true;
                    continue;
                }

                // Secondary: if tied value, choose lowest SkillId
                if (Mathf.Approximately(s.value, bestValue) && idInt < bestIdInt)
                {
                    bestIdInt = idInt;
                    candidate = s.skillId;
                    found = true;
                }
            }

            return found;
        }

        private static void CacheAllSkillIdsIfNeeded()
        {
            if (_allSkillIdsCache != null && _allSkillIdsCache.Length > 0)
                return;

            Array values = Enum.GetValues(typeof(SkillId));
            _allSkillIdsCache = new SkillId[values.Length];

            for (int i = 0; i < values.Length; i++)
                _allSkillIdsCache[i] = (SkillId)values.GetValue(i);
        }
    }
}

