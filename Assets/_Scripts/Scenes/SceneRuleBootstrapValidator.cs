// SceneRuleBootstrapValidator.cs
// Ultimate Dungeon
//
// Purpose (per SCENE_RULE_PROVIDER.md):
// - Scene bootstrap validation must "fail fast" if:
//   - a scene has NO SceneRuleProvider
//   - a scene has MULTIPLE SceneRuleProviders
//
// How to use:
// - Add this to a bootstrap object that exists while gameplay scenes are active.
//   Common choices:
//   - Your SCN_Bootstrap persistent root (DontDestroyOnLoad)
//   - Or a GameBootstrapper object that loads the gameplay scene
//
// Behavior:
// - On the server, whenever the active scene changes, we validate the provider count.
// - If valid, we do nothing (the provider itself will register into SceneRuleRegistry).
// - If invalid, we log a critical error and (optionally) shutdown networking to prevent play.
//
// Notes:
// - We do NOT load scenes automatically here because your scene flow may differ.
// - We keep this script "policy-light": it enforces the contract, but you decide the recovery path.

using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UltimateDungeon.SceneRules
{
    /// <summary>
    /// SceneRuleBootstrapValidator
    /// --------------------------
    /// Server-only runtime guard that enforces the "exactly one provider" rule.
    /// </summary>
    public sealed class SceneRuleBootstrapValidator : MonoBehaviour
    {
        [Header("Fail Fast Policy")]
        [Tooltip("If true, will shut down the NetworkManager when a critical scene rule configuration error is detected.")]
        [SerializeField] private bool _shutdownNetworkOnInvalid = true;

        [Tooltip("If true, clears SceneRuleRegistry when an invalid configuration is detected.")]
        [SerializeField] private bool _clearRegistryOnInvalid = true;

        private void OnEnable()
        {
            // We listen for active-scene changes. This triggers for additive loads too.
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        }

        private void Start()
        {
            // Validate once on start as well, in case we're already in a gameplay scene.
            ValidateActiveSceneNow();
        }

        private void OnActiveSceneChanged(Scene previous, Scene next)
        {
            ValidateActiveSceneNow();
        }

        private void ValidateActiveSceneNow()
        {
            // Only the server enforces hard configuration.
            // Clients may still run this component (if present), but it will no-op.
            if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
                return;

            // If there's no NetworkManager yet (e.g., in editor play without NGO),
            // we still validate, because you want fail-fast even offline.

            if (!SceneRuleProvider.ValidateExactlyOneProviderInActiveScene(out string error))
            {
                Debug.LogError($"[SceneRuleBootstrapValidator] CRITICAL: {error}");

                if (_clearRegistryOnInvalid)
                {
                    SceneRuleRegistry.Clear();
                }

                // Optional: prevent gameplay start.
                // In a real boot flow, you might instead:
                // - stop player spawning
                // - kick clients back to HotnowVillage
                // - load a safe scene
                if (_shutdownNetworkOnInvalid && NetworkManager.Singleton != null)
                {
                    Debug.LogError("[SceneRuleBootstrapValidator] Shutting down NetworkManager due to invalid SceneRuleProvider configuration.");
                    NetworkManager.Singleton.Shutdown();
                }

                return;
            }

            // If valid, do nothing here.
            // The SceneRuleProvider (in that scene) is responsible for registering the resolved snapshot.
            // This validator's job is simply to enforce the "exactly one" rule.
        }
    }
}
