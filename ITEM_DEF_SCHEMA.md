# ITEM_DEF_SCHEMA.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 1.0  
Last Updated: 2026-01-28  
Engine: Unity 6 (URP)  
Networking: Netcode for GameObjects (NGO)  
Authority: Server-authoritative  
Assets: ScriptableObjects-first

---

## PURPOSE

Defines the **authoritative data contract** for `ItemDef` ScriptableObjects in *Ultimate Dungeon*.

`ItemDef` is the immutable, authored definition of an item type.
All runtime state (durability, stack count, rolled affixes, insurance, etc.) lives on **ItemInstance**.

This schema must align with:
- `ITEMS.md` (system laws)
- `ITEM_AFFIX_CATALOG.md` (affix IDs + rules)
- `ITEM_CATALOG.md` (the authoritative list of items and their base values)

If a field or behavior is not defined here, **it does not exist**.

---

## DESIGN LOCKS (ABSOLUTE)

1. **ItemDef is immutable at runtime**
   - No durability, stacks, affixes, or insurance stored here.

2. **ItemInstance stores mutable state**
   - `ItemInstance` references `ItemDefId`.

3. **Server authoritative**
   - Server validates all equip/use/enhance actions.

4. **Data-driven**
   - Combat and crafting interpret authored data.
   - No per-item hardcoded logic.

5. **Stable IDs**
   - `ItemDefId` is stable and must match the ID used in `ITEM_CATALOG.md`.

---

## PRIMARY ASSET

### Asset Type

- `ItemDef : ScriptableObject`

### Naming Convention

- File name: `ItemDef_<Category>_<Name>`
  - Example: `ItemDef_Weapon_Katana`
- Internal ID: `itemDefId = "weapon_sword_katana"` (from `ITEM_CATALOG.md`)

---

## ENUMS (AUTHORITATIVE SET)

### ItemCategory

- Weapon
- Armor
- Jewelry
- Shield
- Consumable
- Resource
- Container
- Tool
- Quest

### ItemPowerState

- Mundane
- Magical

> `DefaultItemPowerState` should be **Mundane** for almost all base items.
> “Always magical artifacts” are an optional future expansion.

### DamageType

- Physical
- Fire
- Cold
- Poison
- Energy

### Handedness

- MainHand
- OffHand
- TwoHanded

### ArmorMaterial

- Cloth
- Leather
- Metal

### EquipmentSlot (LOCKED)

- MainHand
- OffHand
- TwoHanded
- Head
- Torso
- Arms
- Hands
- Legs
- NeckArmor
- Amulet
- Ring1
- Ring2
- Earrings

> Implementation should use a **bitmask** (e.g., `EquipmentSlotMask`) so a single ItemDef can declare which slot(s) it occupies.

### AmmoType

- None
- Arrow
- Bolt

---

## REQUIRED FIELDS (AUTHORITATIVE)

These fields exist on **every** ItemDef.

### 1) Identity

- `string itemDefId` *(stable; matches ITEM_CATALOG.md)*
- `string displayName`
- `ItemCategory category`
- `string shortDescription` *(tooltip)*
- `Sprite icon` *(UI)*

### 2) Core Properties

- `float baseWeight`
- `bool isStackable`
- `int maxStackSize` *(required if stackable; else 1)*

### 3) Durability

- `int maxDurability`

**Rule (LOCKED):**
- All equippable items (Weapon/Armor/Jewelry/Shield) must have durability > 0.
- Containers may have durability (optional).
- Resources/consumables may omit durability (set to 0).

### 4) Item Power Default

- `ItemPowerState defaultItemPowerState`

**Rule (LOCKED):**
- Default for base gear is `Mundane`.

### 5) Affix Authoring Hooks

- `string[] allowedAffixPoolIds`

Notes:
- These IDs are references to `AffixPoolDef` assets (created later).
- Mundane items can still specify pools; they only matter when the item becomes Magical.

---

## CATEGORY-SPECIFIC DATA BLOCKS

`ItemDef` contains optional blocks. Only the relevant block should be enabled.

**Rule (LOCKED):**
> If the category does not match the block, the block must be null/disabled and ignored.

---

## WEAPON BLOCK (WeaponData)

### Fields

- `Handedness handedness`
- `DamageType damageType`
- `int minDamage`
- `int maxDamage`
- `float swingSpeedSeconds`
- `int staminaCostPerSwing`
- `SkillId requiredCombatSkill` *(e.g., Swords, Macing, Fencing, Archery)*

### Ranged Weapon Fields

- `AmmoType ammoType` *(Arrow/Bolt/None)*
- `int ammoConsumedPerAttack` *(default 1 for bows/crossbows)*

### Validation Rules (LOCKED)

- `minDamage >= 0`
- `maxDamage >= minDamage`
- `swingSpeedSeconds > 0`
- If `ammoType != None` then:
  - `requiredCombatSkill == Archery`
  - `handedness == TwoHanded`

