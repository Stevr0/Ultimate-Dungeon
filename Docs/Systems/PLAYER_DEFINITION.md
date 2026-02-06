# PLAYER_DEFINITION.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 0.6  
Last Updated: 2026-02-06  

---

## PURPOSE

Defines the authoritative **PlayerDefinition** data asset used to spawn and validate Players.

This is a **ScriptableObject-first** contract:
- Designers edit the definition
- Runtime systems read from it
- The server enforces caps and rules derived from it

This document owns:
- Player baselines (attributes, vitals derivation, regen)
- Player-wide caps (attributes, vitals, resistances, skills)
- Currency rules (Coins: Held vs Banked)
- Player rules for insurance and death-loss interaction

This document does **not** own:
- SkillId list (owned by `SKILL_ID_CATALOG.md`)
- Combat formulas and order-of-operations (owned by `COMBAT_CORE.md`)
- Item schemas or item lists (owned by `ITEM_DEF_SCHEMA.md` / `ITEM_CATALOG.md`)
- Affix definitions, ranges, stacking (owned by `ITEM_AFFIX_CATALOG.md`)
- Status definitions (owned by `STATUS_EFFECT_CATALOG.md`)

If a player rule is not defined here (or in the referenced authoritative docs), **it does not exist**.

---

## DESIGN LOCKS (MUST ENFORCE)

1. **No classes / levels / XP**
2. **Skill progression is use-based**
3. **Individual Skill Max = 100**
4. **Total Skill Cap = 700**
5. **Starting Attributes = STR/DEX/INT = 10/10/10**
6. **Server authoritative** for vitals, stats, inventory, equipment, skills, statuses, currency

---

## DATA MODEL OVERVIEW

### Asset Type
- `PlayerDefinition : ScriptableObject`

### Runtime Ownership
- Server is authoritative.
- Client can request actions; server validates and applies state changes.

### Determinism Contract
All derived values must be computable from:
- PlayerDefinition (baselines and caps)
- Equipped items (properties)
- Active status effects
- Skill values

---

## AUTHORITATIVE FIELDS

> Field names are recommended; rename to match project conventions if needed.

### Identity
- `string definitionId` *(stable ID, e.g., "player_default")*
- `string displayName` *(editor-only friendly name)*

---

## ATTRIBUTES (LOCKED)

### Starting Attributes (LOCKED)
- `int baseSTR = 10`
- `int baseDEX = 10`
- `int baseINT = 10`

### Attribute Caps (LOCKED)
- **Absolute Max Cap per Attribute: 150**
  - `int attributeCap = 150`

**Meaning:**
- Effective STR/DEX/INT (after item affixes + status modifiers) may not exceed 150.

**Notes:**
- The stat aggregation *sources* and *policies* are owned by `PLAYER_COMBAT_STATS.md`.
- The cap values themselves are owned here.

---

## VITALS (LOCKED)

### Vital Caps
- **Absolute Max Cap per Vital: 150**
  - `int vitalCap = 150`

### Derivation Rules (LOCKED)
- **Max HP = STR**, up to 150
- **Max Stamina = DEX**, up to 150
- **Max Mana = INT**, up to 150

> Any value beyond the attribute-derived amount must come from:
> - Item bonuses (e.g., `Vital_MaxHealth` affix)
> - Status effects

### Regen Rules (LOCKED — Classic / UO-style)
- `float hpRegenPerSec = 0.05f` *(very slow; healing items/spells are primary)*
- `float staminaRegenPerSec = 1.0f`
- `float manaRegenPerSec = 0.5f`

### Regen Modifiers (SOURCE + CAP)
Regen bonuses may come from:
- Item affixes: `Regenerate_Health`, `Regenerate_Stamina`, `Regenerate_Mana` *(see `ITEM_AFFIX_CATALOG.md`)*
- Status effects (future)

**Regen cap policy (LOCKED):**
- Regen modifiers are clamped by a per-channel cap owned here.
- Recommended caps (v0.6):
  - `float regenHealthBonusCapPct = 0.50f` *(+50% max)*
  - `float regenStaminaBonusCapPct = 1.00f` *(+100% max)*
  - `float regenManaBonusCapPct = 1.00f` *(+100% max)*

