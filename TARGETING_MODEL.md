# TARGETING_MODEL.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 1.1  
Last Updated: 2026-01-29  
Engine: Unity 6 (URP)  
Networking: Netcode for GameObjects (NGO)  
Authority: Server-authoritative  

---

## PURPOSE

Defines the authoritative **targeting and interaction intent model** for *Ultimate Dungeon*.

Targeting is not “combat”.
Targeting is a **server-validated intent** that may result in:
- Selection (UI focus)
- Interaction (use/open/talk)
- Attack scheduling (if legal)
- Spellcasting (beneficial or harmful)

Targeting must be aligned with:
- `ACTOR_MODEL.md` (Actor identity + legality + `SceneRuleContext`)
- `COMBAT_CORE.md` (execution pipeline; never decides legality)
- `MAGIC_AND_SPELLS.md` (casting rules + targeting types)

If a targeting rule is not defined here, **it does not exist**.

---

## SCOPE BOUNDARIES (NO OVERLAP)

### This document owns
- Target intent definitions and contracts
- Server validation order for target intents
- Scene-based gating (safe scenes vs dungeon)
- Target selection state rules (what the player is “locked onto”)
- Interaction distance/LoS requirements (high-level; exact numbers are implementation constants)
- Required network events for client UI

### This document does NOT own
- Faction/hostility decisions *(see `ACTOR_MODEL.md`)*
- Hit/miss/damage math *(see `COMBAT_CORE.md`)*
- Spell success, cast times, interruption *(see `MAGIC_AND_SPELLS.md`)*
- Individual status definitions *(see `STATUS_EFFECT_CATALOG.md`)*

---

## DESIGN LOCKS (MUST ENFORCE)

1. **Clients submit intent, server validates**
   - Clients never decide a target is legal.

2. **SceneRuleContext is a hard gate**
   - Safe scenes must refuse all hostile intents.

3. **Targeting is separate from execution**
   - Targeting chooses “who/what”; execution systems decide “what happens next” only after legality.

4. **No hidden auto-hostility**
   - A target cannot become hostile “because you clicked it”.
   - Hostility is derived from Actor rules.

---

## DEPENDENCY: SCENE RULE CONTEXT (AUTHORITATIVE)

Targeting consumes `SceneRuleContext` and `SceneRuleFlags` from `ACTOR_MODEL.md`.

### Required flags
- `CombatAllowed`
- `DamageAllowed`
- `HostileActorsAllowed`
- `PvPAllowed`

**Design lock:** If `CombatAllowed == false`, the server must refuse:
- `Attack`
- `CastHarmful`
- Any intent that would schedule combat execution

---

## TARGET INTENT TYPES (LOCKED)

Targeting is expressed as a **TargetIntent** submitted to the server.

### TargetIntentType
- `Select` *(UI focus only)*
- `Interact` *(use/open/talk/loot)*
- `Attack` *(hostile)*
- `CastBeneficial`
- `CastHarmful`

> NOTE: “Double-click to interact” is a client UX pattern; the server still receives `Interact`.

---

## TARGET SOURCES (LOCKED)

A target may be specified by exactly one source:

- **Entity Target**: a networked Actor (by `NetworkObjectId`)
- **Ground Target**: a world position (Vector3)

Targeting mode is determined by the action being requested:
- Spells declare a `TargetingType` in `SpellDef` *(see `MAGIC_AND_SPELLS.md`)*
- Interactions declare their own target requirements (entity vs ground)

---

## TARGETING VALIDATION ORDER (LOCKED)

When the server receives a `TargetIntent`, it validates in this order:

1. **Resolve SceneRuleContext**
   - Load `SceneRuleFlags` for the current scene.

2. **Resolve Actor references**
   - Attempter (player Actor)
   - Target Actor (if entity target)

3. **Basic Actor validity**
   - Attempter exists and is not dead
   - Target exists (if required) and is not dead (for attack/cast harmful)

4. **Scene gates**
   - If `CombatAllowed == false`:
     - refuse `Attack`
     - refuse `CastHarmful`

