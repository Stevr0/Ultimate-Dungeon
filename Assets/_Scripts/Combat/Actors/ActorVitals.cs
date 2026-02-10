using UnityEngine;
using Unity.Netcode;

// ============================================================================
// ActorVitals.cs (v0.2)
// ----------------------------------------------------------------------------
// SINGLE SOURCE OF TRUTH for vitals for ALL Actors (Players + Monsters + NPCs).
//
// Why this replaces PlayerVitalsNet:
// - Combat core already applies damage through ActorVitals.
// - AI works on monsters using ActorVitals.
// - HUD should display the same replicated values Combat modifies.
//
// Rules:
// - Server writes NetworkVariables.
// - Everyone reads.
// - All mutation methods are server-only.
//
// TODO (later / optional):
// - Expose hooks for death pipeline (corpse, loot, respawn).
// - Hook max values to aggregated stats/items/statuses.
// ============================================================================

namespace UltimateDungeon.Combat
{
    [DisallowMultipleComponent]
    public sealed class ActorVitals : NetworkBehaviour
    {
        [Header("Defaults (Designer Baselines)")]
        [Tooltip("Starting Max HP for this actor (baseline before stats/items/statuses).")]
        [SerializeField] private int defaultMaxHP = 30;

        [Tooltip("Starting Max Stamina for this actor (baseline before stats/items/statuses).")]
        [SerializeField] private int defaultMaxStamina = 30;

        [Tooltip("Starting Max Mana for this actor (baseline before stats/items/statuses).")]
        [SerializeField] private int defaultMaxMana = 30;

        // --------------------------------------------------------------------
        // NetworkVariables
        // --------------------------------------------------------------------

        public readonly NetworkVariable<int> CurrentHPNet = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        public readonly NetworkVariable<int> MaxHPNet = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        public readonly NetworkVariable<int> CurrentStaminaNet = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        public readonly NetworkVariable<int> MaxStaminaNet = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        public readonly NetworkVariable<int> CurrentManaNet = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        public readonly NetworkVariable<int> MaxManaNet = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        // Convenience accessors
        public int CurrentHP => CurrentHPNet.Value;
        public int MaxHP => MaxHPNet.Value;

        public int CurrentStamina => CurrentStaminaNet.Value;
        public int MaxStamina => MaxStaminaNet.Value;

        public int CurrentMana => CurrentManaNet.Value;
        public int MaxMana => MaxManaNet.Value;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // Initialize ONLY on the server.
            // Clients will receive the values via replication.
            if (!IsServer)
                return;

            // Set max baselines first.
            MaxHPNet.Value = Mathf.Max(1, defaultMaxHP);
            MaxStaminaNet.Value = Mathf.Max(0, defaultMaxStamina);
            MaxManaNet.Value = Mathf.Max(0, defaultMaxMana);

            // Start full.
            CurrentHPNet.Value = MaxHPNet.Value;
            CurrentStaminaNet.Value = MaxStaminaNet.Value;
            CurrentManaNet.Value = MaxManaNet.Value;
        }

        // ====================================================================
        // HP
        // ====================================================================

        /// <summary>Server-only damage application.</summary>
        public void TakeDamage(int amount)
        {
            if (!IsServer)
                return;

            amount = Mathf.Max(0, amount);
            if (amount == 0)
                return;

            CurrentHPNet.Value = Mathf.Max(0, CurrentHPNet.Value - amount);
        }

        /// <summary>Server-only healing application.</summary>
        public void Heal(int amount)
        {
            if (!IsServer)
                return;

            amount = Mathf.Max(0, amount);
            if (amount == 0)
                return;

            CurrentHPNet.Value = Mathf.Min(MaxHPNet.Value, CurrentHPNet.Value + amount);
        }

        /// <summary>
        /// Server-only: set MaxHP and optionally clamp CurrentHP.
        /// Use this when stats/items/statuses change max HP.
        /// </summary>
        public void SetMaxHpServer(int newMaxHp, bool clampCurrent = true)
        {
            if (!IsServer)
                return;

            MaxHPNet.Value = Mathf.Max(1, newMaxHp);

            if (clampCurrent)
                CurrentHPNet.Value = Mathf.Min(CurrentHPNet.Value, MaxHPNet.Value);
        }

