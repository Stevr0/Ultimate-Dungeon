# PLAYER_COMBAT_STATS.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 1.1  
Last Updated: 2026-01-28  
Engine: Unity 6 (URP)  
Networking: Netcode for GameObjects (NGO)  
Authority: Server-authoritative  
Determinism: Required (pure computation; no RNG)

---

## PURPOSE

Defines the **authoritative Player Combat Stat Aggregation** rules for *Ultimate Dungeon*.

This document answers one question:

> **How do we compute the player’s final combat-relevant stats**
> from PlayerDefinition + trained skills + equipped items/affixes + status effects?

Combat code (CombatResolver, SwingTimer, Spellcasting, Bandaging) must **never** reach into items/skills directly.
Instead, combat queries a single server-owned snapshot:

- `PlayerCombatStats` (server-owned)

If an aggregation rule is not defined here, **it does not exist**.

---

## SCOPE BOUNDARIES (NO OVERLAP)

This document **owns**:
- Aggregation inputs + allowed sources
- Aggregation policies (Sum / HighestOnly / Multiply / Clamp)
- The `PlayerCombatStats` snapshot contract
- Central cap enforcement for aggregated stats

This document does **not** own:
- Combat resolution order, hit/miss rolls, damage rolls, swing scheduling (owned by `COMBAT_CORE.md`)
- Item definitions / item lists (owned by `ITEM_DEF_SCHEMA.md` and `ITEM_CATALOG.md`)
- Affix definitions and per-affix stacking rules (owned by `ITEM_AFFIX_CATALOG.md`)
- Status definitions (owned by `STATUS_EFFECT_CATALOG.md`)
- Skill check RNG and progression (owned by `PROGRESSION.md`)

---

## DESIGN LOCKS (MUST ENFORCE)

1. **Server authoritative**
   - Only the server computes and owns combat stats.

2. **Deterministic & Pure**
   - Stat computation is pure (no RNG).
   - RNG is only used for combat events (hit rolls, damage rolls, procs) in `COMBAT_CORE.md`.

3. **Separation of concerns**
   - Items contribute **numbers** (base stats + affix modifiers).
   - Status effects contribute **gates and time multipliers**.
   - Combat reads only the aggregated snapshot.

4. **Caps enforced centrally**
   - Caps are applied here, not scattered across combat subsystems.

---

## INPUTS (AUTHORITATIVE SOURCES)

The aggregator may read ONLY from:

1. **PlayerDefinition** (`PLAYER_DEFINITION.md`)
   - Baseline hit/defense constants
   - Unarmed baseline profile
   - Resistance cap

2. **PlayerStats**
   - Effective STR/DEX/INT *(already includes any item/status attribute modifiers)*

3. **PlayerSkillBook**
   - Base trained skill values (0–100)

4. **Equipment / ItemInstances**
   - Equipped items (instances)
   - RolledAffixes[] (instances)
   - Base weapon/armor data (from ItemDef)

5. **StatusEffectSystem**
   - Action blocks (stun/paralyze/silence/etc.)
   - Time multipliers (Swing/Cast/Bandage)
   - Movement multipliers

---

## OUTPUT: PLAYERCOMBATSTATS SNAPSHOT (AUTHORITATIVE)

The server produces a single snapshot struct/class.

### Action Gates (from statuses)
- `bool canAttack`
- `bool canCast`
- `bool canBandage`
- `bool canMove`

### Core Combat Chances (inputs to Combat Core formula)
- `float attackerHciPct` *(0..1 delta; e.g., 0.10 = +10%)*
- `float defenderDciPct` *(0..1 delta)*

> NOTE: Combat Core owns the final hit-chance equation using PlayerDefinition baselines.

### Damage Modifiers
- `float damageIncreasePct` *(0..1; e.g., 0.25 = +25%)*

