# ACTOR_MODEL.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 1.0  
Last Updated: 2026-01-29  
Engine: Unity 6 (URP)  
Networking: Netcode for GameObjects (NGO)  
Authority: Server-authoritative  
Determinism: Required (server-seeded)

---

## PURPOSE

Defines the **authoritative Actor model** for *Ultimate Dungeon*.

This document is the missing “world identity + relationships” layer that allows:
- PvE and PvP to share one combat pipeline
- Targeting to consistently identify **hostile vs neutral vs friendly**
- Combat systems to reliably determine **in combat / out of combat**
- NPCs/guards/summons to follow the same interaction laws as players

**Actor** is the shared contract for:
- Player
- Monster
- NPC (vendors, guards, quest NPCs)
- Summon / Pet
- Future destructibles (optional)

If an Actor rule is not defined here (or in referenced authoritative docs), **it does not exist**.

---

## SCOPE BOUNDARIES (NO OVERLAP)

### This doc owns
- Actor identity and type taxonomy
- Faction identity and relationship model
- Hostility / friendliness evaluation rules
- Combat state (in combat / out of combat) rules
- Targeting eligibility and legality rules
- Aggression + PvP flag surface (minimal v1)

### This doc does NOT own
- Player-only progression, skills, caps, currency, insurance *(see `PLAYER_DEFINITION.md`)*
- Combat math, swing timers, damage formulas *(see `COMBAT_CORE.md`)*
- Combat stat aggregation *(see `PLAYER_COMBAT_STATS.md`)*
- Status definitions / semantics *(see `STATUS_EFFECT_CATALOG.md`)*
- Item schemas and catalogs *(see `ITEM_DEF_SCHEMA.md`, `ITEM_CATALOG.md`, `ITEMS.md`)*
- Spell definitions *(see `SPELL_DEF_SCHEMA.md` and `SPELL_ID_CATALOG.md`)*

---

## DESIGN LOCKS (MUST ENFORCE)

1. **Actor-first**
   - Combat and targeting talk to **Actor**, not “Player”.

2. **Server authoritative**
   - Faction, combat state, aggression, and PvP flags are written only by the server.

3. **Deterministic resolution**
   - Any roll that depends on hostility/flag rules (rare) must be server-seeded.

4. **Single source of truth**
   - There is exactly one place where hostility and “can I attack this?” are decided: **Actor rules**.

5. **Minimal v1, expandable**
   - v1 provides enough to stabilize targeting/combat.
   - Reputation systems (innocent/criminal/murderer), guilds, and regional laws can be layered later.

---

## CORE TERMS

### Actor
Any entity that can:
- be targeted
- be interacted with
- receive damage/healing/status effects
- potentially die

### Faction
A stable identity used to determine default relationships between actors.

### Relationship
How one actor **should treat** another by default:
- Friendly
- Neutral
- Hostile

### Aggressor
An actor who has initiated harmful action toward another actor recently.

---

## AUTHORITATIVE ENUMS

### ActorType (authoritative)
Defines what kind of actor this is.

- **Player**
- **Monster**
- **NPC**
- **Guard** *(special NPC with law enforcement behavior)*
- **Summon**
- **Pet**
- **Destructible** *(optional future; breakable props, doors if they become targetable)*

> NOTE: ActorType is not used for balance. It exists for legality/UX defaults.

---

### FactionId (authoritative, v1)
Stable faction identities.

- **Players**
- **Monsters**
- **TownNPC**
- **Guards**
- **Summons**
- **Pets**

> v1 keeps factions broad. Later expansion may add regional factions (CraterVillage, DungeonBandits, etc.).

---

### RelationshipType (authoritative)
- **Friendly**
- **Neutral**
- **Hostile**

---

### CombatState (authoritative)
A gameplay state, not an animation state.

- **Idle** *(not in combat)*
- **Engaged** *(has a target / is attempting combat actions)*
- **InCombat** *(recently dealt or received hostile action; timers active)*
- **Dead** *(cannot act; targetable rules differ)*

