# DOCUMENTS_INDEX.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 0.1  
Last Updated: 2026-01-29  
Engine: Unity 6 (URP)  
Networking: Netcode for GameObjects (NGO)  

---

## PURPOSE

Provides a single, human-readable index of **all project documents** and what each one is responsible for.

Use this as:
- A **navigation hub** for your Project Files
- A **scope boundary map** (prevents doc overlap)
- A quick prompt context for ChatGPT so it can find the right authority source

---

## HOW TO USE THIS INDEX

- If you’re working on **combat legality**, start with `ACTOR_MODEL.md` and `TARGETING_MODEL.md`
- If you’re working on **combat math/execution**, start with `COMBAT_CORE.md`
- If you’re working on **items**, start with `ITEM_DEF_SCHEMA.md` and `ITEM_CATALOG.md`
- If you’re working on **resource gathering**, start with `GATHERING_NODE_DEFS.md` and `DROP_TABLE_ID_CATALOG.md`
- If you’re working on **housing**, start with `HOUSING_RULES.md` and the house object schema/catalogs

---

## CORE GAME VISION

### `GDD.md`
**What it does:** High-level game design document for *Ultimate Dungeon*. World premise, player fantasy, major systems.

### `ROADMAP.md`
**What it does:** Step-by-step build order for the first playable slice and beyond.

---

## ACTORS, TARGETING, COMBAT

### `ACTOR_MODEL.md`
**What it does:** Defines what an Actor is (Player/Monster/NPC/Vendor/Object), faction/hostility, scene rule context, combat legality gates.

### `TARGETING_MODEL.md`
**What it does:** Defines server-validated targeting intents (Select/Interact/Attack/Cast), validation order, range/LoS policy, deny reasons.

### `COMBAT_CORE.md`
**What it does:** Defines combat execution (timers, hit/miss, damage packets, procs, durability loss triggers) after legality is confirmed.

### `PLAYER_MODEL.md`
**What it does:** Runtime player representation model (identity/network/runtime components). *(Keep aligned to Actor-first rules.)*

### `PLAYER_DEFINITION.md`
**What it does:** ScriptableObject contract for player baselines/caps (skill cap rules, coin model, etc.).

### `PLAYER_COMBAT_STATS.md`
**What it does:** Authoritative combat stat aggregation contract (hit chance, defence, swing speed, damage mods, derived gates).

---

## SKILLS & PROGRESSION

### `SKILLS.md`
**What it does:** Full skill list and design intent.

### `SKILL_ID_CATALOG.md`
**What it does:** Stable SkillId list (IDs are forever). Used by SkillDefs and runtime.

### `PROGRESSION.md`
**What it does:** Skill gain policies, caps, and progression rules. (No levels/XP.)

---

## MAGIC & SPELLS

### `MAGIC_AND_SPELLS.md`
**What it does:** Spell design rules, spell categories, casting model, targeting types.

### `SPELL_ID_CATALOG.md`
**What it does:** Stable SpellId list (IDs are forever).

### `SPELL_DEF_SCHEMA.md`
**What it does:** Authoritative schema for SpellDef ScriptableObjects (mana, reagents, targeting, cast time, cooldown, etc.).

---

## STATUS EFFECTS

### `STATUS_EFFECT_CATALOG.md`
**What it does:** Stable StatusEffectId list and definitions. Used by StatusEffectDefs and runtime.

---

## ITEMS & LOOT

### `ITEMS.md`
**What it does:** Item system design overview (item families, durability, general policies).

### `ITEM_DEF_SCHEMA.md`
**What it does:** Authoritative schema for ItemDef ScriptableObjects (stats, durability, equip rules, etc.).

### `ITEM_CATALOG.md`
**What it does:** Stable ItemId list (IDs are forever). Identity only; no balance.

### `ITEM_AFFIX_CATALOG.md`
**What it does:** Affix list and rules for random item generation/enhancement.

### `RESOURCE_AND_COLLECTABLE_CATALOG.md`
**What it does:** Design catalog of all gatherable/lootable resources and special collectables (meaning + sourcing).

### `ITEMDEF_RESOURCE_TEMPLATES.md`
**What it does:** Default ItemDef baseline templates for resource items (stack size, weight, trade rules).

### `GATHERING_NODE_DEFS.md`
**What it does:** Defines GatheringNodeDef + DropTableDef models and starter content for Trees/OreVeins/Corpses.

### `DROP_TABLE_ID_CATALOG.md`
**What it does:** Stable DropTableId list (IDs are forever). Used by DropTableDef assets and nodes.

---

## HOUSING

### `HOUSING_RULES.md`
**What it does:** Housing rules: land claim, build envelope, permissions, vendor slots, no combat/progression in Mainland.

### `HOUSE_OBJECT_DEF_SCHEMA.md`
**What it does:** Schema for HouseObjectDef assets (placement rules, costs, snapping, collision policy).

### `HOUSE_OBJECT_ID_CATALOG.md`
**What it does:** Stable HouseObjectId list (IDs are forever). Used by HouseObjectDefs and persistence.

---

## SCENE RULES

### `SCENE_RULE_PROVIDER.md`
**What it does:** Implementation contract for SceneRuleContext/Flags exposure and scene transition clearing.

---

## IMPLEMENTATION / INVENTORY OF SCRIPTS

### `SCRIPTS.md`
**What it does:** Non-authoritative list of scripts currently in the Unity project and what each one does.

---

## NOTES / MAINTENANCE RULES

- **Catalog docs** (`*_ID_CATALOG.md`) exist to lock stable IDs.
- **Schema docs** (`*_DEF_SCHEMA.md`) exist to define ScriptableObject fields.
- **Rules/core docs** (Actor/Targeting/Combat/Housing) define authoritative runtime policies.
- If a rule is missing, it **does not exist**.

---

## DESIGN LOCK CONFIRMATION

This document is **authoritative** as the navigation index.

Any change must:
- Increment Version
- Update Last Updated
- Keep descriptions scope-accurate (avoid overlap)

