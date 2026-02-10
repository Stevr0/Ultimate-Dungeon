// ============================================================================
// LocalPlayerUIBinder.cs (ActorVitals Only)
// ----------------------------------------------------------------------------
// Binds UI panels to the local player's replicated components.
//
// IMPORTANT:
// - HUD vitals now bind ONLY to ActorVitals.
// - This removes the "two vitals" problem.
//
// Fix:
// - Adds the correct namespace import for PlayerNetIdentity.
//
// Store at:
// Assets/_Scripts/UI/Binding/LocalPlayerUIBinder.cs
// ============================================================================

using UnityEngine;
using UltimateDungeon.Players.Networking;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace UltimateDungeon.UI.Binding
{
    [DisallowMultipleComponent]
    public sealed class LocalPlayerUIBinder : MonoBehaviour
    {
        [Header("UI Targets")]
        [SerializeField] private UltimateDungeon.UI.HUD.HudVitalsUI hudVitals;
        [SerializeField] private UltimateDungeon.UI.Panels.CharacterStatsPanelUI characterStatsPanel;

        [Header("Optional")]
        [Tooltip("Toggle the Character panel with this key.")]
        [SerializeField] private KeyCode toggleCharacterPanelKey = KeyCode.C;

        private void OnEnable()
        {
            // Subscribe so we can bind whenever the local player spawns.
            PlayerNetIdentity.LocalPlayerSpawned += HandleLocalPlayerSpawned;

            // Optional: if local already exists and UI enabled later, bind immediately.
            if (PlayerNetIdentity.Local != null)
                HandleLocalPlayerSpawned(PlayerNetIdentity.Local);
        }

        private void OnDisable()
        {
            PlayerNetIdentity.LocalPlayerSpawned -= HandleLocalPlayerSpawned;
        }

        private void Update()
        {
            if (characterStatsPanel == null)
                return;

            bool pressed = false;

#if ENABLE_INPUT_SYSTEM
            // New Input System path
            var kb = Keyboard.current;
            if (kb != null)
            {
                switch (toggleCharacterPanelKey)
                {
                    case KeyCode.C:
                        pressed = kb.cKey.wasPressedThisFrame;
                        break;
                    case KeyCode.Tab:
                        pressed = kb.tabKey.wasPressedThisFrame;
                        break;
                    case KeyCode.I:
                        pressed = kb.iKey.wasPressedThisFrame;
                        break;
                    case KeyCode.Escape:
                        pressed = kb.escapeKey.wasPressedThisFrame;
                        break;
                    default:
                        pressed = false;
                        break;
                }
            }
#else
            // Legacy Input path
            pressed = Input.GetKeyDown(toggleCharacterPanelKey);
#endif

            if (pressed)
                characterStatsPanel.gameObject.SetActive(!characterStatsPanel.gameObject.activeSelf);
        }

        private void HandleLocalPlayerSpawned(PlayerNetIdentity identity)
        {
            if (identity == null)
                return;

            // 1) Vitals (single source of truth)
            var actorVitals = identity.GetComponentInChildren<UltimateDungeon.Combat.ActorVitals>(true);

            if (hudVitals != null)
                hudVitals.Bind(actorVitals);

            // 2) Stats panel
            var statsNet = identity.GetComponentInChildren<PlayerStatsNet>(true);
            var combatStatsNet = identity.GetComponentInChildren<PlayerCombatStatsNet>(true);
            var skillBookNet = identity.GetComponentInChildren<PlayerSkillBookNet>(true);
            var playerCore = identity.GetComponentInChildren<UltimateDungeon.Players.PlayerCore>(true);

            var statusRuntime = FindInterfaceInChildren<UltimateDungeon.StatusEffects.IStatusEffectRuntime>(identity);
            var currencyWallet = FindInterfaceInChildren<UltimateDungeon.Economy.IPlayerCurrencyWallet>(identity);

            if (characterStatsPanel != null)
            {
                characterStatsPanel.Bind(
                    statsNet,
                    combatStatsNet,
                    actorVitals,
                    skillBookNet,
                    playerCore,
                    statusRuntime,
                    currencyWallet);
            }
        }

        private static T FindInterfaceInChildren<T>(Component root) where T : class
        {
            if (root == null)
                return null;

            // We scan MonoBehaviours because interfaces are implemented by components.
            var behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is T match)
                    return match;
            }

            return null;
        }

        [ContextMenu("Auto-Wire UI References")]
        private void AutoWire()
        {
            if (hudVitals == null)
                hudVitals = FindFirstObjectByType<UltimateDungeon.UI.HUD.HudVitalsUI>(FindObjectsInactive.Include);

            if (characterStatsPanel == null)
                characterStatsPanel = FindFirstObjectByType<UltimateDungeon.UI.Panels.CharacterStatsPanelUI>(FindObjectsInactive.Include);

            Debug.Log("[LocalPlayerUIBinder] AutoWire complete.");
        }
    }
}
