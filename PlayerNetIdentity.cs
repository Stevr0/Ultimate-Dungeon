using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// PlayerNetIdentity
/// -----------------
/// Ownership + authoritative initialization + local-player binding helper.
///
/// What this solves:
/// - In multiplayer, every client sees every player.
/// - Only the owning client should drive input/camera/UI for their player.
/// - The SERVER must initialize authoritative runtime state (skills/stats/vitals).
///
/// This component:
/// - On the SERVER: calls PlayerCore.InitializeServer() once the NetworkObject spawns
/// - On the OWNING CLIENT: fires a LocalPlayerSpawned event so camera/UI/input can bind
///
/// IMPORTANT:
/// - This does NOT move the player.
/// - This does NOT implement gameplay.
/// - It is purely networking lifecycle + binding glue.
/// </summary>
[DisallowMultipleComponent]
public class PlayerNetIdentity : NetworkBehaviour
{
    /// <summary>
    /// Fired on the owning client when their local player object is spawned.
    /// Subscribers can bind camera, UI, input, etc.
    /// </summary>
    public static event Action<PlayerNetIdentity> LocalPlayerSpawned;

    /// <summary>
    /// The most recently spawned local player identity on this client.
    /// Useful for quick access, but prefer events for binding.
    /// </summary>
    public static PlayerNetIdentity Local { get; private set; }

    // --------------------------------------------------------------------
    // References
    // --------------------------------------------------------------------

    [Header("References")]
    [Tooltip("PlayerCore on the same GameObject (or a parent). Required for server initialization.")]
    [SerializeField] private UltimateDungeon.Players.PlayerCore core;

    // Guards to ensure we only initialize once per spawn.
    private bool _serverInitialized;

    private void Reset()
    {
        // Editor convenience: auto-find PlayerCore.
        // We search on self first, then parents.
        if (core == null)
            core = GetComponent<UltimateDungeon.Players.PlayerCore>();

        if (core == null)
            core = GetComponentInParent<UltimateDungeon.Players.PlayerCore>();
    }

    public override void OnNetworkSpawn()
    {
        var actor = GetComponent<UltimateDungeon.Actors.ActorComponent>();
        Debug.Log($"[PlayerNetIdentity] ActorComponent present = {actor != null}");

        base.OnNetworkSpawn();

        // ----------------------------------------------------------------
        // SERVER: Initialize authoritative runtime state
        // ----------------------------------------------------------------
        // The host is also a server, so this runs for the host as well.
        // We do this on ALL players that spawn on the server (not just owner).
        if (IsServer && !_serverInitialized)
        {
            if (core == null)
            {
                Debug.LogError("[PlayerNetIdentity] Missing PlayerCore reference; cannot InitializeServer().");
            }
            else
            {
                core.InitializeServer();
                _serverInitialized = true;
            }
        }

        // ----------------------------------------------------------------
        // OWNING CLIENT: Bind local camera/UI/input
        // ----------------------------------------------------------------
        // IsOwner means: "this NetworkObject is owned by THIS client".
        // On a host, the host is both server + a client, so the host's own
        // player will be IsOwner=true.
        if (!IsOwner)
            return;

        // Cache a reference for convenience.
        Local = this;

        Debug.Log($"[PlayerNetIdentity] Local player spawned. OwnerClientId={OwnerClientId} NetId={NetworkObjectId}");

        // Fire the event so camera/UI systems can bind.
        LocalPlayerSpawned?.Invoke(this);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        // If the local player despawns (disconnect, scene change, etc.), clear the cache.
        if (Local == this)
            Local = null;

        _serverInitialized = false;
    }
}
