// ============================================================================
// HotbarAbilityBinder.cs
// ----------------------------------------------------------------------------
// Binds the Hotbar UI to the local player's hotbar ability controller.
// ============================================================================

using UnityEngine;
using UltimateDungeon.Spells;

namespace UltimateDungeon.UI.Hotbar
{
    [DisallowMultipleComponent]
    public sealed class HotbarAbilityBinder : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private HotbarUI hotbar;

        private PlayerHotbarAbilityController _controller;

        private void OnEnable()
        {
            PlayerNetIdentity.LocalPlayerSpawned += HandleLocalPlayerSpawned;

            if (hotbar == null)
                hotbar = FindFirstObjectByType<HotbarUI>(FindObjectsInactive.Include);

            if (PlayerNetIdentity.Local != null)
                HandleLocalPlayerSpawned(PlayerNetIdentity.Local);
        }

        private void OnDisable()
        {
            PlayerNetIdentity.LocalPlayerSpawned -= HandleLocalPlayerSpawned;
            Unbind();
        }

        private void HandleLocalPlayerSpawned(PlayerNetIdentity identity)
        {
            if (identity == null)
                return;

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

            _controller.RequestCastFromHotbar(slotIndex);
        }
    }
}
