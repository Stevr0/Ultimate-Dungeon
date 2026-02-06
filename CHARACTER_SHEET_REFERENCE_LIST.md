# CHARACTER_SHEET_REFERENCE_LIST.md — Ultimate Dungeon (UI Projection List)

Version: 0.1  
Last Updated: 2026-02-06  
Engine: Unity 6 (URP)  
Authority: **Non-authoritative UI list** (values are authoritative elsewhere)

---

## PURPOSE

This document exists to answer one UI question:

> **What can the Character Sheet show?**

It is a **single consolidated list** of all player-facing attributes, vitals, combat stats, affixes, and status effects.

### Authority Rules
- **This list does not define gameplay.**
- The meaning, caps, and aggregation policies remain owned by:
  - `PLAYER_DEFINITION.md`
  - `PLAYER_COMBAT_STATS.md`
  - `ITEM_AFFIX_CATALOG.md`
  - `STATUS_EFFECT_CATALOG.md`

---

## CHARACTER SHEET — SINGLE LIST (DISPLAY ORDER)

### A) Identity
- Character Name
- Account/Net Id *(if exposed)*
- Scene / Region *(if exposed)*

### B) Primary Attributes
- Strength (STR)
- Dexterity (DEX)
- Intelligence (INT)

> Display recommendation: show **Base** and **Effective** (after items/status), if you have both.

### C) Vitals
- Health (HP): Current / Max
- Stamina: Current / Max
- Mana: Current / Max

### D) Regeneration
- HP Regen / sec
- Stamina Regen / sec
- Mana Regen / sec

> If you implement regen bonuses, show Base + Bonus% + Final.

### E) Resistances
- Physical Resist
- Fire Resist
- Cold Resist
- Poison Resist
- Energy Resist

> Display recommendation: show `Final / Cap` (cap from `PLAYER_DEFINITION.md`).

### F) Combat Chances & Damage
- Hit Chance Increase (HCI%)
- Defense Chance Increase (DCI%)
- Damage Increase (DI%)

### G) Timing Modifiers
- Swing Speed Bonus (from affixes)
- Status Swing Time Multiplier
- Faster Casting (FC points)
- Casting Recovery (FCR points) *(if implemented)*
- Status Cast Time Multiplier
- Status Bandage Time Multiplier

### H) Weapon Context (if equipped)
- Weapon Damage: Min–Max
- Weapon Swing Speed (seconds)
- Stamina Cost per Swing
- Active Weapon Procs:
  - Hit Spell procs (chance)
  - Leech procs (percent)

### I) Action Gates
- Can Move
- Can Attack
- Can Cast
- Can Bandage

> These come from statuses (stun/paralyze/silence etc.).

### J) Movement
- Move Speed Multiplier *(from statuses)*

### K) Skills
- Total Skill Points Used / Cap (700)
- Individual skills list (SkillId → value)
- Skill lock state (Increase / Decrease / Locked) *(if exposed on sheet)*

### L) Currency
- Held Coins
- Banked Coins

---

## AFFIXES — COMPLETE LIST (DISPLAY IDS)

> **Affixes are numeric modifiers only** and live on `ItemInstance.affixes`.

### Combat — Chance & Damage
- `Combat_HitChance` — Hit Chance Increase
- `Combat_DefenseChance` — Defense Chance Increase
- `Combat_DamageIncrease` — Damage Increase
- `Combat_SwingSpeed` — Swing Speed

### Resists
- `Resist_Physical` — Physical Resist
- `Resist_Fire` — Fire Resist
- `Resist_Cold` — Cold Resist
- `Resist_Poison` — Poison Resist
- `Resist_Energy` — Energy Resist

### Magic — Casting Modifiers
- `Magic_FasterCasting` — Faster Casting
- `Magic_CastingRecovery` — Casting Recovery

### Stat Modifiers
- `Stat_MaxStrength` — Increase Max Strength
- `Stat_MaxDexterity` — Increase Max Dexterity
- `Stat_MaxInteligence` — Increase Max Inteligence

### Vital Modifiers
- `Vital_MaxHealth` — Increase Max Health
- `Vital_MaxStamina` — Increase Max Stamina
- `Vital_MaxMana` — Increase Max Mana

### Regeneration Modifiers
- `Regenerate_Health` — Regenerate Health
- `Regenerate_Stamina` — Regenerate Stamina
- `Regenerate_Mana` — Regenerate Mana

### Weapon Procs — Hit Spells
- `Hit_Lightning` — Hit Lightning
- `Hit_Fireball` — Hit Fireball
- `Hit_Harm` — Hit Harm

### Weapon Procs — Hit Leaches
- `Leech_Life` — Hit Life Leech
- `Leech_Mana` — Hit Mana Leech
- `Leech_Stamina` — Hit Stamina Leech

### Move — Speed Modifiers
- `Move_Speed` — Move Speed Increase

---

## STATUS EFFECTS — COMPLETE LIST (DISPLAY IDS)

> The Character Sheet should display active statuses as **Name + Remaining Duration** (and stacks if applicable).

### Control
- `Control_Stunned` — Stunned
- `Control_Paralyzed` — Paralyzed
- `Control_Rooted` — Rooted

### Debuffs
- `Debuff_Silenced` — Silenced
- `Debuff_Disarmed` — Disarmed
- `Dot_Poisoned` — Poisoned
- `Dot_Bleeding` — Bleeding
- `Debuff_Slowed` — Slowed

### Buffs
- `Buff_Hasted` — Hasted

### Utility / State
- `Utility_Invisible` — Invisible

> Append the remainder of `STATUS_EFFECT_CATALOG.md` here as you add/lock more statuses.

---

## UI IMPLEMENTATION NOTE (RECOMMENDED)

To avoid duplication drift:
- The Character Sheet should build its **Affix list** from `AffixCatalog` at runtime.
- The Character Sheet should build its **Status list** from `StatusEffectDefCatalog` (or equivalent) at runtime.

This doc remains as a human-readable “what exists” reference.

