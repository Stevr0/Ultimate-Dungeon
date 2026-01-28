# COMBAT_CORE.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 1.2  
Last Updated: 2026-01-28  
Engine: Unity 6 (URP)  
Networking: Netcode for GameObjects (NGO)  
Authority: Server-authoritative  
Determinism: Required (server-seeded)

---

## PURPOSE

This document defines the **authoritative Combat Core** for *Ultimate Dungeon*.

Combat Core is the glue between:
- Player stats/vitals/skills
- Items/equipment/affixes/durability
- Magic (spell payloads that deal damage or apply statuses)
- **Healing actions** (bandages)
- Death → corpse → loot → insurance

If a combat rule is not defined here, **it does not exist**.

---

## DESIGN LOCKS (MUST ENFORCE)

1. **Server authoritative**
   - Clients send **intents** only (attack request / target selection / cast request / bandage request).
   - Server validates, resolves, applies results, and replicates.

2. **Deterministic resolution**
   - All RNG rolls use a **deterministic server seed** per event.
   - No global RNG, no client RNG.

3. **One pipeline for all damage**
   - Weapon hits, spell direct damage, hit-spell procs, and DoT ticks all become **DamagePackets**.

4. **Items provide numbers; combat provides logic**
   - Weapons/armor/ammo/bandages define data.
   - Combat reads those values and executes the resolution.

5. **PvE and PvP use the same rules**
   - No PvP-only scaling or alternate formulas.

6. **Status effects modify combat; combat does not hardcode “special cases”**
   - Stun/paralyze/silence/slow/bleed/poison are implemented via the Status system.

---

## CORE TERMS

### Actor
Any entity that can:
- Have vitals (HP/Stam/Mana)
- Be targeted
- Receive damage
- Potentially die

Players and enemies are both Actors.

### Combatant
An Actor currently capable of making attacks (has a weapon or unarmed capability).

### DamagePacket
A single atomic damage application request.
- Created by weapon hit, spell payload, proc, or DoT tick.
- Resolved server-side into final HP reduction.

### HealPacket
A single atomic healing application request.
- Created by **bandage completion** (Healing skill) or spell payload heals.
- Resolved server-side into final HP increase (clamped to MaxHP).

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
- Bandage timers and bandage completion
- Death trigger

### The Client Owns
- UI feedback
- Animations/VFX driven by replicated events
- Input submission (intent)

---

## ATTACK INTENT MODEL (LOCKED)

### Player Attack Inputs
A player may initiate combat via:
1. **Attack Intent** (explicit)
   - Example: “Attack my current target.”
2. **Auto-attack on valid target** (implicit)
   - If enabled by the player (toggle later), the server may continue swinging while:
     - Target remains valid
     - In range
     - Not blocked by statuses

> v1 implementation may start with explicit “Attack Intent” and evolve into auto-attack.

### Authoritative Requirements to Start a Swing
To start or continue an attack loop, server must validate:
- Attacker is alive
- Target is alive
- Target is valid (not null, not self unless allowed)
- Target is within **weapon range** (see Range rules)
- Attacker is not action-blocked by statuses
- Attacker has required weapon/ammo (if applicable)

---

## RANGE & LINE-OF-SIGHT (LOCKED)

### Range (LOCKED)
- **Melee Range:** **2.0m** (+ target bounds)
- **Ranged Range:** **12.0m** (+ target bounds)

### Line of Sight
- **Weapon attacks:** LoS is **optional v1**.
- **Spells:** LoS is defined by `SpellDef.requiresLineOfSight` and is validated by the spell pipeline.

---

## SWING TIMER (LOCKED)

### Base swing time
- Comes from weapon data: `ItemDef.WeaponData.swingSpeedSeconds`.
- Unarmed uses PlayerDefinition baseline: `unarmedDamageRange` + `baseSwingSpeedSeconds`.

