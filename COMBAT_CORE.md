# COMBAT_CORE.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 1.3  
Last Updated: 2026-01-28  
Engine: Unity 6 (URP)  
Networking: Netcode for GameObjects (NGO)  
Authority: Server-authoritative  
Determinism: Required (server-seeded)

---

## PURPOSE

Defines the **authoritative Combat Core** for *Ultimate Dungeon*.

Combat Core is the glue between:
- Player stats/vitals/skills
- Items/equipment/affixes/durability
- Magic (spell payloads that deal damage or apply statuses)
- Healing actions (bandages)
- Death → corpse → loot → insurance

If a combat rule is not defined here, **it does not exist**.

---

## SCOPE BOUNDARIES (NO OVERLAP)

This document **owns**:
- Combat execution order (hit/miss → damage → procs → durability → death)
- Server scheduling model (swing timers, bandage timers)
- Deterministic RNG usage for combat events
- Core formulas for:
  - Final hit chance (given aggregated inputs)
  - Damage application order
  - Durability loss triggers

This document does **not** own:
- Player baselines/caps (owned by `PLAYER_DEFINITION.md`)
- Stat aggregation and caps enforcement (owned by `PLAYER_COMBAT_STATS.md`)
- Status definitions and semantics (owned by `STATUS_EFFECT_CATALOG.md`)
- Spell definitions and costs (owned by Magic docs)
- Affix IDs and stacking policies (owned by `ITEM_AFFIX_CATALOG.md`)

---

## DESIGN LOCKS (MUST ENFORCE)

1. **Server authoritative**
   - Clients send intents only.
   - Server validates, resolves, applies results, and replicates.

2. **Deterministic resolution**
   - All RNG rolls use deterministic server seeds per event.
   - No client RNG.

3. **One pipeline for all damage**
   - Weapon hits, spell direct damage, hit-spell procs, and DoT ticks all become **DamagePackets**.

4. **Items provide numbers; combat provides logic**
   - `ItemDef` and affixes define data.
   - Combat reads aggregated values and executes resolution.

5. **PvE and PvP use the same rules**

6. **Status-first integrity**
   - Combat respects action gates and multipliers from the status system.

---

## CORE TERMS

### Actor
Any entity that can:
- Have vitals (HP/Stam/Mana)
- Be targeted
- Receive damage
- Potentially die

### DamagePacket
A single atomic damage application request.

### HealPacket
A single atomic healing application request.

---

## COMBAT LOOP MODEL (LOCKED)

Combat is **real-time**, resolved by **server scheduled events**, not frame polling.

### The Server Owns
- Swing timers
- Hit/miss rolls
- Damage rolls
- Proc rolls (Hit Spells / Hit Leaches)
- Stamina and ammo consumption
- Durability loss
- Bandage timers and completion
- Death trigger

### The Client Owns
- UI feedback
- Animations/VFX driven by replicated events
- Input submission (intents)

---

## COMBAT INPUT CONTRACT (LOCKED)

Combat systems must consume a single aggregated snapshot:
- `PlayerCombatStats` (server-owned)

Combat code must **not** reach into equipment/skills/statuses directly.

---

## RANGE & LINE-OF-SIGHT (LOCKED)

### Range
- **Melee Range:** **2.0m** (+ target bounds)
- **Ranged Range:** **12.0m** (+ target bounds)

### Line of Sight
- Weapon attacks: LoS is **optional v1**.
- Spells: LoS is validated by spell pipeline (`SpellDef.requiresLineOfSight`).

---

## SWING TIMER (LOCKED)

### Base swing time source
- From weapon profile: `weapon.swingSpeedSeconds` (from `ItemDef`)
- If unarmed: from `PLAYER_DEFINITION.md` unarmed baseline

### Minimum swing time floor (LOCKED)
- **Minimum swing time floor = 1.0s**

### Swing time modifiers (LOCKED)
Final swing time is computed by Combat Core using:
- Dexterity bonus (defined below)
- Swing-speed affix input (from `PlayerCombatStats.swingSpeedAffixPct`)
- Status swing time multiplier (from `PlayerCombatStats.statusSwingTimeMultiplier`)

