# ITEM_AFFIX_CATALOG.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 1.2  
Last Updated: 2026-01-28  
Engine: Unity 6 (URP)  
Authority: Server-authoritative  
Determinism: Required (server-seeded for rolls)

---

## PURPOSE

Defines the **authoritative affix catalog** for *Ultimate Dungeon*.

This document is the single source of truth for:
- Stable `AffixId` list (append-only)
- Affix value ranges and tiers
- Stacking policies (Sum / HighestOnly / NoStack)
- Slot eligibility (weapon-only, armor-only, jewelry-only, etc.)
- The authoritative **AffixCountResolver** rules for:
  - Dungeon loot drops
  - Player enhancement (crafting)

This document does **not**:
- Define ItemDef fields (owned by `ITEM_DEF_SCHEMA.md`)
- Define the base item list (owned by `ITEM_CATALOG.md`)
- Define combat execution (owned by `COMBAT_CORE.md`)
- Define spell requirements (mana/reagents/cast time) (owned by Magic docs)

---

## DESIGN LOCKS (MUST ENFORCE)

1. **Stable IDs (append-only)**
   - Never reorder or rename shipped AffixIds.
   - Only append.

2. **Affix cap per item = 5**
   - No item instance may ever exceed 5 affixes.

3. **Affix ranges are always 0..N**
   - Unless explicitly stated otherwise for a specific affix.

4. **Server-only rolling**
   - Affixes are rolled by the server only.

5. **No duplication of affix-count math**
   - Affix count determination is defined only here via **AffixCountResolver**.

---

## CORE TERMS

### AffixId
A stable identifier for an affix type.

### AffixInstance
A rolled affix on an ItemInstance:
- `AffixId id`
- `int magnitude` or `float magnitude`

### StackingPolicy
- **Sum**: add magnitudes
- **HighestOnly**: take highest magnitude
- **NoStack**: at most one instance allowed

---

## AFFIX COUNT RESOLVER (AUTHORITATIVE)

Affix count is determined by one conceptual resolver used by:
- Loot drop generation
- Player enhancement generation

Inputs:
- Source: `LootDrop` or `Enhancement`
- Item family and allowed pool(s)
- (Enhancement) Player relevant crafting skill (e.g., Blacksmithing/Tailoring)

Output:
- `affixCount` in range `0..5`

### Global cap
- `affixCount <= 5` always.

---

### A) Loot Drops (Dungeon) — Affix Count

Loot generation produces magical items with an affix count based on dungeon tier/rarity.

**Rule (LOCKED):** Loot affix count is always `0..5`.

**v1 placeholder (PROPOSED — Not Locked):**
- Common: 0–1
- Uncommon: 1–2
- Rare: 2–3
- Epic: 3–4
- Legendary: 4–5

> Exact rarity definitions and tier tables can be locked later in a Loot doc.

---

### B) Enhancement (Crafting) — Affix Count

Enhancement attempts add/roll affixes based on player crafting skill.

**Rule (LOCKED):** Enhancement affix count range is determined by the player’s relevant skill.

Define:
- `skill` = player’s crafting skill value (0–100)
- `Nmax(skill)` = maximum number of affixes allowed to be produced by enhancement

**Nmax(skill) (LOCKED):**

| Skill Range | Nmax |
|---:|---:|
| 0–19.9 | 0 |
| 20–39.9 | 1 |
| 40–59.9 | 2 |
| 60–79.9 | 3 |
| 80–99.9 | 4 |
| 100.0 | 5 |

**Rule (LOCKED):**
- Enhancement rolls `affixCount` uniformly (or per enhancement rules) within `0..Nmax(skill)`.

> Whether it’s uniform or weighted is owned by the Enhancement system spec (future).

---

## HIT SPELLS & HIT LEACHES (WEAPON-ONLY)

### Critical clarification (LOCKED)
**Hit Spells reuse spell-like payload logic, not spell definitions.**
They do **not** require mana, reagents, cast time, targeting validation, or line-of-sight checks.

> Combat execution order and proc timing are owned by `COMBAT_CORE.md`.

---

## AFFIX CATALOG (AUTHORITATIVE)

> **IMPORTANT:** Append-only. Do not reorder.

### Legend for Columns
- **Type**: Percent / Flat / ProcChance / ProcPercent
- **Range**: authored as `0..N` unless stated
- **Eligible**: Weapon / Armor / Shield / Jewelry / Any

---

## COMBAT — CHANCE & DAMAGE

