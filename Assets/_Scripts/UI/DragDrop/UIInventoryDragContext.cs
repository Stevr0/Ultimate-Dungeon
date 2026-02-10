// ============================================================================
// UIInventoryDragContext.cs — v2 (Inventory + Equipment drag)
// ----------------------------------------------------------------------------
// Purpose:
// - Shared static context for current UI drag operation.
// - Supports dragging FROM:
//   - Inventory slot
//   - Equipment slot
//
// Notes:
// - MVP approach: static context is simplest and reliable.
// - Later you can replace with a proper UI service.
// ============================================================================

namespace UltimateDungeon.UI
{
    public static class UIInventoryDragContext
    {
        public enum DragKind
        {
            None = 0,
            InventorySlot = 1,
            EquipmentSlot = 2,
        }

        public static DragKind Kind { get; private set; } = DragKind.None;

        // Inventory source
        public static int SourceInventorySlot { get; private set; } = -1;

        // Equipment source
        public static EquipmentSlotId SourceEquipmentSlot { get; private set; } = (EquipmentSlotId)(-1);

        public static void BeginInventorySlotDrag(int sourceSlot)
        {
            Kind = DragKind.InventorySlot;
            SourceInventorySlot = sourceSlot;
            SourceEquipmentSlot = (EquipmentSlotId)(-1);
        }

        public static void BeginEquipmentSlotDrag(EquipmentSlotId sourceSlot)
        {
            Kind = DragKind.EquipmentSlot;
            SourceInventorySlot = -1;
            SourceEquipmentSlot = sourceSlot;
        }

        public static void Clear()
        {
            Kind = DragKind.None;
            SourceInventorySlot = -1;
            SourceEquipmentSlot = (EquipmentSlotId)(-1);
        }
    }
}
