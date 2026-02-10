using System;
using UnityEngine;

namespace UltimateDungeon.Items
{
    /// <summary>
    /// AffixCountResolver (AUTHORITATIVE COUNT MATH)
    /// ============================================
    ///
    /// Determines how many affixes an item instance should roll.
    ///
    /// Source of truth:
    /// - ITEM_AFFIX_CATALOG.md
    ///
    /// DESIGN LOCKS (must enforce):
    /// - Global cap: affixCount <= 5 always
    /// - Loot drops: affixCount in range 0..5
    /// - Enhancement: affixCount in range 0..Nmax(skill)
    /// - Nmax(skill) table is LOCKED
    /// - "No duplication of affix-count math" => all systems should call this
    ///
    /// Determinism:
    /// - This resolver can be used deterministically by passing a seeded System.Random.
    /// - Do NOT use UnityEngine.Random in authoritative/server logic.
    /// </summary>
    public static class AffixCountResolver
    {
        /// <summary>
        /// Hard cap enforced by design.
        /// </summary>
        public const int GlobalAffixCap = 5;

        // --------------------------------------------------------------------
        // A) Loot Drops (Dungeon) — Affix Count
        // --------------------------------------------------------------------
        // v1 placeholder ranges (PROPOSED — Not Locked):
        // - Common:     0–1
        // - Uncommon:   1–2
        // - Rare:       2–3
        // - Epic:       3–4
        // - Legendary:  4–5
        //
        // NOTE:
        // - Exact rarity definitions and distributions will be owned by a future Loot doc.
        // - For now we implement the placeholder exactly as written.

        /// <summary>
        /// Resolves affix count for dungeon loot, based on rarity.
        ///
        /// Behavior:
        /// - Picks an integer uniformly within the rarity's inclusive range.
        /// - Clamps to 0..5.
        /// </summary>
        public static int ResolveForLoot(LootRarity rarity, System.Random rng)
        {
            if (rng == null) throw new ArgumentNullException(nameof(rng));

            (int min, int max) = GetLootRange(rarity);

            // Uniform inclusive roll.
            int count = rng.Next(min, max + 1);

            return ClampToGlobal(count);
        }

        /// <summary>
        /// Returns the inclusive range for a rarity tier.
        /// </summary>
        public static (int min, int max) GetLootRange(LootRarity rarity)
        {
            return rarity switch
            {
                LootRarity.Common => (0, 1),
                LootRarity.Uncommon => (1, 2),
                LootRarity.Rare => (2, 3),
                LootRarity.Epic => (3, 4),
                LootRarity.Legendary => (4, 5),
                _ => (0, 1)
            };
        }

        // --------------------------------------------------------------------
        // B) Enhancement (Crafting) — Affix Count
        // --------------------------------------------------------------------
        // LOCKED table:
        // 0–19.9   => 0
        // 20–39.9  => 1
        // 40–59.9  => 2
        // 60–79.9  => 3
        // 80–99.9  => 4
        // 100.0    => 5

        /// <summary>
        /// Returns Nmax(skill) according to the LOCKED table.
        ///
        /// Input:
        /// - skill: expected 0..100, but we clamp defensively.
        ///
        /// Output:
        /// - integer 0..5
        /// </summary>
        public static int GetEnhancementNmax(float skill)
        {
            // Defensive clamp to expected skill range.
            skill = Mathf.Clamp(skill, 0f, 100f);

            // Important: 100.0 is a special case.
            if (Mathf.Approximately(skill, 100f))
                return 5;

            // Use upper-exclusive ranges as written (e.g., 19.9, 39.9, ...).
            // We implement them as < 20, < 40, < 60, < 80, < 100.
            if (skill < 20f) return 0;
            if (skill < 40f) return 1;
            if (skill < 60f) return 2;
            if (skill < 80f) return 3;
            return 4; // 80..99.999
        }

        /// <summary>
        /// Resolves affix count for enhancement.
        ///
        /// Rule (LOCKED):
        /// - Enhancement rolls affixCount within 0..Nmax(skill)
        ///
        /// Distribution:
        /// - The doc allows uniform OR weighted depending on the enhancement spec.
        /// - For now, we implement UNIFORM, because it's deterministic and simple.
        ///   If you later add weighting, change ONLY here.
        /// </summary>
        public static int ResolveForEnhancement(float skill, System.Random rng)
        {
            if (rng == null) throw new ArgumentNullException(nameof(rng));

            int nmax = GetEnhancementNmax(skill);

            // Uniform inclusive roll 0..nmax.
            int count = rng.Next(0, nmax + 1);

            return ClampToGlobal(count);
        }

        // --------------------------------------------------------------------
        // Helpers
        // --------------------------------------------------------------------

        /// <summary>
        /// Global clamp for safety.
        /// </summary>
        public static int ClampToGlobal(int count)
        {
            if (count < 0) return 0;
            if (count > GlobalAffixCap) return GlobalAffixCap;
            return count;
        }
    }
}
