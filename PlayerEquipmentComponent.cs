// ============================================================================
// PlayerEquipmentComponent.cs — v3 (Targeted Unequip)
// ----------------------------------------------------------------------------
// Adds:
// - Unequip into a SPECIFIC inventory slot (selected drop target)
//
// Safety rules (MVP):
// - Target inventory slot MUST be empty.
//   (We do NOT do equipment<->inventory swapping yet.)
// ============================================================================

using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace UltimateDungeon.Items
{
    using UltimateDungeon.Actors;
    using UltimateDungeon.Spells;
    using UltimateDungeon.UI;

    [DisallowMultipleComponent]
    public sealed class PlayerEquipmentComponent : NetworkBehaviour
    {
        [Header("Required")]
        [SerializeField] private ItemDefCatalog itemDefCatalog;

        [Header("Required")]
        [SerializeField] private PlayerInventoryComponent playerInventory;

        public NetworkList<EquippedSlotNet> EquippedNet { get; private set; }

        // Server-only: keep full ItemInstance data for equipped items.
        private readonly Dictionary<EquipmentSlotId, ItemInstance> _equippedInstances =
            new Dictionary<EquipmentSlotId, ItemInstance>();

        public event Action OnEquipmentChanged;

        private void Awake()
        {
            EquippedNet = new NetworkList<EquippedSlotNet>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (EquippedNet == null)
                EquippedNet = new NetworkList<EquippedSlotNet>();

            EquippedNet.OnListChanged += _ => OnEquipmentChanged?.Invoke();

            if (IsServer)
            {
                InitializeSlotsServer();
                _equippedInstances.Clear();
            }
        }

        // --------------------------------------------------------------------
        // UI read-only helper
        // --------------------------------------------------------------------

        public EquippedSlotNet GetEquippedForUI(EquipmentSlotId slot)
        {
            if (EquippedNet == null)
                return default;

            for (int i = 0; i < EquippedNet.Count; i++)
                if (EquippedNet[i].slot == slot)
                    return EquippedNet[i];

            return default;
        }

        /// <summary>
        /// UI-only helper: attempt to read the full ItemInstance for a slot when available.
        /// This is used for client-side visual updates (e.g., hotbar icon selection).
        /// </summary>
        public bool TryGetEquippedInstanceForUI(EquipmentSlotId slot, out ItemInstance instance)
        {
            instance = null;
            return _equippedInstances.TryGetValue(slot, out instance) && instance != null;
        }

        // --------------------------------------------------------------------
        // Server helpers (authoritative lookups)
        // --------------------------------------------------------------------

        public bool TryGetEquippedItem(EquipSlot equipSlot, out ItemInstance instance, out ItemDef def)
        {
            instance = null;
            def = null;

            if (!IsServer)
                return false;

            if (itemDefCatalog == null)
                return false;

            if (!TryMapEquipSlotToUiSlot(equipSlot, out var uiSlot))
                return false;

            if (!_equippedInstances.TryGetValue(uiSlot, out instance) || instance == null)
                return false;

            return itemDefCatalog.TryGet(instance.itemDefId, out def) && def != null;
        }

        public static bool TryMapEquipSlotToUiSlot(EquipSlot equipSlot, out EquipmentSlotId uiSlot)
        {
            uiSlot = EquipmentSlotId.Bag;

            switch (equipSlot)
            {
                case EquipSlot.Bag:
                    uiSlot = EquipmentSlotId.Bag;
                    return true;
                case EquipSlot.Head:
                    uiSlot = EquipmentSlotId.Head;
                    return true;
                case EquipSlot.Neck:
                    uiSlot = EquipmentSlotId.Neck;
                    return true;
                case EquipSlot.Mainhand:
                    uiSlot = EquipmentSlotId.Mainhand;
                    return true;
                case EquipSlot.Chest:
                    uiSlot = EquipmentSlotId.Chest;
                    return true;
                case EquipSlot.Offhand:
                    uiSlot = EquipmentSlotId.Offhand;
                    return true;
                case EquipSlot.BeltA:
                    uiSlot = EquipmentSlotId.BeltA;
                    return true;
                case EquipSlot.BeltB:
                    uiSlot = EquipmentSlotId.BeltB;
                    return true;
                case EquipSlot.Foot:
                    uiSlot = EquipmentSlotId.Foot;
                    return true;
                case EquipSlot.Mount:
                    uiSlot = EquipmentSlotId.Mount;
                    return true;
                default:
                    return false;
            }
        }

        // --------------------------------------------------------------------
        // Client API (UI)
        // --------------------------------------------------------------------

        public void RequestEquipFromInventory(int inventorySlotIndex, EquipmentSlotId uiSlot)
        {
            if (!IsOwner) return;
            EquipFromInventoryServerRpc(inventorySlotIndex, uiSlot);
        }

        public void RequestUnequipToInventory(EquipmentSlotId uiSlot, int targetInventorySlot)
        {
            if (!IsOwner) return;
            UnequipToInventoryServerRpc(uiSlot, targetInventorySlot);
        }

        public void RequestSetAbilitySelection(EquipmentSlotId uiSlot, AbilityGrantSlot grantSlot, SpellId spellId)
        {
            if (!IsOwner) return;
            RequestSetAbilitySelectionServerRpc(uiSlot, grantSlot, (int)spellId);
        }

        // --------------------------------------------------------------------
        // Server logic
        // --------------------------------------------------------------------

        [ServerRpc]
        private void EquipFromInventoryServerRpc(int inventorySlotIndex, EquipmentSlotId uiSlot)
        {
            if (!IsServer) return;
            if (playerInventory == null || itemDefCatalog == null) return;

            if (inventorySlotIndex < 0 || inventorySlotIndex >= playerInventory.Inventory.SlotCount)
                return;

            var invSlot = playerInventory.Inventory.GetSlot(inventorySlotIndex);
            if (invSlot.IsEmpty || invSlot.item == null)
                return;

            if (!itemDefCatalog.TryGet(invSlot.item.itemDefId, out var def) || def == null)
                return;

            if (!def.equipment.isEquippable)
                return;

            if (!IsLegalEquip(def, uiSlot))
                return;

            // If something already equipped, try to put it back into inventory first.
            var existing = GetEquipped(uiSlot);
            if (!existing.IsEmpty)
            {
                if (!_equippedInstances.TryGetValue(uiSlot, out var existingInstance) || existingInstance == null)
                {
                    existingInstance = new ItemInstance(existing.itemDefId.ToString())
                    {
                        stackCount = existing.stackCount
                    };
                }

                var addResult = playerInventory.ServerTryAdd(existingInstance, out _);
                if (addResult != InventoryOpResult.Success)
                    return; // inventory full; abort

                _equippedInstances.Remove(uiSlot);
            }

            // Remove from inventory.
            var removeResult = playerInventory.ServerTryRemoveAt(inventorySlotIndex, out var removed);
            if (removeResult != InventoryOpResult.Success || removed == null)
                return;

            // Equip.
            BuildEquippedSnapshot(removed, def, out var activeSlot, out var primarySpell, out var secondarySpell, out var utilitySpell);
            SetEquipped(uiSlot, removed.itemDefId, removed.stackCount, (byte)activeSlot, (int)primarySpell, (int)secondarySpell, (int)utilitySpell);
            _equippedInstances[uiSlot] = removed;
        }

        [ServerRpc]
        private void UnequipToInventoryServerRpc(EquipmentSlotId uiSlot, int targetInventorySlot)
        {
            if (!IsServer) return;
            if (playerInventory == null) return;

            // Validate target slot.
            if (targetInventorySlot < 0 || targetInventorySlot >= playerInventory.Inventory.SlotCount)
                return;

            // Require target to be empty (MVP safety).
            var target = playerInventory.Inventory.GetSlot(targetInventorySlot);
            if (!target.IsEmpty)
                return;

            var existing = GetEquipped(uiSlot);
            if (existing.IsEmpty)
                return;

            if (!_equippedInstances.TryGetValue(uiSlot, out var instance) || instance == null)
            {
                instance = new ItemInstance(existing.itemDefId.ToString())
                {
                    stackCount = existing.stackCount
                };
            }

            // Place into the exact slot.
            var place = playerInventory.ServerTryPlaceIntoEmptySlot(targetInventorySlot, instance);
            if (place != InventoryOpResult.Success)
                return;

            // Clear equipped slot.
            SetEquipped(uiSlot, string.Empty, 0, (byte)AbilityGrantSlot.Primary, (int)SpellId.None, (int)SpellId.None, (int)SpellId.None);
            _equippedInstances.Remove(uiSlot);
        }

        [ServerRpc]
        private void RequestSetAbilitySelectionServerRpc(
            EquipmentSlotId uiSlot,
            AbilityGrantSlot grantSlot,
            int spellId,
            ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;

            if (rpcParams.Receive.SenderClientId != OwnerClientId)
                return;

            if (itemDefCatalog == null)
                return;

            if (!_equippedInstances.TryGetValue(uiSlot, out var instance) || instance == null)
                return;

            if (!itemDefCatalog.TryGet(instance.itemDefId, out var def) || def == null)
                return;

            if (TryGetComponent(out ActorComponent actor) && actor.State == CombatState.InCombat)
                return;

            var chosenSpell = (SpellId)spellId;
            if (chosenSpell == SpellId.None)
                return;

            if (!instance.TrySetSelectedSpellId(def, grantSlot, chosenSpell))
                return;

            // "Last changed dropdown sets active" for hotbar.
            instance.activeGrantSlot = grantSlot;

            BuildEquippedSnapshot(instance, def, out var activeSlot, out var primarySpell, out var secondarySpell, out var utilitySpell);
            SetEquipped(uiSlot, instance.itemDefId, instance.stackCount, (byte)activeSlot, (int)primarySpell, (int)secondarySpell, (int)utilitySpell);
        }

        private void InitializeSlotsServer()
        {
            EquippedNet.Clear();

            foreach (EquipmentSlotId slot in Enum.GetValues(typeof(EquipmentSlotId)))
            {
                EquippedNet.Add(new EquippedSlotNet
                {
                    slot = slot,
                    itemDefId = default,
                    stackCount = 0,
                    activeGrantSlotForHotbar = (byte)AbilityGrantSlot.Primary,
                    selectedSpellPrimary = (int)SpellId.None,
                    selectedSpellSecondary = (int)SpellId.None,
                    selectedSpellUtility = (int)SpellId.None
                });
            }
        }

        private EquippedSlotNet GetEquipped(EquipmentSlotId slot)
        {
            for (int i = 0; i < EquippedNet.Count; i++)
                if (EquippedNet[i].slot == slot)
                    return EquippedNet[i];

            return default;
        }

        private void SetEquipped(
            EquipmentSlotId slot,
            string itemDefId,
            int stackCount,
            byte activeGrantSlotForHotbar,
            int selectedSpellPrimary,
            int selectedSpellSecondary,
            int selectedSpellUtility)
        {
            var id = new FixedString64Bytes(itemDefId);

            for (int i = 0; i < EquippedNet.Count; i++)
            {
                if (EquippedNet[i].slot != slot)
                    continue;

                EquippedNet[i] = new EquippedSlotNet
                {
                    slot = slot,
                    itemDefId = id,
                    stackCount = Mathf.Max(0, stackCount),
                    activeGrantSlotForHotbar = activeGrantSlotForHotbar,
                    selectedSpellPrimary = selectedSpellPrimary,
                    selectedSpellSecondary = selectedSpellSecondary,
                    selectedSpellUtility = selectedSpellUtility
                };
                return;
            }

            EquippedNet.Add(new EquippedSlotNet
            {
                slot = slot,
                itemDefId = id,
                stackCount = Mathf.Max(0, stackCount),
                activeGrantSlotForHotbar = activeGrantSlotForHotbar,
                selectedSpellPrimary = selectedSpellPrimary,
                selectedSpellSecondary = selectedSpellSecondary,
                selectedSpellUtility = selectedSpellUtility
            });
        }

        private static void BuildEquippedSnapshot(
            ItemInstance instance,
            ItemDef def,
            out AbilityGrantSlot activeSlot,
            out SpellId selectedPrimary,
            out SpellId selectedSecondary,
            out SpellId selectedUtility)
        {
            activeSlot = AbilityGrantSlot.Primary;
            selectedPrimary = SpellId.None;
            selectedSecondary = SpellId.None;
            selectedUtility = SpellId.None;

            if (instance == null || def == null)
                return;

            activeSlot = instance.activeGrantSlot;

            var grantedSlots = def.grantedAbilities.grantedAbilitySlots;
            if (grantedSlots != null && grantedSlots.Length > 0)
            {
                if (!IsGrantSlotAllowed(grantedSlots, activeSlot))
                    activeSlot = GetDefaultActiveGrantSlot(def);
            }

            instance.activeGrantSlot = activeSlot;

            selectedPrimary = ResolveSelection(instance, def, AbilityGrantSlot.Primary);
            selectedSecondary = ResolveSelection(instance, def, AbilityGrantSlot.Secondary);
            selectedUtility = ResolveSelection(instance, def, AbilityGrantSlot.Utility);
        }

        private static SpellId ResolveSelection(ItemInstance instance, ItemDef def, AbilityGrantSlot slot)
        {
            if (!TryGetGrantedSlot(def, slot, out var grantedSlot))
                return SpellId.None;

            var selected = instance.GetSelectedSpellId(def, slot);
            if (!IsSpellAllowed(selected, grantedSlot.allowedSpellIds))
                selected = SpellId.None;

            if (selected == SpellId.None)
            {
                var fallback = GetFirstAllowedSpell(grantedSlot.allowedSpellIds);
                if (fallback != SpellId.None && instance.TrySetSelectedSpellId(def, slot, fallback))
                    selected = fallback;
            }

            return selected;
        }

        private static AbilityGrantSlot GetDefaultActiveGrantSlot(ItemDef def)
        {
            if (HasGrantSlot(def, AbilityGrantSlot.Primary))
                return AbilityGrantSlot.Primary;
            if (HasGrantSlot(def, AbilityGrantSlot.Secondary))
                return AbilityGrantSlot.Secondary;
            if (HasGrantSlot(def, AbilityGrantSlot.Utility))
                return AbilityGrantSlot.Utility;

            return AbilityGrantSlot.Primary;
        }

        private static bool HasGrantSlot(ItemDef def, AbilityGrantSlot slot)
        {
            if (def == null)
                return false;

            var slots = def.grantedAbilities.grantedAbilitySlots;
            if (slots == null)
                return false;

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].slot == slot)
                    return true;
            }

            return false;
        }

        private static bool IsGrantSlotAllowed(GrantedAbilitySlot[] granted, AbilityGrantSlot slot)
        {
            if (granted == null)
                return false;

            for (int i = 0; i < granted.Length; i++)
            {
                if (granted[i].slot == slot)
                    return true;
            }

            return false;
        }

        private static bool TryGetGrantedSlot(ItemDef def, AbilityGrantSlot slot, out GrantedAbilitySlot grantedSlot)
        {
            var slots = def.grantedAbilities.grantedAbilitySlots;
            if (slots != null)
            {
                for (int i = 0; i < slots.Length; i++)
                {
                    if (slots[i].slot == slot)
                    {
                        grantedSlot = slots[i];
                        return true;
                    }
                }
            }

            grantedSlot = default;
            return false;
        }

        private static bool IsSpellAllowed(SpellId selected, SpellId[] allowed)
        {
            if (selected == SpellId.None)
                return false;

            if (allowed == null)
                return false;

            for (int i = 0; i < allowed.Length; i++)
            {
                if (allowed[i] == selected)
                    return true;
            }

            return false;
        }

        private static SpellId GetFirstAllowedSpell(SpellId[] allowed)
        {
            if (allowed == null || allowed.Length == 0)
                return SpellId.None;

            return allowed[0];
        }

        private static bool IsLegalEquip(ItemDef def, EquipmentSlotId uiSlot)
        {
            if (def.family == ItemFamily.UtilityItem)
                return uiSlot == EquipmentSlotId.BeltA || uiSlot == EquipmentSlotId.BeltB;

            var required = def.equipment.equipSlot;

            return required switch
            {
                EquipSlot.Bag => uiSlot == EquipmentSlotId.Bag,
                EquipSlot.Head => uiSlot == EquipmentSlotId.Head,
                EquipSlot.Neck => uiSlot == EquipmentSlotId.Neck,
                EquipSlot.Mainhand => uiSlot == EquipmentSlotId.Mainhand,
                EquipSlot.Chest => uiSlot == EquipmentSlotId.Chest,
                EquipSlot.Offhand => uiSlot == EquipmentSlotId.Offhand,
                EquipSlot.BeltA => uiSlot == EquipmentSlotId.BeltA,
                EquipSlot.BeltB => uiSlot == EquipmentSlotId.BeltB,
                EquipSlot.Foot => uiSlot == EquipmentSlotId.Foot,
                EquipSlot.Mount => uiSlot == EquipmentSlotId.Mount,
                _ => false,
            };
        }
    }

    [Serializable]

    public struct EquippedSlotNet : INetworkSerializable, IEquatable<EquippedSlotNet>
    {
        public EquipmentSlotId slot;
        public FixedString64Bytes itemDefId;
        public int stackCount;
        public byte activeGrantSlotForHotbar;
        public int selectedSpellPrimary;
        public int selectedSpellSecondary;
        public int selectedSpellUtility;

        public bool IsEmpty => stackCount <= 0 || itemDefId.Length == 0;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref slot);
            serializer.SerializeValue(ref itemDefId);
            serializer.SerializeValue(ref stackCount);
            serializer.SerializeValue(ref activeGrantSlotForHotbar);
            serializer.SerializeValue(ref selectedSpellPrimary);
            serializer.SerializeValue(ref selectedSpellSecondary);
            serializer.SerializeValue(ref selectedSpellUtility);
        }

        public bool Equals(EquippedSlotNet other)
        {
            return slot == other.slot
                && itemDefId.Equals(other.itemDefId)
                && stackCount == other.stackCount
                && activeGrantSlotForHotbar == other.activeGrantSlotForHotbar
                && selectedSpellPrimary == other.selectedSpellPrimary
                && selectedSpellSecondary == other.selectedSpellSecondary
                && selectedSpellUtility == other.selectedSpellUtility;
        }
    }
}
