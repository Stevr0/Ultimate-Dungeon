// SceneRuleProvider.cs
// Ultimate Dungeon
// 
// Purpose:
// - Each loaded *gameplay* scene must include exactly ONE SceneRuleProvider.
// - The provider declares the scene's SceneRuleContext (Dungeon / HotnowVillage / MainlandHousing).
// - The server resolves this to a set of SceneRuleFlags (hard gates) and exposes them via SceneRuleRegistry.
// - Clients may read the mirrored context/flags for UI/camera hints, but server authority is the rule source.
// 
// Authoritative design docs:
// - SCENE_RULE_PROVIDER.md
// - ACTOR_MODEL.md (SceneRuleContext + flag mapping table)
// 
// Notes:
// - This file intentionally includes BOTH the provider + the tiny registry, so you can drop in a single script
//   and get the required global access pattern:
//     SceneRuleRegistry.Current.Context / SceneRuleRegistry.Current.Flags
// - Later, if you prefer separate files, you can split SceneRuleRegistry into its own .cs.

using System;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UltimateDungeon.SceneRules
{
    /// <summary>
    /// SceneRuleContext (LOCKED)
    /// -----------------------
    /// Every loaded scene must declare exactly one SceneRuleContext.
    /// 
    /// This enum is owned by ACTOR_MODEL.md.
    /// If you already have this enum elsewhere in your project, delete this definition
    /// and use the existing one (keep the names identical).
    /// </summary>
    public enum SceneRuleContext : byte
    {
        MainlandHousing = 0,
        HotnowVillage = 1,
        Dungeon = 2,
    }

    /// <summary>
    /// SceneRuleFlags (LOCKED canonical set)
    /// ------------------------------------
    /// These are hard gates. If a flag is false, the server MUST refuse intents that would violate it.
    /// 
    /// This enum is owned by ACTOR_MODEL.md.
    /// </summary>
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

    /// <summary>
    /// SceneRuleRegistry (LOCKED access pattern)
    /// ----------------------------------------
    /// All runtime systems must access scene flags through this one path.
    /// 
    /// IMPORTANT:
    /// - Server is authoritative: only the server may Set().
    /// - Clients may read the mirrored values (if provider is a scene NetworkObject).
    /// - If you don't want to network this provider yet, clients can ignore the mirror.
    /// </summary>
    public static class SceneRuleRegistry
    {
        /// <summary>
        /// The currently active scene rules (set by SceneRuleProvider on the server).
        /// </summary>
        public static SceneRuleSnapshot Current { get; private set; } = SceneRuleSnapshot.Invalid;

        /// <summary>
        /// True once the registry has been populated by a provider.
        /// </summary>
        public static bool HasCurrent => Current.IsValid;

        /// <summary>
        /// Server-only. Called by SceneRuleProvider when it becomes active.
        /// </summary>
        public static void Set(SceneRuleContext context, SceneRuleFlags flags)
        {
            Current = new SceneRuleSnapshot(context, flags);
        }

        /// <summary>
        /// Clears the registry to an invalid state.
        /// Useful during scene transitions / teardown.
        /// </summary>
        public static void Clear()
        {
            Current = SceneRuleSnapshot.Invalid;
        }
    }

    /// <summary>
    /// Small immutable value object for the active scene rule state.
    /// </summary>
    public readonly struct SceneRuleSnapshot
    {
        public readonly SceneRuleContext Context;
        public readonly SceneRuleFlags Flags;

        /// <summary>
        /// True if this snapshot represents a real, active scene rule state.
        /// </summary>
        public bool IsValid { get; }

        public SceneRuleSnapshot(SceneRuleContext context, SceneRuleFlags flags)
        {
            Context = context;
            Flags = flags;
            IsValid = true;
        }

        // Private constructor used only to create the Invalid sentinel.
        private SceneRuleSnapshot(SceneRuleContext context, SceneRuleFlags flags, bool isValid)
        {
            Context = context;
            Flags = flags;
            IsValid = isValid;
        }

        /// <summary>
        /// The "no active rules" sentinel.
        /// </summary>
        public static SceneRuleSnapshot Invalid => new SceneRuleSnapshot(default, SceneRuleFlags.None, false);
    }


    /// <summary>
    /// SceneRuleProvider (LOCKED)
    /// --------------------------
    /// Place exactly ONE of these in each gameplay scene.
    /// 
    /// Responsibilities:
    /// - Declares the SceneRuleContext via Inspector.
    /// - Resolves flags via the canonical mapping table.
    /// - On the server, registers the resolved snapshot into SceneRuleRegistry.
    /// - Optionally mirrors the context/flags to clients (for UI/camera gating).
    /// 
    /// Failure modes (LOCKED):
    /// - No provider in a scene -> fail fast.
    /// - Multiple providers -> fail fast.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SceneRuleProvider : NetworkBehaviour
    {
        [Header("Authoritative Scene Context")]
        [Tooltip("Declare the scene's rule context. Server uses this to resolve SceneRuleFlags.")]
        [SerializeField] private SceneRuleContext _context = SceneRuleContext.HotnowVillage;

        [Header("Debug (Read-only)")]
        [NonSerialized, Tooltip("Resolved flags based on the context mapping table.")]
        private SceneRuleFlags _resolvedFlags = SceneRuleFlags.None;

        // --- Client mirror (informational only) ---
        // NOTE: This requires this component to exist on a NetworkObject that is a *scene object*.
        // If you haven't set that up yet, it's still safe: the server registry is the authority.
        private readonly NetworkVariable<byte> _netContext = new(
            (byte)SceneRuleContext.HotnowVillage,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<ulong> _netFlags = new(
            (ulong)SceneRuleFlags.None,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        public SceneRuleContext Context => _context;
        public SceneRuleFlags Flags => _resolvedFlags;

        /// <summary>
        /// Convenience accessor for client/UI code.
        /// If you are running as a client and the provider is networked, this gives the mirrored values.
        /// If not networked (or not spawned yet), it falls back to the serialized/resolved values.
        /// </summary>
        public SceneRuleSnapshot GetSnapshot()
        {
            if (IsSpawned)
            {
                // NetworkVariables always exist locally; on server they match authoritative values.
                return new SceneRuleSnapshot((SceneRuleContext)_netContext.Value, (SceneRuleFlags)_netFlags.Value);
            }

            // Offline / not spawned: use local resolved values.
            return new SceneRuleSnapshot(_context, _resolvedFlags);
        }

        private void Awake()
        {
            // Resolve early for debug visibility.
            _resolvedFlags = ResolveFlags(_context);

            // We do NOT register here because Awake runs on clients too.
            // Registration happens on the server in OnNetworkSpawn.
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // Always keep local debug copy in sync.
            _resolvedFlags = ResolveFlags(_context);

            if (IsServer)
            {
                // Fail-fast validation (LOCKED): exactly one provider.
                if (!ValidateExactlyOneProviderInActiveScene(out string error))
                {
                    Debug.LogError($"[SceneRuleProvider] CRITICAL: {error}");

                    // Policy in docs: prevent gameplay start or kick players to Hotnow.
                    // We cannot automatically enforce a full session policy here without your boot flow,
                    // but we can at least avoid registering invalid rules.
                    SceneRuleRegistry.Clear();
                    return;
                }

                // Register authoritative state.
                SceneRuleRegistry.Set(_context, _resolvedFlags);

                // Mirror to clients (informational).
                _netContext.Value = (byte)_context;
                _netFlags.Value = (ulong)_resolvedFlags;

                Debug.Log($"[SceneRuleProvider] Registered SceneRuleContext='{_context}' Flags='{_resolvedFlags}'");
            }
        }

        private void OnValidate()
        {
            // Keep the inspector debug flags up-to-date.
            _resolvedFlags = ResolveFlags(_context);
        }

        /// <summary>
        /// Canonical mapping table from ACTOR_MODEL.md.
        /// 
        /// Design lock reminder:
        /// - Flags are immutable during runtime; only change by scene transition.
        /// - If a flag is not set, the server must refuse intents that violate it.
        /// </summary>
        private static SceneRuleFlags ResolveFlags(SceneRuleContext context)
        {
            switch (context)
            {
                case SceneRuleContext.MainlandHousing:
                case SceneRuleContext.HotnowVillage:
                    // SAFE scenes: all these are false.
                    return SceneRuleFlags.None;

                case SceneRuleContext.Dungeon:
                    // DANGER scene: all these are true.
                    return SceneRuleFlags.CombatAllowed
                         | SceneRuleFlags.DamageAllowed
                         | SceneRuleFlags.DeathAllowed
                         | SceneRuleFlags.DurabilityLossAllowed
                         | SceneRuleFlags.ResourceGatheringAllowed
                         | SceneRuleFlags.SkillGainAllowed
                         | SceneRuleFlags.HostileActorsAllowed
                         | SceneRuleFlags.PvPAllowed;

                default:
                    // Unknown context should be treated as critical.
                    return SceneRuleFlags.None;
            }
        }

        /// <summary>
        /// FAIL-FAST validation: the active scene must contain exactly one SceneRuleProvider.
        /// 
        /// Why here?
        /// - Docs require "fail fast" if missing/multiple providers.
        /// - Your GameBootstrapper / scene loader can also call this before enabling gameplay.
        /// </summary>
        public static bool ValidateExactlyOneProviderInActiveScene(out string error)
        {
            // NOTE:
            // - FindObjectsOfType includes disabled objects by default in editor, but at runtime it only returns active.
            // - We want active scene only (the gameplay scene), not DontDestroyOnLoad.

            Scene active = SceneManager.GetActiveScene();
            if (!active.IsValid())
            {
                error = "Active scene is not valid.";
                return false;
            }

            // Find all providers in loaded scenes, then filter to active scene.
            SceneRuleProvider[] allProviders = FindObjectsByType<SceneRuleProvider>(FindObjectsSortMode.None);
            SceneRuleProvider[] inActiveScene = allProviders
                .Where(p => p != null && p.gameObject.scene == active)
                .ToArray();

            if (inActiveScene.Length == 0)
            {
                error = $"Scene '{active.name}' has NO SceneRuleProvider. Add exactly one provider object.";
                return false;
            }

            if (inActiveScene.Length > 1)
            {
                string names = string.Join(", ", inActiveScene.Select(p => p.gameObject.name));
                error = $"Scene '{active.name}' has MULTIPLE SceneRuleProviders ({inActiveScene.Length}): {names}. Keep exactly one.";
                return false;
            }

            error = string.Empty;
            return true;
        }
    }
}
