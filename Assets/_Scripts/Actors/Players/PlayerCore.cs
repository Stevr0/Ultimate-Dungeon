// ============================================================================
// PlayerCore.cs (v0.4)
// ----------------------------------------------------------------------------
// SINGLE VITALS MODEL:
// - ActorVitals is the one-and-only source of truth for HP/Stamina/Mana.
//
// This version aligns exactly with your PlayerDefinition.cs fields:
// - baseSTR/baseDEX/baseINT
// - vitalCap
// - (added) hpRegenPerSec/staminaRegenPerSec/manaRegenPerSec
//
// Store at:
// Assets/_Scripts/Actors/Players/PlayerCore.cs
// ============================================================================

using UnityEngine;
using Unity.Netcode;

namespace UltimateDungeon.Players
{
    [DisallowMultipleComponent]
    public sealed class PlayerCore : MonoBehaviour
    {
        [Header("Definition (Authoritative)")]
        public PlayerDefinition definition;

        [Header("Runtime Components")]
        [SerializeField] private PlayerStats stats;
        [SerializeField] private UltimateDungeon.Combat.ActorVitals vitals;
        [SerializeField] private PlayerSkillBook skillBook;

        [Header("Server Systems")]
        [SerializeField] private PlayerVitalsRegenServer regenServer;

        public PlayerStats Stats => stats;
        public UltimateDungeon.Combat.ActorVitals Vitals => vitals;
        public PlayerSkillBook SkillBook => skillBook;

        private void Reset()
        {
            stats = GetComponentInChildren<PlayerStats>(true);
            vitals = GetComponentInChildren<UltimateDungeon.Combat.ActorVitals>(true);
            skillBook = GetComponentInChildren<PlayerSkillBook>(true);
            regenServer = GetComponentInChildren<PlayerVitalsRegenServer>(true);
        }

        private void Awake()
        {
            ValidateReferences();
        }

        /// <summary>
        /// Server-only initialization. Call this from PlayerNetIdentity on the server.
        /// </summary>
        public void InitializeServer()
        {
            // Hard gate: only the server initializes authoritative state.
            if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
            {
                Debug.LogWarning("[PlayerCore] InitializeServer called on non-server. Ignored.");
                return;
            }

            if (definition == null)
            {
                Debug.LogError("[PlayerCore] InitializeServer failed: PlayerDefinition is null.");
                return;
            }

            ValidateReferences();

            // 1) Skills
            if (skillBook != null)
                skillBook.InitializeFromDefinition(definition);

            // 2) Stats
            if (stats != null)
                stats.InitializeFromDefinition(definition);

            // 3) Vitals derived from attributes (locked rule)
            if (vitals != null)
            {
                int cap = Mathf.Max(1, definition.vitalCap);

                // Locked base rule: HP=STR, Stamina=DEX, Mana=INT
                int maxHp = Mathf.Clamp(definition.baseSTR, 1, cap);
                int maxSta = Mathf.Clamp(definition.baseDEX, 0, cap);
                int maxMana = Mathf.Clamp(definition.baseINT, 0, cap);

                vitals.SetMaxHpServer(maxHp, clampCurrent: false);
                vitals.SetMaxStaminaServer(maxSta, clampCurrent: false);
                vitals.SetMaxManaServer(maxMana, clampCurrent: false);

                // Start full.
                vitals.RestoreAllToMaxServer();
            }

            // 4) Regen ticking (server only)
            if (regenServer != null)
            {
                regenServer.Bind(definition, vitals);
                regenServer.SetServerTickEnabled(true);
            }
            else
            {
                Debug.LogWarning("[PlayerCore] PlayerVitalsRegenServer missing (no regen will occur). ");
            }

            Debug.Log($"[PlayerCore] Server initialization complete for '{definition.definitionId}'.");
        }

        private void ValidateReferences()
        {
            if (stats == null)
                Debug.LogError("[PlayerCore] Missing reference: PlayerStats.");

            if (vitals == null)
                Debug.LogError("[PlayerCore] Missing reference: ActorVitals.");

            if (skillBook == null)
                Debug.LogError("[PlayerCore] Missing reference: PlayerSkillBook.");
        }

        [ContextMenu("Auto-Wire Runtime Components")]
        private void AutoWire()
        {
            stats = GetComponentInChildren<PlayerStats>(true);
            vitals = GetComponentInChildren<UltimateDungeon.Combat.ActorVitals>(true);
            skillBook = GetComponentInChildren<PlayerSkillBook>(true);
            regenServer = GetComponentInChildren<PlayerVitalsRegenServer>(true);

            Debug.Log("[PlayerCore] AutoWire complete.");
        }
    }
}