### Swing speed modifiers
Swing time may be reduced by:
- `Combat_SwingSpeed` affix (percent)
- **Dexterity swing-speed bonus** (below)
- Status effects (e.g., Haste/Slow)

**Rule (LOCKED):** swing time can never go below a minimum floor.
- **Minimum swing time floor = 1.0s**

### Dexterity swing-speed bonus (LOCKED)
Higher DEX reduces swing time, up to a hard cap.

- `DexSwingBonusPct = clamp01( DEX / 150 ) * DexSwingBonusCapPct`
- `SwingTime = max( 1.0s, BaseSwingTime * (1 - DexSwingBonusPct) )`

**DexSwingBonusCapPct (LOCKED):** **0.20** (max 20% faster at DEX cap)

### Dexterity cast-speed bonus (LOCKED)
Higher DEX also reduces spell cast time slightly, up to a hard cap.

- `DexCastBonusPct = clamp01( DEX / 150 ) * DexCastBonusCapPct`
- `CastTime = max( BaseCastTime * 0.50, BaseCastTime * (1 - DexCastBonusPct) )`
  - The **50% floor** comes from Magic rules.

**DexCastBonusCapPct (LOCKED):** **0.10** (max 10% faster at DEX cap)

---

## STAMINA COST (LOCKED)

- Each weapon swing consumes `staminaCostPerSwing` from the weapon.
- Unarmed uses a small constant stamina cost.

### Stamina failure behavior (LOCKED)
If attacker lacks stamina for the swing:
- The swing **does not occur**.
- The attacker’s attack loop **pauses** until stamina is sufficient again.

> v1: no “partial swing” or “forced exhaustion hit”.

---

## AMMUNITION (LOCKED)

Applies only to weapons with `ammoType != None`.

### Ammo consumption rule (LOCKED)
- 1 ammo is consumed **per attack attempt**, regardless of hit or miss.
- Ammo is validated and consumed on the **server**.

### Out of ammo rule (LOCKED)
- If no ammo is present, ranged attack attempts fail (no swing).

---

## HEALING (BANDAGES) — COMBAT-INTEGRATED (LOCKED)

Bandage healing is a **server-scheduled healing action** that can be used in or out of combat.

### Healing Inputs
A player may initiate healing via:
- **Bandage Intent** (explicit)
  - Example: “Use bandage on myself.”
  - (Future) “Use bandage on target ally.”

### Authoritative Requirements to Start Bandaging (LOCKED)
Server must validate:
- Healer is alive
- Target is alive
- Target is valid (self in v1)
- Healer has at least **1 bandage** in an accessible container
- Healer is not blocked by statuses that prevent actions (e.g., stunned/paralyzed)

### Bandage Consumption Rule (LOCKED)
- **1 bandage is consumed when bandaging starts.**
  - If the bandage is interrupted later, the bandage is still consumed.

### Base Bandage Time (PROPOSED — Not Locked)
- `BaseHealTimeSeconds = 4.0s`

> We can lock this once the Healing skill doc is written. Combat only needs the timing hooks.

### Dexterity healing-speed bonus (LOCKED)
Dexterity reduces **bandage healing time**.

- Applies only to **bandage-based healing** (Healing skill)
- Does **not** affect spell heals, potions, or passive regeneration

Formula:
- `DexHealBonusPct = clamp01( DEX / 150 ) * DexHealBonusCapPct`
- `HealTime = max( BaseHealTime * 0.50, BaseHealTime * (1 - DexHealBonusPct) )`

**DexHealBonusCapPct (LOCKED):** **0.20**  
(max 20% faster bandaging at DEX cap)

### Bandage Interruption (LOCKED)
Bandaging is interrupted if:
- The healer takes damage (any damage)
- The healer moves (optional v1; default **true**)
- The healer becomes stunned/paralyzed

On interruption:
- Bandage does not heal
- Bandage is still consumed
- Emit CombatEvent: BandageInterrupted

### Bandage Completion (LOCKED)
On successful completion:
- Server creates a **HealPacket** and applies it to the target
- Emit CombatEvent: BandageHealed

