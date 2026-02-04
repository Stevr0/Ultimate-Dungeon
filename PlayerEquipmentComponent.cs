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
            SetEquipped(uiSlot, removed.itemDefId, removed.stackCount);
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
            SetEquipped(uiSlot, string.Empty, 0);
            _equippedInstances.Remove(uiSlot);
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
                    stackCount = 0
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

        private void SetEquipped(EquipmentSlotId slot, string itemDefId, int stackCount)
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
                    stackCount = Mathf.Max(0, stackCount)
                };
                return;
            }

            EquippedNet.Add(new EquippedSlotNet
            {
                slot = slot,
                itemDefId = id,
                stackCount = Mathf.Max(0, stackCount)
            });
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

        public bool IsEmpty => stackCount <= 0 || itemDefId.Length == 0;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref slot);
            serializer.SerializeValue(ref itemDefId);
            serializer.SerializeValue(ref stackCount);
        }

        public bool Equals(EquippedSlotNet other)
        {
            return slot == other.slot && itemDefId.Equals(other.itemDefId) && stackCount == other.stackCount;
        }
    }
}