---

### TargetingDisposition (authoritative)
The “UI-friendly” evaluation result for a target relative to the viewer.

- **Self**
- **Friendly**
- **Neutral**
- **Hostile**
- **Invalid** *(not targetable due to rules/status)*

---

## ACTOR DATA CONTRACT (RUNTIME)

### ActorIdentity (required runtime data)
Every Actor must expose:
- `ActorId actorId` *(stable runtime id; network object id is acceptable in v1)*
- `ActorType actorType`
- `FactionId factionId`
- `bool isAlive`
- `CombatState combatState`

### ActorVitals (required for combat)
Every combat-capable actor must expose:
- HP current/max (or a source that can provide it)

> v1 already has `ActorVitals` present in scripts. Keep it as the minimal HP contract.

### Status receiver (required)
Actors must have a `StatusEffectSystem` or equivalent receiver, because:
- Targeting legality (e.g., Invisible/Revealed) depends on statuses
- Combat gates depend on statuses

Status semantics remain owned by `STATUS_EFFECT_CATALOG.md`.

---

## FACTION RELATIONSHIP MODEL (DEFAULTS)

This is the **baseline** relationship matrix used when no special case applies.

### Default Relationship Matrix (v1)
Interpretation: row = **viewer**, column = **target**.

| Viewer \\ Target | Players | Monsters | TownNPC | Guards | Summons | Pets |
|---|---:|---:|---:|---:|---:|---:|
| **Players** | Neutral | Hostile | Neutral | Neutral | Neutral | Neutral |
| **Monsters** | Hostile | Neutral | Neutral | Neutral | Hostile | Hostile |
| **TownNPC** | Neutral | Neutral | Neutral | Friendly | Neutral | Neutral |
| **Guards** | Neutral | Hostile | Friendly | Friendly | Neutral | Neutral |
| **Summons** | Neutral | Hostile | Neutral | Hostile* | Neutral | Neutral |
| **Pets** | Neutral | Hostile | Neutral | Neutral | Neutral | Neutral |

\* Summons are **hostile to Guards** by default **only** if the Summon is flagged as “harmful/illegal” in a Guarded region.

> This matrix is intentionally conservative.
> PvP hostility is not “default hostile”; it is governed by PvP legality rules (below).

---

## OWNERSHIP & CONTROL (PLAYER-LINKED ACTORS)

Some actors are controlled/owned by a player:
- Summons
- Pets

### ControlLink (recommended runtime data)
- `ActorId controllerActorId` *(player actor id)*
- `ulong controllerClientId` *(NGO owner client id; optional)*

**Rule (LOCKED):**
- Controlled actors inherit **social consequences** from their controller (PvP flags, guard response) in v1.

---

## PVP LEGALITY (MINIMUM VIABLE v1)

PvP needs a minimum rule-set so targeting and combat can answer “can I attack this?” consistently.

### PvPRegionRule (authoritative enum)
- **PvPAllowed** *(dungeon / wilderness)*
- **NoPvP** *(core village safe zones)*

> Region rules are owned by the world/zone system later, but the enum and meaning are defined here.

### PlayerPvPFlags (v1)
These are server-owned flags on **Player actors**.

- `bool pvpEnabled` *(optional toggle if you want consensual PvP in some areas)*
- `bool isCriminal` *(set when performing illegal hostile action in guarded/noPvP areas)*
- `bool isMurderer` *(future; v1 may not use)*

**v1 recommendation:**
- In **NoPvP** regions: player hostile actions against players are disallowed.
- In **PvPAllowed** regions: player hostile actions are allowed.

> If you want consensual-only PvP, replace the above with: “both players must have pvpEnabled=true.”
> That is a design choice; not locked yet.

---

## AGGRESSION & COMBAT STATE RULES (LOCKED)

### Aggression events (authoritative)
An aggression event occurs when an actor performs a **hostile action** against another actor:
- weapon swing attempt that is validated and executed
- harmful spell payload applied
- hit spell proc applied
- DoT applied that originates from the actor

