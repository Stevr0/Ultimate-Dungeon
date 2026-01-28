# ITEMS.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 1.1  
Last Updated: 2026-01-28  
Engine: Unity 6 (URP)  
Networking: Netcode for GameObjects (NGO)  
Authority: Server-authoritative  
Data Model: ScriptableObjects + runtime instances

---

## PURPOSE

This document defines the **authoritative Item system** for *Ultimate Dungeon*.

Items are a **core pillar** of the game:
- They define **combat power**
- They define **risk and loss**
- They define the **economy and progression pressure**

**Combat MUST NOT be implemented until this document is locked.**

If an item rule is not defined here, **it does not exist**.

---

## DESIGN LOCKS (ABSOLUTE)

1. **Items drive power** — not character levels
2. **Server authoritative** — clients never modify item state
3. **Item instances are unique** — no shared mutable state
4. **Modifiers, not logic** — items modify stats; systems read stats
5. **Loss matters** — durability, death, and insurance are enforced
6. **Data-driven** — all item behavior is defined in data, not hardcoded
7. **Mundane vs Magical is explicit**
   - **Mundane** items have **no modifiers** (no base modifiers, no affixes)
   - **Magical** items have **modifiers applied** (via affixes and/or authored magical bases)
8. **Enhancement creates magical items**
   - A mundane item may be enhanced at a crafting station into a magical item
   - Enhancement may fail and can destroy the item (breakage chance)

---

## ITEM MODEL OVERVIEW

### Mundane vs Magical Items (LOCKED MODEL)

All items in *Ultimate Dungeon* exist in **one of two states**:

- **Mundane** — baseline items with no magical modifiers
- **Magical** — items enhanced with magical properties

This distinction is **state-based**, not category-based.

**Rule (LOCKED):**
> Mundane and Magical items share the same `ItemDef`.  
> The difference exists only at the **ItemInstance** level.

Mundane items:
- Have **no rolled affixes**
- Use only base stats defined by `ItemDef`
- Are safe to use but offer no magical advantages

Magical items:
- Have one or more **rolled affixes**
- Gain power through modifiers
- Carry greater economic value and loss risk

---

## ITEM DEFINITION (ItemDef)

`ItemDef` is a **ScriptableObject**.

### Authoritative Fields

- `ItemDefId` (stable string or enum)
- `DisplayName`
- `ItemCategory`
- `EquipmentSlotMask`
- `BaseWeight`
- `MaxDurability`
- `IsStackable`
- `MaxStackSize`
- `BaseModifiers[]` *(allowed, but see Mundane rule below)*
- `AllowedAffixPools[]`
- `DefaultItemPowerState` *(Mundane or Magical; see below)*

#### Mundane / Magical at the definition level

Some items may be authored as **always-magical** (e.g., rare artifacts) by setting:
- `DefaultItemPowerState = Magical`
- and providing `BaseModifiers[]` and/or curated affix definitions.

Most items should be authored as **mundane**:
- `DefaultItemPowerState = Mundane`
- `BaseModifiers[]` empty
- no pre-baked magical bonuses

> **ItemDefs are immutable at runtime.**

---

## ITEM INSTANCE (RUNTIME)

Each item carried, equipped, dropped, or looted is an **ItemInstance**.

### Required Runtime Fields

- `ItemInstanceId` (unique)
- `ItemDefId`
- `ItemPowerState` *(Mundane or Magical)*
- `CurrentDurability`
- `StackCount`
- `RolledAffixes[]` *(empty for Mundane)*
- `IsInsured`
- `AutoRenewInsurance`
- `InsuranceCostPaid`

#### Mundane rule (LOCKED)

If `ItemPowerState = Mundane`, then:
- `RolledAffixes[]` **must be empty**
- Item contributes **no modifiers** beyond its base weapon/armor numbers

(Weapons still have damage/swing speed; armor still has base resist values. “No modifiers” refers to *bonus stat modifiers/affixes*.)

**Rule (LOCKED):**
> Only the server may mutate ItemInstance data.

---

## ITEM CATEGORIES (LOCKED SET)

- Weapon
- Armor
- Jewelry
- Consumable
- Resource
- Container
- Tool
- Quest

Categories are **semantic only** and do not grant behavior by themselves.

---

## EQUIPMENT SLOTS (LOCKED)

### Weapons
- MainHand
- OffHand
- TwoHanded (occupies both)

### Armor / Wearables
- Head
- Torso
- Arms
- Hands
- Legs
- NeckArmor

### Jewelry
- Amulet
- Ring1
- Ring2
- Earrings

**Rule:**
> Items explicitly declare which slot(s) they occupy.

---

## WEAPON MODEL (COMBAT-CRITICAL)

Weapons must define:

- `MinDamage`
- `MaxDamage`
- `DamageType` (Physical, Fire, Cold, Poison, Energy)
- `SwingSpeedSeconds`
- `RequiredSkill` (e.g., Swords, Macing)
- `StaminaCostPerSwing`

Weapons do **not** apply damage directly.
They expose data consumed by the Combat system.

---

## ARMOR MODEL (COMBAT-CRITICAL)

Armor must define:

- `PhysicalResistance`
- `FireResistance`
- `ColdResistance`
- `PoisonResistance`
- `EnergyResistance`
- `DexterityPenalty` (optional, UO-style)

Resistances are **additive modifiers**, capped by Player rules.

---

## JEWELRY MODEL

Jewelry provides **pure modifiers**:

- Attributes (STR / DEX / INT)
- Hit / Defense Chance
- Damage Increase
- Spell modifiers (Faster Casting, SDI, LMC)

