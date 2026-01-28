# PLAYER_DEFINITION.md — ULTIMATE DUNGEON (AUTHORITATIVE)

Version: 0.3  
Last Updated: 2026-01-28

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
3. **Individual Skill Max = 100**
4. **Total Skill Cap = 700**
5. **Starting Attributes = STR/DEX/INT = 10/10/10**
6. **Server authoritative** for vitals, stats, inventory, equipment, skills, statuses, currency

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

---

## VITALS (LOCKED)

### Vital Caps & Scaling
- **Absolute Max Cap per Vital: 150**
- **Current Max Vitals are derived**, not fixed.

#### Derivation Rules (LOCKED)
- **Max HP = STR**, up to 150
- **Max Stamina = DEX**, up to 150
- **Max Mana = INT**, up to 150

> Any value beyond the attribute-derived amount must come from:
> - Item bonuses (e.g., "Increase Max HP")
> - Status effects

**Example:**
- STR = 100 → Base Max HP = 100
- Item bonuses = +20 HP
- Status bonus = +10 HP
- Final Max HP = 130 (≤ 150 cap)

### Vitals Regen Rules (LOCKED — Classic / UO-style)
- `float hpRegenPerSec = 0.05f` *(very slow; healing items/spells are primary)*
- `float staminaRegenPerSec = 1.0f`
- `float manaRegenPerSec = 0.5f`

> Regen is modified by:
> - Encumbrance
> - Status effects
> - Meditation (mana)
> - Sitting/resting states (if added later)

---

## COMBAT BASELINES (PROPOSED, NOT LOCKED)

Defines the baseline math before items/skills modify it.
- `float baseHitChance = 0.50f`
- `float baseDefenseChance = 0.50f`
- `float baseSwingSpeedSeconds = 2.0f`
- `Vector2Int unarmedDamageRange = (1, 4)`

---

## RESISTANCES & CAPS (LOCKED)

- Damage channels:
  - Physical, Fire, Cold, Poison, Energy

- Baseline resistances:
  - All start at **0** unless modified

- Resistance Cap:
  - **70 max per resistance** (unless overridden by world rules)

---

## SKILLS (LOCKED)

### Skill Cap Rules
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

### Skill Gain Rules (LOCKED)

Skill gain is enforced server-side via `SkillGainSystem`.

- Gains are tied to successful/meaningful use
- Gains can be throttled by time windows

**At Total Skill Cap (700):**
- Gains may only occur if room is made by lowering another skill
- Skill locks determine what can lower

**At Individual Skill Max (100):**
- That skill may not increase further

#### Skill Lock UI Model (LOCKED)
Each skill has one of three states:
- **Increase (+)** — skill is allowed to increase
- **Decrease (−)** — skill is allowed to decrease
- **Locked (🔒)** — skill will not change

> The server validates all gain/loss requests against these locks.
> UI icons are client-side; authority remains server-side.

---

## CURRENCY — COINS (LOCKED)

### Currency Name
- In-game currency is **Coins**.

### Currency Views (UI)
The player sees two values:
- **Held Coins**
- **Banked Coins**

### Rules (LOCKED)

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

### Authoritative Fields (Recommended)
- `int startingBankedCoins = 0`
- `int startingHeldCoins = 0`

> Runtime storage should live in an authoritative player wallet component, but the starting values live here.

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
- Player has a bank container
- Player has a **Banked Coins** balance
- **Insurance Auto-Renew draws from Banked Coins** (LOCKED)

---

## ITEM INSURANCE (LOCKED RULES)

### Fields Needed (on Item Instance)
- `bool isInsured`
- `bool autoRenewInsurance`
- `int insuranceCostPaid` *(optional bookkeeping)*

### Insurance Purchase
- Insurance can be toggled on any item (server validated)
- Cost is paid **up-front** (from **Banked Coins**)

### On Death
- For each item that would be lootable:
  - If insured: item remains with player (or returns on respawn) and is not placed in corpse
  - If auto-renew is enabled: server attempts to withdraw renewal cost from **Banked Coins** and re-apply insurance
  - If insufficient funds: insurance does not renew; item is treated uninsured for that death

> NOTE: Whether insurance "persists" without auto-renew is an implementation detail.
> Recommended: insurance is a state that can lapse; auto-renew keeps it maintained.

---

## NETWORKING OWNERSHIP RULES

### Server-Authoritative State
- Vitals (current/max)
- Attributes + derived stats
- Skill values
- Inventory + bank container
- **Held Coins / Banked Coins** balances
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
7. **PlayerWallet (Coins: Held/Banked) + replication + corpse drop rules**

