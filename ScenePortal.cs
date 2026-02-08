// ============================================================================
// ScenePortal.cs — Ultimate Dungeon (Standalone MVP)
// ----------------------------------------------------------------------------
// PURPOSE
// - World trigger that requests a server-authoritative scene transition.
// - Designed to work with SceneTransitionService (NGO, additive scenes).
//
// HOW IT WORKS
// - Player enters trigger
// - Owning client sends a ServerRpc request
// - Server validates and performs the transition
//
// REQUIREMENTS
// - SceneTransitionService must exist in SCN_Bootstrap
// - Destination scene must be in Build Settings
// - Destination scene must contain a SpawnPoint with matching Tag
//
// USAGE (IN EDITOR)
// 1) Create empty GameObject (eg. Portal_To_Dungeon)
// 2) Add BoxCollider → IsTrigger = true
// 3) Add this ScenePortal component
// 4) Set DestinationSceneName + DestinationSpawnTag
// ============================================================================

using Unity.Netcode;
using UnityEngine;

namespace UltimateDungeon.Scenes
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class ScenePortal : NetworkBehaviour
    {
        [Header("Destination")]
        [Tooltip("Exact scene name as listed in Build Settings (case-sensitive)")]
        public string DestinationSceneName;

        [Tooltip("SpawnPoint.Tag to use in the destination scene")]
        public string DestinationSpawnTag = "Default";

        [Header("Policy")]
        [Tooltip("If true, only the owning player can trigger this portal")]
        public bool OwnerOnly = true;

        [Tooltip("If true, server may reject the transition if player is in combat")]
        public bool RequireOutOfCombat = true;

        private void Reset()
        {
            // Ensure collider is a trigger by default
            var col = GetComponent<Collider>();
            if (col != null)
                col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            // Client-side request only
            if (!NetworkManager.Singleton)
                return;

            var netObj = other.GetComponentInParent<NetworkObject>();
            if (!netObj)
                return;

            // Only the owning client should request the transition
            if (OwnerOnly && !netObj.IsOwner)
                return;

            if (!netObj.IsOwner)
                return;

            var svc = SceneTransitionService.Instance;
            if (!svc)
            {
                Debug.LogWarning("[ScenePortal] SceneTransitionService not found.");
                return;
            }

            svc.RequestSceneTransitionServerRpc(
                netObj.OwnerClientId,
                DestinationSceneName,
                DestinationSpawnTag,
                RequireOutOfCombat);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(transform.position, GetGizmoSize());
        }

        private Vector3 GetGizmoSize()
        {
            var col = GetComponent<Collider>();
            if (col is BoxCollider box)
                return box.size;
            return Vector3.one;
        }
#endif
    }
}
