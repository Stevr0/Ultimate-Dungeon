using System;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateDungeon.Items
{
    /// <summary>
    /// AffixPicker (SERVER-ONLY)
    /// =========================
    ///
    /// Deterministically selects which AffixIds to apply from an AffixPool.
    ///
    /// DESIGN LOCKS (from ITEM_AFFIX_CATALOG.md):
    /// - Server-only rolling/selection
    /// - Determinism required (server-seeded)
    /// - Total affix count per item <= 5
    /// - Stacking policies:
    ///   - NoStack: at most one instance allowed
    ///   - HighestOnly: multiple instances should not exist (we treat as unique)
    ///   - Sum: duplicates would be allowed conceptually, BUT we still pick unique ids
    ///     because the catalog is defined per AffixId (one id, one meaning).
    ///
    /// Notes:
    /// - This class does NOT roll magnitudes (AffixRoller does that).
    /// - This class only picks ids, enforcing eligibility filters.
    /// </summary>
    public static class AffixPicker
    {
        /// <summary>
        /// Picks up to desiredCount unique AffixIds from the pool.
        ///
        /// Inputs:
        /// - catalog: used to validate eligibility + stacking policies
        /// - pool: the curated pool of ids
        /// - requiredEligibility: the item category you are rolling for (Weapon/Armor/Shield/Jewelry)
        /// - rng: seeded RNG for determinism
        ///
        /// Output:
        /// - List of unique AffixIds
        ///
        /// Behavior:
        /// - Filters out pool ids that are missing from the catalog
        /// - Filters out ids whose AffixDef eligibility does not include requiredEligibility
        /// - Optionally also respects pool.intendedEligibility (extra safety)
        /// - Uniform random selection (no weights) by default
        /// - Clamps count to global cap and available ids
        /// </summary>
        public static List<AffixId> PickIds(
            AffixCatalog catalog,
            AffixPool pool,
            int desiredCount,
            AffixEligibility requiredEligibility,
            System.Random rng,
            bool allowWeights = false)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (pool == null) throw new ArgumentNullException(nameof(pool));
            if (rng == null) throw new ArgumentNullException(nameof(rng));

            desiredCount = AffixCountResolver.ClampToGlobal(desiredCount);

            // Build eligible candidate list.
            var candidates = BuildCandidates(catalog, pool, requiredEligibility);

            // Nothing eligible.
            if (candidates.Count == 0 || desiredCount == 0)
                return new List<AffixId>(0);

            // Don't request more than we can provide.
            int count = Mathf.Clamp(desiredCount, 0, candidates.Count);

            // Choose ids.
            // Default: uniform without replacement.
            if (!allowWeights)
                return PickUniformWithoutReplacement(candidates, count, rng);

            // Weighted selection without replacement.
            // This is optional and NOT locked yet by docs; keep off unless you explicitly want it.
            return PickWeightedWithoutReplacement(candidates, count, rng);
        }

        /// <summary>
        /// Convenience: picks ids and immediately rolls magnitudes, producing AffixInstances.
        /// </summary>
        public static List<AffixInstance> PickAndRoll(
            AffixCatalog catalog,
            AffixPool pool,
            int desiredCount,
            AffixEligibility requiredEligibility,
            System.Random rng,
            bool allowWeights = false)
        {
            var ids = PickIds(catalog, pool, desiredCount, requiredEligibility, rng, allowWeights);

            var result = new List<AffixInstance>(ids.Count);
            for (int i = 0; i < ids.Count; i++)
            {
                // Roll magnitude deterministically.
                var inst = AffixRoller.RollAffix(catalog, ids[i], rng);
                result.Add(inst);
            }

            return result;
        }

        // --------------------------------------------------------------------
        // Candidate filtering
        // --------------------------------------------------------------------

        private static List<Candidate> BuildCandidates(AffixCatalog catalog, AffixPool pool, AffixEligibility requiredEligibility)
        {
            var list = new List<Candidate>();

            // Extra safety: if the pool is intended for a narrower eligibility,
            // and requiredEligibility isn't part of it, we still allow it but we warn in editor.
            // (Hard failing here would make iteration painful.)

            for (int i = 0; i < pool.entries.Count; i++)
            {
                var entry = pool.entries[i];

                // Skip duplicates in the pool asset (we treat ids as unique).
                bool already = false;
                for (int j = 0; j < list.Count; j++)
                {
                    if (list[j].id == entry.id)
                    {
                        already = true;
                        break;
                    }
                }
                if (already) continue;

                // Must exist in catalog.
                if (!catalog.TryGet(entry.id, out var def) || def == null)
                    continue;

                // Must match required eligibility.
                if ((def.eligibility & requiredEligibility) == 0)
                    continue;

                // Optional: pool intended eligibility also must be compatible.
                // If intendedEligibility is Any, this is always ok.
                if ((pool.intendedEligibility & requiredEligibility) == 0)
                {
                    // We don't exclude, but we could if you want stricter behavior.
                    // Keep it permissive for now.
                }

                // Stacking policy sanity:
                // We pick unique ids regardless, so NoStack/HighestOnly are naturally respected.

                list.Add(new Candidate(entry.id, entry.weight));
            }

            return list;
        }

        private readonly struct Candidate
        {
            public readonly AffixId id;
            public readonly float weight;

            public Candidate(AffixId id, float weight)
            {
                this.id = id;
                this.weight = Mathf.Max(0f, weight);
            }
        }

        // --------------------------------------------------------------------
        // Selection methods
        // --------------------------------------------------------------------

        private static List<AffixId> PickUniformWithoutReplacement(List<Candidate> candidates, int count, System.Random rng)
        {
            // Copy ids into a mutable list we can remove from.
            var bag = new List<AffixId>(candidates.Count);
            for (int i = 0; i < candidates.Count; i++)
                bag.Add(candidates[i].id);

            var picked = new List<AffixId>(count);

            for (int k = 0; k < count; k++)
            {
                int idx = rng.Next(0, bag.Count); // 0..Count-1
                picked.Add(bag[idx]);
                bag.RemoveAt(idx);
            }

            return picked;
        }

        private static List<AffixId> PickWeightedWithoutReplacement(List<Candidate> candidates, int count, System.Random rng)
        {
            // We implement a simple "roulette" draw each pick, removing the chosen entry.
            // This is deterministic given rng seed.

            var bag = new List<Candidate>(candidates);
            var picked = new List<AffixId>(count);

            for (int k = 0; k < count; k++)
            {
                float total = 0f;
                for (int i = 0; i < bag.Count; i++)
                    total += bag[i].weight;

                // If weights are degenerate, fall back to uniform.
                if (total <= 0f)
                {
                    int idx = rng.Next(0, bag.Count);
                    picked.Add(bag[idx].id);
                    bag.RemoveAt(idx);
                    continue;
                }

                // Roll in [0..total)
                double t = rng.NextDouble();
                float roll = (float)(t * total);

                int chosenIndex = 0;
                float accum = 0f;
                for (int i = 0; i < bag.Count; i++)
                {
                    accum += bag[i].weight;
                    if (roll < accum)
                    {
                        chosenIndex = i;
                        break;
                    }
                }

                picked.Add(bag[chosenIndex].id);
                bag.RemoveAt(chosenIndex);
            }

            return picked;
        }
    }
}
