# SCRIPTS.md — Ultimate Dungeon (Current Script Inventory)

Version: 1.6  
Last Updated: 2026-01-31

---

## PURPOSE

This document lists the **scripts currently present and in active use** in *Ultimate Dungeon*.

This document is **NOT authoritative**.

It exists to:
- Track what scripts currently exist
- Describe their *current responsibilities*
- Help with prefab/scene wiring and cleanup
- Identify legacy, spike, or transitional systems

Authoritative behavior and rules live in the design documents:
- `ACTOR_MODEL.md`
- `TARGETING_MODEL.md`
- `COMBAT_CORE.md`
- `SCENE_RULE_PROVIDER.md`
- `PLAYER_DEFINITION.md`

---

## DESIGN CONTEXT (LOCKED)

- Unity 6 (URP)
- Netcode for GameObjects (NGO)
- Server-authoritative gameplay
- Ultima Online–style controls
  - Right click: move / chase
  - Left click: select target
  - Double left click: attack / interact

---

## BOOTSTRAP & NETWORKING

### `NetworkHudController`
**Purpose:** Temporary Host/Client UI for starting and stopping NGO sessions.

**Notes:**
- Debug-only
- Will be removed once proper frontend exists

---

## ACTOR CORE

### `ActorComponent`
**Purpose:**
- Server-authoritative Actor identity surface
- Holds ActorType, FactionId, Alive/Dead state, CombatState

**Notes:**
- Required on ALL actors (Players, Monsters, NPCs, Objects)
- CombatState is written ONLY by `CombatStateTracker`

---

### `CombatActorFacade`
**Purpose:**
- Unified interface adapter (`ICombatActor`) for Combat Core
- Exposes vitals, stats, transform, and legality gates

**Notes:**
- Consumed by CombatResolver and AttackLoop
- Does not own combat rules

---

## TARGETING & INTENT

### `PlayerTargeting`
**Purpose:**
- Holds the local player’s currently selected target
- Emits events for UI (target rings, frames, etc.)

**Notes:**
- Local-only
- Clearing target triggers RequestStopAttack

---

### `CombatEngageIntent`
**Purpose:**
- Local-only intent model for Ultima Online–style combat
- Tracks whether the player has explicitly armed an attack

**Notes:**
- Being out of range does NOT clear intent
- Intent clears only on explicit cancel or target invalidation

---

## PLAYER COMBAT CONTROL

### `PlayerCombatController`
**Purpose:**
- Server-authoritative attack request handler
- Validates attack intent using Targeting + Scene rules
- Starts and stops AttackLoop

**Notes:**
- Clients submit intent only
- Server commits exactly once

---

## COMBAT EXECUTION (SERVER)

### `AttackLoop`
**Purpose:**
- Server-owned auto-attack loop (swing scheduling)
- Repeats swings while attacker/target remain valid and in range

**Notes:**
- Range gates swings, NOT combat state
- Loop stopping does not end combat by itself

---

### `CombatResolver`
**Purpose:**
- Sole authority that resolves completed combat actions
- Applies DamagePackets and triggers death exactly once

**Notes:**
- Always server-only
- v0.1: always-hit, fixed damage

---

### `CombatStateTracker`
**Purpose:**
- Server-authoritative combat state surface
- Transitions Actor between Peaceful / InCombat / Dead

**Current Rules:**
- Any hostile action refreshes combat window
- Combat persists while AttackLoop is running OR window active
- Combat ends only after disengage timeout

**Notes:**
- Loop stopping is NOT a combat-extending event
- Scene rules may forbid entering combat

---

## MOVEMENT

### `ClickToMoveInput`
**Purpose:**
- Owner-only input collector for Ultima Online–style movement
- Sends destination intent to server motor

**Notes:**
- Right-click move does NOT cancel combat intent
- Chasing while InCombat is supported

---

### `ServerClickMoveMotor`
**Purpose:**
- Server-authoritative movement execution
- Receives destination requests from clients

**Notes:**
- Combat does not own movement

---

## CAMERA

### `IsometricCameraFollow` / `TopDownCameraFollow`
**Purpose:**
- Simple isometric / semi-top-down camera follow
- Uses fixed rotation (no swivel)

**Notes:**
- Runs on CameraRig, not MainCamera
- Implements `ICameraFollowTarget`

---

### `LocalCameraBinder`
**Purpose:**
- Binds the local player transform to the camera follow script
- Retries until local player exists (NGO-safe)

**Notes:**
- Owner-only
- Avoids hard references between camera and player prefabs

---

## VISUAL FEEDBACK

### `HitFlash`
**Purpose:**
- Simple hit flash feedback on damaged actors

---

### `DamageFeedbackReceiver`
**Purpose:**
- Displays floating damage numbers

---

## STATUS & VITALS

### `ActorVitals`
**Purpose:**
- **Single source of truth** for Actor vitals (Players + Monsters + NPCs)
- Holds replicated current/max values for HP, Stamina, Mana
- Server-authoritative mutation helpers

**Notes:**
- Server writes NetworkVariables; clients read
- Combat systems should only mutate via ActorVitals server methods

---

### `PlayerVitalsRegenServer`
**Purpose:**
- Server-only regeneration ticking for Players

**Notes:**
- Exists to keep `ActorVitals` generic

---

## PLAYER RUNTIME HUB

### `PlayerCore`
**Purpose:**
- Central runtime hub for player-only systems
- Holds references to Stats, SkillBook, Vitals

**Notes:**
- Initializes vitals from attributes
- Binds regen server-side

---

## UI BINDING

### `LocalPlayerUIBinder`
**Purpose:**
- Listens for local player spawn
- Binds UI panels to replicated components

---

## HUD

### `HudVitalsUI`
**Purpose:**
- Displays HP/Stamina/Mana bars and values

---

## HOTBAR (UI)

### `HotbarUI`
**Purpose:**
- 10-slot hotbar presentation

---

### `HotbarSlotUI`
**Purpose:**
- Visual hotbar slot

---

### `HotbarInputRouter`
**Purpose:**
- Routes keyboard input 1–0 to hotbar slots

---

## NOTES / CLEANUP

- Vitals consolidation complete
- Spike/test scripts should be clearly marked

---

## DESIGN CONFIRMATION

This document is **informational only**.

Design docs always win.

