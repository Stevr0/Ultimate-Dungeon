using Unity.Netcode;
using UnityEngine;
using UltimateDungeon.Players;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// LeftClickTargetPicker_v3 (UO-style)
/// ----------------------------------
/// Correct target picking rule:
/// - Cast ONE ray and take the NEAREST hit.
/// - If nearest hit is Ground -> clear target
/// - Else if nearest hit is targetable -> set target
/// - Else -> clear
///
/// Why v2 failed sometimes:
/// - It raycasted Ground first using a ground-only mask.
///   That can clear your target even when you're clicking an object,
///   because the ray ALSO intersects the ground behind the object.
///
/// This version only considers what you actually clicked first.
///
/// UO contract:
/// - LEFT CLICK = target/inspect
/// - RIGHT CLICK = move (handled elsewhere)
/// </summary>
[DisallowMultipleComponent]
public class LeftClickTargetPicker : NetworkBehaviour
{
    [Header("Layers")]
    [Tooltip("Layer used by your walkable ground.")]
    [SerializeField] private string groundLayerName = "Ground";

    [Tooltip("Which layers count as targetable.\nTip: set this to Everything, then exclude Ground in code.")]
    [SerializeField] private LayerMask targetableMask = ~0;

    [Header("Raycast")]
    [SerializeField] private float maxRayDistance = 500f;

    [Header("Debug")]
    [SerializeField] private bool logClicks = true;

    private Camera _cam;
    private PlayerTargeting _targeting;
    private int _groundLayer;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsOwner) enabled = false;
    }

    private void Awake()
    {
        _targeting = GetComponent<PlayerTargeting>();
        if (_targeting == null)
            Debug.LogError("[LeftClickTargetPicker_v3] Missing PlayerTargeting on the same object.");
    }

    private void Start()
    {
        _cam = Camera.main;
        if (_cam == null)
            Debug.LogError("[LeftClickTargetPicker_v3] No Camera.main found. Is your camera tagged MainCamera?");

        _groundLayer = LayerMask.NameToLayer(groundLayerName);
        if (_groundLayer < 0)
            Debug.LogWarning($"[LeftClickTargetPicker_v3] Ground layer '{groundLayerName}' not found. Ground clicks won't be detected by layer name.");
    }

    private void Update()
    {
        if (!IsOwner || _cam == null || _targeting == null)
            return;

        if (!WasLeftClickPressedThisFrame())
            return;

        Vector2 mousePos = GetMouseScreenPosition();
        Ray ray = _cam.ScreenPointToRay(mousePos);

        if (!Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            if (logClicks)
                Debug.Log("[LeftClickTargetPicker_v3] Hit nothing -> clear.");

            _targeting.ClearTarget();
            return;
        }

        GameObject hitObj = hit.collider.gameObject;
        string hitLayerName = LayerMask.LayerToName(hitObj.layer);

        if (logClicks)
            Debug.Log($"[LeftClickTargetPicker_v3] Nearest hit: {hitObj.name} (layer={hitLayerName})");

        // If the nearest thing you clicked is ground -> clear.
        if (_groundLayer >= 0 && hitObj.layer == _groundLayer)
        {
            _targeting.ClearTarget();
            return;
        }

        // Otherwise, try to pick something targetable.
        // We also allow clicking child colliders: we pick the parent NetworkObject if present.
        if (((1 << hitObj.layer) & targetableMask) == 0)
        {
            if (logClicks)
                Debug.Log("[LeftClickTargetPicker_v3] Hit object is not in targetableMask -> clear.");

            _targeting.ClearTarget();
            return;
        }

        // Prefer targeting the parent NetworkObject if this collider is a child.
        NetworkObject parentNetObj = hit.collider.GetComponentInParent<NetworkObject>();
        GameObject targetGo = parentNetObj != null ? parentNetObj.gameObject : hitObj;

        _targeting.SetTarget(targetGo);
    }

    private bool WasLeftClickPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
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
