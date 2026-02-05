# ACTOR_MODEL.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 1.1  
Last Updated: 2026-01-29  
Engine: Unity 6 (URP)  
Networking: Netcode for GameObjects (NGO)  
Authority: Server-authoritative  

---

## PURPOSE

Defines the authoritative **Actor runtime model** for *Ultimate Dungeon*.

An **Actor** is any runtime entity that can be:
- Identified
- Targeted
- Interacted with
- (Optionally) damaged or killed

**Combat Core never decides what an Actor is.**  
Combat Core consumes Actors after **Actor rules** validate legality.

---

## SCOPE BOUNDARIES (NO OVERLAP)

### This document owns
- Actor identity and classification (`ActorType`)
- Faction and relationship rules (`FactionId`, hostility / neutrality)
- Targetability and interaction masks
- **SceneRuleContext**: what rules apply in the current Scene
- Combat legality gates (Allowed / Denied reason codes)
- Combat state ownership at the Actor level (Alive / Dead / InCombat, etc.)

### This document does NOT own
- Combat math / hit / damage resolution *(see `COMBAT_CORE.md`)*
- Player stat aggregation *(see `PLAYER_COMBAT_STATS.md`)*
- Spell mechanics *(see `SPELL_CATEGORY_MODEL.md`)*
- Status effect definitions *(see `STATUS_EFFECT_CATALOG.md`)*
- Items, affixes, durability schemas *(see item docs)*

---

## DESIGN LOCKS (MUST ENFORCE)

1. **Actor-first**
   - Players, Monsters, NPCs are all Actors.
   - No special-casing inside combat or targeting pipelines.

2. **Server authoritative**
   - Clients submit intent.
   - Server validates intent against Actor + Scene rules.

3. **Scene rules are hard gates**
   - If a scene disallows an action (combat, damage, etc.), the server refuses it.
   - Clients may not “opt in” locally.

4. **PvE and PvP share a pipeline**
   - Once legality is confirmed, execution is shared.

5. **Status-first integrity**
   - Status effects can modify what an Actor can do.
   - Actor rules do not inspect individual statuses; they consume aggregated gates.

---

## CORE TYPES

### ActorType (LOCKED)
Defines the broad category of Actor.

- `Player`
- `Monster`
- `NPC`
- `Vendor`
- `Pet`
- `Summon`
- `Object` *(doors, chests, traps, shrines, etc.)*

> NOTE: `Vendor` is intentionally separate from `NPC` to support safe-scene commerce rules.

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

## NEW: SCENE RULE CONTEXT (AUTHORITATIVE)

### SceneRuleContext (LOCKED)
Every loaded scene must declare exactly one `SceneRuleContext`.

- `MainlandHousing` *(safe: housing + vendors, immersion camera)*
- `HotnowVillage` *(safe hub: shops, banking, logistics)*
- `Dungeon` *(danger: PvE/PvP, progression, loot)*

**SceneRuleContext is server authority.**
- The server determines the current scene context.
- Clients may display different UI/camera, but may not bypass server gates.

---

### SceneRuleFlags (LOCKED)
Each `SceneRuleContext` resolves to a set of hard flags.

| Flag | MainlandHousing | HotnowVillage | Dungeon |
|---|---:|---:|---:|
| `CombatAllowed` | ❌ | ❌ | ✅ |
| `DamageAllowed` | ❌ | ❌ | ✅ |
| `DeathAllowed` | ❌ | ❌ | ✅ |
| `DurabilityLossAllowed` | ❌ | ❌ | ✅ |
| `ResourceGatheringAllowed` | ❌ | ❌ | ✅ |
| `SkillGainAllowed` | ❌ | ❌ | ✅ |
| `HostileActorsAllowed` | ❌ | ❌ | ✅ |
| `PvPAllowed` | ❌ | ❌ | ✅ *(subject to Actor legality)* |