### Timing Inputs
- `float swingSpeedAffixPct` *(0..1; HighestOnly from equipment)*
- `float statusSwingTimeMultiplier` *(time multiplier; product)*
- `int fasterCastingPoints` *(FC; HighestOnly from equipment)*
- `float statusCastTimeMultiplier` *(time multiplier; product)*
- `float statusBandageTimeMultiplier` *(time multiplier; product)*

> Combat Core and the Magic system own the final time calculations and floors.

### Resistances (integers; capped)
- `int resistPhysical`
- `int resistFire`
- `int resistCold`
- `int resistPoison`
- `int resistEnergy`

### Weapon Context
- `WeaponProfile weapon` *(resolved from equipment or unarmed)*

### Proc Context (weapon-only)
- `ProcProfile procs` *(hit spells + leaches sourced from the active weapon instance)*

---

## IMPORTANT LOCK: TIME MULTIPLIER SEMANTICS

To avoid confusion, all multipliers scale **time** (seconds):

- `FinalTimeSeconds = BaseTimeSeconds * TimeMultiplier`

Meaning:
- `1.0` → no change
- `0.8` → 20% faster (shorter time)
- `1.2` → 20% slower (longer time)

This doc uses the names:
- `statusSwingTimeMultiplier`
- `statusCastTimeMultiplier`
- `statusBandageTimeMultiplier`

---

## BASELINES & CAPS (SINGLE SOURCES)

### Baselines (from PlayerDefinition)
From `PLAYER_DEFINITION.md`:
- `baseHitChance = 0.50`
- `baseDefenseChance = 0.50`
- `resistCap = 70`
- Unarmed profile:
  - `unarmedBaseSwingSpeedSeconds = 2.0`
  - `unarmedDamageRange = 1..4`

### Floors (owned elsewhere; referenced only)
- Minimum swing time floor = **1.0s** (owned by `COMBAT_CORE.md`)
- Cast time floor = **50% of base** (owned by Magic system rules)
- Bandage time floor = **50% of base** (owned by `COMBAT_CORE.md`)

---

## AGGREGATION POLICIES (AUTHORITATIVE)

### 1) HCI% (Hit Chance Increase)

**Source:** `Combat_HitChance` affix (Percent)

- `attackerHciPct = Sum(all equipped Combat_HitChance)`
- `attackerHciPct = Clamp(attackerHciPct, 0, HCI_CAP)`

**HCI_CAP (PROPOSED — Not Locked):** `0.45`

> Combat Core uses: `FinalHitChance = clamp01(baseHitChance + (AttackerHCI - DefenderDCI))`

### 2) DCI% (Defense Chance Increase)

**Source:** `Combat_DefenseChance` affix (Percent)

- `defenderDciPct = Sum(all equipped Combat_DefenseChance)`
- `defenderDciPct = Clamp(defenderDciPct, 0, DCI_CAP)`

**DCI_CAP (PROPOSED — Not Locked):** `0.45`

### 3) DI% (Damage Increase)

**Source:** `Combat_DamageIncrease` affix (Percent)

- `damageIncreasePct = Sum(all equipped Combat_DamageIncrease)`
- `damageIncreasePct = Clamp(damageIncreasePct, 0, DI_CAP)`

**DI_CAP (PROPOSED — Not Locked):** `1.00` *(+100%)*

### 4) Swing-speed affix input (HighestOnly)

**Source:** `Combat_SwingSpeed` affix (Percent)

- `swingSpeedAffixPct = Highest(Combat_SwingSpeed across equipped items)`

> Combat Core converts this percent bonus into a time factor and applies floors.

### 5) Faster Casting (FC) points (HighestOnly)

**Source:** `Magic_FasterCasting` affix (Flat points)

- `fasterCastingPoints = Highest(Magic_FasterCasting across equipped items)`

> Magic system converts FC points into time scaling and applies the 50% floor.

### 6) Resistances (P/F/C/Po/E)

Resists come from:
- Armor base resists (from ItemDef)
- Resist affixes (items)
- Status resist modifiers (optional future)

