# PLAYER_DEFINITION.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 0.5  
Last Updated: 2026-01-28  

---

## PURPOSE

Defines the authoritative **PlayerDefinition** data asset used to spawn and validate Players.

This is a **ScriptableObject-first** contract:
- Designers edit the definition
- Runtime systems read from it
- The server enforces caps and rules derived from it

This document owns:
- Player baselines (attributes, vitals derivation, regen)
- Player-wide caps (vitals cap, resistance cap, skill caps)
- Currency rules (Coins: Held vs Banked)
- Player rules for insurance and death-loss interaction

This document does **not** own:
- SkillId list (owned by `SKILL_ID_CATALOG.md`)
- Combat formulas and order-of-operations (owned by `COMBAT_CORE.md`)
- Item schemas or item lists (owned by `ITEM_DEF_SCHEMA.md` / `ITEM_CATALOG.md`)

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

## STARTING ATTRIBUTES (LOCKED)

- `int baseSTR = 10`
- `int baseDEX = 10`
- `int baseINT = 10`

---

## VITALS (LOCKED)

### Vital Caps
- **Absolute Max Cap per Vital: 150**

### Derivation Rules (LOCKED)
- **Max HP = STR** *2
- **Max Stamina = DEX** *2
- **Max Mana = INT** *2

> Any value beyond the attribute-derived amount must come from:
> - Item bonuses (e.g., `Vital_MaxHP` affix)
> - Status effects

### Regen Rules (LOCKED — Classic / UO-style)
- `float hpRegenPerSec = 0.05f` *(very slow; healing items/spells are primary)*
- `float staminaRegenPerSec = 1.0f`
- `float manaRegenPerSec = 0.5f`

---

## RESISTANCES & CAPS (LOCKED)

- Damage channels:
  - Physical, Fire, Cold, Poison, Energy

- Baseline resistances:
  - All start at **0** unless modified

- Resistance Cap:
  - **70 max per resistance**

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

**Meaning:**
- A player can reach **7 skills at 100** (700 total), or
- More skills at lower values (e.g., 14 skills at 50), as long as:
  - No individual skill exceeds **100**
  - Total does not exceed **700**

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

> Skill check + gain logic is executed by `SkillUseResolver` / `SkillGainSystem` per `PROGRESSION.md`.

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
   - Coins picked up while in dungeon content are added to **Held Coins**.

2. **Exiting the dungeon banks Held Coins automatically**
   - When a player exits the dungeon (returns to village/safe zone), the server:
     - Moves **all Held Coins → Banked Coins**
     - Held Coins becomes 0

3. **Only Banked Coins can be used for economy actions**
   Players can only:
   - Purchase from shops
   - Trade
   - Pay insurance
   using **Banked Coins**.

4. **Held Coins drop on death (dungeon only)**
   - If a player dies in the dungeon:
     - Their **Held Coins remain on the corpse**
     - Banked Coins are not affected

### Starting Values (Recommended)
- `int startingBankedCoins = 0`
- `int startingHeldCoins = 0`

> Runtime storage should live in an authoritative player wallet component.

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

> Container schemas and item lists are defined by the item system docs.

---

## ITEM INSURANCE (LOCKED RULES)

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

## DEATH & LOOT INTERACTION (PLAYER RULES)

On player death (dungeon):
- Corpse container is created
- Uninsured items transfer to corpse
- Insured items remain with player
- Held Coins remain on corpse
- Banked Coins are untouched

> Exact corpse transfer mechanics are implemented by the death/loot system, but must obey these rules.

---

## NETWORKING OWNERSHIP RULES

### Server-Authoritative State
- Vitals (current/max)
- Attributes + derived stats
- Skill values + lock states
- Inventory + bank container
- Held Coins / Banked Coins balances
- Equipment state
- Status effects
- Death/respawn

### Client Responsibilities
- Send input intents (move/attack/interact)
- Render UI from replicated state

---

## IMPLEMENTATION DEPENDENCIES (NEXT)

With this document locked, you can implement safely:
1. `PlayerDefinition` ScriptableObject
2. `PlayerCore` (server binds runtime components)
3. `PlayerStats` + `PlayerVitals`
4. `PlayerSkillBook` + replication
5. `PlayerWallet` (Held/Banked)
6. Death pipeline hooks (corpse drop rules)

---

## DESIGN LOCK CONFIRMATION

This document is **authoritative**.

Any change must:
- Increment Version
- Update Last Updated
- Explicitly call out combat/save-data implications

