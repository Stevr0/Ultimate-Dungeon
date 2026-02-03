using System;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateDungeon.Items
{
    /// <summary>
    /// AffixCatalog (AUTHORITATIVE LIST)
    /// =================================
    ///
    /// Holds all AffixDef assets used by the game.
    ///
    /// Why a catalog asset?
    /// - Avoids Resources.Load / FindObjectsOfType scans
    /// - Lets you validate: duplicates, missing ids, range mistakes
    /// - Provides fast lookup by AffixId at runtime
    ///
    /// Notes:
    /// - The catalog is DESIGN DATA.
    /// - Runtime item instances should only store AffixId + magnitude.
    /// </summary>
    [CreateAssetMenu(menuName = "Ultimate Dungeon/Items/Affix Catalog", fileName = "AffixCatalog")]
    public sealed class AffixCatalog : ScriptableObject
    {
        [Tooltip("All affix definitions. One per AffixId.")]
        public List<AffixDef> defs = new List<AffixDef>();

        // Runtime lookup cache (not serialized)
        [NonSerialized] private Dictionary<AffixId, AffixDef> _byId;

        /// <summary>
        /// Get an AffixDef by id. Returns false if missing.
        /// </summary>
        public bool TryGet(AffixId id, out AffixDef def)
        {
            EnsureCache();
            return _byId.TryGetValue(id, out def);
        }

        /// <summary>
        /// Get an AffixDef by id or throw a helpful error.
        /// Use sparingly (prefer TryGet in production code).
        /// </summary>
        public AffixDef GetOrThrow(AffixId id)
        {
            EnsureCache();
            if (_byId.TryGetValue(id, out var def) && def != null)
                return def;

            throw new KeyNotFoundException($"AffixCatalog is missing AffixDef for id '{id}'.");
        }

        /// <summary>
        /// Returns a snapshot of all defs (may include nulls if the list is dirty).
        /// </summary>
        public IReadOnlyList<AffixDef> All => defs;

        private void EnsureCache()
        {
            if (_byId != null)
                return;

            _byId = new Dictionary<AffixId, AffixDef>();

            for (int i = 0; i < defs.Count; i++)
            {
                var def = defs[i];
                if (def == null)
                    continue;

                // If duplicates exist, keep the first and ignore later.
                // The editor validator will flag duplicates.
                if (_byId.ContainsKey(def.id))
                    continue;

                _byId.Add(def.id, def);
            }
        }

        private void OnEnable()
        {
            // Rebuild cache when entering play mode / domain reload.
            _byId = null;
        }
    }
}
