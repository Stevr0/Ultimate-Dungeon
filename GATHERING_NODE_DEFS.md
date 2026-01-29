# GATHERING_NODE_DEFS.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 0.1  
Last Updated: 2026-01-29  
Engine: Unity 6 (URP)  
Networking: Netcode for GameObjects (NGO)  
Authority: Server-authoritative  

---

## PURPOSE

Defines the authoritative **Gathering Node** content model used to produce resources from:
- **Trees** (wood / plant outputs)
- **Ore Veins** (ores / stone outputs)
- **Corpses** (monster components / hides / bones / meat)

Gathering nodes are **Actors/Objects** in the world.
The harvested outputs are **Items** listed in `ITEM_CATALOG.md`.

---

## DEPENDENCIES (MUST ALIGN)

- `ACTOR_MODEL.md`
  - Nodes are `ActorType = Object`
  - Scene gating via `SceneRuleFlags.ResourceGatheringAllowed`

- `SCENE_RULE_PROVIDER.md`
  - Gathering must be refused if the current scene disallows it

- `ITEM_CATALOG.md`
  - Output ItemIds must exist

- `ITEMDEF_RESOURCE_TEMPLATES.md`
  - Output ItemDefs should follow resource defaults

- `RESOURCE_AND_COLLECTABLE_CATALOG.md`
  - Output meaning & sourcing

---

## DESIGN LOCKS (MUST ENFORCE)

1. **Server-authoritative gathering**
   - Clients request gather intent
   - Server validates legality, rolls drops, commits inventory changes

2. **Scene-gated**
   - If `ResourceGatheringAllowed == false` → gathering always denied

3. **Nodes do not directly spawn items in the world** *(v0.1 lock)*
   - Outputs are granted to the gatherer’s inventory (or a server-owned container) on success
   - (Future) optional “drop to ground” policies may be added per node

4. **Tools and skills gate yield** *(policy-only in v0.1)*
   - Node can require a tool type
   - Node can declare a primary skill
   - Exact skill math is owned by progression/crafting docs

5. **Drop tables are deterministic (server-seeded)**
   - All RNG uses server-seeded deterministic sources

---

## CORE CONCEPTS

### GatheringNodeDef (ScriptableObject)
A content definition that drives:
- What this node is (tree/ore/corpse)
- What tool it needs
- How many “harvest actions” it supports
- Which DropTable it uses
- How it respawns or depletes

### DropTableDef (ScriptableObject)
A content definition listing possible outputs with:
- Weighted chances
- Quantity ranges
- Optional “bonus” rules

---

## NODE TYPES (LOCKED)

- `Tree`
- `OreVein`
- `Corpse`
- `HerbPatch` *(reserved)*
- `WaterSource` *(reserved)*

---

## TOOL REQUIREMENTS (LOCKED)

- `None`
- `Axe`
- `Pickaxe`
- `Knife`

> Tools are items (`ITEM_TOL_*`) and/or equipment. The gate is validated server-side.

---

## GATHER INTENT MODEL (LOCKED)

### GatherIntentType
- `Harvest`

### GatherRequest (client → server)
- `GathererActorId`
- `NodeNetworkObjectId`
- `IntentType`

### GatherResult (server → client)
- `Allowed / Denied`
- `DenyReason` *(see below)*
- `GrantedOutputs[]` *(ItemId, Amount)*
- `NodeStateDelta` *(remaining harvests, depleted, respawn time)*

---

## DENY REASONS (LOCKED)

- `Denied_SceneDisallowsGathering`
- `Denied_NodeInvalid`
- `Denied_NodeDepleted`
- `Denied_RangeOrLoS`
- `Denied_ToolMissing`
- `Denied_SkillTooLow` *(future gate; optional in v0.1)*
- `Denied_InventoryFull`
- `Denied_StatusGated` *(e.g., stunned, pacified; aggregated gates)*

---

## GATHERING VALIDATION ORDER (SERVER, LOCKED)

