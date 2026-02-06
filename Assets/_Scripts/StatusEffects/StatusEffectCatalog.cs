using System;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateDungeon.StatusEffects
{
    /// <summary>
    /// StatusEffectCatalog (AUTHORITATIVE LIST)
    /// ========================================
    ///
    /// Holds all StatusEffectDef assets used by the game.
    /// </summary>
    [CreateAssetMenu(menuName = "Ultimate Dungeon/Status Effects/Status Effect Catalog", fileName = "StatusEffectCatalog")]
    public sealed class StatusEffectCatalog : ScriptableObject
    {
        [Tooltip("All status effect definitions. One per StatusEffectId.")]
        public List<StatusEffectDef> defs = new List<StatusEffectDef>();

        [NonSerialized] private Dictionary<StatusEffectId, StatusEffectDef> _byId;

        public IReadOnlyList<StatusEffectDef> All => defs;

        public bool TryGet(StatusEffectId id, out StatusEffectDef def)
        {
            EnsureCache();
            return _byId.TryGetValue(id, out def);
        }

        private void EnsureCache()
        {
            if (_byId != null)
                return;

            _byId = new Dictionary<StatusEffectId, StatusEffectDef>();

            for (int i = 0; i < defs.Count; i++)
            {
                var def = defs[i];
                if (def == null)
                    continue;

                if (_byId.ContainsKey(def.id))
                    continue;

                _byId.Add(def.id, def);
            }
        }

        private void OnEnable()
        {
            _byId = null;
        }
    }
}
