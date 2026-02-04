// ============================================================================
// ItemInstance.cs
// ----------------------------------------------------------------------------
// ItemInstance (RUNTIME + SAVE STATE)
//
// Adds (MVP): Item-granted ability selections
// - Stores the player's chosen SpellId per AbilityGrantSlot (Primary/Secondary/Utility)
// - Selections are PER-INSTANCE (so two identical swords can have different binds)
//
// IMPORTANT:
// - The ItemDef declares what slots exist + which SpellIds are allowed.
// - The ItemInstance stores the player's choice for those slots.
// - "Cannot change in combat" is a gameplay gate (enforced elsewhere); this file
//   only provides storage + sanitation helpers.
//
// Aligns with:
// - ITEM_DEF_SCHEMA.md (v1.5) item-granted abilities block
// - ItemDef.cs (GrantedAbilities / AbilityGrantSlot)
// ============================================================================

using System;
using System.Collections.Generic;
using UltimateDungeon.Spells;

namespace UltimateDungeon.Items
{
    [Serializable]
    public sealed class ItemInstance
    {
        // --------------------------------------------------------------------
        // Identity
        // --------------------------------------------------------------------

        /// <summary>
        /// Stable reference to ItemDef (must exist in ItemDef catalog/registry).
        /// </summary>
        public string itemDefId;

        // --------------------------------------------------------------------
        // Stack state
        // --------------------------------------------------------------------

        /// <summary>
        /// Stack count for stackable items.
        /// - For non-stackable items, this should remain 1.
        /// </summary>
        public int stackCount = 1;

        // --------------------------------------------------------------------
        // Durability state
        // --------------------------------------------------------------------

        public float durabilityCurrent;
        public float durabilityMax;

        // --------------------------------------------------------------------
        // Affixes (magical items)
        // --------------------------------------------------------------------

        /// <summary>
        /// Rolled affixes for this instance.
        /// Empty list means "mundane" (or "no affixes rolled").
        /// </summary>
        public List<AffixInstance> affixes = new List<AffixInstance>();

        // --------------------------------------------------------------------
        // Item-granted ability selections (PER-INSTANCE)
        // --------------------------------------------------------------------

        /// <summary>
        /// Player selections for item-granted ability slots.
        ///
        /// Why a list (not a dictionary)?
        /// - Easier to serialize to JSON later.
        /// - Unity-like serializers also handle Lists of structs more reliably.
        ///
        /// Invariants we aim to maintain:
        /// - At most one entry per AbilityGrantSlot.
        /// - spellId must be within the ItemDef's allowedSpellIds for that slot.
        /// - If invalid/missing, we fall back to def's defaultSpellId or SpellId.None.
        /// </summary>
        public List<GrantedAbilitySelection> grantedAbilitySelections = new List<GrantedAbilitySelection>();

        /// <summary>
        /// Which AbilityGrantSlot is currently active on the hotbar for this item.
        ///
        /// IMPORTANT:
        /// - This is per-instance configuration.
        /// - Changes are blocked during combat by the authoritative caller (not here).
        /// </summary>
        public AbilityGrantSlot activeGrantSlot = AbilityGrantSlot.Primary;

        // --------------------------------------------------------------------
        // Container contents (RUNTIME-ONLY FOR NOW)
        // --------------------------------------------------------------------

        /// <summary>
        /// If this instance is a container, it may have contents.
        /// Capacity rules live on ItemDef (ContainerData).
        ///
        /// IMPORTANT (v1):
        /// - We do NOT Unity-serialize this.
        /// - We also avoid allocating it for non-container items.
        /// - A dedicated container system will own deep validation + persistence.
        /// </summary>
        [NonSerialized] public List<ItemInstance> contents;

        // --------------------------------------------------------------------
        // Derived state
        // --------------------------------------------------------------------

        public bool IsBroken => durabilityMax > 0f && durabilityCurrent <= 0f;
        public int AffixCount => affixes?.Count ?? 0;
        public bool HasAffixes => affixes != null && affixes.Count > 0;

        public ItemInstance() { }

        public ItemInstance(string itemDefId)
        {
            this.itemDefId = itemDefId;
        }

