# PLAYER_COMBAT_STATS.md — ULTIMATE DUNGEON (AUTHORITATIVE)

Version: 1.0  
Last Updated: 2026-01-28  
Engine: Unity 6 (URP)  
Networking: Netcode for GameObjects (NGO)  
Authority: Server-authoritative  
Determinism: Required (server-seeded)

---

## PURPOSE

Defines the **authoritative Player Combat Stat Aggregation** rules for *Ultimate Dungeon*.

This document answers one question:

> **How do we compute the player’s final combat-relevant stats**
> from PlayerDefinition + skills + equipped items/affixes + status effects?

Combat code (CombatResolver, SwingTimer, Spellcasting, Bandaging) must **never** reach into items/skills directly.
Instead, combat queries a single authoritative snapshot:

- `PlayerCombatStats` (server-owned)

If an aggregation rule is not defined here, **it does not exist**.

---

## DESIGN LOCKS (MUST ENFORCE)

1. **Server authoritative**
   - Only the server computes and owns combat stats.

2. **Deterministic**
   - Stat computation is pure (no RNG).
   - RNG is only used for hit rolls, damage rolls, procs, etc.

3. **Separation of concerns**
   - Items contribute **numbers** (affixes/modifiers).
   - Status effects contribute **gates and multipliers**.
   - Combat reads only the aggregated result.

4. **No hidden stacking**
   - Every stat must declare an aggregation policy:
     - Sum
     - HighestOnly
     - Multiply
     - Clamp

5. **Caps are enforced centrally**
   - Caps are applied in the aggregator, not sprinkled across combat systems.

---

## INPUTS (AUTHORITATIVE SOURCES)

The aggregator may read ONLY from:

1. **PlayerDefinition** (base constants)
   - Baseline hit/defense
   - Unarmed profile
   - Resist cap

2. **PlayerStats**
   - Effective STR/DEX/INT (including item/status attribute mods)

3. **PlayerSkillBook**
   - Base trained skill values (0–100)

4. **Equipment / ItemInstances**
   - Equipped items
   - RolledAffixes[]
   - Base weapon/armor data (from ItemDef)

5. **StatusEffectSystem**
   - Action blocks (stun/paralyze/silence, etc.)
   - Multipliers (MoveSpeed, SwingSpeed, CastSpeed)

---

## OUTPUT: PLAYERCOMBATSTATS SNAPSHOT

The server produces a single snapshot struct/class:

### Core Combat Chances
- `float hitChance` *(0..1)*
- `float defenseChance` *(0..1)*

### Damage Modifiers
- `float damageIncreasePct` *(0..1; e.g. 0.25 = +25%)*

### Swing/Cast/Bandage Timing
- `float swingSpeedMultiplier` *(default 1.0; lower = faster OR higher = faster? see LOCK below)*
- `float castSpeedMultiplier`
- `float bandageSpeedMultiplier`

### Resistances (percent values, already capped)
- `int resistPhysical`
- `int resistFire`
- `int resistCold`
- `int resistPoison`
- `int resistEnergy`

### Weapon Context (resolved from equipment)
- `WeaponProfile weapon` (see Weapon Profile block)

### Proc Context (resolved from affixes)
- `ProcProfile procs` *(hit spells / leaches)*

### Action Gates (derived from statuses)
- `bool canAttack`
- `bool canCast`
- `bool canBandage`
- `bool canMove`

---

## IMPORTANT LOCK: MULTIPLIER SEMANTICS

To avoid confusion, all speed multipliers use the same semantic:

**Rule (LOCKED):**
- Multipliers scale **time** (seconds).
- `FinalTimeSeconds = BaseTimeSeconds * TimeMultiplier`

Meaning:
- `TimeMultiplier = 1.0` → no change
- `TimeMultiplier = 0.8` → 20% faster (shorter time)
- `TimeMultiplier = 1.2` → 20% slower (longer time)

So this doc uses:
- `swingTimeMultiplier`
- `castTimeMultiplier`
- `bandageTimeMultiplier`

(Names in code can vary, but semantics must match.)

---

## BASELINES (FROM PLAYER_DEFINITION)

From `PLAYER_DEFINITION.md`:
- `baseHitChance = 0.50`
- `baseDefenseChance = 0.50`
- Resist cap = `70`
- Unarmed profile:
  - `baseSwingSpeedSeconds = 2.0`
  - `unarmedDamageRange = 1..4`