1. Resolve `SceneRuleFlags`
2. If `ResourceGatheringAllowed == false` → deny
3. Resolve gatherer Actor + node Actor
4. Validate node is a gathering node (has runtime component)
5. Validate node not depleted
6. Validate range/LoS policy (per node type)
7. Validate tool requirement (if any)
8. Validate inventory space
9. Roll drops (deterministic)
10. Commit: grant items, decrement node durability/harvest-count, replicate state

---

## GATHERING NODE DEF — AUTHORITATIVE FIELDS

### Identity
- `NodeId` *(stable string id; never reuse)*
- `DisplayName`
- `NodeType`
- `Icon` *(optional; UI)*

### World/Runtime
- `Prefab` *(NetworkObject-capable)*
- `InteractionRangeMeters`
- `RequiresLineOfSight` *(bool)*

### Tool & Skill Gates
- `RequiredToolType` *(None/Axe/Pickaxe/Knife)*
- `PrimarySkillId` *(optional; e.g., Lumberjacking/Mining/Butchery)*
- `MinSkill` *(optional; default 0)*

### Harvest Budget
- `HarvestActionsMax` *(int; e.g., 6 swings on a tree)*
- `SecondsPerHarvestAction` *(float; server timer)*

### Drops
- `DropTableId` *(reference to DropTableDef)*
- `YieldMultiplierBySkill` *(policy hook; optional)*

### Depletion & Respawn
- `DepletionBehavior`
  - `DepleteAndRespawn`
  - `DepleteAndDespawn`
  - `Infinite` *(rare; water sources later)*

- `RespawnSeconds` *(only if DepleteAndRespawn)*

---

## DROP TABLE DEF — AUTHORITATIVE FIELDS

### Identity
- `DropTableId` *(stable string id; never reuse)*

### Roll Model (LOCKED)
- `RollMode`
  - `PerHarvestAction` *(default; each action rolls the table)*
  - `OnDepletion` *(roll once when node fully depleted; e.g., corpse carve)*

### Entries
Each entry is:
- `ItemId`
- `Weight` *(relative chance)*
- `QuantityMin`
- `QuantityMax`
- `RequiresSkillMin` *(optional)*
- `IsBonusRoll` *(optional; evaluated after base rolls)*

### Output Commit Policy (v0.1 lock)
- `GrantToInventory` *(default)*

---

## STARTER CONTENT SET (V0.1)

> These are starter definitions to unblock implementation and content authoring.

### Trees

#### Node: Pine Tree
- `NodeId`: `NODE_TREE_Pine`
- `NodeType`: Tree
- `RequiredToolType`: Axe
- `HarvestActionsMax`: 6
- `SecondsPerHarvestAction`: 1.2
- `DropTableId`: `DT_TREE_Pine`
- `RespawnSeconds`: 600

#### DropTable: Pine
RollMode: PerHarvestAction
- ITEM_RES_WoodLog (Weight 80, Qty 1–2)
- ITEM_RES_WoodenBranch (Weight 40, Qty 1–3)
- ITEM_RES_Bark (Weight 30, Qty 1–2)
- ITEM_RES_PlantFiber (Weight 15, Qty 1–2)

#### Node: Ancient Tree (Rare)
- `NodeId`: `NODE_TREE_Ancient`
- `NodeType`: Tree
- `RequiredToolType`: Axe
- `HarvestActionsMax`: 8
- `SecondsPerHarvestAction`: 1.4
- `DropTableId`: `DT_TREE_Ancient`
- `RespawnSeconds`: 1800

#### DropTable: Ancient
RollMode: PerHarvestAction
- ITEM_RES_HardwoodLog (Weight 70, Qty 1–2)
- ITEM_RES_AncientWoodLog (Weight 10, Qty 1–1)
- ITEM_RES_Bark (Weight 25, Qty 1–2)
- ITEM_RES_Vines (Weight 20, Qty 1–2)

### Ore Veins

#### Node: Iron Vein
- `NodeId`: `NODE_ORE_Iron`
- `NodeType`: OreVein
- `RequiredToolType`: Pickaxe
- `HarvestActionsMax`: 6
- `SecondsPerHarvestAction`: 1.3
- `DropTableId`: `DT_ORE_Iron`
- `RespawnSeconds`: 900

