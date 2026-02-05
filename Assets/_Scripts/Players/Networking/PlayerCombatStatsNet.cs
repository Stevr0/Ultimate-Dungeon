// ============================================================================
// PlayerCombatStatsNet.cs
// ----------------------------------------------------------------------------
// NGO replication glue for server-authoritative PlayerCombatStats snapshots.
//
// WHY THIS EXISTS:
// - Combat stats are computed on the server from equipment/statuses.
// - UI should read ONLY replicated NetworkVariables.
// - This keeps gameplay components off-limits to client UI code.
//
// IMPORTANT:
// - This does NOT implement new combat math. It only mirrors existing state.
// - If no authoritative aggregator exists yet, we build a minimal snapshot
//   from current equipment/affixes and clearly label it as a TEMPORARY adapter.
//
// Store at:
// Assets/_Scripts/Players/Networking/PlayerCombatStatsNet.cs
// ============================================================================

using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace UltimateDungeon.Players.Networking
{
    /// <summary>
    /// PlayerCombatStatsNet
    /// --------------------
    /// Attach to the Player prefab.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerCombatStatsNet : NetworkBehaviour
    {
        // --------------------------------------------------------------------
        // References
        // --------------------------------------------------------------------

        [Header("References")]
        [Tooltip("Equipment source (server authoritative).")]
        [SerializeField] private UltimateDungeon.Items.PlayerEquipmentComponent equipment;

        [Tooltip("Optional: player core for future aggregator hooks.")]
        [SerializeField] private UltimateDungeon.Players.PlayerCore playerCore;

        [Header("Update")]
        [Tooltip("Server poll interval for snapshot replication.")]
        [SerializeField] private float updateIntervalSeconds = 0.25f;

        // --------------------------------------------------------------------
        // NetworkVariables (Server Write)
        // --------------------------------------------------------------------

        public NetworkVariable<int> ResistPhysical = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<int> ResistFire = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<int> ResistCold = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<int> ResistPoison = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<int> ResistEnergy = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<float> AttackerHciPct = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<float> DefenderDciPct = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<float> DamageIncreasePct = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<bool> CanAttack = new NetworkVariable<bool>(
            true,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<bool> CanCast = new NetworkVariable<bool>(
            true,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<bool> CanMove = new NetworkVariable<bool>(
            true,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<bool> CanBandage = new NetworkVariable<bool>(
            true,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<FixedString64Bytes> WeaponName = new NetworkVariable<FixedString64Bytes>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<int> WeaponMinDamage = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<int> WeaponMaxDamage = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<float> WeaponSwingSpeedSeconds = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        // --------------------------------------------------------------------
        // Cache to avoid writing when unchanged
        // --------------------------------------------------------------------

        private int _lastResistPhysical;
        private int _lastResistFire;
        private int _lastResistCold;
        private int _lastResistPoison;
        private int _lastResistEnergy;

        private float _lastAttackerHciPct;
        private float _lastDefenderDciPct;
        private float _lastDamageIncreasePct;

        private bool _lastCanAttack;
        private bool _lastCanCast;
        private bool _lastCanMove;
        private bool _lastCanBandage;

        private FixedString64Bytes _lastWeaponName;
        private int _lastWeaponMinDamage;
        private int _lastWeaponMaxDamage;
        private float _lastWeaponSwingSpeedSeconds;

        private float _nextUpdateTime;

        private const float FloatEpsilon = 0.0001f;

        private void Reset()
        {
            if (equipment == null)
                equipment = GetComponentInChildren<UltimateDungeon.Items.PlayerEquipmentComponent>(true);

            if (playerCore == null)
                playerCore = GetComponentInChildren<UltimateDungeon.Players.PlayerCore>(true);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (equipment == null)
                equipment = GetComponentInChildren<UltimateDungeon.Items.PlayerEquipmentComponent>(true);

            if (IsServer)
            {
                PushSnapshot(force: true);
                _nextUpdateTime = Time.time + Mathf.Max(0.05f, updateIntervalSeconds);
            }
        }

        private void Update()
        {
            if (!IsServer)
                return;

            if (Time.time < _nextUpdateTime)
                return;

            _nextUpdateTime = Time.time + Mathf.Max(0.05f, updateIntervalSeconds);
            PushSnapshot(force: false);
        }

        /// <summary>
        /// Server-side: snapshot combat stats into NetworkVariables.
        /// Writes only on change unless forced.
        /// </summary>
        private void PushSnapshot(bool force)
        {
            var snapshot = BuildSnapshotFromCurrentState();

            bool anyChanged = force;

            anyChanged |= SetIfChanged(ref _lastResistPhysical, snapshot.ResistPhysical, ResistPhysical, force);
            anyChanged |= SetIfChanged(ref _lastResistFire, snapshot.ResistFire, ResistFire, force);
            anyChanged |= SetIfChanged(ref _lastResistCold, snapshot.ResistCold, ResistCold, force);
            anyChanged |= SetIfChanged(ref _lastResistPoison, snapshot.ResistPoison, ResistPoison, force);
            anyChanged |= SetIfChanged(ref _lastResistEnergy, snapshot.ResistEnergy, ResistEnergy, force);

            anyChanged |= SetIfChanged(ref _lastAttackerHciPct, snapshot.AttackerHciPct, AttackerHciPct, force);
            anyChanged |= SetIfChanged(ref _lastDefenderDciPct, snapshot.DefenderDciPct, DefenderDciPct, force);
            anyChanged |= SetIfChanged(ref _lastDamageIncreasePct, snapshot.DamageIncreasePct, DamageIncreasePct, force);

            anyChanged |= SetIfChanged(ref _lastCanAttack, snapshot.CanAttack, CanAttack, force);
            anyChanged |= SetIfChanged(ref _lastCanCast, snapshot.CanCast, CanCast, force);
            anyChanged |= SetIfChanged(ref _lastCanMove, snapshot.CanMove, CanMove, force);
            anyChanged |= SetIfChanged(ref _lastCanBandage, snapshot.CanBandage, CanBandage, force);

            anyChanged |= SetIfChanged(ref _lastWeaponName, snapshot.WeaponName, WeaponName, force);
            anyChanged |= SetIfChanged(ref _lastWeaponMinDamage, snapshot.WeaponMinDamage, WeaponMinDamage, force);
            anyChanged |= SetIfChanged(ref _lastWeaponMaxDamage, snapshot.WeaponMaxDamage, WeaponMaxDamage, force);
            anyChanged |= SetIfChanged(ref _lastWeaponSwingSpeedSeconds, snapshot.WeaponSwingSpeedSeconds, WeaponSwingSpeedSeconds, force);

            if (!anyChanged)
                return;
        }

        // --------------------------------------------------------------------
        // Snapshot adapter (TEMPORARY)
        // --------------------------------------------------------------------

        /// <summary>
        /// TEMPORARY ADAPTER:
        /// Builds a minimal snapshot from current equipment/affixes/status gates.
        ///
        /// TODO:
        /// - Replace this with the authoritative PlayerCombatStats aggregator
        ///   once it exists, without changing replicated fields.
        /// </summary>
        private CombatSnapshot BuildSnapshotFromCurrentState()
        {
            var snapshot = new CombatSnapshot
            {
                // Default gates (until StatusEffectSystem exists).
                CanAttack = true,
                CanCast = true,
                CanMove = true,
                CanBandage = true,

                // Unarmed baseline (from PLAYER_COMBAT_STATS.md design doc).
                WeaponName = new FixedString64Bytes("Unarmed"),
                WeaponMinDamage = 1,
                WeaponMaxDamage = 4,
                WeaponSwingSpeedSeconds = 2.0f,
            };

            if (equipment == null || !IsServer)
                return snapshot;

            float hciPercentTotal = 0f;
            float dciPercentTotal = 0f;
            float diPercentTotal = 0f;

            int resistPhysical = 0;
            int resistFire = 0;
            int resistCold = 0;
            int resistPoison = 0;
            int resistEnergy = 0;

            foreach (UltimateDungeon.Items.EquipSlot slot in System.Enum.GetValues(typeof(UltimateDungeon.Items.EquipSlot)))
            {
                if (slot == UltimateDungeon.Items.EquipSlot.None)
                    continue;

                if (!equipment.TryGetEquippedItem(slot, out var instance, out var def))
                    continue;

                if (def != null)
                {
                    resistPhysical += def.armor.resistPhysical;
                    resistFire += def.armor.resistFire;
                    resistCold += def.armor.resistCold;
                    resistPoison += def.armor.resistPoison;
                    resistEnergy += def.armor.resistEnergy;

                    if (slot == UltimateDungeon.Items.EquipSlot.Mainhand && def.family == UltimateDungeon.Items.ItemFamily.Mainhand)
                    {
                        snapshot.WeaponName = new FixedString64Bytes(def.displayName);
                        snapshot.WeaponMinDamage = def.weapon.minDamage;
                        snapshot.WeaponMaxDamage = def.weapon.maxDamage;
                        snapshot.WeaponSwingSpeedSeconds = def.weapon.swingSpeedSeconds;
                    }
                }

                if (instance != null && instance.affixes != null)
                {
                    foreach (var affix in instance.affixes)
                    {
                        switch (affix.id)
                        {
                            case UltimateDungeon.Items.AffixId.Combat_HitChance:
                                hciPercentTotal += affix.magnitude;
                                break;
                            case UltimateDungeon.Items.AffixId.Combat_DefenseChance:
                                dciPercentTotal += affix.magnitude;
                                break;
                            case UltimateDungeon.Items.AffixId.Combat_DamageIncrease:
                                diPercentTotal += affix.magnitude;
                                break;
                            case UltimateDungeon.Items.AffixId.Resist_Physical:
                                resistPhysical += Mathf.RoundToInt(affix.magnitude);
                                break;
                            case UltimateDungeon.Items.AffixId.Resist_Fire:
                                resistFire += Mathf.RoundToInt(affix.magnitude);
                                break;
                            case UltimateDungeon.Items.AffixId.Resist_Cold:
                                resistCold += Mathf.RoundToInt(affix.magnitude);
                                break;
                            case UltimateDungeon.Items.AffixId.Resist_Poison:
                                resistPoison += Mathf.RoundToInt(affix.magnitude);
                                break;
                            case UltimateDungeon.Items.AffixId.Resist_Energy:
                                resistEnergy += Mathf.RoundToInt(affix.magnitude);
                                break;
                        }
                    }
                }
            }

            snapshot.ResistPhysical = resistPhysical;
            snapshot.ResistFire = resistFire;
            snapshot.ResistCold = resistCold;
            snapshot.ResistPoison = resistPoison;
            snapshot.ResistEnergy = resistEnergy;

            snapshot.AttackerHciPct = hciPercentTotal * 0.01f;
            snapshot.DefenderDciPct = dciPercentTotal * 0.01f;
            snapshot.DamageIncreasePct = diPercentTotal * 0.01f;

            return snapshot;
        }

        // --------------------------------------------------------------------
        // Helpers
        // --------------------------------------------------------------------

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

        private static bool SetIfChanged(ref bool cached, bool value, NetworkVariable<bool> target, bool force)
        {
            if (!force && cached == value)
                return false;

            cached = value;
            target.Value = value;
            return true;
        }

        private static bool SetIfChanged(ref FixedString64Bytes cached, FixedString64Bytes value, NetworkVariable<FixedString64Bytes> target, bool force)
        {
            if (!force && cached.Equals(value))
                return false;

            cached = value;
            target.Value = value;
            return true;
        }

        private struct CombatSnapshot
        {
            public int ResistPhysical;
            public int ResistFire;
            public int ResistCold;
            public int ResistPoison;
            public int ResistEnergy;

            public float AttackerHciPct;
            public float DefenderDciPct;
            public float DamageIncreasePct;

            public bool CanAttack;
            public bool CanCast;
            public bool CanMove;
            public bool CanBandage;

            public FixedString64Bytes WeaponName;
            public int WeaponMinDamage;
            public int WeaponMaxDamage;
            public float WeaponSwingSpeedSeconds;
        }
    }
}
