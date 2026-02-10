// ============================================================================
// InventoryRuntimeModel.cs — Ultimate Dungeon (AUTHORITATIVE)
// ----------------------------------------------------------------------------
// NOTE:
// - Your project already defines InventoryOpResult elsewhere.
// - This file MUST NOT redefine it, otherwise you'll get CS0101.
// - Keep your existing InventoryOpResult enum and use it here.
//
// Key fix:
// - TrySplit() uses ItemInstance.DeepCloneNewIdentity() so split stacks get a
//   distinct instanceId.
// ============================================================================

using System;
using UnityEngine;

namespace UltimateDungeon.Items
{
    public sealed class InventoryRuntimeModel
    {
        public const int DefaultSlotCount = 40;

        public struct Slot
        {
            public ItemInstance item;
            public bool IsEmpty => item == null;
        }

        private readonly Slot[] _slots;

        // If your existing model already has events, keep them.
        public event Action OnFullRefresh;

        public int SlotCount => _slots.Length;

        public InventoryRuntimeModel(int slotCount)
        {
            if (slotCount <= 0)
                slotCount = DefaultSlotCount;

            _slots = new Slot[slotCount];
        }

        public Slot GetSlot(int index)
        {
            if (index < 0 || index >= _slots.Length)
                return default;

            return _slots[index];
        }

        public void NotifyFullRefresh()
        {
            OnFullRefresh?.Invoke();
        }

        // --------------------------------------------------------------------
        // Operations (minimal set used by the loot/persistence slice)
        // --------------------------------------------------------------------

        public InventoryOpResult TryAdd(ItemInstance item, ItemDefCatalog catalog, out int placedSlot)
        {
            placedSlot = -1;

            if (item == null)
                return InventoryOpResult.Failed;

            if (catalog == null)
                return InventoryOpResult.UnknownItemDef;

            if (!catalog.TryGet(item.itemDefId, out var def) || def == null)
                return InventoryOpResult.UnknownItemDef;

            // MVP add: first empty slot.
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].item != null)
                    continue;

                _slots[i].item = item;
                placedSlot = i;
                NotifyFullRefresh();
                return InventoryOpResult.Success;
            }

            return InventoryOpResult.Failed;
        }

        public InventoryOpResult TryPlaceIntoEmptySlot(int toSlot, ItemInstance item, ItemDefCatalog catalog)
        {
            if (item == null)
                return InventoryOpResult.Failed;

            if (!IsValidSlot(toSlot))
                return InventoryOpResult.Failed;

            if (_slots[toSlot].item != null)
                return InventoryOpResult.Failed;

            _slots[toSlot].item = item;
            NotifyFullRefresh();
            return InventoryOpResult.Success;
        }

        public InventoryOpResult TryMoveOrSwap(int fromSlot, int toSlot, ItemDefCatalog catalog)
        {
            if (!IsValidSlot(fromSlot) || !IsValidSlot(toSlot))
                return InventoryOpResult.Failed;

            if (fromSlot == toSlot)
                return InventoryOpResult.Success;

            var a = _slots[fromSlot].item;
            var b = _slots[toSlot].item;

            if (a == null)
                return InventoryOpResult.Failed;

            _slots[fromSlot].item = b;
            _slots[toSlot].item = a;

            NotifyFullRefresh();
            return InventoryOpResult.Success;
        }

        /// <summary>
        /// Split a stack from one slot to an empty destination slot.
        ///
        /// IMPORTANT FIX:
        /// - The split-off stack MUST be a new ItemInstance with a NEW instanceId.
        /// - We use DeepCloneNewIdentity() to avoid identity collisions.
        /// </summary>
        public InventoryOpResult TrySplit(int fromSlot, int toSlot, int splitAmount, ItemDefCatalog catalog)
        {
            if (!IsValidSlot(fromSlot) || !IsValidSlot(toSlot))
                return InventoryOpResult.Failed;

            if (fromSlot == toSlot)
                return InventoryOpResult.Failed;

            var src = _slots[fromSlot].item;
            if (src == null)
                return InventoryOpResult.Failed;

            if (_slots[toSlot].item != null)
                return InventoryOpResult.Failed;

            if (catalog == null || !catalog.TryGet(src.itemDefId, out var def) || def == null)
                return InventoryOpResult.UnknownItemDef;

            // Only stackable items can be split.
            if (!def.isStackable)
                return InventoryOpResult.Failed;

            // Validate split amount.
            if (splitAmount <= 0)
                return InventoryOpResult.Failed;

            if (src.stackCount <= 1)
                return InventoryOpResult.Failed;

            if (splitAmount >= src.stackCount)
                return InventoryOpResult.Failed;

            // NEW ID for the split stack.
            ItemInstance split = src.DeepCloneNewIdentity();

            split.stackCount = splitAmount;
            src.stackCount -= splitAmount;

            _slots[toSlot].item = split;
            NotifyFullRefresh();
            return InventoryOpResult.Success;
        }

        public InventoryOpResult TryRemoveAt(int slot, out ItemInstance removed)
        {
            removed = null;

            if (!IsValidSlot(slot))
                return InventoryOpResult.Failed;

            if (_slots[slot].item == null)
                return InventoryOpResult.Failed;

            removed = _slots[slot].item;
            _slots[slot].item = null;

            NotifyFullRefresh();
            return InventoryOpResult.Success;
        }

        private bool IsValidSlot(int slot) => slot >= 0 && slot < _slots.Length;
    }
}
