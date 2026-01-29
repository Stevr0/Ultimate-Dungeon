# DROP_TABLE_ID_CATALOG.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 0.1  
Last Updated: 2026-01-29  
Engine: Unity 6 (URP)  
Networking: Netcode for GameObjects (NGO)  
Authority: Server-authoritative  

---

## PURPOSE

Defines the authoritative, stable **DropTableId** values used by:
- `DropTableDef` ScriptableObjects
- `GatheringNodeDef` output references
- Loot containers (chests, breakables) *(future)*
- Monster loot rolls *(future)*

**IDs are forever.**
- Never rename IDs.
- Never reuse IDs.
- If content is removed, the ID is retired and remains reserved.

---

## ID FORMAT (LOCKED)

Drop table IDs use a stable string format:

- `DT_` prefix
- Domain short code
- Specific name

Examples:
- `DT_TREE_Pine`
- `DT_ORE_Iron`
- `DT_CORPSE_Beast`

---

## DOMAIN SHORT CODES (LOCKED)

- `TREE` = Trees / wood nodes
- `ORE` = Ore veins / mining nodes
- `CORPSE` = Carving/harvesting corpses
- `CHEST` = Chests *(reserved)*
- `BREAK` = Breakables *(reserved)*
- `HERB` = Herb patches *(reserved)*
- `WATER` = Water sources *(reserved)*
- `MON` = Monster loot *(reserved)*
- `BOSS` = Boss loot *(reserved)*

---

## V0.1 STARTER SET (AUTHORITATIVE)

These IDs correspond to the starter content defined in `GATHERING_NODE_DEFS.md`.

### Tree drop tables
- `DT_TREE_Pine`
- `DT_TREE_Ancient`

### Ore drop tables
- `DT_ORE_Iron`
- `DT_ORE_Obsidian`

### Corpse drop tables
- `DT_CORPSE_Humanoid`
- `DT_CORPSE_Beast`
- `DT_CORPSE_Spider`

---

## RESERVED FOR NEXT (NOT YET DEFINED)

These are reserved namespaces to avoid later refactors.

### Additional Trees
- `DT_TREE_Oak`
- `DT_TREE_Birch`
- `DT_TREE_Deadwood`

### Additional Ores
- `DT_ORE_Copper`
- `DT_ORE_Tin`
- `DT_ORE_Silver`
- `DT_ORE_Gold`
- `DT_ORE_Mythril`
- `DT_ORE_Adamantite`

### Herbs
- `DT_HERB_Ginseng`
- `DT_HERB_Mandrake`
- `DT_HERB_Nightshade`

### Water sources
- `DT_WATER_Fresh`
- `DT_WATER_Brackish`
- `DT_WATER_Pure`

### Chests / Breakables
- `DT_CHEST_Common`
- `DT_CHEST_Uncommon`
- `DT_CHEST_Rare`
- `DT_BREAK_Urn_Common`
- `DT_BREAK_Crate_Common`

### Monsters / Bosses
- `DT_MON_Common`
- `DT_MON_Elite`
- `DT_BOSS_Tier1`

---

## RETIRED / RESERVED

- None (v0.1)

---

## CHANGE RULES (LOCKED)

Allowed changes:
- Add new IDs
- Mark IDs as retired
- Add comments/notes (non-authoritative)

Forbidden changes:
- Reuse an ID
- Change an existing ID string

---

## NEXT DEPENDENCIES

1. Create `DropTableDef` assets for each active ID
2. Enforce ID validation at boot (registry)
3. Reference IDs from:
   - `GatheringNodeDef`
   - Loot containers
   - Monster definitions

---

## DESIGN LOCK CONFIRMATION

This document is **authoritative**.

Any change must:
- Increment Version
- Update Last Updated
- Call out dependent impacts (nodes, loot, content)

