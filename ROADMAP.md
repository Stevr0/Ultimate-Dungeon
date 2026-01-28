# ROADMAP.md — Ultimate Dungeon

Version: 1.3  
Last Updated: 2026-01-28  
Engine: Unity 6 (URP)  
Networking: Netcode for GameObjects (NGO)  
Authority: Server-authoritative  
Data: ScriptableObjects-first

---

## Purpose

A **step-by-step, logical build order** for the first playable vertical slice of *Ultimate Dungeon*.

**Goal of the first slice:**
- Host can start the game
- Clients can join
- Players spawn in a small “Crater Village” test area
- Ultima Online–style click-to-move works
- Targeting + interaction + visual feedback exists
- Player stats / vitals / skills are visible and authoritative
- Progression laws are locked **before combat math**

---

## Design Locks (Do Not Break)

1. **Persistent multiplayer world**
2. **Server-authoritative rules**
3. **Classless / skill-based progression**
4. **Items + statuses drive power**
5. **Data externalized (ScriptableObjects + registries)**

---

## Phase 1 — Multiplayer Foundation (COMPLETE)

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
- Temporary lighting

**Planned:**
- Replace with Crater Village prototype scene

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
- Players spawn correctly
- Only local player accepts input

---

### Step 4 — Player Core Data Model (SO-first)  
**Status:** ✅ COMPLETED

Implemented:
- `PlayerDefinition` (ScriptableObject)
- `PlayerCore` (server initializer)
- `PlayerStats` (STR / DEX / INT)
- `PlayerVitals` (HP / Stam / Mana, 150 cap)
- `PlayerSkillBook` (all skills present at start)

Locked:
- Stat → Vital derivation
- Hard vital caps
- Skill cap (700) + manual redistribution

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
- `LocalCameraBinder`

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
**Status:** ✅ COMPLETED

Implemented:
- `TargetFrameUI`
- `TargetIndicatorFollower`
- Bounds-correct target ring placement
- `TargetRingPulse`
- `TargetRingFactionTint`

Acceptance met:
- Target ring appears only for local player
- Ring tracks target bounds correctly
- Faction-based tinting works

---

### Step 9 — Player UI (Stats / Vitals / Skills)  
**Status:** ✅ COMPLETED

Implemented:
- `PlayerStatsNet`
- `PlayerVitalsNet`
- `PlayerSkillBookNet`
- `HudVitalsUI`
- `CharacterStatsPanelUI`
- `LocalPlayerUIBinder`

Acceptance met:
- Server-authoritative values displayed
- UI auto-binds on local player spawn
- No gameplay logic in UI

---

## Phase 2 — Progression & Gameplay Laws

### Step 10 — Skill & Stat Gain System (LOCKED)
**Status:** ✅ COMPLETED

Implemented:
- `SkillGainSystem`
  - Use-based gains
  - Skill lock enforcement (+ / − / locked)
  - Total skill cap handling (700)
- `StatGainSystem`
  - UO-style +1 base stat gains
  - Deterministic RNG (caller-provided)
  - Explicit hooks for future stat caps
- `DeterministicRng`
- `StatId`

Validation:
- `InteractableSkillUseTester` confirms:
  - Skill gain works end-to-end
  - Stat gain works end-to-end
  - Vitals recompute once per stat change

**Design Outcome:**
- Progression laws are now locked
- Combat math can be built safely on top

---

## CURRENT PROJECT STATE (SUMMARY)

✅ Networking & spawning complete  
✅ Server-authoritative movement complete  
✅ UO-style targeting complete  
✅ Double-click interaction complete  
✅ Player stats / vitals / skills authoritative  
✅ Progression laws locked (skills + stats)

🚧 Combat not started  
🚧 Status effects not started  
🚧 Items / equipment not started

---

## Phase 3 — Combat & Survival Systems (NEXT)

### Step 11 — Combat Core (NEXT LOCK)

Planned:
- Auto-attack loop (swing timer)
- Hit / miss resolution
- Damage packets
- Death trigger

Design constraints:
- Must consume stamina
- Must integrate with skill + stat values
- Must NOT modify progression rules

---

### Step 12 — Status Effect System

Planned:
- Central status registry
- Timed + conditional effects
- Damage-over-time
- Buffs / debuffs

Design rule:
- Status effects modify combat and survival
- Status effects do NOT grant raw power alone

---

### Step 13 — Item Model + Equipment

Planned:
- Equipment slots
- Stat modifiers
- Durability
- Random properties

---

### Step 14 — Inventory, Loot, Death

Planned:
- Corpse on death
- Full loot
- Insurance system

---

## Immediate Next Task (Recommended)

👉 **Step 11 — Combat Core (Hit / Miss + Swing Timer)**

Reason:
- Progression laws are locked
- UI and replication already exist
- Combat can now be implemented without refactors

---

If you want next:
- A **Combat Core design doc**
- A **server-only combat prototype**
- Or a **Status Effect architecture draft**

Just say the word.

