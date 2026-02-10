using UnityEngine;
using Unity.Netcode;
using UltimateDungeon.Players;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// PlayerInteractor (Input System Compatible)
/// ----------------------------------------
/// Server-authoritative interaction request pipeline.
///
/// Your project is using the NEW Input System.
/// That means UnityEngine.Input.* calls will throw:
/// InvalidOperationException: "You are trying to read Input using UnityEngine.Input..."
///
/// This version:
/// - Uses UnityEngine.InputSystem when ENABLE_INPUT_SYSTEM is defined.
/// - Uses legacy UnityEngine.Input only if the Legacy Input Manager is enabled.
/// - Keeps UO-style separation (left click selects, double left click attacks, interact is separate).
///
/// Recommended UO defaults:
/// - listenToLeftClick = false
/// - listenToRightClick = true
/// - listenToKey = true
/// </summary>
[DisallowMultipleComponent]
public class PlayerInteractor : NetworkBehaviour
{
    [Header("Input")]
    [Tooltip("If enabled, this component will listen for LEFT CLICK and attempt to interact.\n\nFor UO-style controls this should be OFF.")]
    [SerializeField] private bool listenToLeftClick = false;

    [Tooltip("If enabled, this component will listen for RIGHT CLICK and attempt to interact.")]
    [SerializeField] private bool listenToRightClick = true;

    [Tooltip("If enabled, this component will listen for the Interact key (default E) and attempt to interact.")]
    [SerializeField] private bool listenToKey = true;

    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("Debug")]
    [Tooltip("If true, logs a warning when a target is not interactable.\nKeep this OFF during normal gameplay.")]
    [SerializeField] private bool warnIfNotInteractable = false;

    private PlayerTargeting _targeting;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        _targeting = GetComponent<PlayerTargeting>();
        if (_targeting == null)
            Debug.LogError("[PlayerInteractor] Missing PlayerTargeting.");
    }

    private void Update()
    {
        // Owner-only input.
        if (!IsOwner)
            return;

        // If you want FULL separation, set all listen flags false
        // and call TryInteractWithCurrentTarget() from a dedicated input script.

#if ENABLE_INPUT_SYSTEM
        // -------------------------
        // New Input System path
        // -------------------------
        // We use direct device polling to keep setup simple.
        // Later you can switch this to InputActions/PlayerInput for rebinding.

        if (listenToLeftClick)
        {
            // Mouse may be null on some platforms (or in headless builds).
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                TryInteractWithCurrentTarget();
        }

        if (listenToRightClick)
        {
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
                TryInteractWithCurrentTarget();
        }

        if (listenToKey)
        {
            // Map KeyCode -> InputSystem Key with a small helper.
            if (Keyboard.current != null && WasKeyPressedThisFrame(interactKey))
                TryInteractWithCurrentTarget();
        }

#elif ENABLE_LEGACY_INPUT_MANAGER
        // -------------------------
        // Legacy Input Manager path
        // -------------------------
        if (listenToLeftClick && Input.GetMouseButtonDown(0))
            TryInteractWithCurrentTarget();

        if (listenToRightClick && Input.GetMouseButtonDown(1))
            TryInteractWithCurrentTarget();

        if (listenToKey && Input.GetKeyDown(interactKey))
            TryInteractWithCurrentTarget();

#else
        // No input backend enabled.
        // This is fine if you drive interaction from another script.
#endif
    }

    /// <summary>
    /// Client-side: ask the server to interact with the currently selected target.
    ///
    /// NOTE:
    /// - This does not move the player.
    /// - If you want "walk up then interact", that is a higher-level action.
    /// </summary>
    public void TryInteractWithCurrentTarget()
    {
        if (!IsOwner)
            return;

        if (_targeting == null)
            return;

        GameObject target = _targeting.CurrentTarget;
        if (target == null)
            return;

        NetworkObject netObj = target.GetComponent<NetworkObject>();
        if (netObj == null)
            return;

        RequestInteractServerRpc(netObj.NetworkObjectId);
    }

    [ServerRpc]
    private void RequestInteractServerRpc(ulong targetNetId, ServerRpcParams rpcParams = default)
    {
        // Resolve target on server.
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetId, out var targetNetObj))
            return;

        // UO rule: combat actors are NOT interacted with by default.
        // (Attack is a separate action.)
        if (targetNetObj.GetComponent<UltimateDungeon.Combat.ICombatActor>() != null)
            return;

        var interactable = targetNetObj.GetComponent<IInteractable>();
        if (interactable == null)
        {
            if (warnIfNotInteractable)
                Debug.LogWarning($"[PlayerInteractor] Target '{targetNetObj.name}' does not implement IInteractable.");
            return;
        }

        // IMPORTANT:
        // Your IInteractable contract expects a NetworkBehaviour interactor.
        // We pass THIS PlayerInteractor instance so the target can query:
        // - Owner client
        // - PlayerCore
        // - Stats
        // etc.
        interactable.ServerInteract(this);
    }

#if ENABLE_INPUT_SYSTEM
    /// <summary>
    /// Helper: maps a limited set of KeyCodes to InputSystem key presses.
    ///
    /// We only need a tiny subset for v0.1 (E, F, Space, etc.).
    /// Expand as needed.
    /// </summary>
    private static bool WasKeyPressedThisFrame(KeyCode keyCode)
    {
        if (Keyboard.current == null)
            return false;

        // v0.1: support common keys used for interact.
        // Add more cases as your bindings expand.
        return keyCode switch
        {
            KeyCode.E => Keyboard.current.eKey.wasPressedThisFrame,
            KeyCode.F => Keyboard.current.fKey.wasPressedThisFrame,
            KeyCode.Space => Keyboard.current.spaceKey.wasPressedThisFrame,
            KeyCode.Return => Keyboard.current.enterKey.wasPressedThisFrame,
            KeyCode.Escape => Keyboard.current.escapeKey.wasPressedThisFrame,
            _ => false
        };
    }
#endif
}