---

## AGGREGATION RULES — STAT BY STAT (AUTHORITATIVE)

### 1) HIT CHANCE (HCI)

#### Inputs
- `baseHitChance` (PlayerDefinition)
- `HCI%` from item affixes
- (Future) skill-based HCI contributions

#### Aggregation
- `hciPct = Sum(all equipped Combat_HitChance affixes)`
- `hciPct = Clamp(hciPct, 0, HCI_CAP)`

#### Cap (PROPOSED — Not Locked)
- `HCI_CAP = 0.45` *(+45%)*

#### Output
- `attackerHitChanceBase = baseHitChance + hciPct`

> CombatResolver applies defender DCI by subtracting (see Combat Core):
> `FinalHitChance = clamp01(baseHitChance + (AttackerHCI - DefenderDCI))`
>
> This doc defines how to compute AttackerHCI and DefenderDCI.

---

### 2) DEFENSE CHANCE (DCI)

#### Inputs
- `baseDefenseChance` (PlayerDefinition)
- `DCI%` from item affixes

#### Aggregation
- `dciPct = Sum(all equipped Combat_DefenseChance affixes)`
- `dciPct = Clamp(dciPct, 0, DCI_CAP)`

#### Cap (PROPOSED — Not Locked)
- `DCI_CAP = 0.45`

#### Output
- `defenderDefenseChanceBase = baseDefenseChance + dciPct`

---

### 3) DAMAGE INCREASE (DI)

#### Inputs
- `DI%` from item affixes (`Combat_DamageIncrease`)
- (Future) skill-based DI (Tactics/Anatomy) — see SKILL_COMBAT_INTEGRATION.md

#### Aggregation
- `diPct = Sum(all equipped Combat_DamageIncrease affixes)`
- `diPct = Clamp(diPct, 0, DI_CAP)`

#### Cap (PROPOSED — Not Locked)
- `DI_CAP = 1.00` *(+100%)*

#### Output
- `damageIncreasePct = diPct`

---

### 4) SWING TIME MULTIPLIER

Swing time derives from:
- Base swing time (weapon or unarmed)
- Dexterity bonus (defined in Combat Core)
- Affix swing-speed bonus
- Status multipliers (Haste/Slow)

This aggregator is responsible for producing:
- `finalSwingTimeSeconds` OR a multiplier and leave calculation to Combat Core.

**Rule (LOCKED):**
- Combat Core owns the final swing-time formula and floor (1.0s).
- Aggregator supplies only the inputs:
  - `dex` (already known from PlayerStats)
  - `swingSpeedAffixPct`
  - `statusSwingTimeMultiplier`

#### Swing Speed Affix
- Read from `Combat_SwingSpeed`
- Stacking rule in affix catalog is `HighestOnly`

So:
- `swingSpeedAffixPct = Highest(Combat_SwingSpeed across equipment)`

*(Note: This is a percent bonus. Combat Core turns it into time scaling.)*

#### Status swing multiplier
From Status system:
- `statusSwingTimeMultiplier = Product(all active status swing multipliers)`
- Default = 1.0

> In v1, only Haste/Slow are expected. If none exist, this stays 1.0.

---

### 5) CAST TIME MULTIPLIER

Aggregator supplies:
- `dex` (PlayerStats)
- `fasterCasting` (FC) from items (HighestOnly)
- `statusCastTimeMultiplier` (product)

**Rule (LOCKED):**
- Magic system enforces cast-time floor of 50% base.

#### Faster Casting (FC)
From `ITEM_AFFIX_CATALOG.md`:
- `Magic_FasterCasting` is `HighestOnly`

So:
- `fasterCastingPoints = Highest(Magic_FasterCasting across equipment)`

**FC → time scaling (PROPOSED — Not Locked):**
- `fcTimeMultiplier = Clamp(1.0 - (fasterCastingPoints * 0.05), 0.70, 1.0)`
  - Example: FC 2 → 0.90 (10% faster)

> This can be tuned later, but the existence of FC and HighestOnly is locked.

---

### 6) BANDAGE TIME MULTIPLIER

Aggregator supplies:
- `dex` (PlayerStats)
- `statusBandageTimeMultiplier` (product)

