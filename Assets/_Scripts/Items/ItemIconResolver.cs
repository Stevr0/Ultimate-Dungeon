// ============================================================================
// ItemIconResolver.cs
// ----------------------------------------------------------------------------
// TEMP icon resolver (Resources-based).
// Later you can replace with Addressables / SpriteAtlas / async loading.
// Place this in: Assets/_Scripts/Items/UI/ItemIconResolver.cs
// ============================================================================

using System.Collections.Generic;
using UnityEngine;

namespace UltimateDungeon.Items
{
    public static class ItemIconResolver
    {
        // Simple in-memory cache so we don't spam Resources.Load every refresh.
        private static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

        /// <summary>
        /// Resolve a sprite for an item def.
        /// Convention (TEMP):
        /// - ItemDef.iconAddress is a Resources path, e.g. "Icons/weapon_sword_dagger"
        /// - File lives at: Assets/Resources/Icons/weapon_sword_dagger.png
        /// </summary>
        public static Sprite Resolve(ItemDef def)
        {
            if (def == null) return null;

            var path = def.iconAddress;
            if (string.IsNullOrWhiteSpace(path))
                return null;

            if (_cache.TryGetValue(path, out var cached))
                return cached;

            // NOTE: Resources.Load needs the path WITHOUT file extension.
            var sprite = Resources.Load<Sprite>(path);
            _cache[path] = sprite; // cache nulls too, so we don't keep trying

            return sprite;
        }

        /// <summary>
        /// Helpful during iteration when you re-import sprites.
        /// </summary>
        public static void ClearCache()
        {
            _cache.Clear();
        }
    }
}
