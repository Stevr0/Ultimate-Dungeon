// ============================================================================
// NetworkConnectionDataInstaller.cs — Ultimate Dungeon
// ----------------------------------------------------------------------------
// Drop this into SCN_Bootstrap on the same GameObject as NetworkManager.
//
// Goal:
// - Ensure NetworkManager.NetworkConfig.ConnectionData contains the AccountId
//   BEFORE any StartHost / StartClient call happens.
//
// This is intentionally tiny and future-proof:
// - Today: IdentityBootstrapper uses LocalIdentityProvider (GUID)
// - Later: IdentityBootstrapper swaps to SteamIdentityProvider
// - This installer does not change.
// ============================================================================

using Unity.Netcode;
using UnityEngine;

namespace UltimateDungeon.Networking
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkManager))]
    public sealed class NetworkConnectionDataInstaller : MonoBehaviour
    {
        [Header("Optional")]
        [Tooltip("If enabled, logs the outgoing connection payload JSON (dev only).")]
        [SerializeField] private bool logPayloadJson = false;

        private NetworkManager _networkManager;

        private void Awake()
        {
            _networkManager = GetComponent<NetworkManager>();

            // IdentityBootstrapper must exist in the scene and run before networking starts.
            // If it doesn't, we fail loudly so you don't get a silent 'empty payload'.
            if (IdentityBootstrapper.Provider == null)
            {
                Debug.LogError(
                    "[NetworkConnectionDataInstaller] IdentityBootstrapper.Provider is null. " +
                    "Make sure IdentityBootstrapper exists in SCN_Bootstrap and runs before networking starts.");
                return;
            }

            SessionIdentity identity = IdentityBootstrapper.Current;
            if (!identity.IsValid)
            {
                Debug.LogError("[NetworkConnectionDataInstaller] IdentityBootstrapper.Current is invalid.");
                return;
            }

            // Build + install connection payload.
            byte[] payload = NetworkSessionApprovalBridge.BuildConnectionPayload(identity);
            _networkManager.NetworkConfig.ConnectionData = payload;

            if (logPayloadJson)
            {
                // NOTE: This is for debugging only.
                // Avoid shipping this in production logs.
                string json = System.Text.Encoding.UTF8.GetString(payload);
                Debug.Log($"[NetworkConnectionDataInstaller] ConnectionData JSON: {json}");
            }
        }
    }
}
