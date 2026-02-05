# SPELL_STATS_PROPOSED.md — Ultimate Dungeon

Version: 0.1 (PROPOSED)
Last Updated: 2026-02-05
Status: **PROPOSED — NOT LOCKED**

---

## PURPOSE

This document defines **proposed numeric stats** for all `SpellId`s defined in `SPELL_ID_CATALOG.md`.

It provides a **complete, reviewable baseline** for:
- Cast time
- Mana cost
- Cooldown
- Targeting
- Primary effect payload(s)

These values are **not final** and must be balance-reviewed before locking.

Once locked, this document (or its successor) becomes the **single numeric authority** for `SpellDef` ScriptableObjects.

---

## GLOBAL BASELINES (PROPOSED)

| Property | Value |
|---|---|
| Minimum Cast Time | 0.5s |
| Cast Time Floor | 50% of base |
| Global Cooldown Floor | 0.5s |
| Mana Regen Source | Skills + Status |

---

## SPELLS GROUPED BY PROPOSED EQUIPSLOT

> This section reorganizes the same **PROPOSED** spell stats by the **EquipSlot that is expected to grant them**.
> No numeric values have changed — this is a **projection / organization pass only**.

---

## EQUIPSLOT: MAINHAND (Weapons)

> Offensive and combat-adjacent spells typically granted by weapons.

### Primary (Damage)

| SpellId | Cast | Mana | CD | Targeting | Effect |
|---|---:|---:|---:|---|---|
| MagicArrow | 1.0s | 5 | 1.0s | SingleTarget | Damage: 6–10 (Energy) |
| Harm | 1.25s | 7 | 1.0s | SingleTarget | Damage: 10–14 (Physical) |
| Fireball | 1.5s | 9 | 1.5s | SingleTarget | Damage: 12–16 (Fire) |
| Lightning | 1.75s | 11 | 1.5s | SingleTarget | Damage: 18–22 (Energy) |
| EnergyBolt | 2.25s | 18 | 1.5s | SingleTarget | Damage: 25–30 (Energy) |
| Flamestrike | 2.5s | 22 | 2.0s | SingleTarget | Damage: 35–45 (Fire) |

### Secondary (Debuffs / Pressure)

| SpellId | Cast | Mana | CD | Targeting | Effect |
|---|---:|---:|---:|---|---|
| Weaken | 1.0s | 4 | 1.0s | SingleTarget | ApplyStatus: -STR |
| Clumsy | 1.0s | 4 | 1.0s | SingleTarget | ApplyStatus: -DEX (short) |
| Feeblemind | 1.0s | 4 | 1.0s | SingleTarget | ApplyStatus: -INT |
| Curse | 1.75s | 11 | 2.5s | SingleTarget | ApplyStatus: Curse |
| Poison | 1.5s | 9 | 2.0s | SingleTarget | ApplyStatus: Poison (Tier 1) |

### Utility (Situational)

| SpellId | Cast | Mana | CD | Targeting | Effect |
|---|---:|---:|---:|---|---|
| Dispel | 2.25s | 18 | 3.0s | SingleTarget | Dispel Summon/Buff |

---

## EQUIPSLOT: OFFHAND (Rings / Shields)

> Defensive buffs, counters, and control magic.

### Primary (Defense Stance)

| SpellId | Cast | Mana | CD | Targeting | Effect |
|---|---:|---:|---:|---|---|
| Protection | 1.25s | 6 | 2.0s | SingleTarget | ApplyStatus: Protection |
| ReactiveArmor | 1.0s | 6 | 2.0s | Self | ApplyStatus: ReactiveArmor |
| MagicReflection | 2.0s | 14 | 6.0s | Self | ApplyStatus: Reflection |

### Secondary (Control / Counterplay)

| SpellId | Cast | Mana | CD | Targeting | Effect |
|---|---:|---:|---:|---|---|
| Paralyze | 2.0s | 14 | 4.0s | SingleTarget | ApplyStatus: Paralyzed |
| MassDispel | 2.5s | 22 | 6.0s | AreaTarget | Dispel AoE |

### Utility (Group / Cleanup)

| SpellId | Cast | Mana | CD | Targeting | Effect |
|---|---:|---:|---:|---|---|
| ArchProtection | 1.75s | 11 | 4.0s | AreaCaster | Buff: AoE Protection |
| Dispel | 2.25s | 18 | 3.0s | SingleTarget | Dispel Summon/Buff |

---

## EQUIPSLOT: CHEST (Armor)

> Core survivability and stat-altering spells.

### Primary (Core Buffs)

