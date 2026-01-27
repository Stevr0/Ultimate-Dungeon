# PLAYER_MODEL.md — ULTIMATE DUNGEON (AUTHORITATIVE)

Version: 0.1  
Last Updated: 2026-01-27  

---

## PURPOSE

This document defines the **Player Actor** data model and runtime contract for *Ultimate Dungeon*.

The player is a networked Actor in a **server-authoritative** world. Progression is **classless, skill-based**. All gameplay systems (combat, items, statuses, skills, UI) must bind to this model.

---

## DESIGN LOCKS (ABSOLUTE)

1. **Server-authoritative state**
   - The server is the source of truth for: vitals, stats, inventory, equipment state, skill progression, status effects, damage, death.

2. **No classes / no levels / no XP**
   - Progression only occurs through **skills increasing via use**.

3. **Status effects are decoupled from items**
   - Items may *apply* or *grant* effects, but **Status Effects are defined and resolved independently**.

4. **Deterministic math**
   - Combat and effect resolution is numeric and reproducible.

---

## PLAYER = ACTOR

A Player is an **Actor** with:
- A network identity (owner client)
- A runtime state container (vitals, inventory, status, etc.)
- Input driving intents (move / interact / attack), validated server-side

### Components (Unity)
- `PlayerNetIdentity` (NGO ownership, server authority bindings)
- `PlayerCore` (authoritative runtime references)
- `PlayerVitals` (HP/Stam/Mana + regen rules)
- `PlayerStats` (attributes + derived stats)
- `PlayerSkillBook` (skill values + gain rules)
- `InventoryComponent` (bag, weight, stacks, containers)
- `EquipmentComponent` (hands/armor/jewelry slots, if used)
- `StatusEffectSystem` (applied statuses, ticking, stacking)
- `TargetingComponent` (current target; server-validated actions)

> NOTE: Exact script names can match your existing conventions; this doc defines the *contract*.

---

## DATA OWNERSHIP & REPLICATION (NGO)

### Authoritative on Server
- Current vitals (HP/Stam/Mana)
- Stat block (attributes + derived stats)
- Skill values + gain ticks
- Inventory contents + container topology
- Equipped items
- Active status effects
- Position / rotation (via NetworkTransform or custom)

### Predicted on Client (Optional)
- Local movement smoothing
- UI selection/hover state
- Cosmetic-only feedback

### Replicated to Clients
- Public-facing vitals (HP % for others, optional)
- Combat events (hit/miss/damage numbers)
- Status visuals (icons/particles; actual logic stays server)

---

## BASELINE PLAYER STATS (PROPOSED)

### Primary Attributes
These are the only "root" attributes. Everything else derives from them.
- **STR** (Strength)
- **DEX** (Dexterity)
- **INT** (Intelligence)

### Vitals
- **HP** (Hit Points)
- **Stamina**
- **Mana**

### Core Derived Combat Stats (minimal set)
- **Hit Chance**
- **Defense Chance**
- **Swing Speed**
- **Base Damage**
- **Damage Increase**

### Resistances (damage mitigation channels)
- **Physical**
- **Fire**
- **Cold**
- **Poison**
- **Energy**

### Caps (Proposed, not locked)
- Resistance max cap defaults to **70** (UO-like), unless overridden by world rules.

---

## SKILLS (FOUNDATION SET)

This is the *minimum* set to start building combat + gathering + crafting.

### Combat
- Swords
- Macing
- Fencing
- Archery
- Wrestling
- Tactics
- Anatomy
- Parrying

### Magic (choose one model; see Open Questions)
- Magery
- Meditation
- Evaluating Intelligence
- Resist Spells

### Utility / World
- Healing
- Hiding
- Stealth
- Lockpicking

### Crafting (stub OK initially)
- Blacksmithing
- Tailoring
- Carpentry
- Alchemy

> You can start with Combat + Utility only, and add Crafting later; the data model supports both.

---

## INVENTORY & EQUIPMENT MODEL

### Inventory
- Inventory is a **container graph** (bag contains items; items can be containers).
- Each item has:
  - `ItemId` (unique instance)
  - `DefinitionId` (points to an ItemDef/ScriptableObject)
  - `StackCount` (if stackable)
  - `Weight` (per unit)
  - `PropertyRolls` (random affixes/properties)

### Encumbrance
- Total carried weight affects:
  - Movement speed (optional)
  - Stamina regen (optional)
  - Action cost (optional)

