using System;

namespace UltimateDungeon.Items
{
    /// <summary>
    /// InventoryEnums
    /// ==============
    ///
    /// Central place for the small enums used by the runtime inventory model.
    ///
    /// Why this exists:
    /// - We had compile errors because different scripts referenced enum values
    ///   that didn't exist yet.
    /// - Keeping enums in ONE file makes refactors safer.
    ///
    /// Notes:
    /// - These enums are UI/runtime friendly.
    /// - Networking replication is NOT implemented yet.
    /// </summary>

    /// <summary>
    /// High-level outcome for an inventory operation.
    ///
    /// IMPORTANT:
    /// - Multiple names can map to the same value (aliases) so older scripts
    ///   that used a different name (e.g. Success vs Ok) keep compiling.
    /// </summary>
    public enum InventoryOpResult
    {
        // Success aliases (keep both to avoid breaking older code).
        Ok = 0,
        Success = 0,

        Failed = 1,

        // Slot / bounds issues
        InvalidSlot = 10,
        EmptySlot = 11,
        TargetNotEmpty = 12,

        // Capacity / placement
        InventoryFull = 20,

        // Item definition / catalog problems
        UnknownItemDef = 30,

        // Stack rules
        NotStackable = 40,
        InvalidAmount = 41,
        NotEnoughInStack = 42,
    }
}
