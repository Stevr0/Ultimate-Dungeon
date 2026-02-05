# ITEM_CATALOG.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 2.1  
Last Updated: 2026-02-02  
Engine: Unity 6 (URP)  
Authority: Server-authoritative  
Data: ScriptableObjects-first (`ItemDef`)

---

## PURPOSE

This document is the **authoritative catalog of all base items** in *Ultimate Dungeon*.

It specifies:
- The **full list** of `ItemDefId`s that exist in the shipped game
- Each item’s **base authored values** (weight, durability, core stats)
- Which **Affix Pools** the item may roll from when it becomes Magical
- The item’s **equipment identity** and its **item-granted ability choices**

This catalog maps 1:1 to `ItemDef` ScriptableObject assets.

**Important boundaries:**
- `ITEMS.md` defines the item system laws.
- `ITEM_AFFIX_CATALOG.md` defines what affixes exist and how many can roll.
- `ITEM_DEF_SCHEMA.md` defines the ItemDef fields and validation rules.
- `SPELL_ID_CATALOG.md` + `SPELL_DEF_SCHEMA.md` define spell/ability IDs and payload rules.

---

## DESIGN LOCKS (ABSOLUTE)

1. **Stable IDs**
   - `ItemDefId` values are stable.
   - Never reorder or rename shipped IDs without explicit migration.

2. **Equip slots (10-slot model)**
   - `Bag, Head, Neck, Mainhand, Chest, Offhand, BeltA, BeltB, Foot, Mount`

3. **Item families**
   - For equippable items, `ItemFamily` matches the equipment family:
     - `Bag, Head, Neck, Mainhand, Chest, Offhand, UtilityItem, Foot, Mount`

   **UtilityItem rule (LOCKED):**
   - `UtilityItem` items may be equipped into **either** `BeltA` or `BeltB`.
   - Players may equip **any combination** (2 potions, 2 foods, potion+bandage, etc.).

4. **Item-granted abilities**
   - Each equippable item grants **1–3 selectable abilities**.
   - Ability choices are `SpellId`s.
   - The player’s chosen ability is stored on the **ItemInstance** (not here).

5. **Reagents are for potions (Alchemy), not spells**
   - Spellcasting never consumes `Reagent` items.

6. **Rings are Offhand, Earrings are Neck**
   - Rings equip into `Offhand`.
   - Earrings equip into `Neck` (shared with capes/cloaks).

7. **Catalog → Assets**
   - Every entry here must have a corresponding `ItemDef` asset.
   - No “secret” ItemDefs outside this catalog.

---

## ITEMDEF ID RULES

Use stable string IDs:

### Equippable Families
- `bag_<name>`
- `head_<name>`
- `neck_<name>` *(capes/cloaks + earrings/amulets)*
- `mainhand_<weaponType>_<name>`
- `chest_<material>_<name>`
- `offhand_<type>_<name>` *(shields + rings)*
- `utility_<name>` *(potions, food, bandages, torches, etc. — any belt-usable item)*
- `foot_<material>_<name>`
- `mount_<name>`

### Non-Equip Families
- `reagent_<name>` *(alchemy-only reagents)*
- `material_<name>`
- `resource_<name>`
- `consumable_<name>` *(non-equippable consumables)*
- `container_<name>` *(non-equippable containers)*
- `misc_<name>`

---

## NUMERIC STATUS

All numeric values below are **PROPOSED (Not Locked)** until balance passes finalize.

---

## AFFIX POOLS (REFERENCE NAMES)

This catalog references pool names that will become `AffixPoolDef` assets.

> NOTE: Pool naming is content-facing only; the affix IDs and ranges are owned by `ITEM_AFFIX_CATALOG.md`.

- `Pool_Mainhand_Common`
- `Pool_Mainhand_Rare`
- `Pool_Armor_Common` *(Head/Chest/Foot/Neck capes)*
- `Pool_Armor_Rare`
- `Pool_Offhand_Common` *(Shields/Rings)*
- `Pool_Offhand_Rare`
- `Pool_Bag_Common`
- `Pool_Mount_Common`

