# ITEM_DEF_SCHEMA.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 1.5  
Last Updated: 2026-02-02  
Engine: Unity 6 (URP)  
Authority: Server-authoritative  
Data: ScriptableObjects-first (`ItemDef`)

---

## PURPOSE

Defines the **authoritative schema** for the `ItemDef` ScriptableObject used in *Ultimate Dungeon*.

This document is the single source of truth for:
- What fields exist on `ItemDef`
- Which fields are required per item family
- Validation rules (what is illegal)

This document does **not**:
- List the base items (owned by `ITEM_CATALOG.md`)
- Define affix IDs / tiers / stacking (owned by `ITEM_AFFIX_CATALOG.md`)
- Define item-system laws (owned by `ITEMS.md`)

---

## SCOPE BOUNDARIES (NO OVERLAP)

Owned elsewhere:
- Item identity + instance rules: `ITEMS.md`
- Base item list + authored base stats: `ITEM_CATALOG.md`
- Affix definitions, tiers, caps, pools: `ITEM_AFFIX_CATALOG.md`
- Spell/ability IDs: `SPELL_ID_CATALOG.md`
- Spell/ability payload + requirements: `SPELL_DEF_SCHEMA.md`
- Status effects: `STATUS_EFFECT_CATALOG.md`
- Combat rules: `COMBAT_CORE.md`

---

## DESIGN LOCKS (MUST ENFORCE)

1. **Stable IDs**
   - `itemDefId` is stable and must be listed in `ITEM_CATALOG.md`.

2. **Server authoritative**
   - ItemDefs are authored data; runtime derives from them; server enforces.

3. **No hidden behavior**
   - If it affects gameplay, it must be represented as data and/or in authoritative rules docs.

4. **Albion-style equipment → hotbar**
   - Equipping an item can grant abilities.
   - The hotbar is a projection of equipped item grants (UI does not “own” abilities).

5. **Ultima-style random properties**
   - Randomness comes from affixes and rolled instance state.
   - **Granted abilities are not affixes.**

6. **Reagents are for potions (Alchemy), not spells**
   - Spellcasting never consumes reagent items.

7. **Jewelry mapping**
   - Rings equip into `Offhand`.
   - Earrings equip into `Neck` (shared with capes/cloaks).

8. **Backpack lock**
   - All equipped backpacks have exactly **48** container slots.
   - Backpacks differ via **carry weight bonus**, not slot count.

---

## CORE ITEMDEF FIELDS (PRESENT ON ALL ITEMS)

### Identity
- `string itemDefId` *(stable; must exist in `ITEM_CATALOG.md`)*
- `string displayName`
- `ItemFamily family`
- `string iconAddress` *(optional; UI)*

### Economy / Inventory
- `float weight`
- `bool isStackable`
- `int stackMax` *(required if isStackable)*

### Durability
- `bool usesDurability`
- `float durabilityMax` *(required if usesDurability)*

### Affix Pools
- `string[] affixPoolRefs` *(names that map to AffixPoolDef assets)*

Rules:
- Mundane items may declare pools so they can become magical through loot/enhance.
- Pools must exist in `ITEM_AFFIX_CATALOG.md`.

---

## EQUIPMENT MODEL (AUTHORITATIVE)

### EquipSlot (10-slot equipment model)
- `Bag`
- `Head`
- `Neck` *(shared: capes/cloaks + earrings/amulets)*
- `Mainhand`
- `Chest`
- `Offhand` *(includes Rings by design)*
- `BeltA` *(generic quick-slot; accepts UtilityItem)*
- `BeltB` *(generic quick-slot; accepts UtilityItem)*
- `Foot`
- `Mount`

### ItemFamily

Design rule (LOCKED):
- For equippable items, `ItemFamily` determines the allowed equip slot(s).

#### Equippable families
- `Bag` → `EquipSlot.Bag`
- `Head` → `EquipSlot.Head`
- `Neck` → `EquipSlot.Neck`
- `Mainhand` → `EquipSlot.Mainhand`
- `Chest` → `EquipSlot.Chest`
- `Offhand` → `EquipSlot.Offhand`
- `Foot` → `EquipSlot.Foot`
- `Mount` → `EquipSlot.Mount`
- `UtilityItem` → `EquipSlot.BeltA` **or** `EquipSlot.BeltB`

#### Non-equippable families
- `Resource`
- `Material`
- `Reagent` *(alchemy only)*
- `Consumable` *(non-equippable consumables)*
- `Container` *(non-equippable containers)*
- `Misc`

---

## EQUIPMENT DATA (AUTHORITATIVE)

Optional block. Present only if the item is equippable.

### EquipmentData
- `bool isEquippable`
- `EquipSlot equipSlot`

Equip rules (LOCKED):
- If `isEquippable == true`, `equipSlot` must be set.
- For slot-unique families (Bag/Head/Neck/Mainhand/Chest/Offhand/Foot/Mount):
  - `equipSlot` must match that slot.
- For `ItemFamily.UtilityItem`:
  - `equipSlot` may be **either** `BeltA` or `BeltB`.
  - The same UtilityItem type may be equipped in both belt slots.

