# SCRIPTS.md — Ultimate Dungeon (Authoritative)

Version: 1.0  
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
- Emits log when local player spawns (OwnerClientId / NetId)
- Establishes a **local player reference** used by camera + UI binders

**Notes:**
- Considered foundational; many “local-only” systems bind through it.

---

## CAMERA

### `LocalCameraBinder`
**Purpose:** binds the camera rig to the local player when that player spawns.

**Responsibilities (current):**
- Listens for local player spawn
- Binds camera follow/look to the local player

---

## MOVEMENT (SERVER-AUTHORITATIVE)

### `ServerClickMoveMotor`
**Purpose:** server-authoritative CharacterController motor.

**Responsibilities:**
- Receives movement intents via ServerRpc
- Validates sender ownership (sender must be OwnerClientId)
- Moves the CharacterController toward the server-approved destination

**Notes:**
- Straight-line movement only (no pathfinding yet)
- Uses `NetworkTransform` (for now) to replicate server motion

---

### `ClickToMoveInput_UO`
**Purpose:** UO-style move input on the owning client.

**Responsibilities:**
- **Right click press** → send destination under cursor to server
- **Right click hold** → resend destination at interval (steering)
- **Right click release** → stop sending updates

**Notes:**
- Does not move locally; sends intent only.

---

## TARGETING

### `PlayerTargeting`
**Purpose:** local-only target state for the owning client.

**Responsibilities:**
- Stores `CurrentTarget` (GameObject)
- `SetTarget()` and `ClearTarget()`
- Logs target changes

---

### `LeftClickTargetPicker_v3`
**Purpose:** UO-style targeting input (left click).

**Responsibilities:**
- Casts a single ray from camera
- Chooses the **nearest** hit
  - If nearest hit is **Ground** → clear target
  - Else → set target to hit object (prefers parent NetworkObject)

**Notes:**
- Fixes “ground behind target steals click” issue.

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
**Purpose:** UO-style “use” via **double left click**.

**Responsibilities:**
- Detects double left click within window
- If current target has a NetworkObject → ServerRpc
- Server validates:
  - sender is owner of this player
  - target exists
  - target implements `IInteractable`
  - range check
- Calls `ServerInteract()` on the target

---

### `InteractableDummy`
**Purpose:** test interactable that logs on server when used.

**Responsibilities:**
- Implements `IInteractable`
- Logs server interaction including interactor name

---

## UI (VISUAL FEEDBACK)

### `TargetFrameUI`
**Purpose:** minimal Target Frame text.

**Responsibilities:**
- Binds to local `PlayerTargeting` (via `PlayerNetIdentity.Local`)
- Displays: `Target: <name>` or `Target: None`

---

### `TargetIndicatorFollower`
**Purpose:** spawns and positions a ring indicator under the current target.

**Responsibilities:**
- Spawns a ring prefab once
- When a target exists:
  - positions ring under the target (uses **Renderer/Collider bounds min Y**)
  - hides ring when target cleared

**Notes:**
- Correctly handles different pivot heights (cube vs player).

---

### `TargetRingPulse`
**Purpose:** subtle pulse animation for the ring.

**Responsibilities:**
- Uses `MaterialPropertyBlock` to animate color (no material instancing)
- Auto-fallbacks `_BaseColor` → `_Color` if needed

---

## PREFABS / SCENE OBJECTS (CURRENT EXPECTED WIRING)

### Player Prefab (`PF_Player`)
Expected components (relevant to this doc):
- `NetworkObject`
- `NetworkTransform`
- `CharacterController`
- `PlayerNetIdentity`
- `ServerClickMoveMotor`
- `ClickToMoveInput_UO`
- `PlayerTargeting`
- `LeftClickTargetPicker_v3`
- `PlayerInteractor`

---

### Target Indicator System (Scene)
- `TargetIndicatorFollower` (references `PF_TargetRing`)

### Target Ring Prefab (`PF_TargetRing`)
- `TargetRingPulse`

---

### Test Interactable (Scene)
- `NetworkObject`
- `InteractableDummy`

---

## KNOWN LIMITATIONS (INTENTIONAL)

- No pathfinding
- No combat
- No faction / friend-foe tinting yet (planned next)
- Target selection is local-only (not replicated)

---

## NEXT PLANNED SCRIPTS

- `FactionTag` (local-only friend/neutral/hostile labeling)
- `TargetRingFactionTint` (ring color based on faction)
- Hover highlight (cursor-over feedback)
- “Walk-to-interact” (double click triggers movement into range, then interact)