5. **Intent-specific legality**
   - `Select`: always allowed if target exists
   - `Interact`: must pass interaction masks and range/LoS
   - `Attack`: must pass `AttackLegalityResolver` (Actor rules)
   - `CastBeneficial`: must pass spell targeting rules (self/ally rules) and range/LoS
   - `CastHarmful`: must pass Actor legality + spell targeting rules

6. **Commit**
   - If allowed, set selection/interaction state and/or dispatch to the execution system.

---

## SAFE SCENES RULES (LOCKED)

Safe scenes are:
- `MainlandHousing`
- `HotnowVillage`

### Allowed intents in safe scenes
- `Select`
- `Interact`
- `CastBeneficial` *(optional; allowed only if it does not apply harm/damage and does not flag combat)*

### Refused intents in safe scenes
- `Attack` → `Denied_SceneDisallowsCombat`
- `CastHarmful` → `Denied_SceneDisallowsCombat`

**Design lock:** In safe scenes, Actors may not enter `CombatState.InCombat`.

---

## DUNGEON RULES (LOCKED)

In `Dungeon` scenes, all intent types may be allowed, subject to Actor legality.

### Key lock
- Targeting does not decide hostility.
- `Attack` is only allowed if `AttackLegalityResult == Allowed` (Actor rules).

---

## INTERACTION MODEL (LOCKED)

### Interaction categories
- `Use` *(levers, doors, shrines)*
- `Talk` *(NPC dialog)*
- `Loot` *(corpse/chest)*
- `Trade` *(vendor UI)*
- `Build` *(housing placement / deco in Mainland only)*

**Scene constraints**
- `Build` is only legal in `MainlandHousing`.
- `Trade` is legal in `MainlandHousing` and `HotnowVillage`.

> Exact systems (housing permissions, vendor rules) are owned by their own docs.

---

## RANGE / LINE OF SIGHT (LOCKED POLICY)

Targeting must validate:
- **Range**: within action range
- **Line of Sight**: unobstructed where required

Policy:
- `Select`: no range requirement
- `Interact`: must be within interaction range; LoS required unless explicitly overridden by an object
- `Attack`: must be within weapon range and LoS
- `CastBeneficial/Harmful`: must be within spell range and LoS unless the spell explicitly ignores LoS

---

## SELECTION STATE (LOCKED)

The server maintains an authoritative **CurrentSelection** per player.

Rules:
- `Select` updates CurrentSelection
- `Interact` may update CurrentSelection to the interactable target
- `Attack` updates CurrentSelection to the target if legal
- Scene changes must clear selection

> The client may display a local highlight, but server selection is the source of truth.

---

## REQUIRED NETWORK EVENTS (LOCKED)

Clients must receive server events so UI can reflect truth:

- `SelectionChanged(attacker, newTarget)`
- `TargetIntentDenied(attacker, intentType, denyReason)`
- `InteractionStarted(attacker, target, interactionType)`
- `InteractionFailed(attacker, target, reason)`

Combat-related scheduling is emitted by Combat Core / spellcasting systems:
- `AttackScheduled`
- `CastStarted`
- `CastInterrupted`

---

## DENY REASONS (LOCKED)

Targeting uses the same deny reason vocabulary as Actor legality, plus interaction-specific reasons.

Core deny reasons:
- `Denied_SceneDisallowsCombat`
- `Denied_SceneDisallowsDamage`
- `Denied_TargetNotHostile`
- `Denied_TargetNotAttackable`
- `Denied_AttackerDead`
- `Denied_TargetDead`
- `Denied_PvPNotAllowed`
- `Denied_RangeOrLoS`
- `Denied_StatusGated`

Interaction-specific deny reasons:
- `Denied_NotInteractable`
- `Denied_WrongScene`
- `Denied_Permission`

---

## REQUIRED IMPLEMENTATION ARTIFACTS (NEXT)

1. `TargetIntent` message model (network)
2. `TargetIntentValidator` (server; pure rules)
3. `InteractionResolver` (server; pure rules)
4. `SelectionState` (server authoritative)
5. Scene transition hook: clears selection on scene change

---

## DESIGN LOCK CONFIRMATION

This document is **authoritative**.

Any change must:
- Increment Version
- Update Last Updated
- Call out impacted dependent systems (Combat Core, Spells, UI, Housing)