        /// <summary>
        /// Initializes instance state from an ItemDef.
        ///
        /// Use this when the server creates a new instance.
        /// </summary>
        public void InitFromDef(ItemDef def)
        {
            if (def == null) throw new ArgumentNullException(nameof(def));

            itemDefId = def.itemDefId;

            // Stack state
            stackCount = 1;

            // Durability state
            if (def.usesDurability)
            {
                durabilityMax = def.durabilityMax;
                durabilityCurrent = durabilityMax;
            }
            else
            {
                durabilityMax = 0f;
                durabilityCurrent = 0f;
            }

            // Affixes start empty (mundane). Loot/enhancement can add later.
            if (affixes == null) affixes = new List<AffixInstance>();
            else affixes.Clear();

            // Item-granted ability selections start from ItemDef defaults.
            EnsureGrantedAbilityDefaults(def);
            EnsureActiveGrantSlotDefault(def);

            // Container contents:
            // - Non-container items: keep null (no allocation, avoids deep graphs)
            // - Container items: allocate an empty list
            if (def.family == ItemFamily.Container)
            {
                if (contents == null) contents = new List<ItemInstance>();
                else contents.Clear();
            }
            else
            {
                contents = null;
            }
        }

        /// <summary>
        /// Sanitizes the instance against an ItemDef + AffixCatalog.
        ///
        /// Use cases:
        /// - loading from persistence
        /// - receiving a network snapshot (if you ever cache on client)
        ///
        /// Returns:
        /// - true if instance seems valid enough to keep
        /// - false if critical data is missing
        /// </summary>
        public bool TrySanitize(ItemDef def, AffixCatalog affixCatalog)
        {
            if (def == null) return false;
            if (string.IsNullOrWhiteSpace(def.itemDefId)) return false;

            // Ensure our defId matches the provided def.
            itemDefId = def.itemDefId;

            // Stack rules
            if (!def.isStackable)
            {
                stackCount = 1;
            }
            else
            {
                int max = Math.Max(1, def.stackMax);
                if (stackCount < 1) stackCount = 1;
                if (stackCount > max) stackCount = max;
            }

            // Durability rules
            if (!def.usesDurability)
            {
                durabilityMax = 0f;
                durabilityCurrent = 0f;
            }
            else
            {
                durabilityMax = def.durabilityMax;
                if (durabilityCurrent < 0f) durabilityCurrent = 0f;
                if (durabilityCurrent > durabilityMax) durabilityCurrent = durabilityMax;
            }

            // Affix cap (LOCKED): max 5
            if (affixes == null) affixes = new List<AffixInstance>();
            if (affixes.Count > AffixCountResolver.GlobalAffixCap)
                affixes.RemoveRange(AffixCountResolver.GlobalAffixCap, affixes.Count - AffixCountResolver.GlobalAffixCap);

            // Sanitize each affix magnitude against catalog.
            if (affixCatalog != null)
            {
                for (int i = affixes.Count - 1; i >= 0; i--)
                {
                    var a = affixes[i];
                    if (!AffixRoller.TrySanitize(affixCatalog, ref a))
                    {
                        // Unknown affix id -> remove.
                        affixes.RemoveAt(i);
                        continue;
                    }

                    // Write back sanitized value.
                    affixes[i] = a;
                }
            }

            // Item-granted ability selections must match the def.
            SanitizeGrantedAbilitySelections(def);

            // Ensure active grant slot remains valid for this def.
            EnsureActiveGrantSlotDefault(def);

            // Container contents:
            // Keep runtime-only. For v1, we ensure non-containers don't hold allocations.
            if (def.family != ItemFamily.Container)
                contents = null;

            return true;
        }

        /// <summary>
        /// Adds durability damage. Returns true if the item just became broken.
        /// Server-only.
        /// </summary>
        public bool ApplyDurabilityLoss(float amount)
        {
            if (amount <= 0f) return false;
            if (durabilityMax <= 0f) return false;

            bool wasBroken = IsBroken;

            durabilityCurrent -= amount;
            if (durabilityCurrent < 0f) durabilityCurrent = 0f;

            return !wasBroken && IsBroken;
        }

        // ====================================================================
        // Granted ability selection helpers
        // ====================================================================

        /// <summary>
        /// Gets the currently selected SpellId for a given slot.
        /// If no selection exists yet, returns the ItemDef default for that slot,
        /// or SpellId.None if no default exists.
        /// </summary>
        public SpellId GetSelectedSpellId(ItemDef def, AbilityGrantSlot slot)
        {
            if (def == null) return SpellId.None;

            // Try to find an existing selection entry.
            if (grantedAbilitySelections != null)
            {
                for (int i = 0; i < grantedAbilitySelections.Count; i++)
                {
                    if (grantedAbilitySelections[i].slot == slot)
                        return grantedAbilitySelections[i].spellId;
                }
            }

            // Fallback: def default.
            return GetDefaultSpellIdFromDef(def, slot);
        }

