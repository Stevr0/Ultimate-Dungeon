// ============================================================================
// PlayerCombatStatsNet.cs — Ultimate Dungeon
// ----------------------------------------------------------------------------
// PURPOSE
// - Replicate server-authoritative combat stat snapshot to clients for UI.
//
// IMPORTANT ARCHITECTURE RULE
// - Combat (gameplay) should NOT read ItemDef/ItemInstance directly.
// - The server computes a single snapshot (PlayerCombatStatsServer).
// - This NetworkBehaviour mirrors that snapshot into NetworkVariables.
//
// WHY THIS FIX EXISTS
// - Your PlayerCombatStatsServer log shows HCI=0.35 (35%), but UI shows 0%.
// - UI reads PlayerCombatStatsNet.* NetworkVariables.
// - Therefore, HCI/DCI/DI are not being written correctly into these vars.
// - This file makes PlayerCombatStatsNet read ONLY the server snapshot and
//   write NetworkVariables from it.
//
// STORE AT
// Assets/_Scripts/Actors/Players/Networking/PlayerCombatStatsNet.cs
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
    /// Server writes NetworkVariables. Clients read for UI.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerCombatStatsNet : NetworkBehaviour
    {
        // --------------------------------------------------------------------
        // NetworkVariables (Server writes, Everyone reads)
        // --------------------------------------------------------------------

        public NetworkVariable<float> AttackerHciPct { get; private set; }
        public NetworkVariable<float> DefenderDciPct { get; private set; }
        public NetworkVariable<float> DamageIncreasePct { get; private set; }

        public NetworkVariable<int> ResistPhysical { get; private set; }
        public NetworkVariable<int> ResistFire { get; private set; }
        public NetworkVariable<int> ResistCold { get; private set; }
        public NetworkVariable<int> ResistPoison { get; private set; }
        public NetworkVariable<int> ResistEnergy { get; private set; }

        public NetworkVariable<FixedString64Bytes> WeaponName { get; private set; }
        public NetworkVariable<int> WeaponMinDamage { get; private set; }
        public NetworkVariable<int> WeaponMaxDamage { get; private set; }
        public NetworkVariable<float> WeaponSwingSpeedSeconds { get; private set; }

        public NetworkVariable<bool> CanAttack { get; private set; }
        public NetworkVariable<bool> CanCast { get; private set; }
        public NetworkVariable<bool> CanMove { get; private set; }
        public NetworkVariable<bool> CanBandage { get; private set; }

        // --------------------------------------------------------------------
        // References (server-only)
        // --------------------------------------------------------------------

        [Header("Server Source")]
        [Tooltip("Server-only combat stat aggregator that produces the authoritative snapshot.")]
        [SerializeField] private UltimateDungeon.Players.PlayerCombatStatsServer serverStats;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs;

        // --------------------------------------------------------------------
        // Unity lifecycle
        // --------------------------------------------------------------------

        private void Awake()
        {
            // IMPORTANT:
            // NetworkVariables must be constructed in Awake so they exist before
            // OnNetworkSpawn and before any UI attempts to subscribe.

            var serverWrite = NetworkVariableWritePermission.Server;

            AttackerHciPct = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, serverWrite);
            DefenderDciPct = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, serverWrite);
            DamageIncreasePct = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, serverWrite);

            ResistPhysical = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, serverWrite);
            ResistFire = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, serverWrite);
            ResistCold = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, serverWrite);
            ResistPoison = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, serverWrite);
            ResistEnergy = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, serverWrite);

            WeaponName = new NetworkVariable<FixedString64Bytes>(new FixedString64Bytes("Unarmed"), NetworkVariableReadPermission.Everyone, serverWrite);
            WeaponMinDamage = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, serverWrite);
            WeaponMaxDamage = new NetworkVariable<int>(4, NetworkVariableReadPermission.Everyone, serverWrite);
            WeaponSwingSpeedSeconds = new NetworkVariable<float>(2.0f, NetworkVariableReadPermission.Everyone, serverWrite);

            CanAttack = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, serverWrite);
            CanCast = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, serverWrite);
            CanMove = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, serverWrite);
            CanBandage = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, serverWrite);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!IsServer)
                return;

            // Auto-wire the server aggregator if not assigned.
            if (serverStats == null)
                serverStats = GetComponent<UltimateDungeon.Players.PlayerCombatStatsServer>();

            if (serverStats == null)
            {
                Debug.LogWarning("[PlayerCombatStatsNet] Missing PlayerCombatStatsServer on Player. UI combat stats will not update.", this);
                return;
            }

            // First push immediately.
            PushFromServerSnapshot("spawn");

            // Subscribe so we update when equipment changes.
            // This requires PlayerCombatStatsServer to expose the event below:
            //     public event Action<PlayerCombatStatsSnapshot> SnapshotChanged;
            // If you do NOT yet have that event, see NOTE below.
            serverStats.SnapshotChanged += HandleSnapshotChanged;
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer && serverStats != null)
                serverStats.SnapshotChanged -= HandleSnapshotChanged;

            base.OnNetworkDespawn();
        }

        // --------------------------------------------------------------------
        // Event handlers
        // --------------------------------------------------------------------

        private void HandleSnapshotChanged(UltimateDungeon.Players.PlayerCombatStatsSnapshot snapshot)
        {
            // The server has recomputed. Mirror it to clients.
            Push(snapshot, "snapshot-changed");
        }

        // --------------------------------------------------------------------
        // Snapshot mirroring
        // --------------------------------------------------------------------

        private void PushFromServerSnapshot(string reason)
        {
            if (serverStats == null)
                return;

            Push(serverStats.Snapshot, reason);
        }

        private void Push(UltimateDungeon.Players.PlayerCombatStatsSnapshot snapshot, string reason)
        {
            // IMPORTANT: these values are stored as 0..1 (scalars), not 0..100.
            // The UI multiplies by 100 when formatting (FormatPercent).

            AttackerHciPct.Value = snapshot.AttackerHciPct;
            DefenderDciPct.Value = snapshot.DefenderDciPct;
            DamageIncreasePct.Value = snapshot.DamageIncreasePct;

            ResistPhysical.Value = snapshot.ResistPhysical;
            ResistFire.Value = snapshot.ResistFire;
            ResistCold.Value = snapshot.ResistCold;
            ResistPoison.Value = snapshot.ResistPoison;
            ResistEnergy.Value = snapshot.ResistEnergy;

            WeaponName.Value = new FixedString64Bytes(string.IsNullOrWhiteSpace(snapshot.WeaponName) ? "Unarmed" : snapshot.WeaponName);
            WeaponMinDamage.Value = snapshot.WeaponMinDamage;
            WeaponMaxDamage.Value = snapshot.WeaponMaxDamage;
            WeaponSwingSpeedSeconds.Value = snapshot.WeaponSwingSpeedSeconds;

            CanAttack.Value = snapshot.CanAttack;
            CanCast.Value = snapshot.CanCast;
            CanMove.Value = snapshot.CanMove;
            CanBandage.Value = snapshot.CanBandage;

            if (enableDebugLogs)
            {
                Debug.Log(
                    $"[PlayerCombatStatsNet] Pushed ({reason}) " +
                    $"hci={AttackerHciPct.Value:0.00} dci={DefenderDciPct.Value:0.00} di={DamageIncreasePct.Value:0.00} " +
                    $"weapon='{WeaponName.Value}' dmg={WeaponMinDamage.Value}-{WeaponMaxDamage.Value} swing={WeaponSwingSpeedSeconds.Value:0.00}s",
                    this);
            }
        }

        // --------------------------------------------------------------------
        // NOTE: If you don't have SnapshotChanged yet
        // --------------------------------------------------------------------
        // If PlayerCombatStatsServer does not currently expose SnapshotChanged,
        // you have two options:
        // 1) Add the event and invoke it after recompute (recommended).
        // 2) Poll in Update() on server every X seconds and push when changed
        //    (works, but more hacky).
        // --------------------------------------------------------------------
    }
}
