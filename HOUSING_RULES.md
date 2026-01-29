# HOUSING_RULES.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 0.1  
Last Updated: 2026-01-29  
Engine: Unity 6 (URP)  
Networking: Netcode for GameObjects (NGO)  
Authority: Server-authoritative  
SceneRuleContext: MainlandHousing (required)  

---

## PURPOSE

Defines the authoritative **Housing system rules** for *Ultimate Dungeon*.

Housing exists to provide:
- Player identity and persistence
- Storage and decoration
- Player-run vendors (economy)
- Social and immersive gameplay (1st/3rd person)

Housing must **never** create progression exploits.

---

## HARD DESIGN LOCKS (MUST ENFORCE)

1. **Housing only exists in the Mainland scene**
   - SceneRuleContext must be `MainlandHousing`.

2. **No combat or progression in Mainland**
   - `CombatAllowed = false`
   - `DamageAllowed = false`
   - `DeathAllowed = false`
   - `DurabilityLossAllowed = false`
   - `SkillGainAllowed = false`
   - `ResourceGatheringAllowed = false`

3. **Server authoritative placement**
   - Clients preview locally.
   - Server validates and commits placement.

4. **No instanced housing**
   - Mainland is a shared open world.

5. **Land is a scarce world resource**
   - Plots cannot overlap.
   - Buildings cannot clip roads or restricted zones.

---

## TERMS

### Mainland
A large shared map containing roads and many buildable sites.

### Land Claim
A server-owned record that defines a player’s right to build within a region.

### Claim Anchor
The world position where the deed was placed.

### Build Envelope
The geometric region around the Claim Anchor where building is permitted.

### House Object
Any placeable built element (foundation, wall, floor, door, roof, furniture, etc.).

### Vendor
A player-owned Actor that can sell items for coins.

---

## HOUSING FLOW (LOCKED)

### Step 1 — Purchase Deed
- Player buys a **Deed** from an NPC vendor (usually in Hotnow Village).
- The deed is an inventory item with:
  - `DeedType`
  - `EnvelopeRadiusMeters` *(or footprint size)*
  - `MaxAllowedObjects` *(optional cap)*
  - `PlacementRulesetId`

### Step 2 — Place Deed in Mainland
- Player enters Mainland and uses the deed.
- Client enters **Placement Preview Mode**.
- Player selects a location and confirms.

### Step 3 — Server Validates Claim
Server validates:
- SceneRuleContext is `MainlandHousing`
- Location is within buildable zoning
- Not overlapping an existing claim
- Not intersecting roads / restricted areas
- Not inside blocked volumes (mountains, water, etc.)

If valid:
- Server creates a **LandClaim** record.
- Server consumes the deed.
- Server spawns a **Claim Marker** object (optional) at the anchor.

### Step 4 — Build Within Envelope
- Player can place house objects using a building menu.
- Every placement is previewed client-side and committed server-side.

---

## LAND CLAIM MODEL (AUTHORITATIVE)

A LandClaim record must include:

- `ClaimId` (stable unique id)
- `OwnerPlayerId`
- `AnchorPosition`
- `EnvelopeShape`
  - `Circle(radius)` *(default for v0.1)*
  - *(future: rectangle/polygon)*
- `EnvelopeRadiusMeters`
- `PlacementRulesetId`
- `CreatedUtc`
- `LastModifiedUtc`
- `AllowedEditors[]` *(permissions list)*
- `VendorSlots` *(how many vendors can be placed)*

---

## BUILD ENVELOPE (LOCKED)

### Default shape
- Circle centered on Claim Anchor.

### Envelope rules
1. All placeable house objects must have their **placement bounds fully inside** the envelope.
2. No object may cross outside the envelope, even partially.
3. The envelope is authoritative server-side.

### Restricted zones
Even inside the envelope, placement is refused if it intersects:
- Roads
- Public structures
- No-build zones
- Other claims

---

## PLACEMENT VALIDATION (SERVER, LOCKED ORDER)

When a player attempts to place any house object:

1. Validate SceneRuleContext == `MainlandHousing`
2. Validate player has build permission for the claim
3. Validate object definition is allowed in this ruleset
4. Validate object bounds are within envelope
5. Validate collision rules (no overlap with blocked volumes)
6. Validate grid/snap rules (if enabled)
7. Commit: spawn object as a networked entity and record it

If any check fails:
- Server refuses placement
- Client remains in preview mode

---

## BUILDING SYSTEM (VALHEIM-STYLE, LOCKED)

### Build pieces require resources
- House objects have a resource cost.
- Resources are consumed on server commit.

### No resource gathering in Mainland
Resources must be obtained from Dungeon scenes.

**Design consequence:** Mainland is a sink and showcase for dungeon-earned materials.

---

## PERMISSIONS (LOCKED)

A claim has these roles:

- **Owner**: full control
- **Editor**: can place/remove within claim
- **Visitor**: can enter and interact with public items

Rules:
- Only Owner can grant/revoke Editor.
- Public/Private permissions apply per interactable container or door.

---

## CONTAINERS & STORAGE (LOCKED POLICY)

Storage is allowed in housing.

### Security model (Proposed — Not Locked)
- Containers can be:
  - Private (Owner only)
  - Shared (Owner + Editors)
  - Public (anyone)

### Exploit locks
- Storage access must never allow combat/damage.
- Storage must never allow skill gains.

---

## PLAYER VENDORS (AUTHORITATIVE RULES)

### Vendor placement
- Vendors may only be placed inside a LandClaim.
- Each claim has `VendorSlots`.
- Vendor counts are enforced server-side.

### Vendor behavior
- Vendors are Actors with `ActorType = Vendor`.
- Vendors are non-hostile and cannot be attacked.
- Vendors can:
  - List items for sale
  - Hold inventory for sale
  - Accept purchases

### Currency lock
Vendor purchases must use **Banked Coins only**.

> The bank/held split is defined in `PLAYER_DEFINITION.md`. This doc only enforces that Mainland commerce is banked-only.

### Listing rules (Proposed — Not Locked)
- Owner sets price per item stack.
- Optional listing fee to sink currency.

---

## DECAY & MAINTENANCE (Open Question)

We must define whether houses:
- Persist forever
- Require upkeep (tax)
- Decay when inactive

**Open Question:** Do you want UO-style house decay (IDOC) or permanent claims?

---

## MULTIPLAYER & CONSISTENCY (LOCKED)

- All placed objects are spawned/owned by the server.
- The claim record is the source of truth.
- Clients are never trusted for bounds checks or overlap checks.

---

## REQUIRED DEPENDENCIES (NEXT)

1. `Mainland` scene zoning volumes (roads, no-build, buildable)
2. `Deed` item family (ItemDefs)
3. `LandClaim` persistence model (save/load)
4. `HouseObjectDef` definitions (build pieces + costs)
5. `PlacementPreview` client UX
6. `PlacementValidator` server rules
7. `Vendor` UI and purchase pipeline

---

## DESIGN LOCK CONFIRMATION

This document is **authoritative**.

Any change must:
- Increment Version
- Update Last Updated
- Call out impacted dependent systems (Economy, Items, Scene Rules, Vendors)

