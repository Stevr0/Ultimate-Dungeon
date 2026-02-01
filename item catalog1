# ITEM_CATALOG.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 1.2  
Last Updated: 2026-01-28  
Engine: Unity 6 (URP)  
Authority: Server-authoritative  
Data: ScriptableObjects-first (`ItemDef`)

---

## PURPOSE

This document is the **authoritative catalog of all base items** in *Ultimate Dungeon*.

It specifies:
- The **full list** of `ItemDefId`s that exist in the shipped game
- Each item’s **base stats** (weapon/armor/jewelry/etc.)
- Which **Affix Pools** the item may roll from when it becomes Magical

This catalog maps 1:1 to `ItemDef` ScriptableObject assets.

**Important:**
- `ITEMS.md` defines the **system laws**.
- `ITEM_AFFIX_CATALOG.md` defines **what can roll**.
- This document defines **what items exist** and their **base values**.

---

## DESIGN LOCKS (ABSOLUTE)

1. **Stable IDs**
   - `ItemDefId` values are stable.
   - Never reorder or rename shipped IDs without explicit migration.

2. **Base stats are authored**
   - Base weapon/armor numbers come from `ItemDef`, not runtime logic.

3. **Mundane vs Magical**
   - Mundane items have no bonus modifiers/affixes.
   - Magical items are created by loot or enhancement and receive affixes.

4. **Jewelry durability is enabled**
   - Jewelry items have durability and can break like other equipment.

5. **Material-based base resist profiles are locked**
   - Cloth, Leather, and Metal armor use locked baseline resist profiles + slot scalars.

6. **Archery is a separate item family**
   - Bows/crossbows are distinct weapons.
   - Ammunition (arrows/bolts) are separate stackable items consumed on use.

7. **Catalog → Assets**
   - Every entry here must have a corresponding `ItemDef` asset.
   - No “secret” ItemDefs outside the catalog.

---

## ITEMDEF NAMING & ID RULES

Use stable string IDs:
- `weapon_<family>_<name>`
- `armor_<material>_<slot>_<name>`
- `shield_<name>`
- `jewel_<type>_<name>`
- `consumable_<name>`
- `reagent_<name>`
- `resource_<name>`
- `container_<name>`

Examples:
- `weapon_sword_katana`
- `armor_leather_torso_tunic`
- `jewel_ring_gold_band`
- `reagent_pearl`
- `resource_iron_ingot`

---

## NUMERIC STATUS

All numeric values below are **PROPOSED (Not Locked)** until Combat baselines are finalized.

---

## AFFIX POOLS (REFERENCE NAMES)

This catalog references pool names that will become `AffixPoolDef` assets.

- `Pool_Weapon_Common`
- `Pool_Weapon_Rare`
- `Pool_Armor_Common`
- `Pool_Armor_Rare`
- `Pool_Shield_Common`
- `Pool_Shield_Rare`
- `Pool_Jewelry_Common`
- `Pool_Jewelry_Rare`

---

## WEAPONS — MELEE

### Table Columns

- `ItemDefId`
- `Name`
- `Handedness` (MainHand / TwoHanded)
- `DmgType`
- `Damage` (min–max)
- `Swing` (sec)
- `Stam`
- `Skill` (Swords/Macing/Fencing)
- `Dur`
- `AffixPools`

### SWORDS

| ItemDefId | Name | Handedness | DmgType | Damage | Swing | Stam | Skill | Dur | AffixPools |
|---|---|---|---|---:|---:|---:|---|---:|---|
| weapon_sword_dagger | Dagger | MainHand | Physical | 3–6 | 1.75 | 2 | Fencing | 40 | Pool_Weapon_Common |
| weapon_sword_shortsword | Short Sword | MainHand | Physical | 4–8 | 2.00 | 3 | Swords | 45 | Pool_Weapon_Common |
| weapon_sword_broadsword | Broadsword | MainHand | Physical | 6–10 | 2.25 | 4 | Swords | 55 | Pool_Weapon_Common |
| weapon_sword_longsword | Longsword | MainHand | Physical | 7–11 | 2.50 | 4 | Swords | 60 | Pool_Weapon_Rare |
| weapon_sword_scimitar | Scimitar | MainHand | Physical | 5–9 | 2.00 | 3 | Swords | 50 | Pool_Weapon_Common |
| weapon_sword_katana | Katana | TwoHanded | Physical | 8–12 | 2.25 | 4 | Swords | 60 | Pool_Weapon_Rare |

