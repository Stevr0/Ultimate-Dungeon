// ============================================================================
// SceneTransitionService.cs — Ultimate Dungeon (Authoritative Scene Loader)
// ----------------------------------------------------------------------------
// GOAL
// - Make the "SceneTransitionService" component show up in Unity's Add Component
//   menu, and reliably run on the SERVER to load the initial gameplay scene.
//
// WHY A SEPARATE FILE
// - Unity lists attachable components by scanning compiled MonoBehaviours /
//   NetworkBehaviours.
// - If you had multiple classes in one file and Unity only showed the shim
//   (SceneTransitionSystem), it usually means the service class did not compile
//   or Unity isn't actually using the file you think it is.
// - Putting the service in its own file with a matching class name eliminates
//   ambiguity and makes the inspector workflow predictable.
//
// REQUIRED SETUP (SCN_Bootstrap)
// 1) Create a GameObject named "SceneTransitionService".
// 2) Add this component (SceneTransitionService).
// 3) Add a NetworkObject component to the SAME GameObject.
//    - IMPORTANT: It must be a Scene Object (Spawn With Scene).
// 4) Start Play Mode, click HOST.
//    - The server will load InitialGameplaySceneName additively.
//
// REQUIRED SETUP (Gameplay scenes)
// - Add at least one SpawnPoint with Tag "Default".
// - Add exactly one SceneRuleProvider (owned by your SceneRules system).
//
// NOTE
// - This script does NOT attempt to decide legality; it only loads scenes and
//   teleports players.
// - "RequireOutOfCombat" should be checked using your player combat state
//   component (TODO hook below). We deliberately do not gate on scene flags.
// ============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UltimateDungeon.Scenes
{
    /// <summary>
    /// SceneTransitionService
    /// ----------------------
    /// Server-authoritative additive scene loader/unloader.
    ///
    /// IMPORTANT:
    /// - This must be on a spawned NetworkObject, or OnNetworkSpawn will never fire.
    /// - In practice: add NetworkObject + mark it as a Scene Object in SCN_Bootstrap.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    public sealed class SceneTransitionService : NetworkBehaviour
    {
        public static SceneTransitionService Instance { get; private set; }

        [Header("Bootstrap")]
        [Tooltip("Name of the bootstrap scene (kept loaded).")]
        public string BootstrapSceneName = "SCN_Bootstrap";

        [Header("Initial Gameplay Scene")]
        [Tooltip("If true, the server loads the initial gameplay scene when hosting.")]
        public bool AutoLoadInitialGameplayScene = true;

        [Tooltip("Gameplay scene players should start in.")]
        public string InitialGameplaySceneName = "SCN_Village";

        [Tooltip("SpawnPoint.Tag used when players enter the initial scene.")]
        public string InitialSpawnTag = "Default";

        [Header("Runtime (Read-only)")]
        [SerializeField] private string currentGameplayScene;

        private bool waitingForLoad;
        private bool waitingForUnload;
        private string pendingSceneName;

        private void Awake()
        {
            // Singleton pattern so portals can find us easily.
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // We ONLY load scenes on the server.
            if (!IsServer)
                return;

            // Subscribe to NGO events.
            NetworkManager.OnClientConnectedCallback += OnClientConnected;

            var sm = NetworkManager.SceneManager;
            if (sm == null)
            {
                Debug.LogError("[SceneTransitionService] NetworkManager.SceneManager is null. NGO Scene Management may be disabled.");
                return;
            }

            sm.OnLoadEventCompleted += OnLoadEventCompleted;
            sm.OnUnloadEventCompleted += OnUnloadEventCompleted;

            // If we already have a gameplay scene loaded (rare), respect it.
            currentGameplayScene = FindCurrentGameplaySceneName();

            // Normal bootstrap flow: no gameplay scene yet.
            if (AutoLoadInitialGameplayScene && string.IsNullOrWhiteSpace(currentGameplayScene))
            {
                if (string.IsNullOrWhiteSpace(InitialGameplaySceneName))
                {
                    Debug.LogError("[SceneTransitionService] InitialGameplaySceneName is empty. Cannot load start scene.");
                    return;
                }

                Debug.Log($"[SceneTransitionService] Host started. Loading initial gameplay scene '{InitialGameplaySceneName}'...");
                StartCoroutine(ServerLoadGameplayScene(InitialGameplaySceneName, teleportAllPlayersOnComplete: true));
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer && NetworkManager != null)
            {
                NetworkManager.OnClientConnectedCallback -= OnClientConnected;

                if (NetworkManager.SceneManager != null)
                {
                    NetworkManager.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
                    NetworkManager.SceneManager.OnUnloadEventCompleted -= OnUnloadEventCompleted;
                }
            }

            base.OnNetworkDespawn();
        }

        // --------------------------------------------------------------------
        // Public entry point used by portals (server rpc).
        // --------------------------------------------------------------------

        /// <summary>
        /// Client asks the server to transition them to another gameplay scene.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RequestSceneTransitionServerRpc(
            ulong requestingClientId,
            string destinationSceneName,
            string destinationSpawnTag,
            bool requireOutOfCombat)
        {
            if (!IsServer) return;
            if (waitingForLoad || waitingForUnload) return;
            if (string.IsNullOrWhiteSpace(destinationSceneName)) return;

            if (!NetworkManager.ConnectedClients.TryGetValue(requestingClientId, out var client)) return;
            var playerObj = client.PlayerObject;
            if (!playerObj) return;

            // TODO: Out-of-combat is a PLAYER STATE check.
            // If you want it now, wire to your real combat component:
            // - e.g. var tracker = playerObj.GetComponentInChildren<CombatStateTracker>();
            // - if (requireOutOfCombat && tracker != null && tracker.IsInCombat) return;

            StartCoroutine(ServerTransitionRoutine(requestingClientId, destinationSceneName, destinationSpawnTag));
        }

        private IEnumerator ServerTransitionRoutine(ulong clientId, string destScene, string spawnTag)
        {
            // 1) Clear illegal state BEFORE leaving the scene (best-effort).
            ClearIllegalState(clientId);

            // 2) Unload current gameplay scene (if different).
            if (!string.IsNullOrWhiteSpace(currentGameplayScene) && currentGameplayScene != destScene)
                yield return ServerUnloadGameplayScene(currentGameplayScene);

            // 3) Load destination scene.
            if (currentGameplayScene != destScene)
                yield return ServerLoadGameplayScene(destScene, teleportAllPlayersOnComplete: false);

            // 4) Teleport requesting player.
            TeleportPlayerToSpawn(clientId, spawnTag);
        }

        // --------------------------------------------------------------------
        // Load / Unload (server)
        // --------------------------------------------------------------------

        private IEnumerator ServerLoadGameplayScene(string sceneName, bool teleportAllPlayersOnComplete)
        {
            waitingForLoad = true;
            pendingSceneName = sceneName;
            currentGameplayScene = sceneName;

            NetworkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);

            // Wait for NGO to report scene load completion.
            while (waitingForLoad) yield return null;

            if (teleportAllPlayersOnComplete)
                TeleportAllPlayersToSpawn(InitialSpawnTag);
        }

        private IEnumerator ServerUnloadGameplayScene(string sceneName)
        {
            waitingForUnload = true;
            pendingSceneName = sceneName;

            var unityScene = SceneManager.GetSceneByName(sceneName);
            if (unityScene.IsValid() && unityScene.isLoaded)
                NetworkManager.SceneManager.UnloadScene(unityScene);

            while (waitingForUnload) yield return null;
        }

        private void OnLoadEventCompleted(string sceneName, LoadSceneMode mode, List<ulong> _, List<ulong> __)
        {
            if (!IsServer) return;
            if (sceneName == pendingSceneName)
                waitingForUnload = false;
        }

        private void OnUnloadEventCompleted(string sceneName, LoadSceneMode mode, List<ulong> _, List<ulong> __)
        {
            if (!IsServer) return;
            if (sceneName == pendingSceneName)
                waitingForUnload = false;
        }

        // --------------------------------------------------------------------
        // Teleport
        // --------------------------------------------------------------------

        private void TeleportAllPlayersToSpawn(string spawnTag)
        {
            foreach (var kvp in NetworkManager.ConnectedClients)
                TeleportPlayerToSpawn(kvp.Key, spawnTag);
        }

        private void TeleportPlayerToSpawn(ulong clientId, string spawnTag)
        {
            if (!NetworkManager.ConnectedClients.TryGetValue(clientId, out var client)) return;
            var playerObj = client.PlayerObject;
            if (!playerObj) return;

            var scene = SceneManager.GetSceneByName(currentGameplayScene);

            // NOTE: SpawnPoint is a simple MonoBehaviour in your project.
            var sp = FindSpawnPoint(scene, spawnTag) ?? FindSpawnPoint(scene, "Default");

            var pos = sp ? sp.transform.position : Vector3.zero;
            var rot = sp ? sp.transform.rotation : Quaternion.identity;

            playerObj.transform.SetPositionAndRotation(pos, rot);
        }

        private static SpawnPoint FindSpawnPoint(Scene scene, string tag)
        {
            if (!scene.isLoaded) return null;

            foreach (var root in scene.GetRootGameObjects())
            {
                var sps = root.GetComponentsInChildren<SpawnPoint>(true);
                for (int i = 0; i < sps.Length; i++)
                {
                    if (string.Equals(sps[i].Tag, tag, StringComparison.OrdinalIgnoreCase))
                        return sps[i];
                }
            }

            return null;
        }

        // --------------------------------------------------------------------
        // Clears required by SCENE_RULE_PROVIDER.md (best-effort)
        // --------------------------------------------------------------------

        private static void ClearIllegalState(ulong clientId)
        {
            if (!NetworkManager.Singleton) return;
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return;

            var player = client.PlayerObject;
            if (!player) return;

            // These calls are intentionally "best effort" because we do not want
            // this loader to hard-depend on unrelated systems.
            TryInvoke(player, "PlayerTargeting", "ClearSelection");
            TryInvoke(player, "PlayerTargeting", "ClearTarget");
            TryInvoke(player, "AttackLoop", "CancelAll");
            TryInvoke(player, "PlayerCombatController", "CancelAll");
            TryInvoke(player, "PlayerSpellcasting", "CancelCast");
            TryInvoke(player, "CombatStateTracker", "ForcePeaceful");
        }

        private static void TryInvoke(NetworkObject root, string typeName, string method)
        {
            var behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                var mb = behaviours[i];
                if (!mb) continue;
                if (mb.GetType().Name != typeName) continue;

                var m = mb.GetType().GetMethod(method);
                if (m != null)
                    m.Invoke(mb, null);

                return; // only invoke once per type
            }
        }

        // --------------------------------------------------------------------
        // Late join support
        // --------------------------------------------------------------------

        private void OnClientConnected(ulong clientId)
        {
            StartCoroutine(DeferredTeleport(clientId));
        }

        private IEnumerator DeferredTeleport(ulong clientId)
        {
            // Wait one frame so NGO has time to spawn the player object.
            yield return null;

            if (!string.IsNullOrWhiteSpace(currentGameplayScene))
                TeleportPlayerToSpawn(clientId, InitialSpawnTag);
        }

        // --------------------------------------------------------------------
        // Helpers
        // --------------------------------------------------------------------

        private string FindCurrentGameplaySceneName()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scn = SceneManager.GetSceneAt(i);
                if (!scn.isLoaded) continue;
                if (scn.name == "DontDestroyOnLoad") continue;
                if (!string.IsNullOrWhiteSpace(BootstrapSceneName) && scn.name == BootstrapSceneName) continue;
                return scn.name;
            }
            return string.Empty;
        }
    }
}
