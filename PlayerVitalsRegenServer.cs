// ============================================================================
// PlayerVitalsRegenServer.cs (v0.2)
// ----------------------------------------------------------------------------
// Server-only vitals regeneration for Players.
//
// Reads regen rates from PlayerDefinition:
// - hpRegenPerSec
// - staminaRegenPerSec
// - manaRegenPerSec
//
// Writes to ActorVitals (single vitals source of truth).
// ============================================================================

using UnityEngine;
using Unity.Netcode;

namespace UltimateDungeon.Players
{
    [DisallowMultipleComponent]
    public sealed class PlayerVitalsRegenServer : MonoBehaviour
    {
        [Header("Runtime Bindings")]
        [SerializeField] private PlayerDefinition definition;
        [SerializeField] private UltimateDungeon.Combat.ActorVitals vitals;

        [Header("Tick")]
        [SerializeField] private bool serverTickEnabled = false;

        // Accumulators allow fractional regen-per-second rates.
        private float _hpAcc;
        private float _staAcc;
        private float _manaAcc;

        private bool _hasRegenOverride;
        private float _overrideHpRegenPerSec;
        private float _overrideStaminaRegenPerSec;
        private float _overrideManaRegenPerSec;

        private void Awake()
        {
            if (vitals == null)
                vitals = GetComponentInChildren<UltimateDungeon.Combat.ActorVitals>(true);
        }

        public void Bind(PlayerDefinition def, UltimateDungeon.Combat.ActorVitals actorVitals)
        {
            definition = def;
            vitals = actorVitals;

            _hpAcc = 0f;
            _staAcc = 0f;
            _manaAcc = 0f;
        }

        public void SetServerTickEnabled(bool enabled)
        {
            serverTickEnabled = enabled;
        }

        /// <summary>
        /// Server-only: override regen rates (used for equipment/status-derived bonuses).
        /// </summary>
        public void SetRegenRatesServer(float hpPerSec, float staminaPerSec, float manaPerSec)
        {
            if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
                return;

            _hasRegenOverride = true;
            _overrideHpRegenPerSec = hpPerSec;
            _overrideStaminaRegenPerSec = staminaPerSec;
            _overrideManaRegenPerSec = manaPerSec;
        }

        private void Update()
        {
            // Server-only gate
            if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
                return;

            if (!serverTickEnabled)
                return;

            if (definition == null || vitals == null)
                return;

            // Dead players do not regen by default.
            if (vitals.CurrentHPNet.Value <= 0)
                return;

            float dt = Time.deltaTime;
            if (dt <= 0f)
                return;

            float hpPerSec = _hasRegenOverride ? _overrideHpRegenPerSec : definition.hpRegenPerSec;
            float staminaPerSec = _hasRegenOverride ? _overrideStaminaRegenPerSec : definition.staminaRegenPerSec;
            float manaPerSec = _hasRegenOverride ? _overrideManaRegenPerSec : definition.manaRegenPerSec;

            hpPerSec = Mathf.Max(0f, hpPerSec);
            staminaPerSec = Mathf.Max(0f, staminaPerSec);
            manaPerSec = Mathf.Max(0f, manaPerSec);

            _hpAcc += hpPerSec * dt;
            _staAcc += staminaPerSec * dt;
            _manaAcc += manaPerSec * dt;

            int hpWhole = Mathf.FloorToInt(_hpAcc);
            if (hpWhole > 0)
            {
                vitals.Heal(hpWhole);
                _hpAcc -= hpWhole;
            }

            int staWhole = Mathf.FloorToInt(_staAcc);
            if (staWhole > 0)
            {
                vitals.RestoreStamina(staWhole);
                _staAcc -= staWhole;
            }

            int manaWhole = Mathf.FloorToInt(_manaAcc);
            if (manaWhole > 0)
            {
                vitals.RestoreMana(manaWhole);
                _manaAcc -= manaWhole;
            }
        }
    }
}
