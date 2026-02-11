# SCENE_RULE_PROVIDER.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 1.0  
Last Updated: 2026-02-11  
Engine: Unity 6 (URP)  
Authority: Server-authoritative (Shard Host)  

---

## PURPOSE

Defines the authoritative mechanism for declaring **SceneRuleContext** and **SceneRuleFlags** at runtime.

This document locks:
- How each scene declares its rule context
- How code queries scene legality gates
- The canonical contexts used by the project

This document is the single source of truth for **“what rules apply in this scene?”**.

---

## ALIGNMENT (LOCKED)

This document is aligned to:
- `ACTOR_MODEL.md` (scene contexts + legality gates)
- `HOUSING_RULES.md` (Village-only construction)
- `PLAYER_HOSTED_SHARDS_MODEL.md` (dungeons run on the shard host)

---

## SCOPE BOUNDARIES (NO OVERLAP)

Owned elsewhere:
- Actor legality & flags definition: `ACTOR_MODEL.md`
- Housing permissions: `HOUSING_RULES.md`
- Combat execution: `COMBAT_CORE.md`

This document does **not** define:
- How combat math works
- How building placement UI works
- Dungeon generation content

---

## CANONICAL CONTEXTS (LOCKED)

The project uses exactly these canonical contexts:

- `ShardVillage`
  - Safe social space
  - **Building allowed** (permissions still apply)
  - Vendors / economy

- `Dungeon`
  - Risk space
  - **PvP enabled**
  - Damage/death/durability/skill gain allowed

> Note: Under the player-hosted shard model, `Dungeon` scenes run inside the owning player’s shard.

---

## SCENE DECLARATION RULES (LOCKED)

1. Every gameplay scene must declare **exactly one** `SceneRuleContext`.
2. The declaration must be readable on the **server**.
3. If the context cannot be resolved, the server must default to the safest option:
   - `ShardVillage` (combat off, building off unless explicitly allowed)

---

## PROVIDER PATTERN (AUTHORITATIVE)

### SceneRuleProvider Component
Each scene must contain a `SceneRuleProvider` (or equivalent singleton object) that declares:
- `SceneRuleContext`
- Optional overrides (if supported later)

The provider is:
- Read by server systems at runtime
- Not trusted from clients

---

## RULE FLAG RESOLUTION

The `SceneRuleContext` resolves into `SceneRuleFlags` as defined by `ACTOR_MODEL.md`.

### Key invariants
- If `CombatAllowed == false`, hostile intents must be rejected.
- If `BuildingAllowed == false`, housing placement/removal intents must be rejected.

**Important:**
- `BuildingAllowed` means the *scene permits building*.
- Who can build is enforced by `HOUSING_RULES.md` (Owner/CoOwner/Editor).

---

## HOUSING LEGALITY (LOCKED)

Housing/build placement is legal only when:

1. `SceneRuleContext == ShardVillage`
2. `BuildingAllowed == true`
3. The actor has permission (Owner / CoOwner / Editor)

Any code path that checks for older contexts such as `MainlandHousing` is **invalid**.

---

## DUNGEON LEGALITY (LOCKED)

Dungeon scenes must declare:
- `SceneRuleContext == Dungeon`

Dungeon legality expectations:
- `CombatAllowed == true`
- `DamageAllowed == true`
- `DeathAllowed == true`
- `DurabilityLossAllowed == true`
- `SkillGainAllowed == true`
- `PvPAllowed == true`

---

## MULTI-SCENE / ADDITIVE LOADING

When using additive scenes:
- Exactly one loaded scene at a time must be designated as the **Primary Rule Source**.
- The Primary Rule Source determines the current `SceneRuleContext`.

Suggested approach (non-binding):
- The server sets the Primary Rule Source when it completes a scene transition.

If Primary Rule Source is ambiguous:
- Default to `ShardVillage`.

---

## VALIDATION (RECOMMENDED)

At server scene-load time, validate:
- A provider exists
- Context is one of the canonical values
- The resolved flags match the expected template for that context

On validation failure:
- Log an error
- Fall back to `ShardVillage`

---

## REQUIRED UPDATES TO DOCUMENTS_INDEX.md (PATCH)

Update the entry for `SCENE_RULE_PROVIDER.md` to reflect:
- Canonical contexts: `ShardVillage`, `Dungeon`
- Housing legality tied to `ShardVillage`
- Removal of `MainlandHousing` assumptions

---

## DESIGN LOCK CONFIRMATION

This document is **authoritative**.

Any change must:
- Increment Version
- Update Last Updated
- Explicitly call out impacts to scene legality gates, housing legality, or additive scene resolution

