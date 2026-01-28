# ITEM_DEF_SCHEMA.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 1.1  
Last Updated: 2026-01-28  
Engine: Unity 6 (URP)  
Authority: Server-authoritative  
Data: ScriptableObjects-first (`ItemDef`)

---

## PURPOSE

Defines the **authoritative schema** for the `ItemDef` ScriptableObject in *Ultimate Dungeon*.

This document is the single source of truth for:
- What fields exist on `ItemDef`
- Which fields are required per item family
- Validation rules (what is illegal)

This document does **not**:
- List the base items (owned by `ITEM_CATALOG.md`)
- Define affix IDs / tiers / stacking (owned by `ITEM_AFFIX_CATALOG.md`)
- Define item system laws (owned by `ITEMS.md`)

---

## SCOPE BOUNDARIES (NO OVERLAP)

### Owned elsewhere
- Item identity model (ItemDef vs ItemInstance): `ITEMS.md`
- Base item list + base stats: `ITEM_CATALOG.md`
- Affix pools and affix definitions: `ITEM_AFFIX_CATALOG.md`
- Combat execution rules: `COMBAT_CORE.md`

---

## DESIGN LOCKS (MUST ENFORCE)

1. **Stable IDs**
   - `ItemDefId` is stable and matches an entry in `ITEM_CATALOG.md`.

2. **Server authoritative**
   - `ItemDef` is authored content; runtime logic reads it; server enforces.

3. **No hidden fields**
   - If a field affects gameplay, it must be represented here.

4. **No duplication**
   - Do not define item lists or affix catalogs in this schema.

---

## ITEMDEF ID RULES (LOCKED)

- `ItemDefId` is a stable string.
- All shipped IDs are append-only in `ITEM_CATALOG.md`.
- `ItemDefId` naming convention (recommended):
  - `weapon_<family>_<name>`
  - `armor_<material>_<slot>_<name>`
  - `shield_<name>`
  - `jewel_<type>_<name>`
  - `consumable_<name>`
  - `reagent_<name>`
  - `resource_<name>`
  - `container_<name>`

---

## CORE ITEMDEF FIELDS (AUTHORITATIVE)

These fields exist on every `ItemDef`.

### Identity
- `string itemDefId` *(stable; must match `ITEM_CATALOG.md`)*
- `string displayName`
- `ItemFamily family` *(Weapon/Armor/Shield/Jewelry/Consumable/Reagent/Resource/Container)*
- `string iconAddress` *(optional; UI)*

### Economy / Inventory
- `float weight`
- `bool isStackable`
- `int stackMax` *(required if isStackable)*

### Durability
- `bool usesDurability`
- `float durabilityMax` *(required if usesDurability)*

> Jewelry durability is enabled by design lock (enforced by catalog + assets).

### Affix Pools
- `string[] affixPoolRefs` *(names that map to `AffixPoolDef` assets)*

Rules:
- Mundane items may still declare pools (so they can become magical via loot/enhance).
- Allowed pools are validated against `ITEM_AFFIX_CATALOG.md`.

---

## WEAPON DATA (AUTHORITATIVE)

Required when `family == Weapon`.

### WeaponData
- `WeaponHandedness handedness` *(MainHand / TwoHanded)*
- `DamageType damageType` *(Physical/Fire/Cold/Poison/Energy; v1 weapons are mostly Physical)*
- `int minDamage`
- `int maxDamage`
- `float swingSpeedSeconds`
- `int staminaCostPerSwing`
- `SkillId requiredCombatSkill`
- `float rangeMeters` *(optional; if not set, combat defaults are used)*

### Ranged-only fields
- `AmmoType ammoType` *(None/Arrow/Bolt)*

#### Schema rule (LOCKED)
- If `ammoType != None`, weapon is treated as Ranged for Combat and must be TwoHanded.

#### SkillId rule (LOCKED)
- `requiredCombatSkill` must be a `SkillId` from `SKILL_ID_CATALOG.md`.
- This schema must not contain any duplicate SkillId list.

---

## ARMOR DATA (AUTHORITATIVE)

Required when `family == Armor`.

### ArmorData
- `ArmorMaterial material` *(Cloth/Leather/Metal)*
- `ArmorSlot slot` *(Head/Torso/Arms/Hands/Legs/NeckArmor)*
- `int resistPhysical`
- `int resistFire`
- `int resistCold`
- `int resistPoison`
- `int resistEnergy`
- `int dexPenalty` *(may be 0; metal may be negative)*

#### Resist authoring rule (LOCKED)
Material baseline profiles + slot scalars are owned by `ITEM_CATALOG.md`.
This schema simply defines the fields that store the authored results.

---

## SHIELD DATA (AUTHORITATIVE)

Required when `family == Shield`.

### ShieldData
- `ShieldBlockType blockType` *(Basic/Heavy/etc.)*
- `int blockChanceBonus` *(optional; v1 may be 0 if block is skill-only)*

> If shields later contribute base resists, they must do so through explicit resist fields (either ShieldData or shared resist fields). No hidden behavior.

---

## JEWELRY DATA (AUTHORITATIVE)

Required when `family == Jewelry`.

### JewelryData
- `JewelrySlot slot` *(Amulet/Ring/Earrings)*

Rules:
- Jewelry has durability enabled (via `usesDurability=true`).
- Jewelry has no base resists/stats by default; power comes from affixes.

---

## CONSUMABLE DATA (AUTHORITATIVE)

Required when `family == Consumable`.

### ConsumableData
- `ConsumableType type` *(Bandage/Food/Torch/etc.)*
- `bool isUsable`
- `float useTimeSeconds` *(optional; v1 may be instant for some types)*

> Spell scrolls/potions can be added later as separate families or subtypes; do not overload this schema with magic rules.

---

## REAGENT DATA (AUTHORITATIVE)

Required when `family == Reagent`.

### ReagentData
- `ReagentId reagentId` *(must match Magic schema; no duplicates here)*

---

## RESOURCE DATA (AUTHORITATIVE)

Required when `family == Resource`.

### ResourceData
- `ResourceType type` *(Ore/Ingot/Leather/Cloth/Wood/etc.)*

---

## CONTAINER DATA (AUTHORITATIVE)

Required when `family == Container`.

### ContainerData
- `int capacitySlots`
- `bool allowNestedContainers` *(default true)*

---

## VALIDATION RULES (LOCKED)

### Global
- `itemDefId` must exist in `ITEM_CATALOG.md`.
- If `isStackable == true` then `stackMax > 1`.
- If `usesDurability == true` then `durabilityMax > 0`.
- All `affixPoolRefs` must exist as pool names in `ITEM_AFFIX_CATALOG.md`.

### Weapons
- `minDamage <= maxDamage`
- `swingSpeedSeconds > 0`
- `staminaCostPerSwing >= 0`
- If `ammoType != None`:
  - `handedness == TwoHanded`

### Armor
- `material` and `slot` must be valid enums.
- Resist fields must be >= 0.

### Jewelry
- `usesDurability == true` (enforced by content validation)

---

## NEXT STEPS

1. Update `ITEM_CATALOG.md` to ensure every entry has a matching `ItemDef` asset shaped by this schema.
2. Ensure `requiredCombatSkill` across all weapon defs uses SkillIds from `SKILL_ID_CATALOG.md`.
3. Add an Editor validator:
   - Missing/unknown IDs
   - Invalid family field sets
   - Missing required blocks (WeaponData/ArmorData/etc.)

---

## DESIGN LOCK CONFIRMATION

This document is **authoritative**.

Any change must:
- Increment Version
- Update Last Updated
- Call out save-data and tooling implications
