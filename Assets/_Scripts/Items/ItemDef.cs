// ============================================================================
// ItemDef.cs � Ultimate Dungeon (AUTHORITATIVE SCHEMA)
// ----------------------------------------------------------------------------
// Aligns with:
// - ITEM_DEF_SCHEMA.md (v1.5)
// - ITEM_CATALOG.md (v2.1)
//
// Key changes vs older prototype ItemDef:
// - Introduces 10-slot equipment model (EquipSlot)
// - Introduces EquipmentData block (isEquippable + equipSlot)
// - Renames/expands ItemFamily to match catalog (Bag/Head/Neck/Mainhand/...)
// - Adds Item-granted abilities (GrantedAbilities) with stable slots
// - Adds schema-required family blocks (Weapon/Armor/Shield/Jewelry/...)
// - Adds authoring-time validation for locked rules
//
// IMPORTANT:
// - ItemDef is immutable design data.
// - Runtime mutable state is ItemInstance.
// - The SERVER is authoritative for any gameplay derived from these definitions.
//
// Authoring note (Icons):
// - ItemIconResolver uses Resources.Load<Sprite>(iconAddress)
// - Therefore iconAddress must be a Resources path WITHOUT extension.
//   Example:
//     Sprite: Assets/Resources/Icons/Dagger.png
//     iconAddress: "Icons/Dagger"
// ============================================================================

using System;
using UnityEngine;
using UltimateDungeon.Skills;
using UltimateDungeon.Spells;

namespace UltimateDungeon.Items
{
    [CreateAssetMenu(menuName = "Ultimate Dungeon/Items/Item Def", fileName = "ItemDef_")]
    public sealed class ItemDef : ScriptableObject
    {
        // --------------------------------------------------------------------
        // CORE ITEMDEF FIELDS (PRESENT ON ALL ITEMS)
        // --------------------------------------------------------------------

        [Header("Identity")]
        [Tooltip("Stable id. MUST match an entry in ITEM_CATALOG.md (append-only).")]
        public string itemDefId;

        [Tooltip("UI display name. Can change without breaking saves.")]
        public string displayName;

        [Tooltip("Family determines slot legality and which data blocks are required.")]
        public ItemFamily family;

        [Tooltip("Optional Resources path (no extension). Used by ItemIconResolver.")]
        public string iconAddress;

        [Header("Economy / Inventory")]
        [Min(0f)] public float weight = 0f;

        [Tooltip("If true, this item may stack in inventory.")]
        public bool isStackable = false;

        [Tooltip("Required if isStackable == true. Must be > 1.")]
        public int stackMax = 1;

        [Header("Durability")]
        public bool usesDurability = false;

        [Tooltip("Required if usesDurability == true. Must be > 0.")]
        public float durabilityMax = 0f;

        [Header("Affix Pools")]
        [Tooltip("Names/refs that map to AffixPoolDef assets.")]
        public string[] affixPoolRefs;

        // --------------------------------------------------------------------
        // EQUIPMENT MODEL (AUTHORITATIVE)
        // --------------------------------------------------------------------

        [Header("Equipment (optional block)")]
        [Tooltip("Present for equippable items only.")]
        public EquipmentData equipment;

        [Header("Item-Granted Abilities (equippable only)")]
        [Tooltip("Equippable items should grant 1�3 ability choice slots.")]
        public GrantedAbilities grantedAbilities;

        // --------------------------------------------------------------------
        // FAMILY-SPECIFIC DATA BLOCKS
        // --------------------------------------------------------------------
        // These are optional at the schema level, but REQUIRED for specific families.

        [Header("Container Data (Bag/Container)")]
        public ContainerData container;

        [Header("Weapon Data (Mainhand)")]
        public WeaponData weapon;

        [Header("Armor Data (Head/Chest/Foot and some Neck items)")]
        public ArmorData armor;

        [Header("Shield Data (Offhand - shields)")]
        public ShieldData shield;

        [Header("Jewelry Data (Offhand rings, Neck earrings/amulets)")]
        public JewelryData jewelry;

        [Header("Consumable Data (UtilityItem / Consumable)")]
        public ConsumableData consumable;

        [Header("Mount Data (Mount)")]
        public MountData mount;

        [Header("Reagent Data (Reagent)")]
        public ReagentData reagent;

        // --------------------------------------------------------------------
        // Convenience helpers
        // --------------------------------------------------------------------

        public bool IsEquippable => equipment.isEquippable;

