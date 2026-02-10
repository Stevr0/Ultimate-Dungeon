// ============================================================================
// InventoryRuntimeModel.cs  v2
// ----------------------------------------------------------------------------
// Adds targeted placement API needed for:
// - Equipment -> Inventory into a selected slot
//
// New API:
// - TryPlaceIntoEmptySlot(toSlot, item)
//
// IMPORTANT:
// - This model remains pure C# runtime state.
// - Server-only mutation enforced by the component that owns this model.
// ============================================================================

using System;
using System.Collections.Generic;

namespace UltimateDungeon.Items
{
    [Serializable]
    public sealed class InventoryRuntimeModel
    {
        public const int DefaultSlotCount = 48;

        private readonly InventorySlotData[] _slots;

        public event Action<InventoryChangeType, int> OnChanged;

        public int SlotCount => _slots.Length;
        public IReadOnlyList<InventorySlotData> Slots => _slots;

        public InventoryRuntimeModel(int slotCount = DefaultSlotCount)
        {
            slotCount = Math.Max(0, slotCount);
            _slots = new InventorySlotData[slotCount];
        }

        // --------------------------------------------------------------------
        // Queries
        // --------------------------------------------------------------------

        public InventorySlotData GetSlot(int slotIndex)
        {
            if (!IsValidSlot(slotIndex))
                return default;

            return _slots[slotIndex];
        }

        public bool IsValidSlot(int slotIndex) => slotIndex >= 0 && slotIndex < _slots.Length;

        // --------------------------------------------------------------------
        // Mutations
        // --------------------------------------------------------------------

        public InventoryOpResult TryAdd(ItemInstance item, ItemDefCatalog itemDefCatalog, out int placedSlot)
        {
            placedSlot = -1;

            if (item == null)
                return InventoryOpResult.Failed;

            if (itemDefCatalog == null)
                return InventoryOpResult.UnknownItemDef;

            if (!itemDefCatalog.TryGet(item.itemDefId, out var def) || def == null)
                return InventoryOpResult.UnknownItemDef;

            // 1) Try stack into existing stack first.
            if (def.isStackable)
            {
                for (int i = 0; i < _slots.Length; i++)
                {
                    if (_slots[i].IsEmpty) continue;

                    var existing = _slots[i].item;
                    if (existing == null) continue;
                    if (!string.Equals(existing.itemDefId, item.itemDefId, StringComparison.Ordinal))
                        continue;

                    int max = Math.Max(1, def.stackMax);
                    if (existing.stackCount >= max) continue;

                    int canTake = max - existing.stackCount;
                    int toMove = Math.Min(canTake, Math.Max(1, item.stackCount));

                    existing.stackCount += toMove;
                    item.stackCount -= toMove;

                    _slots[i].item = existing;
                    OnChanged?.Invoke(InventoryChangeType.StackChanged, i);

                    if (item.stackCount <= 0)
                    {
                        placedSlot = i;
                        return InventoryOpResult.Success;
                    }

                    // If remainder exists, we will place it into an empty slot.
                    break;
                }
            }

            // 2) Place remaining item into empty slot.
            for (int i = 0; i < _slots.Length; i++)
            {
                if (!_slots[i].IsEmpty) continue;

                _slots[i].item = item;
                placedSlot = i;

                OnChanged?.Invoke(InventoryChangeType.ItemAdded, i);
                return InventoryOpResult.Success;
            }

            return InventoryOpResult.InventoryFull;
        }

        /// <summary>
        /// Places an item into a specific slot.
        ///
        /// Rules:
        /// - Target slot MUST be empty.
        /// - No auto-stacking.
        /// - This is intended for things like targeted unequip.
        /// </summary>
        public InventoryOpResult TryPlaceIntoEmptySlot(int toSlot, ItemInstance item, ItemDefCatalog itemDefCatalog)
        {
            if (!IsValidSlot(toSlot))
                return InventoryOpResult.InvalidSlot;

            if (item == null)
                return InventoryOpResult.Failed;

            if (!_slots[toSlot].IsEmpty)
                return InventoryOpResult.TargetNotEmpty;

            // Optional: validate the def exists (prevents junk ids).
            if (itemDefCatalog == null)
                return InventoryOpResult.UnknownItemDef;

            if (!itemDefCatalog.TryGet(item.itemDefId, out var def) || def == null)
                return InventoryOpResult.UnknownItemDef;

            _slots[toSlot].item = item;
            OnChanged?.Invoke(InventoryChangeType.ItemAdded, toSlot);
            return InventoryOpResult.Success;
        }

        public InventoryOpResult TryRemoveAt(int slotIndex, out ItemInstance removed)
        {
            removed = null;

            if (!IsValidSlot(slotIndex))
                return InventoryOpResult.InvalidSlot;

            if (_slots[slotIndex].IsEmpty || _slots[slotIndex].item == null)
                return InventoryOpResult.EmptySlot;

            removed = _slots[slotIndex].item;
            _slots[slotIndex].item = null;

            OnChanged?.Invoke(InventoryChangeType.ItemRemoved, slotIndex);
            return InventoryOpResult.Success;
        }

        public InventoryOpResult TryMoveOrSwap(int fromSlot, int toSlot, ItemDefCatalog itemDefCatalog)
        {
            if (!IsValidSlot(fromSlot) || !IsValidSlot(toSlot))
                return InventoryOpResult.InvalidSlot;

            if (_slots[fromSlot].IsEmpty || _slots[fromSlot].item == null)
                return InventoryOpResult.EmptySlot;

            var tmp = _slots[toSlot].item;
            _slots[toSlot].item = _slots[fromSlot].item;
            _slots[fromSlot].item = tmp;

            OnChanged?.Invoke(InventoryChangeType.ItemChanged, fromSlot);
            OnChanged?.Invoke(InventoryChangeType.ItemChanged, toSlot);

            return InventoryOpResult.Success;
        }

        public InventoryOpResult TrySplit(int fromSlot, int toSlot, int splitAmount, ItemDefCatalog itemDefCatalog)
        {
            if (!IsValidSlot(fromSlot) || !IsValidSlot(toSlot))
                return InventoryOpResult.InvalidSlot;

            if (_slots[fromSlot].IsEmpty || _slots[fromSlot].item == null)
                return InventoryOpResult.EmptySlot;

            if (!_slots[toSlot].IsEmpty)
                return InventoryOpResult.TargetNotEmpty;

            if (splitAmount <= 0)
                return InventoryOpResult.InvalidAmount;

            var src = _slots[fromSlot].item;
            if (src == null) return InventoryOpResult.EmptySlot;

            if (itemDefCatalog == null || !itemDefCatalog.TryGet(src.itemDefId, out var def) || def == null)
                return InventoryOpResult.UnknownItemDef;

            if (!def.isStackable)
                return InventoryOpResult.NotStackable;

            if (src.stackCount <= 1)
                return InventoryOpResult.NotEnoughInStack;

            if (splitAmount >= src.stackCount)
                return InventoryOpResult.InvalidAmount;

            src.stackCount -= splitAmount;

            // Split-off stack is a new item entity and must get a distinct instanceId.
            var split = src.DeepCloneNewIdentity();
            split.stackCount = splitAmount;

            _slots[fromSlot].item = src;
            _slots[toSlot].item = split;

            OnChanged?.Invoke(InventoryChangeType.StackChanged, fromSlot);
            OnChanged?.Invoke(InventoryChangeType.ItemAdded, toSlot);

            return InventoryOpResult.Success;
        }

        public void NotifyFullRefresh()
        {
            OnChanged?.Invoke(InventoryChangeType.FullRefresh, -1);
        }
    }
}
