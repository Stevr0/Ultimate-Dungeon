
// ============================================================================
// 3) HotbarInputRouter.cs
// ============================================================================

using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace UltimateDungeon.UI.Hotbar
{
    /// <summary>
    /// HotbarInputRouter
    /// -----------------
    /// Polls keyboard keys 1..0 and activates hotbar slots.
    ///
    /// Why separate from HotbarUI?
    /// - HotbarUI should be presentation + events.
    /// - Input routing is a different concern.
    ///
    /// UI blocking:
    /// - If you have a UIInputGate (modal UI blocks gameplay), you can hook it here.
    /// - For now we expose a simple boolean hook you can set externally.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HotbarInputRouter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private HotbarUI hotbar;

        [Header("Blocking")]
        [Tooltip("If true, hotbar input is ignored.")]
        [SerializeField] private bool inputBlocked = false;

        public void SetInputBlocked(bool blocked) => inputBlocked = blocked;

        private void Awake()
        {
            if (hotbar == null)
                hotbar = FindFirstObjectByType<HotbarUI>(FindObjectsInactive.Include);

            if (hotbar == null)
                Debug.LogError("[HotbarInputRouter] No HotbarUI found in scene.");
        }

        private void Update()
        {
            if (inputBlocked)
                return;

            if (hotbar == null)
                return;

            int slotIndex = GetPressedSlotIndexThisFrame();
            if (slotIndex < 0)
                return;

            hotbar.ActivateSlot(slotIndex);
        }

        /// <summary>
        /// Returns slotIndex 0..9 when key pressed this frame, otherwise -1.
        ///
        /// Mapping:
        ///   '1' -> 0
        ///   '2' -> 1
        ///   ...
        ///   '9' -> 8
        ///   '0' -> 9
        /// </summary>
        private static int GetPressedSlotIndexThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null)
                return -1;

            if (kb.digit1Key.wasPressedThisFrame) return 0;
            if (kb.digit2Key.wasPressedThisFrame) return 1;
            if (kb.digit3Key.wasPressedThisFrame) return 2;
            if (kb.digit4Key.wasPressedThisFrame) return 3;
            if (kb.digit5Key.wasPressedThisFrame) return 4;
            if (kb.digit6Key.wasPressedThisFrame) return 5;
            if (kb.digit7Key.wasPressedThisFrame) return 6;
            if (kb.digit8Key.wasPressedThisFrame) return 7;
            if (kb.digit9Key.wasPressedThisFrame) return 8;
            if (kb.digit0Key.wasPressedThisFrame) return 9;

            return -1;
#else
            if (Input.GetKeyDown(KeyCode.Alpha1)) return 0;
            if (Input.GetKeyDown(KeyCode.Alpha2)) return 1;
            if (Input.GetKeyDown(KeyCode.Alpha3)) return 2;
            if (Input.GetKeyDown(KeyCode.Alpha4)) return 3;
            if (Input.GetKeyDown(KeyCode.Alpha5)) return 4;
            if (Input.GetKeyDown(KeyCode.Alpha6)) return 5;
            if (Input.GetKeyDown(KeyCode.Alpha7)) return 6;
            if (Input.GetKeyDown(KeyCode.Alpha8)) return 7;
            if (Input.GetKeyDown(KeyCode.Alpha9)) return 8;
            if (Input.GetKeyDown(KeyCode.Alpha0)) return 9;

            return -1;
#endif
        }
    }
}