---

## ABILITY GRANTS (AUTHORING FORMAT)

Each equippable item grants **1–3** selectable abilities.

We author them as:
- **Primary:** main identity action
- **Secondary:** alternate action
- **Utility:** mobility/defense/cleanse/etc.

Each slot lists **1–3** allowed `SpellId` choices.

---

# EQUIPPABLE ITEMS

## BAG

> Bags are Containers that equip into `Bag`.

Design lock:
- **All backpacks have exactly 48 slots**.
- Backpacks differ by their **weight capacity increase** (carry weight bonus), not slot count.

| ItemDefId | Name | Weight | Dur | CapacitySlots | CarryWeightBonusKg | Granted Abilities (choose 1 per slot) | AffixPools |
|---|---|---:|---:|---:|---:|---|---|
| bag_worn_pack | Worn Pack | 1.0 | 60 | 48 | 10 | Utility: {CreateFood, NightSight, Telekinesis} | Pool_Bag_Common |
| bag_traveler_pack | Traveler Pack | 1.2 | 70 | 48 | 20 | Utility: {NightSight, Telekinesis, Unlock} | Pool_Bag_Common |
| bag_mercenary_pack | Mercenary Pack | 1.4 | 80 | 48 | 30 | Utility: {Telekinesis, Unlock, MagicLock} | Pool_Bag_Common |

---

## HEAD

> Head items are armor pieces.

| ItemDefId | Name | Weight | Dur | Material | Resists (P/F/C/Po/E) | DexPenalty | Granted Abilities | AffixPools |
|---|---|---:|---:|---|---|---:|---|---|
| head_cloth_hood | Cloth Hood | 0.5 | 40 | Cloth | 1/1/1/1/1 | 0 | Utility: {NightSight, Protection, ReactiveArmor} | Pool_Armor_Common |
| head_leather_cap | Leather Cap | 0.7 | 55 | Leather | 2/1/1/2/1 | 0 | Utility: {Protection, ReactiveArmor, Cure} | Pool_Armor_Common |
| head_metal_helm | Metal Helm | 1.2 | 80 | Metal | 3/1/2/2/1 | -1 | Utility: {ReactiveArmor, MagicReflection, Protection} | Pool_Armor_Common |

---

## NECK (Capes/Cloaks + Earrings)

> `Neck` is a shared slot.

### Capes / Cloaks

| ItemDefId | Name | Weight | Dur | Type | Resists (P/F/C/Po/E) | DexPenalty | Granted Abilities | AffixPools |
|---|---|---:|---:|---|---|---:|---|---|
| neck_cloth_cape | Cloth Cape | 0.4 | 35 | Cape | 0/1/1/0/0 | 0 | Utility: {Incognito, Invisibility, Reveal} | Pool_Armor_Common |
| neck_leather_cloak | Leather Cloak | 0.6 | 50 | Cloak | 1/1/0/1/0 | 0 | Utility: {Invisibility, Reveal, Protection} | Pool_Armor_Common |
| neck_metal_mantle | Metal Mantle | 0.9 | 70 | Mantle | 2/0/1/1/0 | -1 | Utility: {MagicReflection, Protection, ReactiveArmor} | Pool_Armor_Common |

### Earrings

| ItemDefId | Name | Weight | Dur | JewelryType | Granted Abilities | AffixPools |
|---|---|---:|---:|---|---|---|
| neck_earrings_copper | Copper Earrings | 0.1 | 30 | Earring | Utility: {Cure, Heal, NightSight} | Pool_Offhand_Common |
| neck_earrings_silver | Silver Earrings | 0.1 | 40 | Earring | Utility: {Heal, GreaterHeal, Cure} | Pool_Offhand_Common |
| neck_earrings_gold | Gold Earrings | 0.1 | 50 | Earring | Utility: {Protection, MagicReflection, GreaterHeal} | Pool_Offhand_Rare |

---

## MAINHAND

> Mainhand items are weapons.

### Swords

