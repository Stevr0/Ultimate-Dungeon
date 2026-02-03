using Unity.Netcode;
using UnityEngine;

/// <summary>
/// ServerClickMoveMotor
/// --------------------
/// Server-authoritative movement controller for click-to-move.
///
/// Data flow:
/// - Client (owner) clicks ground
/// - ClickToMoveInput sends destination intent to THIS component
/// - This component forwards intent to server via ServerRpc
/// - Server stores destination + stop distance
/// - Server moves CharacterController toward destination each tick
/// - NetworkTransform replicates the result to all clients (including owner)
///
/// IMPORTANT:
/// - This script does not do pathfinding yet.
/// - Movement is straight-line toward destination.
///
/// Later extensions:
/// - legality checks: stunned/rooted/dead/overweight
/// - interaction override: if clicking an interactable, stop at interact range
/// - navmesh/path following
/// </summary>
[RequireComponent(typeof(CharacterController))]
[DisallowMultipleComponent]
public class ServerClickMoveMotor : NetworkBehaviour
{
    [Header("Tuning")]
    [Tooltip("Move speed in units per second.")]
    [SerializeField] private float moveSpeed = 6f;

    [Tooltip("How fast we rotate to face the movement direction (degrees/second).")]
    [SerializeField] private float turnSpeedDegPerSec = 720f;

    [Header("Debug")]
    [SerializeField] private bool drawDebug = true;

    private CharacterController _cc;

    // Server-side movement state
    private Vector3 _destination;
    private float _stopDistance;
    private bool _hasDestination;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // This component should be enabled on all instances,
        // but only the SERVER will actually move the CharacterController.
        // Clients will just sit here and receive replicated transforms.
    }

    /// <summary>
    /// Called by ClickToMoveInput on the owning client.
    ///
    /// This is a local call that then triggers a ServerRpc.
    /// </summary>
    public void RequestMove(Vector3 destination, float stopDistance)
    {
        // Only the owner should ever be issuing move requests.
        if (!IsOwner)
            return;

        // Small sanity clamp.
        stopDistance = Mathf.Max(0.01f, stopDistance);

        RequestMoveServerRpc(destination, stopDistance);
    }

    /// <summary>
    /// ServerRpc: executes on the server.
    /// This is where we accept/deny movement intents.
    /// </summary>
    [ServerRpc]
    private void RequestMoveServerRpc(Vector3 destination, float stopDistance, ServerRpcParams rpcParams = default)
    {
        // SECURITY CHECK:
        // Ensure the sender is actually the owner of this NetworkObject.
        // Otherwise a client could try to move other players.
        ulong senderId = rpcParams.Receive.SenderClientId;
        if (senderId != OwnerClientId)
        {
            Debug.LogWarning($"[ServerClickMoveMotor] Reject move: sender {senderId} is not owner {OwnerClientId}.");
            return;
        }

        // TODO (later): legality checks
        // - dead?
        // - stunned/rooted?
        // - overweight?
        // - in dialogue/shop UI?

        _destination = destination;
        _stopDistance = Mathf.Max(0.01f, stopDistance);
        _hasDestination = true;

        if (drawDebug)
            Debug.Log($"[ServerClickMoveMotor] Move accepted. Owner={OwnerClientId} Dest={_destination}");
    }

    private void Update()
    {
        // Only the SERVER moves the player.
        if (!IsServer)
            return;

        if (!_hasDestination)
            return;

        // Compute planar (XZ) direction to destination.
        Vector3 current = transform.position;
        Vector3 toDest = _destination - current;

        // Ignore Y difference so we don't try to climb into the sky.
        toDest.y = 0f;

        float dist = toDest.magnitude;
        if (dist <= _stopDistance)
        {
            _hasDestination = false;
            return;
        }

        // Desired movement direction.
        Vector3 dir = toDest / dist; // normalized

        // Rotate to face movement direction.
        if (dir.sqrMagnitude > 0.0001f)
        {
            Quaternion desiredRot = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                desiredRot,
                turnSpeedDegPerSec * Time.deltaTime);
        }

        // Move using CharacterController.
        // NOTE: CharacterController needs gravity if you want slopes/steps to behave perfectly.
        // For now, we keep it planar. We can add gravity in the next iteration.
        Vector3 delta = dir * moveSpeed * Time.deltaTime;
        _cc.Move(delta);

        if (drawDebug)
        {
            Debug.DrawLine(current, _destination, Color.yellow);
            Debug.DrawRay(current + Vector3.up * 0.1f, dir, Color.green);
        }
    }
}
