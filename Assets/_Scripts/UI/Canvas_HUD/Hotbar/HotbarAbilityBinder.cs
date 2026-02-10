// ============================================================================
// HotbarAbilityBinder.cs
// ----------------------------------------------------------------------------
// Binds the Hotbar UI to the local player's hotbar ability controller.
// ============================================================================

using UnityEngine;
using UltimateDungeon.Spells;
using UltimateDungeon.Players.Networking;

namespace UltimateDungeon.UI.Hotbar
{
    [DisallowMultipleComponent]
    public sealed class HotbarAbilityBinder : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private HotbarUI hotbar;

        // Cached controller on the local player.
        private PlayerHotbarAbilityController _controller;

        private void OnEnable()
        {
            // Subscribe to the local player spawn event so we can bind as soon as
            // the local PlayerNetIdentity exists.
            PlayerNetIdentity.LocalPlayerSpawned += HandleLocalPlayerSpawned;

            // Safety: auto-find the HotbarUI if it wasn't wired in the inspector.
            if (hotbar == null)
                hotbar = FindFirstObjectByType<HotbarUI>(FindObjectsInactive.Include);

            // If the local identity already exists (e.g., UI enabled after spawn), bind immediately.
            if (PlayerNetIdentity.Local != null)
                HandleLocalPlayerSpawned(PlayerNetIdentity.Local);
        }

        private void OnDisable()
        {
            PlayerNetIdentity.LocalPlayerSpawned -= HandleLocalPlayerSpawned;
            Unbind();

            _controller = null;
        }

        private void HandleLocalPlayerSpawned(PlayerNetIdentity identity)
        {
            if (identity == null)
                return;

            // The local player should carry the server-authoritative hotbar ability controller.
            _controller = identity.GetComponent<PlayerHotbarAbilityController>();
            if (_controller == null)
            {
                Debug.LogWarning("[HotbarAbilityBinder] PlayerHotbarAbilityController missing on local player.");
                return;
            }

            Bind();
        }

        private void Bind()
        {
            if (hotbar == null || _controller == null)
                return;

            // Prevent double-subscription.
            hotbar.SlotActivated -= HandleSlotActivated;
            hotbar.SlotActivated += HandleSlotActivated;
        }

        private void Unbind()
        {
            if (hotbar == null)
                return;

            hotbar.SlotActivated -= HandleSlotActivated;
        }

        private void HandleSlotActivated(int slotIndex)
        {
            if (_controller == null)
                return;

            // Delegate to the controller, which should enforce server rules/validation.
            _controller.RequestCastFromHotbar(slotIndex);
        }
    }
}
