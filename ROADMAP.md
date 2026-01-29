# ROADMAP.md — Ultimate Dungeon

Version: 1.4  
Last Updated: 2026-01-29  
Engine: Unity 6 (URP)  
Networking: Netcode for GameObjects (NGO)  
Authority: Server-authoritative  
Data: ScriptableObjects-first

---

## PURPOSE

Defines the **authoritative build roadmap** for *Ultimate Dungeon*, including near-term implementation steps **and long-term vision features**.

This roadmap is intentionally staged so that:
- Core combat and survival systems are locked first
- World-building, housing, and player expression systems are layered **after** stability
- No future feature undermines server authority, risk, or persistence

---

## DESIGN LOCKS (DO NOT BREAK)

1. Persistent multiplayer world
2. Server-authoritative gameplay
3. Classless, skill-based progression
4. Items + statuses drive power
5. Risk, loss, and permanence matter
6. No late-stage features may bypass combat or progression laws

---

## PHASE 1 — MULTIPLAYER FOUNDATION (COMPLETE)

Status: ✅ COMPLETE

- Networking bootstrap (Host / Client)
- Player spawning and ownership
- Server-authoritative movement (UO click-to-move)
- Targeting and interaction
- Player stats, vitals, skills
- Progression laws locked

---

## PHASE 2 — CORE GAMEPLAY LAWS (COMPLETE)

Status: ✅ COMPLETE

- Actor Model (PvE / PvP / factions / combat state)
- Skill system and caps
- Currency rules (Held vs Banked Coins)
- Item system laws (ItemDef / ItemInstance)
- Status Effect catalog
- Spell schema

> With Phase 2 complete, systems can be built without refactors.

---

## PHASE 3 — COMBAT & SURVIVAL (CURRENT FOCUS)

Status: 🚧 IN PROGRESS

### Step 11 — Combat Core (Immediate)

- Server-side swing timer
- Hit / miss resolution
- DamagePacket pipeline
- Death trigger
- CombatStateTracker integration

Acceptance:
- Players can fight monsters
- PvE loop feels correct
- Combat state transitions replicate

---

### Step 12 — Status Effects Runtime

- StatusEffectSystem implementation
- DoT ticking
- Action gating (stun, paralyze, silence, root)
- Invisibility / reveal integration

Acceptance:
- Status effects meaningfully alter combat
- No combat action bypasses status gates

---

### Step 13 — Items, Equipment & Loot

- Equipment slots and handedness
- Combat stat aggregation
- Durability loss
- Corpse + loot containers
- Insurance rules

Acceptance:
- Death has meaningful item loss
- Gear matters more than base stats

---

## PHASE 4 — WORLD DEPTH & PLAYER AGENCY (VISION STAGE)

> **This phase represents your longer-term Ultima Online–style vision.**
> These systems are intentionally delayed until combat is proven stable.

---

### Step 14 — Camera Expansion (Top-Down → Third-Person Hybrid)

**Vision:**
- Default view: top-down / isometric (combat readability)
- Mouse wheel zooms smoothly down into a **third-person orbit camera**
- Player can rotate camera freely at close zoom
- Camera transitions are cosmetic only (no gameplay authority)

**Design Constraints (LOCKED):**
- Server never depends on camera state
- Targeting remains raycast + Actor-based
- Combat readability must remain valid at all zoom levels

Acceptance:
- Zoom feels smooth and intentional
- No gameplay advantage from camera angle

---

### Step 15 — Land Claim & Housing System (Major Feature)

**Core Concept (LOCKED VISION):**
- Players purchase a **Land Deed** from an NPC vendor
- Deed placement claims a parcel of land
- Each deed defines a **build radius / envelope**
- Only the owning player (or permitted players) may build within the radius

#### Land Claim Rules
- Claims exist in the persistent world
- Claims cannot overlap
- Claims may be restricted by region (no-build zones)
- Claims are server-authoritative and validated

#### Ownership Model
- ClaimOwner = Player ActorId
- Optional: Co-owners / permissions
- Claims persist across sessions

---

### Step 16 — Construction & Building (Valheim-Inspired)

**Building Model:**
- Construction is **resource-driven**, not instant
- Players must gather resources (wood, stone, metal, etc.)
- Structures are assembled from placeable components

Examples:
- Foundations
- Walls
- Roofs
- Doors
- Furniture
- Crafting stations

**Design Constraints:**
- Building actions are server-validated
- Structures are world Actors or world-owned objects
- No free placement without resources

Acceptance:
- Building feels earned
- Houses are meaningful player achievements

---

### Step 17 — Housing Integration

- Player housing provides:
  - Storage
  - Decoration
  - Crafting bonuses
  - Social identity

- Housing does NOT provide:
  - Combat immunity
  - Free fast travel
  - Risk-free progression

> Housing is expression and logistics, not power creep.

---

## PHASE 5 — SOCIAL & LONG-TERM SYSTEMS (FUTURE)

- Guilds
- Permissions and shared housing
- Regional factions
- Guard / law systems
- World events
- Deeper dungeon layers

---

## SUMMARY

- Phases 1–2 are **done**
- Phase 3 makes the game playable
- Phase 4 fulfills the **Ultima Online fantasy**
- Housing is treated as a **world system**, not a side feature
- Camera expansion is cosmetic, not authoritative

This roadmap intentionally protects the core while allowing your long-term vision to grow naturally.

