using System;
using System.Collections.Generic;
using UltimateDungeon.Progression;
using UnityEngine;

namespace UltimateDungeon.Items
{
    /// <summary>
    /// ItemInstanceFactory (SERVER-ONLY)
    /// ================================
    ///
    /// Creates ItemInstances from ItemDefs and rolls affixes deterministically.
    ///
    /// Rules:
    /// - Server-only creation + RNG
    /// - Must call ItemInstance.InitFromDef(def)
    /// - Affix pools are sourced from ItemDef.affixPoolRefs
    /// - Affix count must respect AffixCountResolver.GlobalAffixCap
    /// </summary>
    [CreateAssetMenu(menuName = "Ultimate Dungeon/Items/Item Instance Factory", fileName = "ItemInstanceFactory")]
    public sealed class ItemInstanceFactory : ScriptableObject
    {
        [Header("Catalogs")]
        [SerializeField] private ItemDefCatalog itemDefCatalog;
        [SerializeField] private AffixCatalog affixCatalog;

        [Header("Affix Pools")]
        [Tooltip("All AffixPool assets that may be referenced by ItemDef.affixPoolRefs.")]
        [SerializeField] private List<AffixPool> affixPools = new List<AffixPool>();

        [Header("Loot (Placeholder)")]
        [Tooltip("Temporary rarity used for loot affix count until a real loot system exists.")]
        [SerializeField] private LootRarity lootRarity = LootRarity.Uncommon;
        [SerializeField] private bool debugLogs;

        private Dictionary<string, AffixPool> _poolByName;

        private void OnEnable()
        {
            RebuildPoolLookup();
        }

        public void RebuildPoolLookup()
        {
            _poolByName = new Dictionary<string, AffixPool>(StringComparer.Ordinal);

            foreach (var pool in affixPools)
            {
                if (pool == null) continue;
                if (_poolByName.ContainsKey(pool.name)) continue;
                _poolByName.Add(pool.name, pool);
            }
        }

