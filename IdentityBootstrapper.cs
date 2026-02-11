// ============================================================================
// IdentityBootstrapper.cs — Ultimate Dungeon
// ----------------------------------------------------------------------------
// Drop this into SCN_Bootstrap.
//
// Goal:
// - Establish a SessionIdentity BEFORE networking starts.
// - Provide a single place for the rest of the game to query identity.
//
// This script intentionally does NOT start hosting or joining.
// It only ensures identity exists and is stable.
// ============================================================================

using UnityEngine;

namespace UltimateDungeon.Networking
{
    /// <summary>
    /// IdentityBootstrapper
    /// --------------------
    /// Attach to a GameObject in SCN_Bootstrap (e.g., "BootstrapRoot").
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class IdentityBootstrapper : MonoBehaviour
    {
        // Static access is acceptable here because identity is a global singleton concept.
        // Later, if you prefer dependency injection, you can swap this out.
        public static IIdentityProvider Provider { get; private set; }
        public static SessionIdentity Current { get; private set; }

        [Header("Optional")]
        [Tooltip("If set, overrides the display name stored in local_identity.json on Play.")]
        [SerializeField] private string forceDisplayName = string.Empty;

        [Tooltip("Optional build id for gating connections. If empty, leave ungated.")]
        [SerializeField] private string buildId = "dev";

        private void Awake()
        {
            // We want this object to survive scene loads so identity is stable during
            // the app lifetime (Bootstrap -> Village -> Dungeon -> etc.).
            DontDestroyOnLoad(gameObject);

            // Step 1: Choose the identity backend.
            // MVP: LocalIdentityProvider.
            // Future: SteamIdentityProvider (drop-in replacement).
            Provider = new LocalIdentityProvider();

            // Step 2: Build the session identity struct.
            var id = Provider.GetAccountId();

            if (!string.IsNullOrWhiteSpace(forceDisplayName))
                Provider.SetDisplayName(forceDisplayName);

            Current = new SessionIdentity
            {
                accountId = id,
                displayName = Provider.GetDisplayName(),
                buildId = buildId,
                isHost = false // set later by your Host/Join UI/controller
            };

            Debug.Log($"[IdentityBootstrapper] Identity ready: {Current}");
            Debug.Log($"[IdentityBootstrapper] persistentDataPath: {Application.persistentDataPath}");
        }
    }
}
