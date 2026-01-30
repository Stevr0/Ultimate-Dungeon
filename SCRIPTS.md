# SCRIPTS.md — Ultimate Dungeon (Current Script Inventory)

Version: 1.5  
Last Updated: 2026-01-30

---

## PURPOSE

This document lists the **scripts currently present and in use** in *Ultimate Dungeon*.

This document is **NOT authoritative**.
It exists to:
- Track what scripts currently exist
- Describe their *current* responsibilities
- Help with prefab/scene wiring and cleanup
- Identify legacy or transitional systems

Authoritative behavior and rules live in the design docs:
- `ACTOR_MODEL.md`
- `TARGETING_MODEL.md`
- `COMBAT_CORE.md`
- `PLAYER_DEFINITION.md`
- `STATUS_EFFECT_CATALOG.md`

---

## DESIGN CONTEXT (LOCKED)

- Unity 6 (URP)
- Netcode for GameObjects (NGO)
- Server-authoritative gameplay
- Ultima Online–style controls
  - Right click: move
  - Right click hold: continuous steering
  - Left click: target/select
  - Double left click: attack or interact

---

## ACTOR / TARGETING LAYER (CURRENT)

These scripts now exist and form the **mandatory foundation** for combat and interaction.

### `ActorComponent`
**Purpose:** Canonical runtime identity for all actors.

**Responsibilities:**
- ActorType (Player, Monster, NPC, Vendor, Pet, Summon, Object)
- FactionId
- Alive / Dead state
- Scene-aware behavior hooks

**Notes:**
- Present on **all** actors
- Referenced by targeting, faction, combat, interaction systems

---

### `FactionService`
**Purpose:** Pure rules lookup for actor relationships.

**Responsibilities:**
- Evaluates Friendly / Neutral / Hostile relationships
- Uses data defined in `ACTOR_MODEL.md`

**Notes:**
- No Unity, no networking, no scene access

---

### `TargetingResolver`
**Purpose:** Pure rules engine for targeting and attack legality.

**Responsibilities:**
- Eligibility checks (alive, visibility, range)
- Disposition resolution (Self / Friendly / Neutral / Hostile / Invalid)
- Attack legality resolution

**Notes:**
- Deterministic and unit-testable
- Does not access Unity objects directly

---

### `ServerTargetValidator`
**Purpose:** Server-only validation bridge.

**Responsibilities:**
- Resolves NetworkObjectId → ActorComponent
- Gathers SceneRuleFlags
- Computes range / LoS (v1: range only)
- Delegates legality to `TargetingResolver`

---

### `CombatStateTracker`
**Purpose:** Tracks hostile engagement state.

**Responsibilities:**
- Records hostile actions started / received
- Drives CombatState transitions:
  - Idle
  - Engaged
  - InCombat
  - Dead

**Notes:**
- Server authoritative
- Not a damage system

---

## SCENE RULES

### `SceneRuleProvider`
**Purpose:** Declares authoritative scene behavior.

**Responsibilities:**
- Declares SceneRuleContext (Dungeon / Village / Mainland)
- Resolves SceneRuleFlags
- Registers flags into SceneRuleRegistry

---

### `SceneRuleRegistry`
**Purpose:** Global access point for active scene rules.

**Notes:**
- Server authoritative
- Readable by clients for UI hints

---

## NETWORKING / BOOTSTRAP

### `NetworkHudController`
**Purpose:** Temporary Host / Client UI.

---

### `PlayerNetIdentity`
**Purpose:** Player NGO identity and discovery.

**Responsibilities:**
- Detects player spawn
- Determines local ownership
- Emits LocalPlayerSpawned event

---

## PLAYER CORE / STATS / VITALS

### `PlayerCore`
**Purpose:** Server-side initialization root.

Initializes:
- PlayerStats
- PlayerVitals
- PlayerSkillBook

---

### `PlayerStats`
**Purpose:** STR / DEX / INT container.

---

### `PlayerVitals`
**Purpose:** Authoritative HP / Stamina / Mana.

---

### `PlayerSkillBook`
**Purpose:** Skill container + caps.

---

## MOVEMENT

### `ServerClickMoveMotor`
**Purpose:** Server-authoritative movement motor.

---

### `ClickToMoveInput_UO`
**Purpose:** Client-side UO-style movement input.

---

## TARGETING (LOCAL UX ONLY)

### `PlayerTargeting`
**Purpose:** Local target selection state.

**Notes:**
- UX only
- Does not imply legality

---

### `LeftClickTargetPicker_v3`
**Purpose:** Raycast-based target picker.

---

## COMBAT CORE

### `ICombatActor`
**Purpose:** Minimal combat-facing actor interface.

---

### `CombatActorFacade`
**Purpose:** Adapter between ActorComponent/Vitals and combat core.

---

### `ActorVitals`
**Purpose:** Combat-only HP container.

**Notes:**
- Currently separate from PlayerVitals

---

### `PlayerCombatController`
**Purpose:** Client → server attack intent bridge.

---

### `AttackLoop`
**Purpose:** Server-side swing timer and auto-attack loop.

---

### `CombatResolver`
**Purpose:** Server-only resolution of hits, damage, and death.

**Responsibilities:**
- Applies DamagePackets
- Triggers death once
- Despawns actors on death (v1 quick fix)

---

### `DoubleClickAttackInput`
**Purpose:** UX detection of attack intent.

---

## VISUAL FEEDBACK (LOCAL ONLY)

### `HitFlash`
**Purpose:** Simple material flash on hit.

---

### `DamageFeedbackReceiver`
**Purpose:** Floating damage numbers (e.g. “-1”).

---

### `MinimalDebugHud`
**Purpose:** Lightweight on-screen debug readout.

Displays:
- SceneRuleContext / Flags
- Targeting disposition
- CombatState

---

## PREFABS — EXPECTED STATE

### Player (`PF_Player`)
Required components:
- NetworkObject
- NetworkTransform
- CharacterController
- PlayerNetIdentity
- PlayerCore
- PlayerStats
- PlayerVitals
- ActorVitals
- PlayerSkillBook
- ServerClickMoveMotor
- ClickToMoveInput_UO
- PlayerTargeting
- LeftClickTargetPicker_v3
- PlayerCombatController
- CombatActorFacade
- AttackLoop
- ActorComponent
- CombatStateTracker

---

## KNOWN INTENTIONAL GAPS

- No corpse persistence (despawn-on-death v1)
- No AI retaliation
- No ranged combat
- No spellcasting
- No inventory/equipment UI

---

## NEXT CLEANUP TARGETS

1. Corpse vs Despawn policy
2. Combat disengage rules
3. AI aggression & retaliation
4. Unify PlayerVitals ↔ ActorVitals
5. Drive UI tinting from TargetingDisposition

