using UnityEngine;

namespace UltimateDungeon.StatusEffects
{
    /// <summary>
    /// StatusEffectDef (AUTHORITATIVE DATA)
    /// ====================================
    ///
    /// ScriptableObject definition for a single status effect.
    /// This is lightweight metadata for UI and lookup.
    /// </summary>
    [CreateAssetMenu(menuName = "Ultimate Dungeon/Status Effects/Status Effect Def", fileName = "StatusEffectDef_")]
    public sealed class StatusEffectDef : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable status effect identifier.")]
        public StatusEffectId id;

        [Tooltip("UI display name (can change without breaking saves).")]
        public string displayName;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(displayName))
                displayName = id.ToString();
        }
#endif
    }
}
