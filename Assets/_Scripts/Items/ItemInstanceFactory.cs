using System;
using System.Collections.Generic;
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

            if (!CanRollAffixes(def))
            {
                LogLootResult(instance, Array.Empty<AffixInstance>());
                return true;
            }

            int desiredCount = AffixCountResolver.ResolveForLoot(lootRarity, rng);
            desiredCount = AffixCountResolver.ClampToGlobal(desiredCount);

            if (desiredCount <= 0)
            {
                LogLootResult(instance, Array.Empty<AffixInstance>());
                return true;
            }

            var pools = ResolvePools(def.affixPoolRefs);
            if (pools.Count == 0)
            {
                LogLootResult(instance, Array.Empty<AffixInstance>());
                return true;
            }

            var rolledAffixes = RollAffixes(def, pools, desiredCount, rng);
            if (rolledAffixes.Count > 0)
            {
                instance.affixes.AddRange(rolledAffixes);
            }

            LogLootResult(instance, rolledAffixes);
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

        private List<AffixInstance> RollAffixes(ItemDef def, List<AffixPool> pools, int desiredCount, System.Random rng)
        {
            var result = new List<AffixInstance>(desiredCount);
            var used = new HashSet<AffixId>();
            var fallbackEligibility = ResolveEligibility(def);
            int remaining = desiredCount;

            for (int i = 0; i < pools.Count; i++)
            {
                if (remaining <= 0)
                    break;

                var pool = pools[i];
                var requiredEligibility = pool.intendedEligibility != AffixEligibility.Any
                    ? pool.intendedEligibility
                    : fallbackEligibility;

                var rolled = AffixPicker.PickAndRoll(affixCatalog, pool, remaining, requiredEligibility, rng);
                for (int j = 0; j < rolled.Count && remaining > 0; j++)
                {
                    if (used.Add(rolled[j].id))
                    {
                        result.Add(rolled[j]);
                        remaining--;
                    }
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
                case ItemDef.ItemFamily.Mainhand:
                    return AffixEligibility.Weapon;
                case ItemDef.ItemFamily.Head:
                case ItemDef.ItemFamily.Chest:
                case ItemDef.ItemFamily.Foot:
                    return AffixEligibility.Armor;
                case ItemDef.ItemFamily.Offhand:
                    return AffixEligibility.Shield;
                case ItemDef.ItemFamily.Neck:
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

        private static void LogLootResult(ItemInstance instance, IReadOnlyList<AffixInstance> affixes)
        {
            string affixList = affixes == null || affixes.Count == 0
                ? "(none)"
                : string.Join(", ", GetAffixIds(affixes));

            Debug.Log($"[ItemInstanceFactory] Spawned loot '{instance.itemDefId}' | affixes={instance.AffixCount} | ids={affixList}");
        }

        private static IEnumerable<string> GetAffixIds(IReadOnlyList<AffixInstance> affixes)
        {
            for (int i = 0; i < affixes.Count; i++)
                yield return affixes[i].id.ToString();
        }
    }
}
