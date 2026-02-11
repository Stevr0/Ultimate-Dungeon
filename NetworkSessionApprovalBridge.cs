// ============================================================================
// NetworkSessionApprovalBridge.cs — Ultimate Dungeon
// ----------------------------------------------------------------------------
// Hooks NGO connection approval and turns the connection payload into the
// authoritative AccountId used by session + persistence.
//
// IMPORTANT (Design Locks):
// - Identity is AccountId (planned: SteamId). Never use username as identity.
// - Join flow gates (SESSION_AND_PERSISTENCE_MODEL.md):
//     * BuildId mismatch -> reject
//     * Duplicate login (same AccountId already connected) -> reject
//     * Shard full -> reject (optional; usually handled by NGO max connections)
// - No passwords. Shards are public.
//
// Attach to the same GameObject as NetworkManager (typically in SCN_Bootstrap).
// ============================================================================

using System;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace UltimateDungeon.Networking
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkManager))]
    public sealed class NetworkSessionApprovalBridge : MonoBehaviour
    {
        // --------------------------------------------------------------------
        // Connection Payload (client -> server during approval)
        // --------------------------------------------------------------------
        // Keep this tiny. It is NOT character data.
        // Character Snapshot is a separate step after approval.
        // --------------------------------------------------------------------

        [Serializable]
        private struct JoinPayload
        {
            public string accountId;    // canonical, stable identity (local GUID today, SteamId later)
            public string displayName;  // cosmetic only
            public string buildId;      // optional gating (must match if server requires)
        }

        [Header("Optional Server Gates")]
        [Tooltip("When set, clients must send the same buildId in connection payload.")]
        [SerializeField] private string requiredClientBuildId = string.Empty;

        [Tooltip("Optional: maximum players allowed on this shard.")]
        [Min(0)]
        [SerializeField] private int maxPlayers = 0; // 0 = do not enforce here

        private NetworkManager _networkManager;

        private void Awake()
        {
            _networkManager = GetComponent<NetworkManager>();
            if (_networkManager == null)
            {
                Debug.LogError("[NetworkSessionApprovalBridge] Missing NetworkManager component.");
                return;
            }

            _networkManager.NetworkConfig.ConnectionApproval = true;

            // We own approval callback registration so server-side login is deterministic.
            _networkManager.ConnectionApprovalCallback = HandleConnectionApproval;
            _networkManager.OnClientDisconnectCallback += HandleClientDisconnected;
            _networkManager.OnServerStarted += HandleServerStarted;
        }

        private void OnDestroy()
        {
            if (_networkManager == null)
                return;

            if (_networkManager.ConnectionApprovalCallback == HandleConnectionApproval)
                _networkManager.ConnectionApprovalCallback = null;

            _networkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
            _networkManager.OnServerStarted -= HandleServerStarted;
        }

        private void HandleServerStarted()
        {
            // Fresh server lifecycle: clear stale in-memory mappings from previous sessions.
            SessionRegistry.Clear();
        }

        private void HandleConnectionApproval(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            response.Pending = false;
            response.CreatePlayerObject = true;

            // 1) Decode payload
            if (!TryDecodePayload(request.Payload, out JoinPayload payload))
            {
                Deny(response, "Invalid connection payload.");
                return;
            }

            // 2) Normalize + validate AccountId
            // NOTE: SessionAccountId.Normalize keeps IDs path-safe and consistent.
            string normalizedAccountId = SessionAccountId.Normalize(payload.accountId);
            if (string.IsNullOrWhiteSpace(normalizedAccountId))
            {
                Deny(response, "AccountId missing/invalid.");
                return;
            }

            // 3) BuildId gate (optional)
            if (!string.IsNullOrWhiteSpace(requiredClientBuildId) && payload.buildId != requiredClientBuildId)
            {
                Deny(response, "Client build mismatch.");
                return;
            }

            // 4) Duplicate login gate
            if (SessionRegistry.IsAccountActive(normalizedAccountId))
            {
                Deny(response, "Account already connected.");
                return;
            }

            // 5) Capacity gate (optional)
            if (maxPlayers > 0)
            {
                // ConnectedClientsList includes the host when running as host.
                int currentPlayers = _networkManager.ConnectedClientsList.Count;
                if (currentPlayers >= maxPlayers)
                {
                    Deny(response, "Shard full.");
                    return;
                }
            }

            // 6) Register mapping (clientId -> AccountId)
            SessionRegistry.Register(request.ClientNetworkId, normalizedAccountId);

            response.Approved = true;
            Debug.Log($"[NetworkSessionApprovalBridge] Approved client {request.ClientNetworkId} as AccountId '{normalizedAccountId}'.");
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            SessionRegistry.Remove(clientId);
        }

        private static void Deny(NetworkManager.ConnectionApprovalResponse response, string reason)
        {
            response.Approved = false;
            response.Reason = reason;
            response.CreatePlayerObject = false;
            Debug.LogWarning($"[NetworkSessionApprovalBridge] Connection denied: {reason}");
        }

        private static bool TryDecodePayload(byte[] payloadBytes, out JoinPayload payload)
        {
            payload = default;

            if (payloadBytes == null || payloadBytes.Length == 0)
                return false;

            try
            {
                string json = Encoding.UTF8.GetString(payloadBytes);
                payload = JsonUtility.FromJson<JoinPayload>(json);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // --------------------------------------------------------------------
        // Client helper
        // --------------------------------------------------------------------

        /// <summary>
        /// Builds the NGO connection payload from a session identity.
        /// Call this BEFORE StartHost/StartClient.
        /// </summary>
        public static byte[] BuildConnectionPayload(SessionIdentity identity)
        {
            var payload = new JoinPayload
            {
                accountId = identity.accountId.Value ?? string.Empty,
                displayName = identity.displayName ?? string.Empty,
                buildId = identity.buildId ?? string.Empty
            };

            string json = JsonUtility.ToJson(payload);
            return Encoding.UTF8.GetBytes(json);
        }
    }
}
