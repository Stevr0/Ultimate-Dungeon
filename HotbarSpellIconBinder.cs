// ============================================================================
// HotbarSpellIconBinder.cs
// ----------------------------------------------------------------------------
// UI-only binder that resolves the active spell per equipped item and paints
// the hotbar slot icon using SpellDef.spellIcon.
// ============================================================================

using UnityEngine;
using UltimateDungeon.Items;
using UltimateDungeon.Spells;

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
            PlayerNetIdentity.LocalPlayerSpawned += HandleLocalPlayerSpawned;

            if (hotbar == null)
                hotbar = FindFirstObjectByType<HotbarUI>(FindObjectsInactive.Include);

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
                if (!PlayerHotbarAbilityController.TryMapHotbarIndexToEquipSlot(i, out var equipSlot))
                    continue;

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
            if (hotbar == null || _equipment == null || itemDefCatalog == null || spellCatalog == null)
            {
                hotbar?.ClearSlot(hotbarIndex);
                return;
            }

            if (!PlayerHotbarAbilityController.TryMapHotbarIndexToEquipSlot(hotbarIndex, out var equipSlot))
            {
                hotbar.ClearSlot(hotbarIndex);
                return;
            }

            if (!PlayerEquipmentComponent.TryMapEquipSlotToUiSlot(equipSlot, out var uiSlot))
            {
                hotbar.ClearSlot(hotbarIndex);
                return;
            }

            var equipped = _equipment.GetEquippedForUI(uiSlot);
            if (equipped.IsEmpty)
            {
                hotbar.ClearSlot(hotbarIndex);
                return;
            }

            if (!itemDefCatalog.TryGet(equipped.itemDefId.ToString(), out var itemDef) || itemDef == null)
            {
                hotbar.ClearSlot(hotbarIndex);
                return;
            }

            if (!_equipment.TryGetEquippedInstanceForUI(uiSlot, out var instance) || instance == null)
            {
                // UI-only: without an instance we cannot resolve the active grant slot selection.
                hotbar.ClearSlot(hotbarIndex);
                return;
            }

            if (!instance.TryResolveActiveGrantSlot(itemDef, allowUpdate: false, out var activeSlot))
            {
                hotbar.ClearSlot(hotbarIndex);
                return;
            }

            var spellId = instance.GetSelectedSpellId(itemDef, activeSlot);
            if (spellId == SpellId.None)
            {
                hotbar.ClearSlot(hotbarIndex);
                return;
            }

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
    }
}
