using System;
using UnityEngine;

namespace UltimateDungeon.Items
{
    /// <summary>
    /// AffixRoller (SERVER-ONLY)
    /// ========================
    ///
    /// Rolls magnitudes for affixes using a deterministic RNG.
    ///
    /// DESIGN LOCKS (from ITEM_AFFIX_CATALOG.md):
    /// - Server-only rolling
    /// - Determinism required (server-seeded)
    /// - Ranges are enforced (0..N unless explicitly stated)
    ///
    /// IMPORTANT:
    /// - This class intentionally does NOT pick which AffixIds to apply.
    ///   It only rolls a magnitude for a chosen AffixId.
    /// - Pool selection + affix count is owned by other systems (loot/enhancement).
    ///
    /// Determinism approach:
    /// - We use System.Random (seeded) for repeatability.
    /// - Do NOT use UnityEngine.Random for authoritative logic.
    /// </summary>
    public static class AffixRoller
    {
        /// <summary>
        /// Rolls a magnitude for the given affix definition.
        ///
        /// Returns:
        /// - float magnitude in the affix's unit system
        ///   (percent values are still stored as 0..N percent units).
        ///
        /// Rules:
        /// - Clamp to [min..max]
        /// - If integerMagnitude is true, round to nearest int
        /// - Uses inclusive range semantics for integer rolls (common RPG expectation)
        /// </summary>
        public static float RollMagnitude(AffixDef def, System.Random rng)
        {
            if (def == null) throw new ArgumentNullException(nameof(def));
            if (rng == null) throw new ArgumentNullException(nameof(rng));

            float min = Mathf.Max(0f, def.min);
            float max = Mathf.Max(0f, def.max);

            if (max < min)
                max = min;

            // If the range is degenerate, return min.
            if (Mathf.Approximately(min, max))
                return def.integerMagnitude ? Mathf.Round(min) : min;

            float value;

            if (def.integerMagnitude)
            {
                // Inclusive integer roll: [ceil(min) .. floor(max)]
                int imin = Mathf.CeilToInt(min);
                int imax = Mathf.FloorToInt(max);

                if (imax < imin)
                    imax = imin;

                // System.Random.Next(maxExclusive) -> so use (imax + 1) for inclusive.
                int rolled = rng.Next(imin, imax + 1);
                value = rolled;
            }
            else
            {
                // Float roll: [min..max)
                // (We can treat it as inclusive enough for continuous values.)
                double t = rng.NextDouble(); // 0..1
                value = (float)(min + (max - min) * t);

                // Optionally, you could quantize if you later want 0.1 steps, etc.
            }

            // Final clamp to be safe.
            value = def.Clamp(value);

            return value;
        }

        /// <summary>
        /// Creates an AffixInstance by rolling magnitude for a specific AffixId.
        ///
        /// Requires:
        /// - An AffixCatalog for lookup
        /// - A seeded RNG for determinism
        /// </summary>
        public static AffixInstance RollAffix(AffixCatalog catalog, AffixId id, System.Random rng)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (rng == null) throw new ArgumentNullException(nameof(rng));

            var def = catalog.GetOrThrow(id);
            float mag = RollMagnitude(def, rng);

            return new AffixInstance(id, mag);
        }

        /// <summary>
        /// Sanitizes an incoming instance against the catalog.
        ///
        /// Use cases:
        /// - Loading saves
        /// - Receiving network state (if you ever allow client cache)
        ///
        /// Behavior:
        /// - If def missing -> returns false
        /// - Else clamps magnitude and rounds if integerMagnitude
        /// </summary>
        public static bool TrySanitize(AffixCatalog catalog, ref AffixInstance inst)
        {
            if (catalog == null) return false;

            if (!catalog.TryGet(inst.id, out var def) || def == null)
                return false;

            float v = def.Clamp(inst.magnitude);
            if (def.integerMagnitude)
                v = Mathf.Round(v);

            inst.magnitude = v;
            return true;
        }
    }
}