**Design lock:** If a flag is ❌, the server must refuse all intents that would violate it.

---

### Allowed Camera Modes (DESIGN SUPPORT)
Camera is not owned by Actor rules, but scenes must still declare what camera modes are allowed for clarity.

- MainlandHousing: `FirstPerson`, `ThirdPerson`
- HotnowVillage: `TopDown` *(optionally allow limited zoom/tilt later, but still non-combat)*
- Dungeon: `TopDown`

> Camera is a client presentation layer. Scene flags are the authority layer.

---

## ACTOR BEHAVIOR BY SCENE (LOCKED)

This section defines **how the same ActorType behaves differently** depending on `SceneRuleContext`.

### MainlandHousing (SAFE)
**Intent:** social, housing, economy, immersion.

**Actors allowed**
- Players
- Vendors (player vendors + NPC vendors, if needed)
- NPCs (non-hostile)
- Objects (doors, chests, house pieces, deco)

**Actors forbidden**
- Hostile Monsters
- Any Actor with hostile relationship to Players

**Behavior constraints**
- Players cannot:
  - Start attacks
  - Apply harmful spells
  - Perform hostile targeting actions
  - Cause durability loss
  - Gain skills
- Vendors can:
  - Trade
  - List items for sale
  - Receive coins (banked-only rules belong to economy docs)
- Objects can:
  - Be used/interacted with
  - Be placed/constructed (building system)
  - Be locked down (housing permissions)

---

### HotnowVillage (SAFE HUB)
**Intent:** spawn hub, preparation, services.

**Actors allowed**
- Players
- NPCs
- Vendors
- Objects (bank, crafting stations, doors)

**Actors forbidden**
- Hostile Monsters
- PvP hostility

**Behavior constraints**
- Players cannot:
  - Start attacks
  - Apply harmful spells
  - Force hostile combat states
  - Cause durability loss
  - Gain skills
- Players can:
  - Target/select (non-hostile selection)
  - Interact/use
  - Trade
  - Bank / insure / repair (systems outside this doc)

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
- Monsters:
  - Are usually hostile to Players, but this is data-driven

---

## COMBAT STATE (LOCKED)

### CombatState
Represents server-authoritative combat posture.

- `Peaceful` *(not in combat)*
- `InCombat` *(recent hostile action)*
- `Dead`

**Rules**
- `Dead` Actors cannot submit or receive combat actions.
- SceneRuleContext must be checked before any state transition into `InCombat`.
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

**Scene gates**
- In safe scenes (`CombatAllowed == false`):
  - `Attack` and `CastHarmful` must be refused.

---

## ATTACK LEGALITY (AUTHORITATIVE)

### AttackLegalityResult
Server returns a structured result for any hostile attempt.

- `Allowed`
- `Denied_SceneDisallowsCombat`
- `Denied_SceneDisallowsDamage`
- `Denied_TargetNotHostile`
- `Denied_TargetNotAttackable`
- `Denied_AttackerDead`
- `Denied_TargetDead`
- `Denied_PvPNotAllowed`
- `Denied_RangeOrLoS`
- `Denied_StatusGated` *(e.g., stunned, pacified, etc. via aggregated gates)*

**Design lock:** `COMBAT_CORE.md` must require `Allowed` before scheduling any swing/cast damage resolution.

---

## REQUIRED IMPLEMENTATION ARTIFACTS (NEXT)

1. `SceneRuleContext` provider (server) that exposes flags to all validation systems
2. `FactionRelationshipTable` (ScriptableObject)
3. `AttackLegalityResolver` (pure rules)
4. `TargetIntentValidator` (pure rules)
5. `CombatStateTracker` integration (combat entry forbidden in safe scenes)

---

## DESIGN LOCK CONFIRMATION

This document is **authoritative**.

Any change must:
- Increment Version
- Update Last Updated
- Call out impacted dependent systems (Targeting, Combat Core, UI, Housing)
