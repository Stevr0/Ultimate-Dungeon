# SKILLS.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 1.0  
Last Updated: 2026-01-28  
Engine: Unity 6 (URP)  
Networking: Netcode for GameObjects (NGO)  
Authority: Server-authoritative  

---

## PURPOSE

Defines the **authoritative skill system semantics** for *Ultimate Dungeon*.

This document specifies:
- The **Skill value model** (precision, bounds)
- **Skill lock states** (+ / − / 🔒) and what they mean
- The **SkillDef** data contract (what designers author)
- What it means to **meaningfully use** a skill

This document does **not** own the list of SkillIds.

### Single sources of truth

- **Skill identifiers + taxonomy** (append-only): `SKILL_ID_CATALOG.md`
- **Caps (Individual Max 100, Total 700)** and lock model: `PLAYER_DEFINITION.md`
- **Use → check → gain bridge**: `PROGRESSION.md`

If a rule is not defined here (or in the referenced authoritative docs above), **it does not exist**.

---

## DESIGN LOCKS (MUST ENFORCE)

1. **No classes / levels / XP**
2. **Use-based progression only**
3. **Server authoritative**
   - Only the server may modify skill values and lock states.
4. **Skills modify outcomes, not permissions**
   - Skills influence success chances, magnitudes, and timings.
   - Permission gates (e.g., “can equip”, “can enter area”) must be authored as separate systems.

---

## SKILL VALUE MODEL (LOCKED)

### Storage
- Skills are stored as **floating-point values**.
- Recommended UI display: **1 decimal place**.

### Bounds
- Individual skill maximum and total skill cap are defined in `PLAYER_DEFINITION.md`.
- Runtime effective skill may exceed trained value via item bonuses, but:
  - **Item bonuses do not change trained skill** (trained value remains 0–100)
  - Bonus stacking rules are defined by the affix catalog / combat stat aggregation.

### Gain step size (PROPOSED — Not Locked)
- Default gain increment: **0.1**

> Tuning note: you can later move to 0.2 or 0.5 globally without changing the model.

---

## SKILL LOCK STATES (LOCKED)

Each skill has one of three **server-validated** lock states:

- **Increase (+)**
  - Skill may gain when meaningfully used.

- **Decrease (−)**
  - Skill is eligible to be reduced by the cap-pressure solver when other skills gain at total cap.

- **Locked (🔒)**
  - Skill neither gains nor loses.

Authoritative cap-pressure behavior (when total cap is reached) is defined in `PLAYER_DEFINITION.md` and executed by the progression systems.

---

## SKILL DEFINITIONS (SkillDef) — DATA CONTRACT

Each SkillId listed in `SKILL_ID_CATALOG.md` should have a corresponding authored `SkillDef` asset.

### Asset type
- `SkillDef : ScriptableObject`

### Required fields (AUTHORITATIVE)
- `SkillId id` *(must exist in `SKILL_ID_CATALOG.md`)*
- `string displayName`
- `SkillCategory category` *(from `SKILL_ID_CATALOG.md`)*
- `GoverningStat governingStat` *(from `SKILL_ID_CATALOG.md`)*
- `string description` *(tooltip / help text)*

### Optional fields (FOUNDATION)
- `float uiSortOrder`
- `string[] useTags` *(for validation/telemetry; must match catalog tag set if used)*

### Runtime note (LOCKED)
- `SkillDef` is **immutable** at runtime.
- Skill values and lock states live on the player (server) in `PlayerSkillBook`.

---

## MEANINGFUL USE (PROGRESSION GATE)

A gameplay system may request a skill check / gain attempt only when **all** are true:

1. The caller asserts the action is a **meaningful use** of that skill.
2. The player’s lock state for that skill is **Increase (+)**.
3. The action outcome is **Success** or **Partial Success** (caller provides outcome).
4. Any per-skill or global anti-spam throttles allow an attempt.

The authoritative resolver for:
- skill checks
- success/failure math
- gain attempts
- stat gain attempts

…is `SkillUseResolver` as defined in `PROGRESSION.md`.

---

## SYSTEM INTEGRATION RULES (LOCKED)

### Combat
Combat systems must never “manually” change skills.
They must call `SkillUseResolver` for:
- weapon hits (combat skill + Tactics)
- bandage completion (Healing)
- parry/block events (Parrying)

### Magic
Spellcasting must call `SkillUseResolver` for:
- Magery (casting)
- Evaluating Intelligence (spell effectiveness context)
- Resist Spells (defensive checks when applicable)

### Crafting / Enhancement
Enhancement actions must call `SkillUseResolver` for:
- the relevant crafting skill (e.g., Blacksmithing/Tailoring)

> Which crafting skill applies to which item family is owned by the crafting/enhancement system.

---

## DE-DUPLICATION RULE

- **Do not list SkillIds here.**
  - The authoritative list is `SKILL_ID_CATALOG.md`.
- If you need to add a new skill:
  1) Append it to `SKILL_ID_CATALOG.md`
  2) Create its `SkillDef`
  3) Add semantics/notes here only if there are system-wide rules that apply

---

## OPEN QUESTIONS (PROPOSED — NOT LOCKED)

1. Global gain step size (default 0.1) final tuning
2. Whether some skills should prefer different gain throttles (per-skill cooldowns)

> New skills (e.g., Carpentry, Tinkering) must be introduced by appending to `SKILL_ID_CATALOG.md` first.

---

## NEXT IMPLEMENTATION ARTIFACTS

1. Generate `SkillId.cs` from `SKILL_ID_CATALOG.md` (append-only)
2. Implement `SkillDef` ScriptableObject
3. Implement `PlayerSkillBook` (server authoritative)
4. Implement `SkillUseResolver` + `StatGainSystem` (per `PROGRESSION.md`)

---

## DESIGN LOCK CONFIRMATION

This document is **authoritative**.

Any change must:
- Increment Version
- Update Last Updated
- Call out save-data implications (adding new SkillIds affects serialization)