        /// <summary>Server-only: set CurrentHP (clamped to MaxHP).</summary>
        public void SetCurrentHpServer(int newCurrentHp)
        {
            if (!IsServer)
                return;

            CurrentHPNet.Value = Mathf.Clamp(newCurrentHp, 0, MaxHPNet.Value);
        }

        // ====================================================================
        // STAMINA
        // ====================================================================

        /// <summary>
        /// Server-only: try to spend stamina.
        /// Returns true if spend succeeded.
        /// </summary>
        public bool TrySpendStamina(int amount)
        {
            if (!IsServer)
                return false;

            amount = Mathf.Max(0, amount);
            if (amount == 0)
                return true;

            if (CurrentStaminaNet.Value < amount)
                return false;

            CurrentStaminaNet.Value -= amount;
            return true;
        }

        /// <summary>Server-only: restore stamina (clamped to MaxStamina).</summary>
        public void RestoreStamina(int amount)
        {
            if (!IsServer)
                return;

            amount = Mathf.Max(0, amount);
            if (amount == 0)
                return;

            CurrentStaminaNet.Value = Mathf.Min(MaxStaminaNet.Value, CurrentStaminaNet.Value + amount);
        }

        /// <summary>Server-only: set MaxStamina and optionally clamp CurrentStamina.</summary>
        public void SetMaxStaminaServer(int newMaxStamina, bool clampCurrent = true)
        {
            if (!IsServer)
                return;

            MaxStaminaNet.Value = Mathf.Max(0, newMaxStamina);

            if (clampCurrent)
                CurrentStaminaNet.Value = Mathf.Min(CurrentStaminaNet.Value, MaxStaminaNet.Value);
        }

        /// <summary>Server-only: set CurrentStamina (clamped to MaxStamina).</summary>
        public void SetCurrentStaminaServer(int newCurrentStamina)
        {
            if (!IsServer)
                return;

            CurrentStaminaNet.Value = Mathf.Clamp(newCurrentStamina, 0, MaxStaminaNet.Value);
        }

        // ====================================================================
        // MANA
        // ====================================================================

        /// <summary>
        /// Server-only: try to spend mana.
        /// Returns true if spend succeeded.
        /// </summary>
        public bool TrySpendMana(int amount)
        {
            if (!IsServer)
                return false;

            amount = Mathf.Max(0, amount);
            if (amount == 0)
                return true;

            if (CurrentManaNet.Value < amount)
                return false;

            CurrentManaNet.Value -= amount;
            return true;
        }

        /// <summary>Server-only: restore mana (clamped to MaxMana).</summary>
        public void RestoreMana(int amount)
        {
            if (!IsServer)
                return;

            amount = Mathf.Max(0, amount);
            if (amount == 0)
                return;

            CurrentManaNet.Value = Mathf.Min(MaxManaNet.Value, CurrentManaNet.Value + amount);
        }

        /// <summary>Server-only: set MaxMana and optionally clamp CurrentMana.</summary>
        public void SetMaxManaServer(int newMaxMana, bool clampCurrent = true)
        {
            if (!IsServer)
                return;

            MaxManaNet.Value = Mathf.Max(0, newMaxMana);

            if (clampCurrent)
                CurrentManaNet.Value = Mathf.Min(CurrentManaNet.Value, MaxManaNet.Value);
        }

        /// <summary>Server-only: set CurrentMana (clamped to MaxMana).</summary>
        public void SetCurrentManaServer(int newCurrentMana)
        {
            if (!IsServer)
                return;

            CurrentManaNet.Value = Mathf.Clamp(newCurrentMana, 0, MaxManaNet.Value);
        }

        // ====================================================================
        // Utility
        // ====================================================================

        /// <summary>
        /// Server-only: restore all vitals to their max.
        /// Useful for respawn, debug, or "heal to full".
        /// </summary>
        public void RestoreAllToMaxServer()
        {
            if (!IsServer)
                return;

            CurrentHPNet.Value = MaxHPNet.Value;
            CurrentStaminaNet.Value = MaxStaminaNet.Value;
            CurrentManaNet.Value = MaxManaNet.Value;
        }
    }
}
