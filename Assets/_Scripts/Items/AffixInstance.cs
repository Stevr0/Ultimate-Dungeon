using System;
using UnityEngine;

namespace UltimateDungeon.Items
{
    /// <summary>
    /// AffixInstance (RUNTIME DATA)
    /// ===========================
    ///
    /// Represents a rolled affix on an ItemInstance.
    ///
    /// DESIGN LOCKS (from ITEM_AFFIX_CATALOG.md):
    /// - Affixes are rolled server-side only.
    /// - Magnitudes must be within the declared range for that AffixId.
    /// - Stacking rules (Sum / HighestOnly / NoStack) are enforced by aggregation/validation.
    ///
    /// Storage note:
    /// - We store magnitude as float to cover both int and percent-like values.
    /// - If integerMagnitude is true on the AffixDef, the roller will round.
    /// </summary>
    [Serializable]
    public struct AffixInstance
    {
        [Tooltip("Stable affix identifier.")]
        public AffixId id;

        [Tooltip("Rolled magnitude. For percent-like affixes this is in percent units (e.g., 15 = 15%).")]
        public float magnitude;

        public AffixInstance(AffixId id, float magnitude)
        {
            this.id = id;
            this.magnitude = magnitude;
        }

        public override string ToString() => $"{id} ({magnitude})";
    }
}
