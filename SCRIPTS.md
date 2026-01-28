# SCRIPTS.md — Ultimate Dungeon (Current Script Inventory)

Version: 1.4  
Last Updated: 2026-01-29

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

## NEW ACTOR/TARGETING REQUIREMENTS (AS OF 2026-01-29)

These are required for the current design direction and must exist as scripts (some may be new).

### Actor Layer (required)
- `ActorComponent` *(NEW — required)*
  - Exposes Actor identity: ActorType, FactionId, CombatState, alive/dead
  - Present on Player, Monster, NPC, Summon, Pet

- `FactionService` *(NEW — pure rules)*
  - Relationship matrix lookup from `ACTOR_MODEL.md`

- `TargetingResolver` *(NEW — pure rules)*
  - Eligibility + Disposition + AttackLegality evaluation
  - Must not reference UI or Unity objects directly

- `ServerTargetValidator` *(NEW — server utility)*
  - Resolves NetworkObjectId → Actor
  - Calls `TargetingResolver` for action-time validation

- `CombatStateTracker` *(NEW — server system)*
  - Tracks aggression events
  - Drives CombatState transitions (Idle/Engaged/InCombat/Dead)

> Note: Combat Core must call CombatStateTracker on hostile actions.

---

## NETWORKING / BOOTSTRAP

### `NetworkHudController`
**Purpose:** Temporary Host/Client UI.

---

### `PlayerNetIdentity`
**Purpose:** Player NGO identity + local-player discovery.

**Responsibilities:**
- Detects when the player NetworkObject spawns
- Determines local ownership (`IsOwner`)
- Emits `LocalPlayerSpawned` event for camera/UI binding

---

## PLAYER CORE / STATS / VITALS (SERVER-AUTHORITATIVE)

### `PlayerCore`
**Purpose:** Central server-side initialization hub.

Initializes:
- `PlayerSkillBook`
- `PlayerStats`
- `PlayerVitals`

---

### `PlayerStats`
**Purpose:** Authoritative STR / DEX / INT container.

---

### `PlayerVitals`
**Purpose:** Authoritative player HP / Stamina / Mana system.

---

### `PlayerSkillBook`
**Purpose:** Authoritative skill container.

---

## PROGRESSION

### `SkillGainSystem`
**Purpose:** Skill gain + cap enforcement.

---

### `StatGainSystem`
**Purpose:** Base stat gain logic.

---

### `DeterministicRng`
**Purpose:** Deterministic server-side RNG helper.

---

### `StatId`
**Purpose:** Enum for primary attributes (STR / DEX / INT).

---

### `SkillUseResolver` *(Scaffolding)*
**Purpose:** Bridge between gameplay actions and progression.

---

## CAMERA

### `LocalCameraBinder`
**Purpose:** Binds camera to local player.

---

## MOVEMENT (SERVER-AUTHORITATIVE)

### `ServerClickMoveMotor`
**Purpose:** Server-authoritative movement motor.

---

### `ClickToMoveInput_UO`
**Purpose:** UO-style client movement input.

---

## TARGETING (LOCAL-ONLY SELECTION)

### `PlayerTargeting`
**Purpose:** Local-only target state.

**Responsibilities:**
- Stores selected target (recommended: NetworkObjectId)
- Emits `OnTargetSet` / `OnTargetCleared` events

**Notes:**
- This is UX-only. It does not imply legality.
- Server validation must occur on action attempts (`ServerTargetValidator`).

---

### `LeftClickTargetPicker_v3`
**Purpose:** UO-style target selection via raycast.

**Notes (alignment):**
- Should select only objects that expose `ActorComponent` (recommended filter)
- Must clear target on empty click

---

## INTERACTION (SERVER-AUTHORITATIVE)

### `IInteractable`
**Purpose:** Minimal server-authoritative interaction contract.

---

### `PlayerInteractor`
**Purpose:** Sends interaction intent to server.

**Notes:**
- Intended to be triggered by explicit input (double click / key)
- Must validate target actor via server-side validation path before executing

---

### `SimpleInteractable`
**Purpose:** Generic interactable component.

**Notes:**
- Transitional utility
- Prefer gameplay-specific interactables

---

## COMBAT CORE (SERVER-AUTHORITATIVE)

