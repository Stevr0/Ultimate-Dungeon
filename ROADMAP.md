# ROADMAP.md — Ultimate Dungeon

Version: 1.2  
Last Updated: 2026-01-29  
Engine: Unity 6 (URP)  
Networking: Netcode for GameObjects (NGO)  
Authority: Server-authoritative  
Data Model: ScriptableObjects-first  

---

## PURPOSE

Defines the **authoritative build order** for *Ultimate Dungeon*, aligned to locked systems and scene separation.

This roadmap prioritizes:
- A clean **first playable combat slice**
- Zero rewrites later due to camera, housing, or scene-rule mistakes
- Explicit separation between **danger scenes** and **safe scenes**

---

## DESIGN LOCKS (DO NOT BREAK)

1. Persistent multiplayer world
2. Server-authoritative rules
3. Classless, skill-based progression
4. Items + statuses drive power
5. Deterministic combat resolution
6. **SceneRuleContext gates all combat, damage, and progression**

---

## SCENE MODEL (LOCKED)

| Scene | Purpose | Combat | Camera |
|---|---|---:|---|
| Hotnow Village | Spawn / hub / services | ❌ | Top-down |
| Dungeon Scenes | Core gameplay | ✅ | Top-down (UO-style) |
| Mainland Housing | Housing / vendors / immersion | ❌ | 1st / 3rd person |

---

## PHASE 1 — MULTIPLAYER FOUNDATION (COMPLETE)

✅ NGO setup (Host / Client)
✅ Player spawning
✅ Server-authoritative movement (click-to-move)
✅ Status system bootstrap
✅ Actor model established

---

## PHASE 2 — CORE RULE SYSTEMS (COMPLETE)

✅ Actor Model (`ACTOR_MODEL.md`)
✅ SceneRuleContext + SceneRuleFlags
✅ Targeting Model (`TARGETING_MODEL.md`)
✅ Combat Core (`COMBAT_CORE.md`)

> With Phase 2 complete, **illegal combat is structurally impossible** in safe scenes.

---

## PHASE 3 — FIRST PLAYABLE COMBAT SLICE (CURRENT)

**Goal:** A small dungeon where players can fight, die, loot, and extract.

### Required Systems

- ⬜ AttackLoop / SwingTimer (server)
- ⬜ DamagePacket / HealPacket resolution
- ⬜ CombatStateTracker
- ⬜ Basic melee weapons
- ⬜ Monster Actor definitions
- ⬜ Death → corpse → loot flow
- ⬜ Respawn / extraction back to Hotnow Village

### Explicitly Out of Scope

- Housing
- Vendors
- Crafting
- Resource gathering
- First/third person camera

---

## PHASE 4 — DUNGEON EXPANSION & POLISH

- ⬜ Spellcasting pipeline
- ⬜ Status-driven spell effects
- ⬜ Ranged combat + ammo
- ⬜ PvP enablement (Actor legality)
- ⬜ Durability loss + repairs
- ⬜ Bandaging / healing

---

## PHASE 5 — MAINLAND HOUSING & ECONOMY (POST-COMBAT)

**This phase is intentionally deferred until combat is proven.**

### Mainland Scene Systems

- ⬜ Mainland world scene (large map, roads, plots)
- ⬜ SceneRuleContext = `MainlandHousing`
- ⬜ 1st / 3rd person camera controller
- ⬜ Housing deeds
- ⬜ Build envelope / placement rules
- ⬜ Decoration & construction system
- ⬜ Player vendors
- ⬜ Vendor UI + pricing

### Explicit Locks

- No combat
- No damage
- No skill gain
- No durability loss
- No resource gathering

---

## PHASE 6 — SOCIAL & LONG-TAIL SYSTEMS

- ⬜ Guilds
- ⬜ Party finder
- ⬜ Friends / ignore lists
- ⬜ Player housing permissions
- ⬜ Mail system

---

## PHASE 7 — LIVE WORLD EVOLUTION

- ⬜ Additional dungeons
- ⬜ Dungeon modifiers / themes
- ⬜ Seasonal content
- ⬜ World events (dungeon-only)

---

## GUIDING PRINCIPLE (LOCKED)

> **Danger lives only where clarity exists.**  
> **Comfort lives only where exploits cannot.**

Housing, immersion, and social systems must never weaken combat integrity.

---

## DESIGN LOCK CONFIRMATION

This roadmap is **authoritative**.

Any change must:
- Increment Version
- Update Last Updated
- Call out impacted phases or systems