| SpellId | Cast | Mana | CD | Targeting | Effect |
|---|---:|---:|---:|---|---|
| Bless | 1.5s | 9 | 2.5s | SingleTarget | ApplyStatus: +All Stats |
| Strength | 1.25s | 6 | 2.0s | SingleTarget | ApplyStatus: +STR |

### Secondary (Resource Pressure)

| SpellId | Cast | Mana | CD | Targeting | Effect |
|---|---:|---:|---:|---|---|
| ManaDrain | 1.75s | 11 | 2.5s | SingleTarget | Drain Mana |
| ManaVampire | 2.5s | 22 | 4.0s | SingleTarget | Drain Mana |

### Utility (Stabilize)

| SpellId | Cast | Mana | CD | Targeting | Effect |
|---|---:|---:|---:|---|---|
| GreaterHeal | 1.75s | 11 | 2.0s | SingleTarget | Heal: 20–28 |

---

## EQUIPSLOT: HEAD (Helms / Hoods)

> Perception, utility, and anti-stealth magic.

### Primary (Perception)

| SpellId | Cast | Mana | CD | Targeting | Effect |
|---|---:|---:|---:|---|---|
| NightSight | 1.0s | 4 | 2.0s | SingleTarget | ApplyStatus: NightSight |

### Secondary (Disruption)

| SpellId | Cast | Mana | CD | Targeting | Effect |
|---|---:|---:|---:|---|---|
| Feeblemind | 1.0s | 4 | 1.0s | SingleTarget | ApplyStatus: -INT |
| Clumsy | 1.0s | 4 | 1.0s | SingleTarget | ApplyStatus: -DEX (short) |

### Utility (Anti-Stealth)

| SpellId | Cast | Mana | CD | Targeting | Effect |
|---|---:|---:|---:|---|---|
| Reveal | 2.25s | 18 | 4.0s | AreaCaster | Reveal Invisible |

---

## EQUIPSLOT: NECK (Capes / Earrings)

> Stealth, illusion, and identity-altering spells.

### Primary (Stealth)

| SpellId | Cast | Mana | CD | Targeting | Effect |
|---|---:|---:|---:|---|---|
| Invisibility | 2.25s | 18 | 6.0s | SingleTarget | ApplyStatus: Invisible |

### Secondary (Identity / Form)

| SpellId | Cast | Mana | CD | Targeting | Effect |
|---|---:|---:|---:|---|---|
| Incognito | 2.0s | 14 | 6.0s | Self | ApplyStatus: Incognito |
| Polymorph | 2.5s | 22 | 10.0s | Self | Transform |

### Utility (Escape / Setup)

| SpellId | Cast | Mana | CD | Targeting | Effect |
|---|---:|---:|---:|---|---|
| Teleport | 1.5s | 9 | 2.0s | GroundTarget | Utility: teleport |

---

## EQUIPSLOT: FOOT (Boots)

> Mobility and movement-related abilities.

### Primary (Mobility)

| SpellId | Cast | Mana | CD | Targeting | Effect |
|---|---:|---:|---:|---|---|
| Teleport | 1.5s | 9 | 2.0s | GroundTarget | Utility: teleport |

### Secondary (Chase / Stop)

| SpellId | Cast | Mana | CD | Targeting | Effect |
|---|---:|---:|---:|---|---|
| Paralyze | 2.0s | 14 | 4.0s | SingleTarget | ApplyStatus: Paralyzed |
| Weaken | 1.0s | 4 | 1.0s | SingleTarget | ApplyStatus: -STR |

### Utility (Self-Enhance)

| SpellId | Cast | Mana | CD | Targeting | Effect |
|---|---:|---:|---:|---|---|
| Agility | 1.25s | 6 | 2.0s | SingleTarget | ApplyStatus: +DEX |

---

## EQUIPSLOT: BELT (UtilityItem — BeltA / BeltB)

> Consumables, emergency actions, and support magic.

### Primary (Emergency)

| SpellId | Cast | Mana | CD | Targeting | Effect |
|---|---:|---:|---:|---|---|
| Heal | 1.0s | 4 | 1.0s | SingleTarget | Heal: 8–12 |
| GreaterHeal | 1.75s | 11 | 2.0s | SingleTarget | Heal: 20–28 |
| Cure | 1.25s | 6 | 1.5s | SingleTarget | RemoveStatus: Poison |

### Secondary (Area Support)