### `ICombatActor`
**Purpose:** Minimal combat-facing actor contract.

**Notes (alignment):**
- Should be implementable by any Actor that can fight
- Must not assume Player-only fields

---

### `CombatActorFacade`
**Purpose:** Adapts runtime actors (player, enemy, dummy) to combat core.

**Notes (alignment):**
- Should read identity from `ActorComponent`
- Should expose vitals from `ActorVitals` (or unified vitals source)

---

### `ActorVitals`
**Purpose:** Combat-only HP container.

**Notes:**
- Used by all combat actors
- Currently separate from `PlayerVitals`
- Planned cleanup: unify or establish an authoritative mapping

---

### `PlayerCombatController`
**Purpose:** Client → server bridge for attack intent.

**Notes (alignment):**
- Must pass selected target as stable ID
- Server must validate target via `ServerTargetValidator` before starting attacks

---

### `AttackLoop`
**Purpose:** Server-side swing timer and attack loop.

**Notes (alignment):**
- Must pre-check legality before starting
- Must revalidate legality on swing completion

---

### `CombatResolver`
**Purpose:** Resolves hit, damage, and death.

**Notes (alignment):**
- Executes combat only after legality pre-check
- Must notify `CombatStateTracker` on hostile actions

---

### `DoubleClickAttackInput`
**Purpose:** Detects double left click and requests attack.

**Notes (alignment):**
- UX-only; does not imply legality

---

## UI / VISUAL FEEDBACK (LOCAL-ONLY)

### `HudVitalsUI`
**Purpose:** Displays HP / Stamina / Mana.

---

### `CharacterStatsPanelUI`
**Purpose:** Displays STR / DEX / INT.

---

### `LocalPlayerUIBinder`
**Purpose:** Binds UI to local player.

---

### `TargetFrameUI`
**Purpose:** Displays current target name.

**Notes (alignment):**
- Should display disposition indicator (friendly/neutral/hostile) once available

---

### `TargetIndicatorFollower`
**Purpose:** Positions target ring under current target.

---

### `TargetRingPresenter`
**Purpose:** Changes target ring state based on targeting and combat.

**Notes (alignment):**
- Must consume:
  - Local selection events (instant)
  - Server-replicated disposition/combat state (authoritative)

---

### `TargetRingPulse`
**Purpose:** Visual pulse animation for target ring.

**Notes (alignment):**
- Should pulse when target or viewer is `CombatState.InCombat` (as desired)

---

### `TargetRingFactionTint`
**Purpose:** Applies faction-based color tint to target ring.

**Notes (alignment):**
- Must tint from `TargetingDisposition` (Self/Friendly/Neutral/Hostile/Invalid)
- Must not hardcode “enemy vs neutral” ad-hoc rules

---

## PREFABS (EXPECTED CURRENT STATE)

### Player Prefab (`PF_Player`)
Expected components (current + required alignment):
- `NetworkObject`
- `NetworkTransform`
- `CharacterController`
- `PlayerNetIdentity`
- `PlayerCore`
- `PlayerStats`
- `PlayerVitals`
- `ActorVitals`
- `PlayerSkillBook`
- `PlayerStatsNet`
- `PlayerVitalsNet`
- `PlayerSkillBookNet`
- `ServerClickMoveMotor`
- `ClickToMoveInput_UO`
- `PlayerTargeting`
- `LeftClickTargetPicker_v3`
- `PlayerInteractor`
- `PlayerCombatController`
- `CombatActorFacade`
- `AttackLoop`
- **`ActorComponent` (NEW required)**

---

## KNOWN INTENTIONAL GAPS

- No pathfinding
- No inventory/equipment UI
- No ranged combat
- No spellcasting pipeline
- Death visuals minimal
- Actor layer scripts (listed above) may not exist yet and must be implemented

---

## NEXT CLEANUP TARGETS

1. Implement Actor layer scripts:
   - `ActorComponent`
   - `FactionService`
   - `TargetingResolver`
   - `ServerTargetValidator`
   - `CombatStateTracker`

2. Unify `PlayerVitals` ↔ `ActorVitals` data flow

3. Update targeting UI:
   - Drive ring tint from `TargetingDisposition`
   - Drive ring pulse from `CombatState`

4. Replace debug combat dummy with real enemy prefab implementing `ActorComponent`

