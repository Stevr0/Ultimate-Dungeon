// ============================================================================
// NetworkHudController.cs — Ultimate Dungeon
// ----------------------------------------------------------------------------
// Tiny dev HUD for starting/stopping NGO during iteration.
//
// IMPORTANT (Session Identity MVP):
// - We no longer send username/password.
// - Identity is AccountId (stable, opaque) created by IdentityBootstrapper.
// - We install AccountId into NetworkConfig.ConnectionData BEFORE StartHost/StartClient.
//
// This keeps the join/approval pipeline aligned with SESSION_AND_PERSISTENCE_MODEL.md:
// - BuildId mismatch -> reject
// - Duplicate login (same AccountId) -> reject
// - Shard full -> reject (optional)
// ============================================================================

using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UltimateDungeon.Networking;

/// <summary>
/// NetworkHudController
/// --------------------
/// Tiny "day 1" UI controller for starting/stopping NGO.
///
/// Why this exists:
/// - You need a reliable, repeatable way to start Host/Client while iterating.
/// - It reduces time wasted clicking around in the NetworkManager inspector.
///
/// IMPORTANT:
/// - This is a dev HUD. You can replace it later with a proper Bootstrap/Lobby UI.
/// - The identity / approval contract it uses is NOT temporary. Only the UI is.
/// </summary>
public class NetworkHudController : MonoBehaviour
{
    [Header("UI References (assign in Inspector)")]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private Button shutdownButton;
    [SerializeField] private TMP_Text statusText;

    [Header("Optional")]
    [Tooltip("If set, overrides the local_identity.json display name each play.")]
    [SerializeField] private string forceDisplayName = string.Empty;

    private void Awake()
    {
        // Defensive checks so setup errors are obvious.
        if (hostButton == null) Debug.LogError("[NetworkHudController] Host Button not assigned.");
        if (clientButton == null) Debug.LogError("[NetworkHudController] Client Button not assigned.");
        if (shutdownButton == null) Debug.LogError("[NetworkHudController] Shutdown Button not assigned.");
        if (statusText == null) Debug.LogError("[NetworkHudController] Status Text not assigned.");

        // Wire up button click events.
        // (We do this in code so you don't have to add OnClick entries manually.)
        if (hostButton != null) hostButton.onClick.AddListener(OnClickHost);
        if (clientButton != null) clientButton.onClick.AddListener(OnClickClient);
        if (shutdownButton != null) shutdownButton.onClick.AddListener(OnClickShutdown);
    }

    private void OnEnable()
    {
        RefreshStatus();
    }

    private void Update()
    {
        // Simple dev HUD update.
        RefreshStatus();
    }

    // ------------------------------------------------------------------------
    // Identity → NGO ConnectionData
    // ------------------------------------------------------------------------

    /// <summary>
    /// Installs the current SessionIdentity into NGO ConnectionData.
    ///
    /// MUST be called BEFORE StartHost/StartClient so the server can approve us
    /// by AccountId.
    /// </summary>
    private void InstallConnectionData(bool isHost)
    {
        // IdentityBootstrapper should exist in SCN_Bootstrap and run on Awake.
        // If it doesn't exist, we fail loudly.
        if (IdentityBootstrapper.Provider == null)
        {
            Debug.LogError(
                "[NetworkHudController] IdentityBootstrapper.Provider is null. " +
                "Ensure IdentityBootstrapper is present in SCN_Bootstrap.");
            return;
        }

        // Pull the current identity (AccountId + displayName + buildId).
        SessionIdentity identity = IdentityBootstrapper.Current;

        // Optionally force the display name for local testing.
        // This writes to local_identity.json so you'll keep it across play sessions.
        if (!string.IsNullOrWhiteSpace(forceDisplayName))
        {
            IdentityBootstrapper.Provider.SetDisplayName(forceDisplayName);
            identity.displayName = IdentityBootstrapper.Provider.GetDisplayName();
        }

        // Mark whether we are starting as host or not (debug/UI only).
        identity.isHost = isHost;

        // Build the payload used by NetworkSessionApprovalBridge.
        byte[] payload = NetworkSessionApprovalBridge.BuildConnectionPayload(identity);

        // Install it onto the NetworkManager config.
        NetworkManager.Singleton.NetworkConfig.ConnectionData = payload;
    }

    // ------------------------------------------------------------------------
    // Button handlers
    // ------------------------------------------------------------------------

    private void OnClickHost()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null)
        {
            Debug.LogError("[NetworkHudController] No NetworkManager.Singleton found. Did you add NetworkManager to the scene?");
            return;
        }

        if (nm.IsListening)
        {
            Debug.LogWarning("[NetworkHudController] Network already running. Click Shutdown first.");
            return;
        }

        // Host includes local client login payload too, so host-player identity uses
        // the same AccountId pipeline as remote clients.
        InstallConnectionData(isHost: true);

        // StartHost:
        // - Starts a server
        // - Also starts a local client
        // - Spawns the local player
        bool started = nm.StartHost();
        Debug.Log(started
            ? "[NetworkHudController] Host started."
            : "[NetworkHudController] Failed to start Host.");

        RefreshStatus();
    }

    private void OnClickClient()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null)
        {
            Debug.LogError("[NetworkHudController] No NetworkManager.Singleton found. Did you add NetworkManager to the scene?");
            return;
        }

        if (nm.IsListening)
        {
            Debug.LogWarning("[NetworkHudController] Network already running. Click Shutdown first.");
            return;
        }

        // Install the same identity payload used by approval.
        InstallConnectionData(isHost: false);

        // StartClient:
        // - Connects to the configured address/port (UnityTransport)
        // - Server will spawn this client's player (if PlayerPrefab is set)
        bool started = nm.StartClient();
        Debug.Log(started
            ? "[NetworkHudController] Client started (attempting to connect)."
            : "[NetworkHudController] Failed to start Client.");

        RefreshStatus();
    }

    private void OnClickShutdown()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null)
        {
            Debug.LogWarning("[NetworkHudController] No NetworkManager.Singleton found. Nothing to shutdown.");
            return;
        }

        nm.Shutdown();
        Debug.Log("[NetworkHudController] Network shutdown.");

        RefreshStatus();
    }

    // ------------------------------------------------------------------------
    // UI status
    // ------------------------------------------------------------------------

    private void RefreshStatus()
    {
        if (statusText == null)
            return;

        var nm = NetworkManager.Singleton;
        if (nm == null)
        {
            statusText.text = "Status: No NetworkManager in scene";
            return;
        }

        if (!nm.IsListening)
        {
            statusText.text = "Status: Offline";
            return;
        }

        if (nm.IsHost)
            statusText.text = "Status: Host (Server + Client)";
        else if (nm.IsServer)
            statusText.text = "Status: Server";
        else if (nm.IsClient)
            statusText.text = "Status: Client";
        else
            statusText.text = "Status: Unknown";
    }
}
