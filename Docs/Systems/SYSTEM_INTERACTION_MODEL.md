# SYSTEM_INTERACTION_MODEL.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 0.3  
Last Updated: 2026-02-04  
Engine: Unity 6 (URP)  
Networking: Netcode for GameObjects (NGO)  
Authority: Server-authoritative  

---

## PURPOSE

This document defines **how the major gameplay systems interact** in *Ultimate Dungeon*.

Its role is to prevent systemic drift ("system soup") by explicitly locking:
- **System ownership boundaries**
- **Allowed interaction paths**
- **Where RNG is permitted**
- **Where player power originates**

If an interaction is not defined here (or in a referenced authoritative document), **it does not exist**.

---

## AUTHORITATIVE DEPENDENCIES

This model must remain aligned with:
- `DOCUMENTS_INDEX.md`
- `ITEMS.md`
- `ITEM_DEF_SCHEMA.md`
- `ITEM_CATALOG.md`
- `ITEM_AFFIX_CATALOG.md`
- `SKILL_ID_CATALOG.md`
- `SPELL_ID_CATALOG.md`
- `STATUS_EFFECT_CATALOG.md`
- `ACTOR_MODEL.md`
- `TARGETING_MODEL.md`
- `SCENE_RULE_PROVIDER.md`
- `COMBAT_CORE.md`
- `PLAYER_MODEL.md`
- `PLAYER_DEFINITION.md`
- `PLAYER_COMBAT_STATS.md`

---

## DESIGN PILLAR (LOCKED)

### The game is **Item-First**

**Items are the primary source of player power, identity, and agency.**

- Items grant the player their **verbs** (abilities)
- Items define base combat context (weapon/armor profiles)
- Items introduce **RNG** via affixes
- Skills modify efficiency and permissions, but **never replace items**

This pillar must remain intact to avoid class- or build-tree drift.

---

## SYSTEM ROLES & OWNERSHIP (LOCKED)

Each system may only perform the responsibilities listed under **Owns**.

---

### 1) Items

**Owns (authoritative):**
- Item identity model: `ItemDef` vs `ItemInstance`
- Equipment slot legality and family mapping
- Authored base data for weapons, armor, containers, consumables
- Item durability state (instance-only)
- Affix pool eligibility
- Item-granted ability *choices* (allowed `SpellId`s per slot)

**Does NOT own:**
- Combat legality
- Combat execution
- Status semantics
- Spell payload logic

**Locks:**
- `ItemDef` is immutable authored data
- `ItemInstance` holds runtime state (durability, affixes, selected spells)
- Server exclusively creates and mutates instances
- Magical items are defined by affix presence (0..N)

---

### 2) Affixes (Random Properties)

**Owns:**
- Affix definitions and IDs
- Valid numeric ranges and tiers
- Stacking and cap rules
- Affix count resolution (loot vs enhancement)

**Does NOT own:**
- Abilities or verbs
- Combat logic

**Locks:**
- **Affixes are numeric modifiers only**
- **Affixes never grant abilities**

---

### 3) Item-Granted Abilities

**Definition (LOCKED):**  
An **ability** is an action the player may activate that resolves through the spell/ability payload pipeline.

**Owns:**
- Ability availability via **equipped items only**
- Ability grant structure (`AbilityGrantSlots`: Primary / Secondary / Utility)
- Ability selection stored on the **ItemInstance**
- Hotbar as a projection of equipped item grants

**Does NOT own:**
- RNG
- Progression unlocks

**Locks:**
- Granted abilities are part of base item identity
- Ability access is never randomized
- **During combat, ability selection and hotbar configuration are frozen**

---

### 4) Skills

**Owns:**
- Skill values (0–100) and global caps
- Permission and efficiency inputs:
  - Weapon proficiency requirements
  - Crafting/enhancement skill checks
  - Use-based progression rules

**Does NOT own:**
- Ability unlocks
- Item definitions
- RNG

**Locks:**
- No classes, levels, or XP
- Server authoritative values

---

### 5) Spells / Ability Payloads

**Definition (LOCKED):**  
A **SpellId** is a stable identifier for an effect payload. Spells are **data**, not progression.

**Owns:**
- Targeting model (self / target / ground / area)
- Effect payloads:
  - Damage
  - Heal
  - ApplyStatus
  - Utility
  - Summon (restricted; see locks)

**Does NOT own:**
- Access control
- Combat legality
- Status semantics

**Locks:**
- All item-granted abilities resolve via `SpellId`
- Reagents are used by **Alchemy only**, never spellcasting

---

### 6) Status Effects

**Definition (LOCKED):**  
A **Status Effect** is an authoritative state applied to an Actor that gates actions, modifies timing, or emits ticks.

**Owns:**
- Status IDs and semantics
- Stack rules and durations
- Global action gates:
  - BlocksWeaponAttacks
  - BlocksSpellcasting
  - BlocksBandaging
  - BlocksMovement
- Time multipliers and tick effects

**Does NOT own:**
- Items
- Combat resolution
- Targeting validation

