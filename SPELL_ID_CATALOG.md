# SPELL_ID_CATALOG.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 1.0  
Last Updated: 2026-01-28

---

## PURPOSE

Defines the **authoritative SpellId catalog** for *Ultimate Dungeon*, based on **Ultima Online Magery spells** (Circles 1–8).

This document is the single source of truth for:
- Stable spell identifiers (`SpellId`)
- Circle assignment
- High-level effect tags (damage / heal / status / utility)
- Targeting mode (self / target / ground / area)

If a spell is not listed here, **it does not exist**.

---

## DESIGN LOCKS (MUST ENFORCE)

1. **Stable IDs**
   - Once shipped, SpellIds must never be reordered.
   - Only append new spells at the end.

2. **Ultima Online Baseline**
   - This catalog mirrors classic UO Magery spell names.
   - Future expansions (Necromancy, Chivalry, etc.) are separate catalogs.

3. **Server Authoritative**
   - Spell casting, success/fizzle, cooldowns, and resolution are server-owned.

4. **Targeting Is Explicit**
   - Each spell declares a single Targeting Mode.

---

## TARGETING MODES (AUTHORITATIVE ENUM)

Use these values in `SpellDef`:

- **Self** — affects caster only
- **SingleTarget** — requires a valid target actor
- **GroundTarget** — requires a ground-point selection
- **AreaAroundTarget** — AoE centered on target actor
- **AreaAroundCaster** — AoE centered on caster

---

## EFFECT TAGS (AUTHORITATIVE FLAGS)

These tags are for **design clarity and UI grouping**. They do not replace actual effect data in `SpellDef`.

- **Damage** — applies immediate damage
- **Heal** — restores HP (or removes damage)
- **Cure** — removes poison/status
- **Buff** — positive modifier / protection
- **Debuff** — negative modifier / disruption
- **Control** — stun/paralyze/sleep/root
- **Summon** — creates an allied entity
- **Utility** — reveal, unlock, teleport, light, etc.

---

## CIRCLE 1 — FIRST CIRCLE

| SpellId | Display Name | Targeting | Tags |
|---|---|---|---|
| Clumsy | Clumsy | SingleTarget | Debuff |
| CreateFood | Create Food | Self | Utility |
| Feeblemind | Feeblemind | SingleTarget | Debuff |
| Heal | Heal | SingleTarget | Heal |
| MagicArrow | Magic Arrow | SingleTarget | Damage |
| NightSight | Night Sight | SingleTarget | Buff, Utility |
| ReactiveArmor | Reactive Armor | Self | Buff |
| Weaken | Weaken | SingleTarget | Debuff |

---

## CIRCLE 2 — SECOND CIRCLE

| SpellId | Display Name | Targeting | Tags |
|---|---|---|---|
| Agility | Agility | SingleTarget | Buff |
| Cunning | Cunning | SingleTarget | Buff |
| Cure | Cure | SingleTarget | Cure |
| Harm | Harm | SingleTarget | Damage |
| MagicTrap | Magic Trap | SingleTarget | Utility |
| RemoveTrap | Remove Trap | SingleTarget | Utility |
| Protection | Protection | SingleTarget | Buff |
| Strength | Strength | SingleTarget | Buff |

---

## CIRCLE 3 — THIRD CIRCLE

| SpellId | Display Name | Targeting | Tags |
|---|---|---|---|
| Bless | Bless | SingleTarget | Buff |
| Fireball | Fireball | SingleTarget | Damage |
| MagicLock | Magic Lock | SingleTarget | Utility |
| Poison | Poison | SingleTarget | Debuff |
| Telekinesis | Telekinesis | SingleTarget | Utility |
| Teleport | Teleport | GroundTarget | Utility |
| Unlock | Unlock | SingleTarget | Utility |
| WallOfStone | Wall of Stone | GroundTarget | Control, Utility |

---

## CIRCLE 4 — FOURTH CIRCLE

