# HOUSE_OBJECT_DEF_SCHEMA.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 0.1  
Last Updated: 2026-01-29  
Engine: Unity 6 (URP)  
Networking: Netcode for GameObjects (NGO)  
Authority: Server-authoritative  
SceneRuleContext: MainlandHousing (required for placement)  

---

## PURPOSE

Defines the authoritative **data schema** for **house/build pieces** used by the Mainland Housing system.

This schema is used for:
- Placement preview (client)
- Validation + commit (server)
- Persistence (save/load)
- Vendor/home decoration catalogs

If a field or rule is not defined here, it does not exist.

---

## DEPENDENCIES (MUST ALIGN)

- `HOUSING_RULES.md`
  - Build envelope validation
  - Server-authoritative placement
  - No combat/progression in Mainland

- `SCENE_RULE_PROVIDER.md`
  - Placement must require `SceneRuleContext == MainlandHousing`

- `ITEM_DEF_SCHEMA.md` *(resource items referenced by costs)*

---

## DESIGN LOCKS (MUST ENFORCE)

1. **Placement is server-authoritative**
   - Client preview is cosmetic.
   - Server validates bounds, envelope, collisions, permissions, and resource costs.

2. **House objects are content-defined**
   - Placement behavior must be data-driven via defs.

3. **Deterministic placement**
   - A given def + transform must produce identical bounds checks on server.

4. **No combat interactions**
   - House objects cannot deal damage, apply hostile status effects, or grant skill gain.

---

## HOUSE OBJECT DEFINITIONS

Each build piece is defined by a `HouseObjectDef` ScriptableObject.

### Identity fields (LOCKED)
- `HouseObjectId` *(stable ID; never reuse)*
- `DisplayName`
- `Category`
- `Icon`

### Content fields (LOCKED)
- `Prefab` *(NetworkObject-capable prefab)*
- `DefaultRotationY` *(0/90/180/270 typical)*
- `AllowRotateInPreview` *(bool)*
- `AllowMirrorInPreview` *(bool; optional for later)*

### Placement bounds (LOCKED)
- `PlacementBoundsModel`
  - `PrefabColliderBounds` *(default)*
  - `ExplicitBox` *(authoritative fallback for tricky prefabs)*

- `ExplicitBounds` *(only used if ExplicitBox)*
  - `CenterLocal` (Vector3)
  - `SizeLocal` (Vector3)

**Design lock:** The server must compute bounds using the same method every time.

### Snap / grid rules (LOCKED)
- `SnapMode`
  - `None`
  - `Grid`
  - `Socket`
  - `GridThenSocket` *(optional)*

- `Grid`
  - `CellSizeMeters` *(e.g., 0.5, 1.0, 2.0)*
  - `SnapPosition` *(bool)*
  - `SnapRotation` *(bool; typically 90° increments)*

- `Sockets`
  - `RequiredSocketTags[]` *(e.g., FoundationEdge, WallTop, DoorFrame)*
  - `ProvidedSocketTags[]` *(what this piece exposes to others)*

> Socket implementation is a future step, but the schema must support it.

### Collision / overlap policy (LOCKED)
- `PlacementCollisionPolicy`
  - `BlockAllOverlaps` *(default)*
  - `AllowOverlapWithSameClaimOnly` *(rare; e.g., decorative decals)*
  - `AllowOverlapWithDecorOnly` *(future)*

- `BlockingVolumeType`
  - `Solid` *(walls, floors, foundations)*
  - `NonBlocking` *(rugs, small deco)*

**Design lock:** Solid pieces must prevent placement overlap with other Solid pieces.

### Terrain / surface constraints (LOCKED)
- `SurfaceRequirement`
  - `Any`
  - `GroundOnly`
  - `MustAttachToFoundation`
  - `MustAttachToWall`
  - `MustAttachToCeiling`

- `MaxSlopeDegrees` *(for GroundOnly/foundations)*

### Claim & permission constraints (LOCKED)
- `AllowedInClaim`
  - `OwnerOnly`
  - `OwnerOrEditors`

- `AllowPlacementOutsideClaims` *(bool; must be false for v0.1)*

**Design lock:** All placement must occur inside a valid LandClaim.

### Limits / caps (LOCKED)
- `CountsTowardClaimObjectCap` *(bool; default true)*
- `PerClaimLimit` *(0 = unlimited within claim cap)*

Examples:
- `PerClaimLimit = 1` for a unique mailbox
- `PerClaimLimit = 2` for a vendor stall (if vendors handled as objects)

### Resource cost (LOCKED)
Defines what the server consumes on successful placement.

- `BuildCost[]`
  - `ItemId` *(reference to item definition; resource item)*
  - `Amount` *(int)*

**Design lock:** Costs are consumed only after placement passes all validation.

### Refund / removal policy (LOCKED)
- `RemovalPolicy`
  - `NotRemovable`
  - `RemovableByOwner`
  - `RemovableByOwnerOrEditors`

- `RefundPolicy`
  - `None`
  - `Full`
  - `Partial` *(future: based on damage/decay)*

- `RefundRate` *(0.0–1.0; only for Partial)*

---

## CATEGORIES (LOCKED LIST)

- `Foundation`
- `Floor`
- `Wall`
- `Roof`
- `Door`
- `Stair`
- `Fence`
- `Container`
- `CraftingStation`
- `Decoration`
- `Lighting`
- `Utility`

> Vendors are Actors (`ActorType = Vendor`) and are not defined as HouseObjectDefs unless explicitly changed later.

---

## HOUSE OBJECT RUNTIME INSTANCE (LOCKED)

When placed, a `HouseObjectInstance` record must be created:

- `InstanceId` (unique)
- `HouseObjectId`
- `ClaimId`
- `PlacedByPlayerId`
- `Position`
- `Rotation`
- `Scale` *(optional; default 1,1,1; usually locked)*
- `PlacedUtc`

If the object is an interactable:
- `Permissions` *(private/shared/public depending on object type)*

---

## SERVER PLACEMENT VALIDATION (MUST FOLLOW)

On placement request:

1. Require `SceneRuleContext == MainlandHousing`
2. Resolve `LandClaim` at placement point
3. Check builder permission vs `AllowedInClaim`
4. Validate `HouseObjectDef` exists and is allowed by claim ruleset
5. Compute authoritative bounds (per bounds model)
6. Validate bounds fully inside claim envelope
7. Validate restricted zones (roads/no-build)
8. Validate overlap/collision policy
9. Validate surface requirements (ground/foundation/wall)
10. Validate limits (PerClaimLimit + claim cap)
11. Validate player has required resources
12. Commit: consume resources, spawn object, write instance record

---

## PROPOSED (NOT LOCKED YET)

These are likely needed, but are not locked in v0.1:

- Structural integrity/support rules (walls require foundations)
- Damage/decay on house pieces (if upkeep/decay is added)
- Socket graph building for smart snapping
- Seasonal cosmetics

---

## REQUIRED IMPLEMENTATION ARTIFACTS (NEXT)

1. `HouseObjectId` catalog (stable IDs)
2. `HouseObjectDef` ScriptableObject class
3. Server `HousePlacementValidator`
4. Client placement preview UX (snap, rotate, red/green validity)
5. Persistence: save/load `LandClaim` + `HouseObjectInstance`

---

## DESIGN LOCK CONFIRMATION

This document is **authoritative**.

Any change must:
- Increment Version
- Update Last Updated
- Call out impacted dependent systems (Housing, Items, Persistence, UI)

