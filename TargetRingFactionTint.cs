using UnityEngine;
using UltimateDungeon.Players;

/// <summary>
/// TargetRingFactionTint
/// --------------------
/// Tints the target ring based on the current target's FactionTag.
///
/// IMPORTANT PREFAB NOTE
/// - This script is typically placed on the *ring prefab* (the object that gets
///   instantiated by TargetIndicatorFollower).
/// - In that setup, the TargetIndicatorFollower lives on the *parent* (usually
///   the local player), because it is the system that spawns/positions the ring.
///
/// Therefore:
/// - We look up TargetIndicatorFollower using GetComponentInParent(...)
///   (NOT GetComponent on the same object).
///
/// Also:
/// - We avoid FindObjectOfType (slow + can grab the wrong ring in multiplayer).
/// - We directly reference the TargetRingPulse on THIS ring instance.
/// </summary>
[DisallowMultipleComponent]
public class TargetRingFactionTint : MonoBehaviour
{
    [Header("Colors")]
    [SerializeField] private Color friendlyColor = Color.green;
    [SerializeField] private Color neutralColor = Color.yellow;
    [SerializeField] private Color hostileColor = Color.red;

    // Parent system that owns this ring instance.
    private TargetIndicatorFollower _follower;

    // Pulse component that actually applies the material tint.
    private TargetRingPulse _ringPulse;

    // Cache last target so we only retint on change.
    private GameObject _lastTarget;

    private void Awake()
    {
        // If this script is on the ring prefab, the follower will usually be on a parent.
        _follower = GetComponentInParent<TargetIndicatorFollower>();

        // Pulse should exist on this ring instance (same object or a child).
        _ringPulse = GetComponentInChildren<TargetRingPulse>(true);

        // If pulse is missing, tinting is impossible.
        if (_ringPulse == null)
        {
            Debug.LogError("[TargetRingFactionTint] Missing TargetRingPulse on this ring prefab (self or child).", this);
            enabled = false;
            return;
        }

        // Follower is optional for tinting logic (we can still tint using PlayerTargeting),
        // but if it is missing it usually means the prefab hierarchy is wrong.
        // We warn (not error) and continue.
        if (_follower == null)
        {
            Debug.LogWarning("[TargetRingFactionTint] No TargetIndicatorFollower found in parents. " +
                             "If this ring is not parented under the follower, ensure it is instantiated as a child.", this);
        }

        // Start neutral.
        _ringPulse.SetBaseColor(neutralColor);
    }

    private void Update()
    {
        // We tint based on the LOCAL player's current target.
        // Only the local player should have an active ring system anyway.
        if (PlayerNetIdentity.Local == null)
            return;

        var targeting = PlayerNetIdentity.Local.GetComponent<PlayerTargeting>();
        if (targeting == null)
            return;

        GameObject current = targeting.CurrentTarget;

        // Only retint when target changes.
        if (current == _lastTarget)
            return;

        _lastTarget = current;

        // No target -> neutral (ring may be hidden by follower).
        if (current == null)
        {
            _ringPulse.SetBaseColor(neutralColor);
            return;
        }

        // FactionTag may be on the target or a parent.
        FactionTag tag = current.GetComponentInParent<FactionTag>();
        if (tag == null)
        {
            _ringPulse.SetBaseColor(neutralColor);
            return;
        }

        switch (tag.Value)
        {
            case FactionTag.Faction.Friendly:
                _ringPulse.SetBaseColor(friendlyColor);
                break;

            case FactionTag.Faction.Hostile:
                _ringPulse.SetBaseColor(hostileColor);
                break;

            default:
                _ringPulse.SetBaseColor(neutralColor);
                break;
        }
    }
}
