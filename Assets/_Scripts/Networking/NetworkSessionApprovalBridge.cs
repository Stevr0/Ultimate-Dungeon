using System;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace UltimateDungeon.Networking
{
    /// <summary>
    /// Hooks NGO connection approval and turns login payload into an authoritative AccountId.
    /// Attach to the same GameObject as NetworkManager (typically in bootstrap scene).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkManager))]
    public sealed class NetworkSessionApprovalBridge : MonoBehaviour
    {
        [Serializable]
        private struct LoginPayload
        {
            public string username;
            public string password;
            public string buildId;
        }

        [Header("Optional Server Gates")]
        [Tooltip("When set, clients must send the same buildId in connection payload.")]
        [SerializeField] private string requiredClientBuildId = string.Empty;

        [Tooltip("When set, clients must send the same password in connection payload.")]
        [SerializeField] private string serverPassword = string.Empty;

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

            // We own approval callback registration so server-side login is always deterministic.
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

            if (!TryDecodePayload(request.Payload, out LoginPayload payload))
            {
                Deny(response, "Invalid connection payload.");
                return;
            }

            string normalizedAccountId = SessionAccountId.Normalize(payload.username);
            if (string.IsNullOrWhiteSpace(normalizedAccountId))
            {
                Deny(response, "Username is invalid after normalization.");
                return;
            }

            if (!string.IsNullOrWhiteSpace(requiredClientBuildId) && payload.buildId != requiredClientBuildId)
            {
                Deny(response, "Client build mismatch.");
                return;
            }

            if (!string.IsNullOrWhiteSpace(serverPassword) && payload.password != serverPassword)
            {
                Deny(response, "Server password mismatch.");
                return;
            }

            if (SessionRegistry.IsAccountActive(normalizedAccountId))
            {
                Deny(response, "Account already connected.");
                return;
            }

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

        private static bool TryDecodePayload(byte[] payloadBytes, out LoginPayload payload)
        {
            payload = default;

            if (payloadBytes == null || payloadBytes.Length == 0)
                return false;

            try
            {
                string json = Encoding.UTF8.GetString(payloadBytes);
                payload = JsonUtility.FromJson<LoginPayload>(json);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static byte[] BuildConnectionPayload(string username, string password, string buildId)
        {
            var payload = new LoginPayload
            {
                username = username ?? string.Empty,
                password = password ?? string.Empty,
                buildId = buildId ?? string.Empty
            };

            string json = JsonUtility.ToJson(payload);
            return Encoding.UTF8.GetBytes(json);
        }
    }
}
