# PLAYER_DEFINITION.md — ULTIMATE DUNGEON (AUTHORITATIVE)

Version: 0.2  
Last Updated: 2026-01-27  

---

## PURPOSE

Defines the authoritative **PlayerDefinition** data asset used to spawn and validate Players.

This is a **ScriptableObject-first** contract:
- Designers edit the definition
- Runtime systems read from it
- The server enforces caps and rules derived from it

---

## DESIGN LOCKS (MUST ENFORCE)

1. **No classes / levels / XP**
2. **Skill progression is use-based**
3. **Total Skill Cap = 700**
4. **Starting Attributes = STR/DEX/INT = 10/10/10**
5. **Server authoritative** for vitals, stats, inventory, equipment, skills, statuses

---

## DATA MODEL OVERVIEW

### Asset Type
- `PlayerDefinition` (ScriptableObject)

### Runtime Owner
- Server is authoritative.
- Client can request actions; server validates and applies state changes.

### Determinism Contract
- All derived values must be computable from:
  - PlayerDefinition (base rules)
  - Equipped items (properties)
  - Active status effects
  - Skill values

---

## AUTHORITATIVE FIELDS

> Field names are recommended; we can rename to match your project conventions.

### Identity
- `string definitionId` (stable ID, e.g., "player_default")
- `string displayName` (editor-only friendly name)

### Starting Attributes (LOCKED)
- `int baseSTR = 10`
- `int baseDEX = 10`
- `int baseINT = 10`

### Vital Caps & Scaling (LOCKED)

- **Absolute Max Cap per Vital: 150**
- **Current Max Vitals are derived**, not fixed.

#### Derivation Rules (LOCKED)
- **Max HP = STR**, up to 150
- **Max Stamina = DEX**, up to 150
- **Max Mana = INT**, up to 150

> Any value beyond the attribute-derived amount (e.g. STR 100 → HP 100) must come from:
> - Item bonuses ("Increase Max HP", etc.)
> - Status effects

> Example:
> - STR = 100 → Base Max HP = 100
> - Item bonuses = +20 HP
> - Status bonus = +10 HP
> - Final Max HP = 130 (≤ 150 cap)

### Vitals Regen Rules (LOCKED — Classic / UO-style)
- `float hpRegenPerSec = 0.05f` (very slow; healing items/spells are primary)
- `float staminaRegenPerSec = 1.0f`
- `float manaRegenPerSec = 0.5f`

> Regen pacing is intentionally **slow/classic (UO-like)** and is heavily modified by:
> - Encumbrance
> - Status effects
> - Meditation (mana)
> - Sitting/resting states (if added later)
- `float staminaRegenPerSec = 1.0f`
- `float manaRegenPerSec = 0.5f`

> Regen is modified by:
> - encumbrance
> - status effects
> - meditation (mana)

### Combat Baselines (PROPOSED, NOT LOCKED)
Defines the baseline math before items/skills modify it.
- `float baseHitChance = 0.50f`
- `float baseDefenseChance = 0.50f`
- `float baseSwingSpeedSeconds = 2.0f`
- `Vector2Int unarmedDamageRange = (1, 4)`

### Resistances & Caps (LOCKED)

- Damage channels:
  - Physical, Fire, Cold, Poison, Energy

- Baseline resistances:
  - All start at **0** unless modified

- Resistance Cap:
  - **70 max per resistance** (unless overridden by world rules)

---

## SKILLS

### Skill Cap Rules (LOCKED)
- `int totalSkillCap = 700`

### Starting Skills (PROPOSED)
Start at 0 unless specified.
- A list of `(SkillId, startValue)`

### Skill Gain Rules (LOCKED)

Skill gain is enforced server-side via `SkillGainSystem`.

- Gains are tied to successful/meaningful use
- Gains can be throttled by time windows
- **At Total Skill Cap (700):**
  - **Manual control only**
  - Player must explicitly lower another skill before a gain can occur

#### Skill Lock UI Model (LOCKED)
Each skill has one of three states:
- **Increase (+)** — skill is allowed to increase
- **Decrease (−)** — skill is allowed to decrease
- **Locked (🔒)** — skill will not change

> The server validates all gain/loss requests against these locks.
> UI icons are client-side; authority remains server-side.

---

## EQUIPMENT SLOTS (LOCKED)

### Weapons
- MainHand
- OffHand
- BothHands (two-handed items occupy both)

### Armor / Wearables
- Head
- Torso
- Arms
- Hands
- Legs
- NeckArmor (gorget/scarf)

### Jewelry
- Amulet
- Ring1
- Ring2
- Earrings

---

## INVENTORY & BANK (AUTHORITATIVE)

### Inventory
- Player has a root container (backpack)
- Items are instance-based and may be containers
- Weight is computed from instances

### Bank
- Player has a bank container and bank gold balance
- **Insurance Auto-Renew draws from bank** (LOCKED)

---

## ITEM INSURANCE (LOCKED RULES)

### Fields Needed (on Item Instance)
- `bool isInsured`
- `bool autoRenewInsurance`
- `int insuranceCostPaid` (optional bookkeeping)

### Insurance Purchase
- Insurance can be toggled on any item (server validated)
- Cost is paid **up-front**

### On Death
- For each item that would be lootable:
  - If insured: item remains with player (or returns on respawn) and is not placed in corpse
  - If auto-renew is enabled: server attempts to withdraw renewal cost from bank and re-apply insurance
  - If insufficient funds: insurance does not renew; item is treated uninsured for that death

> NOTE: Whether insurance "persists" without auto-renew is an implementation detail.
> Recommended: insurance is a state that can lapse; auto-renew keeps it maintained.

---

## NETWORKING OWNERSHIP RULES

### Server-Authoritative State
- Vitals (current/max)
- Attributes + derived stats
- Skill values
- Inventory + bank
- Equipment slots
- Status effects
- Death/respawn

### Client Responsibilities
- Send input intents (move/attack/interact)
- Render UI from replicated state

---

## OPEN QUESTIONS

All numeric and behavioral questions in this document are now **LOCKED**.

---

## NEXT IMPLEMENTATION ARTIFACTS

After locking the numeric open questions, we implement:
1. `SkillId` enum + `SkillDef` ScriptableObjects
2. `PlayerDefinition` ScriptableObject
3. `PlayerCore` MonoBehaviour (server binds runtime components)
4. `PlayerStats` + `PlayerVitals`
5. `PlayerSkillBook` + `SkillGainSystem`
6. `EquipmentComponent` + replication glue

