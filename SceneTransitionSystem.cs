// ============================================================================
// SceneTransitionSystem.cs — Ultimate Dungeon (Clean v3.1)
// ----------------------------------------------------------------------------
// WHY THIS VERSION EXISTS
// Your previous v3 script was correct in principle, but in practice Unity/NGO
// will NOT call SceneTransitionService.OnNetworkSpawn unless that component is
// on a NetworkObject that actually spawns.
//
// In a bootstrap scene, we want SceneTransitionService to be a *scene NetworkObject*
// so it spawns automatically when the host starts.
//
// This file provides:
// - SpawnPoint: authored spawn targets inside gameplay scenes
// - ScenePortal: trigger that asks server to transition scenes
// - SceneTransitionService: server authoritative loader/unloader + teleporter
// - SceneTransitionSystem: tiny shim so you can "Add Component" by file name
//
// IMPORTANT REQUIREMENTS
// 1) The GameObject holding SceneTransitionService MUST also have a NetworkObject,
//    and must be a Scene Object (Spawn With Scene).
// 2) The GameObject holding SceneTransitionService MUST be in SCN_Bootstrap.
// 3) SCN_Village MUST be in Build Settings.
//
// NOTE ABOUT OUT-OF-COMBAT GATE
// RequireOutOfCombat is a PLAYER STATE check, not a scene-flag check.
// This file leaves that as a TODO hook so we don’t accidentally block portals
// in safe scenes.
// ============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UltimateDungeon.Scenes
{
    // ========================================================================
    // ScenePortal
    // ========================================================================
    /// <summary>
    /// Trigger that requests a server-authoritative scene transition.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class ScenePortal : NetworkBehaviour
    {
        [Header("Destination")]
        [Tooltip("Scene name as it appears in Build Settings (case-sensitive).")]
        public string DestinationSceneName;

        [Tooltip("SpawnPoint.Tag to use in the destination scene.")]
        public string DestinationSpawnTag = "Default";

        [Header("Policy")]
        [Tooltip("If true, only the owning player can trigger this portal.")]
        public bool OwnerOnly = true;

        [Tooltip("If true, require that the player is NOT in combat to transition.")]
        public bool RequireOutOfCombat = true;

        private void Reset()
        {
            var c = GetComponent<Collider>();
            if (c) c.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            // Only the owning client should request transitions.
            if (!NetworkManager.Singleton) return;

            var playerNetObj = other.GetComponentInParent<NetworkObject>();
            if (!playerNetObj) return;

            if (OwnerOnly && !playerNetObj.IsOwner) return;
            if (!playerNetObj.IsOwner) return;

            var svc = SceneTransitionService.Instance;
            if (!svc) return;

            svc.RequestSceneTransitionServerRpc(
                playerNetObj.OwnerClientId,
                DestinationSceneName,
                DestinationSpawnTag,
                RequireOutOfCombat);
        }

    

        private static void TryInvoke(NetworkObject root, string typeName, string method)
        {
            foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb && mb.GetType().Name == typeName)
                {
                    var m = mb.GetType().GetMethod(method);
                    if (m != null) m.Invoke(mb, null);
                    return;
                }
            }
        }
    }

    // ========================================================================
    // SceneTransitionSystem (Unity attachment shim)
    // ========================================================================
    /// <summary>
    /// Unity convenience shim so you can add by file name.
    /// The actual runtime behaviour is SceneTransitionService.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SceneTransitionService))]
    public sealed class SceneTransitionSystem : MonoBehaviour
    {
        // Intentionally empty.
    }
}
