using System;
using UltimateDungeon.Progression;
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
        /// Deterministic magnitude roll sourced from a seed. This is the preferred
        /// server-authoritative entry point for loot generation.
        /// </summary>
        public static float RollMagnitude(AffixDef def, uint seed, string itemDefId = null, bool debugLogs = false)
        {
            var rng = new DeterministicRng(unchecked((int)seed));

            if (debugLogs)
            {
                Debug.Log($"[AffixRoller][SeedTrace] itemDefId={itemDefId ?? "(unknown)"} seed={seed}");
            }

            return RollMagnitude(def, ref rng);
        }

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
        private static float RollMagnitude(AffixDef def, ref DeterministicRng rng)
        {
            if (def == null) throw new ArgumentNullException(nameof(def));

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
                int rolled = rng.NextInt(imin, imax + 1);
                value = rolled;
            }
            else
            {
                // Float roll: [min..max)
                // (We can treat it as inclusive enough for continuous values.)
                float t = rng.NextFloat01(); // 0..1
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
        public static AffixInstance RollAffix(AffixCatalog catalog, AffixId id, uint seed, string itemDefId = null, bool debugLogs = false)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));

            var def = catalog.GetOrThrow(id);
            float mag = RollMagnitude(def, seed, itemDefId, debugLogs);

            return new AffixInstance(id, mag);
        }

        /// <summary>
        /// Backward-compatible overload retained for non-loot callsites that already
        /// hold a seeded System.Random stream.
        /// </summary>
        public static AffixInstance RollAffix(AffixCatalog catalog, AffixId id, System.Random rng)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (rng == null) throw new ArgumentNullException(nameof(rng));

            var def = catalog.GetOrThrow(id);
            float min = Mathf.Max(0f, def.min);
            float max = Mathf.Max(0f, def.max);

            if (max < min)
                max = min;

            float mag;
            if (Mathf.Approximately(min, max))
            {
                mag = def.integerMagnitude ? Mathf.Round(min) : min;
            }
            else if (def.integerMagnitude)
            {
                int imin = Mathf.CeilToInt(min);
                int imax = Mathf.FloorToInt(max);
                if (imax < imin)
                    imax = imin;
                mag = rng.Next(imin, imax + 1);
            }
            else
            {
                mag = (float)(min + (max - min) * rng.NextDouble());
            }

            mag = def.Clamp(mag);
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
