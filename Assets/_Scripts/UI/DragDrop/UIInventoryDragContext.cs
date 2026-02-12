// ============================================================================
// UIInventoryDragContext.cs - Inventory + Equipment + Corpse loot drag context
// ----------------------------------------------------------------------------
// Purpose:
// - Shared static context for the current UGUI drag operation.
// - Supports dragging FROM:
//   - Inventory slot
//   - Equipment slot
//   - Corpse loot entry (read-only source)
//
// Notes:
// - MVP approach: static context is simple and reliable.
// - Corpse loot drags never move items locally; they only trigger a server take
//   request when dropped on a valid target.
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
            LootEntry = 3,
        }

        public static DragKind Kind { get; private set; } = DragKind.None;

        // Inventory source
        public static int SourceInventorySlot { get; private set; } = -1;

        // Equipment source
        public static EquipmentSlotId SourceEquipmentSlot { get; private set; } = (EquipmentSlotId)(-1);

        // Corpse loot source (take-only)
        public static ulong SourceCorpseNetId { get; private set; }
        public static string SourceLootInstanceId { get; private set; } = string.Empty;
        public static string SourceLootItemDefId { get; private set; } = string.Empty;

        public static void BeginInventorySlotDrag(int sourceSlot)
        {
            Kind = DragKind.InventorySlot;
            SourceInventorySlot = sourceSlot;
            SourceEquipmentSlot = (EquipmentSlotId)(-1);
            SourceCorpseNetId = 0;
            SourceLootInstanceId = string.Empty;
            SourceLootItemDefId = string.Empty;
        }

        public static void BeginEquipmentSlotDrag(EquipmentSlotId sourceSlot)
        {
            Kind = DragKind.EquipmentSlot;
            SourceInventorySlot = -1;
            SourceEquipmentSlot = sourceSlot;
            SourceCorpseNetId = 0;
            SourceLootInstanceId = string.Empty;
            SourceLootItemDefId = string.Empty;
        }

        public static void BeginLootEntryDrag(ulong sourceCorpseNetId, string instanceId, string itemDefId)
        {
            Kind = DragKind.LootEntry;
            SourceInventorySlot = -1;
            SourceEquipmentSlot = (EquipmentSlotId)(-1);
            SourceCorpseNetId = sourceCorpseNetId;
            SourceLootInstanceId = instanceId ?? string.Empty;
            SourceLootItemDefId = itemDefId ?? string.Empty;
        }

        public static void Clear()
        {
            Kind = DragKind.None;
            SourceInventorySlot = -1;
            SourceEquipmentSlot = (EquipmentSlotId)(-1);
            SourceCorpseNetId = 0;
            SourceLootInstanceId = string.Empty;
            SourceLootItemDefId = string.Empty;
        }
    }
}