| AffixId | Display Name | Type | Range | Stacking | Eligible | Notes |
|---|---|---|---|---|---|---|
| Combat_HitChance | Hit Chance Increase | Percent | 0..45% | Sum | Any | Aggregated by `PLAYER_COMBAT_STATS.md` |
| Combat_DefenseChance | Defense Chance Increase | Percent | 0..45% | Sum | Any | Aggregated by `PLAYER_COMBAT_STATS.md` |
| Combat_DamageIncrease | Damage Increase | Percent | 0..100% | Sum | Any | Aggregated by `PLAYER_COMBAT_STATS.md` |
| Combat_SwingSpeed | Swing Speed | Percent | 0..30% | HighestOnly | Any | Time formula owned by `COMBAT_CORE.md` |

---

## RESISTS

| AffixId | Display Name | Type | Range | Stacking | Eligible | Notes |
|---|---|---|---|---|---|---|
| Resist_Physical | Physical Resist | Flat | 0..15 | Sum | Armor/Shield | Capped by Player resist cap |
| Resist_Fire | Fire Resist | Flat | 0..15 | Sum | Armor/Shield |  |
| Resist_Cold | Cold Resist | Flat | 0..15 | Sum | Armor/Shield |  |
| Resist_Poison | Poison Resist | Flat | 0..15 | Sum | Armor/Shield |  |
| Resist_Energy | Energy Resist | Flat | 0..15 | Sum | Armor/Shield |  |

---

## MAGIC — CASTING MODIFIERS

| AffixId | Display Name | Type | Range | Stacking | Eligible | Notes |
|---|---|---|---|---|---|---|
| Magic_FasterCasting | Faster Casting | Flat | 0..4 | HighestOnly | Jewelry | Time scaling owned by Magic rules |

---

## VITAL MODIFIERS

| AffixId | Display Name | Type | Range | Stacking | Eligible | Notes |
|---|---|---|---|---|---|---|
| Vital_MaxHP | Increase Max HP | Flat | 0..25 | Sum | Any | Final caps owned by `PLAYER_DEFINITION.md` |
| Vital_MaxStamina | Increase Max Stamina | Flat | 0..25 | Sum | Any |  |
| Vital_MaxMana | Increase Max Mana | Flat | 0..25 | Sum | Any |  |

---

## WEAPON PROCS — HIT SPELLS

| AffixId | Display Name | Type | Range | Stacking | Eligible | Notes |
|---|---|---|---|---|---|---|
| Hit_Lightning | Hit Lightning | ProcChance | 0..30% | NoStack | Weapon | Applies lightning damage payload |
| Hit_Fireball | Hit Fireball | ProcChance | 0..30% | NoStack | Weapon | Applies fire damage payload |
| Hit_Harm | Hit Harm | ProcChance | 0..30% | NoStack | Weapon | Applies energy/negative payload |

> Add more hit spells as needed by appending.

---

## WEAPON PROCS — HIT LEACHES

| AffixId | Display Name | Type | Range | Stacking | Eligible | Notes |
|---|---|---|---|---|---|---|
| Leech_Life | Hit Life Leech | ProcPercent | 0..20% | NoStack | Weapon | Restores HP based on final damage |
| Leech_Mana | Hit Mana Leech | ProcPercent | 0..20% | NoStack | Weapon | Restores Mana based on final damage |
| Leech_Stamina | Hit Stamina Leech | ProcPercent | 0..20% | NoStack | Weapon | Restores Stam based on final damage |

---

## ELIGIBILITY RULES (LOCKED)

- Hit Spells and Leaches are **weapon-only**.
- Faster Casting is **jewelry-only** (v1).
- Resist affixes are **armor/shield-only** (v1).

> If eligibility expands later, update this doc and append new affixes; do not silently change meaning.

---

## VALIDATION RULES (LOCKED)

An Editor validator must ensure:
- No duplicate AffixIds
- All magnitudes are within declared ranges
- Stacking policies are obeyed (HighestOnly / NoStack)
- Weapon-only affixes never appear on non-weapons
- Total affix count on any ItemInstance never exceeds 5

---

## OPEN QUESTIONS (PROPOSED — NOT LOCKED)

- Full affix set expansion (loot depth)
- Loot rarity tables (exact affix count distributions)
- Whether some affixes should be mutually exclusive by pool

---

## DESIGN LOCK CONFIRMATION

This document is **authoritative**.

Any change must:
- Increment Version
- Update Last Updated
- Call out save-data and balance implications

