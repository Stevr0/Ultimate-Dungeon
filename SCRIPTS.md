# SCRIPTS.md — Ultimate Dungeon (Authoritative)

Version: 1.1  
Last Updated: 2026-01-27

---

## PURPOSE

This document lists the **current scripts implemented so far** in *Ultimate Dungeon*.

It is intended to be:
- A **single source of truth** for what exists
- A quick onboarding reference (what each script does)
- A checklist for wiring and prefab/scene placement

---

## DESIGN CONTEXT (LOCKED)

- **Unity 6 (URP)**
- **Netcode for GameObjects (NGO)**
- **Server-authoritative gameplay**
- **Ultima Online-style controls**
  - **Right click**: move
  - **Right click hold**: continuously update destination under cursor
  - **Left click**: target/select
  - **Double left click**: interact/use

---

## NETWORKING / BOOTSTRAP

### `NetworkHudController`
**Purpose:** temporary Host/Client UI for starting/stopping sessions.

**Notes:**
- Logs: Host started / shutdown
- Will be replaced later by a real main menu flow.

---

### `PlayerNetIdentity`
**Purpose:** Player NGO identity + local player binding.

**Responsibilities (current):**
- Detects when a player NetworkObject spawns
- Establishes **local player ownership** (`IsOwner`)
- Emits `LocalPlayerSpawned` event used by camera and UI binders

**Notes:**
- Foundational system; many local-only systems bind through it

---

## PLAYER CORE / STATS / VITALS (SERVER-AUTHORITATIVE)

### `PlayerCore`
**Purpose:** central runtime hub for the player.

**Responsibilities:**
- Holds reference to `PlayerDefinition`
- Initializes player systems on the **server**:
  - `PlayerSkillBook`
  - `PlayerStats`
  - `PlayerVitals`
- Enables server-only systems (e.g. vitals regen)

---

### `PlayerStats`
**Purpose:** authoritative STR / DEX / INT container.

**Responsibilities:**
- Stores base attributes from `PlayerDefinition`
- Applies item and status modifiers
- Exposes effective STR / DEX / INT values

---

### `PlayerVitals`
**Purpose:** authoritative HP / Stamina / Mana system.

**Responsibilities:**
- Derives max vitals from STR / DEX / INT
- Enforces **150 hard cap** on all vitals
- Applies slow/classic regen (**server-only**)
- Handles damage and resource spending

---

### `PlayerSkillBook`
**Purpose:** authoritative container for all player skills.

**Responsibilities:**
- Initializes **all skills at character creation**
- Stores current value per skill
- Stores skill lock state (+ / − / locked)
- Enforces total skill cap rules (700)

---

## NETWORK REPLICATION (NGO GLUE)

### `PlayerVitalsNet`
**Purpose:** NGO replication for player vitals.

**Responsibilities:**
- Replicates current/max HP, Stamina, Mana via `NetworkVariable<int>`
- Server writes, everyone reads
- Provides normalized helpers for UI

---

### `PlayerStatsNet`
**Purpose:** NGO replication for player stats.

**Responsibilities:**
- Replicates base and effective STR / DEX / INT
- Server writes, everyone reads
- Used exclusively for UI display

---

### `PlayerSkillBookNet`
**Purpose:** NGO replication for player skills.

**Responsibilities:**
- Uses `NetworkList<SkillNetState>` to replicate skills
- Replicates:
  - SkillId
  - Value (float)
  - Lock state
- Server writes, everyone reads

---

## CAMERA

### `LocalCameraBinder`
**Purpose:** binds the camera rig to the local player when that player spawns.

**Responsibilities (current):**
- Listens for `LocalPlayerSpawned`
- Binds camera follow/look to the local player

---

## MOVEMENT (SERVER-AUTHORITATIVE)

### `ServerClickMoveMotor`
**Purpose:** server-authoritative CharacterController motor.

**Responsibilities:**
- Receives movement intents via ServerRpc
- Validates sender ownership
- Moves CharacterController toward server-approved destination