> Exact application policy (sum/multiply) is owned by the aggregator; these are only the caps.

---

## RESISTANCES & CAPS (LOCKED)

- Damage channels:
  - Physical, Fire, Cold, Poison, Energy

- Baseline resistances:
  - All start at **0** unless modified

- Resistance Cap:
  - **70 max per resistance**
    - `int resistanceCap = 70`

> Aggregation of base armor resists + affix resists + status resists is owned by `PLAYER_COMBAT_STATS.md`.

---

## COMBAT BASELINES (LOCKED CONSTANTS)

These are **constants** consumed by `COMBAT_CORE.md` and the stat aggregator.
Combat formulas and execution order are defined in `COMBAT_CORE.md`.

### Hit / Defense Baselines
- `float baseHitChance = 0.50f`
- `float baseDefenseChance = 0.50f`

### Unarmed Baselines
- `float unarmedBaseSwingSpeedSeconds = 2.0f`
- `Vector2Int unarmedDamageRange = (1, 4)`

---

## SKILLS (LOCKED)

### Skill Caps
- `int totalSkillCap = 700`
- `int individualSkillMax = 100`

### Starting Skills (PROPOSED)
Start at 0 unless specified.
- A list of `(SkillId, startValue)`

**Rule:**
- `SkillId` values must exist in `SKILL_ID_CATALOG.md`.

### Skill Lock UI Model (LOCKED)
Each skill has one of three states:
- **Increase (+)** — skill is allowed to increase
- **Decrease (−)** — skill is allowed to decrease
- **Locked (🔒)** — skill will not change

**Authority:**
- The server validates all gain/loss requests against these locks.

---

## CURRENCY — COINS (LOCKED)

### Currency Name
- In-game currency is **Coins**.

### Currency Views (UI)
The player sees two values:
- **Held Coins**
- **Banked Coins**

### Rules

1. **Looting in the dungeon adds to Held Coins**
2. **Exiting the dungeon banks Held Coins automatically**
3. **Only Banked Coins can be used for economy actions**
4. **Held Coins drop on death (dungeon only)**

### Starting Values (Recommended)
- `int startingBankedCoins = 0`
- `int startingHeldCoins = 0`

---

## INVENTORY & BANK (AUTHORITATIVE)

### Inventory
- Player has a root container (backpack)
- Items are instance-based and may be containers
- Weight is computed from instances

### Bank
- Player has a bank container
- Player has a **Banked Coins** balance
- **Insurance Auto-Renew draws from Banked Coins** (LOCKED)

---

## ITEM INSURANCE (LOCKED RULES)

**Policy only.** This section specifies the design rules and does **not** imply
that insurance mechanics are implemented in code yet.

Insurance is **per-item** and optional.

### Runtime Fields Needed (on ItemInstance)
- `bool isInsured`
- `bool autoRenewInsurance`
- `int insuranceCostPaid` *(optional bookkeeping)*

### Insurance Purchase
- Insurance can be toggled on any item (server validated)
- Cost is paid **up-front** from **Banked Coins**

### On Death
- For each item that would be lootable:
  - If insured: item remains with player and is not placed in corpse
  - If auto-renew is enabled: server attempts to withdraw renewal cost from **Banked Coins** and re-apply insurance
  - If insufficient funds: insurance does not renew; item is treated uninsured for that death

> Insurance does not protect against crafting/enhancement breakage.

---

## CHARACTER SHEET PROJECTION (REFERENCE)

The Character Sheet UI should present a comprehensive list of possible player-facing stats.

- **Authoritative sources** remain the core docs (`PLAYER_COMBAT_STATS.md`, `ITEM_AFFIX_CATALOG.md`, `STATUS_EFFECT_CATALOG.md`).
- The UI list itself is maintained in:
  - `CHARACTER_SHEET_REFERENCE_LIST.md` *(UI projection list; non-authoritative)*

---

## DESIGN LOCK CONFIRMATION

This document is **authoritative**.

Any change must:
- Increment Version
- Update Last Updated
- Call out dependent impacts (aggregation, UI, save migration)
