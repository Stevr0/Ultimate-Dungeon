# TARGETING_MODEL.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 1.0  
Last Updated: 2026-02-11  
Engine: Unity 6 (URP)  
Authority: Server-authoritative (Shard Host)  

---

## PURPOSE

Defines the **authoritative targeting and intent validation model** for *Ultimate Dungeon*.

This document locks:
- What it means to target something
- Which intents are legal against which targets
- How scene rules, actor rules, and relationships gate targeting
- The separation between **selection**, **interaction**, and **hostile intent**

Targeting is a **legality gate**, not a combat system.

---

## SCOPE BOUNDARIES (NO OVERLAP)

Owned elsewhere:
- Actor identity & legality: `ACTOR_MODEL.md`
- Scene legality contexts & flags: `SCENE_RULE_PROVIDER.md`
- Combat execution & math: `COMBAT_CORE.md`
- Status effects: `STATUS_EFFECT_CATALOG.md`

This document does **not** define:
- Damage formulas
- Hit/miss resolution
- Spell payload execution
- UI targeting widgets

---

## DESIGN LOCKS (MUST ENFORCE)

1. **Targeting is server-authoritative**
   - Clients submit targeting intents.
   - Server validates legality.

2. **Selection ≠ Interaction ≠ Hostility**
   - Selecting a target never implies permission to interact or attack.

3. **Scene rules are hard gates**
   - If a scene disallows an intent, targeting fails regardless of actor state.

4. **Actor legality precedes combat**
   - Combat systems never decide if a target is legal.

5. **Targeting does not bypass housing or permissions**
   - Targeting an object does not grant authority over it.

---

## CORE CONCEPTS

### Target
Any **Actor** or **Object** that can be referenced by an intent.

Examples:
- Player
- Monster
- NPC / Vendor
- Door / Chest / Lever
- Housing object

---

### TargetIntentType (LOCKED)

All targeting requests declare exactly one intent:

- `Select`  
  *Non-hostile highlight or focus*

- `Interact`  
  *Use, open, talk, trade*

- `Attack`  
  *Direct hostile action*

- `CastBeneficial`  
  *Buffs, heals, self/ally effects*

- `CastHarmful`  
  *Offensive or debuff spells*

---

## VALIDATION PIPELINE (AUTHORITATIVE)

When the server receives a targeting intent:

1. **Resolve Target**
   - Does the target exist?
   - Is it an Actor/Object?

2. **Resolve SceneRuleContext**
   - From `SCENE_RULE_PROVIDER.md`

3. **Check Scene Flags**
   - `CombatAllowed`
   - `PvPAllowed`
   - `BuildingAllowed`

4. **Resolve Actor Legality**
   - ActorType vs ActorType
   - Faction relationship
   - Alive / Dead state

5. **Resolve Intent-Specific Rules**
   - Attack vs Interact vs Cast

6. **Return Allow or Deny + Reason**

If any step fails, the intent is rejected.

---

## SCENE-SPECIFIC TARGETING RULES (LOCKED)

### ShardVillage

**Allowed**:
- `Select` any non-hostile Actor/Object
- `Interact` with NPCs, Vendors, Doors, Containers
- `CastBeneficial` (self / allies)

**Denied**:
- `Attack`
- `CastHarmful`
- Any hostile targeting intent

**Notes**:
- PvP is always denied in ShardVillage.
- Targeting housing objects does not grant modify rights.

---

### Dungeon

**Allowed**:
- `Select` any Actor/Object
- `Interact` with dungeon objects
- `Attack` hostile Actors
- `CastBeneficial`
- `CastHarmful`

**PvP**:
- Allowed if:
  - `PvPAllowed == true`
  - Actor relationship permits hostility

---

## PvP TARGETING (LOCKED)

PvP targeting is legal only when:

1. `SceneRuleContext == Dungeon`
2. `PvPAllowed == true`
3. Both Actors are alive
4. Faction/relationship rules allow hostility

Shard host latency advantage is intentional and accepted.

---

## HOUSING & OBJECT TARGETING

- Housing objects may always be:
  - Selected
  - Inspected

- Housing objects may be interacted with only if:
  - Interaction is read-only (e.g. vendor browse), **or**
  - The actor has housing permission (Owner / CoOwner / Editor)

Targeting does not bypass permission checks.

---

## FAILURE MODES (LOCKED)

- Invalid target → reject
- Scene disallows intent → reject
- Actor state invalid → reject
- Permission failure → reject

All failures must return a **deterministic deny reason**.

---

## REQUIRED UPDATES TO DOCUMENTS_INDEX.md (PATCH)

Update `TARGETING_MODEL.md` description to reflect:
- ShardVillage vs Dungeon legality split
- Explicit separation of Select / Interact / Hostile intents

---

## DESIGN LOCK CONFIRMATION

This document is **authoritative**.

Any change must:
- Increment Version
- Update Last Updated
- Explicitly call out impacts to targeting legality, PvP rules, or housing interaction

