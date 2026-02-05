# SPELL_CATEGORY_MODEL.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 1.0  
Last Updated: 2026-02-05  
Engine: Unity 6 (URP)  
Networking: Netcode for GameObjects (NGO)  
Authority: Server-authoritative

---

## PURPOSE

Defines the authoritative **Spell Category Model** for *Ultimate Dungeon*.

This document answers:

- How SpellIds are grouped for **authoring, balance, and item identity**
- Which equipment slots are allowed to grant which spell categories
- The minimum viable **category spell sets** to reach the 30-spell baseline

This model exists because *Ultimate Dungeon* is **Item-First**:
- Items grant player verbs (abilities)
- SpellIds are payload identifiers
- Categories replace "spell circles" as the primary design structure

If a rule is missing here, **it does not exist**.

---

## DEPENDENCIES (MUST ALIGN)

- `SYSTEM_INTERACTION_MODEL.md` (Item-first; hotbar projection; out-of-combat config freeze)
- `ITEM_DEF_SCHEMA.md` (GrantedAbilities; AbilityGrantSlot; EquipSlot model)
- `SPELL_ID_CATALOG.md` (stable SpellId list; append-only)
- `SPELL_DEF_SCHEMA.md` (payload/timing/cost schema)
- `SCENE_RULE_PROVIDER.md` (travel, combat, and targeting gates)

---

## CORE PRINCIPLES (LOCKED)

1. **Categories are Item-Centric**
   - Spell groups are defined by *where they come from* (equipment slot / item type), not by circle or school.

2. **Categories are Design Taxonomy, not Progression**
   - Categories do not imply learning, spellbooks, or unlock trees.

3. **Access Control is Item-Granted Only**
   - A SpellId becomes usable only if an equipped item grants it.

4. **Categories prevent identity drift**
   - A spell must have a clear "home". Cross-category grants are explicit exceptions.

5. **No circles / no reagents for spellcasting**
   - Circles are not used.
   - Reagents are Alchemy-only.

---

## AUTHORITATIVE ENUMS (DESIGN)

### SpellCategory (authoritative taxonomy)

- `Bag`
- `Mainhand`
- `Offhand`
- `Head`
- `Neck`
- `Chest`
- `Foot`
- `Mount`
- `UtilityItem_Food`
- `UtilityItem_Potion`
- `UtilityItem_Bandage`
- `UtilityItem_Tool`
- `Rune`
- `Scroll_SingleUse`
- `WeaponProc`

> NOTE: These are design categories used for authoring + validation.
> The runtime may optionally encode them for tooling/UI, but gameplay access remains item-driven.

---

## EQUIPMENT SLOT → ALLOWED CATEGORIES (LOCKED)

The following mapping defines which categories each equipped slot may grant.

### Bag
Allowed categories:
- `Bag`

### Head
Allowed categories:
- `Head`

### Neck
Allowed categories:
- `Neck`

### Mainhand
Allowed categories:
- `Mainhand`
- `WeaponProc` *(weapon-only procs, authored separately as proc payloads)*

### Chest
Allowed categories:
- `Chest`

### Offhand
Allowed categories:
- `Offhand`

### BeltA / BeltB
Allowed categories:
- `UtilityItem_Food`
- `UtilityItem_Potion`
- `UtilityItem_Bandage`
- `UtilityItem_Tool`
- `Rune` *(Rune Stones only; see Travel spells)*
- `Scroll_SingleUse` *(Scrolls only; see Summons)*

### Foot
Allowed categories:
- `Foot`

### Mount
Allowed categories:
- `Mount`

**Design lock:**
- Items may not grant spells outside their slot’s allowed categories.
- Cross-category grants require an explicit exception rule in this document (future version).

---

## BASELINE CONTENT GOAL (MVP)

Minimum viable target is **category coverage**, not a global spell-count.

**Rule (LOCKED):**
- Every **SpellCategory** intended for player-facing items must have **at least 3 SpellIds** available for authoring.
- These 3 SpellIds are the *minimum pool* per category so each equipment slot can offer meaningful **Primary / Secondary / Utility** choices across its items.

This means the total number of SpellIds in the game may be **well above 30**.

---

## MVP CATEGORY SPELL SETS (AUTHORITATIVE LIST)

> Each category below lists a **minimum pool of 3 SpellIds**.
> Categories may (and usually should) grow over time.
> This list is about *coverage* so every equipment slot has enough ability choices.

### Bag (min 3)
- `CreateFood`
- `Telekinesis`
- `Unlock`

### Head (min 3)
- `NightSight`
- `Protection`
- `ReactiveArmor`

### Neck (min 3)
- `Incognito`
- `Invisibility`
- `Reveal`

### Mainhand (min 3)
- `MagicArrow`
- `Fireball`
- `Lightning`

### Chest (min 3)
- `MagicReflection`
- `ReactiveArmor`
- `Bless`

### Offhand (min 3)
- `Protection`
- `MagicReflection`
- `ArchProtection`

### Foot (min 3)
- `Teleport`
- `Agility`
- `Bless`

### Mount (min 3)
- `Bless`
- `Protection`
- `CreateFood`

### UtilityItem_Food (min 3)
- `Heal`
- `Strength`
- `Bless`

### UtilityItem_Potion (min 3)
- `Heal`
- `GreaterHeal`
- `Cure`

### UtilityItem_Bandage (min 3)
- `Heal`
- `GreaterHeal`
- `Cure`

### UtilityItem_Tool (min 3)
- `NightSight`
- `Reveal`
- `Telekinesis`

### Rune (min 3) — Rune Stone only (LOCKED)
- `Recall`
- `Mark`
- `GateTravel`

### Scroll_SingleUse (min 3) — Large summons only (LOCKED)
- `BladeSpirits`
- `EnergyVortex`
- `SummonDaemon`

### WeaponProc (min 3) — weapon-only (LOCKED)
- `Lightning`
- `Harm`
- `Poison`

> Notes:
> - Some SpellIds appear in multiple categories as *design reuse*. Access is still item-granted only.
> - Display names may differ by item context (same SpellId, different presentation).

## SPELLID REALITY CHECK (LOCKED POLICY)

Some entries above reference concepts that may not exist yet as SpellIds.

**Policy:**
- If a required concept is missing from `SPELL_ID_CATALOG.md`, it must be added by **appending** a new SpellId.
- Existing SpellIds may keep UO-ish names while display names shift to match item identity.

Examples:
- SpellId `Poison` can display as "Poison Strike" when granted by Mainhand.
- SpellId `CreateFood` can spawn a belt-usable food item.

---

## SUMMONS & FIELDS (LOCKED RESTRICTIONS)

### Large Summons
- Category: `Scroll_SingleUse`
- Source: Scroll items only (UtilityItem)
- Single-use; consumed on success
- Rare loot only

### Persistent Fields
- Category: `WeaponProc` or explicit item grants
- Not globally available
- Must be authored per weapon proc or explicit item

---

## UI / TOOLING NOTES (NON-AUTHORITATIVE)

- UI may group spells by SpellCategory for clarity.
- Hotbar labels remain per EquipSlot.
- Display names may differ by item context (same SpellId, different presentation).

---

## DESIGN LOCK CONFIRMATION

This document is **authoritative**.

Any change must:
- Increment Version
- Update Last Updated
- Call out implications for:
  - `SPELL_ID_CATALOG.md` (append-only changes)
  - Item authoring rules
  - Hotbar projections and selection rules

