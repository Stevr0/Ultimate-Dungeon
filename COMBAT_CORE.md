# COMBAT_CORE.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 1.5  
Last Updated: 2026-01-29  
Engine: Unity 6 (URP)  
Networking: Netcode for GameObjects (NGO)  
Authority: Server-authoritative  
Determinism: Required (server-seeded)  

---

## PURPOSE

Defines the **authoritative Combat Core** for *Ultimate Dungeon*, aligned with:
- `ACTOR_MODEL.md` (Actors, legality, factions, **SceneRuleContext**)
- `TARGETING_MODEL.md` (intent validation and scene gating)

Combat Core is responsible for **how combat is executed**, not **whether combat is legal**.

Combat Core executes:
- Swing timers
- Hit / miss resolution
- Damage application
- Status application hooks
- Death triggers

If a combat rule is not defined here, **it does not exist**.

---

## SCOPE BOUNDARIES (NO OVERLAP)

### This document owns
- Combat execution order
- Server scheduling model (swing timers, bandage timers)
- Deterministic RNG usage
- `DamagePacket` / `HealPacket` resolution
- Durability loss triggers
- Death trigger and handoff

### This document does NOT own
- Actor identity, faction, hostility, PvP legality *(see `ACTOR_MODEL.md`)*
- Scene rule definitions *(see `ACTOR_MODEL.md`)*
- Target intent validation *(see `TARGETING_MODEL.md`)*
- Player baselines and caps *(see `PLAYER_DEFINITION.md`)*
- Combat stat aggregation *(see `PLAYER_COMBAT_STATS.md`)*
- Status definitions *(see `STATUS_EFFECT_CATALOG.md`)*
- Item schemas and affix catalogs *(see item system docs)*

---

## DESIGN LOCKS (MUST ENFORCE)

1. **Actor-first**
   - Combat Core operates exclusively on Actors.
   - Player, Monster, NPC logic is never special-cased here.

2. **Server authoritative**
   - Clients send intents only.
   - All combat resolution happens on the server.

3. **Deterministic resolution**
   - All RNG uses server-seeded deterministic sources.

4. **Single damage pipeline**
   - Weapon hits, spell damage, DoT ticks, and procs all resolve via `DamagePacket`.

5. **PvE and PvP share rules**
   - The same execution pipeline is used once legality is confirmed.

6. **Scene rules are hard gates**
   - If a scene disallows combat/damage/death/durability, Combat Core must not schedule or resolve those outcomes.

7. **Status-first integrity**
   - Combat respects action gates and modifiers derived from status effects.

---

## DEPENDENCY: SCENE RULE CONTEXT (MANDATORY)

Combat Core must consume **SceneRuleFlags** derived from `SceneRuleContext` (see `ACTOR_MODEL.md`).

### Required flags
- `CombatAllowed`
- `DamageAllowed`
- `DeathAllowed`
- `DurabilityLossAllowed`

**Design lock:** If `CombatAllowed == false`, Combat Core must refuse:
- Scheduling any swing timers
- Resolving any hit/miss rolls
- Creating any `DamagePacket` from weapon attacks

**Design lock:** If `DamageAllowed == false`, Combat Core must refuse:
- Applying any damage (even if a timer was scheduled by mistake)

This double-gate prevents exploits caused by miswired client/UI or stale timers.

---

## CORE TERMS

### Actor
A runtime entity defined by `ACTOR_MODEL.md` that:
- Has `ActorType`, `FactionId`, and `CombatState`
- Can be targeted and damaged (depending on scene + legality)
- May die (depending on scene)

Combat Core **never** decides hostility or PvP rules.

---

### CombatState
Server-authoritative state defined in `ACTOR_MODEL.md`.

Combat Core must:
- Refuse actions from `CombatState.Dead`
- Notify the CombatStateTracker on hostile actions
- Never infer combat state implicitly

**Scene lock:** In safe scenes, an Actor may never transition into `InCombat`.

---

### DamagePacket
A single atomic damage request produced by combat execution.

### HealPacket
A single atomic healing request produced by bandaging or spells.

---

## COMBAT INPUT CONTRACT (LOCKED)

Combat Core consumes a **validated snapshot**:

- Attacker Actor
- Target Actor
- Aggregated combat stats (from `PLAYER_COMBAT_STATS.md` or equivalent)
- SceneRuleFlags (from `ACTOR_MODEL.md`)

Combat Core must **not**:
- Decide whether the target is hostile
- Decide PvP legality
- Decide whether an actor *should* be attackable
- Decide whether a scene allows combat

Those decisions are resolved **before** Combat Core scheduling.

---

## ATTACK LEGALITY (MANDATORY PRE-CHECK)