### MACING

| ItemDefId | Name | Handedness | DmgType | Damage | Swing | Stam | Skill | Dur | AffixPools |
|---|---|---|---|---:|---:|---:|---|---:|---|
| weapon_mace_club | Club | MainHand | Physical | 4–8 | 2.00 | 3 | Macing | 45 | Pool_Weapon_Common |
| weapon_mace_mace | Mace | MainHand | Physical | 6–10 | 2.25 | 4 | Macing | 55 | Pool_Weapon_Common |
| weapon_mace_warhammer | War Hammer | TwoHanded | Physical | 9–14 | 2.75 | 5 | Macing | 65 | Pool_Weapon_Rare |
| weapon_mace_maul | Maul | TwoHanded | Physical | 10–15 | 3.00 | 6 | Macing | 70 | Pool_Weapon_Rare |

### FENCING

| ItemDefId | Name | Handedness | DmgType | Damage | Swing | Stam | Skill | Dur | AffixPools |
|---|---|---|---|---:|---:|---:|---|---:|---|
| weapon_fence_kryss | Kryss | MainHand | Physical | 5–9 | 1.75 | 3 | Fencing | 45 | Pool_Weapon_Rare |
| weapon_fence_spear | Spear | TwoHanded | Physical | 7–12 | 2.50 | 4 | Fencing | 60 | Pool_Weapon_Common |
| weapon_fence_warfork | War Fork | TwoHanded | Physical | 8–13 | 2.75 | 5 | Fencing | 60 | Pool_Weapon_Rare |

---

## WEAPONS — RANGED (ARCHERY)

Ranged weapons consume ammunition items.

### Table Columns

- `ItemDefId`
- `Name`
- `Handedness`
- `Damage` (min–max)
- `Swing` (sec)
- `Stam`
- `Skill = Archery`
- `Dur`
- `AmmoType` (Arrow/Bolt)
- `AffixPools`

| ItemDefId | Name | Handedness | Damage | Swing | Stam | Skill | Dur | AmmoType | AffixPools |
|---|---|---|---:|---:|---:|---|---:|---|---|
| weapon_bow_shortbow | Short Bow | TwoHanded | 6–10 | 2.50 | 4 | Archery | 55 | Arrow | Pool_Weapon_Common |
| weapon_bow_longbow | Long Bow | TwoHanded | 7–12 | 2.75 | 5 | Archery | 60 | Arrow | Pool_Weapon_Rare |
| weapon_bow_crossbow | Crossbow | TwoHanded | 8–13 | 3.00 | 6 | Archery | 65 | Bolt | Pool_Weapon_Rare |
| weapon_bow_heavycrossbow | Heavy Crossbow | TwoHanded | 10–15 | 3.25 | 7 | Archery | 70 | Bolt | Pool_Weapon_Rare |

---

## AMMUNITION

Ammunition is **consumed on use** (hit or miss), resolved in Combat.

| ItemDefId | Name | AmmoType | DamageBonus | StackMax | Weight |
|---|---|---|---:|---:|---:|
| ammo_arrow | Arrow | Arrow | +0 | 200 | 0.01 |
| ammo_bolt | Crossbow Bolt | Bolt | +0 | 200 | 0.01 |

---

## ARMOR — MATERIAL BASE RESIST PROFILES (LOCKED)

Armor base resists are authored using a two-step model:

1) **Material Profile** (locked baseline per material)
2) **Slot Scalar** (locked)

### Profile Values (LOCKED)

| Material | Physical | Fire | Cold | Poison | Energy |
|---|---:|---:|---:|---:|---:|
| Cloth | 6 | 2 | 2 | 2 | 2 |
| Leather | 10 | 4 | 4 | 4 | 4 |
| Metal | 14 | 6 | 6 | 6 | 6 |