For each channel:

1. `baseResist = Sum(ArmorPiece.BaseResistX for equipped armor/shield)`
2. `bonusResist = Sum(Resist_X affix magnitudes across equipment)`
3. `statusResistBonus = Sum(status-based resist bonuses)` *(v1 likely 0)*

`rawResist = baseResist + bonusResist + statusResistBonus`

`finalResist = Clamp(rawResist, 0, resistCap)`

**resistCap is owned by `PLAYER_DEFINITION.md`**.

### 7) Status Gates (canAttack/canCast/canBandage/canMove)

Derived from `STATUS_EFFECT_CATALOG.md` semantics.

Rules:
- If any active status declares `BlocksWeaponAttacks` → `canAttack = false`
- If any active status declares `BlocksSpellcasting` → `canCast = false`
- If any active status declares `BlocksBandaging` → `canBandage = false`
- If any active status declares `BlocksMovement` → `canMove = false`

### 8) Status Time Multipliers

Aggregator reads time multipliers from statuses and combines them multiplicatively:

- `statusSwingTimeMultiplier = Product(all active swing time multipliers)`
- `statusCastTimeMultiplier = Product(all active cast time multipliers)`
- `statusBandageTimeMultiplier = Product(all active bandage time multipliers)`

Default for each is `1.0`.

---

## WEAPON PROFILE RESOLUTION (LOCKED)

Combat needs a single resolved weapon profile.

### If a weapon is equipped
Use the equipped weapon’s ItemDef weapon block:
- min/max damage
- damage type
- swing speed seconds
- stamina cost per swing
- required combat skill (SkillId)
- ammo type (if ranged)

### If no weapon is equipped
Use PlayerDefinition unarmed baseline.

### If Disarmed status is active
The player is treated as **unarmed** for attacks.

---

## PROC PROFILE RESOLUTION (LOCKED)

Proc affixes are **weapon-only**.

### Hit Spells
- Only read from the **active weapon ItemInstance**.
- Resolve proc chance values (0..1) for each allowed hit spell affix present.

### Hit Leaches
- Only read from the **active weapon ItemInstance**.
- Resolve leach percent values (0..1).

> Allowed hit spell IDs and tiering are defined in `ITEM_AFFIX_CATALOG.md`.

---

## ORDER OF OPERATIONS (LOCKED)

When recomputing the snapshot, perform in this order:

1. Read PlayerDefinition caps/baselines
2. Read effective STR/DEX/INT (PlayerStats)
3. Resolve weapon profile (equipment vs unarmed, plus disarm)
4. Aggregate item-based modifiers (HCI/DCI/DI/Resists/FC/etc.)
5. Aggregate status gates and time multipliers
6. Apply caps (resist cap, HCI/DCI/DI caps)
7. Emit snapshot

---

## IMPLEMENTATION CONTRACT (NEXT)

### Required runtime component (server)
- `PlayerCombatStatAggregator`
  - Inputs: PlayerCore (Definition/Stats/Skills), Equipment, StatusEffectSystem
  - Output: `PlayerCombatStats` snapshot

### Recompute triggers
Recompute on:
- Equip/unequip
- Item durability break/unbreak
- Status apply/remove
- Skill value changed
- Base stat change

### Consumers
- CombatResolver (`COMBAT_CORE.md`)
- Spellcasting pipeline (Magic docs)
- Bandage system (`COMBAT_CORE.md`)
- Targeting legality (invisibility / revealed)

---

## OPEN QUESTIONS (PROPOSED — NOT LOCKED)

- Exact caps:
  - HCI_CAP
  - DCI_CAP
  - DI_CAP
- Whether status time multipliers should ever use HighestOnly instead of Product (v1 uses Product)

---

## DESIGN LOCK CONFIRMATION

This document is **authoritative**.

Any change must:
- Increment Version
- Update Last Updated
- Call out save-data and balance implications

