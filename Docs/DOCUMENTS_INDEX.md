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

- If you’re working on **combat legality**, start with [ACTOR_MODEL.md](Systems/ACTOR_MODEL.md) and [TARGETING_MODEL.md](Systems/TARGETING_MODEL.md)
- If you’re working on **combat math/execution**, start with [COMBAT_CORE.md](Systems/COMBAT_CORE.md)
- If you’re working on **items**, start with [ITEM_DEF_SCHEMA.md](Systems/ITEM_DEF_SCHEMA.md) and [ITEM_CATALOG.md](Catalogs/ITEM_CATALOG.md)
- If you’re working on **resource gathering**, start with [GATHERING_NODE_DEFS.md](Systems/GATHERING_NODE_DEFS.md) and [DROP_TABLE_ID_CATALOG.md](Catalogs/DROP_TABLE_ID_CATALOG.md)
- If you’re working on **housing**, start with [HOUSING_RULES.md](Systems/HOUSING_RULES.md) and the house object schema/catalogs

---

## CORE GAME VISION

### `GDD.md`
**What it does:** High-level game design document for *Ultimate Dungeon*. World premise, player fantasy, major systems.  
**Location:** [Docs/Design/GDD.md](Design/GDD.md)

### `ROADMAP.md`
**What it does:** Step-by-step build order for the first playable slice and beyond.  
**Location:** [Docs/Design/ROADMAP.md](Design/ROADMAP.md)

---

## ACTORS, TARGETING, COMBAT

### `ACTOR_MODEL.md`
**What it does:** Defines what an Actor is (Player/Monster/NPC/Vendor/Object), faction/hostility, scene rule context, combat legality gates.  
**Location:** [Docs/Systems/ACTOR_MODEL.md](Systems/ACTOR_MODEL.md)

### `TARGETING_MODEL.md`
**What it does:** Defines server-validated targeting intents (Select/Interact/Attack/Cast), validation order, range/LoS policy, deny reasons.  
**Location:** [Docs/Systems/TARGETING_MODEL.md](Systems/TARGETING_MODEL.md)

### `COMBAT_CORE.md`
**What it does:** Defines combat execution (timers, hit/miss, damage packets, procs, durability loss triggers) after legality is confirmed.  
**Location:** [Docs/Systems/COMBAT_CORE.md](Systems/COMBAT_CORE.md)

### `COMBAT_DISENGAGE_RULES.md`
**What it does:** Defines combat disengage policy and validation rules.  
**Location:** [Docs/Systems/COMBAT_DISENGAGE_RULES.md](Systems/COMBAT_DISENGAGE_RULES.md)

### `PLAYER_MODEL.md`
**What it does:** Runtime player representation model (identity/network/runtime components). *(Keep aligned to Actor-first rules.)*  
**Location:** [Docs/Systems/PLAYER_MODEL.md](Systems/PLAYER_MODEL.md)

### `PLAYER_DEFINITION.md`
**What it does:** ScriptableObject contract for player baselines/caps (skill cap rules, coin model, etc.).  
**Location:** [Docs/Systems/PLAYER_DEFINITION.md](Systems/PLAYER_DEFINITION.md)

### `PLAYER_COMBAT_STATS.md`
**What it does:** Authoritative combat stat aggregation contract (hit chance, defence, swing speed, damage mods, derived gates).  
**Location:** [Docs/Systems/PLAYER_COMBAT_STATS.md](Systems/PLAYER_COMBAT_STATS.md)

---

## SKILLS & PROGRESSION

### `SKILLS.md`
**What it does:** Full skill list and design intent.  
**Location:** [Docs/Systems/SKILLS.md](Systems/SKILLS.md)

### `SKILL_ID_CATALOG.md`
**What it does:** Stable SkillId list (IDs are forever). Used by SkillDefs and runtime.  
**Location:** [Docs/Catalogs/SKILL_ID_CATALOG.md](Catalogs/SKILL_ID_CATALOG.md)

### `PROGRESSION.md`
**What it does:** Skill gain policies, caps, and progression rules. (No levels/XP.)  
**Location:** [Docs/Systems/PROGRESSION.md](Systems/PROGRESSION.md)

---

## MAGIC & SPELLS

### `SPELL_CATEGORY_MODEL.md`
**What it does:** Spell design rules, spell categories, casting model, targeting types.  
**Location:** [Docs/Systems/SPELL_CATEGORY_MODEL.md](Systems/SPELL_CATEGORY_MODEL.md)

### `ABILITY_AND_SPELL_PAYLOADS.md`
**What it does:** Authoritative payload definitions for spell/ability effects and runtime execution.  
**Location:** [Docs/Systems/ABILITY_AND_SPELL_PAYLOADS.md](Systems/ABILITY_AND_SPELL_PAYLOADS.md)

### `SPELL_ID_CATALOG.md`
**What it does:** Stable SpellId list (IDs are forever).  
**Location:** [Docs/Catalogs/SPELL_ID_CATALOG.md](Catalogs/SPELL_ID_CATALOG.md)

