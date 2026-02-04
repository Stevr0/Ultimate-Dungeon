// ============================================================================
// SpellDefCatalog.cs — Ultimate Dungeon
// ----------------------------------------------------------------------------
// Simple lookup catalog for SpellDef assets by SpellId.
// This keeps spell metadata data-driven (no hardcoded tables).
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateDungeon.Spells
{
    [CreateAssetMenu(menuName = "Ultimate Dungeon/Spells/SpellDef Catalog", fileName = "SpellDefCatalog")]
    public sealed class SpellDefCatalog : ScriptableObject
    {
        [SerializeField] private SpellDef[] spells = Array.Empty<SpellDef>();

        private Dictionary<SpellId, SpellDef> _map;

        public bool TryGet(SpellId id, out SpellDef def)
        {
            if (_map == null || _map.Count != spells.Length)
                Rebuild();

            return _map.TryGetValue(id, out def);
        }

        /// <summary>
        /// Returns a SpellDef for the given id (or null if not found).
        /// </summary>
        public SpellDef Get(SpellId id)
        {
            return TryGet(id, out var def) ? def : null;
        }

        private void Rebuild()
        {
            _map = new Dictionary<SpellId, SpellDef>();

            if (spells == null)
                return;

            for (int i = 0; i < spells.Length; i++)
            {
                var def = spells[i];
                if (def == null)
                    continue;

                _map[def.spellId] = def;
            }
        }
    }
}
