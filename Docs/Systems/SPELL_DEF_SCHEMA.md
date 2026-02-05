# SPELL_DEF_SCHEMA.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 1.0\
Last Updated: 2026-01-28

---

## PURPOSE

Defines the **authoritative data contract** for `SpellDef` ScriptableObjects in *Ultimate Dungeon*.

`SpellDef` is the single source of truth for:

- Spell requirements (skills, mana, reagents)
- Cast timing (cast time, cooldown)
- Targeting (mode, range, LoS)
- Resolution payload (damage, status effects, utility)

This schema must be locked **before** implementing:

- Spellbook learning
- Spellcasting pipeline/state machine
- Combat math
- Item affixes that modify magic (Faster Casting, LMC, SDI)

---

## DESIGN LOCKS (MUST ENFORCE)

1. \*\*Spell identity is \*\***`SpellId`**

   - Spell IDs are defined in `SPELL_ID_CATALOG.md`.
   - `SpellDef.spellId` must match an enum entry.

2. **Server authoritative**

   - Validation, cooldowns, interruption, and payload application occur on the server.

3. **Deterministic resolution**

   - Any roll (hit/fizzle/resist/damage range) must use a deterministic server seed.

4. **Explicit targeting**

   - Every spell declares exactly one targeting mode.

5. **Effects are data-driven**

   - No hardcoded per-spell behaviors in code.
   - Code interprets a structured payload.

---

## PRIMARY ASSET

### Asset Type

- `SpellDef : ScriptableObject`

### Asset Naming Convention

- `SpellDef_<Circle>_<SpellName>`
  - Example: `SpellDef_C3_Fireball`

---

## REQUIRED FIELDS (AUTHORITATIVE)

### 1) Identity

- `SpellId spellId`
- `string displayName`
- `int circle` *(1–8)*
- `string shortDescription` *(UI tooltip)*

### 2) Requirements

- `float requiredMagery`

- `float requiredEvaluatingIntelligence` *(optional; 0 if unused)*

- `int manaCost`

- `ReagentCost[] reagents`

  - Each entry includes:
    - `ReagentId reagentId`
    - `int amount`

> **LOCKED:** Requirements are validated server-side before casting begins.

### 3) Timing

- `float baseCastTimeSeconds`
- `float cooldownSeconds`

> **LOCKED:**
>
> - Cast time is a *commitment window*.
> - Cooldown is per-spell and tracked server-side.

### 4) Targeting

- `SpellTargetingMode targetingMode`

  - `Self`
  - `SingleTarget`
  - `GroundTarget`
  - `AreaAroundTarget`
  - `AreaAroundCaster`

- `float rangeMeters`

  - Used for `SingleTarget`, `GroundTarget`, `AreaAroundTarget`

- `float areaRadiusMeters`

  - Used for AoE modes (`AreaAroundTarget`, `AreaAroundCaster`)

- `bool requiresLineOfSight`

  - Default `true` for most targeted spells

- `TargetFilter targetFilter`

  - `AnyActor`
  - `FriendlyOnly`
  - `HostileOnly`
  - `SelfOnly` *(only valid if targetingMode=Self)*
  - `WorldOnly` *(only valid if targetingMode=GroundTarget)*

> **LOCKED:** Target validation occurs on the server at cast start and again at cast completion.

### 5) Interruptibility

- `bool interruptedByDamage`

- `bool interruptedByMovement`

- `bool interruptedByStunOrParalyze`

- `float damageInterruptThreshold`

  - Minimum damage required to force an interruption (0 = any damage)

> **LOCKED DEFAULTS (recommended):**
>
> - `interruptedByDamage = true`
> - `interruptedByMovement = true`
> - `interruptedByStunOrParalyze = true`
> - `damageInterruptThreshold = 0`

### 6) Resolution Payload

A spell may include one or more payload entries. Payload entries resolve **in order**.

- `SpellPayloadEntry[] payload`

Each `SpellPayloadEntry` has:

- `SpellPayloadType type`

  - `DirectDamage`
  - `Heal`
  - `ApplyStatus`
  - `RemoveStatus`
  - `Dispel`
  - `Teleport`
  - `FieldSpawn`
  - `Summon`
  - `Reveal`
  - `Unlock`
  - `Lock`
  - `UtilityCustom` *(reserved; avoid until necessary)*

- `TargetingOverride targetOverride` *(optional)*

  - Default: uses spell’s targeting result.
  - Allows certain payloads to apply to `Caster`, `PrimaryTarget`, `AllInArea`, etc.

---

## PAYLOAD TYPE DETAILS (AUTHORITATIVE)

### A) DirectDamage

Fields:

- `DamageType damageType` *(Physical, Fire, Cold, Poison, Energy)*
- `int minDamage`
- `int maxDamage`
- `float evalIntScaling` *(multiplier; 0 = none)*
- `float mageryScaling` *(multiplier; 0 = none)*
- `bool canBeResisted` *(if true, Resist Spells reduces damage)*