        public bool IsRangedWeapon => family == ItemFamily.Mainhand && weapon.ammoType != AmmoType.None;

        public bool IsBackpack => equipment.isEquippable && equipment.equipSlot == EquipSlot.Bag;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // -------------------------
            // Identity sanity
            // -------------------------
            if (!string.IsNullOrWhiteSpace(itemDefId))
                itemDefId = itemDefId.Trim();

            if (string.IsNullOrWhiteSpace(displayName))
                displayName = string.IsNullOrWhiteSpace(itemDefId) ? name : itemDefId;

            // -------------------------
            // Inventory rules
            // -------------------------
            if (weight < 0f) weight = 0f;

            // Stacking rules (LOCKED): stackable => stackMax > 1
            if (!isStackable)
            {
                if (stackMax < 1) stackMax = 1;
            }
            else
            {
                if (stackMax <= 1) stackMax = 2;
            }

            // Durability rules (LOCKED): usesDurability => durabilityMax > 0
            if (!usesDurability)
            {
                if (durabilityMax < 0f) durabilityMax = 0f;
            }
            else
            {
                if (durabilityMax <= 0f) durabilityMax = 1f;
            }

            // -------------------------
            // Equipment legality
            // -------------------------
            // Authoring convenience: auto-set isEquippable for known equippable families.
            bool familyIsEquippable = family is
                ItemFamily.Bag or ItemFamily.Head or ItemFamily.Neck or ItemFamily.Mainhand or
                ItemFamily.Chest or ItemFamily.Offhand or ItemFamily.UtilityItem or ItemFamily.Foot or ItemFamily.Mount;

            equipment.isEquippable = familyIsEquippable;

            if (equipment.isEquippable)
            {
                // Slot must match family rules.
                EquipSlot required = EquipSlot.None;
                bool allowEitherBelt = false;

                switch (family)
                {
                    case ItemFamily.Bag: required = EquipSlot.Bag; break;
                    case ItemFamily.Head: required = EquipSlot.Head; break;
                    case ItemFamily.Neck: required = EquipSlot.Neck; break;
                    case ItemFamily.Mainhand: required = EquipSlot.Mainhand; break;
                    case ItemFamily.Chest: required = EquipSlot.Chest; break;
                    case ItemFamily.Offhand: required = EquipSlot.Offhand; break;
                    case ItemFamily.Foot: required = EquipSlot.Foot; break;
                    case ItemFamily.Mount: required = EquipSlot.Mount; break;
                    case ItemFamily.UtilityItem:
                        allowEitherBelt = true;
                        break;
                }

                if (allowEitherBelt)
                {
                    // LOCKED: UtilityItem may be BeltA or BeltB.
                    if (equipment.equipSlot != EquipSlot.BeltA && equipment.equipSlot != EquipSlot.BeltB)
                        equipment.equipSlot = EquipSlot.BeltA;
                }
                else
                {
                    if (equipment.equipSlot != required)
                        equipment.equipSlot = required;
                }

                // Equippable items should grant 1..3 ability slots.
                grantedAbilities.SanitizeForEquippable();
            }
            else
            {
                // Non-equippable items should not declare an equip slot.
                equipment.equipSlot = EquipSlot.None;

                // Non-equippables should not grant abilities.
                grantedAbilities.Clear();
            }

            // -------------------------
            // Family required blocks
            // -------------------------

            // Backpack lock (LOCKED): equipped backpacks are always 48 slots.
            if (family == ItemFamily.Bag)
            {
                container.capacitySlots = 48;

                // Bags are containers; they should not be stackable.
                isStackable = false;
                stackMax = 1;

                // Bags are durable.
                if (!usesDurability) usesDurability = true;
                if (durabilityMax <= 0f) durabilityMax = 1f;
            }

            // Containers (non-equip) should not be stackable.
            if (family == ItemFamily.Container)
            {
                isStackable = false;
                stackMax = 1;
                if (container.capacitySlots < 0) container.capacitySlots = 0;
            }

            // Weapon rules (LOCKED)
            if (family == ItemFamily.Mainhand)
            {
                if (weapon.minDamage < 0) weapon.minDamage = 0;
                if (weapon.maxDamage < 0) weapon.maxDamage = 0;
                if (weapon.maxDamage < weapon.minDamage) weapon.maxDamage = weapon.minDamage;

                if (weapon.swingSpeedSeconds <= 0f) weapon.swingSpeedSeconds = 1f;
                if (weapon.staminaCostPerSwing < 0) weapon.staminaCostPerSwing = 0;

                if (weapon.ammoType != AmmoType.None)
                    weapon.handedness = WeaponHandedness.TwoHanded;

                if (weapon.rangeMeters < 0f) weapon.rangeMeters = 0f;

                // Weapons are durable.
                if (!usesDurability) usesDurability = true;
                if (durabilityMax <= 0f) durabilityMax = 1f;
            }