| SpellId | Display Name | Targeting | Tags |
|---|---|---|---|
| ArchCure | Arch Cure | AreaAroundTarget | Cure |
| ArchProtection | Arch Protection | AreaAroundCaster | Buff |
| Curse | Curse | SingleTarget | Debuff |
| FireField | Fire Field | GroundTarget | Damage, Control |
| GreaterHeal | Greater Heal | SingleTarget | Heal |
| Lightning | Lightning | SingleTarget | Damage |
| ManaDrain | Mana Drain | SingleTarget | Debuff |
| Recall | Recall | Self | Utility |

---

## CIRCLE 5 — FIFTH CIRCLE

| SpellId | Display Name | Targeting | Tags |
|---|---|---|---|
| BladeSpirits | Blade Spirits | GroundTarget | Summon |
| DispelField | Dispel Field | GroundTarget | Utility |
| Incognito | Incognito | Self | Utility |
| MagicReflection | Magic Reflection | Self | Buff |
| MindBlast | Mind Blast | SingleTarget | Damage |
| Paralyze | Paralyze | SingleTarget | Control |
| PoisonField | Poison Field | GroundTarget | Debuff, Control |
| SummonCreature | Summon Creature | GroundTarget | Summon |

---

## CIRCLE 6 — SIXTH CIRCLE

| SpellId | Display Name | Targeting | Tags |
|---|---|---|---|
| Dispel | Dispel | SingleTarget | Utility |
| EnergyBolt | Energy Bolt | SingleTarget | Damage |
| Explosion | Explosion | SingleTarget | Damage |
| Invisibility | Invisibility | SingleTarget | Utility |
| Mark | Mark | Self | Utility |
| MassCurse | Mass Curse | AreaAroundTarget | Debuff |
| ParalyzeField | Paralyze Field | GroundTarget | Control |
| Reveal | Reveal | AreaAroundCaster | Utility |

---

## CIRCLE 7 — SEVENTH CIRCLE

| SpellId | Display Name | Targeting | Tags |
|---|---|---|---|
| ChainLightning | Chain Lightning | AreaAroundTarget | Damage |
| EnergyField | Energy Field | GroundTarget | Control |
| Flamestrike | Flamestrike | SingleTarget | Damage |
| GateTravel | Gate Travel | GroundTarget | Utility |
| ManaVampire | Mana Vampire | SingleTarget | Debuff |
| MassDispel | Mass Dispel | AreaAroundTarget | Utility |
| MeteorSwarm | Meteor Swarm | AreaAroundTarget | Damage |
| Polymorph | Polymorph | Self | Utility, Buff |

---

## CIRCLE 8 — EIGHTH CIRCLE

| SpellId | Display Name | Targeting | Tags |
|---|---|---|---|
| Earthquake | Earthquake | AreaAroundCaster | Damage |
| EnergyVortex | Energy Vortex | GroundTarget | Summon |
| Resurrection | Resurrection | SingleTarget | Heal, Utility |
| SummonAirElemental | Summon Air Elemental | GroundTarget | Summon |
| SummonDaemon | Summon Daemon | GroundTarget | Summon |
| SummonEarthElemental | Summon Earth Elemental | GroundTarget | Summon |
| SummonFireElemental | Summon Fire Elemental | GroundTarget | Summon |
| SummonWaterElemental | Summon Water Elemental | GroundTarget | Summon |

---

## IMPLEMENTATION NOTES (CODE)

### Enum Generation Rules

- Create a `SpellId` enum that matches this catalog.
- Do not reorder.
- Consider assigning ranges by circle for clarity (optional):
  - Circle 1: 0–99
  - Circle 2: 100–199
  - …

### Future Spell Schools

These are intentionally excluded and must use separate catalogs:
- Necromancy
- Chivalry
- Bushido
- Ninjitsu
- Spellweaving
- Mysticism

---

## NEXT STEP

1. Generate `SpellId.cs` from this catalog
2. Create `SpellDef` ScriptableObjects for each spell
3. Implement spellbook learning (scroll unlock)
4. Implement server spellcasting pipeline (cast time, cooldown, interrupt)