**Locks:**
- Server authoritative application and ticking
- Systems query structured flags, not per-status hardcode

---

### 7) Actor + Targeting (Legality)

**Owns:**
- Actor identity and faction context
- Scene rule context and hard gates
- Validation of player intents:
  - Select
  - Interact
  - Attack
  - CastBeneficial
  - CastHarmful

**Does NOT own:**
- Combat math
- Stat aggregation

**Locks:**
- Clients submit intents; server validates
- Safe scenes categorically refuse hostile intents

---

### 8) Combat Core (Execution)

**Owns:**
- Scheduling and resolving attacks and bandages
- Deterministic hit/miss and damage resolution
- Weapon proc rolls (weapon-only)
- Durability loss and death triggers

**Does NOT own:**
- Hostility decisions
- Item schemas

**Locks:**
- Legality must be validated before scheduling
- Scene flags must be re-checked at resolution

---

### 9) Player Combat Stat Aggregation

**Owns:**
- Single authoritative combat stat snapshot
- Centralized caps and aggregation policies

**Consumes:**
- PlayerDefinition baselines
- Equipped items and affixes
- Status gates and multipliers

**Does NOT own:**
- RNG
- Combat flow

---

## HOTBAR → EQUIPMENT → ABILITY RESOLUTION (LOCKED)

### Core Rule

The hotbar is a **direct projection of equipped items**.

- The hotbar contains **exactly one slot per EquipSlot**
- Hotbar slot indices map **1:1** to `EquipSlot`
- Hotbar slots **never reference inventory slots or container indices**

Inventory is storage only. Power originates exclusively from **equipped items**.

---

### Per-Item Hotbar Configuration

Each equipped `ItemInstance`:
- Defines one or more `AbilityGrantSlots` (`Primary`, `Secondary`, `Utility`)
- Stores a **selected SpellId** per grant slot (from allowed options)
- Stores **which AbilityGrantSlot is active on the hotbar** for that item

The player may:
- Select which grant slot is active for the hotbar
- Select which SpellId is chosen within each grant slot

**Restrictions (LOCKED):**
- Ability selection and hotbar configuration **cannot change during combat**
- If an item supports only one grant slot, it is forced active

---

### Hotbar Activation Flow (Authoritative)

When a hotbar slot is activated:

1. Resolve `EquipSlot` from hotbar index
2. Resolve equipped `ItemInstance`
3. Resolve item’s active `AbilityGrantSlot`
4. Resolve selected `SpellId` for that grant slot
5. Validate:
   - Scene rules
   - Actor legality
   - Combat state
   - Targeting legality
6. Submit intent (`CastBeneficial` or `CastHarmful`)
7. Execute spell/ability payload on server

If any step fails, the activation is **authoritatively denied**.

---

### Unequip & Invalid State Rules (LOCKED)

- Unequipping an item immediately invalidates its hotbar slot
- Hotbar slots with no equipped item are inert
- If an item changes (durability break, replacement):
  - Hotbar resolution always uses **current ItemInstance state**

No hotbar binding is cached across equip changes.

---

## CANONICAL RUNTIME FLOWS (LOCKED)

### Flow A — Equip Change → Stat Refresh
1. Equip/unequip `ItemInstance`
2. Server validates legality
3. Equipment state updates
4. `PlayerCombatStats` recomputed
5. Clients update UI

---

### Flow B — Using an Item-Granted Ability (Hotbar)
1. Client activates hotbar slot
2. Client submits intent (`CastBeneficial` or `CastHarmful`)
3. Server validates:
   - Scene rules
   - Actor legality
   - Targeting
   - Item access (equipped + selected SpellId)
4. Server executes payload
5. Payload may:
   - Apply statuses
   - Emit Damage/Heal packets
   - Trigger visuals / UI events

---

### Flow C — Weapon Attack
1. Client issues Attack intent
2. Server validates legality
3. Combat Core schedules swing
4. On resolution:
   - Re-gate scene
   - Consume stamina/ammo
   - Roll hit/miss
   - Apply damage, procs, durability loss

---

### Flow D — Weapon Procs
1. Weapon hit resolves
2. ProcProfile read from weapon instance
3. Deterministic proc roll
4. On success:
   - Damage payloads, and/or
   - ApplyStatus payloads

---

## HARD RULES (LOCKED)

1. No hidden auto-hostility
2. No client-authoritative outcomes
3. Affixes never grant abilities
4. Skills never unlock spells
5. No combat in safe scenes
6. No ability selection or hotbar reassignment during combat

---

## TERMINOLOGY (LOCKED)

- **ItemDef** — immutable authored definition
- **ItemInstance** — runtime state (durability, affixes, selected spells)
- **Ability** — item-granted action executed via SpellId
- **SpellId** — payload identifier
- **Affix** — numeric modifier
- **Status Effect** — authoritative gated state
- **Intent** — client request validated by server

---

## DESIGN LOCK CONFIRMATION

This document is **authoritative**.

Any change must:
- Increment Version
- Update Last Updated
- Explicitly call out impacted systems