            // Armor rules (LOCKED): resists must be >= 0 (dexPenalty may be negative)
            if (family == ItemFamily.Head || family == ItemFamily.Chest || family == ItemFamily.Foot)
            {
                armor.resistPhysical = Mathf.Max(0, armor.resistPhysical);
                armor.resistFire = Mathf.Max(0, armor.resistFire);
                armor.resistCold = Mathf.Max(0, armor.resistCold);
                armor.resistPoison = Mathf.Max(0, armor.resistPoison);
                armor.resistEnergy = Mathf.Max(0, armor.resistEnergy);

                // Armor is durable.
                if (!usesDurability) usesDurability = true;
                if (durabilityMax <= 0f) durabilityMax = 1f;
            }

            // Jewelry rule (LOCKED): jewelry uses durability.
            // Rings are Offhand; Earrings/Amulets are Neck.
            if (family == ItemFamily.Offhand && jewelry.slot == JewelrySlot.Ring)
            {
                usesDurability = true;
                if (durabilityMax <= 0f) durabilityMax = 1f;
            }

            if (family == ItemFamily.Neck && (jewelry.slot == JewelrySlot.Earring || jewelry.slot == JewelrySlot.Amulet))
            {
                usesDurability = true;
                if (durabilityMax <= 0f) durabilityMax = 1f;
            }

            // UtilityItem should always be consumable-like.
            if (family == ItemFamily.UtilityItem)
            {
                // Utility items are generally usable; allow author override if desired.
                if (consumable.useTimeSeconds < 0f) consumable.useTimeSeconds = 0f;

                // Utility items are typically stackable (potions/food/bandages),
                // but torches etc may not be. We don't force stacking here.
            }

            // Mount sanity
            if (family == ItemFamily.Mount)
            {
                if (mount.moveSpeedMultiplier < 0f) mount.moveSpeedMultiplier = 0f;
                if (mount.staminaDrainPerSecond < 0f) mount.staminaDrainPerSecond = 0f;

                // Mounts are durable.
                if (!usesDurability) usesDurability = true;
                if (durabilityMax <= 0f) durabilityMax = 1f;
            }

