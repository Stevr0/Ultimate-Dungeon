# ACTOR_MODEL.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 1.2  
Last Updated: 2026-02-11  
Engine: Unity 6 (URP)  
Networking: Netcode for GameObjects (NGO)  
Authority: Server-authoritative (Shard Host)  

---

## PURPOSE

Defines the authoritative **Actor runtime model** for *Ultimate Dungeon*.

An **Actor** is any runtime entity that can be:
- Identified
- Targeted
- Interacted with
- (Optionally) damaged or killed

**Combat Core never decides what an Actor is.**  
Combat Core consumes Actors only after **Actor rules** validate legality.

---

## SCOPE BOUNDARIES (NO OVERLAP)

### This document owns
- Actor identity and classification (`ActorType`)
- Faction and relationship rules (`FactionId`, hostility / neutrality)
- Targetability and interaction masks (intent gating)
- **SceneRuleContext**: what rules apply in the current scene
- Scene hard-flags (combat/damage/death/build legality)
- Combat legality gates (Allowed / Denied reason codes)
- Combat state ownership at the Actor level (Alive / Dead / InCombat)

### This document does NOT own
- Combat math / hit / damage resolution *(see `COMBAT_CORE.md`)*
- Player combat stat aggregation *(see `PLAYER_COMBAT_STATS.md`)*
- Spell payload semantics *(see spell/status docs)*
- Item schemas / affix schemas *(see item docs)*
- Housing content rules *(see `HOUSING_RULES.md`)*

---

## DESIGN LOCKS (MUST ENFORCE)

1. **Actor-first**
   - Players, Monsters, NPCs are all Actors.
   - No special-casing inside combat or targeting pipelines.

2. **Server authoritative**
   - Clients submit intent.
   - Server validates intent against Actor + Scene rules.

3. **Scene rules are hard gates**
   - If a scene disallows an action, the server refuses it.
   - Clients may not bypass legality locally.

4. **PvE and PvP share a pipeline**
   - Once legality is confirmed, execution is shared.

5. **Status-first integrity**
   - Status effects may modify what an Actor can do.
   - Actor rules do not inspect individual statuses; they consume aggregated gates.

---

## CORE TYPES

### ActorType (LOCKED)

- `Player`
- `Monster`
- `NPC`
- `Vendor`
- `Pet`
- `Summon`
- `Object` *(doors, chests, traps, shrines, house pieces, etc.)*

> `Vendor` is intentionally separate from `NPC` to support safe-scene commerce rules.

---

### FactionId (LOCKED MODEL)

Actors belong to a faction.

Examples:
- `Players`
- `Monsters`
- `Village`
- `Neutral`
- `Guards`

Faction relationships are resolved by a **FactionRelationshipTable** (data-driven).

Relationship outcomes:
- Friendly
- Neutral
- Hostile

---

## SCENE RULE CONTEXT (AUTHORITATIVE)

### SceneRuleContext (LOCKED)

Every loaded scene must declare exactly one `SceneRuleContext`.

- `ShardVillage` *(safe + build zone: housing, vendors, social)*
- `Dungeon` *(danger: PvE/PvP, loot, progression)*

**SceneRuleContext is server authority.**
- The shard host determines the current scene context.
- Clients may display different UI/camera, but may not bypass server gates.

> Note: Under the player-hosted shard model, `Dungeon` scenes run on the same shard host.

---

### SceneRuleFlags (LOCKED)

Each `SceneRuleContext` resolves to a set of hard flags.

| Flag | ShardVillage | Dungeon |
|---|---:|---:|
| `CombatAllowed` | ❌ | ✅ |
| `DamageAllowed` | ❌ | ✅ |
| `DeathAllowed` | ❌ | ✅ |
| `DurabilityLossAllowed` | ❌ | ✅ |
| `ResourceGatheringAllowed` | ❌ | ✅ |
| `SkillGainAllowed` | ❌ | ✅ |
| `HostileActorsAllowed` | ❌ | ✅ |
| `PvPAllowed` | ❌ | ✅ *(subject to Actor legality)* |
| `BuildingAllowed` | ✅ *(Owner-permissions apply)* | ❌ |
| `VendorTradeAllowed` | ✅ | ✅ *(rare; content-driven)* |

**Design lock:** If a flag is ❌, the server must refuse all intents that would violate it.

`BuildingAllowed` does not grant permission by itself; it only means the *scene* permits building.  
Who can build is owned by `HOUSING_RULES.md`.

---

### Allowed Camera Modes (DESIGN SUPPORT)

Camera is a client presentation layer. Scene flags are the authority layer.

- ShardVillage: `TopDown` *(optionally allow limited zoom/tilt)*
- Dungeon: `TopDown`

---

## ACTOR BEHAVIOR BY SCENE (LOCKED)

### ShardVillage (SAFE + BUILD)
**Intent:** social, housing, economy.

**Actors allowed**
- Players
- Vendors
- NPCs (non-hostile)
- Objects (doors, containers, house pieces, deco)

**Actors forbidden**
- Hostile Monsters
- Any Actor with hostile relationship to Players

**Behavior constraints**
- Players cannot:
  - Start attacks
  - Apply harmful spells
  - Force hostile combat states
  - Cause durability loss
  - Gain skills
- Players can:
  - Select/target non-hostile
  - Interact/use
  - Trade
  - Build/modify housing **if housing permissions allow**

---

### Dungeon (DANGER)
**Intent:** core gameplay (loot, risk, PvE/PvP).

**Actors allowed**
- Players
- Monsters
- NPCs (neutral/hostile depending on content)
- Vendors *(rare: dungeon merchants if designed)*
- Objects (doors, traps, chests, shrines)
- Summons / Pets

**Behavior constraints**
- Players can:
  - Attack hostile actors if legality allows
  - Be attacked
  - Take damage / die
  - Lose durability
  - Gain skills
- PvP:
  - Enabled by scene flag
  - Still subject to Actor legality and faction/relationship rules

---

## COMBAT STATE (LOCKED)

### CombatState

- `Peaceful` *(not in combat)*
- `InCombat` *(recent hostile action)*
- `Dead`

**Rules**
- `Dead` Actors cannot submit or receive combat actions.
- SceneRuleContext must be checked before any transition into `InCombat`.
  - If `CombatAllowed == false`, an Actor may never enter `InCombat`.

---

## TARGETING & INTERACTION MASKS (LOCKED)

Actor rules gate **what targeting intents are valid**, independent of UI.

### TargetIntentType
- `Select` *(non-hostile selection)*
- `Interact` *(use/open/talk)*
- `Attack` *(hostile)*
- `CastBeneficial`
- `CastHarmful`

Scene flags + Actor legality determine whether each intent can proceed.

---

## REQUIRED UPDATES TO DOCUMENTS_INDEX.md (PATCH)

Update the `ACTOR_MODEL.md` description if needed to reflect:
- `ShardVillage` replacing prior housing/hub contexts
- Added `BuildingAllowed` as a scene legality flag (permission owned by housing)

---

## DESIGN LOCK CONFIRMATION

This document is **authoritative**.

Any change must:
- Increment Version
- Update Last Updated
- Explicitly call out impacts to scene legality gates or targeting/combat legality