### Aggression memory (v1)
- Aggression is remembered for **N seconds** after the last hostile action.

**AggressionTimeoutSeconds (PROPOSED — Not Locked):** `10.0`

### CombatState transitions (LOCKED semantics)
- **Idle → Engaged**: actor sets an attack target or begins an attack/cast intent.
- **Engaged → InCombat**: actor deals or receives a hostile action.
- **InCombat → Idle**: no hostile actions dealt/received for AggressionTimeoutSeconds.
- **Any → Dead**: HP reaches 0 (Combat Core triggers once).

**Rules:**
- CombatState is server-owned and replicated (for UI/rings).
- Client may display “in combat” using replicated state only.

---

## TARGETING ELIGIBILITY & DISPOSITION

Targeting is a two-step decision:

1) **Eligibility** — can this actor be targeted at all?
2) **Disposition** — if targetable, what is it relative to the viewer?

### 1) Eligibility (LOCKED)
An Actor is **not targetable** if any are true:
- `combatState == Dead` *(unless the system is corpse/loot; out of scope here)*
- Actor has `Utility_Invisible` and viewer does not have valid reveal/permission
- Actor is outside maximum targeting range (system-specific)

> Exact invisibility rules are enforced via status semantics (`STATUS_EFFECT_CATALOG.md`).

### 2) Disposition resolution (LOCKED order)
Given a `viewer` and `target` actor:

1. If `viewer.actorId == target.actorId` → `Self`
2. If target not eligible → `Invalid`
3. Resolve default relationship using the Faction matrix
4. Apply overrides:
   - Player vs Player legality (region + flags)
   - Controller inheritance (summons/pets inherit controller hostility)
   - Guard law overrides (guards treat criminals as Hostile in guarded zones)
5. Output one of: Friendly / Neutral / Hostile

---

## “CAN I ATTACK THIS?” (AUTHORITATIVE LEGALITY)

Combat must ask Actor rules before starting/continuing attacks.

### AttackLegalityResult (authoritative)
- **Allowed**
- **Disallowed_NotHostile** *(e.g., trying to attack a friendly NPC)*
- **Disallowed_Region** *(NoPvP zone)*
- **Disallowed_TargetInvalid** *(invisible, dead, etc.)*
- **Disallowed_StatusGate** *(attacker cannot attack due to status gates)*

> Status gates (stun/paralyze/etc.) are defined in `STATUS_EFFECT_CATALOG.md` and surfaced via aggregated combat stats.

### Legality rules (v1)
An attack is allowed when all are true:
- target is eligible
- viewer is allowed to attack the target under PvE/PvP rules
- target disposition resolves to **Hostile**

**PvE default:** Players can attack Monsters.

**PvP default (recommended):**
- Allowed only in PvPAllowed regions.

---

## REQUIRED IMPLEMENTATION ARTIFACTS (NEXT)

1. `ActorComponent` (runtime)
   - Exposes ActorIdentity fields and combat state.

2. `FactionService` (pure rules)
   - Provides relationship matrix lookup.

3. `TargetingResolver` (pure rules)
   - Implements eligibility + disposition evaluation.

4. `CombatStateTracker` (server)
   - Tracks aggression events and drives CombatState transitions.

5. Integrations:
   - Target ring tint uses TargetingDisposition (already exists as `TargetRingFactionTint` conceptually)
   - Combat Core validates AttackLegality before scheduling swings

---

## OPEN QUESTIONS (PROPOSED — NOT LOCKED)

1. PvP model:
   - Free-for-all in dungeon vs consensual toggle
2. Guarded region definitions:
   - How zones are authored and replicated
3. Reputation depth:
   - Innocent/Criminal/Murderer system (UO-like) scope and timeline

---

## DESIGN LOCK CONFIRMATION

This document is **authoritative**.

Any change must:
- Increment Version
- Update Last Updated
- Call out impacted dependent docs/systems (Combat, Targeting, AI, Zones)

