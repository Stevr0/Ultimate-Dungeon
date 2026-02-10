using UnityEngine;
using Unity.Netcode;

/// <summary>
/// SimpleInteractable
/// ------------------
/// Drop-in interactable component that implements YOUR existing IInteractable interface.
///
/// Why you still need this even if you already have IInteractable:
/// - Unity cannot add an interface in the Inspector.
/// - You add a MonoBehaviour (like this) that implements the interface.
///
/// What this gives you (v0.1):
/// - DisplayName (for UI)
/// - InteractRange (for server validation)
/// - A simple "action" you can configure per object
///
/// Server-authoritative:
/// - ServerInteract(...) is expected to be called by your PlayerInteractor ON THE SERVER.
/// - This script is safe to put on networked doors/chests/levers/vendors.
///
/// IMPORTANT NOTE about toggling GameObjects:
/// - Disabling the same GameObject that owns the NetworkObject can despawn it.
/// - Prefer toggling a CHILD mesh or visuals instead.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkObject))]
public class SimpleInteractable : NetworkBehaviour, IInteractable
{
    public enum ActionType
    {
        None = 0,
        PrintServerLog = 1,
        ToggleChildActive = 2,
        ToggleAnimatorBool = 3
    }

    [Header("Identity")]
    [SerializeField] private string displayName = "Interactable";

    [Header("Range")]
    [Tooltip("Maximum distance required to interact (server validated).")]
    [SerializeField] private float interactRange = 2.0f;

    [Header("Action")]
    [SerializeField] private ActionType action = ActionType.PrintServerLog;

    [Tooltip("For ToggleChildActive: a child GameObject to enable/disable.")]
    [SerializeField] private GameObject childToToggle;

    [Tooltip("For ToggleAnimatorBool: Animator to drive. If null, searches children.")]
    [SerializeField] private Animator animator;

    [Tooltip("For ToggleAnimatorBool: Bool parameter name.")]
    [SerializeField] private string animatorBool = "IsOpen";

    // Server-authoritative toggle state (everyone reads).
    private readonly NetworkVariable<bool> _state = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // -------------------------
    // IInteractable implementation
    // -------------------------

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public float InteractRange => Mathf.Max(0.1f, interactRange);


    // A tiny helper so the property above is never ambiguous.
    private NetworkObject NetworkObjectReference => GetComponent<NetworkObject>();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Ensure we react to state changes on ALL peers.
        _state.OnValueChanged += OnStateChanged;

        // Apply initial visuals.
        ApplyStateToVisuals(_state.Value);
    }

    public override void OnNetworkDespawn()
    {
        _state.OnValueChanged -= OnStateChanged;
        base.OnNetworkDespawn();
    }

    /// <summary>
    /// Server-only interaction callback.
    /// Called by your PlayerInteractor AFTER ownership + range validation.
    /// </summary>
    public void ServerInteract(NetworkBehaviour interactor)
    {
        if (!IsServer)
        {
            Debug.LogWarning($"[SimpleInteractable] ServerInteract called on client for '{name}'. Ignored.");
            return;
        }

        // Optional: you can add extra server-side validation here.
        // Example: block interaction if interactor is stunned, dead, etc.

        switch (action)
        {
            case ActionType.None:
                break;

            case ActionType.PrintServerLog:
                Debug.Log($"[SimpleInteractable] '{DisplayName}' used by '{interactor.name}'.");
                break;

            case ActionType.ToggleChildActive:
            case ActionType.ToggleAnimatorBool:
                // Flip the shared state; visuals will update on everyone.
                _state.Value = !_state.Value;
                break;
        }
    }

    // -------------------------
    // Visual sync
    // -------------------------

    private void OnStateChanged(bool oldValue, bool newValue)
    {
        ApplyStateToVisuals(newValue);
    }

    private void ApplyStateToVisuals(bool state)
    {
        if (action == ActionType.ToggleChildActive)
        {
            if (childToToggle != null)
                childToToggle.SetActive(state);
        }

        if (action == ActionType.ToggleAnimatorBool)
        {
            Animator a = animator != null ? animator : GetComponentInChildren<Animator>();
            if (a != null && !string.IsNullOrWhiteSpace(animatorBool))
                a.SetBool(animatorBool, state);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Keep inspector values sane.
        interactRange = Mathf.Max(0.1f, interactRange);
    }
#endif
}
