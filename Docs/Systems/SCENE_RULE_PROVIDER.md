# SCENE_RULE_PROVIDER.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 0.1  
Last Updated: 2026-01-29  
Engine: Unity 6 (URP)  
Networking: Netcode for GameObjects (NGO)  
Authority: Server-authoritative  

---

## PURPOSE

Defines the authoritative runtime mechanism that exposes **SceneRuleContext** and **SceneRuleFlags** to all server systems.

This document exists to ensure:
- Safe scenes can never accidentally allow combat
- Every validation pipeline has one shared source of truth
- Scene transitions clear illegal state (combat, selection, etc.) deterministically

This doc is the **implementation contract** between:
- Scene loading / bootstrap
- Actor legality
- Targeting validation
- Combat execution
- Housing placement

---

## DEPENDENCIES (MUST ALIGN)

- `ACTOR_MODEL.md`
  - `SceneRuleContext`
  - `SceneRuleFlags` table

- `TARGETING_MODEL.md`
  - Intent gating by scene

- `COMBAT_CORE.md`
  - Mandatory scene gate before scheduling and at resolution

- `HOUSING_RULES.md`
  - Housing allowed only in `MainlandHousing`

---

## DESIGN LOCKS (MUST ENFORCE)

1. **Server is source of truth**
   - SceneRuleContext is decided by the server.
   - Clients may display UI/camera changes, but cannot override rules.

2. **Exactly one active SceneRuleContext per scene instance**
   - No mixed contexts.

3. **SceneRuleFlags are immutable during runtime**
   - Once a scene is loaded and context selected, flags do not change until a scene transition.

4. **All gameplay pipelines must consume SceneRuleFlags**
   - Actor legality
   - Targeting
   - Combat Core
   - Spellcasting damage/harm validation
   - Housing placement

5. **Scene transitions clear illegal state**
   - Leaving a dungeon must clear combat scheduling and hostile state.
   - Entering a safe scene must guarantee peaceful state.

---

## AUTHORITATIVE DATA

### SceneRuleContext (from ACTOR_MODEL.md)
- `MainlandHousing`
- `HotnowVillage`
- `Dungeon`

### SceneRuleFlags (canonical set)
- `CombatAllowed`
- `DamageAllowed`
- `DeathAllowed`
- `DurabilityLossAllowed`
- `ResourceGatheringAllowed`
- `SkillGainAllowed`
- `HostileActorsAllowed`
- `PvPAllowed`

> The canonical mapping from context → flags is defined in `ACTOR_MODEL.md`.

---

## RUNTIME MODEL (LOCKED)

### SceneRuleProvider
Each scene must contain exactly one provider object:

- `SceneRuleProvider` (server authoritative component)
  - Declares `SceneRuleContext`
  - Exposes resolved `SceneRuleFlags`
  - Provides a global access point for server systems

**Rules**
- If a scene loads without a provider, the server must fail fast (log error and prevent gameplay start).
- If more than one provider exists, the server must fail fast.

---

## ACCESS PATTERN (LOCKED)

All runtime systems must access scene flags through a single path:

- `SceneRuleRegistry.Current` *(server singleton created at scene boot)*
  - holds the active `SceneRuleContext`
  - holds the active `SceneRuleFlags`

**No system is allowed** to hardcode safe/dungeon checks.

---

## REQUIRED SERVER VALIDATION HOOKS (LOCKED)

### Actor Legality
- `AttackLegalityResolver` must refuse if `CombatAllowed == false`.

### Targeting
- `TargetIntentValidator` must refuse:
  - `Attack` if `CombatAllowed == false`
  - `CastHarmful` if `CombatAllowed == false`

### Combat Core
- Must refuse scheduling if `CombatAllowed == false`
- Must re-gate at timer completion using flags

### Spellcasting
- Harmful spells must require `CombatAllowed == true`
- Damage application must require `DamageAllowed == true`

### Housing
- Placement must require `SceneRuleContext == MainlandHousing`

---

## SCENE TRANSITION RULES (LOCKED)

When the server transitions a player between scenes:

### Always clear
- Current selection (target)
- Any queued combat actions / swing timers
- Any in-progress cast channel timers (if applicable)
- Any combat state (`InCombat` → `Peaceful`)

### Safe scene entry guarantee
When entering `MainlandHousing` or `HotnowVillage`:
- Player is forced to `CombatState.Peaceful`
- Player cannot enter `InCombat` while in the scene

### Dungeon entry guarantee
When entering `Dungeon`:
- Player starts `Peaceful`
- Legality is determined normally

---

## NETWORKING / CLIENT MIRROR (LOCKED)

Clients must be informed of the current SceneRuleContext to drive:
- Camera mode
- UI gating (disable attack buttons, hide hostile reticles)
- Tutorial/help messaging

**Important:** Client UI is informational only.

### Replicated payload (minimum)
- `SceneRuleContext`
- Optional: a compact bitmask of `SceneRuleFlags`

---

## FAILURE MODES (LOCKED)

The server must treat these as critical configuration errors:

- Scene has no `SceneRuleProvider`
- Scene has multiple `SceneRuleProvider` components
- Provider references an unknown `SceneRuleContext`

Policy:
- Log error
- Prevent session start or kick players back to Hotnow Village

---

## REQUIRED IMPLEMENTATION ARTIFACTS (NEXT)

1. `SceneRuleProvider` MonoBehaviour (server)
2. `SceneRuleRegistry` singleton (server)
3. Scene bootstrap validation (assert exactly one provider)
4. Scene transition hook that clears illegal state
5. Client mirror message (context only; flags optional)

---

## DESIGN LOCK CONFIRMATION

This document is **authoritative**.

Any change must:
- Increment Version
- Update Last Updated
- Call out impacted dependent systems (Actor, Targeting, Combat, Housing, UI)

