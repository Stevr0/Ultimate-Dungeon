using UnityEngine;
using UnityEngine.EventSystems;
using Unity.Netcode;
using UltimateDungeon.Players;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace UltimateDungeon.Combat
{
    /// <summary>
    /// DoubleClickAttackInput
    /// ----------------------
    /// Input System compatible "double left click to attack" trigger.
    ///
    /// UO-style behavior we want:
    /// 1) Single left click selects (handled by LeftClickTargetPicker).
    /// 2) Double left click issues an ATTACK INTENT on the currently selected target.
    /// 3) Once attack intent is set, combat systems handle:
    ///    - moving into range (player-driven or auto-follow later)
    ///    - auto-swing when in range
    ///
    /// IMPORTANT:
    /// - This script only triggers the attack request.
    /// - It does NOT do movement.
    /// - It does NOT clear target.
    ///
    /// Why your double click currently does nothing:
    /// - Your project uses the NEW Input System, so any legacy Input code will not run.
    /// - Or you don't currently have a dedicated script that detects double clicks.
    ///
    /// Attach to:
    /// - Player prefab (local owner object)
    ///
    /// Requires:
    /// - PlayerTargeting (for CurrentTarget)
    /// - PlayerCombatController (to request attack)
    /// </summary>
    [DisallowMultipleComponent]
    public class DoubleClickAttackInput : NetworkBehaviour
    {
        [Header("Double Click Settings")]
        [Tooltip("Max time (seconds) between clicks to count as a double click.")]
        [SerializeField] private float doubleClickTime = 0.30f;

        [Tooltip("Max cursor movement (pixels) allowed between clicks.")]
        [SerializeField] private float maxMouseMovePixels = 18f;

        [Tooltip("If true, double click only triggers attack when the second click is on the SAME target under the cursor.")]
        [SerializeField] private bool requireSecondClickOnTarget = true;

        [Header("Raycast")]
        [Tooltip("Which layers are considered 'clickable targets'. If empty, everything is considered.")]
        [SerializeField] private LayerMask targetLayers = ~0;

        [Tooltip("Max raycast distance for clicking a target.")]
        [SerializeField] private float maxRayDistance = 200f;

        [Header("Debug")]
        [SerializeField] private bool debugLogs = false;

        private Camera _cam;
        private PlayerTargeting _targeting;
        private PlayerCombatController _combat;

        // Double click tracking.
        private float _lastClickTime = -999f;
        private Vector2 _lastClickPos;
        private ulong _lastClickedNetId = 0;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _targeting = GetComponent<PlayerTargeting>();
            _combat = GetComponent<PlayerCombatController>();

            if (_targeting == null)
                Debug.LogError("[DoubleClickAttackInput] Missing PlayerTargeting.");

            if (_combat == null)
                Debug.LogError("[DoubleClickAttackInput] Missing PlayerCombatController.");

            // Main camera is fine for this project (top-down).
            // If you later have per-player cameras, inject/bind that instead.
            _cam = Camera.main;
            if (_cam == null)
                Debug.LogWarning("[DoubleClickAttackInput] No Camera.main found. Double click raycasts will fail.");
        }

        private void Update()
        {
            // Local owner only.
            if (!IsOwner)
                return;

            // Don't trigger attacks when clicking UI.
            // (Prevents accidental attacks when using inventory/hotbar later.)
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current == null)
                return;

            // We only care about LEFT click.
            if (!Mouse.current.leftButton.wasPressedThisFrame)
                return;

            Vector2 clickPos = Mouse.current.position.ReadValue();
            float now = Time.unscaledTime;

            // Identify what was clicked (if anything).
            ulong clickedNetId = 0;
            GameObject clickedGo = RaycastClickedObject(clickPos, out clickedNetId);

            bool withinTime = (now - _lastClickTime) <= doubleClickTime;
            bool withinMove = Vector2.Distance(clickPos, _lastClickPos) <= maxMouseMovePixels;

            // If we require the second click to be on the same target:
            // - if click didn't hit any NetworkObject, we treat as not valid for combat.
            bool sameObject = (!requireSecondClickOnTarget) || (clickedNetId != 0 && clickedNetId == _lastClickedNetId);

            if (withinTime && withinMove && sameObject)
            {
                // Double click detected.
                if (debugLogs)
                {
                    string hitName = clickedGo != null ? clickedGo.name : "<none>";
                    Debug.Log($"[DoubleClickAttackInput] DoubleClick detected. Hit='{hitName}' netId={clickedNetId}.");
                }

                TryRequestAttack(clickedGo, clickedNetId);

                // Reset so triple-click doesn't spam.
                _lastClickTime = -999f;
                _lastClickedNetId = 0;
                return;
            }

            // Not a double click yet: store click.
            _lastClickTime = now;
            _lastClickPos = clickPos;
            _lastClickedNetId = clickedNetId;

            if (debugLogs)
            {
                string hitName = clickedGo != null ? clickedGo.name : "<none>";
                Debug.Log($"[DoubleClickAttackInput] Click stored. Hit='{hitName}' netId={clickedNetId}.");
            }

#else
            // Project uses the NEW Input System.
            // If you ever enable Legacy Input Manager, you could add a fallback here.
#endif
        }

        /// <summary>
        /// Attempts to start combat using the selected target.
        /// We support two modes:
        /// - If requireSecondClickOnTarget is true and we hit something clickable,
        ///   we trust that click.
        /// - Otherwise we fall back to PlayerTargeting.CurrentTarget.
        /// </summary>
        private void TryRequestAttack(GameObject clickedGo, ulong clickedNetId)
        {
            if (_combat == null || _targeting == null)
                return;

            // Primary: use clicked object if valid.
            if (requireSecondClickOnTarget && clickedGo != null)
            {
                // If the clicked object is NOT the currently selected target,
                // we can still allow attack, but that becomes a design choice.
                // For UO feel, we typically want: double click the thing to attack it.
                // We'll set it as target (client-side) then request attack.
                if (_targeting.CurrentTarget != clickedGo)
                {
                    _targeting.SetTarget(clickedGo);
                }

                _combat.RequestStartAttack();
                return;
            }

            // Fallback: attack current target.
            if (_targeting.CurrentTarget == null)
                return;

            _combat.RequestStartAttack();
        }

        /// <summary>
        /// Raycasts from screen position and returns the clicked GameObject.
        /// Also returns the NetworkObjectId if the hit object (or its parent) has a NetworkObject.
        ///
        /// We search up the hierarchy because many targets have colliders on child meshes.
        /// </summary>
        private GameObject RaycastClickedObject(Vector2 screenPos, out ulong netId)
        {
            netId = 0;

            if (_cam == null)
                return null;

            Ray ray = _cam.ScreenPointToRay(screenPos);
            if (!Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, targetLayers, QueryTriggerInteraction.Ignore))
                return null;

            GameObject go = hit.collider != null ? hit.collider.gameObject : null;
            if (go == null)
                return null;

            // Find NetworkObject on self or parents.
            NetworkObject no = go.GetComponentInParent<NetworkObject>();
            if (no != null)
                netId = no.NetworkObjectId;

            return no != null ? no.gameObject : go;
        }
    }
}