---

## ITEM-GRANTED ABILITIES (AUTHORITATIVE)

### Key principles (LOCKED)
- Granted abilities are part of base item identity.
- Variation comes from affixes + instance rolls.
- The player selects which ability is active per granted slot.
- Selection is stored on the **ItemInstance**, not the ItemDef.

### AbilityGrantSlot
A stable purpose label for a granted ability choice.

Recommended enum values (v1):
- `Primary`
- `Secondary`
- `Utility`

### GrantedAbilities
- `GrantedAbilitySlot[] grantedAbilitySlots` *(required for equippable items; 1..3)*

Each `GrantedAbilitySlot` contains:
- `AbilityGrantSlot slot`
- `SpellId[] allowedSpellIds` *(authoring rule: 1..3 entries)*
- `SpellId defaultSpellId` *(optional; must be in allowedSpellIds if set)*

Runtime interface rules (LOCKED):
- ItemInstance stores `SelectedSpellId` per `AbilityGrantSlot`.
- Selection may be changed only when the actor is **not in combat**.

---

## FAMILY-SPECIFIC DATA BLOCKS

These blocks are **optional** at the schema level, but are **required** for specific families.

### ContainerData (required for `family == Bag` or `family == Container`)
- `int capacitySlots`
- `bool allowNestedContainers` *(default true)*
- `float carryWeightBonusKg` *(optional; 0 allowed)*

Rules (LOCKED):
- If item equips into `EquipSlot.Bag`:
  - `capacitySlots` must be **48**
  - `carryWeightBonusKg` may be > 0

### WeaponData (required for `family == Mainhand`)
- `WeaponHandedness handedness` *(MainHand / TwoHanded)*
- `DamageType damageType` *(Physical/Fire/Cold/Poison/Energy)*
- `int minDamage`
- `int maxDamage`
- `float swingSpeedSeconds`
- `int staminaCostPerSwing`
- `SkillId requiredCombatSkill`
- `float rangeMeters` *(optional)*
- `AmmoType ammoType` *(None/Arrow/Bolt)*

Rules (LOCKED):
- `minDamage <= maxDamage`
- `swingSpeedSeconds > 0`
- `staminaCostPerSwing >= 0`
- If `ammoType != None` then `handedness == TwoHanded`

### ArmorData (required for `family == Head` or `family == Chest` or `family == Foot` or (family == Neck AND item is a cape/cloak))
- `ArmorMaterial material` *(Cloth/Leather/Metal)*
- `ArmorSlot slot` *(Head/Torso/Feet/Back/etc.)*
- `int resistPhysical`
- `int resistFire`
- `int resistCold`
- `int resistPoison`
- `int resistEnergy`
- `int dexPenalty` *(0 allowed)*

### ShieldData (required for `family == Offhand` when the item is a shield)
- `ShieldBlockType blockType`

### JewelryData (required for `family == Offhand` when the item is a ring, OR `family == Neck` when the item is an earring/amulet)
- `JewelrySlot slot` *(Ring/Earring/Amulet)*

Rules (LOCKED):
- Jewelry uses durability (`usesDurability = true`).

### ConsumableData (required for `family == UtilityItem` or `family == Consumable`)
- `ConsumableType type` *(Potion/Food/Bandage/Torch/Scroll/Rune)*
- `bool isUsable`
- `int charges`
- `bool isConsumed`
- `float useTimeSeconds`

Belt rules (LOCKED):
- Items authored as `family == UtilityItem` are equippable consumable-like items.
- They may be equipped into either `BeltA` or `BeltB`.

### MountData (required for `family == Mount`)
- `float moveSpeedMultiplier` *(e.g., 1.10 = +10%)*
- `float staminaDrainPerSecond` *(0 allowed)*

### ReagentData (required for `family == Reagent`)
- `ReagentId reagentId`

Rules (LOCKED):
- Reagents are used by **Alchemy** for potions.
- Spellcasting systems must not depend on reagents.

---

## VALIDATION RULES (LOCKED)

### Global
- `itemDefId` must exist in `ITEM_CATALOG.md`.
- If `isStackable == true` then `stackMax > 1`.
- If `usesDurability == true` then `durabilityMax > 0`.
- All `affixPoolRefs` must exist in `ITEM_AFFIX_CATALOG.md`.

### Equip legality
- If `EquipmentData.isEquippable == true`:
  - If `family == UtilityItem` then `equipSlot` must be `BeltA` or `BeltB`.
  - If `family != UtilityItem` then `equipSlot` must match the family’s slot.

### Granted abilities
- Equippable items must have `grantedAbilitySlots.Length` in range `1..3`.
- Each granted slot must have `allowedSpellIds.Length` in range `1..3`.
- `defaultSpellId`, if set, must be within `allowedSpellIds`.

### Backpack lock
- If `equipSlot == Bag` then `capacitySlots == 48`.

---

## DESIGN LOCK CONFIRMATION

This document is **authoritative**.

Any change must:
- Increment Version
- Update Last Updated
- Call out save-data implications

