// ============================================================================
// EquipmentSlotId.cs — Ultimate Dungeon (UI Contract)
// ----------------------------------------------------------------------------
// Updated to match the AUTHORITATIVE 10-slot equipment model from:
// - ITEM_DEF_SCHEMA.md (v1.5)
// - ITEM_CATALOG.md (v2.1)
//
// IMPORTANT:
// - This is a UI contract for your equipment/paperdoll view.
// - Gameplay equip legality is owned by ItemDef (EquipSlot + ItemFamily rules).
//
// Changes:
// - Cape merged into Neck
// - Potion + Food replaced by BeltA + BeltB
// - Boots renamed to Foot
// - MainHand renamed to Mainhand (to match schema naming)
// ============================================================================

namespace UltimateDungeon.UI
{
    public enum EquipmentSlotId
    {
        Bag = 0,
        Head = 1,
        Neck = 2,
        Mainhand = 3,
        Chest = 4,
        Offhand = 5,
        BeltA = 6,
        BeltB = 7,
        Foot = 8,
        Mount = 9,
    }
}
