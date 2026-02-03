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
                InitializeSlotsServer();
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
                var existingInstance = new ItemInstance(existing.itemDefId.ToString())
                {
                    stackCount = existing.stackCount
                };

                var addResult = playerInventory.ServerTryAdd(existingInstance, out _);
                if (addResult != InventoryOpResult.Success)
                    return; // inventory full; abort
            }

            // Remove from inventory.
            var removeResult = playerInventory.ServerTryRemoveAt(inventorySlotIndex, out var removed);
            if (removeResult != InventoryOpResult.Success || removed == null)
                return;

            // Equip.
            SetEquipped(uiSlot, removed.itemDefId, removed.stackCount);
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

            var instance = new ItemInstance(existing.itemDefId.ToString())
            {
                stackCount = existing.stackCount
            };

            // Place into the exact slot.
            var place = playerInventory.ServerTryPlaceIntoEmptySlot(targetInventorySlot, instance);
            if (place != InventoryOpResult.Success)
                return;

            // Clear equipped slot.
            SetEquipped(uiSlot, string.Empty, 0);
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
