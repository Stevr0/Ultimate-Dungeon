// ============================================================================
// ItemDefCatalog.cs — Ultimate Dungeon
// ----------------------------------------------------------------------------
// ScriptableObject registry for all ItemDef assets.
// Aligns with:
// - ITEM_CATALOG.md (stable ids)
// - ITEM_DEF_SCHEMA.md (schema legality)
//
// Notes:
// - This is authoring/runtime lookup convenience.
// - Hard enforcement belongs in Editor validators.
// ============================================================================

using System.Collections.Generic;
using UnityEngine;

namespace UltimateDungeon.Items
{
    [CreateAssetMenu(menuName = "Ultimate Dungeon/Items/ItemDef Catalog")]
    public sealed class ItemDefCatalog : ScriptableObject
    {
        [SerializeField] private List<ItemDef> defs = new();

        [Tooltip("Optional: point to ITEM_CATALOG.md for id existence validation.")]
        [SerializeField] private TextAsset itemCatalogMarkdown;

        private Dictionary<string, ItemDef> _byId;

        private void OnEnable() => RebuildLookup();

        public void RebuildLookup()
        {
            _byId = new Dictionary<string, ItemDef>();

            foreach (var def in defs)
            {
                if (def == null) continue;
                if (string.IsNullOrWhiteSpace(def.itemDefId)) continue;

                // Last writer wins (editor validator will report duplicates).
                _byId[def.itemDefId] = def;
            }
        }

        public bool TryGet(string itemDefId, out ItemDef def)
        {
            def = null;

            if (string.IsNullOrWhiteSpace(itemDefId))
                return false;

            if (_byId == null)
                RebuildLookup();

            return _byId.TryGetValue(itemDefId, out def);
        }

        // ---- Read-only accessors for tooling ----
        public IReadOnlyList<ItemDef> AllDefs => defs;
        public TextAsset CatalogMarkdown => itemCatalogMarkdown;
    }
}
