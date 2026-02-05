# ABILITY_AND_SPELL_PAYLOADS.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 1.0  
Last Updated: 2026-02-05  
Engine: Unity 6 (URP)  
Networking: Netcode for GameObjects (NGO)  
Authority: Server-authoritative

---

## PURPOSE

This document defines the **authoritative Ability & Spell Payload system** for *Ultimate Dungeon*.

It replaces legacy "Magery / Spellbook" assumptions and realigns spell usage with the **Item-First** design pillar.

If an interaction is not defined here (or in a referenced authoritative document), **it does not exist**.

---

## CORE DESIGN TRUTH (LOCKED)

### Spells are **not learned**.  
### Spells are **not owned by the player**.  
### Spells are **granted by items**.

In *Ultimate Dungeon*:

- A **SpellId** is a **payload identifier**, not a progression unlock
- An **Ability** is an action granted by an equipped item
- The **hotbar** is a projection of equipped item abilities
- Skills modify **efficiency and risk**, never access

There are:
- ❌ No mage classes
- ❌ No spellbooks as progression
- ❌ No reagent-based spellcasting
- ❌ No permanent spell unlocks

This model is locked by:
- `SYSTEM_INTERACTION_MODEL.md`
- `ITEM_DEF_SCHEMA.md`

---

## DEFINITIONS (AUTHORITATIVE)

### SpellId
A stable identifier for a **payload definition**.

- SpellIds are defined in `SPELL_ID_CATALOG.md`
- IDs are append-only and never reordered
- Display names are cosmetic and may change

A SpellId **does not imply**:
- Who can use it
- How it is accessed
- How often it can be used

---

### Ability
An **Ability** is an action a player may activate that resolves through a `SpellId` payload.

Abilities:
- Are granted **only** by equipped items
- Are selected per-item via `AbilityGrantSlot`
- Are executed via the hotbar

Abilities are **never randomized** and are **not affixes**.

---

### SpellDef
A `SpellDef` ScriptableObject defines **how a SpellId resolves**.

SpellDefs own:
- Cast / use time
- Recovery / cooldown
- Costs (mana, stamina, durability, charges)
- Interruptibility rules
- Targeting rules
- Payload execution (damage, status, utility, summon, etc.)

SpellDefs **do not own**:
- Access control
- Item eligibility
- Hotbar configuration

Schema is defined in `SPELL_DEF_SCHEMA.md`.

---

## ITEM → ABILITY → SPELL RESOLUTION (LOCKED FLOW)

1. Player equips an item
2. Item grants one or more `AbilityGrantSlot`s
3. Player selects a `SpellId` per slot (out of combat only)
4. Hotbar activation resolves:
   - EquipSlot
   - ItemInstance
   - Active AbilityGrantSlot
   - Selected SpellId
5. Server validates:
   - Scene rules
   - Actor legality
   - Targeting legality
6. Server executes the SpellDef payload

If any step fails, the action is authoritatively denied.

---

## COST MODEL (LOCKED)

Spell usage costs are **item-defined or SpellDef-defined**, never reagent-based.

Allowed cost types:
- Mana
- Stamina
- Item durability
- Item charges / consumption
- Cooldown / recovery time

Reagents are **alchemy-only** and may never be required for spellcasting.

---

## INTERRUPT & RISK MODEL (LOCKED)

Abilities may be interrupted by:
- Damage
- Movement
- Status effects (Stun, Paralyze, Silence)

Interrupt rules are defined per `SpellDef`.

Interrupted abilities:
- Consume time
- Do not resolve payloads
- Do not consume single-use items unless explicitly defined

---

## SPECIAL SPELL CATEGORY RULES (LOCKED)

### Travel Spells: Mark / Recall / GateTravel

These spells **do not exist as generic abilities**.

They are restricted as follows:

- Source: **Rune Stone** (ItemFamily: UtilityItem)
- Equip Slot: `BeltA` or `BeltB`
- Ability Grants:
  - Primary: `Recall`
  - Secondary: `Mark`
  - Utility: `GateTravel`

Rules:
- Rune Stones store location data on the ItemInstance
- Travel legality is still gated by `SCENE_RULE_PROVIDER.md`
- No other item may grant these SpellIds unless explicitly authored

---

### Large Summon Spells

Examples:
- Energy Vortex
- Elementals
- Blade Spirits

Rules (LOCKED):
- Source: **Scroll items only** (ItemFamily: UtilityItem)
- Single-use (item is consumed on success)
- Rare loot only
- Cannot be granted by equipment
- Cannot be granted by affixes

Summons must still resolve through `SpellDef` and `StatusEffect` systems.

---

### Persistent Fields

Examples:
- Fire Field
- Poison Field
- Energy Field

Rules (LOCKED):
- Not globally available
- Must be explicitly granted by:
  - Specific items, **or**
  - Weapon proc payloads

No generic caster access exists.

---

## STATUS EFFECT INTEGRATION (LOCKED)

Spell payloads may:
- Apply statuses
- Remove statuses
- Refresh or stack statuses

All status semantics are owned by `STATUS_EFFECT_CATALOG.md`.

SpellDefs may never redefine:
- Action blocks
- Tick behavior
- Stack semantics

---

## SKILLS INTEGRATION (LOCKED)

Skills modify **efficiency and risk only**:

- Faster casting
- Lower failure chance
- Improved scaling
- Reduced interruption chance

Skills **never**:
- Unlock abilities
- Grant SpellIds
- Replace item requirements

---

## HARD RULES (ABSOLUTE)

1. Items grant abilities — nothing else
2. SpellIds are payload identifiers, not progression
3. Affixes never grant abilities
4. Skills never unlock spells
5. Hotbar configuration is frozen during combat
6. Reagents are never used for spellcasting

---

## DEPENDENCIES (AUTHORITATIVE)

This document must remain aligned with:
- `SYSTEM_INTERACTION_MODEL.md`
- `ITEM_DEF_SCHEMA.md`
- `SPELL_ID_CATALOG.md`
- `SPELL_DEF_SCHEMA.md`
- `STATUS_EFFECT_CATALOG.md`
- `SCENE_RULE_PROVIDER.md`

---

## DESIGN LOCK CONFIRMATION

This document is **authoritative**.

Any change must:
- Increment Version
- Update Last Updated
- Explicitly call out impacted systems

---

**Power comes from what you carry — not what you studied.**

