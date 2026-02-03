using Unity.Netcode;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// ClickToMoveInput
/// -------------------
/// Owner-only input collector for Ultima Online-style movement.
///
/// FIX2:
/// - Right-click movement does NOT cancel combat engagement.
///
/// Why:
/// - In UO-style combat, you must be able to chase while still engaged.
/// - Swings are gated by range; the player keeps steering with right click.
///
/// Cancellation of engagement should be an explicit action (e.g., ESC, UI button,
/// or a dedicated "stop attack" input), not normal movement.
/// </summary>
[DisallowMultipleComponent]
public class ClickToMoveInput : NetworkBehaviour
{
    [Header("Raycast")]
    [Tooltip("Which layers count as walkable ground for click-to-move.")]
    [SerializeField] private LayerMask groundMask;

    [Tooltip("Max distance for the click raycast.")]
    [SerializeField] private float maxRayDistance = 500f;

    [Header("Move")]
    [Tooltip("How close we must get to the destination before stopping.")]
    [SerializeField] private float stopDistance = 0.2f;

    [Tooltip("How often we resend destination while holding right click (seconds). Lower = more responsive, more traffic.")]
    [SerializeField] private float holdUpdateInterval = 0.05f;

    private Camera _cam;
    private ServerClickMoveMotor _motor;

    private bool _isHoldingRight;
    private float _nextHoldSendTime;

    private void Awake()
    {
        _motor = GetComponent<ServerClickMoveMotor>();

        if (_motor == null)
            Debug.LogError("[ClickToMoveInput_UO] Missing ServerClickMoveMotor on the same GameObject.");
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsOwner)
            enabled = false;
    }

    private void Start()
    {
        _cam = Camera.main;
        if (_cam == null)
            Debug.LogError("[ClickToMoveInput_UO] No Camera.main found. Is your camera tagged MainCamera?");
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        if (_cam == null || _motor == null)
            return;

        if (WasRightClickPressedThisFrame())
        {
            _isHoldingRight = true;
            _nextHoldSendTime = 0f;
            SendMoveIntentUnderCursor();
        }

        if (WasRightClickReleasedThisFrame())
        {
            _isHoldingRight = false;
        }

        if (_isHoldingRight && Time.time >= _nextHoldSendTime)
        {
            _nextHoldSendTime = Time.time + Mathf.Max(0.01f, holdUpdateInterval);
            SendMoveIntentUnderCursor();
        }
    }

    private void SendMoveIntentUnderCursor()
    {
        Vector2 mousePos = GetMouseScreenPosition();
        Ray ray = _cam.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            Vector3 destination = hit.point;
            _motor.RequestMove(destination, stopDistance);
        }
    }

    private bool WasRightClickPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(1);
#endif
    }

    private bool WasRightClickReleasedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.rightButton.wasReleasedThisFrame;
#else
        return Input.GetMouseButtonUp(1);
#endif
    }

    private Vector2 GetMouseScreenPosition()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
        return Input.mousePosition;
#endif
    }
}
