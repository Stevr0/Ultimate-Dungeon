# ITEM\_DEF\_SCHEMA.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 1.2\
Last Updated: 2026-02-02\
Engine: Unity 6 (URP)\
Authority: Server-authoritative\
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
- Spell definitions and targeting rules: `SPELL_DEF_SCHEMA.md` + `MAGIC_AND_SPELLS.md`

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
  - `mount_<name>`

---

## CORE ITEMDEF FIELDS (AUTHORITATIVE)

These fields exist on every `ItemDef`.

### 1) Identity

- `string itemDefId` *(stable; must match ********************************************`ITEM_CATALOG.md`********************************************)*
- `string displayName`
- `ItemFamily family` *(see enum below)*
- `string iconAddress` *(optional; UI)*

### 2) Economy / Inventory

- `float weight`
- `bool isStackable`
- `int stackMax` *(required if isStackable)*

### 3) Durability

- `bool usesDurability`
- `float durabilityMax` *(required if usesDurability)*

> Jewelry durability is enabled by design lock (enforced by catalog + assets).

### 4) Affix Pools

- `string[] affixPoolRefs` *(names that map to ********************************************`AffixPoolDef`******************************************** assets)*

Rules:

- Mundane items may still declare pools (so they can become magical via loot/enhance).
- Allowed pools are validated against `ITEM_AFFIX_CATALOG.md`.

---

## NEW: EQUIPMENT SLOT + ITEM-GRANTED ABILITIES (AUTHORITATIVE)

This section enables an **Albion-style equipment → hotbar** model while preserving Ultima-style random properties.

### Key Principles (LOCKED)

1. **Equipment slot is explicit**

   - If an item can be equipped into one of the 10 equipment slots, it declares `EquipmentData.equipSlot`.

2. **Item-granted abilities are NOT affixes**

   - Affixes remain the random-property system.
   - Abilities are part of the base item identity and are selectable per item instance.

3. **Selection is instance state (not ItemDef)**

   - `ItemDef` declares what is possible.
   - `ItemInstance` stores what the player chose (one active choice per granted ability slot).

4. **Ability IDs reuse ****************************************************************************************`SpellId`**************************************************************************************** (v1)**

   - This keeps targeting + requirements + cooldowns unified and data-driven.
   - Non-magical “moves” (e.g., sprint, dash, taunt) are implemented as `SpellDef` entries that primarily apply statuses and/or utility payloads.

> NOTE: This schema only defines fields. Hotbar binding rules live in a rules doc (future), and UI remains a projection of equipped grants.

---

## ENUMS (AUTHORITATIVE)

### ItemFamily

ItemFamily is the **primary categorization** for items.

Design rule (LOCKED):
- For **equippable items**, `ItemFamily` **must match the target `EquipSlot`** (one-to-one).
- For **non-equippable items**, `ItemFamily` uses the Non-Equip families.

#### Equippable Families (must match EquipSlot)
- `Bag`
- `Head`
- `Neck`
- `Mainhand`
- `Chest`
- `Offhand`
- `Potion`
- `Foot`
- `Food`
- `Mount`

#### Non-Equip Families
- `Resource`
- `Material`
- `Reagent` *(alchemy only)*
- `Consumable` *(non-equippable consumables, e.g. bandages, torches if not slotted)*
- `Container` *(non-equippable containers, world containers, etc.)*
- `Misc`

> NOTE: This replaces the older Weapon/Armor/Shield/Jewelry split. Any weapon/armor specifics are now expressed via optional data blocks and tags (see below).

### EquipSlot (10-slot equipment model)

- `Bag`
- `Head`
- `Neck` *(shared slot: capes/cloaks + earrings and other neck accessories)*
- `Mainhand`
- `Chest`
- `Offhand` *(includes Rings by design)*
- `Potion`
- `Foot`
- `Food`
- `Mount`

### AbilityGrantSlot

A stable *purpose* label for what the granted ability is “about”.

Recommended enum values (v1):

- `Primary`
- `Secondary`
- `Utility`

> **Rule:** A single item may grant 1–3 slots, each of which allows selecting exactly 1 active ability.

---

## EQUIPMENT DATA (AUTHORITATIVE)

Optional block. Present only if the item is equippable into the 10-slot model.

### EquipmentData

- `bool isEquippable` *(required; if true, EquipmentData must be present)*
- `EquipSlot equipSlot`

### GrantedAbilities

- `GrantedAbilitySlot[] grantedAbilitySlots`

Each `GrantedAbilitySlot` contains:

- `AbilityGrantSlot slot` *(Primary/Secondary/Utility)*
- `SpellId[] allowedSpellIds` *(authoring rule: 1..3 entries)*
- `SpellId defaultSpellId` *(optional; must exist in allowedSpellIds if set)*

Authoring Rules (LOCKED):

- `allowedSpellIds` is an explicit curated list (no implicit “all spells by circle”).
- A granted slot must always have at least 1 allowed spell.
- Items may share allowed spells (e.g., swords and axes both allow `Weaken`) while still having unique options.

Runtime Rules (LOCKED INTERFACE):

- ItemInstance stores `SelectedSpellId per GrantedAbilitySlot`.
- Selection may be changed only when the actor is **not in combat** (combat state is owned by combat/scene rules).

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

---

## ARMOR DATA (AUTHORITATIVE)

Required when `family == Armor`.

