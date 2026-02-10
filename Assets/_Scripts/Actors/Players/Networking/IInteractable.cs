using Unity.Netcode;
using UnityEngine;

/// <summary>
/// IInteractable
/// -------------
/// Minimal server-authoritative interaction contract.
///
/// Anything you can "use" must:
/// - have a NetworkObject
/// - define an interaction range
/// - implement ServerInteract (runs on server only)
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// Name shown in UI for this interactable.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Max distance required to interact.
    /// </summary>
    float InteractRange { get; }

    /// <summary>
    /// Network identity for RPC referencing.
    /// </summary>
    ulong NetworkObjectId { get; }

    /// <summary>
    /// Server-only interaction callback.
    /// The server calls this after validating ownership + range.
    /// </summary>
    void ServerInteract(NetworkBehaviour interactor);
}
