// ============================================================================
// InventorySlotData.cs
// ----------------------------------------------------------------------------
// Value-type data container for a single inventory slot.
// ============================================================================
using System;

namespace UltimateDungeon.Items
{
    [Serializable]
    public struct InventorySlotData
    {
        public ItemInstance item;

        // True if slot has no item
        public bool IsEmpty => item == null;

        public bool HasItem => item != null;

        public void Clear() => item = null;
    }
}