        public bool TryCreateLootItem(string itemDefId, uint seed, out ItemInstance instance)
        {
            instance = null;

            if (itemDefCatalog == null)
            {
                Debug.LogError("[ItemInstanceFactory] Missing ItemDefCatalog reference.");
                return false;
            }

            if (affixCatalog == null)
            {
                Debug.LogError("[ItemInstanceFactory] Missing AffixCatalog reference.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(itemDefId))
            {
                Debug.LogWarning("[ItemInstanceFactory] ItemDefId was null or empty.");
                return false;
            }

            if (!itemDefCatalog.TryGet(itemDefId, out var def) || def == null)
            {
                Debug.LogWarning($"[ItemInstanceFactory] Unknown ItemDefId '{itemDefId}'.");
                return false;
            }

            var rng = new System.Random(unchecked((int)seed));

            instance = new ItemInstance();
            instance.InitFromDef(def);
            instance.rollSeed = seed;
            instance.EnsureInstanceId();

            if (debugLogs)
            {
                Debug.Log($"[ItemInstanceFactory][SeedTrace] itemDefId={itemDefId} seed={seed} instanceId={instance.instanceId}");
            }

            if (!CanRollAffixes(def))
            {
                LogLootResult(instance, instance.rollSeed, Array.Empty<AffixInstance>());
                return true;
            }

            int desiredCount = AffixCountResolver.ResolveForLoot(lootRarity, rng);
            desiredCount = AffixCountResolver.ClampToGlobal(desiredCount);

            if (desiredCount <= 0)
            {
                LogLootResult(instance, instance.rollSeed, Array.Empty<AffixInstance>());
                return true;
            }

            var pools = ResolvePools(def.affixPoolRefs);
            if (pools.Count == 0)
            {
                LogLootResult(instance, instance.rollSeed, Array.Empty<AffixInstance>());
                return true;
            }

            var rolledAffixes = RollAffixes(def, pools, desiredCount, seed, rng);
            if (rolledAffixes.Count > 0)
            {
                instance.affixes.AddRange(rolledAffixes);
            }

            LogLootResult(instance, instance.rollSeed, rolledAffixes);
            return true;
        }

        private bool CanRollAffixes(ItemDef def)
        {
            return def.affixPoolRefs != null && def.affixPoolRefs.Length > 0;
        }

        private List<AffixPool> ResolvePools(string[] poolRefs)
        {
            var pools = new List<AffixPool>();

            if (poolRefs == null || poolRefs.Length == 0)
                return pools;

            if (_poolByName == null)
                RebuildPoolLookup();

            for (int i = 0; i < poolRefs.Length; i++)
            {
                var poolRef = poolRefs[i];
                if (string.IsNullOrWhiteSpace(poolRef))
                    continue;

                if (_poolByName != null && _poolByName.TryGetValue(poolRef, out var pool) && pool != null)
                {
                    pools.Add(pool);
                }
                else
                {
                    Debug.LogWarning($"[ItemInstanceFactory] Missing AffixPool reference '{poolRef}'.");
                }
            }

            return pools;
        }

        private List<AffixInstance> RollAffixes(ItemDef def, List<AffixPool> pools, int desiredCount, uint itemSeed, System.Random selectionRng)
        {
            var result = new List<AffixInstance>(desiredCount);
            var used = new HashSet<AffixId>();
            var fallbackEligibility = ResolveEligibility(def);
            int remaining = desiredCount;
            int rollIndex = 0;

            for (int i = 0; i < pools.Count; i++)
            {
                if (remaining <= 0)
                    break;

                var pool = pools[i];
                var requiredEligibility = pool.intendedEligibility != AffixEligibility.Any
                    ? pool.intendedEligibility
                    : fallbackEligibility;

                var pickedIds = AffixPicker.PickIds(affixCatalog, pool, remaining, requiredEligibility, selectionRng);
                for (int j = 0; j < pickedIds.Count && remaining > 0; j++)
                {
                    if (!used.Add(pickedIds[j]))
                        continue;

                    // IMPORTANT seed plumbing:
                    // each affix roll consumes a deterministic sub-seed derived from the
                    // per-item seed + roll index, so magnitude rolls cannot accidentally
                    // drift onto any global/shared RNG stream.
                    uint affixSeed = DeterministicMix(itemSeed, (uint)rollIndex);
                    var rolledAffix = AffixRoller.RollAffix(affixCatalog, pickedIds[j], affixSeed, def.itemDefId, debugLogs);
                    result.Add(rolledAffix);

                    rollIndex++;
                    remaining--;
                }
            }

            return result;
        }

        private AffixEligibility ResolveEligibility(ItemDef def)
        {
            var eligibility = ResolveEligibilityFromPools(def.affixPoolRefs);
            if (eligibility != AffixEligibility.None)
                return eligibility;

            switch (def.family)
            {
                case ItemFamily.Mainhand:
                    return AffixEligibility.Weapon;
                case ItemFamily.Head:
                case ItemFamily.Chest:
                case ItemFamily.Foot:
                    return AffixEligibility.Armor;
                case ItemFamily.Offhand:
                    return AffixEligibility.Shield;
                case ItemFamily.Neck:
                    return AffixEligibility.Jewelry;
                default:
                    return AffixEligibility.Any;
            }
        }

        private AffixEligibility ResolveEligibilityFromPools(string[] poolRefs)
        {
            if (poolRefs == null || poolRefs.Length == 0)
                return AffixEligibility.None;

            var eligibility = AffixEligibility.None;

            for (int i = 0; i < poolRefs.Length; i++)
            {
                var poolRef = poolRefs[i];
                if (string.IsNullOrWhiteSpace(poolRef))
                    continue;

                if (poolRef.IndexOf("Weapon", StringComparison.OrdinalIgnoreCase) >= 0)
                    eligibility |= AffixEligibility.Weapon;
                if (poolRef.IndexOf("Armor", StringComparison.OrdinalIgnoreCase) >= 0
                    || poolRef.IndexOf("Boot", StringComparison.OrdinalIgnoreCase) >= 0
                    || poolRef.IndexOf("Cape", StringComparison.OrdinalIgnoreCase) >= 0)
                    eligibility |= AffixEligibility.Armor;
                if (poolRef.IndexOf("Shield", StringComparison.OrdinalIgnoreCase) >= 0)
                    eligibility |= AffixEligibility.Shield;
                if (poolRef.IndexOf("Jewelry", StringComparison.OrdinalIgnoreCase) >= 0)
                    eligibility |= AffixEligibility.Jewelry;
            }

            return eligibility;
        }


        private static uint DeterministicMix(uint a, uint b)
        {
            unchecked
            {
                uint x = a ^ 0x9E3779B9u;
                x ^= b + 0x85EBCA6Bu + (x << 6) + (x >> 2);
                x ^= x >> 16;
                x *= 0x7FEB352Du;
                x ^= x >> 15;
                x *= 0x846CA68Bu;
                x ^= x >> 16;
                return x;
            }
        }

        private static void LogLootResult(ItemInstance instance, uint seed, IReadOnlyList<AffixInstance> affixes)
        {
            string affixList = affixes == null || affixes.Count == 0
                ? "(none)"
                : string.Join(", ", GetAffixIds(affixes));

            Debug.Log($"[ItemInstanceFactory] Spawned loot '{instance.itemDefId}' | seed={seed} | affixes={instance.AffixCount} | ids={affixList}");
        }

        private static IEnumerable<string> GetAffixIds(IReadOnlyList<AffixInstance> affixes)
        {
            for (int i = 0; i < affixes.Count; i++)
                yield return affixes[i].id.ToString();
        }
    }
}
