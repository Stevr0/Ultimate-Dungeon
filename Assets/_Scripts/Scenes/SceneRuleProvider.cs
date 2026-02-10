// ============================================================================
// SceneRuleProvider.cs — Ultimate Dungeon (Patch v1.1)
// ----------------------------------------------------------------------------
// CHANGE SUMMARY
// - Fixes the additive-loading edge case where Unity's ActiveScene can remain
//   SCN_Bootstrap, causing validation to fail when Bootstrap has no provider.
//
// WHAT CHANGED
// 1) Validation no longer checks "ActiveScene".
//    Instead, it validates the provider's OWN scene: `gameObject.scene`.
//    This is correct because:
//    - Each gameplay scene must contain exactly ONE provider.
//    - The provider knows which scene it belongs to.
//
// 2) (Optional) Kept a helper to validate the ActiveScene if you still want it
//    for external callers, but provider registration uses provider-scene.
// ============================================================================

using System;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UltimateDungeon.SceneRules
{
    public enum SceneRuleContext : byte
    {
        Bootstrap = 0,
        Mainland = 1,
        Village = 2,
        Dungeon = 3,
    }

    [Flags]
    public enum SceneRuleFlags : ulong
    {
        None = 0,

        CombatAllowed = 1UL << 0,
        DamageAllowed = 1UL << 1,
        DeathAllowed = 1UL << 2,
        DurabilityLossAllowed = 1UL << 3,
        ResourceGatheringAllowed = 1UL << 4,
        SkillGainAllowed = 1UL << 5,
        HostileActorsAllowed = 1UL << 6,
        PvPAllowed = 1UL << 7,
    }

    public static class SceneRuleRegistry
    {
        public static SceneRuleSnapshot Current { get; private set; } = SceneRuleSnapshot.Invalid;
        public static bool HasCurrent => Current.IsValid;

        public static void Set(SceneRuleContext context, SceneRuleFlags flags)
        {
            Current = new SceneRuleSnapshot(context, flags);
        }

        public static void Clear()
        {
            Current = SceneRuleSnapshot.Invalid;
        }
    }

    public readonly struct SceneRuleSnapshot
    {
        public readonly SceneRuleContext Context;
        public readonly SceneRuleFlags Flags;
        public bool IsValid { get; }

        public SceneRuleSnapshot(SceneRuleContext context, SceneRuleFlags flags)
        {
            Context = context;
            Flags = flags;
            IsValid = true;
        }

        private SceneRuleSnapshot(SceneRuleContext context, SceneRuleFlags flags, bool isValid)
        {
            Context = context;
            Flags = flags;
            IsValid = isValid;
        }

        public static SceneRuleSnapshot Invalid => new SceneRuleSnapshot(default, SceneRuleFlags.None, false);
    }

    [DisallowMultipleComponent]
    public sealed class SceneRuleProvider : NetworkBehaviour
    {
        [Header("Authoritative Scene Context")]
        [SerializeField] private SceneRuleContext _context = SceneRuleContext.Village;

        [Header("Debug (Read-only)")]
        [NonSerialized] private SceneRuleFlags _resolvedFlags = SceneRuleFlags.None;

        private readonly NetworkVariable<byte> _netContext = new(
            (byte)SceneRuleContext.Village,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<ulong> _netFlags = new(
            (ulong)SceneRuleFlags.None,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        public SceneRuleContext Context => _context;
        public SceneRuleFlags Flags => _resolvedFlags;

        public SceneRuleSnapshot GetSnapshot()
        {
            if (IsSpawned)
                return new SceneRuleSnapshot((SceneRuleContext)_netContext.Value, (SceneRuleFlags)_netFlags.Value);

            return new SceneRuleSnapshot(_context, _resolvedFlags);
        }

        private void Awake()
        {
            _resolvedFlags = ResolveFlags(_context);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _resolvedFlags = ResolveFlags(_context);

            if (!IsServer)
                return;

            // ----------------------------------------------------------------
            // IMPORTANT FIX:
            // Validate the provider's OWN scene (gameObject.scene), not ActiveScene.
            // ActiveScene can remain SCN_Bootstrap in additive loading.
            // ----------------------------------------------------------------
            if (!ValidateExactlyOneProviderInScene(gameObject.scene, out string error))
            {
                Debug.LogError($"[SceneRuleProvider] CRITICAL: {error}");
                SceneRuleRegistry.Clear();
                return;
            }

            SceneRuleRegistry.Set(_context, _resolvedFlags);

            _netContext.Value = (byte)_context;
            _netFlags.Value = (ulong)_resolvedFlags;

            Debug.Log($"[SceneRuleProvider] Registered SceneRuleContext='{_context}' Flags='{_resolvedFlags}'");
        }

        private void OnValidate()
        {
            _resolvedFlags = ResolveFlags(_context);
        }

        private static SceneRuleFlags ResolveFlags(SceneRuleContext context)
        {
            switch (context)
            {
                case SceneRuleContext.Mainland:
                case SceneRuleContext.Village:
                case SceneRuleContext.Bootstrap:
                    return SceneRuleFlags.None;

                case SceneRuleContext.Dungeon:
                    return SceneRuleFlags.CombatAllowed
                         | SceneRuleFlags.DamageAllowed
                         | SceneRuleFlags.DeathAllowed
                         | SceneRuleFlags.DurabilityLossAllowed
                         | SceneRuleFlags.ResourceGatheringAllowed
                         | SceneRuleFlags.SkillGainAllowed
                         | SceneRuleFlags.HostileActorsAllowed
                         | SceneRuleFlags.PvPAllowed;

                default:
                    return SceneRuleFlags.None;
            }
        }

        // ====================================================================
        // Validation
        // ====================================================================

        /// <summary>
        /// Validates that the specified scene contains exactly one SceneRuleProvider.
        /// This is the correct invariant for additive loading.
        /// </summary>
        public static bool ValidateExactlyOneProviderInScene(Scene scene, out string error)
        {
            if (!scene.IsValid())
            {
                error = "Scene is not valid.";
                return false;
            }

            // Find all providers in loaded scenes, then filter to the specified scene.
            var allProviders = FindObjectsByType<SceneRuleProvider>(FindObjectsSortMode.None);
            var inScene = allProviders
                .Where(p => p != null && p.gameObject.scene == scene)
                .ToArray();

            if (inScene.Length == 0)
            {
                error = $"Scene '{scene.name}' has NO SceneRuleProvider. Add exactly one provider object.";
                return false;
            }

            if (inScene.Length > 1)
            {
                string names = string.Join(", ", inScene.Select(p => p.gameObject.name));
                error = $"Scene '{scene.name}' has MULTIPLE SceneRuleProviders ({inScene.Length}): {names}. Keep exactly one.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        /// <summary>
        /// Optional legacy helper: validates the current ActiveScene.
        /// Keep only if other boot code uses it.
        /// </summary>
        public static bool ValidateExactlyOneProviderInActiveScene(out string error)
        {
            return ValidateExactlyOneProviderInScene(SceneManager.GetActiveScene(), out error);
        }
    }
}