            // Icon authoring convenience: if the author accidentally pastes a full path,
            // we do NOT try to fix it (that could be destructive). We just warn.
            if (!string.IsNullOrWhiteSpace(iconAddress))
            {
                if (iconAddress.Contains("Assets/") || iconAddress.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    Debug.LogWarning($"[ItemDef] iconAddress should be a Resources path without extension. Current: '{iconAddress}'", this);
                }
            }
        }
#endif
    }

    // ========================================================================
    // SCHEMA TYPES
    // ========================================================================

    [Serializable]
    public struct EquipmentData
    {
        [Tooltip("True if the item may be equipped.")]
        public bool isEquippable;

        [Tooltip("The slot this item equips into (if equippable).")]
        public EquipSlot equipSlot;
    }

    public enum EquipSlot
    {
        None = 0,
        Bag = 1,
        Head = 2,
        Neck = 3,
        Mainhand = 4,
        Chest = 5,
        Offhand = 6,
        BeltA = 7,
        BeltB = 8,
        Foot = 9,
        Mount = 10,
    }

    // ------------------------------------------------------------------------
    // Item-granted abilities
    // ------------------------------------------------------------------------

    public enum AbilityGrantSlot
    {
        Primary = 0,
        Secondary = 1,
        Utility = 2,
    }

    [Serializable]
    public struct GrantedAbilities
    {
        [Tooltip("Equippable items should have 1..3 slots.")]
        public GrantedAbilitySlot[] grantedAbilitySlots;

        /// <summary>
        /// Ensures equippable items satisfy basic authoring invariants.
        /// This is not your full validation (that belongs in Editor validators),
        /// but it keeps assets from drifting into invalid states.
        /// </summary>
        public void SanitizeForEquippable()
        {
            if (grantedAbilitySlots == null || grantedAbilitySlots.Length == 0)
            {
                // Default to a single Utility slot.
                grantedAbilitySlots = new[]
                {
                    new GrantedAbilitySlot
                    {
                        slot = AbilityGrantSlot.Utility,
                        allowedSpellIds = new SpellId[0],
                        defaultSpellId = SpellId.None
                    }
                };
                return;
            }

            if (grantedAbilitySlots.Length > 3)
            {
                Array.Resize(ref grantedAbilitySlots, 3);
            }

            // Ensure allowed arrays are non-null.
            for (int i = 0; i < grantedAbilitySlots.Length; i++)
            {
                if (grantedAbilitySlots[i].allowedSpellIds == null)
                    grantedAbilitySlots[i].allowedSpellIds = Array.Empty<SpellId>();
            }
        }

        public void Clear()
        {
            grantedAbilitySlots = Array.Empty<GrantedAbilitySlot>();
        }
    }

    [Serializable]
    public struct GrantedAbilitySlot
    {
        public AbilityGrantSlot slot;

        [Tooltip("Authoring rule: 1..3 SpellIds.")]
        public SpellId[] allowedSpellIds;

        [Tooltip("Optional. If set, must be within allowedSpellIds.")]
        public SpellId defaultSpellId;
    }

    // ------------------------------------------------------------------------
    // Family-specific blocks
    // ------------------------------------------------------------------------

    [Serializable]
    public struct ContainerData
    {
        public int capacitySlots;

        [Tooltip("Default true. Some special containers might forbid nesting.")]
        public bool allowNestedContainers;

        [Tooltip("Optional. Equipped backpacks may increase carry weight.")]
        public float carryWeightBonusKg;
    }

    [Serializable]
    public struct WeaponData
    {
        public WeaponHandedness handedness;
        public DamageType damageType;

        public int minDamage;
        public int maxDamage;

        [Tooltip("Seconds per swing (base). Combat will apply swing speed modifiers.")]
        public float swingSpeedSeconds;

        public int staminaCostPerSwing;

        [Tooltip("SkillId required to use this weapon.")]
        public SkillId requiredCombatSkill;

        [Tooltip("Optional.")]
        public float rangeMeters;

        [Header("Ranged")]
        public AmmoType ammoType;
    }

    [Serializable]
    public struct ArmorData
    {
        public ArmorMaterial material;
        public ArmorSlot slot;

        public int resistPhysical;
        public int resistFire;
        public int resistCold;
        public int resistPoison;
        public int resistEnergy;

        [Tooltip("0 allowed; may be negative for heavy armor.")]
        public int dexPenalty;
    }

    [Serializable]
    public struct ShieldData
    {
        public ShieldBlockType blockType;
    }

    [Serializable]
    public struct JewelryData
    {
        public JewelrySlot slot;
    }

    [Serializable]
    public struct ConsumableData
    {
        public ConsumableType type;
        public bool isUsable;
        public float useTimeSeconds;
    }

    [Serializable]
    public struct MountData
    {
        public float moveSpeedMultiplier;
        public float staminaDrainPerSecond;
    }

    [Serializable]
    public struct ReagentData
    {
        public ReagentId reagentId;
    }

    // ========================================================================
    // ENUMS (schema-required)
    // ========================================================================

    public enum ItemFamily
    {
        // Equippable families
        Bag = 0,
        Head = 1,
        Neck = 2,
        Mainhand = 3,
        Chest = 4,
        Offhand = 5,
        UtilityItem = 6,
        Foot = 7,
        Mount = 8,

        // Non-equippable families
        Resource = 20,
        Material = 21,
        Reagent = 22,
        Consumable = 23,
        Container = 24,
        Misc = 25,
    }

    public enum WeaponHandedness
    {
        MainHand = 0,
        TwoHanded = 1,
    }

    public enum DamageType
    {
        Physical = 0,
        Fire = 1,
        Cold = 2,
        Poison = 3,
        Energy = 4,
    }

    public enum AmmoType
    {
        None = 0,
        Arrow = 1,
        Bolt = 2,
    }

    public enum ArmorMaterial
    {
        Cloth = 0,
        Leather = 1,
        Metal = 2,
    }

    public enum ArmorSlot
    {
        Head = 0,
        Torso = 1,
        Feet = 2,
        Back = 3,
        NeckArmor = 4,
        Other = 99,
    }

    public enum ShieldBlockType
    {
        Basic = 0,
        Heavy = 1,
    }

    public enum JewelrySlot
    {
        Ring = 0,
        Earring = 1,
        Amulet = 2,
    }

    public enum ConsumableType
    {
        Potion = 0,
        Food = 1,
        Bandage = 2,
        Torch = 3,
        Utility = 4,
    }

}