### `SPELL_DEF_SCHEMA.md`
**What it does:** Authoritative schema for SpellDef ScriptableObjects (mana, reagents, targeting, cast time, cooldown, etc.).  
**Location:** [Docs/Systems/SPELL_DEF_SCHEMA.md](Systems/SPELL_DEF_SCHEMA.md)

---

## STATUS EFFECTS

### `STATUS_EFFECT_CATALOG.md`
**What it does:** Stable StatusEffectId list and definitions. Used by StatusEffectDefs and runtime.  
**Location:** [Docs/Catalogs/STATUS_EFFECT_CATALOG.md](Catalogs/STATUS_EFFECT_CATALOG.md)

---

## ITEMS & LOOT

### `ITEMS.md`
**What it does:** Item system design overview (item families, durability, general policies).  
**Location:** [Docs/Systems/ITEMS.md](Systems/ITEMS.md)

### `ITEM_DEF_SCHEMA.md`
**What it does:** Authoritative schema for ItemDef ScriptableObjects (stats, durability, equip rules, etc.).  
**Location:** [Docs/Systems/ITEM_DEF_SCHEMA.md](Systems/ITEM_DEF_SCHEMA.md)

### `ITEM_CATALOG.md`
**What it does:** Stable ItemId list (IDs are forever). Identity only; no balance.  
**Location:** [Docs/Catalogs/ITEM_CATALOG.md](Catalogs/ITEM_CATALOG.md)

### `ITEM_AFFIX_CATALOG.md`
**What it does:** Affix list and rules for random item generation/enhancement.  
**Location:** [Docs/Catalogs/ITEM_AFFIX_CATALOG.md](Catalogs/ITEM_AFFIX_CATALOG.md)

### `RESOURCE_AND_COLLECTABLE_CATALOG.md`
**What it does:** Design catalog of all gatherable/lootable resources and special collectables (meaning + sourcing).  
**Location:** [Docs/Catalogs/RESOURCE_AND_COLLECTABLE_CATALOG.md](Catalogs/RESOURCE_AND_COLLECTABLE_CATALOG.md)

### `ITEMDEF_RESOURCE_TEMPLATES.md`
**What it does:** Default ItemDef baseline templates for resource items (stack size, weight, trade rules).  
**Location:** [Docs/Systems/ITEMDEF_RESOURCE_TEMPLATES.md](Systems/ITEMDEF_RESOURCE_TEMPLATES.md)

### `GATHERING_NODE_DEFS.md`
**What it does:** Defines GatheringNodeDef + DropTableDef models and starter content for Trees/OreVeins/Corpses.  
**Location:** [Docs/Systems/GATHERING_NODE_DEFS.md](Systems/GATHERING_NODE_DEFS.md)

### `DROP_TABLE_ID_CATALOG.md`
**What it does:** Stable DropTableId list (IDs are forever). Used by DropTableDef assets and nodes.  
**Location:** [Docs/Catalogs/DROP_TABLE_ID_CATALOG.md](Catalogs/DROP_TABLE_ID_CATALOG.md)

---

## HOUSING

### `HOUSING_RULES.md`
**What it does:** Housing rules: land claim, build envelope, permissions, vendor slots, no combat/progression in Mainland.  
**Location:** [Docs/Systems/HOUSING_RULES.md](Systems/HOUSING_RULES.md)

### `HOUSE_OBJECT_DEF_SCHEMA.md`
**What it does:** Schema for HouseObjectDef assets (placement rules, costs, snapping, collision policy).  
**Location:** [Docs/Systems/HOUSE_OBJECT_DEF_SCHEMA.md](Systems/HOUSE_OBJECT_DEF_SCHEMA.md)

### `HOUSE_OBJECT_ID_CATALOG.md`
**What it does:** Stable HouseObjectId list (IDs are forever). Used by HouseObjectDefs and persistence.  
**Location:** [Docs/Catalogs/HOUSE_OBJECT_ID_CATALOG.md](Catalogs/HOUSE_OBJECT_ID_CATALOG.md)

---

## SCENE RULES

### `SCENE_RULE_PROVIDER.md`
**What it does:** Implementation contract for SceneRuleContext/Flags exposure and scene transition clearing.  
**Location:** [Docs/Systems/SCENE_RULE_PROVIDER.md](Systems/SCENE_RULE_PROVIDER.md)

---

## IMPLEMENTATION / INVENTORY OF SCRIPTS

### `SCRIPTS.generated.md`
**What it does:** Non-authoritative list of scripts currently in the Unity project and what each one does.  
**Location:** [Docs/Systems/SCRIPTS.generated.md](Systems/SCRIPTS.generated.md)

---

## NOTES / MAINTENANCE RULES

- **Catalog docs** (`*_ID_CATALOG.md`) exist to lock stable IDs.
- **Schema docs** (`*_DEF_SCHEMA.md`) exist to define ScriptableObject fields.
- **Rules/core docs** (Actor/Targeting/Combat/Housing) define authoritative runtime policies.
- If a rule is missing, it **does not exist**.

---

## DESIGN LOCK CONFIRMATION