Jewelry never defines damage or armor directly.

---

## MODIFIER SYSTEM (LOCKED)

Items apply **stat modifiers** only.

### Mundane vs Magical

- **Mundane items** contribute **no bonus modifiers** (no affixes, no base modifiers).
- **Magical items** contribute modifiers via:
  - `BaseModifiers[]` (authored magical bases / artifacts)
  - `RolledAffixes[]` (rolled/enhanced modifiers)

### Modifier Examples

- `+STR`, `+DEX`, `+INT`
- `+HitChance`
- `+DefenseChance`
- `+DamageIncrease`
- `+SpellDamageIncrease`
- `+FasterCasting`
- `+LowerManaCost`
- `+Resist[Fire]`

**Rule (CRITICAL):**
> Items never contain combat logic. They only contribute numbers.

---

## DURABILITY SYSTEM (LOCKED)

Every equippable item has durability.

### Fields

- `MaxDurability`
- `CurrentDurability`

### Durability Loss

- Weapons: lose durability on swing
- Armor: lose durability when wearer is hit
- Death: durability damage to equipped items

### Breakage Rule

- At `CurrentDurability <= 0`:
  - Item becomes unusable
  - Modifiers stop applying
  - Item remains lootable

### Enhancement Breakage (LOCKED)

Enhancement attempts (see Enhancement section) can destroy an item even if durability is not 0.
This is separate from normal durability decay.

---

## CRAFTING & ENHANCEMENT (LOCKED)

### Mundane → Magical Enhancement

Mundane items may be **enhanced** into Magical items at appropriate crafting stations.

Enhancement requires:
- A valid crafting station (e.g. Forge, Enchanting Table)
- The Mundane item
- Required resources (defined per enhancement recipe)

### Enhancement Outcome Rules (LOCKED)

On enhancement attempt:

1. Server validates:
   - Item is Mundane
   - Item is enhanceable
   - Required resources are present

2. Server rolls enhancement result:
   - **Success** → Item becomes Magical
   - **Failure** → Item is destroyed (breakage)

3. On success:
   - `ItemQuality` changes to **Magical**
   - One or more affixes are rolled
   - Durability may be reduced as part of enhancement cost

4. On failure:
   - ItemInstance is destroyed
   - Resources are consumed

**Rule (CRITICAL):**
> Enhancement is irreversible.  
> Magical items cannot revert to Mundane.

---

## INVENTORY & CONTAINERS

- Inventory is a **container graph**
- Items may contain other items
- Containers have:
  - Capacity
  - Weight limits

Corpse objects are containers.

---

## ENHANCEMENT (MUNDANE → MAGICAL) (LOCKED)

Enhancement is the primary way a player turns a mundane item into a magical item.

### Core Concept

- Players find or craft **Mundane** items.
- At an appropriate **crafting station**, the player may combine:
  - The mundane item
  - Required resources (materials, reagents, essences, etc.)
- The server performs an **enhancement attempt**.
- On success:
  - Item becomes **Magical**
  - Affixes/modifiers are applied to the ItemInstance
- On failure:
  - The item may **break/destroy** based on a breakage chance
  - Consumed resources are still consumed (default)

### Enhancement Outputs

On successful enhancement:
- `ItemPowerState` is set to `Magical`
- `RolledAffixes[]` is populated (one or more entries)
- The item now contributes modifiers through the standard modifier pipeline

### Breakage Chance (Authoritative)

- Enhancement defines a `breakageChance` per attempt.
- Breakage is resolved server-side and deterministically.
- If broken:
  - The item instance is destroyed (removed from inventory)
  - No insurance payout occurs (insurance is for death loss, not crafting failure)

### Determinism + Networking

- Client sends **intent**: “Enhance this item with these resources at this station.”
- Server validates:
  - Station access
  - Ownership of item/resources
  - Eligibility (item must be Mundane unless a future rule allows rerolling)
  - Skill requirements (if/when crafting skills are hooked)
- Server rolls deterministically, applies result, replicates.

### Design Notes

- This system naturally supports later depth:
  - Higher tier resources → better affix pools
  - Crafting skill affects success/breakage chance
  - Special stations for specific item categories

---

## ITEM INSURANCE (LOCKED)

Insurance is **per-item** and optional.

### Rules

1. Insurance is purchased **up-front**
2. Cost is paid from **Banked Coins**
3. Insured items do **not** drop on death
4. Insurance is consumed on death
5. Auto-renew attempts to reapply insurance from Banked Coins

Failure to renew → item becomes uninsured.

---

## DEATH & LOOT INTERACTION

On player death:

- Corpse container is created
- All uninsured items transfer to corpse
- Insured items remain with player
- Held Coins drop to corpse
- Banked Coins are untouched

---

## NETWORKING RULES

### Server Authoritative

- Inventory contents
- Equipment state
- Durability
- Insurance
- Item drops

### Client Responsibilities

- Display UI
- Send equip/unequip requests
- Send use/interact intents

---

## IMPLEMENTATION DEPENDENCIES (UNBLOCKED)

With this document locked, the following systems can be implemented safely:

1. `ItemDef` ScriptableObject
2. `ItemInstance` runtime struct/class
3. EquipmentComponent
4. InventoryComponent
5. Durability processing
6. Corpse & loot system
7. **Combat Core**

---

## DESIGN LOCK CONFIRMATION

This document is **authoritative**.

Any change must:
- Increment Version
- Update Last Updated
- Explicitly call out combat or save-data implications

---

**Items are power.**  
**Power is risk.**  
**Risk creates stories.**