#### DropTable: Iron
RollMode: PerHarvestAction
- ITEM_RES_IronOre (Weight 70, Qty 1–2)
- ITEM_RES_StoneChunk (Weight 40, Qty 1–2)
- ITEM_RES_GraniteBlock (Weight 10, Qty 1–1)

#### Node: Obsidian Vein (Rare)
- `NodeId`: `NODE_ORE_Obsidian`
- `NodeType`: OreVein
- `RequiredToolType`: Pickaxe
- `HarvestActionsMax`: 5
- `SecondsPerHarvestAction`: 1.5
- `DropTableId`: `DT_ORE_Obsidian`
- `RespawnSeconds`: 2400

#### DropTable: Obsidian
RollMode: PerHarvestAction
- ITEM_RES_ObsidianOre (Weight 60, Qty 1–2)
- ITEM_RES_StoneChunk (Weight 30, Qty 1–2)
- ITEM_GEM_VoidCrystal (Weight 5, Qty 1–1) *(bonus-feel rare)*

### Corpses

> Corpses are Objects spawned on death (lootable + carveable).
> Carving uses `ToolType = Knife`.

#### Node: Humanoid Corpse (Generic)
- `NodeId`: `NODE_CORPSE_Humanoid`
- `NodeType`: Corpse
- `RequiredToolType`: Knife
- `HarvestActionsMax`: 1
- `SecondsPerHarvestAction`: 1.0
- `DropTableId`: `DT_CORPSE_Humanoid`
- `DepletionBehavior`: DepleteAndDespawn

#### DropTable: Humanoid Corpse
RollMode: OnDepletion
- ITEM_RES_Bone (Weight 60, Qty 1–2)
- ITEM_RES_BoneShard (Weight 40, Qty 1–3)
- ITEM_RES_Fat (Weight 25, Qty 1–1)

#### Node: Beast Corpse (Generic)
- `NodeId`: `NODE_CORPSE_Beast`
- `NodeType`: Corpse
- `RequiredToolType`: Knife
- `HarvestActionsMax`: 1
- `SecondsPerHarvestAction`: 1.2
- `DropTableId`: `DT_CORPSE_Beast`
- `DepletionBehavior`: DepleteAndDespawn

#### DropTable: Beast Corpse
RollMode: OnDepletion
- ITEM_RES_RawHide (Weight 70, Qty 1–2)
- ITEM_RES_FreshMeat (Weight 60, Qty 1–2)
- ITEM_RES_BoneShard (Weight 35, Qty 1–3)

#### Node: Spider Corpse
- `NodeId`: `NODE_CORPSE_Spider`
- `NodeType`: Corpse
- `RequiredToolType`: Knife
- `HarvestActionsMax`: 1
- `SecondsPerHarvestAction`: 1.2
- `DropTableId`: `DT_CORPSE_Spider`
- `DepletionBehavior`: DepleteAndDespawn

#### DropTable: Spider Corpse
RollMode: OnDepletion
- ITEM_RES_ChitinPlate (Weight 60, Qty 1–2)
- ITEM_RES_VenomSac (Weight 30, Qty 1–1)
- ITEM_REG_SpiderSilk (Weight 40, Qty 1–2)

---

## REQUIRED RUNTIME COMPONENTS (NEXT)

1. `GatheringNode` (NetworkBehaviour)
   - References `GatheringNodeDef`
   - Tracks remaining harvest actions
   - Replicates depleted/respawn state

2. `DropTableResolver` (server)
   - Deterministic roll evaluation
   - Produces granted outputs

3. `GatheringIntentHandler` (server)
   - Implements validation order and commits

4. `CorpseSpawner` integration
   - On death: spawn corpse object with optional carve node and separate loot container

---

## OPEN QUESTIONS (NOT LOCKED)

- Should tree/ore harvesting yield be **per action** only, or allow a guaranteed “base yield” + bonus rolls?
- Do you want **tool durability loss** on each harvest action?
- Should corpses support **multiple harvest actions** (meat + hide + bones) or a single “carve” roll?

---

## DESIGN LOCK CONFIRMATION

This document is **authoritative**.

Any change must:
- Increment Version
- Update Last Updated
- Call out impacted systems (Items, Loot, Crafting, Skills, World Spawns)

