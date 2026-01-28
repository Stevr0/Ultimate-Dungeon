# ITEMS.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 1.1  
Last Updated: 2026-01-28  
Engine: Unity 6 (URP)  
Authority: Server-authoritative  
Data: ScriptableObjects-first  

---

## PURPOSE

Defines the **authoritative item system laws** for *Ultimate Dungeon*.

This document is the single source of truth for:
- Item identity model (ItemDef vs ItemInstance)
- What “Magical” means (affixes / modifiers)
- Durability and break-state laws (system-level)
- Containers, stacking, and ownership rules
- What other documents own (schemas, catalogs)

This document does **not**:
- List all base items (owned by `ITEM_CATALOG.md`)
- Define every affix (owned by `ITEM_AFFIX_CATALOG.md`)
- Define ItemDef fields (owned by `ITEM_DEF_SCHEMA.md`)

If an item rule is not defined here (or in the referenced authoritative docs), **it does not exist**.

---

## SCOPE BOUNDARIES (NO OVERLAP)

### This doc owns (SYSTEM LAWS)
- Item identity + persistence model
- What counts as equipment vs consumable vs resource vs container
- Durability law (when it changes; break behavior)
- Instance mutation rules (what can change at runtime)

### Other authoritative docs
- **ItemDef schema:** `ITEM_DEF_SCHEMA.md`
- **Base item list + base stats:** `ITEM_CATALOG.md`
- **Affix list + stacking + tiers:** `ITEM_AFFIX_CATALOG.md`
- **Affix count determination (loot/enhance):** `ITEM_AFFIX_CATALOG.md` (via `AffixCountResolver` rules)

---

## DESIGN LOCKS (MUST ENFORCE)

1. **Server authoritative**
   - Only the server creates ItemInstances, rolls affixes, modifies durability, and transfers ownership.

2. **Stable IDs**
   - `ItemDefId` values are stable and append-only in the catalog.

3. **Two-layer item model (LOCKED)**
   - `ItemDef` is immutable data (ScriptableObject)
   - `ItemInstance` is mutable state (runtime + save)

4. **Mundane vs Magical**
   - Mundane items have **no affixes**.
   - Magical items have **0..N affixes**.

5. **Affix cap per item**
   - Max affixes per item = **5** (locked).
   - How many affixes an item *actually* gets is determined by a single resolver (see below).

6. **Jewelry durability is enabled**
   - Jewelry breaks like other equipment.

7. **Material-based base resist profiles are locked**
   - Cloth/Leather/Metal baseline profiles are owned by `ITEM_CATALOG.md`.

8. **Archery is a separate item family**
   - Bows/crossbows use ammo items (arrows/bolts) consumed by Combat.

---

## ITEM IDENTITY MODEL (LOCKED)

### ItemDef (immutable)
Authored data that describes a type of item.
- Stored as a ScriptableObject
- Referenced by stable `ItemDefId`
- Defines base stats, allowed affix pools, and presentation defaults

### ItemInstance (mutable)
Runtime + saved state.
- References one `ItemDefId`
- Contains rolled affixes (if magical)
- Contains durability state
- Contains stack count (if stackable)
- Contains container contents (if container)
- Contains insurance flags (if applicable)

---

## ITEM FAMILIES (AUTHORITATIVE)

Item family categorization is data-driven (ItemDef fields), but these families must exist:

- Weapons (melee, ranged)
- Armor (cloth/leather/metal)
- Shields
- Jewelry
- Consumables
- Reagents
- Resources
- Containers

> The full base list of items is owned by `ITEM_CATALOG.md`.

---

## STACKING RULES (LOCKED)

### Stackable items
Examples: reagents, ammo, resources.

ItemDef must declare:
- `isStackable`
- `stackMax`

ItemInstance stores:
- `stackCount` (1..stackMax)

### Non-stackable items
Equipment, containers, and magical items are non-stackable.

---

## CONTAINER RULES (LOCKED)

Containers are ItemInstances that can hold other ItemInstances.

Rules:
- Containers have a capacity model (slots and/or weight), defined on ItemDef.
- Nested containers are allowed unless explicitly blocked by later rules.
- Ownership and access are server-validated.

---

## DURABILITY (LOCKED SYSTEM LAWS)

### What durability is
Durability is per ItemInstance, expressed as:
- `durabilityCurrent`
- `durabilityMax`

### When durability changes
- Combat triggers durability loss for weapons/armor/shields/jewelry per `COMBAT_CORE.md`.
- Non-combat loss (e.g., crafting mishaps) must be defined by that system.

### Break state behavior (LOCKED)
When `durabilityCurrent <= 0`:
- Item becomes **Broken**
- Broken items contribute **no modifiers** and may be unusable
- Item remains lootable/tradable unless restricted by economy rules

> Repair rules (materials, skills, success/break) are owned by the crafting system docs (future).

---

## MAGICAL ITEMS & AFFIXES (LOCKED)

### Magical definition
An item is Magical if it has an affix list (possibly empty if explicitly allowed).

### Affix rules
- Affixes are rolled only by the server.
- Affixes come from allowed pools on ItemDef.
- Affix IDs, stacking, tiers, and ranges are owned by `ITEM_AFFIX_CATALOG.md`.

### Affix count (NO DUPLICATION)
Affix count must be determined by a single conceptual resolver:

- **AffixCountResolver** (authoritative rule location: `ITEM_AFFIX_CATALOG.md`)

Inputs (examples):
- Source type: LootDrop vs Enhancement
- Player skill used for enhancement (e.g., Blacksmithing/Tailoring)
- Item rarity / dungeon tier

Outputs:
- `affixCount` in range `0..5`

**Rule:** No other document re-defines affix-count math.

---

## OWNERSHIP, TRADE, AND ECONOMY (LOCKED INTERFACE)

This item system must interoperate with:
- Player Wallet rules (Held/Banked coins) in `PLAYER_DEFINITION.md`
- Insurance rules in `PLAYER_DEFINITION.md`
- Death/corpse transfer rules (death system; must obey PlayerDefinition)

This doc intentionally does not re-specify those systems.

---

## AUTHORING WORKFLOW (RECOMMENDED)

1. Add base items to `ITEM_CATALOG.md` (IDs + base stats)
2. Create matching `ItemDef` assets
3. Maintain `ITEM_AFFIX_CATALOG.md` for all affix definitions
4. Ensure validators enforce:
   - No unknown `ItemDefId`
   - No unknown `AffixId`
   - No invalid pool references
   - No illegal stacking

---

## DESIGN LOCK CONFIRMATION

This document is **authoritative**.

Any change must:
- Increment Version
- Update Last Updated
- Call out save-data implications