Before scheduling or resolving any combat action, the server must validate:

- Scene: `CombatAllowed == true`
- Attacker Actor is alive and not `CombatState.Dead`
- Target Actor is alive and eligible
- `AttackLegalityResult == Allowed` (from Actor rules)

If legality fails:
- The combat attempt is cancelled
- No stamina, ammo, or durability is consumed
- No Hit/Miss/Damage events are emitted

Combat Core **never overrides** legality decisions.

---

## COMBAT LOOP MODEL (LOCKED)

Combat is **real-time**, resolved by **server-scheduled events**.

### The Server Owns
- Swing timers
- Hit/miss rolls
- Damage rolls
- Proc rolls (Hit Spells / Hit Leaches)
- Stamina and ammo consumption
- Durability loss
- Bandage timers and resolution
- Death trigger

### The Client Owns
- UI feedback
- Animations/VFX driven by replicated events
- Input submission only

---

## SWING TIMER (LOCKED)

### Base swing time source
- Weapon swing speed from `ItemDef`
- Unarmed baseline from `PLAYER_DEFINITION.md`

### Minimum swing time floor
- **1.0 seconds**

### Swing time modifiers
Final swing time is computed using:
- Dexterity bonus
- Swing-speed affixes
- Status effect multipliers

Aggregation policies are owned by `PLAYER_COMBAT_STATS.md`.

---

## STAMINA & AMMUNITION (LOCKED)

- Each swing consumes stamina
- Ranged weapons consume ammo per attempt
- If resources are insufficient, the swing does not occur

---

## HIT / MISS RESOLUTION (LOCKED ORDER)

When a swing timer completes:

0. **Scene re-gate (mandatory)**
   - If `CombatAllowed == false` → cancel resolution
   - If `DamageAllowed == false` → cancel damage creation

1. **Revalidate**
   - Attacker and target still alive
   - Target still eligible

2. **Consume resources**
   - Stamina
   - Ammo (if applicable)

3. **Roll Hit**

4. **On Miss**
   - Emit `Miss` event
   - End resolution

5. **On Hit**
   - Roll base weapon damage
   - Apply damage modifiers
   - Apply resist mitigation
   - Emit `DamagePacket`
   - Resolve on-hit procs
   - Resolve hit leaches
   - Apply durability loss *(only if `DurabilityLossAllowed == true`)*
   - Emit `Hit` event

6. **Death check (scene-gated)**
   - If `DeathAllowed == false` → do not kill; clamp at minimum HP (implementation policy)
   - If `DeathAllowed == true` and HP ≤ 0 → trigger death exactly once

---

## DURABILITY LOSS (SCENE-GATED LOCK)

Durability loss is only allowed if:
- `DurabilityLossAllowed == true`

If the scene disallows durability loss, Combat Core must:
- Skip durability reduction triggers
- Still allow non-damage actions only if scene allows them (usually not relevant in safe scenes)

---

## STATUS EFFECT INTEGRATION (LOCKED)

Combat Core respects action gates and modifiers surfaced via aggregated combat stats:

- If `canAttack == false` → swings cannot start or complete
- If `canBandage == false` → bandaging cannot start or complete

Combat Core never inspects individual status effects directly.

---

## AGGRESSION & COMBAT STATE HOOKS (LOCKED)

On any successful hostile action:
- Combat Core must notify the **CombatStateTracker**

**Scene lock:** This notification must never occur in safe scenes (because hostile actions cannot be legal).

Combat Core does **not**:
- Track aggression timers
- Decide when combat ends

---

## DEATH HANDOFF (LOCKED)

When an Actor reaches 0 HP (and `DeathAllowed == true`):

- Combat Core emits `OnActorKilled(attacker, target, context)`
- Combat Core never performs loot, insurance, or respawn logic

Those systems consume the event and apply their own rules.

---

## NETWORKING / REPLICATION (LOCKED)

Clients receive:
- Hit / Miss events
- Damage numbers (optional)
- Bandage events
- Updated vitals
- Death events

Clients never receive authority to resolve combat.

---

## REQUIRED IMPLEMENTATION ARTIFACTS (NEXT)

1. `CombatResolver` (server)
2. `AttackLoop` / SwingTimer scheduler (server)
3. `DamagePacket` / `HealPacket` models
4. `CombatStateTracker` (server)
5. Actor legality integration (`AttackLegalityResult`)
6. Scene rule provider integration (Combat Core must be handed `SceneRuleFlags`)

---

## DESIGN LOCK CONFIRMATION

This document is **authoritative**.

Any change must:
- Increment Version
- Update Last Updated
- Call out impacted dependent systems (Actor, Targeting, Status, Items, UI)

