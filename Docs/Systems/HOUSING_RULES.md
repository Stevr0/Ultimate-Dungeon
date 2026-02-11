# HOUSING_RULES.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 1.0  
Last Updated: 2026-02-11  
Engine: Unity 6 (URP)  
Authority: Server-authoritative (Shard Host)  

---

## PURPOSE

Defines the **deedless, shard-based housing system** for *Ultimate Dungeon*.

This document locks:
- Where players may build
- Who may build
- How permissions replace land ownership
- What housing is allowed to do (and not do)

Housing is treated as **personal world expression and utility**, not a scarce global resource.

---

## DESIGN SUMMARY (LOCKED)

- There are **no land deeds**
- There is **no land claiming system**
- Each player owns **one shard**
- On their own shard, players may build **freely** within the Village scene
- Visitors may only build if explicitly permitted
- Housing authority is enforced via **roles**, not items

---

## SCOPE BOUNDARIES (NO OVERLAP)

Owned elsewhere:
- Multiplayer topology & shard ownership: `PLAYER_HOSTED_SHARDS_MODEL.md`
- Item identity & instances: `ITEMS.md`
- Vendor behavior & economy: `ITEMS.md`, economy docs
- Scene legality & PvP rules: `ACTOR_MODEL.md`, `TARGETING_MODEL.md`

This document does **not** define:
- Building placement UI
- Crafting recipes
- Decorative vs functional object stats

---

## CORE DEFINITIONS

### Shard Owner
The player hosting the shard.

- Has full build authority within permitted scenes.
- May delegate permissions to other players.

### Village Scene (LOCKED)
A designated scene on each shard where player housing is allowed.

- Free-build zone for the shard owner.
- No land claims, plots, or boundaries.
- Server enforces placement legality.

### Non-Village Scenes
All other scenes (e.g. wilderness, dungeons).

- **No player construction allowed by default**.
- Exceptions must be explicitly documented elsewhere.

---

## DESIGN LOCKS (MUST ENFORCE)

1. **No Deeds, No Claims**
   - There is no item or system that grants land ownership.
   - Land ownership is implicit via shard ownership.

2. **Village-Only Construction**
   - Player construction is allowed only in the Village scene.

3. **Shard-Local Authority**
   - All housing actions are validated by the shard host.

4. **Permissions Over Items**
   - Build rights are role-based, not item-based.

---

## PERMISSIONS MODEL

### Roles

| Role     | Place Objects | Remove Objects | Manage Vendors | Grant Permissions |
|----------|---------------|----------------|----------------|-------------------|
| Owner    | Yes           | Yes            | Yes            | Yes               |
| CoOwner  | Yes           | Yes            | Yes            | No                |
| Editor   | Yes (scoped)  | Yes (scoped)   | No             | No                |
| Visitor  | No            | No             | No             | No                |

### Enforcement Rules
- All permission checks are server-side.
- Editors may be restricted to specific areas or object categories (implementation-defined).
- Permission failures must return explicit deny reasons.

---

## BUILDING RULES

### Placement
- Objects may be placed anywhere in the Village scene, subject to:
  - Collision checks
  - Terrain alignment rules
  - Scene-specific placement constraints

### Removal
- Only permitted roles may remove objects.
- Removing an object destroys it unless a recovery mechanic is explicitly added later.

### Overlap & Abuse Prevention
- Server must prevent:
  - Object overlap exploits
  - Blocking of essential NPCs or spawn points
  - Denial-of-service style spam placement

---

## VENDORS & INTERACTION

- Vendors are considered housing-linked objects.
- Vendor placement follows housing permissions.
- Visitors may interact with vendors but may not modify them.

---

## PERSISTENCE

### What Is Saved
- All placed housing objects
- Vendor states and inventories
- Permissions assignments

### Storage
- Housing state is saved as part of shard world data.
- Persistence format is owned by the shard save system.

---

## VISITOR SAFETY & PvP INTERACTION

- Village scenes are **non-PvP by default**.
- Combat legality is enforced by scene rules.
- Housing objects cannot be damaged unless explicitly enabled later.

---

## REMOVED SYSTEMS (EXPLICIT)

The following systems **do not exist**:
- Land deeds
- Plot boundaries
- Claim flags
- Rent or upkeep mechanics

Any implementation introducing these is **invalid** unless this document is revised.

---

## REQUIRED DOCUMENT INDEX UPDATE

Ensure `HOUSING_RULES.md` is listed under **WORLD & HOUSING SYSTEMS** in `DOCUMENTS_INDEX.md`.

---

## DESIGN LOCK CONFIRMATION

This document is **authoritative**.

Any change must:
- Increment Version
- Update Last Updated
- Explicitly call out impacts to shards, permissions, or persistence

