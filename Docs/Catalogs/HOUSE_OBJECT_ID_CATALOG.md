# HOUSE_OBJECT_ID_CATALOG.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 0.1  
Last Updated: 2026-01-29  
Engine: Unity 6 (URP)  
Authority: Server-authoritative  

---

## PURPOSE

Defines the authoritative, stable **HouseObjectId** values used by:
- `HouseObjectDef` ScriptableObjects
- Housing placement/persistence
- Server validation and save data

**IDs are forever.**
- Never rename IDs.
- Never reuse IDs.
- If content is removed, the ID is retired and remains reserved.

---

## ID FORMAT (LOCKED)

House object IDs use a stable string format:

- `HO_` prefix
- Category short code
- Name

Examples:
- `HO_FND_StoneFoundation_2x2`
- `HO_WALL_WoodWall_Solid`
- `HO_DOOR_WoodDoor_Single`

---

## CATEGORY SHORT CODES (LOCKED)

- `FND` = Foundation
- `FLR` = Floor
- `WALL` = Wall
- `ROOF` = Roof
- `DOOR` = Door
- `STAIR` = Stair
- `FENCE` = Fence
- `CONT` = Container
- `CRAFT` = CraftingStation
- `DECO` = Decoration
- `LIGHT` = Lighting
- `UTIL` = Utility

---

## V0.1 STARTER SET (AUTHORITATIVE)

This is the minimum set needed to prototype:
- Claim + envelope
- Basic house footprint
- Door access
- A few decoration + storage items

### Foundations
| HouseObjectId | Display Name | Notes |
|---|---|---|
| `HO_FND_WoodFoundation_2x2` | Wood Foundation (2x2) | Ground-only, low slope |
| `HO_FND_StoneFoundation_2x2` | Stone Foundation (2x2) | Ground-only, low slope |
| `HO_FND_WoodFoundation_4x4` | Wood Foundation (4x4) | Larger footprint |
| `HO_FND_StoneFoundation_4x4` | Stone Foundation (4x4) | Larger footprint |

### Floors
| HouseObjectId | Display Name | Notes |
|---|---|---|
| `HO_FLR_WoodFloor_2x2` | Wood Floor (2x2) | Must attach to foundation |
| `HO_FLR_StoneFloor_2x2` | Stone Floor (2x2) | Must attach to foundation |

### Walls
| HouseObjectId | Display Name | Notes |
|---|---|---|
| `HO_WALL_WoodWall_Solid` | Wood Wall (Solid) | Solid blocker |
| `HO_WALL_StoneWall_Solid` | Stone Wall (Solid) | Solid blocker |
| `HO_WALL_WoodWall_Window` | Wood Wall (Window) | Solid blocker |
| `HO_WALL_StoneWall_Window` | Stone Wall (Window) | Solid blocker |

### Doors
| HouseObjectId | Display Name | Notes |
|---|---|---|
| `HO_DOOR_WoodDoor_Single` | Wood Door (Single) | Interactable; permission-gated |
| `HO_DOOR_StoneDoor_Single` | Stone Door (Single) | Interactable; permission-gated |

### Roofs
| HouseObjectId | Display Name | Notes |
|---|---|---|
| `HO_ROOF_WoodRoof_Slope` | Wood Roof (Slope) | Visual only for v0.1 |
| `HO_ROOF_StoneRoof_Slope` | Stone Roof (Slope) | Visual only for v0.1 |

### Stairs
| HouseObjectId | Display Name | Notes |
|---|---|---|
| `HO_STAIR_WoodStairs_Straight` | Wood Stairs (Straight) | For second floor prototypes |

### Fences
| HouseObjectId | Display Name | Notes |
|---|---|---|
| `HO_FENCE_WoodFence_Straight` | Wood Fence (Straight) | Boundary decoration |
| `HO_FENCE_WoodFence_Gate` | Wood Fence Gate | Permission-gated |

### Containers
| HouseObjectId | Display Name | Notes |
|---|---|---|
| `HO_CONT_WoodChest_Small` | Small Wooden Chest | Storage; permission-gated |
| `HO_CONT_WoodChest_Large` | Large Wooden Chest | Storage; permission-gated |

### Lighting
| HouseObjectId | Display Name | Notes |
|---|---|---|
| `HO_LIGHT_TorchWall` | Wall Torch | Decoration/light |
| `HO_LIGHT_LanternStanding` | Standing Lantern | Decoration/light |

### Decorations
| HouseObjectId | Display Name | Notes |
|---|---|---|
| `HO_DECO_Table_Wood` | Wooden Table | Non-blocking or solid (choose in def) |
| `HO_DECO_Chair_Wood` | Wooden Chair | Non-blocking or solid |
| `HO_DECO_Bed_Simple` | Simple Bed | Solid footprint |
| `HO_DECO_Rug_Small` | Small Rug | Non-blocking; overlap-allowed with decor |

### Utility
| HouseObjectId | Display Name | Notes |
|---|---|---|
| `HO_UTIL_Mailbox` | Mailbox | Per-claim limit 1 |
| `HO_UTIL_Signpost_House` | House Sign | Displays owner/claim name |

---

## RETIRED / RESERVED

- None in v0.1

---

## CHANGE RULES (LOCKED)

Allowed changes:
- Add new IDs
- Mark IDs as retired
- Add display name tweaks (non-authoritative)

Forbidden changes:
- Reuse an ID
- Change an existing ID string

---

## NEXT DEPENDENCIES

1. Create `HouseObjectDef` assets for each ID
2. Author prefabs and set bounds/snap/collision rules per schema
3. Implement placement preview + validator

---

## DESIGN LOCK CONFIRMATION

This document is **authoritative**.

Any change must:
- Increment Version
- Update Last Updated
- Call out dependent impacts (prefabs, defs, persistence)

