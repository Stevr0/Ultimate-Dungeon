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

        // --------------------------------------------------------------------
        // Cache to avoid writing when unchanged
        // --------------------------------------------------------------------

        private int _lastBaseSTR;
        private int _lastBaseDEX;
        private int _lastBaseINT;

        private int _lastSTR;
        private int _lastDEX;
        private int _lastINT;

        private void Reset()
        {
            if (stats == null)
                stats = GetComponentInChildren<UltimateDungeon.Players.PlayerStats>(true);

            if (stats == null)
                stats = GetComponentInParent<UltimateDungeon.Players.PlayerStats>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (stats == null)
                stats = GetComponentInChildren<UltimateDungeon.Players.PlayerStats>(true);

            if (IsServer)
            {
                PushSnapshot(force: true);
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

        /// <summary>
        /// Server-side: snapshot stats into NetworkVariables.
        /// Writes only on change unless forced.
        /// </summary>
        private void PushSnapshot(bool force)
        {
            int baseStr = stats.BaseSTR;
            int baseDex = stats.BaseDEX;
            int baseInt = stats.BaseINT;

            int str = stats.STR;
            int dex = stats.DEX;
            int intel = stats.INT;

            if (!force)
            {
                if (baseStr == _lastBaseSTR && baseDex == _lastBaseDEX && baseInt == _lastBaseINT &&
                    str == _lastSTR && dex == _lastDEX && intel == _lastINT)
                {
                    return;
                }
            }

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

        // --------------------------------------------------------------------
        // UI helpers
        // --------------------------------------------------------------------

        public string GetStatLine()
        {
            return $"STR {STR.Value} / DEX {DEX.Value} / INT {INT.Value}";
        }
    }
}
