using UnityEngine;
using UltimateDungeon.Players;
using UltimateDungeon.Players.Networking;

/// <summary>
/// TargetIndicatorFollower
/// -----------------------
/// Spawns a "target ring" prefab and keeps it positioned under the
/// local player's current target.
///
/// Local-only:
/// - Every client can have their own ring (no networking)
///
/// Setup:
/// - Create a prefab ring (flat cylinder) and assign it
/// - Place this script on a scene object (e.g., TargetIndicatorSystem)
///
/// Notes:
/// - We follow the target's XZ.
/// - We place the ring at a small Y offset to avoid z-fighting.
/// - We DON'T parent the ring to the target because targets may be networked
///   and reparented; following is simpler and safer.
/// </summary>
public class TargetIndicatorFollower : MonoBehaviour
{
    [Header("Prefab")]
    [Tooltip("Prefab for the ring indicator (e.g., a flat cylinder).")]
    [SerializeField] private GameObject ringPrefab;

    [Header("Placement")]
    [Tooltip("Height above the target's pivot to place the ring. Usually 0.02 - 0.05.")]
    [SerializeField] private float yOffset = 0.03f;

    [Tooltip("If true, ring rotates to match target rotation (usually false for top-down).")]
    [SerializeField] private bool matchTargetYaw = false;

    [Header("Debug")]
    [SerializeField] private bool logBind = false;

    private PlayerTargeting _targeting;
    private GameObject _ringInstance;

    private void Start()
    {
        if (ringPrefab == null)
        {
            Debug.LogError("[TargetIndicatorFollower] Missing ringPrefab.");
            return;
        }

        // ✅ IMPORTANT:
        // Instantiate as a CHILD of this follower, so the ring can find:
        // - TargetIndicatorFollower in parents
        // - PlayerTargeting in parents (on the player)
        // - PlayerCombatController in parents (on the player)
        _ringInstance = Instantiate(ringPrefab, transform);

        // Optional: reset local transform so it's nicely aligned under the follower
        _ringInstance.transform.localPosition = Vector3.zero;
        _ringInstance.transform.localRotation = Quaternion.identity;
        _ringInstance.transform.localScale = Vector3.one;
    }

    private void Update()
    {
        if (_targeting == null)
        {
            TryBind();
            return;
        }

        if (_ringInstance == null)
            return;

        GameObject target = _targeting.CurrentTarget;
        if (target == null)
        {
            // No target -> hide.
            if (_ringInstance.activeSelf)
                _ringInstance.SetActive(false);
            return;
        }

        // Have a target -> show and follow.
        if (!_ringInstance.activeSelf)
            _ringInstance.SetActive(true);

        // Place the ring at the bottom of the target, not at its pivot.
        // Different objects have different pivot heights (player vs cube),
        // so we derive the Y from render bounds when possible.
        float y = target.transform.position.y;

        // Prefer Renderer bounds (most reliable for visuals)
        Renderer r = target.GetComponentInChildren<Renderer>();
        if (r != null)
        {
            y = r.bounds.min.y + yOffset;
        }
        else
        {
            // Fallback: use collider bounds if no renderer exists
            Collider c = target.GetComponentInChildren<Collider>();
            if (c != null)
                y = c.bounds.min.y + yOffset;
            else
                y = target.transform.position.y + yOffset;
        }

        Vector3 p = target.transform.position;
        p.y = y;
        _ringInstance.transform.position = p;

        if (matchTargetYaw)
        {
            Vector3 e = _ringInstance.transform.eulerAngles;
            e.y = target.transform.eulerAngles.y;
            _ringInstance.transform.eulerAngles = e;
        }
    }

    private void TryBind()
    {
        if (PlayerNetIdentity.Local == null)
            return;

        _targeting = PlayerNetIdentity.Local.GetComponent<PlayerTargeting>();
        if (_targeting != null && logBind)
            Debug.Log("[TargetIndicatorFollower] Bound to local PlayerTargeting.");
    }

    private void OnDestroy()
    {
        if (_ringInstance != null)
            Destroy(_ringInstance);
    }
}
