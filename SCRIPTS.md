# SCRIPTS.md — Ultimate Dungeon (Current Script Inventory)

Version: 1.3  
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

Authoritative behavior and rules live in the design docs (PLAYER_*, COMBAT_*, etc.).

---

## DESIGN CONTEXT (LOCKED)

- **Unity 6 (URP)**
- **Netcode for GameObjects (NGO)**
- **Server-authoritative gameplay**
- **Ultima Online–style controls**
  - Right click: move
  - Right click hold: continuous steering
  - Left click: target/select
  - Double left click: attack or interact

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
**Purpose:** Planned bridge between gameplay actions and progression.

---

## NETWORK REPLICATION (UI ONLY)

### `PlayerVitalsNet`
**Purpose:** Replicates vitals to clients.

---

### `PlayerStatsNet`
**Purpose:** Replicates stats to clients.

---

### `PlayerSkillBookNet`
**Purpose:** Replicates skills to clients.

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

## TARGETING (LOCAL-ONLY)

### `PlayerTargeting`
**Purpose:** Local-only target state.

**Responsibilities:**
- Stores `CurrentTarget`
- Emits `OnTargetSet` / `OnTargetCleared` events

---

### `LeftClickTargetPicker_v3`
**Purpose:** UO-style target selection via raycast.

---

## INTERACTION (SERVER-AUTHORITATIVE)

### `IInteractable`
**Purpose:** Minimal server-authoritative interaction contract.

---

### `PlayerInteractor`
**Purpose:** Sends interaction intent to server.

**Notes:**
- No longer hard-wired to left click
- Intended to be triggered by explicit input (double click / key)

---

### `SimpleInteractable`
**Purpose:** Generic interactable component.

**Notes:**
- Transitional utility
- Should be used sparingly; gameplay-specific interactables preferred

---

## COMBAT CORE (SERVER-AUTHORITATIVE)

### `ICombatActor`
**Purpose:** Minimal combat-facing actor contract.

---

### `CombatActorFacade`
**Purpose:** Adapts runtime actors (player, enemy, dummy) to combat core.

**Notes:**
- Uses `ActorVitals` as combat HP source
- Logs warning if missing

---

### `ActorVitals`
**Purpose:** Combat-only HP container.

**Notes:**
- Used by all combat actors (including player)
- Separate from `PlayerVitals`

---

### `PlayerCombatController`
**Purpose:** Client → server bridge for attack intent.

---

### `AttackLoop`
**Purpose:** Server-side swing timer and attack loop.

---

### `CombatResolver`
**Purpose:** Resolves hit, damage, and death.

---

### `DoubleClickAttackInput`
**Purpose:** Detects double left click and requests attack.

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

---

### `TargetIndicatorFollower`
**Purpose:** Positions target ring under current target.

---

### `TargetRingPresenter`
**Purpose:** Changes target ring state based on targeting and combat.

---

### `TargetRingPulse`
**Purpose:** Visual pulse animation for target ring.

---

### `TargetRingFactionTint`
**Purpose:** Applies faction-based color tint to target ring.

---

## PREFABS (EXPECTED CURRENT STATE)

### Player Prefab (`PF_Player`)
Expected components (current):
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

---

## KNOWN INTENTIONAL GAPS

- No pathfinding
- No inventory/equipment UI
- No ranged combat
- No spellcasting
- Death visuals minimal

---

## NEXT CLEANUP TARGETS

- Remove legacy interactables
- Consolidate input listeners
- Unify PlayerVitals ↔ ActorVitals data flow
- Replace debug combat dummy with real enemy prefab

