# PLAYER_MODEL.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 0.2  
Last Updated: 2026-01-28

---

## PURPOSE

This document defines the **Player Actor runtime model and architectural contract** for *Ultimate Dungeon*.

It answers the question:

> *What is a Player at runtime, and how do systems are allowed to interact with it?*

This document is **structural and architectural only**.

It deliberately does **not** define:
- Numeric values
- Caps or balance
- Progression rules
- Combat math
- Item stats

All such rules live in their respective authoritative documents.

---

## DESIGN LOCKS (ABSOLUTE)

1. **Server-authoritative state**
   - The server is the sole authority for all gameplay-affecting state.

2. **No classes / no levels / no XP**
   - Progression is strictly **skill-based**.

3. **Status effects are decoupled from items**
   - Items may apply or grant statuses, but **status behavior is defined and resolved independently**.

4. **Deterministic resolution**
   - All gameplay outcomes must be reproducible from deterministic inputs.

---

## SCOPE BOUNDARIES (NO OVERLAP)

### This document **owns**:
- Player-as-Actor definition
- Runtime component responsibilities
- Authority & replication rules
- High-level integration contracts between systems

### This document **does NOT own**:
- Player baselines, caps, or starting values *(see `PLAYER_DEFINITION.md`)*
- Combat rules or formulas *(see `COMBAT_CORE.md`)*
- Stat aggregation logic *(see `PLAYER_COMBAT_STATS.md`)*
- Skill identifiers *(see `SKILL_ID_CATALOG.md`)*
- Item schemas or lists *(see `ITEM_DEF_SCHEMA.md`, `ITEM_CATALOG.md`)*
- Status semantics *(see `STATUS_EFFECT_CATALOG.md`)*

If a rule is not explicitly structural, it does **not** belong here.

---

## PLAYER = ACTOR

A **Player** is a specialized **Actor** that:
- Is network-owned by a single client
- Exists in a persistent multiplayer world
- Issues *intents* (never direct actions)
- Has all gameplay state validated and mutated by the server

The Player is **not privileged**.
NPCs, enemies, and summons follow the same Actor rules where applicable.

---

## RUNTIME COMPONENT MODEL

The Player Actor is composed of modular, server-authoritative components.

> Exact class names may vary; this list defines the **required responsibilities**, not the exact implementation.

### Core Identity & Authority

- **PlayerNetIdentity**
  - NGO ownership
  - Local vs remote player detection
  - Server authority enforcement

- **PlayerCore**
  - Central runtime hub
  - Binds and exposes authoritative subsystems
  - Initializes server-only systems

---

### Attributes, Vitals & Skills

- **PlayerStats**
  - Holds primary attributes
  - Applies modifiers from items and statuses
  - Exposes *effective* values only

- **PlayerVitals**
  - Holds current/max vitals
  - Applies regen and damage
  - Exposes vitals to Combat Core

- **PlayerSkillBook**
  - Holds skill values and lock states
  - Receives gain/loss requests from progression systems

> Numeric derivation, caps, and progression rules are defined in `PLAYER_DEFINITION.md` and `PROGRESSION.md`.

---

### Inventory & Equipment

- **InventoryComponent**
  - Container graph (bags within bags)
  - Stack handling and weight calculation

- **EquipmentComponent**
  - Manages equipped ItemInstances
  - Enforces slot and handedness rules
  - Exposes equipped items to stat aggregation

> Item identity, schema, and base values are defined in item system documents.

---

### Status Effects

- **StatusEffectSystem**
  - Applies, refreshes, stacks, and removes statuses
  - Ticks timed effects
  - Exposes action gates and modifiers

Status behavior and semantics are defined exclusively in `STATUS_EFFECT_CATALOG.md`.

This system **never** executes combat logic.

---

### Targeting & Interaction

- **TargetingComponent**
  - Tracks the player’s current target
  - Exposes validated target references

- **InteractionComponent**
  - Submits interaction intents
  - Server validates range, legality, and ownership

---

## AUTHORITY & REPLICATION MODEL

### Server-Authoritative State

The server is the sole writer for:
- Vitals (current and max)
- Attributes and derived stats
- Skill values and lock states
- Inventory contents and container topology
- Equipped items
- Active status effects
- Currency balances
- Death and respawn state

---

### Client Responsibilities

Clients are limited to:
- Submitting input intents
- Rendering UI and visuals
- Cosmetic-only prediction (optional)

Clients **never**:
- Modify gameplay state
- Roll RNG
- Apply damage or healing

---

### Replication

The following are replicated to clients as read-only data:
- Vitals snapshots
- Skill values
- Attribute summaries
- Combat events
- Status visuals (icons, VFX triggers)

---

## STATUS EFFECT INTEGRATION CONTRACT

The Player Actor must expose a status receiver interface that allows:
- Querying action gates (movement, attack, casting, bandaging)
- Applying periodic effects (DoT, HoT)
- Applying modifiers (speed, swing time, cast time)

Status execution is handled by consuming systems (Combat, Movement), not by the Player itself.

---

## DEATH & RESPAWN (STRUCTURAL ONLY)

This document defines **when** death occurs, not **what happens**.

- When HP reaches zero, the Player enters a Dead state
- Combat Core triggers death exactly once
- Death handling is delegated to the death/loot pipeline

> Loot loss, insurance, corpse creation, and currency rules are owned by `PLAYER_DEFINITION.md`.

---

## IMPLEMENTATION GUARANTEES

With this model locked:
- Combat systems can rely on stable interfaces
- Status effects can be added without refactors
- Item systems remain decoupled
- Progression laws remain enforceable

This document must remain **numerically empty**.

---

## DESIGN LOCK CONFIRMATION

This document is **authoritative for player architecture only**.

Any change must:
- Increment Version
- Update Last Updated
- Declare impacted dependent documents