### ArmorData

- `ArmorMaterial material` *(Cloth/Leather/Metal)*
- `ArmorSlot slot` *(see enum below)*
- `int resistPhysical`
- `int resistFire`
- `int resistCold`
- `int resistPoison`
- `int resistEnergy`
- `int dexPenalty` *(may be 0; metal may be negative)*

### ArmorSlot (expanded)

Existing (v1.1):

- `Head`
- `Chest`
- `Foot`
- `Neck`

#### Resist authoring rule (LOCKED)

Material baseline profiles + slot scalars are owned by `ITEM_CATALOG.md`.
This schema simply defines the fields that store the authored results.

---

## SHIELD DATA (AUTHORITATIVE)

Required when `family == Shield`.

### ShieldData

- `ShieldBlockType blockType` *(Basic/Heavy/etc.)*

> If shields later contribute base resists, they must do so through explicit resist fields (either ShieldData or shared resist fields). No hidden behavior.

---

## JEWELRY DATA (AUTHORITATIVE)

Required when `family == Jewelry`.

### JewelryData
- `JewelrySlot slot` *(Ring / Earring / Amulet)*

Equip mapping rules (LOCKED):
- **Rings equip into `EquipSlot.Offhand`.**
- **Earrings equip into `EquipSlot.Neck`** *(shared with capes/cloaks).*
- Amulets (if used) also equip into `EquipSlot.Neck`.

Rules:
- Jewelry has durability enabled (via `usesDurability=true`).
- Jewelry has no base resists/stats by default; power comes from affixes and/or granted abilities.

## CONSUMABLE DATA (AUTHORITATIVE)

Required when `family == Consumable`.

### ConsumableData

- `ConsumableType type` *(Bandage/Food/Torch/Potion/etc.)*
- `bool isUsable`
- `float useTimeSeconds` *(optional; v1 may be instant for some types)*

> Potion/food can participate in equipment slots by declaring `EquipmentData.equipSlot` as `Potion` or `Food`.

---

## REAGENT DATA (AUTHORITATIVE)

Required when `family == Reagent`.

### ReagentData
- `ReagentId reagentId`

Rules (LOCKED):
- **Reagents are for making potions (Alchemy), not spells.**
- `ReagentId` must exist in the **potion/alchemy reagent catalog** (to be introduced as an authoritative ID catalog).
- Spellcasting systems must not depend on `ReagentData`.

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

> Bags used as equipment (EquipSlot.Bag) are simply Containers with EquipmentData.

---

## MOUNT DATA (AUTHORITATIVE)

Required when `family == Mount`.

### MountData

- `float moveSpeedMultiplier` *(e.g., 1.10 for +10% move speed)*
- `float staminaDrainPerSecond` *(optional; 0 allowed)*

> Mount abilities (e.g., “Charge”, “Dismount Kick”) are granted through `EquipmentData.grantedAbilitySlots` like any other item.

---

## VALIDATION RULES (LOCKED)

### Global

- `itemDefId` must exist in `ITEM_CATALOG.md`.
- If `isStackable == true` then `stackMax > 1`.
- If `usesDurability == true` then `durabilityMax > 0`.
- All `affixPoolRefs` must exist as pool names in `ITEM_AFFIX_CATALOG.md`.

### EquipmentData

- If `EquipmentData.isEquippable == true`:
  - `EquipmentData.equipSlot` must be set.
  - `grantedAbilitySlots.Length` must be in range `1..3`.
  - Each `GrantedAbilitySlot.allowedSpellIds.Length` must be in range `1..3`.
  - `defaultSpellId`, if present, must be contained in `allowedSpellIds`.

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

### Mount

- `moveSpeedMultiplier > 0`
- `staminaDrainPerSecond >= 0`

---

## NEXT STEPS

1. Update `ITEM_CATALOG.md` to add entries for the 10-slot equipment model:
   - Bags (Container items with `EquipSlot.Bag`)
   - Helmets (Armor items with `EquipSlot.Head`)
   - **Capes/Cloaks** (Armor-like accessories equipped in `EquipSlot.Neck`)
   - Boots (Armor items with `EquipSlot.Foot`)
   - Rings (Jewelry items with `EquipSlot.Offhand`)
   - Earrings/Amulets (Jewelry items with `EquipSlot.Neck`)
   - Mainhand weapons (Weapon items with `EquipSlot.Mainhand`)
   - Offhand shields (Shield items with `EquipSlot.Offhand`)
   - Potions (Consumable items with `EquipSlot.Potion`)
   - Food (Consumable items with `EquipSlot.Food`)
   - Mount items (ItemFamily.Mount with `EquipSlot.Mount`)

2. Introduce an authoritative **Potion/Alchemy reagent catalog** (ID list) and ensure `ReagentId` references it.

3. Update/introduce an authoritative Equipment/Hotbar rules doc to define:
   - Which `EquipSlot` maps to which hotbar index
   - When ability selection may change (combat gate)
   - How multiple granted slots per item map into hotbar (Primary/Secondary/Utility)

4. Add an Editor validator for:
   - Missing/unknown IDs
   - Invalid family field sets
   - Invalid spell references (must exist in `SpellId` enum)

---

## DESIGN LOCK CONFIRMATION

This document is **authoritative**.

Any change must:

- Increment Version
- Update Last Updated
- Call out save-data and tooling implications

