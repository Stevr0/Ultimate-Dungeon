// ============================================================================
// HotbarSpellIconBinder.cs
// ----------------------------------------------------------------------------
// UI-only binder that resolves the active spell per equipped item and paints
// the hotbar slot icon using SpellDef.spellIcon.
//
// Fix:
// - Adds the correct namespace import for PlayerNetIdentity.
//
// Notes:
// - This script is UI-only; it never mutates gameplay state.
// - It listens for the local player spawn event (PlayerNetIdentity.LocalPlayerSpawned)
//   then binds to PlayerEquipmentComponent changes to keep icons fresh.
// ============================================================================

using UnityEngine;
using UltimateDungeon.Items;
using UltimateDungeon.Spells;
using UltimateDungeon.Players.Networking;

namespace UltimateDungeon.UI.Hotbar
{
    [DisallowMultipleComponent]
    public sealed class HotbarSpellIconBinder : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private HotbarUI hotbar;
        [SerializeField] private ItemDefCatalog itemDefCatalog;
        [SerializeField] private SpellDefCatalog spellCatalog;

        [Header("Fallbacks")]
        [Tooltip("Optional placeholder icon when a SpellDef has no icon assigned.")]
        [SerializeField] private Sprite fallbackIcon;

        private PlayerEquipmentComponent _equipment;

        private void OnEnable()
        {
            // Bind when the local player is available.
            PlayerNetIdentity.LocalPlayerSpawned += HandleLocalPlayerSpawned;

            // Auto-find the hotbar if it wasn't wired in the inspector.
            if (hotbar == null)
                hotbar = FindFirstObjectByType<HotbarUI>(FindObjectsInactive.Include);

            // If the local player already exists, bind immediately.
            if (PlayerNetIdentity.Local != null)
                HandleLocalPlayerSpawned(PlayerNetIdentity.Local);
        }

        private void OnDisable()
        {
            PlayerNetIdentity.LocalPlayerSpawned -= HandleLocalPlayerSpawned;
            UnbindEquipment();
        }

        private void HandleLocalPlayerSpawned(PlayerNetIdentity identity)
        {
            if (identity == null)
                return;

            _equipment = identity.GetComponent<PlayerEquipmentComponent>();
            if (_equipment == null)
            {
                Debug.LogWarning("[HotbarSpellIconBinder] PlayerEquipmentComponent missing on local player.");
                return;
            }

            BindEquipment();
            RefreshAllSlots();
        }

        private void BindEquipment()
        {
            if (_equipment == null)
                return;

            // Prevent double-subscribe.
            _equipment.OnEquipmentChanged -= HandleEquipmentChanged;
            _equipment.OnEquipmentChanged += HandleEquipmentChanged;
        }

        private void UnbindEquipment()
        {
            if (_equipment == null)
                return;

            _equipment.OnEquipmentChanged -= HandleEquipmentChanged;
            _equipment = null;
        }

        private void HandleEquipmentChanged()
        {
            RefreshAllSlots();
        }

        /// <summary>
        /// UI-only refresh for all hotbar slots.
        /// </summary>
        public void RefreshAllSlots()
        {
            if (hotbar == null)
                return;

            for (int i = 0; i < HotbarUI.SlotCount; i++)
                RefreshSlotByHotbarIndex(i);
        }

        /// <summary>
        /// UI-only refresh for the hotbar slot mapped to a specific equipment slot.
        /// </summary>
        public void RefreshSlotForEquipmentSlot(EquipmentSlotId slotId)
        {
            if (hotbar == null)
                return;

            for (int i = 0; i < HotbarUI.SlotCount; i++)
            {
                // Hotbar index -> equipment slot
                if (!PlayerHotbarAbilityController.TryMapHotbarIndexToEquipSlot(i, out var equipSlot))
                    continue;

                // equipment slot -> UI equipment slot id
                if (!PlayerEquipmentComponent.TryMapEquipSlotToUiSlot(equipSlot, out var uiSlot))
                    continue;

                if (uiSlot != slotId)
                    continue;

                RefreshSlotByHotbarIndex(i);
                return;
            }
        }

        private void RefreshSlotByHotbarIndex(int hotbarIndex)
        {
            // If any dependency is missing, clear the UI slot to avoid stale icons.
            if (hotbar == null || _equipment == null || itemDefCatalog == null || spellCatalog == null)
            {
                hotbar?.ClearSlot(hotbarIndex);
                return;
            }

            // Map hotbar index -> equipment slot.
            if (!PlayerHotbarAbilityController.TryMapHotbarIndexToEquipSlot(hotbarIndex, out var equipSlot))
            {
                hotbar.ClearSlot(hotbarIndex);
                return;
            }

            // Map equipment slot -> UI equipment slot id.
            if (!PlayerEquipmentComponent.TryMapEquipSlotToUiSlot(equipSlot, out var uiSlot))
            {
                hotbar.ClearSlot(hotbarIndex);
                return;
            }

            // Read equipped snapshot for UI.
            var equipped = _equipment.GetEquippedForUI(uiSlot);
            if (equipped.IsEmpty)
            {
                hotbar.ClearSlot(hotbarIndex);
                return;
            }

            // Optional debug trace.
            Debug.Log(
                $"[HotbarIcon] equip={equipSlot} active={(AbilityGrantSlot)equipped.activeGrantSlotForHotbar} " +
                $"P={(SpellId)equipped.selectedSpellPrimary} S={(SpellId)equipped.selectedSpellSecondary} U={(SpellId)equipped.selectedSpellUtility}");

            // Validate the item def exists.
            if (!itemDefCatalog.TryGet(equipped.itemDefId.ToString(), out var itemDef) || itemDef == null)
            {
                hotbar.ClearSlot(hotbarIndex);
                return;
            }

            // Resolve which spell is currently active for the equipped item.
            var activeSlot = (AbilityGrantSlot)equipped.activeGrantSlotForHotbar;
            var spellId = ResolveSelectedSpell(equipped, activeSlot);
            if (spellId == SpellId.None)
            {
                hotbar.ClearSlot(hotbarIndex);
                return;
            }

            // Resolve the spell def.
            var spellDef = spellCatalog.Get(spellId);
            if (spellDef == null)
            {
                hotbar.ClearSlot(hotbarIndex);
                return;
            }

            // Spell icons are authored on SpellDef. Use a placeholder if missing.
            var icon = spellDef.spellIcon != null ? spellDef.spellIcon : fallbackIcon;
            hotbar.SetSlotIcon(hotbarIndex, icon);
        }

        private static SpellId ResolveSelectedSpell(EquippedSlotNet equipped, AbilityGrantSlot activeSlot)
        {
            // IMPORTANT:
            // - EquippedSlotNet contains three selected spell fields (Primary/Secondary/Utility)
            // - activeGrantSlotForHotbar chooses which one should be displayed.
            return activeSlot switch
            {
                AbilityGrantSlot.Primary => (SpellId)equipped.selectedSpellPrimary,
                AbilityGrantSlot.Secondary => (SpellId)equipped.selectedSpellSecondary,
                AbilityGrantSlot.Utility => (SpellId)equipped.selectedSpellUtility,
                _ => SpellId.None
            };
        }
    }
}
