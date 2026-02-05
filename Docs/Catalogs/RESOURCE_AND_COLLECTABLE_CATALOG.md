# RESOURCE_AND_COLLECTABLE_CATALOG.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 0.1  
Last Updated: 2026-01-29  
Engine: Unity 6 (URP)  
Networking: Netcode for GameObjects (NGO)  
Authority: Server-authoritative  

---

## PURPOSE

Defines the authoritative **list of all resource, material, and collectable item families** in *Ultimate Dungeon*.

These items:
- Are primarily obtained in **Dungeon scenes**
- Are consumed by **crafting**, **housing/building**, **repair**, **enhancement**, and **spellcasting** systems
- Are treated as **Items** (via `ITEM_DEF_SCHEMA.md`), not Actors

If a resource is not listed here, it **does not exist**.

---

## DESIGN LOCKS (MUST ENFORCE)

1. **No resource gathering in safe scenes**
   - Gathering is only allowed when `SceneRuleFlags.ResourceGatheringAllowed == true` (Dungeon only)

2. **Resources are items, not actors**
   - Nodes (trees, rocks, corpses) are Actors/Objects
   - The harvested output is always an Item

3. **All resources are tradeable unless explicitly locked**

4. **Stackable by default**
   - Stack size is defined per ItemDef

5. **Resources are sinks**
   - Used by crafting, housing, enhancement, reagents, upkeep

---

## RESOURCE CATEGORIES (LOCKED)

- Raw Materials
- Processed Materials
- Organic Materials
- Monster Components
- Ores & Metals
- Gems & Rares
- Alchemical Reagents
- Magical Essences
- Environmental Collectables
- Currency-like Materials (non-coin)

---

## RAW MATERIALS

### Wood & Plant Matter
- Wood Log
- Hardwood Log
- Ancient Wood Log *(rare)*
- Wooden Branch
- Bark
- Plant Fiber
- Vines
- Thatch

### Stone & Earth
- Stone Chunk
- Smooth Stone
- Granite Block
- Limestone Block
- Clay
- Sand
- Gravel

### Water & Fluids
- Fresh Water
- Brackish Water
- Pure Water *(alchemical grade)*
- Lava Sample *(rare, dungeon-specific)*

---

## PROCESSED MATERIALS

### Wood Products
- Wooden Plank
- Hardwood Plank
- Treated Timber
- Charcoal

### Stone Products
- Stone Block
- Cut Stone Block
- Mortar

### Metal Intermediates
- Metal Scrap
- Metal Ingot *(generic placeholder, refined by type)*

---

## ORGANIC MATERIALS

### Hides & Leather
- Raw Hide
- Cured Leather
- Thick Leather
- Scaled Hide *(from reptiles/dragons)*

### Bone & Flesh
- Bone
- Bone Shard
- Skull
- Fresh Meat
- Preserved Meat
- Fat

---

## MONSTER COMPONENTS

- Monster Claw
- Monster Fang
- Monster Eye
- Monster Heart
- Monster Blood
- Chitin Plate
- Venom Sac
- Essence Gland

> Used for crafting, alchemy, spell reagents, and enhancement

---

## ORES & METALS

### Ores (Unrefined)
- Iron Ore
- Copper Ore
- Tin Ore
- Silver Ore
- Gold Ore
- Obsidian Ore
- Mythril Ore *(rare)*
- Adamantite Ore *(very rare)*

### Refined Metals
- Iron Ingot
- Copper Ingot
- Bronze Ingot
- Steel Ingot
- Silver Ingot
- Gold Ingot
- Mythril Ingot
- Adamantite Ingot

---

## GEMS & RARES

### Common Gems
- Quartz
- Amber
- Garnet

### Uncommon Gems
- Sapphire
- Ruby
- Emerald
- Topaz

### Rare / Legendary
- Diamond
- Void Crystal
- Dragonstone
- Soul Gem *(empty / filled variants)*

---

## ALCHEMICAL REAGENTS

*(Aligns with existing reagent definitions; listed here for completeness)*

- Black Pearl
- Blood Moss
- Garlic
- Ginseng
- Mandrake Root
- Nightshade
- Spider Silk
- Sulfurous Ash

---

## MAGICAL ESSENCES

- Arcane Essence
- Elemental Essence (Fire)
- Elemental Essence (Water)
- Elemental Essence (Air)
- Elemental Essence (Earth)
- Shadow Essence
- Light Essence
- Corrupted Essence

---

## ENVIRONMENTAL COLLECTABLES

- Ancient Coin Relic *(non-spendable; lore or conversion)*
- Ruined Artifact Fragment
- Dungeon Key Fragment
- Map Fragment
- Rune Fragment

---

## CURRENCY-LIKE MATERIALS (NON-COIN)

These are **materials**, not money, but may behave as sinks or gates.

- Inscription Scroll Blank
- Enchantment Dust
- Runic Powder
- Soul Ash

---

## OPEN QUESTIONS (NOT LOCKED)

- Do you want **food spoilage** for organic materials?
- Should some rare resources be **non-tradeable**?
- Do we want **biome-specific exclusives** per dungeon tier?

---

## NEXT DEPENDENCIES

1. Create `ItemDef` entries for each resource
2. Define stack sizes and weights
3. Link gathering nodes (Tree, Rock, Corpse) to drop tables
4. Reference resources in:
   - `HOUSE_OBJECT_DEF_SCHEMA.md` (build costs)
   - Crafting system docs
   - Enhancement system

---

## DESIGN LOCK CONFIRMATION

This document is **authoritative**.

Any change must:
- Increment Version
- Update Last Updated
- Call out impacted systems (Items, Crafting, Housing, Loot Tables)