| ItemDefId | Name | Weight | Dur | Handed | Damage | Swing | Stam | Skill | Granted Abilities | AffixPools |
|---|---|---:|---:|---|---:|---:|---:|---|---|---|
| mainhand_sword_dagger | Dagger | 1.0 | 40 | MainHand | 3–6 | 1.75 | 2 | Fencing | Primary: {Harm, MagicArrow}; Utility: {Weaken, Clumsy} | Pool_Mainhand_Common |
| mainhand_sword_shortsword | Short Sword | 1.6 | 45 | MainHand | 4–8 | 2.00 | 3 | Swords | Primary: {MagicArrow, Fireball}; Utility: {Weaken, Strength} | Pool_Mainhand_Common |
| mainhand_sword_katana | Katana | 2.2 | 65 | TwoHanded | 6–12 | 2.25 | 4 | Swords | Primary: {Fireball, Lightning}; Utility: {Bless, Protection} | Pool_Mainhand_Rare |

### Axes (shares some abilities; keeps unique options)

| ItemDefId | Name | Weight | Dur | Handed | Damage | Swing | Stam | Skill | Granted Abilities | AffixPools |
|---|---|---:|---:|---|---:|---:|---:|---|---|---|
| mainhand_axe_hatchet | Hatchet | 2.0 | 55 | MainHand | 5–9 | 2.25 | 4 | Macing | Primary: {Fireball, Harm}; Utility: {Weaken, Curse} | Pool_Mainhand_Common |
| mainhand_axe_battleaxe | Battle Axe | 3.5 | 80 | TwoHanded | 8–14 | 2.75 | 6 | Macing | Primary: {Lightning, Explosion}; Utility: {Curse, Bless} | Pool_Mainhand_Rare |

---

## CHEST

> Chest items are the primary armor body piece.

| ItemDefId | Name | Weight | Dur | Material | Resists (P/F/C/Po/E) | DexPenalty | Granted Abilities | AffixPools |
|---|---|---:|---:|---|---|---:|---|---|
| chest_cloth_robe | Cloth Robe | 1.0 | 45 | Cloth | 2/2/2/2/2 | 0 | Primary: {ReactiveArmor, MagicReflection}; Utility: {Protection, Bless} | Pool_Armor_Common |
| chest_leather_tunic | Leather Tunic | 1.8 | 70 | Leather | 4/2/2/4/2 | 0 | Primary: {Protection, Agility}; Utility: {Cure, Bless} | Pool_Armor_Common |
| chest_metal_cuirass | Metal Cuirass | 3.0 | 100 | Metal | 6/2/3/4/2 | -2 | Primary: {MagicReflection, ReactiveArmor}; Utility: {Strength, Bless} | Pool_Armor_Rare |

---

## OFFHAND (Shields + Rings)

### Shields

| ItemDefId | Name | Weight | Dur | BlockType | Granted Abilities | AffixPools |
|---|---|---:|---:|---|---|---|
| offhand_shield_buckler | Buckler | 2.0 | 70 | Basic | Utility: {Protection, ReactiveArmor, ArchProtection} | Pool_Offhand_Common |
| offhand_shield_kite | Kite Shield | 3.0 | 90 | Heavy | Utility: {Protection, MagicReflection, ArchProtection} | Pool_Offhand_Common |

### Rings (equip into Offhand)

| ItemDefId | Name | Weight | Dur | JewelryType | Granted Abilities | AffixPools |
|---|---|---:|---:|---|---|---|
| offhand_ring_copper_band | Copper Band | 0.1 | 30 | Ring | Utility: {Agility, Strength, Cunning} | Pool_Offhand_Common |
| offhand_ring_silver_band | Silver Band | 0.1 | 40 | Ring | Utility: {Bless, Protection, Cunning} | Pool_Offhand_Common |
| offhand_ring_gold_band | Gold Band | 0.1 | 50 | Ring | Utility: {MagicReflection, Bless, Protection} | Pool_Offhand_Rare |

---

## UTILITYITEM (BeltA / BeltB)