### Slot Scalars (LOCKED)

| Slot | Scalar |
|---|---:|
| Head | 0.12 |
| Torso | 0.30 |
| Arms | 0.16 |
| Hands | 0.10 |
| Legs | 0.22 |
| NeckArmor | 0.10 |

### Authoring Rule (LOCKED)

For each armor piece:
- `PieceResist[type] = round(Profile[type] * SlotScalar)`

The tables below follow this rule.

---

## ARMOR — CLOTH (FULL SET)

| ItemDefId | Name | Material | Slot | Resists (P/F/C/Po/E) | DexPen | Dur | AffixPools |
|---|---|---|---|---|---:|---:|---|
| armor_cloth_head_hood | Cloth Hood | Cloth | Head | 1/0/0/0/0 | 0 | 25 | Pool_Armor_Common |
| armor_cloth_torso_robe | Cloth Robe | Cloth | Torso | 2/1/1/1/1 | 0 | 35 | Pool_Armor_Rare |
| armor_cloth_arms_sleeves | Cloth Sleeves | Cloth | Arms | 1/0/0/0/0 | 0 | 25 | Pool_Armor_Common |
| armor_cloth_hands_gloves | Cloth Gloves | Cloth | Hands | 1/0/0/0/0 | 0 | 20 | Pool_Armor_Common |
| armor_cloth_legs_pants | Cloth Pants | Cloth | Legs | 1/0/0/0/0 | 0 | 30 | Pool_Armor_Common |
| armor_cloth_neck_scarf | Cloth Scarf | Cloth | NeckArmor | 1/0/0/0/0 | 0 | 20 | Pool_Armor_Common |

---

## ARMOR — LEATHER (FULL SET)

| ItemDefId | Name | Material | Slot | Resists (P/F/C/Po/E) | DexPen | Dur | AffixPools |
|---|---|---|---|---|---:|---:|---|
| armor_leather_head_cap | Leather Cap | Leather | Head | 1/0/0/0/0 | 0 | 35 | Pool_Armor_Common |
| armor_leather_torso_tunic | Leather Tunic | Leather | Torso | 3/1/1/1/1 | 0 | 45 | Pool_Armor_Rare |
| armor_leather_arms_sleeves | Leather Sleeves | Leather | Arms | 2/1/1/1/1 | 0 | 40 | Pool_Armor_Common |
| armor_leather_hands_gloves | Leather Gloves | Leather | Hands | 1/0/0/0/0 | 0 | 30 | Pool_Armor_Common |
| armor_leather_legs_leggings | Leather Leggings | Leather | Legs | 2/1/1/1/1 | 0 | 45 | Pool_Armor_Common |
| armor_leather_neck_gorget | Leather Gorget | Leather | NeckArmor | 1/0/0/0/0 | 0 | 35 | Pool_Armor_Common |

---

## ARMOR — METAL (FULL SET)

Metal armor includes Dexterity penalties (PROPOSED).

| ItemDefId | Name | Material | Slot | Resists (P/F/C/Po/E) | DexPen | Dur | AffixPools |
|---|---|---|---|---|---:|---:|---|
| armor_metal_head_helm | Metal Helm | Metal | Head | 2/1/1/1/1 | -1 | 55 | Pool_Armor_Common |
| armor_metal_torso_chestplate | Metal Chestplate | Metal | Torso | 4/2/2/2/2 | -2 | 70 | Pool_Armor_Rare |
| armor_metal_arms_arms | Metal Arms | Metal | Arms | 2/1/1/1/1 | -1 | 60 | Pool_Armor_Common |
| armor_metal_hands_gauntlets | Metal Gauntlets | Metal | Hands | 1/1/1/1/1 | -1 | 55 | Pool_Armor_Common |
| armor_metal_legs_greaves | Metal Greaves | Metal | Legs | 3/1/1/1/1 | -2 | 65 | Pool_Armor_Common |
| armor_metal_neck_gorget | Metal Gorget | Metal | NeckArmor | 1/1/1/1/1 | -1 | 55 | Pool_Armor_Common |

---

