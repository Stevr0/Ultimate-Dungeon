# ITEMDEF_RESOURCE_TEMPLATES.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 0.1  
Last Updated: 2026-01-29  
Engine: Unity 6 (URP)  
Networking: Netcode for GameObjects (NGO)  
Authority: Server-authoritative  

---

## PURPOSE

Defines **default ItemDef templates** for **Resource-type items** in *Ultimate Dungeon*.

These templates provide:
- Consistent baseline values (stack size, weight, trade rules)
- A fast, safe way to author new `ItemDef` assets
- Predictable behavior across loot, crafting, housing, and vendors

Templates are **authoritative starting points**, not balance locks.

---

## SCOPE & RESPONSIBILITY

This document:
- Defines **default values by resource category**
- Does NOT define drop rates, rarity, or crafting recipes
- Does NOT override per-item tuning when explicitly needed

Dependencies:
- `ITEM_CATALOG.md` (ItemId identity)
- `ITEM_DEF_SCHEMA.md` (field definitions)
- `RESOURCE_AND_COLLECTABLE_CATALOG.md` (meaning & sourcing)

---

## GLOBAL DESIGN LOCKS (MUST ENFORCE)

1. **All resources are stackable**
2. **All resources are tradeable by default**
   - Exceptions must be explicitly documented
3. **Resources have no durability**
4. **Resources do not grant power directly**
   - Power only emerges when consumed by systems

---

## COMMON ITEMDEF FLAGS (RESOURCES)

Unless explicitly overridden:

- `ItemType = Resource`
- `EquipSlot = None`
- `IsEquipable = false`
- `HasDurability = false`
- `CanBeInsured = false`
- `CanBeDestroyed = true`
- `CanBeDroppedOnDeath = true`
- `CanBeTraded = true`
- `CanBeStored = true`

---

## TEMPLATE: RAW MATERIAL (LIGHT)

**Use for:**
- Logs, plant matter, fibers, hides, bones

```yaml
TemplateId: Resource_Raw_Light
StackSizeMax: 100
WeightPerUnit: 0.10
ValuePerUnit: 1
Tradeable: true
VendorSellAllowed: true
VendorBuyAllowed: false
```

Examples:
- ITEM_RES_WoodLog
- ITEM_RES_PlantFiber
- ITEM_RES_BoneShard

---

## TEMPLATE: RAW MATERIAL (HEAVY)

**Use for:**
- Stone, clay, sand, gravel

```yaml
TemplateId: Resource_Raw_Heavy
StackSizeMax: 50
WeightPerUnit: 0.50
ValuePerUnit: 1
Tradeable: true
VendorSellAllowed: true
VendorBuyAllowed: false
```

Examples:
- ITEM_RES_StoneChunk
- ITEM_RES_Clay
- ITEM_RES_Gravel

---

## TEMPLATE: PROCESSED MATERIAL

**Use for:**
- Planks, cut stone, ingots, mortar

```yaml
TemplateId: Resource_Processed
StackSizeMax: 100
WeightPerUnit: 0.25
ValuePerUnit: 3
Tradeable: true
VendorSellAllowed: true
VendorBuyAllowed: false
```

Examples:
- ITEM_RES_WoodenPlank
- ITEM_RES_IronIngot
- ITEM_RES_CutStoneBlock

---

## TEMPLATE: ORE (UNREFINED)

**Use for:**
- All unrefined ores

```yaml
TemplateId: Resource_Ore
StackSizeMax: 50
WeightPerUnit: 0.75
ValuePerUnit: 4
Tradeable: true
VendorSellAllowed: true
VendorBuyAllowed: false
```

Examples:
- ITEM_RES_IronOre
- ITEM_RES_GoldOre
- ITEM_RES_MythrilOre

---

## TEMPLATE: ORGANIC MATERIAL

**Use for:**
- Meat, fat, hides, monster organs

```yaml
TemplateId: Resource_Organic
StackSizeMax: 50
WeightPerUnit: 0.30
ValuePerUnit: 2
Tradeable: true
VendorSellAllowed: true
VendorBuyAllowed: false
```

Examples:
- ITEM_RES_FreshMeat
- ITEM_RES_MonsterHeart
- ITEM_RES_ScaledHide

---

## TEMPLATE: ALCHEMICAL REAGENT

**Use for:**
- Spell and potion reagents

```yaml
TemplateId: Resource_Reagent
StackSizeMax: 100
WeightPerUnit: 0.05
ValuePerUnit: 5
Tradeable: true
VendorSellAllowed: true
VendorBuyAllowed: true
```

Examples:
- ITEM_REG_BlackPearl
- ITEM_REG_Ginseng
- ITEM_REG_Nightshade

---

## TEMPLATE: MAGICAL ESSENCE

**Use for:**
- Essences, arcane residues

```yaml
TemplateId: Resource_Essence
StackSizeMax: 100
WeightPerUnit: 0.02
ValuePerUnit: 10
Tradeable: true
VendorSellAllowed: true
VendorBuyAllowed: false
```

Examples:
- ITEM_ESS_Arcane
- ITEM_ESS_Shadow

---

## TEMPLATE: GEM / RARE MATERIAL

**Use for:**
- Gems, soul items, rare crafting inputs

```yaml
TemplateId: Resource_Gem
StackSizeMax: 20
WeightPerUnit: 0.05
ValuePerUnit: 25
Tradeable: true
VendorSellAllowed: true
VendorBuyAllowed: false
```

Examples:
- ITEM_GEM_Ruby
- ITEM_GEM_Diamond
- ITEM_GEM_Dragonstone

---

## TEMPLATE: SPECIAL / LORE COLLECTABLE

**Use for:**
- Quest items, fragments, relics

```yaml
TemplateId: Resource_Collectable_Special
StackSizeMax: 10
WeightPerUnit: 0.10
ValuePerUnit: 0
Tradeable: false
VendorSellAllowed: false
VendorBuyAllowed: false
```

Examples:
- ITEM_COL_MapFragment
- ITEM_COL_DungeonKeyFragment

---

## OVERRIDE POLICY (LOCKED)

Overrides are allowed **only when justified**:
- Stack size (very rare items)
- Tradeability (quest gating)
- Weight (extreme fantasy materials)

All overrides must be documented in the specific `ItemDef`.

---

## NEXT DEPENDENCIES

1. Batch-create `ItemDef` assets using these templates
2. Wire templates into an ItemDef creation tool (editor utility)
3. Reference ValuePerUnit in:
   - Vendor pricing
   - Insurance
   - Repair & enhancement sinks

---

## DESIGN LOCK CONFIRMATION

This document is **authoritative**.

Any change must:
- Increment Version
- Update Last Updated
- Call out impacted systems (Items, Economy, Loot, Crafting)

