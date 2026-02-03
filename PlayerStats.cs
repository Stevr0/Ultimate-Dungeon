// ============================================================================
// PlayerStats.cs
// ----------------------------------------------------------------------------
// Server-authoritative primary attributes + derived stats.
//
// LOCKED RULES (from docs / your decisions):
// - Starting STR/DEX/INT are fixed at 10/10/10 (from PlayerDefinition)
// - Max vitals derive from these attributes (handled in PlayerVitals)
// - No classes / levels / XP
//
// This file focuses on:
// - Primary attributes (base + modifiers)
// - A clean place to compute derived values later
//
// NOTE:
// We intentionally separate:
// - BASE values (from PlayerDefinition)
// - MODIFIERS (from items + status effects)
// - EFFECTIVE values (base + modifiers, clamped)
// ============================================================================

using UnityEngine;

namespace UltimateDungeon.Players
{
    /// <summary>
    /// PlayerStats
    /// ----------
    /// Holds primary attributes and provides effective values.
    ///
    /// This component is intended to be written by the SERVER.
    /// Clients should treat it as read-only state (replicated).
    /// </summary>
    public sealed class PlayerStats : MonoBehaviour
    {
        // --------------------------------------------------------------------
        // Base Attributes (from PlayerDefinition)
        // --------------------------------------------------------------------

        [Header("Base Attributes (from PlayerDefinition)")]
        [SerializeField] private int _baseSTR;
        [SerializeField] private int _baseDEX;
        [SerializeField] private int _baseINT;

        // --------------------------------------------------------------------
        // Modifier Buckets (from Items / Status Effects)
        // --------------------------------------------------------------------
        // We keep modifiers separated to make it very clear where changes come from.
        // Later, you can add more buckets (e.g., buffs, debuffs, auras, zones).

        [Header("Attribute Modifiers (runtime)")]
        [SerializeField] private int _modFromItemsSTR;
        [SerializeField] private int _modFromItemsDEX;
        [SerializeField] private int _modFromItemsINT;

        [SerializeField] private int _modFromStatusesSTR;
        [SerializeField] private int _modFromStatusesDEX;
        [SerializeField] private int _modFromStatusesINT;

        // --------------------------------------------------------------------
        // Effective Attributes
        // --------------------------------------------------------------------
        // Effective = base + all modifiers.
        // We do not clamp attributes here (you haven't locked a stat cap yet).
        // If you later decide "stats cap at 100" (UO-like), we can clamp here.

        public int STR => _baseSTR + _modFromItemsSTR + _modFromStatusesSTR;
        public int DEX => _baseDEX + _modFromItemsDEX + _modFromStatusesDEX;
        public int INT => _baseINT + _modFromItemsINT + _modFromStatusesINT;

        // For UI/debugging you may also want explicit base/mod readouts:
        public int BaseSTR => _baseSTR;
        public int BaseDEX => _baseDEX;
        public int BaseINT => _baseINT;

        // --------------------------------------------------------------------
        // Initialization
        // --------------------------------------------------------------------

        /// <summary>
        /// Server-side initialization from PlayerDefinition.
        /// </summary>
        public void InitializeFromDefinition(PlayerDefinition def)
        {
            if (def == null)
            {
                Debug.LogError("[PlayerStats] InitializeFromDefinition failed: PlayerDefinition is null.");
                return;
            }

            // Locked by design: starting stats fixed at 10 each.
            _baseSTR = def.baseSTR;
            _baseDEX = def.baseDEX;
            _baseINT = def.baseINT;

            // Reset modifiers for a fresh spawn.
            _modFromItemsSTR = 0;
            _modFromItemsDEX = 0;
            _modFromItemsINT = 0;

            _modFromStatusesSTR = 0;
            _modFromStatusesDEX = 0;
            _modFromStatusesINT = 0;

            Debug.Log($"[PlayerStats] Initialized. STR={STR}, DEX={DEX}, INT={INT}");
        }

        // --------------------------------------------------------------------
        // Modifier Setters (Server-side)
        // --------------------------------------------------------------------
        // These are the methods other systems will call when equipment changes
        // or when status effects apply/remove.

        /// <summary>
        /// Server-side: set the total attribute modifiers coming from equipped items.
        ///
        /// Why set totals instead of add/remove?
        /// - It keeps the system deterministic and robust.
        /// - Equipment systems can recompute totals and set them in one go.
        /// - Prevents "forgot to remove" bugs.
        /// </summary>
        public void SetItemAttributeModifiers(int str, int dex, int intel)
        {
            _modFromItemsSTR = str;
            _modFromItemsDEX = dex;
            _modFromItemsINT = intel;
        }

        /// <summary>
        /// Server-side: set the total attribute modifiers coming from status effects.
        /// </summary>
        public void SetStatusAttributeModifiers(int str, int dex, int intel)
        {
            _modFromStatusesSTR = str;
            _modFromStatusesDEX = dex;
            _modFromStatusesINT = intel;
        }

        // --------------------------------------------------------------------
        // Derived Stats (Stub)
        // --------------------------------------------------------------------
        // You will later implement derived stats used by combat:
        // - hit chance
        // - defense chance
        // - swing speed
        // - damage modifiers
        // - resistances
        //
        // We keep these out of v1 until combat math is locked.

        /// <summary>
        /// Example derived value placeholder.
        /// You can delete this once real derived stats exist.
        /// </summary>
        public float GetExampleAttackRating()
        {
            // Simple example: STR contributes more than DEX.
            // This is NOT locked; just a placeholder.
            return STR * 1.0f + DEX * 0.5f;
        }

        // --------------------------------------------------------------------
        // Base Stat Progression (Server-only)
        // --------------------------------------------------------------------


        /// <summary>
        /// Server-side: increase a base stat by a given amount.
        ///
        /// IMPORTANT:
        /// - This is permanent progression.
        /// - Only the server should call this.
        /// - Stat caps are not locked yet; add clamps here once decided.
        /// </summary>
        public void IncreaseBaseStat(UltimateDungeon.Progression.StatId statId, int amount)
        {
            if (amount <= 0) return;


            switch (statId)
            {
                case UltimateDungeon.Progression.StatId.STR:
                    _baseSTR += amount;
                    break;


                case UltimateDungeon.Progression.StatId.DEX:
                    _baseDEX += amount;
                    break;


                case UltimateDungeon.Progression.StatId.INT:
                    _baseINT += amount;
                    break;
            }


            // TODO (once locked): apply individual stat cap and/or total stat cap.
            // Example:
            // _baseSTR = Mathf.Min(_baseSTR, 100);
        }


        /// <summary>
        /// Convenience (optional): total base stats.
        /// Useful once you lock a total stat cap.
        /// </summary>
        public int GetTotalBaseStats()
        {
            return _baseSTR + _baseDEX + _baseINT;
        }
    }
}