> Aggregation policies (HighestOnly for swing-speed affix; Product for status multipliers) are owned by `PLAYER_COMBAT_STATS.md`.

### Dexterity swing-time bonus (LOCKED)
Higher DEX reduces swing time, up to a hard cap.

- `DexSwingBonusPct = clamp01( DEX / 150 ) * DexSwingBonusCapPct`
- `SwingTime = max( 1.0s, BaseSwingTime * (1 - DexSwingBonusPct) )`

**DexSwingBonusCapPct (LOCKED):** **0.20**

---

## STAMINA COST (LOCKED)

- Each weapon swing consumes `staminaCostPerSwing` from the weapon.
- Unarmed uses a small constant stamina cost.

### Stamina failure behavior (LOCKED)
If attacker lacks stamina:
- The swing does not occur.
- The attack loop pauses until stamina is sufficient.

---

## AMMUNITION (LOCKED)

Applies only to weapons with `ammoType != None`.

### Ammo consumption rule
- 1 ammo is consumed **per attack attempt**, hit or miss.

### Out of ammo rule
- If no ammo is present, ranged attack attempts fail.

---

## HEALING (BANDAGES) — COMBAT-INTEGRATED (LOCKED)

Bandage healing is a **server-scheduled** healing action.

### Authoritative Requirements to Start Bandaging
Server validates:
- Healer alive
- Target alive
- Target valid (self in v1)
- At least 1 bandage available
- Not blocked by status gates (stun/paralyze/etc.)

### Bandage Consumption Rule (LOCKED)
- **1 bandage is consumed when bandaging starts.**

### Base Bandage Time (PROPOSED — Not Locked)
- `BaseHealTimeSeconds = 4.0s`

### Bandage time floor (LOCKED)
- Heal time cannot go below **50% of base**.

### Dexterity healing-time bonus (LOCKED)
Dex reduces bandage time up to a hard cap.

- `DexHealBonusPct = clamp01( DEX / 150 ) * DexHealBonusCapPct`
- `HealTime = max( BaseHealTime * 0.50, BaseHealTime * (1 - DexHealBonusPct) )`

**DexHealBonusCapPct (LOCKED):** **0.20**

### Bandage Interruption (LOCKED)
Bandaging is interrupted if:
- Healer takes damage
- Healer moves (default true)
- Healer becomes stunned/paralyzed

On interruption:
- No heal
- Bandage remains consumed
- Emit event: `BandageInterrupted`

### Bandage Completion (LOCKED)
On completion:
- Server creates a HealPacket
- Emit event: `BandageHealed`

### Bandage Amount (PROPOSED — Not Locked)
Placeholder:
- `HealAmount = RandomInt(3, 7)`

> Final formula belongs to a Healing skill spec.

---

## HIT / MISS RESOLUTION (LOCKED ORDER)

When a swing timer completes, server resolves in order:

1. **Revalidate** (alive, in range, allowed)
2. **Consume resources**
   - Consume stamina
   - Consume ammo (if ranged)
3. **Roll Hit**
4. **On Miss**
   - Emit event: `Miss`
   - End resolution
5. **On Hit**
   - Roll base weapon damage
   - Apply damage modifiers
   - Apply mitigation (resists)
   - Apply final damage via DamagePacket
   - Roll and resolve on-hit procs (Hit Spells)
   - Resolve on-hit leaches
   - Apply durability loss
   - Emit event: `Hit`
6. **Death check**
   - If HP <= 0: trigger death pipeline

---

## HIT CHANCE / DEFENSE CHANCE (LOCKED FORMULA)

Combat Core consumes:
- `baseHitChance` and `baseDefenseChance` from `PLAYER_DEFINITION.md`
- `attackerHciPct` and `defenderDciPct` from `PLAYER_COMBAT_STATS.md`

### Final Hit Chance
- `FinalHitChance = clamp01( baseHitChance + (attackerHciPct - defenderDciPct) )`

> HCI/DCI caps are enforced by the stat aggregator.

---

## DAMAGE MODEL (LOCKED ORDER)

