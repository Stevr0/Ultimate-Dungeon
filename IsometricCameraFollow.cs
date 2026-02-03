using UnityEngine;

/// <summary>
/// IsometricCameraFollow
/// ---------------------
/// Fixed-angle, non-rotating camera follow for isometric / top-down gameplay.
///
/// DESIGN LOCKS:
/// - Camera angle NEVER changes
/// - Camera does NOT look-at the target
/// - Camera ONLY follows position
/// - Rotation is authored, not computed
/// </summary>
[DisallowMultipleComponent]
public class IsometricCameraFollow : MonoBehaviour, ICameraFollowTarget

{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Offset")]
    [Tooltip("World-space offset from the target.")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 12f, -12f);

    [Header("Follow")]
    [Tooltip("Higher = snappier follow. Lower = smoother follow.")]
    [SerializeField] private float followLerp = 10f;

    [Header("Fixed Rotation")]
    [Tooltip("Author the isometric angle here. This rotation NEVER changes at runtime.")]
    [SerializeField] private Vector3 fixedEulerRotation = new Vector3(45f, 45f, 0f);

    private void Awake()
    {
        // Hard-lock rotation at startup
        transform.rotation = Quaternion.Euler(fixedEulerRotation);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;

        if (target != null)
        {
            transform.position = target.position + offset;
        }
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desiredPos = target.position + offset;
        transform.position = Vector3.Lerp(
            transform.position,
            desiredPos,
            followLerp * Time.deltaTime
        );
    }
}
