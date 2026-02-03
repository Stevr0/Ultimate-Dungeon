#if ENABLE_INPUT_SYSTEM
using UnityEngine;
using UnityEngine.InputSystem; // Keyboard, Key

namespace UltimateDungeon.UI
{
    /// <summary>
    /// UIHotkeyToggler_InputSystem (SPIKE)
    /// ==================================
    ///
    /// This is the same validation helper as UIHotkeyToggler, but written for
    /// Unity's *Input System package* (NOT the legacy UnityEngine.Input API).
    ///
    /// Why this exists:
    /// - Your project is set to "Active Input Handling = Input System Package"
    ///   so calling UnityEngine.Input.GetKeyDown throws an exception.
    ///
    /// PURPOSE / SPIKE GOAL:
    /// ---------------------
    /// Temporary keyboard-only helper to validate the UI shell
    /// (windows + modal stack) WITHOUT any gameplay wiring.
    ///
    /// SUCCESS CRITERIA:
    /// -----------------
    /// - Inventory, Paperdoll, Skills, Magic can all be opened together
    /// - Clicking windows brings them to front
    /// - ESC closes modals first (if any are open)
    ///
    /// DELETE WHEN:
    /// ------------
    /// - Input is driven by a proper Input Action / UI command system
    /// - Hotkeys are rebound or driven by player settings
    ///
    /// DEPENDENCIES:
    /// -------------
    /// - UIWindowManager exists in scene (Canvas_Windows)
    /// - UIModalManager exists in scene (Canvas_Modals)
    /// - Windows have UIWindow components with ids:
    ///     "Inventory", "Paperdoll", "Skills", "Magic"
    /// </summary>
    public sealed class UIHotkeyToggler_InputSystem : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private UIWindowManager windowManager;
        [SerializeField] private UIModalManager modalManager;

        private void Awake()
        {
            // Defensive checks so failures are obvious during setup.
            if (windowManager == null)
                Debug.LogError("[UIHotkeyToggler_InputSystem] UIWindowManager reference not assigned.");

            if (modalManager == null)
                Debug.LogError("[UIHotkeyToggler_InputSystem] UIModalManager reference not assigned.");
        }

        private void Update()
        {
            if (windowManager == null || modalManager == null)
                return;

            // Keyboard.current can be null on some platforms / when no keyboard is present.
            // For desktop dev this is almost always present, but we guard anyway.
            var kb = Keyboard.current;
            if (kb == null)
                return;

            // NOTE: We use .wasPressedThisFrame to match "GetKeyDown" behavior.
            // This only fires for the frame the key transitions from up -> down.

            // ---- Window Toggles (UO-style) ----
            if (kb.iKey.wasPressedThisFrame)
                windowManager.Toggle("Inventory");

            if (kb.pKey.wasPressedThisFrame)
                windowManager.Toggle("Paperdoll");

            if (kb.kKey.wasPressedThisFrame)
                windowManager.Toggle("Skills");

            if (kb.bKey.wasPressedThisFrame)
                windowManager.Toggle("Magic");

            // ---- Modal Control ----
            if (kb.escapeKey.wasPressedThisFrame)
                modalManager.CloseTop();
        }
    }
}

#else
// If someone imports this script into a project without the Input System enabled,
// we provide a clear compiler error message instead of failing mysteriously.
#error UIHotkeyToggler_InputSystem requires ENABLE_INPUT_SYSTEM. Enable the Input System package or remove this script.
#endif