**Rule (LOCKED):**
- Combat Core owns the dex formula and 50% floor for bandage time.

---

### 7) RESISTANCES (P/F/C/Po/E)

Resists come from:
- Armor base resists (from ItemDef)
- Resist affixes (items)
- Status effects (optional future)

**Rule (LOCKED):**
- Resist totals are integers.
- Resist cap is enforced (default 70 from PlayerDefinition).

#### Aggregation
For each channel (e.g. Fire):

1. `baseResist = Sum(ArmorPiece.BaseResistFire for equipped armor/shield)`
2. `bonusResist = Sum(Resist_Fire affix magnitudes across equipment)`
3. `statusResistBonus = Sum(status-based resist bonuses)` *(v1 likely 0)*

`rawResist = baseResist + bonusResist + statusResistBonus`

`finalResist = Clamp(rawResist, 0, ResistCap)`

#### Notes
- Jewelry has no base resists.
- Shields may have no base resists unless later authored (v1: only affixes).

---

## WEAPON PROFILE RESOLUTION (LOCKED)

Combat needs a single resolved weapon profile.

### If a weapon is equipped
Use the equipped weapon’s ItemDef weapon block:
- min/max damage
- damage type
- swing speed seconds
- stamina cost per swing
- required combat skill
- ammo type (if ranged)

### If no weapon is equipped
Use PlayerDefinition unarmed baseline.

### If Disarmed status is active
- The player is treated as **unarmed** for attacks.

---

## PROC PROFILE RESOLUTION (LOCKED)

From `ITEM_AFFIX_CATALOG.md`:

### Hit Spells
- Allowed weapon-only proc affixes (Hit_Lightning, etc.)
- Stacking: NoStack (but only weapons can carry them)

Aggregation:
- Only read from the **active weapon** item instance.
- `procChance = value on that weapon` (0..1)

### Hit Leaches
- Life/Mana/Stamina leach percent

Aggregation:
- Only read from the **active weapon** item instance.

---

## STATUS GATES (LOCKED)

Derived from `STATUS_EFFECT_CATALOG.md`.

The aggregator exposes booleans:
- `canAttack`
- `canCast`
- `canBandage`
- `canMove`

Rules:
- If any active status declares `BlocksWeaponAttacks` → `canAttack = false`
- If any active status declares `BlocksSpellcasting` → `canCast = false`
- If any active status declares `BlocksBandaging` → `canBandage = false`
- If any active status declares `BlocksMovement` → `canMove = false`

Also expose:
- `isInvisible` / `isRevealed` (optional, useful for targeting legality)

---

## ORDER OF OPERATIONS (LOCKED)

When recomputing the snapshot, perform in this order:

1. Read PlayerDefinition baselines
2. Resolve effective STR/DEX/INT (PlayerStats)
3. Resolve weapon profile (equipment vs unarmed, plus disarm)
4. Resolve item-based combat modifiers (HCI/DCI/DI/Resists/FC/LMC/SDI/etc.)
5. Resolve status gates and multipliers
6. Apply caps (resist cap, HCI/DCI/DI caps)
7. Emit snapshot

---

## CAPS SUMMARY

### Locked
- Resist cap: 70 (from PlayerDefinition)
- Minimum swing time: 1.0s (Combat Core)
- Cast time floor: 50% base (Magic)
- Bandage time floor: 50% base (Combat Core)

### Proposed (Not Locked)
- HCI cap: +45%
- DCI cap: +45%
- DI cap: +100%
- FC scaling: 5% per point, with a floor (tunable)

---

## IMPLEMENTATION CONTRACT (NEXT)

### Required runtime component (server)
- `PlayerCombatStatAggregator`
  - Inputs: PlayerCore (Definition/Stats/Vitals/Skills), Equipment, StatusEffectSystem
  - Output: `PlayerCombatStats` snapshot

### Events
- Recompute on:
  - Equip/unequip
  - Item durability break/unbreak
  - Status apply/remove
  - Skill value changed
  - Base stat change

### Consumers
- CombatResolver
- Spellcasting pipeline
- Bandage system
- Targeting legality checks (invisibility)

---

## DESIGN LOCK CONFIRMATION

This document is **authoritative**.

Any change must:
- Increment Version
- Update Last Updated
- Call out save-data and balance implications

