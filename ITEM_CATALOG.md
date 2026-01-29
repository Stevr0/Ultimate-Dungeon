# ITEM_CATALOG.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 1.1  
Last Updated: 2026-01-29  
Engine: Unity 6 (URP)  
Networking: Netcode for GameObjects (NGO)  
Authority: Server-authoritative  

---

## PURPOSE

Defines the authoritative list of **all ItemIds** used in *Ultimate Dungeon*.

This document:
- Locks **stable identity** (ItemId strings)
- Does **not** define balance, stats, or behavior
- Is referenced by ItemDefs, loot tables, crafting, housing, combat, and magic systems

If an ItemId is not defined here, it **does not exist**.

---

## DESIGN LOCKS (MUST ENFORCE)

1. **ItemIds are permanent**
   - Never reuse an ItemId
   - Retired ItemIds remain reserved forever

2. **No balance data in this file**
   - Stack size, weight, durability, and rules live in `ItemDef` assets

3. **Single source of identity**
   - All systems must reference ItemIds from this catalog only

---

## ITEM ID PREFIX CONVENTIONS (LOCKED)

| Prefix | Meaning |
|---|---|
| `ITEM_RES_` | Raw / processed resources |
| `ITEM_GEM_` | Gems & rare materials |
| `ITEM_REG_` | Alchemical reagents |
| `ITEM_ESS_` | Magical essences |
| `ITEM_COL_` | Special / lore collectables |
| `ITEM_MAT_` | Crafting & enhancement materials |
| `ITEM_WPN_` | Weapons |
| `ITEM_ARM_` | Armor |
| `ITEM_JEW_` | Jewelry |
| `ITEM_CON_` | Consumables |
| `ITEM_TOL_` | Tools |
| `ITEM_MISC_` | Miscellaneous |

---

## RESOURCES (AUTHORITATIVE ITEM IDS)

> Identity-only list.  
> Meaning, sourcing, and usage are defined in `RESOURCE_AND_COLLECTABLE_CATALOG.md`.

### Raw Materials — Wood & Plant
- ITEM_RES_WoodLog
- ITEM_RES_HardwoodLog
- ITEM_RES_AncientWoodLog
- ITEM_RES_WoodenBranch
- ITEM_RES_Bark
- ITEM_RES_PlantFiber
- ITEM_RES_Vines
- ITEM_RES_Thatch

### Raw Materials — Stone & Earth
- ITEM_RES_StoneChunk
- ITEM_RES_SmoothStone
- ITEM_RES_GraniteBlock
- ITEM_RES_LimestoneBlock
- ITEM_RES_Clay
- ITEM_RES_Sand
- ITEM_RES_Gravel

### Raw Materials — Water & Fluids
- ITEM_RES_FreshWater
- ITEM_RES_BrackishWater
- ITEM_RES_PureWater
- ITEM_RES_LavaSample

### Processed Materials — Wood
- ITEM_RES_WoodenPlank
- ITEM_RES_HardwoodPlank
- ITEM_RES_TreatedTimber
- ITEM_RES_Charcoal

### Processed Materials — Stone
- ITEM_RES_StoneBlock
- ITEM_RES_CutStoneBlock
- ITEM_RES_Mortar

### Processed Materials — Metal Intermediates
- ITEM_RES_MetalScrap

### Organic Materials — Hides & Leather
- ITEM_RES_RawHide
- ITEM_RES_CuredLeather
- ITEM_RES_ThickLeather
- ITEM_RES_ScaledHide

### Organic Materials — Bone & Flesh
- ITEM_RES_Bone
- ITEM_RES_BoneShard
- ITEM_RES_Skull
- ITEM_RES_FreshMeat
- ITEM_RES_PreservedMeat
- ITEM_RES_Fat

### Monster Components
- ITEM_RES_MonsterClaw
- ITEM_RES_MonsterFang
- ITEM_RES_MonsterEye
- ITEM_RES_MonsterHeart
- ITEM_RES_MonsterBlood
- ITEM_RES_ChitinPlate
- ITEM_RES_VenomSac
- ITEM_RES_EssenceGland

### Ores (Unrefined)
- ITEM_RES_IronOre
- ITEM_RES_CopperOre
- ITEM_RES_TinOre
- ITEM_RES_SilverOre
- ITEM_RES_GoldOre
- ITEM_RES_ObsidianOre
- ITEM_RES_MythrilOre
- ITEM_RES_AdamantiteOre

### Refined Metals
- ITEM_RES_IronIngot
- ITEM_RES_CopperIngot
- ITEM_RES_BronzeIngot
- ITEM_RES_SteelIngot
- ITEM_RES_SilverIngot
- ITEM_RES_GoldIngot
- ITEM_RES_MythrilIngot
- ITEM_RES_AdamantiteIngot

### Gems & Rares — Common
- ITEM_GEM_Quartz
- ITEM_GEM_Amber
- ITEM_GEM_Garnet

### Gems & Rares — Uncommon
- ITEM_GEM_Sapphire
- ITEM_GEM_Ruby
- ITEM_GEM_Emerald
- ITEM_GEM_Topaz

### Gems & Rares — Legendary
- ITEM_GEM_Diamond
- ITEM_GEM_VoidCrystal
- ITEM_GEM_Dragonstone
- ITEM_GEM_SoulGem_Empty
- ITEM_GEM_SoulGem_Filled

### Alchemical Reagents
- ITEM_REG_BlackPearl
- ITEM_REG_BloodMoss
- ITEM_REG_Garlic
- ITEM_REG_Ginseng
- ITEM_REG_MandrakeRoot
- ITEM_REG_Nightshade
- ITEM_REG_SpiderSilk
- ITEM_REG_SulfurousAsh

### Magical Essences
- ITEM_ESS_Arcane
- ITEM_ESS_Elemental_Fire
- ITEM_ESS_Elemental_Water
- ITEM_ESS_Elemental_Air
- ITEM_ESS_Elemental_Earth
- ITEM_ESS_Shadow
- ITEM_ESS_Light
- ITEM_ESS_Corrupted

### Environmental & Special Collectables
- ITEM_COL_AncientCoinRelic
- ITEM_COL_RuinedArtifactFragment
- ITEM_COL_DungeonKeyFragment
- ITEM_COL_MapFragment
- ITEM_COL_RuneFragment

### Crafting & Enhancement Materials
- ITEM_MAT_InscriptionScrollBlank
- ITEM_MAT_EnchantmentDust
- ITEM_MAT_RunicPowder
- ITEM_MAT_SoulAsh

---

## RETIRED / RESERVED ITEM IDS

- None (v1.1)

---

## DESIGN LOCK CONFIRMATION

This document is **authoritative**.

Any change must:
- Increment Version
- Update Last Updated
- Call out impacted systems (Items, Loot, Crafting, Housing, Magic)
