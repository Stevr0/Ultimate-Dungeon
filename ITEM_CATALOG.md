# ITEM_CATALOG.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 1.0  
Last Updated: 2026-01-28  
Engine: Unity 6 (URP)  
Authority: Server-authoritative  
Data: ScriptableObjects-first (`ItemDef` + Catalog)

---

## PURPOSE

Defines the **authoritative base item list** for *Ultimate Dungeon*.

This document is the single source of truth for:
- The stable list of **ItemDefIds** (append-only)
- Each item’s **base values** (weapon damage/swing, armor base resists, durability max, weight, stack rules)
- Material/slot base resist profiles (Cloth / Leather / Metal) and how they are authored into items

This document does **not**:
- Define the ItemDef field schema (owned by `ITEM_DEF_SCHEMA.md`)
- Define affix IDs, tiers, stacking, or ranges (owned by `ITEM_AFFIX_CATALOG.md`)
- Define item system laws (owned by `ITEMS.md`)

---

## DESIGN LOCKS (MUST ENFORCE)

1. **Stable IDs (append-only)**
   - Never reorder or rename shipped ItemDefIds.
   - Only append new entries.

2. **Base values live here**
   - Any “base” weapon/armor stats must be authored here (and mirrored in ItemDef assets).

3. **Material resist profiles are locked**
   - Cloth/Leather/Metal baseline intent is defined here.

4. **Jewelry durability is enabled**

5. **Archery/ammo is separate item family**
   - Bows/crossbows + arrows/bolts.

---

## CATALOG VS ASSETS (LOCKED WORKFLOW)

- **This catalog** is the human-readable authoritative list.
- **ItemDef assets** are the runtime data.

**Rule:** Every row in this catalog must have exactly one matching `ItemDef` asset with:
- `itemDefId` equal to this catalog id
- Fields populated according to `ITEM_DEF_SCHEMA.md`

An editor validator must enforce:
- Missing ItemDef assets
- Duplicate ids
- Asset values not matching catalog values

---

## MATERIAL BASE RESIST PROFILES (LOCKED)

These profiles represent the **intended baseline** per material.
Exact per-slot values are authored in the base item entries.

### Cloth (baseline intent)
- Very low Physical
- Low/variable elemental resists
- No DEX penalty

### Leather (baseline intent)
- Moderate Physical
- Moderate elemental resists
- Low/none DEX penalty

### Metal (baseline intent)
- High Physical
- Moderate elemental resists
- Possible DEX penalty

> Final numeric per-piece values are authored below per item.

---

## BASE ITEM LIST (AUTHORITATIVE)

> **IMPORTANT:** Append-only. Do not reorder.
>
> Columns here map directly to fields in `ITEM_DEF_SCHEMA.md`.

### Column Key
- **Family**: Weapon / Armor / Shield / Jewelry / Consumable / Reagent / Resource / Container
- **DurMax**: durabilityMax (if usesDurability)
- **Stack**: stackMax (if isStackable)

---

## WEAPONS — MELEE (AUTHORITATIVE)

| ItemDefId | Name | Hand | Skill | Dmg (Min–Max) | Swing (s) | Stam | Type | Range | DurMax | Weight |
|---|---|---|---|---:|---:|---:|---|---:|---:|---:|
| weapon_sword_short | Short Sword | MainHand | Swords | 4–8 | 2.25 | 4 | Physical | 2.0 | 60 | 5.0 |
| weapon_sword_long | Long Sword | MainHand | Swords | 6–12 | 2.75 | 5 | Physical | 2.0 | 70 | 6.0 |
| weapon_mace_club | Club | MainHand | Macing | 4–9 | 2.50 | 4 | Physical | 2.0 | 65 | 6.0 |
| weapon_mace_warhammer | War Hammer | TwoHanded | Macing | 10–18 | 3.25 | 7 | Physical | 2.0 | 80 | 10.0 |
| weapon_fence_spear | Spear | TwoHanded | Fencing | 8–14 | 3.00 | 6 | Physical | 2.0 | 75 | 8.0 |
| weapon_fence_dagger | Dagger | MainHand | Fencing | 3–7 | 2.00 | 3 | Physical | 2.0 | 55 | 2.0 |

> Notes:
> - Range defaults to Combat Core melee range if omitted; included here for clarity.
> - Exact balance is tunable; the schema and ownership are locked.

---

## WEAPONS — RANGED (AUTHORITATIVE)

| ItemDefId | Name | Hand | Skill | Dmg (Min–Max) | Swing (s) | Stam | Type | Ammo | Range | DurMax | Weight |
|---|---|---|---|---:|---:|---:|---|---|---:|---:|---:|
| weapon_archery_bow | Bow | TwoHanded | Archery | 6–12 | 2.75 | 5 | Physical | Arrow | 12.0 | 65 | 6.0 |
| weapon_archery_crossbow | Crossbow | TwoHanded | Archery | 8–14 | 3.25 | 6 | Physical | Bolt | 12.0 | 70 | 7.0 |

---

## AMMUNITION (AUTHORITATIVE)

| ItemDefId | Name | Family | Stack | Weight |
|---|---|---|---:|---:|
| ammo_arrow | Arrow | Resource | 100 | 0.02 |
| ammo_bolt | Bolt | Resource | 100 | 0.02 |