## SHIELDS

| ItemDefId | Name | BlockType | Dur | AffixPools |
|---|---|---|---:|---|
| shield_wooden_buckler | Wooden Buckler | Basic | 45 | Pool_Shield_Common |
| shield_metal_heatershield | Heater Shield | Heavy | 65 | Pool_Shield_Rare |

---

## JEWELRY (DURABILITY ENABLED)

Jewelry has no base combat stats; power comes from magical affixes.

| ItemDefId | Name | Slot | Dur | AffixPools |
|---|---|---|---:|---|
| jewel_ring_gold_band | Gold Ring | Ring | 30 | Pool_Jewelry_Common |
| jewel_ring_silver_band | Silver Ring | Ring | 30 | Pool_Jewelry_Common |
| jewel_amulet_silver | Silver Amulet | Amulet | 30 | Pool_Jewelry_Rare |
| jewel_amulet_gold | Gold Amulet | Amulet | 35 | Pool_Jewelry_Rare |
| jewel_earrings_silver | Silver Earrings | Earrings | 25 | Pool_Jewelry_Common |
| jewel_earrings_gold | Gold Earrings | Earrings | 25 | Pool_Jewelry_Rare |

---

## CONSUMABLES

(These are mundane, unless later you add potion/scroll magic systems.)

| ItemDefId | Name | StackMax | Weight | Notes |
|---|---|---:|---:|---|
| consumable_bandage | Bandage | 50 | 0.01 | Used by Healing skill (later) |
| consumable_torch | Torch | 20 | 0.25 | Light source |
| consumable_food_ration | Food Ration | 20 | 0.25 | Simple food item |

---

## REAGENTS (FOR SPELLCASTING)

These correspond to `ReagentId` in your spell schema.

| ItemDefId | Name | StackMax | Weight |
|---|---|---:|---:|
| reagent_pearl | Pearl | 200 | 0.01 |
| reagent_moss | Moss | 200 | 0.01 |
| reagent_garlic | Garlic | 200 | 0.01 |
| reagent_ginseng | Ginseng | 200 | 0.01 |
| reagent_root | Root | 200 | 0.01 |
| reagent_shade | Shade | 200 | 0.01 |
| reagent_ash | Ash | 200 | 0.01 |
| reagent_silk | Silk | 200 | 0.01 |

---

## RESOURCES (CRAFTING)

| ItemDefId | Name | StackMax | Weight | Notes |
|---|---|---:|---:|---|
| resource_iron_ore | Iron Ore | 200 | 0.05 | Smelt into ingots |
| resource_iron_ingot | Iron Ingot | 200 | 0.05 | Blacksmithing base |
| resource_leather | Leather | 200 | 0.02 | Tailoring base |
| resource_cloth | Cloth | 200 | 0.02 | Tailoring base |
| resource_wood_log | Wood Log | 200 | 0.05 | Craft boards/shafts |
| resource_wood_board | Wood Board | 200 | 0.03 | Carpentry base (later) |
| resource_feather | Feather | 500 | 0.001 | Arrow crafting |
| resource_arrow_shaft | Arrow Shaft | 500 | 0.001 | Arrow crafting |

---

## CONTAINERS

| ItemDefId | Name | Capacity | Weight | Notes |
|---|---|---:|---:|---|
| container_backpack | Backpack | 50 slots | 1.0 | Player root inventory |
| container_pouch | Pouch | 10 slots | 0.2 | Small container |
| container_bag | Bag | 20 slots | 0.5 | Medium container |
| container_chest_wood | Wooden Chest | 60 slots | 5.0 | World container |

---

## CONTENT AUTHORING WORKFLOW (RECOMMENDED)

1. Maintain this catalog as the **design source-of-truth**.
2. Create one `ItemDef` asset per entry.
3. Add an editor validator to ensure:
   - Every `ItemDefId` in assets exists in this doc
   - No duplicate IDs
   - No missing required fields

---

## DESIGN LOCK CONFIRMATION

This document is **authoritative** for the item list.

Numeric balance values may remain **Proposed** until Combat baselines are locked.
When you lock numbers, increment version and note the change.

