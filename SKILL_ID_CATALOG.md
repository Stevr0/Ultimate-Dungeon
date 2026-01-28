# SKILL_ID_CATALOG.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 1.0  
Last Updated: 2026-01-28  
Engine: Unity 6 (URP)  
Networking: Netcode for GameObjects (NGO)  
Authority: Server-authoritative  

---

## PURPOSE

Defines the **authoritative SkillId catalog** for *Ultimate Dungeon*.

This document is the single source of truth for:
- Stable skill identifiers (`SkillId`) *(append-only)*
- Skill grouping (Combat / Magic / Crafting / Utility)
- Governing stat alignment (STR / DEX / INT) *(used by progression + stat gain weighting)*
- High-level usage tags (what systems are allowed to call this skill)

If a skill is not listed here, **it does not exist**.

This catalog must align with:
- `PLAYER_DEFINITION.md` (caps: individual 100, total 700)
- `PROGRESSION.md` (SkillUseResolver + StatGainSystem)
- `ITEM_DEF_SCHEMA.md` (requiredCombatSkill for weapons/tools)
- `COMBAT_CORE.md` (skill use hooks)

---

## DESIGN LOCKS (MUST ENFORCE)

1. **Stable IDs (append-only)**
   - Never reorder or rename shipped SkillIds.
   - Only append new skills at the end.

2. **No classes / no levels / no XP**
   - Skills are the progression surface.

3. **Server authoritative**
   - Skill values are written only by the server.

4. **Catalog vs Design**
   - This file defines **SkillIds + taxonomy**.
   - Detailed behavior (skill checks, gains, formulas) lives in:
     - `SKILLS.md` (skill semantics)
     - `PROGRESSION.md` (use → check → gain bridge)

---

## AUTHORITATIVE ENUMS

### SkillCategory (authoritative)
- **Combat**
- **Magic**
- **Crafting**
- **Utility**

### GoverningStat (authoritative)
- **STR**
- **DEX**
- **INT**

### SkillUseTag (authoritative flags)
Used to gate which systems can legitimately report “meaningful use”.
- **WeaponCombat**
- **RangedCombat**
- **DefenseCombat**
- **MagicCasting**
- **MagicDefense**
- **CraftingEnhancement**
- **WorldInteraction**
- **SupportHealing**

> Tags are for validation and analytics, not UI.

---

## SKILL LIST (AUTHORITATIVE)

> **IMPORTANT:** Order is authoritative. Append-only.

| SkillId | Display Name | Category | Governing Stat | Use Tags |
|---|---|---|---|---|
| Swords | Swords | Combat | STR | WeaponCombat |
| Macing | Macing | Combat | STR | WeaponCombat |
| Fencing | Fencing | Combat | DEX | WeaponCombat |
| Archery | Archery | Combat | DEX | RangedCombat |
| Wrestling | Wrestling | Combat | STR | WeaponCombat |
| Tactics | Tactics | Combat | STR | WeaponCombat |
| Parrying | Parrying | Combat | STR | DefenseCombat |
| Anatomy | Anatomy | Combat | STR | WeaponCombat |
| Healing | Healing | Utility | DEX | SupportHealing |
| Magery | Magery | Magic | INT | MagicCasting |
| EvaluatingIntelligence | Evaluating Intelligence | Magic | INT | MagicCasting |
| Meditation | Meditation | Magic | INT | MagicCasting |
| ResistSpells | Resist Spells | Magic | INT | MagicDefense |
| Blacksmithing | Blacksmithing | Crafting | STR | CraftingEnhancement |
| Tailoring | Tailoring | Crafting | DEX | CraftingEnhancement |
| Alchemy | Alchemy | Crafting | INT | CraftingEnhancement |
| Hiding | Hiding | Utility | DEX | WorldInteraction |
| Stealth | Stealth | Utility | DEX | WorldInteraction |
| Lockpicking | Lockpicking | Utility | DEX | WorldInteraction |

---

## INTEGRATION RULES (LOCKED)

### 1) Weapons must reference SkillId
All weapons set `requiredCombatSkill` using a `SkillId` from this catalog.

- Swords weapons → `Swords`
- Maces/weapons → `Macing`
- Spears/kryss/etc → `Fencing`
- Bows/crossbows → `Archery`
- Unarmed fallback → `Wrestling` *(optional; Combat may treat unarmed as Wrestling for progression)*

### 2) Crafting enhancement skill selection
Enhancement skill selection is based on item family:
- Metal weapons/armor/shields → `Blacksmithing`
- Cloth/leather armor → `Tailoring`
- Consumables/potions → `Alchemy` *(future; if implemented)*

Exact enhancement success/breakage math is defined outside this catalog.

### 3) Progression bridge uses SkillId
`SkillUseResolver` and `SkillGainSystem` accept only `SkillId` from this catalog.

### 4) Stat-alignment is taxonomy, not a hard rule
`GoverningStat` is used as an input to stat gain weighting.
Final stat gain formulas and caps are defined in `PROGRESSION.md` and `PLAYER_DEFINITION.md`.

---

## IMPLEMENTATION NOTES (CODE)

### Enum generation rules
- Generate a `SkillId` enum that matches the table order.
- Do not reorder.
- Prefer explicit integer values only if you need stable serialization.

### Suggested enum naming
- Use PascalCase identifiers exactly as listed in `SkillId` column.
- Display name is separate and belongs in `SkillDef` (in `SKILLS.md`).

---

## NEXT STEPS

1. Update `SKILLS.md` to:
   - Remove any duplicated SkillId lists
   - Define `SkillDef` schema + per-skill semantics
   - Reference this catalog as the source of SkillIds

2. Update `ITEM_DEF_SCHEMA.md` to ensure `requiredCombatSkill : SkillId` aligns 1:1.

3. Update `PROGRESSION.md` to reference this catalog for stat alignment mapping.

---

## DESIGN LOCK CONFIRMATION

This catalog is **authoritative**.

Any change must:
- Increment Version
- Update Last Updated
- Append-only changes (no reorder/rename) unless a migration plan is provided