        /// <summary>
        /// Resolve the active AbilityGrantSlot for this instance.
        ///
        /// If allowUpdate is false (e.g., during combat) we do NOT mutate state.
        /// </summary>
        public bool TryResolveActiveGrantSlot(ItemDef def, bool allowUpdate, out AbilityGrantSlot slot)
        {
            slot = AbilityGrantSlot.Primary;

            if (def == null)
                return false;

            var granted = def.grantedAbilities.grantedAbilitySlots;
            if (granted == null || granted.Length == 0)
                return false;

            if (IsGrantSlotAllowed(granted, activeGrantSlot))
            {
                slot = activeGrantSlot;
                return true;
            }

            if (!allowUpdate)
                return false;

            // Fall back to the first available slot and persist (not during combat).
            slot = granted[0].slot;
            activeGrantSlot = slot;
            return true;
        }

        /// <summary>
        /// Attempts to set the selected SpellId for a slot.
        ///
        /// Returns false if:
        /// - the slot does not exist on this item (per def)
        /// - the chosen spell is not allowed for that slot
        ///
        /// NOTE:
        /// - This does NOT enforce "out of combat".
        ///   That gate belongs to gameplay/server logic.
        /// </summary>
        public bool TrySetSelectedSpellId(ItemDef def, AbilityGrantSlot slot, SpellId chosen)
        {
            if (def == null) return false;

            // Ensure the slot exists and grab the allowed list.
            if (!TryGetGrantedSlotFromDef(def, slot, out var grantedSlot))
                return false;

            // If the author didn't include any allowed spells, we treat it as "no choices".
            // (This prevents selecting random spells on mis-authored items.)
            if (grantedSlot.allowedSpellIds == null || grantedSlot.allowedSpellIds.Length == 0)
                return false;

            // Validate: chosen must be one of the allowed.
            bool allowed = false;
            for (int i = 0; i < grantedSlot.allowedSpellIds.Length; i++)
            {
                if (grantedSlot.allowedSpellIds[i] == chosen)
                {
                    allowed = true;
                    break;
                }
            }

            if (!allowed)
                return false;

            // Upsert the selection.
            if (grantedAbilitySelections == null)
                grantedAbilitySelections = new List<GrantedAbilitySelection>();

            for (int i = 0; i < grantedAbilitySelections.Count; i++)
            {
                if (grantedAbilitySelections[i].slot == slot)
                {
                    grantedAbilitySelections[i] = new GrantedAbilitySelection { slot = slot, spellId = chosen };
                    return true;
                }
            }

            grantedAbilitySelections.Add(new GrantedAbilitySelection { slot = slot, spellId = chosen });
            return true;
        }

        /// <summary>
        /// Ensures that we have a selection entry for every slot declared on the def,
        /// using def defaults (or None).
        ///
        /// Use this on new items, or whenever you want to make sure the list is complete.
        /// </summary>
        public void EnsureGrantedAbilityDefaults(ItemDef def)
        {
            if (def == null)
                return;

            if (grantedAbilitySelections == null)
                grantedAbilitySelections = new List<GrantedAbilitySelection>();
            else
                grantedAbilitySelections.Clear();

            // If the item doesn't grant abilities, keep empty.
            var slots = def.grantedAbilities.grantedAbilitySlots;
            if (slots == null || slots.Length == 0)
                return;

            for (int i = 0; i < slots.Length; i++)
            {
                AbilityGrantSlot slot = slots[i].slot;
                SpellId chosen = slots[i].defaultSpellId;

                // If default is None, we still create an entry. This makes the UI easier
                // because it can always show the current selection per slot.
                grantedAbilitySelections.Add(new GrantedAbilitySelection { slot = slot, spellId = chosen });
            }

            // Final pass: keep it valid.
            SanitizeGrantedAbilitySelections(def);
        }

