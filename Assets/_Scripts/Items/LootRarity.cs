namespace UltimateDungeon.Items
{
    /// <summary>
    /// LootRarity
    /// ==========
    ///
    /// Rarity tiers used by the (v1 placeholder) loot affix count rules.
    ///
    /// Source of truth:
    /// - ITEM_AFFIX_CATALOG.md
    ///   Section: "Loot Drops (Dungeon) — Affix Count" (PROPOSED / Not Locked)
    ///
    /// IMPORTANT:
    /// - Exact rarity definitions and drop tables will likely be owned by a future Loot doc.
    /// - Keep this enum stable once referenced by loot generation.
    /// </summary>
    public enum LootRarity
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4,
    }
}
