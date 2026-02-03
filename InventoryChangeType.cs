using System;

namespace UltimateDungeon.Items
{
    /// <summary>
    /// InventoryChangeType
    /// ===================
    ///
    /// UI + systems need to know *what kind* of change happened to a slot.
    /// This keeps refresh work minimal (later), but for now it's mainly debug-friendly.
    ///
    /// IMPORTANT:
    /// - Append-only once you start relying on numeric values for persistence/networking.
    /// - For now, we keep it simple and human-readable.
    /// </summary>
    public enum InventoryChangeType
    {
        /// <summary>No meaningful change (usually not fired).</summary>
        None = 0,

        /// <summary>No specific type (fallback).</summary>
        Unknown = 1,

        /// <summary>A new item was placed into an empty slot.</summary>
        ItemAdded = 10,

        /// <summary>An item was removed from the slot (slot became empty).</summary>
        ItemRemoved = 20,

        /// <summary>The slot still contains an item, but the item reference changed (swap/move).</summary>
        ItemChanged = 30,

        /// <summary>The slot's stack count changed (merge/split/stack add).</summary>
        StackChanged = 40,

        /// <summary>Durability values changed.</summary>
        DurabilityChanged = 50,

        /// <summary>Affixes changed (re-rolled, added, removed).</summary>
        AffixesChanged = 60,

        /// <summary>
        /// The UI should refresh ALL slots.
        /// (Useful during initial bind or after load.)
        /// </summary>
        FullRefresh = 100,
    }
}