Rules:

- Damage roll is deterministic and server-seeded.
- Resistances mitigate by damageType.
- `Resist Spells` may reduce damage when `canBeResisted` is true.

### B) Heal

Fields:

- `int minHeal`
- `int maxHeal`
- `float evalIntScaling`
- `float mageryScaling`

Rules:

- Heal is deterministic and server-seeded.
- Healing cannot exceed target MaxHP.

### C) ApplyStatus

Fields:

- `StatusEffectId statusId`

- `int baseDurationSeconds`

- `int baseMagnitude`

- `StackRule stackRule` *(Refresh, AddStack, Replace, IgnoreIfPresent)*

- `bool canBeResisted`

- `float resistScalar` *(how strongly Resist Spells affects application chance/duration)*

Rules:

- Status application is server-authoritative.
- `canBeResisted` delegates to Resist Spells logic.

### D) RemoveStatus

Fields:

- `StatusEffectId statusId`
- `RemoveRule removeRule` *(RemoveAllStacks, RemoveOneStack, RemoveIfMagnitudeBelow, etc.)*

### E) Dispel

Fields:

- `DispelStrength strength` *(Lesser, Normal, Greater)*
- `TargetTag[] dispellableTags` *(Summon, Field, Buff, Debuff, etc.)*

### F) Teleport

Fields:

- `TeleportMode mode` *(ToGroundPoint, ToMarkedLocation, RecallHome, GateTravel)*
- `bool blockedByNoTeleportZones`

### G) FieldSpawn

Fields:

- `FieldType fieldType` *(FireField, PoisonField, ParalyzeField, EnergyField, WallOfStone)*
- `float fieldLengthMeters`
- `int fieldDurationSeconds`
- `bool blocksMovement`
- `bool damagesOnTouch`

### H) Summon

Fields:

- `SummonId summonId` *(BladeSpirits, EnergyVortex, AirElemental, etc.)*
- `int durationSeconds`
- `bool controllable`

### I) Reveal

Fields:

- `float radiusMeters`
- `int revealDurationSeconds`

### J) Unlock / Lock

Fields:

- `int power`
- `bool worksOnPlayerContainers`

---

## REAGENTS (AUTHORITATIVE MODEL)

### ReagentId (required enum)

At minimum (UO baseline):

- Pearl
- Moss
- Garlic
- Ginseng
- Root
- Shade
- Ash
- Silk

### ReagentCost struct

- `ReagentId reagentId`
- `int amount`

Rules:

- Reagents are consumed on **successful cast completion**.
- On fizzle/failure:
  - Reagents are still consumed (UO-like failure cost)

---

## FAILURE / FIZZLE (AUTHORITATIVE)

Fields:

- `float baseFizzleChance` *(recommended 0; computed via Magery vs requiredMagery)*
- `bool consumeManaOnFizzle` *(default true)*
- `bool consumeReagentsOnFizzle` *(default true)*

Rules:

- If a cast fails:
  - No payload resolves
  - Costs apply per flags

---

## COOLDOWN MODEL (AUTHORITATIVE)

Fields:

- `float cooldownSeconds`

Rules:

- Cooldown begins when:
  - Cast completes (success) OR
  - Cast fizzles (failure)
- Interrupted casts do **not** start cooldown (recommended), unless a spell explicitly overrides.

Optional override field:

- `CooldownStartPolicy cooldownStartPolicy`
  - `OnSuccessOrFizzle`
  - `OnAnyEnd` *(includes interruption)*

---

## EXAMPLE SPELL DEFINITIONS (REFERENCE)

These examples are not full numeric balance; they illustrate how the schema is used.

### Example 1 — Fireball (Circle 3)

- targetingMode: `SingleTarget`
- manaCost: moderate
- reagents: `Pearl`, `Ash`
- payload:
  1. `DirectDamage` (Fire, min/max)

### Example 2 — Greater Heal (Circle 4)

- targetingMode: `SingleTarget`
- reagents: `Ginseng`, `Garlic`
- payload:
  1. `Heal`

### Example 3 — Paralyze (Circle 5)

- targetingMode: `SingleTarget`
- reagents: Moss, Ash, Silk
- payload:
  1. `ApplyStatus` (Paralyzed, duration, resistible)

---

## IMPLEMENTATION CHECKLIST (NEXT)

1. Create `SpellId.cs` from `SPELL_ID_CATALOG.md`
2. Create `ReagentId.cs`
3. Implement `SpellDef` ScriptableObject with this schema
4. Implement `SpellRegistry` (SpellId → SpellDef)
5. Implement server spellcasting pipeline:
   - validate → cast state → interrupt checks → resolve payload → cooldown
6. Implement Spellbook learning:
   - scroll unlock → persisted learned spell flags

---

## DESIGN LOCK CONFIRMATION

This schema is **authoritative**.

Changes to fields or semantics must:

- Increment Version
- Update Last Updated
- Call out breaking implications for save data and combat math

