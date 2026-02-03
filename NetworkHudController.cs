using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// NetworkHudController
/// --------------------
/// Tiny “day 1” UI controller for starting/stopping NGO.
///
/// Why this exists:
/// - You need a reliable, repeatable way to start Host/Client while iterating.
/// - It reduces time wasted clicking around in the NetworkManager inspector.
///
/// This script is intentionally simple and temporary.
/// You can replace it later with a proper main menu.
/// </summary>
public class NetworkHudController : MonoBehaviour
{
    [Header("UI References (assign in Inspector)")]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private Button shutdownButton;
    [SerializeField] private TMP_Text statusText;

    private void Awake()
    {
        // Defensive checks so setup errors are obvious.
        if (hostButton == null) Debug.LogError("[NetworkHudController] Host Button not assigned.");
        if (clientButton == null) Debug.LogError("[NetworkHudController] Client Button not assigned.");
        if (shutdownButton == null) Debug.LogError("[NetworkHudController] Shutdown Button not assigned.");
        if (statusText == null) Debug.LogError("[NetworkHudController] Status Text not assigned.");

        // Wire up button click events.
        // (We do this in code so you don’t have to add OnClick entries manually.)
        if (hostButton != null) hostButton.onClick.AddListener(OnClickHost);
        if (clientButton != null) clientButton.onClick.AddListener(OnClickClient);
        if (shutdownButton != null) shutdownButton.onClick.AddListener(OnClickShutdown);
    }

    private void OnEnable()
    {
        // Update status immediately when UI appears.
        RefreshStatus();
    }

    private void Update()
    {
        // Keep it simple: refresh every frame.
        // This is fine for a tiny dev HUD. Later we can swap to event-based updates.
        RefreshStatus();
    }

    private void OnClickHost()
    {
        // NetworkManager.Singleton is created by the NetworkManager object in your scene.
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

        // StartClient:
        // - Connects to the configured address/port (UnityTransport)
        // - Server will spawn this client’s player (if PlayerPrefab is set)
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

        // Shutdown stops the network session (host/server/client) cleanly.
        nm.Shutdown();
        Debug.Log("[NetworkHudController] Network shutdown.");

        RefreshStatus();
    }

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

        // IsListening tells us whether NGO is currently running.
        if (!nm.IsListening)
        {
            statusText.text = "Status: Offline";
            return;
        }

        // Determine role:
        // - Host is both server + client
        // - Server is server only
        // - Client is client only
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