> `BeltA` and `BeltB` are generic quick-slots.
> Any item with `ItemFamily = UtilityItem` can be equipped into **either** slot.

| ItemDefId | Name | Kind | Weight | Stackable | StackMax | UseTime | Granted Abilities | AffixPools |
|---|---|---|---:|---|---:|---:|---|---|
| utility_lesser_heal_potion | Lesser Heal Potion | Potion | 0.2 | Yes | 10 | 0.5s | Primary: {Heal, GreaterHeal} | *(none)* |
| utility_cure_potion | Cure Potion | Potion | 0.2 | Yes | 10 | 0.5s | Primary: {Cure, ArchCure} | *(none)* |
| utility_refresh_potion | Refresh Potion | Potion | 0.2 | Yes | 10 | 0.5s | Utility: {Agility, Bless} | *(none)* |
| utility_bread | Bread | Food | 0.3 | Yes | 20 | 1.0s | Utility: {Heal, Bless} | *(none)* |
| utility_cooked_meat | Cooked Meat | Food | 0.4 | Yes | 20 | 1.0s | Utility: {Strength, Heal} | *(none)* |
| utility_bandage | Bandage | Bandage | 0.1 | Yes | 50 | 1.5s | Primary: {Heal, GreaterHeal} | *(none)* |
| utility_torch | Torch | Utility | 0.8 | No | 1 | 0.2s | Utility: {NightSight, Reveal} | *(none)* |

---

## FOOT

> Boots are armor pieces that equip into `Foot`.

| ItemDefId | Name | Weight | Dur | Material | Resists (P/F/C/Po/E) | DexPenalty | Granted Abilities | AffixPools |
|---|---|---:|---:|---|---|---:|---|---|
| foot_cloth_shoes | Cloth Shoes | 0.5 | 40 | Cloth | 1/1/1/1/1 | 0 | Utility: {Agility, Teleport, Bless} | Pool_Armor_Common |
| foot_leather_boots | Leather Boots | 0.8 | 60 | Leather | 2/1/1/2/1 | 0 | Utility: {Agility, Teleport, Invisibility} | Pool_Armor_Common |
| foot_metal_greaves | Metal Greaves | 1.5 | 85 | Metal | 3/1/2/2/1 | -1 | Utility: {Strength, Protection, Teleport} | Pool_Armor_Common |

---

## MOUNT

> Mount is an equippable mount item.

| ItemDefId | Name | Weight | Dur | MoveSpeedMult | StaminaDrain/s | Granted Abilities | AffixPools |
|---|---|---:|---:|---:|---:|---|---|
| mount_pack_mule | Pack Mule | 0.0 | 100 | 1.05 | 0.0 | Utility: {CreateFood, NightSight} | Pool_Mount_Common |
| mount_riding_horse | Riding Horse | 0.0 | 120 | 1.10 | 0.0 | Utility: {Agility, Bless} | Pool_Mount_Common |
| mount_war_horse | War Horse | 0.0 | 140 | 1.12 | 0.0 | Utility: {Protection, Bless, ArchProtection} | Pool_Mount_Common |

---

# NON-EQUIPPABLE ITEMS (STARTER)

## REAGENTS (ALCHEMY ONLY)

| ItemDefId | Name | Weight | Stackable | StackMax |
|---|---|---:|---|---:|
| reagent_ginseng | Ginseng | 0.01 | Yes | 100 |
| reagent_garlic | Garlic | 0.01 | Yes | 100 |
| reagent_nightshade | Nightshade | 0.01 | Yes | 100 |

---

## MATERIALS / RESOURCES (STARTER)

| ItemDefId | Name | Weight | Stackable | StackMax |
|---|---|---:|---|---:|
| material_iron_ingot | Iron Ingot | 0.10 | Yes | 100 |
| material_leather | Leather | 0.05 | Yes | 200 |
| material_cloth | Cloth | 0.05 | Yes | 200 |

---

## DESIGN LOCK CONFIRMATION

This document is **authoritative**.

Any change must:
- Increment Version
- Update Last Updated
- Maintain stable IDs (migration required for changes)

