# ROADMAP.md — Ultimate Dungeon

Version: 1.1  
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
- Ultima Online–style click-to-move works
- Targeting + interaction + visual feedback exists
- Combat can be added without rewriting foundations

---

## Design Locks (Do Not Break)

1. **Persistent multiplayer world**
2. **Server-authoritative rules**
3. **Classless / skill-based progression**
4. **Items + statuses drive power**
5. **Data externalized (ScriptableObjects + registries)**

---

## Phase 1 — Multiplayer Foundation (CURRENT)

### Step 0 — Repo + Project Hygiene  
**Status:** ✅ COMPLETED

- Git repo created
- Unity-safe folder structure established
- Naming conventions locked

---

### Step 1 — Core Packages  
**Status:** ✅ COMPLETED

- URP
- Netcode for GameObjects
- Unity Transport
- Input System

---

### Step 2 — Test World Scene  
**Status:** ⚠️ PARTIAL

- Simple test scene in use
- Flat ground + test objects
- Will later be replaced by Crater Village prototype

---

### Step 3 — Networking Bootstrap (Host / Client / Spawn)  
**Status:** ✅ COMPLETED

Implemented:
- `NetworkHudController`
- NGO + Transport configured
- Player prefab registered
- Ownership validated

Acceptance met:
- Host + client connect
- Both players visible
- Only local player accepts input

---

### Step 4 — Player Core Data Model (SO-first)  
**Status:** ⏳ NOT STARTED

Planned:
- `PlayerArchetypeDef`
- `PlayerCore`
- Baseline stats + vitals container

---

### Step 5 — Server-Authoritative Movement (UO Style)  
**Status:** ✅ COMPLETED

Implemented:
- `ServerClickMoveMotor` (CharacterController)
- `ClickToMoveInput_UO`
  - Right click = move
  - Hold right click = steer
- Server ownership validation

Acceptance met:
- Smooth multiplayer movement
- No client-side authority

---

### Step 6 — Camera + Input Binding  
**Status:** ⚠️ PARTIAL

Completed:
- `LocalCameraBinder` (camera follows local player)

Remaining:
- CursorStack
- UIInputGate
- Camera polish (zoom, clamp)

---

### Step 7 — Targeting & Interaction Skeleton  
**Status:** ✅ COMPLETED

Implemented:
- `PlayerTargeting`
- `LeftClickTargetPicker_v3`
- `IInteractable`
- `PlayerInteractor` (double left click)
- `InteractableDummy`

Acceptance met:
- Left click selects / clears target
- Double left click interacts
- Server validates ownership + range

---

### Step 8 — Visual Feedback (Targeting UI)  
**Status:** ✅ COMPLETED (Targeting subset)

Implemented:
- `TargetFrameUI`
- `TargetIndicatorFollower`
- Bounds-correct target ring placement
- `TargetRingPulse` (visual feedback)

Remaining:
- Hotbar placeholder
- Vitals placeholder (HP / Stam / Mana)

---

## CURRENT PROJECT STATE (SUMMARY)

✅ Networking & spawning complete  
✅ Server-authoritative movement complete  
✅ UO-style targeting complete  
✅ Double-click interaction complete  
✅ Visual target feedback complete  

🚧 Player data model (SO-first) pending  
🚧 Hotbar & vitals UI pending  
🚧 Combat not started  

---

## Phase 2 — Gameplay Systems (NEXT)

1. **Player Core + Stats (SO-first)**
2. **Combat Core**
   - Auto-attack loop
   - Hit / miss math
   - Damage packets
3. **Status Effect System**
4. **Item Model + Affixes**
5. **Inventory & Loot Containers**
6. **Use-based Skill Progression**

---

## Immediate Next Task (Recommended)

👉 **Step 4 — Player Core Data Model**

Reason:
- Every upcoming system (combat, status, inventory, encumbrance)
  depends on it
- Locks numeric authority early
- Avoids refactors later

---

If you want, next I can:
- Break **Step 4** into a mini-checklist  
- Or write `PLAYER_CORE.md` + starter ScriptableObjects  
- Or continue UI (Hotbar / Vitals) first

Just say the word.