### Weapon Damage Roll
- Roll integer damage uniformly between `minDamage..maxDamage` from weapon profile.

### Damage Increase (DI)
- `damageIncreasePct` comes from `PLAYER_COMBAT_STATS.md`
- `DamageAfterDI = BaseDamage * (1 + damageIncreasePct)`

### Resist Mitigation
Resists (already capped) come from `PLAYER_COMBAT_STATS.md`.

- `ResistPct = clamp(ResistValue, 0, ResistCap) / 100`
- `FinalDamage = round( DamageAfterDI * (1 - ResistPct) )`

> `ResistCap` is owned by `PLAYER_DEFINITION.md`.

---

## HIT SPELL PROCS (LOCKED)

Hit Spells are weapon affixes that may trigger **only on successful weapon hits**.

### Critical clarification (LOCKED)
**Hit Spells reuse spell-like payload logic, not spell definitions.**
They do **not** require mana, reagents, cast time, targeting validation, or line-of-sight checks.

### Proc roll
- Server rolls proc chance deterministically.

### Proc resolution
- Proc creates a DamagePacket and/or applies statuses via the status system.
- Proc respects target resists and Resist Spells rules as defined by Status/Magic rules.

Allowed hit spells and tiers are defined in `ITEM_AFFIX_CATALOG.md`.

---

## HIT LEACHES (LOCKED)

Hit Leaches restore resources to the attacker based on **final damage dealt**.

### Leach timing
- Resolve after final damage is applied.

### Leach amount
- `LeachAmount = floor( FinalDamageDealt * LeachPercent )`
- Clamp to max vitals.

---

## DURABILITY IN COMBAT (LOCKED)

Combat is responsible for calling durability loss hooks.

### Weapon durability
- Weapon loses durability **only on successful hits**.
- **Durability decrement per hit:** **0.1**

### Armor durability
- Armor loses durability when wearer is **hit**.
- **Durability decrement per hit:** **0.1**

### Break state behavior
- At durability <= 0, item becomes unusable and contributes no modifiers.

---

## SKILL INTEGRATION (LOCKED HOOKS)

Combat must call into progression through a single bridge:
- `SkillUseResolver` (defined in `PROGRESSION.md`)

### On successful weapon hit
- Attempt skill use for the weapon’s required combat skill
- Attempt skill use for Tactics (if used)
- Optional: Anatomy (if used)

### On bandage completion
- Attempt skill use for Healing

### On parry/block
- Attempt skill use for Parrying

---

## STATUS EFFECT INTEGRATION (LOCKED DEPENDENCY)

Combat respects gates from `PlayerCombatStats` (derived from `STATUS_EFFECT_CATALOG.md`).

- If `canAttack == false` → cannot start/complete swings
- If `canBandage == false` → cannot start/complete bandages

---

## DEATH HANDOFF (LOCKED)

Combat triggers death deterministically.

- When HP reaches 0 or below, server triggers death exactly once.

Combat emits:
- `OnActorKilled(killer, victim, context)`

Death/loot systems then enforce:
- corpse spawn
- insurance rules
- Held Coins drop (dungeon)

Player-facing death-loss rules are owned by `PLAYER_DEFINITION.md`.

---

## NETWORKING / REPLICATION (LOCKED)

Clients must receive:
- Hit/Miss events
- Damage numbers (optional)
- Bandage start/interrupt/complete events
- Updated vitals
- Death events

---

## REQUIRED IMPLEMENTATION ARTIFACTS (NEXT)

1. Combat stat aggregator (server)
2. DamagePacket / HealPacket models (server)
3. CombatResolver (server)
4. AttackLoop / SwingTimer scheduler (server)
5. BandageHealAction (server)
6. CombatEvents (net)

---

## OPEN QUESTIONS (PROPOSED — NOT LOCKED)

- Weapon LoS behavior (if weapon attacks require LoS in v2)
- Bandage heal amount formula (Healing skill spec)
- Exact DI stacking/caps beyond aggregator caps

---

## DESIGN LOCK CONFIRMATION

This document is **authoritative**.

Any change must:
- Increment Version
- Update Last Updated
- Call out save-data and balance implications