**Notes:**
- Straight-line movement only (no pathfinding yet)
- Uses `NetworkTransform` for replication (temporary)

---

### `ClickToMoveInput_UO`
**Purpose:** Ultima Online-style move input (client-side).

**Responsibilities:**
- Right click press → send destination to server
- Right click hold → resend destination (steering)
- Right click release → stop sending

**Notes:**
- Sends intent only; never moves locally

---

## TARGETING

### `PlayerTargeting`
**Purpose:** local-only target state for the owning client.

**Responsibilities:**
- Stores `CurrentTarget`
- Provides `SetTarget()` / `ClearTarget()`
- Logs target changes

---

### `LeftClickTargetPicker_v3`
**Purpose:** UO-style targeting input.

**Responsibilities:**
- Casts a ray from camera
- Selects nearest valid hit
- Clears target when ground is clicked

---

## INTERACTION (SERVER-AUTHORITATIVE)

### `IInteractable`
**Purpose:** minimal interface for server-validated interactions.

**Contract:**
- `DisplayName`
- `InteractRange`
- `NetworkObject`
- `ServerInteract(NetworkBehaviour interactor)`

---

### `PlayerInteractor`
**Purpose:** UO-style interaction via double left click.

**Responsibilities:**
- Detects double click
- Sends ServerRpc with current target
- Server validates ownership, target, and range
- Calls `ServerInteract()` on target

---

### `InteractableDummy`
**Purpose:** test interactable for validation.

**Responsibilities:**
- Implements `IInteractable`
- Logs server-side interaction

---

## UI (VISUAL FEEDBACK)

### `HudVitalsUI`
**Purpose:** HUD display for player vitals.

**Responsibilities:**
- Displays HP / Stamina / Mana bars
- Displays numeric Current / Max values
- Subscribes to `PlayerVitalsNet`

---

### `CharacterStatsPanelUI`
**Purpose:** character panel for player attributes.

**Responsibilities:**
- Displays effective STR / DEX / INT
- Optionally displays base stats
- Subscribes to `PlayerStatsNet`

---

### `LocalPlayerUIBinder`
**Purpose:** automatic UI binding for the local player.

**Responsibilities:**
- Listens for `LocalPlayerSpawned`
- Binds HUD and panels to replicated components
- Handles Character panel toggle key

**Notes:**
- Supports **Input System** and **Legacy Input**
- UI-only, no gameplay logic

---

### `TargetFrameUI`
**Purpose:** minimal target frame text.

**Responsibilities:**
- Displays current target name or None

---

### `TargetIndicatorFollower`
**Purpose:** spawns and positions target ring.

**Responsibilities:**
- Positions ring under current target
- Hides ring when no target

---

### `TargetRingPulse`
**Purpose:** subtle pulse animation for target ring.

**Responsibilities:**
- Animates ring color via `MaterialPropertyBlock`

---

## PREFABS / SCENE OBJECTS (CURRENT EXPECTED WIRING)

### Player Prefab (`PF_Player`)
Expected components:
- `NetworkObject`
- `NetworkTransform`
- `CharacterController`
- `PlayerNetIdentity`
- `PlayerCore`
- `PlayerStats`
- `PlayerVitals`
- `PlayerSkillBook`
- `PlayerStatsNet`
- `PlayerVitalsNet`
- `PlayerSkillBookNet`
- `ServerClickMoveMotor`
- `ClickToMoveInput_UO`
- `PlayerTargeting`
- `LeftClickTargetPicker_v3`
- `PlayerInteractor`

---

## KNOWN LIMITATIONS (INTENTIONAL)

- No pathfinding
- No combat
- No inventory/equipment UI yet
- Target selection is local-only

---

## NEXT PLANNED SCRIPTS

- `SkillGainSystem`
- `EquipmentComponent`
- `Death / Corpse / Insurance systems`
- `FactionTag` and faction-based target tinting

