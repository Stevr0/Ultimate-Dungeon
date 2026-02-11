# DOCUMENTS_INDEX.md — Ultimate Dungeon (AUTHORITATIVE ROUTING INDEX)

Version: 1.0  
Last Updated: 2026-02-11  

---

## PURPOSE

This document is the **routing table** for all authoritative design documents in *Ultimate Dungeon*.

It defines:
- Which document owns which rules
- Which document must be consulted first for a given system
- Clear scope boundaries to prevent rule duplication or contradiction

**If a rule is not owned by a document listed here, it does not exist.**

---

## CORE AUTHORITY ORDER (READ FIRST)

1. `DOCUMENTS_INDEX.md` *(this file)*
2. `PLAYER_HOSTED_SHARDS_MODEL.md`
3. `SESSION_AND_PERSISTENCE_MODEL.md`
4. `ACTOR_MODEL.md`
5. `SCENE_RULE_PROVIDER.md`

---

## MULTIPLAYER TOPOLOGY & IDENTITY (AUTHORITATIVE)

### `PLAYER_HOSTED_SHARDS_MODEL.md`
**Owns:**
- Player-hosted shard model
- One shard per player
- Public shard discovery (no passwords)
- Shard lifecycle (online only while host is running)
- Shard-local PvP dungeons
- Physical item transport rules between shards

---

### `SESSION_AND_PERSISTENCE_MODEL.md`
**Owns:**
- Account identity (SteamId planned)
- Single-character-per-account rule
- Character snapshot contract
- Join / leave / visit shard flow
- Character vs shard persistence boundaries
- Crash and recovery behavior

---

## WORLD, SCENES & HOUSING

### `ACTOR_MODEL.md`
**Owns:**
- Actor identity and classification
- SceneRuleContext definitions (`ShardVillage`, `Dungeon`)
- Scene legality flags (combat, PvP, building, death, etc.)
- Targeting and interaction legality

---

### `SCENE_RULE_PROVIDER.md`
**Owns:**
- How scenes declare their rule context
- Primary rule source selection (additive scenes)
- Resolution of legality flags at runtime

---

### `HOUSING_RULES.md`
**Owns:**
- Deedless housing model
- Village-only construction
- Permission-based build authority
- Housing persistence expectations

---

## COMBAT SYSTEMS

### `COMBAT_CORE.md`
**Owns:**
- Combat execution pipeline
- Damage resolution
- Hit / miss / mitigation

> Combat legality is never decided here — it is gated by `ACTOR_MODEL.md` + `SCENE_RULE_PROVIDER.md`.

---

## ITEMS, INVENTORY & ECONOMY

### `ITEMS.md`
**Owns:**
- Item instance lifecycle
- Equip rules
- Vendor trade rules

---

### `ITEM_DEF_SCHEMA.md`
**Owns:**
- ItemDef ScriptableObject schema
- Validation rules

---

### `ITEM_CATALOG.md`
**Owns:**
- Authored base items
- Base stats per item

---

### `ITEM_AFFIX_CATALOG.md`
**Owns:**
- Affix definitions
- Affix tiers and caps

---

## SKILLS, SPELLS & STATUS

### `SKILL_ID_CATALOG.md`
**Owns:**
- Skill ID enumeration

---

### `SPELL_ID_CATALOG.md`
**Owns:**
- Spell ID enumeration

---

### `STATUS_EFFECT_CATALOG.md`
**Owns:**
- Status effect definitions

---

## SCENE CONTENT & WORLD DATA

### `GATHERING_NODE_DEFS.md`
**Owns:**
- Resource node definitions

---

### `DROP_TABLE_ID_CATALOG.md`
**Owns:**
- Loot table identifiers

---

## RULES OF CHANGE

- Any change to an authoritative document must:
  - Increment its version
  - Update its Last Updated field
  - Call out downstream impacts

- If two documents appear to conflict:
  - The one listed **higher** in this index wins

---

## DESIGN LOCK CONFIRMATION

This document is **authoritative**.

If a rule is not routed here, it is invalid by default.

