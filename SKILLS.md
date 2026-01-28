# SKILLS.md — ULTIMATE DUNGEON (AUTHORITATIVE)

Version: 0.1  
Last Updated: 2026-01-27  

---

## PURPOSE

Defines the **authoritative skill catalog** for *Ultimate Dungeon*.

This document is the single source of truth for:
- Skill identifiers
- Categories
- Default behavior flags
- Gain semantics

If a skill is not defined here, **it does not exist**.

---

## GLOBAL SKILL LAWS (DESIGN LOCK)

1. **Use-based progression only**
2. **Total Skill Cap = 700** (enforced server-side)
3. **Individual Skill Cap = 100**
4. **Manual cap management**
   - At cap, skills only increase if another skill is set to Decrease (−)
5. **Server authoritative**
6. **Skills modify outcomes, not permissions**

---

## SKILL VALUE MODEL

- Skills are stored as **floating-point values**
- Recommended display precision: **1 decimal place**

### Gain / Loss Step (PROPOSED DEFAULT)
- **Step size:** `0.1`

> This matches classic UO feel and allows fine-grained control.
> If you want integer-only skills later, this can be changed globally.

---

## SKILL LOCK STATES (LOCKED)

Each skill has one of three server-validated states:

- **Increase (+)**
  - Skill may gain when used

- **Decrease (−)**
  - Skill may be reduced to offset gains in other skills at cap

- **Locked (🔒)**
  - Skill neither gains nor loses

> Lock state is stored per-character and persisted server-side.

---

## SKILL CATEGORIES

Skills are grouped for clarity only.
Categories have **no gameplay effect**.

- Combat
- Magic
- Utility
- Crafting

---

## SKILL CATALOG (FOUNDATION SET)

### Combat Skills

| SkillId | Display Name | Notes |
|-------|--------------|-------|
| Swords | Swords | Blade weapons |
| Macing | Macing | Blunt weapons |
| Fencing | Fencing | Spears / rapiers |
| Wrestling | Wrestling | Unarmed combat |
| Archery | Archery | Bows / ranged |
| Tactics | Tactics | Damage modifier |
| Anatomy | Anatomy | Crit / damage insight |
| Parrying | Parrying | Defensive skill |

---

### Magic Skills

| SkillId | Display Name | Notes |
|-------|--------------|-------|
| Magery | Magery | Spell casting |
| Meditation | Meditation | Mana regen |
| EvaluatingIntelligence | Evaluating Intelligence | Spell effectiveness |
| ResistSpells | Resist Spells | Magical resistance |

---

### Utility Skills

| SkillId | Display Name | Notes |
|-------|--------------|-------|
| Healing | Healing | Bandages |
| Hiding | Hiding | Stealth entry |
| Stealth | Stealth | Silent movement |
| Lockpicking | Lockpicking | Locks / chests |

---

### Crafting Skills (Initial)

| SkillId | Display Name | Notes |
|-------|--------------|-------|
| Blacksmithing | Blacksmithing | Metal crafting |
| Tailoring | Tailoring | Cloth / leather |
| Carpentry | Carpentry | Wood items |
| Alchemy | Alchemy | Potions |

---

## GAIN ELIGIBILITY RULES

A skill gain check may occur when:
- The skill is marked **Increase (+)**
- The action meaningfully used the skill
- The action succeeded or partially succeeded
- Global cooldown allows a gain attempt

The **SkillGainSystem** is responsible for:
- Rolling gain chance
- Applying increments
- Resolving cap pressure via Decrease (−) skills

---

## IMPLEMENTATION NOTES

- `SkillId` should be an enum matching this table exactly
- `SkillDef` should be a ScriptableObject with:
  - SkillId
  - DisplayName
  - Category
  - Description
  - Governing Attribute (optional)

---

## NEXT STEPS

1. Generate `SkillId` enum from this catalog
2. Create `SkillDef` ScriptableObjects
3. Implement `PlayerSkillBook`
4. Implement `SkillGainSystem`

