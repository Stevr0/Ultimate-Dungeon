# ULTIMATE_DUNGEON_BOOTSTRAP_PLAN.md — Ultimate Dungeon

Version: 1.0  
Last Updated: 2026-01-27  
Engine: Unity 6 (URP)  
Networking: Netcode for GameObjects (NGO)  
Authority: Server-authoritative  
Data: ScriptableObjects-first  

---

## Purpose

A **step-by-step, logical build order** for the first playable vertical slice of *Ultimate Dungeon*.

Goal of the first slice:
- Host can start the game
- Clients can join
- Players spawn in a small “Crater Village” test area
- Click-to-move works (server-validated)
- Basic interaction + UI exists (targeting, hotbar placeholder)
- You can add combat later without rewriting foundations

---

## Design Locks (Do Not Break)

1. **Persistent multiplayer world** (no “single player first” shortcuts that rewrite later)
2. **Server-authoritative rules** (clients request, server validates, server commits)
3. **Classless / skill-based** (no levels / XP bars)
4. **Items + statuses drive power** (not character level scaling)
5. **Data is externalized** (ScriptableObjects + registries)

---

## What to do first (in order)

### Step 0 — Repo + Project Hygiene (1 time)
**Output:** clean Unity project ready for multiplayer iteration.

- Create Git repo (GitHub) + `.gitignore` for Unity
- Define folder structure:
  - `Assets/_Project/` (everything you own)
  - `Assets/_Project/Scripts/`
  - `Assets/_Project/ScriptableObjects/`
  - `Assets/_Project/Prefabs/`
  - `Assets/_Project/Scenes/`
- Decide naming conventions (PascalCase, no spaces)
- Enable **Enter Play Mode Options** (optional) only when stable

**Why first:** avoids asset chaos and merge pain once you add networking + prefabs.

---

### Step 1 — Install & Configure Core Packages
**Output:** stable baseline packages.

- URP (already chosen) + URP pipeline asset
- Netcode for GameObjects
- Unity Transport
- (Optional but recommended) Input System
- (Optional) Cinemachine (top-down camera)

**Why now:** movement + spawning must be built around NGO constraints.

---

### Step 2 — Build the “Test World” Scene
**Output:** one simple scene that never breaks.

Create scene: `SCN_Village_Test` with:
- Flat ground + a few obstacles
- Lighting + basic post-processing (keep it simple)
- A few spawn points

**Design note:** this scene is your “laboratory”. Don’t build the real village yet.

---

### Step 3 — Networking Bootstrap (Host/Client + Spawn)
**Output:** you can run Host and connect Clients reliably.

Implement:
- `NetworkBootstrapper` (singleton-ish scene object)
  - Creates/holds `NetworkManager`
  - Sets up `UnityTransport`
  - Simple UI buttons: Host / Client / Shutdown
- `PlayerPrefab` registered in `NetworkManager`
- Connection flow:
  - Host starts
  - Client connects
  - Server spawns player

**Acceptance test:**
- Host sees their player
- Client sees both players
- Players are owned correctly (only local player accepts input)

---

### Step 4 — Player “Core” Data Model (SO-first)
**Output:** every system can find the player’s authoritative data.

Create ScriptableObjects:
- `PlayerArchetypeDef` (starting defaults; not classes)
  - baseline attributes (STR/DEX/INT etc.)
  - baseline vitals (HP/Stam/Mana)
  - baseline movement tuning

Runtime components:
- `PlayerCore` (MonoBehaviour on player prefab)
  - reference to `PlayerArchetypeDef`
  - runtime state containers (vitals, flags)

**Why before movement:** movement decisions often need speed, encumbrance, statuses.

---

### Step 5 — CharacterController-Based Movement (Server-Validated)
**Output:** click-to-move works in multiplayer without rubber-banding hacks.

**Recommended choice:** `CharacterController` (not Rigidbody) because:
- deterministic-ish capsule sweep
- stable against slopes/steps
- easy to predict + reconcile

Architecture:
- Client:
  - reads input (click to move)
  - sends **MoveRequest** to server (desired destination / direction)
- Server:
  - validates move legality (speed caps, blocked, dead, stunned, etc.)
  - moves the CharacterController
  - replicates position (NetworkTransform or custom)

**Acceptance test:**
- Both players can click-to-move
- Remote players are smooth
- Client cannot move if server denies

---

### Step 6 — Camera + Cursor + Input Gate
**Output:** top-down camera follows local player and UI can safely block gameplay input.

Implement:
- `LocalPlayerBinder` (binds camera/UI to local owned player)
- `TopDownCameraRig`
- `CursorStack` (manages lock state + visibility)
- `UIInputGate` (global “if any modal UI open, block gameplay input”)

**Acceptance test:**
- Opening inventory (later) blocks movement
- Closing inventory restores movement

---

### Step 7 — Interaction Skeleton
**Output:** a consistent way to click things (NPCs, doors, loot later).

Implement:
- `IInteractable` interface
  - `GetInteractionName()`
  - `CanInteract(PlayerCore)`
  - `ServerInteract(PlayerCore)`
- `InteractionRaycaster` (from camera to world)
- `Targeting` (what you’re currently pointing at / selected)

**Acceptance test:**
- You can click a dummy object and see “Selected: X” on UI
- Interact sends request to server and server prints log

---

### Step 8 — Minimal UI Framework (Hotbar placeholder)
**Output:** UI foundation without “final art”.

- `HudRoot`
- `Hotbar` (slots are placeholders)
- `TargetFrame` (shows selected entity name)
- `Vitals` (HP/Stam/Mana placeholders)

**Acceptance test:**
- UI binds to local player on spawn
- UI updates when you swap target

---

## End of Phase 1 Milestone

You should now have a **multiplayer-ready shell** where:
- networking works
- spawning works
- click-to-move works (server-authoritative)
- camera & UI binding is correct
- interactions have a stable contract

This is the foundation you build combat, skills, items, crafting, and dungeon systems on.

---

## What comes next (Phase 2 preview)

1. **Combat core** (targeting + swing timers + hit resolution)
2. **Status system** (buffs/debuffs, DoTs, stuns) — drives survival pressure
3. **Item model** (base item defs + random affixes)
4. **Inventory + loot** (server-authoritative containers)
5. **Skills** (use-based gain, no XP bar)
6. **World persistence** (save/load world + player state)

---

## Immediate “Day 1” Checklist

- [ ] Create `SCN_Village_Test`
- [ ] Add `NetworkManager` + Transport
- [ ] Register Player prefab
- [ ] Host/Client buttons working
- [ ] Player spawns correctly and ownership is correct

If you do only one thing today: **get Step 3 passing** (host + client + spawn).