| SpellId | Cast | Mana | CD | Targeting | Effect |
|---|---:|---:|---:|---|---|
| ArchCure | 1.75s | 11 | 3.0s | AreaTarget | Cure: AoE |
| ArchProtection | 1.75s | 11 | 4.0s | AreaCaster | Buff: AoE Protection |

### Utility (Sustain)

| SpellId | Cast | Mana | CD | Targeting | Effect |
|---|---:|---:|---:|---|---|
| CreateFood | 1.0s | 4 | 1.0s | Self | Utility: spawn food |

---

## EQUIPSLOT: BAG (Backpacks)

> World interaction and non-combat utility.

### Primary (World Interaction)

| SpellId | Cast | Mana | CD | Targeting | Effect |
|---|---:|---:|---:|---|---|
| Telekinesis | 1.5s | 9 | 2.0s | SingleTarget | Remote interact |
| Unlock | 1.5s | 9 | 3.0s | SingleTarget | Unlock |
| MagicLock | 1.5s | 9 | 3.0s | SingleTarget | Lock |

### Secondary (Safety / Control)

| SpellId | Cast | Mana | CD | Targeting | Effect |
|---|---:|---:|---:|---|---|
| WallOfStone | 1.5s | 10 | 5.0s | GroundTarget | Field: wall |

### Utility (Sustain)

| SpellId | Cast | Mana | CD | Targeting | Effect |
|---|---:|---:|---:|---|---|
| CreateFood | 1.0s | 4 | 1.0s | Self | Utility: spawn food |

---

## EQUIPSLOT: MOUNT

> Travel, large-scale movement, and positioning magic.

### Primary (Travel)

| SpellId | Cast | Mana | CD | Targeting | Effect |
|---|---:|---:|---:|---|---|
| Recall | 1.75s | 11 | 10.0s | Self | Teleport: mark/home |

### Secondary (Group Travel)

| SpellId | Cast | Mana | CD | Targeting | Effect |
|---|---:|---:|---:|---|---|
| GateTravel | 2.5s | 22 | 15.0s | GroundTarget | Teleport Gate |

### Utility (—)

> None proposed yet.

---

## EQUIPSLOT: HIGH-END / RARE (Typically Mainhand or Chest)

> High-impact AoE, fields, and summons. Expected on rare items only.

### Primary (AoE Damage)

| SpellId | Cast | Mana | CD | Targeting | Effect |
|---|---:|---:|---:|---|---|
| ChainLightning | 2.5s | 22 | 3.0s | AreaTarget | Damage: Chain |
| MeteorSwarm | 2.5s | 22 | 5.0s | AreaTarget | AoE Fire Damage |
| Earthquake | 3.0s | 30 | 6.0s | AreaCaster | AoE Damage |

### Secondary (Fields)

| SpellId | Cast | Mana | CD | Targeting | Effect |
|---|---:|---:|---:|---|---|
| FireField | 1.75s | 11 | 5.0s | GroundTarget | Field: Fire DOT |
| PoisonField | 2.0s | 14 | 6.0s | GroundTarget | Field: Poison |
| ParalyzeField | 2.25s | 18 | 7.0s | GroundTarget | Field: Paralyze |
| EnergyField | 2.5s | 22 | 8.0s | GroundTarget | Field: Energy |

### Utility (Summons / Big Utility)

| SpellId | Cast | Mana | CD | Targeting | Effect |
|---|---:|---:|---:|---|---|
| BladeSpirits | 2.0s | 14 | 8.0s | GroundTarget | Summon |
| SummonCreature | 2.0s | 14 | 6.0s | GroundTarget | Summon (random) |
| EnergyVortex | 3.0s | 30 | 12.0s | GroundTarget | Summon |
| SummonAirElemental | 3.0s | 30 | 12.0s | GroundTarget | Summon |
| SummonDaemon | 3.0s | 30 | 15.0s | GroundTarget | Summon |
| SummonEarthElemental | 3.0s | 30 | 12.0s | GroundTarget | Summon |
| SummonFireElemental | 3.0s | 30 | 12.0s | GroundTarget | Summon |
| SummonWaterElemental | 3.0s | 30 | 12.0s | GroundTarget | Summon |

---

## NOTES (IMPORTANT)

- This grouping is **intentional design guidance**, not a hard restriction.
- ItemDefs may offer **subsets** of these spells.
- High-end spells should be **rare, late-game, or durability-risky**.
- Numeric values remain **PROPOSED** until locked.

---

## NEXT STEPS

1. Validate spell → EquipSlot fit
2. Remove or reassign any outliers
3. Lock EquipSlot spell pools
4. Generate per-slot spell whitelist enums
5. Enforce in ItemDef authoring validators