        /// <summary>
        /// Makes sure selections:
        /// - have at most one entry per slot
        /// - only include slots that exist on the ItemDef
        /// - only include SpellIds allowed by the ItemDef
        /// - fall back to default/None when invalid
        /// </summary>
        public void SanitizeGrantedAbilitySelections(ItemDef def)
        {
            if (def == null)
                return;

            if (grantedAbilitySelections == null)
                grantedAbilitySelections = new List<GrantedAbilitySelection>();

            // Build a quick set of valid slots from the def.
            var grantedSlots = def.grantedAbilities.grantedAbilitySlots;
            if (grantedSlots == null || grantedSlots.Length == 0)
            {
                // Item grants nothing -> wipe selections.
                grantedAbilitySelections.Clear();
                return;
            }

            // Remove duplicates by walking from end and tracking seen.
            // (We keep the FIRST occurrence and remove later duplicates.)
            var seen = new HashSet<AbilityGrantSlot>();
            for (int i = grantedAbilitySelections.Count - 1; i >= 0; i--)
            {
                var entry = grantedAbilitySelections[i];
                if (seen.Contains(entry.slot))
                {
                    grantedAbilitySelections.RemoveAt(i);
                    continue;
                }

                seen.Add(entry.slot);
            }

            // Remove entries whose slot does not exist on the def.
            for (int i = grantedAbilitySelections.Count - 1; i >= 0; i--)
            {
                if (!TryGetGrantedSlotFromDef(def, grantedAbilitySelections[i].slot, out _))
                    grantedAbilitySelections.RemoveAt(i);
            }

            // For each slot on the def, ensure we have an entry and that it is allowed.
            for (int i = 0; i < grantedSlots.Length; i++)
            {
                AbilityGrantSlot slot = grantedSlots[i].slot;
                int idx = IndexOfSelection(slot);

                if (idx < 0)
                {
                    // Missing -> add default.
                    grantedAbilitySelections.Add(new GrantedAbilitySelection
                    {
                        slot = slot,
                        spellId = grantedSlots[i].defaultSpellId
                    });
                    continue;
                }

                // Present -> validate selection is allowed, otherwise fallback.
                SpellId current = grantedAbilitySelections[idx].spellId;

                // If author didn't list allowed spells, force to default.
                if (grantedSlots[i].allowedSpellIds == null || grantedSlots[i].allowedSpellIds.Length == 0)
                {
                    grantedAbilitySelections[idx] = new GrantedAbilitySelection
                    {
                        slot = slot,
                        spellId = grantedSlots[i].defaultSpellId
                    };
                    continue;
                }

                // If selection is None, it's always OK.
                if (current == SpellId.None)
                    continue;

                bool allowed = false;
                for (int a = 0; a < grantedSlots[i].allowedSpellIds.Length; a++)
                {
                    if (grantedSlots[i].allowedSpellIds[a] == current)
                    {
                        allowed = true;
                        break;
                    }
                }

                if (!allowed)
                {
                    // Fallback: default if valid, else None.
                    SpellId fallback = grantedSlots[i].defaultSpellId;

                    if (fallback != SpellId.None)
                    {
                        bool fallbackAllowed = false;
                        for (int a = 0; a < grantedSlots[i].allowedSpellIds.Length; a++)
                        {
                            if (grantedSlots[i].allowedSpellIds[a] == fallback)
                            {
                                fallbackAllowed = true;
                                break;
                            }
                        }

                        if (!fallbackAllowed)
                            fallback = SpellId.None;
                    }

                    grantedAbilitySelections[idx] = new GrantedAbilitySelection { slot = slot, spellId = fallback };
                }
            }

            // Local helper: find selection index.
            int IndexOfSelection(AbilityGrantSlot s)
            {
                for (int i = 0; i < grantedAbilitySelections.Count; i++)
                    if (grantedAbilitySelections[i].slot == s) return i;
                return -1;
            }
        }

        private static bool TryGetGrantedSlotFromDef(ItemDef def, AbilityGrantSlot slot, out GrantedAbilitySlot grantedSlot)
        {
            var slots = def.grantedAbilities.grantedAbilitySlots;
            if (slots != null)
            {
                for (int i = 0; i < slots.Length; i++)
                {
                    if (slots[i].slot == slot)
                    {
                        grantedSlot = slots[i];
                        return true;
                    }
                }
            }

            grantedSlot = default;
            return false;
        }

        private static SpellId GetDefaultSpellIdFromDef(ItemDef def, AbilityGrantSlot slot)
        {
            var slots = def.grantedAbilities.grantedAbilitySlots;
            if (slots == null) return SpellId.None;

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].slot == slot)
                    return slots[i].defaultSpellId;
            }

            return SpellId.None;
        }

        private void EnsureActiveGrantSlotDefault(ItemDef def)
        {
            if (def == null)
                return;

            var granted = def.grantedAbilities.grantedAbilitySlots;
            if (granted == null || granted.Length == 0)
                return;

            if (IsGrantSlotAllowed(granted, activeGrantSlot))
                return;

            activeGrantSlot = granted[0].slot;
        }

        private static bool IsGrantSlotAllowed(GrantedAbilitySlot[] granted, AbilityGrantSlot slot)
        {
            if (granted == null)
                return false;

            for (int i = 0; i < granted.Length; i++)
            {
                if (granted[i].slot == slot)
                    return true;
            }

            return false;
        }
    }

    // ========================================================================
    // Supporting types
    // ========================================================================

    /// <summary>
    /// A single selection: "for slot X, the player chose SpellId Y".
    ///
    /// Stored on ItemInstance so it can be persisted and networked.
    /// </summary>
    [Serializable]
    public struct GrantedAbilitySelection
    {
        public AbilityGrantSlot slot;
        public SpellId spellId;
    }
}
