// ============================================================================
// PlayerStatsNet.cs
// ----------------------------------------------------------------------------
// NGO replication glue for PlayerStats.
//
// Why this exists:
// - PlayerStats is a MonoBehaviour (gameplay-only, server-authoritative).
// - Replication stays isolated in a NetworkBehaviour.
// - Clients read NetworkVariables for UI display.
//
// What it replicates:
// - Base STR/DEX/INT (optional, useful for UI)
// - Effective STR/DEX/INT (base + modifiers)
//
// Authority:
// - Server writes, Everyone reads.
//
// Recommended folder:
// Assets/Scripts/Players/Networking/PlayerStatsNet.cs
// ============================================================================

using Unity.Netcode;
using UnityEngine;

namespace UltimateDungeon.Players.Networking
{
    /// <summary>
    /// PlayerStatsNet
    /// -------------
    /// Attach to the Player prefab.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerStatsNet : NetworkBehaviour
    {
        // --------------------------------------------------------------------
        // References
        // --------------------------------------------------------------------

        [Header("References")]
        [Tooltip("Gameplay stats component. Server reads from this.")]
        [SerializeField] private UltimateDungeon.Players.PlayerStats stats;

        [Tooltip("Equipment source (server authoritative).")]
        [SerializeField] private UltimateDungeon.Items.PlayerEquipmentComponent equipment;

        [Tooltip("Player core definition (authoritative baselines/caps).")]
        [SerializeField] private UltimateDungeon.Players.PlayerCore playerCore;

        [Tooltip("Actor vitals to update on the server.")]
        [SerializeField] private UltimateDungeon.Combat.ActorVitals vitals;

        [Tooltip("Server regen system (optional).")]
        [SerializeField] private UltimateDungeon.Players.PlayerVitalsRegenServer regenServer;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs;

        // --------------------------------------------------------------------
        // NetworkVariables (Server Write)
        // --------------------------------------------------------------------

        public NetworkVariable<int> BaseSTR = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<int> BaseDEX = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<int> BaseINT = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<int> STR = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<int> DEX = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<int> INT = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<int> MaxHP = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<int> MaxStamina = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<int> MaxMana = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<float> HpRegenPerSec = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<float> StaminaRegenPerSec = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<float> ManaRegenPerSec = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        // --------------------------------------------------------------------
        // Cache to avoid writing when unchanged
        // --------------------------------------------------------------------

        private int _lastBaseSTR;
        private int _lastBaseDEX;
        private int _lastBaseINT;

        private int _lastSTR;
        private int _lastDEX;
        private int _lastINT;

        private int _lastMaxHp;
        private int _lastMaxStamina;
        private int _lastMaxMana;

        private float _lastHpRegenPerSec;
        private float _lastStaminaRegenPerSec;
        private float _lastManaRegenPerSec;

        private int _itemBonusStr;
        private int _itemBonusDex;
        private int _itemBonusInt;

        private int _itemBonusMaxHp;
        private int _itemBonusMaxStamina;
        private int _itemBonusMaxMana;

        private float _itemBonusHpRegen;
        private float _itemBonusStaminaRegen;
        private float _itemBonusManaRegen;

        private const float FloatEpsilon = 0.0001f;

        private bool _pendingDebugLog;
        private string _pendingDebugReason;

        private void Reset()
        {
            if (stats == null)
                stats = GetComponentInChildren<UltimateDungeon.Players.PlayerStats>(true);

            if (stats == null)
                stats = GetComponentInParent<UltimateDungeon.Players.PlayerStats>();

            if (equipment == null)
                equipment = GetComponentInChildren<UltimateDungeon.Items.PlayerEquipmentComponent>(true);

            if (playerCore == null)
                playerCore = GetComponentInChildren<UltimateDungeon.Players.PlayerCore>(true);

            if (vitals == null)
                vitals = GetComponentInChildren<UltimateDungeon.Combat.ActorVitals>(true);

            if (regenServer == null)
                regenServer = GetComponentInChildren<UltimateDungeon.Players.PlayerVitalsRegenServer>(true);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (stats == null)
                stats = GetComponentInChildren<UltimateDungeon.Players.PlayerStats>(true);

            if (IsServer)
            {
                SubscribeToEquipment();
                RecomputeAndReplicate(force: true, reason: "spawn");
            }
        }

        private void Update()
        {
            if (!IsServer)
                return;

            if (stats == null)
                return;

            PushSnapshot(force: false);
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            UnsubscribeFromEquipment();
        }

        private void SubscribeToEquipment()
        {
            if (equipment != null)
                equipment.OnEquipmentChanged += HandleEquipmentChanged;
        }

        private void UnsubscribeFromEquipment()
        {
            if (equipment != null)
                equipment.OnEquipmentChanged -= HandleEquipmentChanged;
        }

        private void HandleEquipmentChanged()
        {
            if (!IsServer)
                return;

            RecomputeAndReplicate(force: true, reason: "equipment");
        }

        /// <summary>
        /// Server-side: snapshot stats into NetworkVariables.
        /// Writes only on change unless forced.
        /// </summary>
        private void PushSnapshot(bool force)
        {
            if (stats == null)
                return;

            int baseStr = stats.BaseSTR;
            int baseDex = stats.BaseDEX;
            int baseInt = stats.BaseINT;

            int str = stats.STR;
            int dex = stats.DEX;
            int intel = stats.INT;

            bool statsChanged = force;

            if (!force)
            {
                if (baseStr == _lastBaseSTR && baseDex == _lastBaseDEX && baseInt == _lastBaseINT &&
                    str == _lastSTR && dex == _lastDEX && intel == _lastINT)
                {
                    statsChanged = false;
                }
                else
                {
                    statsChanged = true;
                }
            }
            else
            {
                statsChanged = true;
            }

            if (statsChanged)
            {
                _lastBaseSTR = baseStr;
                _lastBaseDEX = baseDex;
                _lastBaseINT = baseInt;

                _lastSTR = str;
                _lastDEX = dex;
                _lastINT = intel;

                BaseSTR.Value = baseStr;
                BaseDEX.Value = baseDex;
                BaseINT.Value = baseInt;

                STR.Value = str;
                DEX.Value = dex;
                INT.Value = intel;
            }

            ApplyDerivedVitalsAndRegen(force || statsChanged);
        }

        /// <summary>
        /// Server-side: recompute equipment-driven bonuses, update stats, vitals, and replication.
        /// </summary>
        private void RecomputeAndReplicate(bool force, string reason)
        {
            if (!IsServer || stats == null)
                return;

            RecomputeEquipmentBonuses();

            stats.SetItemAttributeModifiers(_itemBonusStr, _itemBonusDex, _itemBonusInt);

            _pendingDebugLog = enableDebugLogs;
            _pendingDebugReason = reason;

            PushSnapshot(force: force);
        }

        private void RecomputeEquipmentBonuses()
        {
            _itemBonusStr = 0;
            _itemBonusDex = 0;
            _itemBonusInt = 0;
            _itemBonusMaxHp = 0;
            _itemBonusMaxStamina = 0;
            _itemBonusMaxMana = 0;
            _itemBonusHpRegen = 0f;
            _itemBonusStaminaRegen = 0f;
            _itemBonusManaRegen = 0f;

            if (equipment == null)
                return;

            foreach (UltimateDungeon.Items.EquipSlot slot in System.Enum.GetValues(typeof(UltimateDungeon.Items.EquipSlot)))
            {
                if (slot == UltimateDungeon.Items.EquipSlot.None)
                    continue;

                if (!equipment.TryGetEquippedItem(slot, out var instance, out _))
                    continue;

                if (instance?.affixes == null)
                    continue;

                foreach (var affix in instance.affixes)
                {
                    switch (affix.id)
                    {
                        case UltimateDungeon.Items.AffixId.Stat_MaxStrength:
                            _itemBonusStr += Mathf.RoundToInt(affix.magnitude);
                            break;
                        case UltimateDungeon.Items.AffixId.Stat_MaxDexterity:
                            _itemBonusDex += Mathf.RoundToInt(affix.magnitude);
                            break;
                        case UltimateDungeon.Items.AffixId.Stat_MaxInteligence:
                            _itemBonusInt += Mathf.RoundToInt(affix.magnitude);
                            break;
                        case UltimateDungeon.Items.AffixId.Vital_MaxHP:
                            _itemBonusMaxHp += Mathf.RoundToInt(affix.magnitude);
                            break;
                        case UltimateDungeon.Items.AffixId.Vital_MaxStamina:
                            _itemBonusMaxStamina += Mathf.RoundToInt(affix.magnitude);
                            break;
                        case UltimateDungeon.Items.AffixId.Vital_MaxMana:
                            _itemBonusMaxMana += Mathf.RoundToInt(affix.magnitude);
                            break;
                        case UltimateDungeon.Items.AffixId.Regenerate_Health:
                            _itemBonusHpRegen += affix.magnitude;
                            break;
                        case UltimateDungeon.Items.AffixId.Regenerate_Stamina:
                            _itemBonusStaminaRegen += affix.magnitude;
                            break;
                        case UltimateDungeon.Items.AffixId.Regenerate_Mana:
                            _itemBonusManaRegen += affix.magnitude;
                            break;
                    }
                }
            }
        }

        private void ApplyDerivedVitalsAndRegen(bool force)
        {
            if (!IsServer || stats == null)
                return;

            var definition = playerCore != null ? playerCore.definition : null;
            int cap = definition != null ? Mathf.Max(1, definition.vitalCap) : 150;

            int maxHp = Mathf.Clamp(stats.STR + _itemBonusMaxHp, 1, cap);
            int maxStamina = Mathf.Clamp(stats.DEX + _itemBonusMaxStamina, 0, cap);
            int maxMana = Mathf.Clamp(stats.INT + _itemBonusMaxMana, 0, cap);

            float baseHpRegen = definition != null ? definition.hpRegenPerSec : 0f;
            float baseStaminaRegen = definition != null ? definition.staminaRegenPerSec : 0f;
            float baseManaRegen = definition != null ? definition.manaRegenPerSec : 0f;

            float hpRegen = Mathf.Max(0f, baseHpRegen + _itemBonusHpRegen);
            float staminaRegen = Mathf.Max(0f, baseStaminaRegen + _itemBonusStaminaRegen);
            float manaRegen = Mathf.Max(0f, baseManaRegen + _itemBonusManaRegen);

            if (_pendingDebugLog)
            {
                Debug.Log($"[PlayerStatsNet] Recompute ({_pendingDebugReason}) " +
                          $"STR={stats.STR} DEX={stats.DEX} INT={stats.INT} " +
                          $"MaxHP={maxHp} MaxStam={maxStamina} MaxMana={maxMana} " +
                          $"Regen={hpRegen:0.##}/{staminaRegen:0.##}/{manaRegen:0.##}");
                _pendingDebugLog = false;
                _pendingDebugReason = null;
            }

            SetIfChanged(ref _lastMaxHp, maxHp, MaxHP, force);
            SetIfChanged(ref _lastMaxStamina, maxStamina, MaxStamina, force);
            SetIfChanged(ref _lastMaxMana, maxMana, MaxMana, force);

            SetIfChanged(ref _lastHpRegenPerSec, hpRegen, HpRegenPerSec, force);
            SetIfChanged(ref _lastStaminaRegenPerSec, staminaRegen, StaminaRegenPerSec, force);
            SetIfChanged(ref _lastManaRegenPerSec, manaRegen, ManaRegenPerSec, force);

            if (vitals != null)
            {
                vitals.SetMaxHpServer(maxHp, clampCurrent: true);
                vitals.SetMaxStaminaServer(maxStamina, clampCurrent: true);
                vitals.SetMaxManaServer(maxMana, clampCurrent: true);
            }

            if (regenServer != null)
                regenServer.SetRegenRatesServer(hpRegen, staminaRegen, manaRegen);
        }

        // --------------------------------------------------------------------
        // UI helpers
        // --------------------------------------------------------------------

        public string GetStatLine()
        {
            return $"STR {STR.Value} / DEX {DEX.Value} / INT {INT.Value}";
        }

        private static bool SetIfChanged(ref int cached, int value, NetworkVariable<int> target, bool force)
        {
            if (!force && cached == value)
                return false;

            cached = value;
            target.Value = value;
            return true;
        }

        private static bool SetIfChanged(ref float cached, float value, NetworkVariable<float> target, bool force)
        {
            if (!force && Mathf.Abs(cached - value) <= FloatEpsilon)
                return false;

            cached = value;
            target.Value = value;
            return true;
        }
    }
}
