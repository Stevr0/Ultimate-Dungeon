# SKILLDEF_CREATION_CHECKLIST.md — ULTIMATE DUNGEON (AUTHORITATIVE)

Version: 1.0  
Last Updated: 2026-01-27  

---

## PURPOSE

This document is a **step-by-step checklist** for creating all `SkillDef` ScriptableObject assets required by *Ultimate Dungeon*.

It ensures:
- One `SkillDef` exists for **every `SkillId`**
- Asset names, categories, and attributes are consistent
- No runtime system references an undefined skill

If a `SkillId` exists **without** a corresponding `SkillDef` asset, that is a **hard error**.

---

## GLOBAL RULES (LOCKED)

- **One SkillDef per SkillId**
- Asset naming must be stable
- SkillDefs contain **no runtime values** (no current skill level)
- Server systems read these assets; clients never author them

---

## FOLDER STRUCTURE (RECOMMENDED)

```
Assets/
 └─ ScriptableObjects/
    └─ Skills/
       ├─ Combat/
       ├─ Magic/
       ├─ Utility/
       └─ Crafting/
```

> Folder structure is for editor sanity only; runtime lookup should use `SkillId`.

---

## HOW TO CREATE A SKILLDEF ASSET

For **each** skill below:

1. Right‑click in the appropriate folder
2. Select **Create → Ultimate Dungeon → Skills → Skill Definition**
3. Name the asset exactly as specified
4. Fill out the Inspector fields using the table values

---

## COMBAT SKILLS

| Asset Name | SkillId | Category | Governing Attribute | affectsRegen | Notes |
|-----------|--------|----------|---------------------|--------------|-------|
| SkillDef_Swords | Swords | Combat | Dexterity | false | Blade weapons |
| SkillDef_Macing | Macing | Combat | Strength | false | Blunt weapons |
| SkillDef_Fencing | Fencing | Combat | Dexterity | false | Spears / rapiers |
| SkillDef_Wrestling | Wrestling | Combat | Strength | false | Unarmed combat |
| SkillDef_Archery | Archery | Combat | Dexterity | false | Ranged combat |
| SkillDef_Tactics | Tactics | Combat | None | false | Damage modifier |
| SkillDef_Anatomy | Anatomy | Combat | Intelligence | false | Crit / damage insight |
| SkillDef_Parrying | Parrying | Combat | Dexterity | false | Defensive blocking |

---

## MAGIC SKILLS

| Asset Name | SkillId | Category | Governing Attribute | affectsRegen | Notes |
|-----------|--------|----------|---------------------|--------------|-------|
| SkillDef_Magery | Magery | Magic | Intelligence | false | Spell casting |
| SkillDef_Meditation | Meditation | Magic | Intelligence | true | Mana regeneration |
| SkillDef_EvaluatingIntelligence | EvaluatingIntelligence | Magic | Intelligence | false | Spell power |
| SkillDef_ResistSpells | ResistSpells | Magic | Intelligence | false | Magical resistance |

---

## UTILITY SKILLS

| Asset Name | SkillId | Category | Governing Attribute | affectsRegen | Notes |
|-----------|--------|----------|---------------------|--------------|-------|
| SkillDef_Healing | Healing | Utility | Dexterity | false | Bandages |
| SkillDef_Hiding | Hiding | Utility | Dexterity | false | Enter stealth |
| SkillDef_Stealth | Stealth | Utility | Dexterity | false | Silent movement |
| SkillDef_Lockpicking | Lockpicking | Utility | Dexterity | false | Locks / chests |

---

## CRAFTING SKILLS (INITIAL SET)

| Asset Name | SkillId | Category | Governing Attribute | affectsRegen | Notes |
|-----------|--------|----------|---------------------|--------------|-------|
| SkillDef_Blacksmithing | Blacksmithing | Crafting | Strength | false | Metal crafting |
| SkillDef_Tailoring | Tailoring | Crafting | Dexterity | false | Cloth / leather |
| SkillDef_Carpentry | Carpentry | Crafting | Strength | false | Woodworking |
| SkillDef_Alchemy | Alchemy | Crafting | Intelligence | false | Potions |

---

## OPTIONAL TUNING (SAFE DEFAULTS)

For all skills unless explicitly needed:
- `gainChanceMultiplier = 1.0`
- `affectsRegen = false`

Only **Meditation** should have `affectsRegen = true` initially.

---

## VALIDATION CHECKLIST

Before moving on:

- [ ] Every `SkillId` has exactly **one** `SkillDef` asset
- [ ] No duplicate SkillIds
- [ ] Asset names match the table exactly
- [ ] Categories match SKILLS.md
- [ ] GoverningAttribute is set consistently

---

## NEXT SYSTEM TO IMPLEMENT

Once all SkillDefs exist:

➡ **PlayerDefinition ScriptableObject**

This will reference:
- Starting skills
- Skill cap (700)
- Default lock states

---

## DESIGN LOCK NOTICE

This checklist is **authoritative**.
Any new skill added in the future requires:
1. Updating `SKILLS.md`
2. Updating `SkillId.cs`
3. Adding a new row here
4. Creating a new `SkillDef` asset

