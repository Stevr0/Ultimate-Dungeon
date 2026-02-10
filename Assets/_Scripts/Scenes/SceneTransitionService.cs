// ============================================================================
// SceneTransitionService.cs — Ultimate Dungeon (Authoritative Scene Loader)
// ----------------------------------------------------------------------------
// PURPOSE
// - Server-authoritative scene transitions using NGO additive scene loading.
// - Designed to live in SCN_Bootstrap and persist via DontDestroyOnLoad.
// - Loads initial gameplay scene on host start (optional).
// - Supports portal-triggered transitions to other gameplay scenes.
//
// FIXES IN THIS VERSION
// 1) Teleport now hardens against NavMeshAgent "random snap" issues:
//    - Clears any old path (ResetPath)
//    - Temporarily disables agent, moves transform / NetworkTransform
//    - Re-enables and Warps agent to a sampled NavMesh point
//
// 2) Uses correct NetworkBehaviour references:
//    - Connected clients are accessed through NetworkManager.Singleton
//      (NOT NetworkManager.ConnectedClients, which is an instance property)
//
// 3) Keeps the original singleton API:
//    - SceneTransitionService.Instance exists so ScenePortal can find it.
//
// WHERE TO PUT
//   Assets/_Scripts/Scenes/SceneTransitionService.cs
// ============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace UltimateDungeon.Scenes
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    public sealed class SceneTransitionService : NetworkBehaviour
    {
        // --------------------------------------------------------------------
        // Singleton
        // --------------------------------------------------------------------

        public static SceneTransitionService Instance { get; private set; }

        // --------------------------------------------------------------------
        // Inspector
        // --------------------------------------------------------------------

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

        // --------------------------------------------------------------------
        // Internal transition bookkeeping
        // --------------------------------------------------------------------

        private bool waitingForLoad;
        private bool waitingForUnload;
        private string pendingLoadScene;
        private string pendingUnloadScene;

        // --------------------------------------------------------------------
        // Unity lifetime
        // --------------------------------------------------------------------

        private void Awake()
        {
            // Standard singleton.
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

            // Only the server is allowed to load/unload scenes.
            if (!IsServer)
                return;

            // Hook client join so late joiners get teleported into the current gameplay scene.
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

            // Hook NGO scene events.
            var sm = NetworkManager.Singleton.SceneManager;
            if (sm == null)
            {
                Debug.LogError("[SceneTransitionService] NetworkManager.SceneManager is null. Is NGO Scene Management disabled on NetworkManager?");
                return;
            }

            sm.OnLoadEventCompleted += OnLoadEventCompleted;
            sm.OnUnloadEventCompleted += OnUnloadEventCompleted;
            Debug.Log("[SceneTransitionService] Subscribed to SceneManager load/unload callbacks.", this);

            // If a gameplay scene is already loaded (rare in bootstrap flow), respect it.
            currentGameplayScene = FindCurrentGameplaySceneName();

            // Normal flow: SCN_Bootstrap is loaded first, then server loads the initial gameplay scene.
            if (AutoLoadInitialGameplayScene && string.IsNullOrWhiteSpace(currentGameplayScene))
            {
                if (string.IsNullOrWhiteSpace(InitialGameplaySceneName))
                {
                    Debug.LogError("[SceneTransitionService] InitialGameplaySceneName is empty. Cannot load start scene.");
                    return;
                }

                Debug.Log($"[SceneTransitionService] Host started. Loading initial gameplay scene '{InitialGameplaySceneName}'...", this);

                // After initial scene loads, teleport everyone currently connected (host player).
                StartCoroutine(ServerLoadGameplayScene(InitialGameplaySceneName, teleportAllPlayersOnComplete: true));
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer && NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;

                var sm = NetworkManager.Singleton.SceneManager;
                if (sm != null)
                {
                    sm.OnLoadEventCompleted -= OnLoadEventCompleted;
                    sm.OnUnloadEventCompleted -= OnUnloadEventCompleted;
                }
            }

            base.OnNetworkDespawn();
        }

        // --------------------------------------------------------------------
        // Entry point used by portals
        // --------------------------------------------------------------------

        [ServerRpc(RequireOwnership = false)]
        public void RequestSceneTransitionServerRpc(
            ulong requestingClientId,
            string destinationSceneName,
            string destinationSpawnTag,
            bool requireOutOfCombat)
        {
            Debug.Log(
                $"[SceneTransitionService] RPC ENTER dest='{destinationSceneName}' tag='{destinationSpawnTag}' " +
                $"IsServer={IsServer} waitingLoad={waitingForLoad} waitingUnload={waitingForUnload}",
                this);

            if (!IsServer)
                return;

            // If we are mid-transition, refuse new transitions (MVP).
            if (waitingForLoad || waitingForUnload)
                return;

            if (string.IsNullOrWhiteSpace(destinationSceneName))
                return;

            // Validate this client exists and has a spawned player.
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(requestingClientId, out var client))
                return;

            var playerObj = client.PlayerObject;
            if (!playerObj)
                return;

            // TODO: enforce requireOutOfCombat using your combat tracker.
            // For now, we accept the transition.

            StartCoroutine(ServerTransitionRoutine(requestingClientId, destinationSceneName, destinationSpawnTag));
        }

        private IEnumerator ServerTransitionRoutine(ulong clientId, string destScene, string spawnTag)
        {
            // 1) Clear illegal state BEFORE leaving the scene (best-effort).
            ClearIllegalState(clientId);

            // 2) Unload current gameplay scene (if different).
            if (!string.IsNullOrWhiteSpace(currentGameplayScene) && currentGameplayScene != destScene)
                yield return ServerUnloadGameplayScene(currentGameplayScene);

            // 3) Load destination scene if needed.
            if (currentGameplayScene != destScene)
                yield return ServerLoadGameplayScene(destScene, teleportAllPlayersOnComplete: false);

            // 4) Teleport just the requesting player.
            TeleportPlayerToSpawn(clientId, spawnTag);
        }

        // --------------------------------------------------------------------
        // Load / Unload (server)
        // --------------------------------------------------------------------

        private IEnumerator ServerLoadGameplayScene(string sceneName, bool teleportAllPlayersOnComplete = false)
        {
            waitingForLoad = true;
            pendingLoadScene = sceneName;

            // We set the current gameplay scene name before load completes so
            // teleport logic knows where to search once the scene is loaded.
            currentGameplayScene = sceneName;

            Debug.Log($"[SceneTransitionService] LoadScene START '{sceneName}'", this);

            NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);

            // Timeout safety.
            float timeout = 10f;
            while (waitingForLoad && timeout > 0f)
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }

            if (waitingForLoad)
            {
                Debug.LogError(
                    $"[SceneTransitionService] LoadScene TIMEOUT '{sceneName}'. " +
                    $"Check: NetworkManager -> Enable Scene Management, and BuildSettings includes the scene name.",
                    this);
                waitingForLoad = false;
            }

            Debug.Log($"[SceneTransitionService] LoadScene DONE '{sceneName}'", this);

            if (teleportAllPlayersOnComplete)
                TeleportAllPlayersToSpawn(InitialSpawnTag);
        }

        private IEnumerator ServerUnloadGameplayScene(string sceneName)
        {
            waitingForUnload = true;
            pendingUnloadScene = sceneName;

            Debug.Log($"[SceneTransitionService] UnloadScene START '{sceneName}'", this);

            var unityScene = SceneManager.GetSceneByName(sceneName);
            if (unityScene.IsValid() && unityScene.isLoaded)
                NetworkManager.Singleton.SceneManager.UnloadScene(unityScene);

            float timeout = 10f;
            while (waitingForUnload && timeout > 0f)
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }

            if (waitingForUnload)
            {
                Debug.LogError($"[SceneTransitionService] UnloadScene TIMEOUT '{sceneName}'", this);
                waitingForUnload = false;
            }

            Debug.Log($"[SceneTransitionService] UnloadScene DONE '{sceneName}'", this);
        }

        private void OnLoadEventCompleted(string sceneName, LoadSceneMode mode, List<ulong> _, List<ulong> __)
        {
            Debug.Log($"[SceneTransitionService] OnLoadEventCompleted '{sceneName}' pending='{pendingLoadScene}'", this);

            if (sceneName == pendingLoadScene)
                waitingForLoad = false;
        }

        private void OnUnloadEventCompleted(string sceneName, LoadSceneMode mode, List<ulong> _, List<ulong> __)
        {
            Debug.Log($"[SceneTransitionService] OnUnloadEventCompleted '{sceneName}' pending='{pendingUnloadScene}'", this);

            if (sceneName == pendingUnloadScene)
                waitingForUnload = false;
        }

        // --------------------------------------------------------------------
        // Teleport (server)
        // --------------------------------------------------------------------

        private void TeleportAllPlayersToSpawn(string spawnTag)
        {
            foreach (var kvp in NetworkManager.Singleton.ConnectedClients)
                TeleportPlayerToSpawn(kvp.Key, spawnTag);
        }

        private void TeleportPlayerToSpawn(ulong clientId, string spawnTag)
        {
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return;

            var playerObj = client.PlayerObject;
            if (!playerObj)
                return;

            var scene = SceneManager.GetSceneByName(currentGameplayScene);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogWarning(
                    $"[SceneTransitionService] TeleportPlayerToSpawn: gameplay scene '{currentGameplayScene}' is not loaded. clientId={clientId}",
                    this);
                return;
            }

            // Find requested tag, else Default.
            SpawnPoint sp = FindSpawnPoint(scene, spawnTag);
            if (!sp && !string.Equals(spawnTag, "Default", StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning(
                    $"[SceneTransitionService] SpawnPoint tag '{spawnTag}' not found in scene '{currentGameplayScene}'. Falling back to 'Default'.",
                    this);
                sp = FindSpawnPoint(scene, "Default");
            }

            if (!sp)
            {
                Debug.LogError(
                    $"[SceneTransitionService] NO SpawnPoint found in scene '{currentGameplayScene}' for tag '{spawnTag}' or 'Default'. Teleport cancelled.",
                    this);
                return;
            }

            Vector3 targetPos = sp.transform.position;
            Quaternion targetRot = sp.transform.rotation;

            // --------------------
            // NavMeshAgent hardening
            // --------------------
            // If you move the transform while an agent is enabled, the agent may
            // immediately snap to a different spot or continue an old path.
            var agent = playerObj.GetComponent<NavMeshAgent>();
            bool hadAgent = agent != null;
            bool agentWasEnabled = false;

            if (hadAgent)
            {
                agentWasEnabled = agent.enabled;

                if (agent.enabled)
                {
                    // Stop + clear destination so it doesn't keep walking.
                    agent.isStopped = true;
                    agent.ResetPath();
                }

                // Disable before moving the transform.
                agent.enabled = false;
            }

            // --------------------
            // NetworkTransform hardening
            // --------------------
            // Prefer Teleport so clients snap instead of interpolating.
            var netTransform = playerObj.GetComponent<NetworkTransform>();
            if (netTransform != null)
            {
                netTransform.Teleport(targetPos, targetRot, playerObj.transform.localScale);
            }
            else
            {
                playerObj.transform.SetPositionAndRotation(targetPos, targetRot);
            }

            // Re-enable + warp agent onto navmesh.
            if (hadAgent)
            {
                agent.enabled = agentWasEnabled;

                if (agent.enabled)
                {
                    // If spawn isn't exactly on mesh, sample nearby to avoid off-mesh agent.
                    if (NavMesh.SamplePosition(targetPos, out var hit, 2.0f, NavMesh.AllAreas))
                        agent.Warp(hit.position);
                    else
                        agent.Warp(targetPos);

                    agent.ResetPath();
                    agent.isStopped = true;
                }
            }

            Debug.Log(
                $"[SceneTransitionService] Teleported clientId={clientId} to SpawnPoint tag='{sp.Tag}' scene='{currentGameplayScene}' pos={targetPos}",
                this);
        }

        private static SpawnPoint FindSpawnPoint(Scene scene, string tag)
        {
            if (!scene.isLoaded)
                return null;

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
        // Clears required by Scene rules (best-effort)
        // --------------------------------------------------------------------

        private static void ClearIllegalState(ulong clientId)
        {
            if (!NetworkManager.Singleton)
                return;

            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return;

            var player = client.PlayerObject;
            if (!player)
                return;

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

                return; // invoke once per type
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

                return scn.name; // first non-bootstrap loaded scene
            }

            return string.Empty;
        }
    }
}
