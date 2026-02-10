using UnityEngine;
using Unity.Netcode;

/// <summary>
/// LocalCameraBinder
/// --------------------
/// Robust NGO-safe camera binding with NO reflection.
///
/// Why your previous binder was failing:
/// - Serialized MonoBehaviour references can become null after script recompiles / domain reload.
/// - Reflection-based auto-detection can pick the wrong component or fail in edge cases.
/// - NetworkManager/SpawnManager may not be ready in Start(), so early binding attempts can nullref.
///
/// This version:
/// - Requires a follow script on the SAME CameraRig that implements ICameraFollowTarget.
/// - Retries binding until the local player exists AND the follow component exists.
/// - Never throws NullReferenceException.
///
/// Setup:
/// 1) Put your follow script (IsometricCameraFollow / TopDownCameraFollow) on CameraRig.
/// 2) Make that follow script implement ICameraFollowTarget (1-line change; see note below).
/// 3) Put THIS script on CameraRig.
/// 4) Press Play (Host/Client). CameraRig will bind to the local player.
/// </summary>
[DisallowMultipleComponent]
public class LocalCameraBinder : MonoBehaviour
{
    [Header("Bind")]
    [Tooltip("Optional explicit follow component. If empty, we auto-find ICameraFollowTarget on this rig.")]
    [SerializeField] private MonoBehaviour followComponent;

    [Tooltip("How often to retry while waiting for the local player to spawn.")]
    [SerializeField] private float retryIntervalSeconds = 0.25f;

    private ICameraFollowTarget _follow;
    private bool _bound;
    private float _nextRetryTime;

    private void Awake()
    {
        ResolveFollowComponent();
    }

    private void Start()
    {
        // Try immediately, but don't assume NGO is ready in Start().
        TryBind();
    }

    private void Update()
    {
        if (_bound)
            return;

        if (Time.unscaledTime < _nextRetryTime)
            return;

        _nextRetryTime = Time.unscaledTime + Mathf.Max(0.05f, retryIntervalSeconds);

        // Follow component can become null after recompiles. Re-resolve.
        if (_follow == null)
            ResolveFollowComponent();

        TryBind();
    }

    private void ResolveFollowComponent()
    {
        _follow = null;

        // 1) If user assigned something, prefer it.
        if (followComponent != null)
        {
            _follow = followComponent as ICameraFollowTarget;
            if (_follow == null)
            {
                Debug.LogWarning($"[LocalCameraBinder_v2] Assigned followComponent '{followComponent.GetType().Name}' does not implement ICameraFollowTarget.");
            }
        }

        // 2) Auto-find any component on THIS GameObject that implements ICameraFollowTarget.
        if (_follow == null)
        {
            _follow = GetComponent<ICameraFollowTarget>();
        }

        if (_follow == null)
        {
            Debug.LogWarning("[LocalCameraBinder_v2] No follow component found. Add a follow script that implements ICameraFollowTarget to CameraRig.");
        }
    }

    private void TryBind()
    {
        if (_bound)
            return;

        if (_follow == null)
            return;

        // NGO not running yet? We'll retry.
        if (NetworkManager.Singleton == null)
            return;

        // Guard: SpawnManager can be null very early.
        var spawnManager = NetworkManager.Singleton.SpawnManager;
        if (spawnManager == null)
            return;

        NetworkObject localPlayer = spawnManager.GetLocalPlayerObject();
        if (localPlayer == null)
            return;

        _follow.SetTarget(localPlayer.transform);
        _bound = true;

        Debug.Log($"[LocalCameraBinder_v2] Bound camera to local player: {localPlayer.name}");
    }
}

/*
---------------------------------
ONE-LINE CHANGE NEEDED
---------------------------------
On your follow script, change:

public class TopDownCameraFollow : MonoBehaviour

to:

public class TopDownCameraFollow : MonoBehaviour, ICameraFollowTarget

(or IsometricCameraFollow, etc.)

No other code changes required, because your follow script already has:
public void SetTarget(Transform target)
*/