---

## ARMOR BLOCK (ArmorData)

### Fields

- `ArmorMaterial material`
- `EquipmentSlotMask armorSlot` *(Head/Torso/Arms/Hands/Legs/NeckArmor)*

- Base resist values (authorable per piece; catalog currently derives them from locked profiles):
  - `int resistPhysical`
  - `int resistFire`
  - `int resistCold`
  - `int resistPoison`
  - `int resistEnergy`

- `int dexterityPenalty` *(optional; may be negative for metal)*

### Validation Rules (LOCKED)

- Armor must occupy exactly one armor slot.
- Resist values must be >= 0.

---

## SHIELD BLOCK (ShieldData)

### Fields

- `EquipmentSlotMask slot` *(must include OffHand)*
- `string blockType` *(design tag: Basic/Heavy/etc.)*

> Shields are equipment with durability and can have affix pools.
> The actual parry/block math is handled in Combat.

### Validation Rules (LOCKED)

- Must be equippable in OffHand.
- Must not be TwoHanded.

---

## JEWELRY BLOCK (JewelryData)

### Fields

- `EquipmentSlotMask slot` *(Amulet/Ring/Earrings)*

### Validation Rules (LOCKED)

- Jewelry must occupy exactly one jewelry slot type.
- Jewelry has durability (per locked decision).

---

## CONSUMABLE BLOCK (ConsumableData)

Consumables are used/consumed and may apply effects via other systems.

### Fields (foundation)

- `bool consumedOnUse` *(default true)*
- `int usesPerStack` *(optional; default 1)*

> More complex consumable effects should be handled by a separate data system (e.g., Status effects, spell scroll learning, potion system).

---

## RESOURCE BLOCK (ResourceData)

### Fields

- `string resourceTag` *(e.g., IronOre, IronIngot, Leather, Cloth, Feather, ArrowShaft)*

Resources are stackable, weight-bearing items.

---

## REAGENT BLOCK (ReagentData)

Reagents are item defs that correspond to `ReagentId` in magic.

### Fields

- `ReagentId reagentId`

**Rule (LOCKED):**
- Reagents are stackable.

---

## AMMUNITION BLOCK (AmmoData)

Ammo is stackable and consumed by archery weapons.

### Fields

- `AmmoType ammoType` *(Arrow/Bolt)*
- `int damageBonus` *(optional; default 0)*

**Rule (LOCKED):**
- Ammo is stackable.

---

## CONTAINER BLOCK (ContainerData)

Containers form the inventory graph.

### Fields (foundation)

- `int capacitySlots`
- `float maxWeight` *(optional; 0 = unlimited)*

> Slot-based capacity is used for early implementation.
> Later you can add grid sizes or volume rules if desired.

---

## TOOL BLOCK (ToolData)

Tools are used for crafting and may have durability.

### Fields (foundation)

- `string toolTag` *(e.g., SmithHammer, SewingKit, TinkerTools)*
- `int maxUses` *(optional; alternative to durability)*

---

## EQUIPMENT SLOT MASK (AUTHORITATIVE)

Implementation should use an `int` bitmask.

**Rule (LOCKED):**
- Weapons:
  - MainHand OR TwoHanded
- Shields:
  - OffHand
- Armor:
  - Exactly one armor slot
- Jewelry:
  - Exactly one jewelry slot type

---

## VALIDATION (EDITOR + RUNTIME)

### Editor Validation (REQUIRED)

Create an editor-time validator that checks:

- `itemDefId` is non-empty and unique
- `itemDefId` exists in `ITEM_CATALOG.md` (optional validator stage)
- Stackability rules:
  - if `isStackable` then `maxStackSize > 1`
  - if not stackable then `maxStackSize == 1`
- Category ↔ block consistency:
  - Weapon items must have WeaponData enabled; others disabled
  - Armor items must have ArmorData enabled; others disabled
  - etc.
- Equipment durability rules:
  - Equippable categories must have `maxDurability > 0`

### Runtime Validation (SERVER)

On equip/use requests, server validates:
- ItemInstance references a valid ItemDef
- Slot compatibility and conflicts
- Ownership and container legality

---

## SAVE / NETWORK NOTES

- `ItemDef` is never networked directly.
- Network/Save transmits:
  - `ItemInstanceId`
  - `ItemDefId`
  - runtime state (durability, stack count, affixes, insurance)

Clients resolve `ItemDefId → ItemDef` through a registry.

---

## NEXT IMPLEMENTATION ARTIFACTS

1. `ItemDef.cs` (ScriptableObject class)
2. `ItemDefRegistry` (ItemDefId → ItemDef)
3. `ItemInstance` runtime model
4. Editor validator / audit tool

---

## DESIGN LOCK CONFIRMATION

This schema is **authoritative**.

Changes must:
- Increment Version
- Update Last Updated
- Call out save-data and content implications