### Bandage Amount (PROPOSED — Not Locked)
Combat Core does not lock the Healing formula yet. Minimum v1 placeholder:
- `HealAmount = RandomInt(3, 7)`

> Final HealAmount should come from a Healing skill spec (skill vs difficulty, target state, etc.).

---

## HIT / MISS RESOLUTION (LOCKED ORDER)

When a swing timer completes, server resolves the following in order:

1. **Revalidate** (still alive, still in range, still allowed)
2. **Consume resources**
   - Consume stamina
   - Consume ammo (if ranged)
3. **Roll Hit**
4. **On Miss**
   - Emit CombatEvent: Miss
   - End resolution (no damage)
5. **On Hit**
   - Roll base weapon damage
   - Apply damage modifiers
   - Apply mitigation (resists)
   - Apply final damage via DamagePacket
   - Roll and resolve on-hit procs (Hit Spells)
   - Resolve on-hit leaches (based on final damage dealt)
   - Apply durability loss
   - Emit CombatEvent: Hit
6. **Death check**
   - If target HP <= 0, trigger Death pipeline

---

## HIT CHANCE / DEFENSE CHANCE (LOCKED FORMULA)

Combat Core requires the stat aggregator to expose at minimum:
- Attacker: `HitChance` (HCI)
- Defender: `DefenseChance` (DCI)

### Baselines
PlayerDefinition defines these as starting baselines (proposed):
- `baseHitChance = 0.50`
- `baseDefenseChance = 0.50`

### Final Hit Chance (LOCKED)
- `FinalHitChance = clamp01( baseHitChance + (AttackerHCI - DefenderDCI) )`

> Where HCI/DCI are expressed as **0.00–1.00** deltas (e.g., +0.10 = +10%).

---

## DAMAGE MODEL (LOCKED ORDER)

### Damage sources
- **Weapon Hit** (Physical or weapon-defined type)
- **Spell Direct Damage** (type from Spell payload)
- **Proc Damage** (Hit Spells produce spell damage packets)
- **DoT ticks** (status system emits packets)

### Weapon Damage Roll (LOCKED)
On weapon hit:
- Roll integer damage uniformly between `minDamage..maxDamage` from the weapon.

### Damage Increase (DI) (LOCKED)
- `Combat_DamageIncrease` modifies weapon damage.

Application:
- `DamageAfterDI = BaseDamage * (1 + DI%)`

### Resist Mitigation (LOCKED)
Damage is mitigated by the target’s resistance for that damage type.

- `FinalDamage = round( DamageAfterDI * (1 - ResistPct) )`
  - `ResistPct = clamp(ResistValue, 0, ResistCap) / 100`

> Resist channels: Physical/Fire/Cold/Poison/Energy. ResistCap defaults to Player cap (70).

---

## HIT SPELL PROCS (LOCKED)

Hit Spells are weapon affixes that may trigger **only on successful weapon hits**.

### Proc roll (LOCKED)
- Server rolls proc chance deterministically using the same combat seed stream.

### Proc resolution (LOCKED)
- A proc creates a **spell-like DamagePacket** (or status payload) through the damage pipeline.
- Proc does not consume reagents or mana.
- Proc respects target resists and Resist Spells rules as defined by magic/status systems.

Allowed hit spells and tiers are defined in `ITEM_AFFIX_CATALOG.md`.

---

## HIT LEACHES (LOCKED)

Hit Leaches restore resources to the attacker based on **final damage dealt**.

### Leach timing (LOCKED)
- Resolve after final damage is applied.

### Leach amount (LOCKED)
- `LeachAmount = floor( FinalDamageDealt * LeachPercent )`
- Apply to HP/Mana/Stamina as appropriate.
- Clamp to max vitals.

---

## DURABILITY IN COMBAT (LOCKED)

Combat is responsible for calling durability loss hooks.

