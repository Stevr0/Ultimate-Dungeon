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
                ServerRecomputeEffectiveStats("spawn");
            }
        }

        private void Update()
        {
            if (!IsServer)
                return;

            if (stats == null)
                return;

            if (stats.BaseSTR != _lastBaseSTR || stats.BaseDEX != _lastBaseDEX || stats.BaseINT != _lastBaseINT ||
                stats.STR != _lastSTR || stats.DEX != _lastDEX || stats.INT != _lastINT)
            {
                ServerRecomputeEffectiveStats("stats");
            }
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

            ServerRecomputeEffectiveStats("equipment");
        }

        /// <summary>
        /// Server-side: recompute effective stats from definition + equipment affixes.
        /// </summary>
        public void ServerRecomputeEffectiveStats(string reason = "manual")
        {
            if (!IsServer)
                return;

            var definition = playerCore != null ? playerCore.definition : null;
            int baseStr = stats != null ? stats.BaseSTR : (definition != null ? definition.baseSTR : 0);
            int baseDex = stats != null ? stats.BaseDEX : (definition != null ? definition.baseDEX : 0);
            int baseInt = stats != null ? stats.BaseINT : (definition != null ? definition.baseINT : 0);

            _itemBonusStr = 0;
            _itemBonusDex = 0;
            _itemBonusInt = 0;
            _itemBonusMaxHp = 0;
            _itemBonusMaxStamina = 0;
            _itemBonusMaxMana = 0;
            _itemBonusHpRegen = 0f;
            _itemBonusStaminaRegen = 0f;
            _itemBonusManaRegen = 0f;

            var equippedCount = 0;
            if (equipment != null)
            {
                var instances = equipment.ServerGetEquippedInstancesSnapshot();
                equippedCount = instances.Count;
                for (int i = 0; i < instances.Count; i++)
                {
                    var instance = instances[i];
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

            if (stats != null)
                stats.SetItemAttributeModifiers(_itemBonusStr, _itemBonusDex, _itemBonusInt);

            int effectiveStr = stats != null ? stats.STR : baseStr + _itemBonusStr;
            int effectiveDex = stats != null ? stats.DEX : baseDex + _itemBonusDex;
            int effectiveInt = stats != null ? stats.INT : baseInt + _itemBonusInt;

            int cap = definition != null ? Mathf.Max(1, definition.vitalCap) : 150;
            int maxHp = Mathf.Clamp(effectiveStr + _itemBonusMaxHp, 1, cap);
            int maxStamina = Mathf.Clamp(effectiveDex + _itemBonusMaxStamina, 0, cap);
            int maxMana = Mathf.Clamp(effectiveInt + _itemBonusMaxMana, 0, cap);

            float baseHpRegen = definition != null ? definition.hpRegenPerSec : 0f;
            float baseStaminaRegen = definition != null ? definition.staminaRegenPerSec : 0f;
            float baseManaRegen = definition != null ? definition.manaRegenPerSec : 0f;

            float hpRegen = Mathf.Max(0f, baseHpRegen + _itemBonusHpRegen);
            float staminaRegen = Mathf.Max(0f, baseStaminaRegen + _itemBonusStaminaRegen);
            float manaRegen = Mathf.Max(0f, baseManaRegen + _itemBonusManaRegen);

            Debug.Log($"[PlayerStatsNet] Equipped instances read={equippedCount} " +
                      $"STR={effectiveStr} INT={effectiveInt} " +
                      $"Regen={hpRegen:0.##}/{staminaRegen:0.##}/{manaRegen:0.##}");

            if (enableDebugLogs)
            {
                Debug.Log($"[PlayerStatsNet] Recompute ({reason}) " +
                          $"STR={effectiveStr} DEX={effectiveDex} INT={effectiveInt} " +
                          $"MaxHP={maxHp} MaxStam={maxStamina} MaxMana={maxMana} " +
                          $"Regen={hpRegen:0.##}/{staminaRegen:0.##}/{manaRegen:0.##}");
            }

            SetIfChanged(ref _lastBaseSTR, baseStr, BaseSTR, force: false);
            SetIfChanged(ref _lastBaseDEX, baseDex, BaseDEX, force: false);
            SetIfChanged(ref _lastBaseINT, baseInt, BaseINT, force: false);

            SetIfChanged(ref _lastSTR, effectiveStr, STR, force: false);
            SetIfChanged(ref _lastDEX, effectiveDex, DEX, force: false);
            SetIfChanged(ref _lastINT, effectiveInt, INT, force: false);

            SetIfChanged(ref _lastMaxHp, maxHp, MaxHP, force: false);
            SetIfChanged(ref _lastMaxStamina, maxStamina, MaxStamina, force: false);
            SetIfChanged(ref _lastMaxMana, maxMana, MaxMana, force: false);

            SetIfChanged(ref _lastHpRegenPerSec, hpRegen, HpRegenPerSec, force: false);
            SetIfChanged(ref _lastStaminaRegenPerSec, staminaRegen, StaminaRegenPerSec, force: false);
            SetIfChanged(ref _lastManaRegenPerSec, manaRegen, ManaRegenPerSec, force: false);

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