### Equipment (Proposed slots)
- **Hands**: MainHand, OffHand (supports 2-hand items)
- **Armor**: Head, Chest, Legs, Hands, Feet
- **Jewelry**: Ring1, Ring2, Necklace

> If you want more UO authenticity (layers), we can expand this later without breaking save data.

---

## STATUS EFFECT INTEGRATION

Players must be valid targets for any status effect in the catalog.

### Runtime Requirements
- Player must expose a `StatusReceiver` interface:
  - Apply / refresh / stack / remove
  - Query immunities/resistances
  - Provide hooks for:
    - movement restrictions
    - action blocking
    - damage over time
    - regen modifiers

---

## DEATH & RESPAWN (PROPOSED)

- At **HP <= 0**: Player enters **Dead** state.
- On death:
  - Drop loot? (Open Question)
  - Durability damage? (Open Question)
  - Corpse container created server-side? (Open Question)

---

## LOCKED DECISIONS (CONFIRMED)

The following decisions are **locked** and must not be violated by implementation.

### Starting Stats
- Fixed baseline: **STR 10 / DEX 10 / INT 10**

### Skill Caps
- **Total Skill Cap (UO-style): 700**
- Individual skills increase by use
- Global cap enforces tradeoffs and respecialization pressure

### Magic Model
- **Ultima Online–style spellbook + reagents**
- Spell knowledge gated by scroll acquisition
- Casting consumes reagents and mana

### Death Penalty
- **Full Loot on Death**
- Corpse container created server-authoritative
- Items may be protected via **Item Insurance** (LOCKED RULES BELOW)

#### Item Insurance (LOCKED)
- **Optional per-item** insurance toggle.
- **Paid up-front** when insurance is applied.
- Optional **Auto-Renew** per item:
  - On death, if Auto-Renew is enabled and the player has sufficient funds, the server **withdraws gold from the player’s bank** and **re-applies insurance** automatically.
  - If insufficient funds, Auto-Renew fails and the item is treated as uninsured for that death.

### Equipment Layers (LOCKED)

#### Armor / Wearables
- Head
- Torso
- Arms
- Hands
- Legs
- **Neck (Armor):** Gorget / Scarf / neck-armor layer

#### Jewelry
- **Amulet** (separate from Neck armor)
- **Rings:** Ring1, Ring2
- **Earrings**

#### Weapons
- Main Hand
- Off Hand
- Two-Handed (occupies both)

---

## UPDATED IMPLEMENTATION CHECKLIST (NEXT)

The following systems are now unblocked and should be implemented **in order**:

1. **PlayerDefinition** *(ScriptableObject)*
   - Starting stats (10/10/10)
   - Total skill cap
   - Starting skills (likely 0 or minimal)

2. **PlayerCore** *(MonoBehaviour)*
   - Central authoritative reference hub
   - Holds references to all sub-systems

3. **PlayerVitals** *(Server-authoritative)*
   - HP / Stamina / Mana
   - Regen rules + hooks for status effects

4. **PlayerStats**
   - Attributes → derived combat stats
   - Resistance caps + modifiers

5. **PlayerSkillBook**
   - Skill values
   - Skill gain ticks
   - Enforcement of total skill cap

6. **Corpse & Loot Container System**
   - Server-spawned corpse
   - Inventory transfer
   - Insurance exemptions

7. **Read-only UI Binders**
   - Vitals panel
   - Skill list panel
   - Equipment paperdoll (no interaction yet)

---

## ITEM INSURANCE SYSTEM (LOCKED)

Insurance is **optional for all items** and fully server-authoritative.

### Insurance Rules
1. **Upfront Purchase**
   - Insurance is purchased in advance per item.
   - Cost is paid immediately and stored on the item instance.

2. **Auto-Renew (Optional Flag)**
   - Items may enable `AutoRenewInsurance`.

3. **On Death Resolution**
   - If the item is insured:
     - The item is **not transferred to the corpse**.
     - Insurance is considered consumed.
   - If **Auto-Renew** is enabled:
     - Server attempts to withdraw insurance cost from the player **bank**.
     - If funds are available → insurance is reapplied.
     - If funds are insufficient → item becomes uninsured.

4. **Failure Case**
   - Uninsured items are fully lootable.

### Data Requirements
- Item Instance Fields:
  - `IsInsured`
  - `InsuranceCost`
  - `AutoRenewInsurance`

---

## DESIGN NOTE (IMPORTANT)

With these locks in place:
- Combat math
- Status effects
- Item affixes
- Skill gain
- Insurance rules

can now be implemented **without refactors**.

This Player model is now stable enough to be treated as **authoritative source-of-truth**.