### Weapon durability (LOCKED)
- Weapon loses durability **only on successful hits**.
- **Durability decrement per hit:** **0.1**
- **No durability loss on miss.**

### Armor durability (LOCKED)
- Armor loses durability when the wearer is **hit**.
- **Durability decrement per hit:** **0.1**

### Break state behavior
- At durability <= 0, item becomes unusable and contributes no modifiers (still lootable).

---

## SKILL INTEGRATION (LOCKED HOOKS)

Combat must call into progression through a single bridge (recommended: `SkillUseResolver`).

### On successful weapon hit
- Attempt skill gain for the weapon’s required combat skill (Swords/Macing/Fencing/Archery/Wrestling)
- Attempt skill gain for Tactics (if used)
- Optional: Anatomy gain (if used)

### On bandage completion
- Attempt skill gain for Healing

### On parry/block events
- Attempt Parrying gain (if Parrying is used)

> Exact skill gain triggers beyond the above are **PROPOSED** until the Status system exists.

---

## STATUS EFFECT INTEGRATION (LOCKED DEPENDENCY)

Combat must respect these status gates (names are conceptual; actual IDs come from the Status catalog):

- **Stunned / Paralyzed:** cannot start swings; cannot complete swings; cannot bandage
- **Disarmed:** cannot swing weapon (may fall back to unarmed)
- **Frozen / Rooted:** movement blocked, but swing/bandage may still occur if allowed
- **Silenced:** blocks spellcasting (not weapon swings or bandaging)

> The Status system is implemented separately. Combat queries it via an interface.

---

## DEATH HANDOFF (LOCKED)

Combat does not implement loot logic directly, but it must **trigger death deterministically**.

### Authoritative rule
- When an Actor’s HP reaches 0 (or below), the server triggers **Death** exactly once.

### Death pipeline responsibilities
Combat triggers a single call/event:
- `OnActorKilled(killer, victim, context)`

Death system then:
- Spawns corpse container
- Transfers uninsured items
- Applies insurance rules
- Drops Held Coins (dungeon only)

---

## NETWORKING / REPLICATION (LOCKED)

### Replicated outcomes
Clients must receive:
- Hit/Miss events (for VFX/anim/sfx)
- Damage numbers (optional)
- Bandage start/interrupt/complete events
- Updated vitals (authoritative)
- Death events

### Recommended model
- Server emits `CombatEvent` messages (RPC or NetworkEvent buffer)
- Vitals replication remains via `PlayerVitalsNet`

---

## REQUIRED IMPLEMENTATION ARTIFACTS (NEXT)

These are the minimum scripts/data contracts needed to implement this doc:

1. **Combat stat aggregator** (server)
   - Computes HitChance, DefenseChance, DamageIncrease, Resist totals, swing/cast modifiers
   - Reads from: Player base stats + skills + equipped item affixes + status effects

2. **DamagePacket / HealPacket models** (server)
   - Source, target, amount, type, flags (weapon/spell/proc/dot/bandage)

3. **CombatResolver** (server)
   - Owns hit/miss/damage order
   - Owns deterministic seed usage per swing

4. **AttackLoop / SwingTimer** (server)
   - Schedules swings per attacker-target pair

5. **BandageHealAction** (server)
   - Validates bandage use, consumes bandage, schedules heal completion, handles interruptions

6. **CombatEvents** (net)
   - Hit/Miss/Proc/Bandage/Death events for client visuals

---

## OPEN QUESTIONS (PROPOSED — NOT LOCKED)

The following are intentionally not locked in this doc version:

- Exact weapon LoS behavior (if weapon attacks require LoS in v2)
- Exact DI stacking/caps across equipment
- Exact HCI/DCI sourcing from skills vs items (aggregation details)
- Exact bandage heal amount formula (Healing skill spec)
- Exact durability break behavior UX (warnings, visuals)

---

## DESIGN LOCK CONFIRMATION

This document is **authoritative**.

Any future change must:
- Increment Version
- Update Last Updated
- Explicitly call out combat/save-data implications