> Ammo is consumed by Combat Core: 1 per attack attempt.

---

## ARMOR — CLOTH (AUTHORITATIVE)

| ItemDefId | Name | Material | Slot | P | F | C | Po | E | DexPen | DurMax | Weight |
|---|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| armor_cloth_cap | Cloth Cap | Cloth | Head | 1 | 1 | 1 | 0 | 1 | 0 | 35 | 1.0 |
| armor_cloth_tunic | Cloth Tunic | Cloth | Torso | 2 | 1 | 1 | 1 | 1 | 0 | 45 | 2.0 |
| armor_cloth_leggings | Cloth Leggings | Cloth | Legs | 2 | 1 | 1 | 1 | 1 | 0 | 40 | 2.0 |

---

## ARMOR — LEATHER (AUTHORITATIVE)

| ItemDefId | Name | Material | Slot | P | F | C | Po | E | DexPen | DurMax | Weight |
|---|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| armor_leather_cap | Leather Cap | Leather | Head | 2 | 2 | 1 | 1 | 1 | 0 | 45 | 1.5 |
| armor_leather_tunic | Leather Tunic | Leather | Torso | 3 | 2 | 2 | 1 | 1 | 0 | 55 | 3.0 |
| armor_leather_gloves | Leather Gloves | Leather | Hands | 2 | 1 | 1 | 1 | 1 | 0 | 40 | 1.0 |

---

## ARMOR — METAL (AUTHORITATIVE)

| ItemDefId | Name | Material | Slot | P | F | C | Po | E | DexPen | DurMax | Weight |
|---|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| armor_metal_helm | Iron Helm | Metal | Head | 4 | 2 | 2 | 1 | 1 | -1 | 60 | 4.0 |
| armor_metal_chest | Iron Chest | Metal | Torso | 6 | 3 | 2 | 2 | 2 | -2 | 75 | 8.0 |
| armor_metal_leggings | Iron Leggings | Metal | Legs | 5 | 2 | 2 | 2 | 2 | -2 | 70 | 7.0 |

---

## SHIELDS (AUTHORITATIVE)

| ItemDefId | Name | BlockType | DurMax | Weight |
|---|---|---|---:|---:|
| shield_buckler | Buckler | Basic | 60 | 4.0 |
| shield_kite | Kite Shield | Heavy | 80 | 7.0 |

---

## JEWELRY (AUTHORITATIVE)

| ItemDefId | Name | Slot | DurMax | Weight |
|---|---|---|---:|---:|
| jewel_amulet_plain | Plain Amulet | Amulet | 40 | 0.5 |
| jewel_ring_plain | Plain Ring | Ring | 35 | 0.2 |
| jewel_earrings_plain | Plain Earrings | Earrings | 35 | 0.2 |

> Jewelry has no base resists; durability is enabled.

---

## CONSUMABLES (AUTHORITATIVE)

| ItemDefId | Name | Type | Stack | UseTime (s) | Weight |
|---|---|---|---:|---:|---:|
| consumable_bandage | Bandage | Bandage | 50 | 0.0 | 0.05 |

---

## REAGENTS (AUTHORITATIVE)

| ItemDefId | Name | Stack | Weight |
|---|---|---:|---:|
| reagent_black_pearl | Black Pearl | 100 | 0.02 |
| reagent_blood_moss | Blood Moss | 100 | 0.02 |
| reagent_garlic | Garlic | 100 | 0.02 |
| reagent_ginseng | Ginseng | 100 | 0.02 |
| reagent_mandrake_root | Mandrake Root | 100 | 0.02 |
| reagent_nightshade | Nightshade | 100 | 0.02 |
| reagent_spiders_silk | Spider's Silk | 100 | 0.02 |
| reagent_sulfurous_ash | Sulfurous Ash | 100 | 0.02 |

---

## RESOURCES (AUTHORITATIVE)

| ItemDefId | Name | Stack | Weight |
|---|---|---:|---:|
| resource_ore_iron | Iron Ore | 50 | 0.5 |
| resource_ingot_iron | Iron Ingot | 50 | 0.3 |
| resource_leather | Leather | 50 | 0.2 |
| resource_cloth | Cloth | 50 | 0.2 |

---

## CONTAINERS (AUTHORITATIVE)

| ItemDefId | Name | Slots | AllowNested | DurMax | Weight |
|---|---|---:|---|---:|---:|
| container_backpack | Backpack | 30 | true | 50 | 2.0 |
| container_pouch | Pouch | 10 | true | 35 | 1.0 |

---

## VALIDATION CHECKLIST (LOCKED)

An editor validator must ensure:
- Every `ItemDefId` here has a matching `ItemDef` asset
- Every `ItemDef` asset uses this catalog’s base values
- No duplicates
- Append-only changes for shipped content

---

## OPEN QUESTIONS (PROPOSED — NOT LOCKED)

- Full expansion of the base catalog (more weapons/armor variants)
- Balance pass across damage/swing/durability/weights

---

## DESIGN LOCK CONFIRMATION

This document is **authoritative**.

Any change must:
- Increment Version
- Update Last Updated
- Call out save-data implications (new ids affect serialization)

