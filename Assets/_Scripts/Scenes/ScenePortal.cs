using UnityEngine;
using Unity.Netcode;

namespace UltimateDungeon.Scenes
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class ScenePortal : MonoBehaviour
    {
        [Header("Destination")]
        public string DestinationSceneName;
        public string DestinationSpawnTag = "Default";

        [Header("Policy")]
        public bool OwnerOnly = true;
        public bool RequireOutOfCombat = true;

        private void Reset()
        {
            var c = GetComponent<Collider>();
            c.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!NetworkManager.Singleton)
                return;

            // SERVER ONLY: prevents host double-trigger + clients spamming RPCs
            if (!NetworkManager.Singleton.IsServer)
                return;

            var netObj = other.GetComponentInParent<NetworkObject>();
            if (!netObj)
                return;

            // If OwnerOnly is enabled, make sure the thing entering is actually the
            // owning client's PlayerObject (works for Host AND Dedicated).
            if (OwnerOnly)
            {
                if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(netObj.OwnerClientId, out var owningClient))
                    return;

                if (owningClient.PlayerObject == null)
                    return;

                // Only proceed if THIS NetworkObject is the player's PlayerObject
                if (owningClient.PlayerObject.NetworkObjectId != netObj.NetworkObjectId)
                    return;
            }

            var svc = SceneTransitionService.Instance;
            if (!svc)
            {
                Debug.LogWarning("[ScenePortal] No SceneTransitionService.Instance found.", this);
                return;
            }

            Debug.Log($"[ScenePortal] Server trigger -> '{DestinationSceneName}' tag '{DestinationSpawnTag}' ownerClientId={netObj.OwnerClientId}", this);

            svc.RequestSceneTransitionServerRpc(
                netObj.OwnerClientId,
                DestinationSceneName,
                DestinationSpawnTag,
                RequireOutOfCombat);
        }
    }
}
