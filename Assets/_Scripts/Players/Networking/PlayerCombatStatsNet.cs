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
// - Combat stats are sourced from PlayerCombatStatsServer snapshots.
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
        [Tooltip("Authoritative combat stats aggregator (server-only).")]
        [SerializeField] private UltimateDungeon.Players.PlayerCombatStatsServer combatStatsServer;

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
            if (combatStatsServer == null)
                combatStatsServer = GetComponentInChildren<UltimateDungeon.Players.PlayerCombatStatsServer>(true);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (combatStatsServer == null)
                combatStatsServer = GetComponentInChildren<UltimateDungeon.Players.PlayerCombatStatsServer>(true);

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
            var snapshot = GetAuthoritativeSnapshot();

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

            var weaponName = new FixedString64Bytes(
                string.IsNullOrWhiteSpace(snapshot.WeaponName) ? "Unarmed" : snapshot.WeaponName);

            anyChanged |= SetIfChanged(ref _lastWeaponName, weaponName, WeaponName, force);
            anyChanged |= SetIfChanged(ref _lastWeaponMinDamage, snapshot.WeaponMinDamage, WeaponMinDamage, force);
            anyChanged |= SetIfChanged(ref _lastWeaponMaxDamage, snapshot.WeaponMaxDamage, WeaponMaxDamage, force);
            anyChanged |= SetIfChanged(ref _lastWeaponSwingSpeedSeconds, snapshot.WeaponSwingSpeedSeconds, WeaponSwingSpeedSeconds, force);

            if (!anyChanged)
                return;
        }

        // --------------------------------------------------------------------
        // Snapshot adapter
        // --------------------------------------------------------------------

        /// <summary>
        /// Mirrors the authoritative PlayerCombatStatsServer snapshot.
        /// </summary>
        private UltimateDungeon.Players.PlayerCombatStatsSnapshot GetAuthoritativeSnapshot()
        {
            if (!IsServer || combatStatsServer == null)
                return CreateDefaultSnapshot();

            return combatStatsServer.Snapshot;
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

        private static UltimateDungeon.Players.PlayerCombatStatsSnapshot CreateDefaultSnapshot()
        {
            return new UltimateDungeon.Players.PlayerCombatStatsSnapshot
            {
                CanAttack = true,
                CanCast = true,
                CanMove = true,
                CanBandage = true,
                WeaponName = "Unarmed",
                WeaponMinDamage = 1,
                WeaponMaxDamage = 4,
                WeaponSwingSpeedSeconds = 2.0f
            };
        }
    }
}
