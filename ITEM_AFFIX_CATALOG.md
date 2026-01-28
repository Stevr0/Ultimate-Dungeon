# ITEM\_AFFIX\_CATALOG.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 1.2\
Last Updated: 2026-01-28\
Engine: Unity 6 (URP)\
Authority: Server-authoritative\
Scope: Mundane → Magical enhancement and magical loot generation

---

## PURPOSE

This document defines the **authoritative catalog of Item Affixes** in *Ultimate Dungeon*.

Affixes are the **only** standard way items gain bonus power (beyond their base weapon/armor numbers).
Affixes are:

- Rolled onto items via **loot generation** and/or **enhancement**
- Stored on an **ItemInstance** (never on ItemDef)
- Applied as **modifiers** (numbers only)

If an affix is not defined here, **it does not exist**.

---

## DESIGN LOCKS (ABSOLUTE)

1. **Server-authoritative**

   - All affix rolls, magnitudes, success/failure, and application occur on the server.

2. **Deterministic rolling**

   - Any roll must use a deterministic server seed (no global RNG).

3. **Mundane items have no affixes**

   - `ItemPowerState = Mundane` → `RolledAffixes[]` must be empty.

4. **Affixes are modifiers, not behaviors**

   - Affixes never execute combat logic.
   - Combat/magic systems read final aggregated stats.

5. **Save integrity**

   - Affix identifiers must be stable.
   - Never rename or reorder shipped IDs without explicit migration.

---

## TERMINOLOGY

- **Affix**: A named modifier entry (e.g., “Damage Increase +12%”).
- **Affix Roll**: The process of selecting affix ID(s) and magnitude(s).
- **Tier**: A discrete quality band controlling magnitude and rarity.
- **Pool**: A curated set of allowed affixes for an item category (e.g., WeaponPool, JewelryPool).

---

## AFFX IDENTIFIERS (LOCKED MODEL)

Each affix has a stable `AffixId`.
Recommended implementation: `enum AffixId` (append-only), or stable string IDs.

Naming convention:

- `Stat_*` for attributes
- `Combat_*` for hit/def/damage/speed
- `Resist_*` for resist bonuses
- `Magic_*` for casting-related bonuses
- `Proc_*` reserved (NOT USED in v1; on-hit behavior is a status/combat feature)

---

## AFFIX DATA CONTRACT (AUTHORITATIVE)

Every affix in this catalog defines:

- **AffixId**
- **DisplayName** (UI)
- **Group** (conflict group)
- **AllowedItemKinds**
  - Weapon / Armor / Jewelry / Shield / Tool (optional)
- **Magnitude Type**
  - Flat (e.g., +5 STR)
  - Percent (e.g., +12% Damage Increase)
- **Tier Table**
  - `Tier1..TierN` magnitude ranges
- **Stacking Rule**
  - `Unique` (at most one instance)
  - `NoStack` (can exist on multiple items but sums normally)
  - `HighestOnly` (only highest across all equipped items applies)
- **Conflicts**
  - Cannot appear together with other affixes in the same Group on a single item

---

## GLOBAL CAPS & INTERACTIONS (LOCKED REFERENCES)

- Resistance caps are defined by Player rules (default cap **70** per resistance).
- Casting modifiers are constrained by Magic rules:
  - Cast time cannot be reduced below **50% of base**.

This catalog defines *what can roll*; combat/magic defines *how it applies*.

---

## AFFIX TIERS (PROPOSED, NOT LOCKED)

Tiers are an authoring convenience for balancing.
You may change magnitudes later by versioning this document.

Recommended tiers:

- **T1** Common
- **T2** Uncommon
- **T3** Rare
- **T4** Epic
- **T5** Legendary

---

## CONFLICT GROUPS (LOCKED)

Conflict groups prevent nonsensical stacking on a single item.

Examples:

- `Group: Attribute_STR` → only one STR affix per item.
- `Group: Weapon_SwingSpeed` → only one swing speed affix per item.
- `Group: Magic_FasterCasting` → only one FC affix per item.

**Rule:**

> A single item may not contain two affixes from the same conflict group.

---

## AFFIX CATALOG — ATTRIBUTES

### Stat\_STR (Strength Bonus)

- **AffixId:** `Stat_STR`
- **Group:** `Attribute_STR`
- **Allowed:** Jewelry, Armor
- **Magnitude:** Flat
- **Tiers:**
  - T1: +1–2
  - T2: +3–4
  - T3: +5–6
  - T4: +7–8
  - T5: +9–10
- **Stacking:** NoStack

### Stat\_DEX (Dexterity Bonus)

- **AffixId:** `Stat_DEX`
- **Group:** `Attribute_DEX`
- **Allowed:** Jewelry, Armor
- **Magnitude:** Flat
- **Tiers:** T1 +1–2, T2 +3–4, T3 +5–6, T4 +7–8, T5 +9–10
- **Stacking:** NoStack

### Stat\_INT (Intelligence Bonus)

- **AffixId:** `Stat_INT`
- **Group:** `Attribute_INT`
- **Allowed:** Jewelry, Armor
- **Magnitude:** Flat
- **Tiers:** T1 +1–2, T2 +3–4, T3 +5–6, T4 +7–8, T5 +9–10
- **Stacking:** NoStack

---

## AFFIX CATALOG — COMBAT (OFFENSE)

### Combat\_DamageIncrease (Damage Increase %)

- **AffixId:** `Combat_DamageIncrease`
- **Group:** `Combat_DI`
- **Allowed:** Weapon, Jewelry
- **Magnitude:** Percent
- **Tiers:**
  - T1: +5–10%
  - T2: +11–20%
  - T3: +21–30%
  - T4: +31–40%
  - T5: +41–50%
- **Stacking:** NoStack

### Combat\_HitChance (Hit Chance %)

- **AffixId:** `Combat_HitChance`
- **Group:** `Combat_HCI`
- **Allowed:** Weapon, Jewelry
- **Magnitude:** Percent
- **Tiers:** T1 +2–4%, T2 +5–8%, T3 +9–12%, T4 +13–16%, T5 +17–20%
- **Stacking:** NoStack

### Combat\_SwingSpeed (Swing Speed %)

- **AffixId:** `Combat_SwingSpeed`
- **Group:** `Weapon_SwingSpeed`
- **Allowed:** Weapon
- **Magnitude:** Percent (reduces swing time via combat formula)
- **Tiers:** T1 +5–10%, T2 +11–15%, T3 +16–20%, T4 +21–25%, T5 +26–30%
- **Stacking:** HighestOnly *(prevents degenerate multi-item stacking)*

---

## AFFIX CATALOG — COMBAT (DEFENSE)

### Combat\_DefenseChance (Defense Chance %)

- **AffixId:** `Combat_DefenseChance`
- **Group:** `Combat_DCI`
- **Allowed:** Jewelry, Shield
- **Magnitude:** Percent
- **Tiers:** T1 +2–4%, T2 +5–8%, T3 +9–12%, T4 +13–16%, T5 +17–20%
- **Stacking:** NoStack

### Combat\_ParryBonus (Parry Skill Bonus)

- **AffixId:** `Combat_ParrySkill`
- **Group:** `Skill_Parrying`
- **Allowed:** Shield
- **Magnitude:** Flat (Skill points)
- **Tiers:** T1 +1–3, T2 +4–6, T3 +7–9, T4 +10–12, T5 +13–15
- **Stacking:** HighestOnly

---

## AFFIX CATALOG — RESISTANCES

Resist affixes are **bonus resist** added to the player’s resist totals.
Caps are enforced by Player rules.

### Resist\_Physical

- **AffixId:** `Resist_Physical`
- **Group:** `Resist_Physical`
- **Allowed:** Armor, Jewelry, Shield
- **Magnitude:** Flat
- **Tiers:** T1 +1–3, T2 +4–6, T3 +7–9, T4 +10–12, T5 +13–15
- **Stacking:** NoStack

### Resist\_Fire

- **AffixId:** `Resist_Fire`
- **Group:** `Resist_Fire`
- **Allowed:** Armor, Jewelry, Shield
- **Magnitude:** Flat
- **Tiers:** T1 +1–3, T2 +4–6, T3 +7–9, T4 +10–12, T5 +13–15
- **Stacking:** NoStack

### Resist\_Cold

- **AffixId:** `Resist_Cold`
- **Group:** `Resist_Cold`
- **Allowed:** Armor, Jewelry, Shield
- **Magnitude:** Flat
- **Tiers:** T1 +1–3, T2 +4–6, T3 +7–9, T4 +10–12, T5 +13–15
- **Stacking:** NoStack

### Resist\_Poison

- **AffixId:** `Resist_Poison`
- **Group:** `Resist_Poison`
- **Allowed:** Armor, Jewelry, Shield
- **Magnitude:** Flat
- **Tiers:** T1 +1–3, T2 +4–6, T3 +7–9, T4 +10–12, T5 +13–15
- **Stacking:** NoStack

### Resist\_Energy

- **AffixId:** `Resist_Energy`
- **Group:** `Resist_Energy`
- **Allowed:** Armor, Jewelry, Shield
- **Magnitude:** Flat
- **Tiers:** T1 +1–3, T2 +4–6, T3 +7–9, T4 +10–12, T5 +13–15
- **Stacking:** NoStack

---

## AFFIX CATALOG — MAGIC

### Magic\_FasterCasting (FC)

- **AffixId:** `Magic_FasterCasting`
- **Group:** `Magic_FC`
- **Allowed:** Jewelry
- **Magnitude:** Flat (points)
- **Tiers:**
  - T1: +1
  - T2: +2
  - T3: +3
  - T4: +4
  - T5: +5
- **Stacking:** HighestOnly *(cap/limits handled by Magic rules)*

### Magic\_LowerManaCost (LMC)

- **AffixId:** `Magic_LowerManaCost`
- **Group:** `Magic_LMC`
- **Allowed:** Jewelry, Armor
- **Magnitude:** Percent
- **Tiers:** T1 +2–4%, T2 +5–8%, T3 +9–12%, T4 +13–16%, T5 +17–20%
- **Stacking:** NoStack

### Magic\_SpellDamageIncrease (SDI)

- **AffixId:** `Magic_SpellDamageIncrease`
- **Group:** `Magic_SDI`
- **Allowed:** Jewelry
- **Magnitude:** Percent
- **Tiers:** T1 +5–10%, T2 +11–20%, T3 +21–30%, T4 +31–40%, T5 +41–50%
- **Stacking:** NoStack

---

## AFFIX CATALOG — HIT SPELLS (WEAPONS ONLY)

Hit Spells are **on-hit proc affixes** that have a chance to trigger when a weapon successfully lands a hit.

**Design Rule (LOCKED):**

- Hit Spells trigger **only on successful weapon hits**
- They do **not** trigger on spell damage
- Proc chance is rolled server-side, deterministically
- Proc damage/effects resolve through the normal spell/combat pipeline

### Allowed Hit Spells (LOCKED LIST)

Only a curated subset of spells may exist as Hit Spell procs.
This avoids degenerate or abusive interactions.

Allowed baseline Hit Spells:

- Hit\_Lightning
- Hit\_Fireball
- Hit\_Harm
- Hit\_MagicArrow
- Hit\_EnergyBolt

> Spells with crowd control, summons, teleportation, or large AoE are **never valid** as Hit Spells.

### Hit\_Lightning

- **AffixId:** `Hit_Lightning`
- **Group:** `HitSpell_Lightning`
- **Allowed:** Weapon
- **Magnitude:** Percent (proc chance)
- **Tiers:** T1 5%, T2 10%, T3 15%, T4 20%, T5 25%
- **Stacking:** NoStack

### Hit\_Fireball

- **AffixId:** `Hit_Fireball`
- **Group:** `HitSpell_Fireball`
- **Allowed:** Weapon
- **Magnitude:** Percent (proc chance)
- **Tiers:** T1 5%, T2 10%, T3 15%, T4 20%, T5 25%
- **Stacking:** NoStack

### Hit\_MagicArrow

- **AffixId:** `Hit_MagicArrow`
- **Group:** `HitSpell_MagicArrow`
- **Allowed:** Weapon
- **Magnitude:** Percent (proc chance)
- **Tiers:** T1 5%, T2 10%, T3 15%, T4 20%, T5 25%
- **Stacking:** NoStack

### Hit\_Harm

- **AffixId:** `Hit_Harm`
- **Group:** `HitSpell_Harm`
- **Allowed:** Weapon
- **Magnitude:** Percent (proc chance)
- **Tiers:** T1 5%, T2 10%, T3 15%, T4 20%, T5 25%
- **Stacking:** NoStack

### Hit\_EnergyBolt

- **AffixId:** `Hit_EnergyBolt`
- **Group:** `HitSpell_EnergyBolt`
- **Allowed:** Weapon
- **Magnitude:** Percent (proc chance)
- **Tiers:** T1 5%, T2 10%, T3 15%, T4 20%, T5 25%
- **Stacking:** NoStack

---

## AFFIX CATALOG — HIT LEACHES (WEAPONS ONLY)

Hit Leaches restore resources to the attacker based on **damage successfully dealt**.

**Design Rules (LOCKED):**

- Leach effects trigger only on successful weapon hits
- Leach amount is calculated from **final damage dealt** (after mitigation)
- Leaches do not function on spell damage
- Resource restoration is clamped by max values

### Hit\_LifeLeach

- **AffixId:** `Hit_LifeLeach`
- **Group:** `HitLeach_Life`
- **Allowed:** Weapon
- **Magnitude:** Percent of damage dealt returned as HP
- **Tiers:** T1 5%, T2 10%, T3 15%, T4 20%, T5 25%
- **Stacking:** NoStack

### Hit\_ManaLeach

- **AffixId:** `Hit_ManaLeach`
- **Group:** `HitLeach_Mana`
- **Allowed:** Weapon
- **Magnitude:** Percent of damage dealt returned as Mana
- **Tiers:** T1 5%, T2 10%, T3 15%, T4 20%, T5 25%
- **Stacking:** NoStack

### Hit\_StaminaLeach

- **AffixId:** `Hit_StaminaLeach`
- **Group:** `HitLeach_Stamina`
- **Allowed:** Weapon
- **Magnitude:** Percent of damage dealt returned as Stamina
- **Tiers:** T1 5%, T2 10%, T3 15%, T4 20%, T5 25%
- **Stacking:** NoStack

---

## AFFIX CATALOG — VITAL BONUSES

These are **bonus max vitals** applied through PlayerVitals bonus setters.
Vital caps (150) are enforced by Player rules.

### Vital\_MaxHP

- **AffixId:** `Vital_MaxHP`
- **Group:** `Vital_MaxHP`
- **Allowed:** Armor, Jewelry
- **Magnitude:** Flat
- **Tiers:** T1 +1–3, T2 +4–6, T3 +7–9, T4 +10–12, T5 +13–15
- **Stacking:** NoStack

### Vital\_MaxStamina

- **AffixId:** `Vital_MaxStamina`
- **Group:** `Vital_MaxStamina`
- **Allowed:** Armor, Jewelry
- **Magnitude:** Flat
- **Tiers:** T1 +1–3, T2 +4–6, T3 +7–9, T4 +10–12, T5 +13–15
- **Stacking:** NoStack

### Vital\_MaxMana

- **AffixId:** `Vital_MaxMana`
- **Group:** `Vital_MaxMana`
- **Allowed:** Armor, Jewelry
- **Magnitude:** Flat
- **Tiers:** T1 +1–3, T2 +4–6, T3 +7–9, T4 +10–12, T5 +13–15
- **Stacking:** NoStack

---

## AFFIX CATALOG — SKILL BONUSES (FOUNDATION SET)

Skill bonuses are **flat skill points** and should be treated as item modifiers.
Skill caps and total cap rules still apply to the player’s *base* skills; bonuses are additive at runtime.

**Rule (LOCKED):**

> Skill bonuses do not change the player’s underlying trained skill values.

### Skill\_Magery

- **AffixId:** `Skill_Magery`
- **Group:** `Skill_Magery`
- **Allowed:** Jewelry
- **Magnitude:** Flat
- **Tiers:** T1 +1–3, T2 +4–6, T3 +7–9, T4 +10–12, T5 +13–15
- **Stacking:** HighestOnly

### Skill\_ResistSpells

- **AffixId:** `Skill_ResistSpells`
- **Group:** `Skill_ResistSpells`
- **Allowed:** Jewelry
- **Magnitude:** Flat
- **Tiers:** T1 +1–3, T2 +4–6, T3 +7–9, T4 +10–12, T5 +13–15
- **Stacking:** HighestOnly

### Skill\_Tactics

- **AffixId:** `Skill_Tactics`
- **Group:** `Skill_Tactics`
- **Allowed:** Jewelry
- **Magnitude:** Flat
- **Tiers:** T1 +1–3, T2 +4–6, T3 +7–9, T4 +10–12, T5 +13–15
- **Stacking:** HighestOnly

---

## POOLS (AUTHORING HOOK)

ItemDefs should reference one or more **Affix Pools**.
A Pool is a curated list of AffixIds valid for that item type.

Recommended pools (v1):

- `Pool_Weapon_Common`
- `Pool_Weapon_Rare`
- `Pool_Armor_Common`
- `Pool_Armor_Rare`
- `Pool_Jewelry_Common`
- `Pool_Jewelry_Rare`
- `Pool_Shield_Common`

Pools are defined in data (ScriptableObjects) later.
This document only defines the **AffixIds** and their rules.

---

## AFFIX COUNT DETERMINATION (LOCKED)

Affix count is always an integer roll in the inclusive range:

- `AffixCount = RandomInt(0, Nmax)`

Where `Nmax` is determined by context (loot or enhancement), and the global hard cap is enforced.

### Global Cap (LOCKED)

- **Maximum affixes per item is 5**.
- `Nmax` is always clamped to `0..5`.

### Loot-Spawned Magic Items (Dungeon Drops)

Loot generation determines `Nmax` from the drop context (dungeon depth, chest tier, boss tier, etc.) and then rolls:

- `AffixCount = RandomInt(0, Nmax)`

**Rule (LOCKED):**

- If `AffixCount = 0`, the item is spawned as **Mundane** (no `RolledAffixes[]`).
- If `AffixCount > 0`, the item is spawned as **Magical** and receives that many rolled affixes.

> The loot system owns the tables that map dungeon depth/tier → `Nmax` and tier ceilings.

### Enhancement (Mundane → Magical)

For enhancement, the player’s **crafting skill** determines `Nmax`.

- Eligible skills depend on item type:
  - **Blacksmithing**: weapons, metal armor, shields
  - **Tailoring**: cloth/leather armor
  - *(future)* Carpentry, Tinkering, etc.

#### Skill → Nmax mapping (LOCKED)

Let `S` be the player’s relevant crafting skill value (0–100).

- `Nmax = clamp( floor((S - 20) / 20) + 1, 0, 5 )`

This matches the intended mapping below:
- Skill 0–19  → `Nmax = 0`
- Skill 20–39 → `Nmax = 1`
- Skill 40–59 → `Nmax = 2`
- Skill 60–79 → `Nmax = 3`
- Skill 80–99 → `Nmax = 4`
- Skill 100  → `Nmax = 5`

Meaning:

- Skill 0–19  → `Nmax = 0`
- Skill 20–39 → `Nmax = 1`
- Skill 40–59 → `Nmax = 2`
- Skill 60–79 → `Nmax = 3`
- Skill 80–99 → `Nmax = 4`
- Skill 100 → `Nmax = 5`

#### Enhancement roll (LOCKED)

On a non-break outcome (item survives the attempt), the server rolls:

- `AffixCount = RandomInt(0, Nmax)`

**Rule (LOCKED):**

- If `AffixCount = 0`, the item remains **Mundane** (no conversion; resources are still consumed).
- If `AffixCount > 0`, the item becomes **Magical** and receives that many rolled affixes.

---

## ENHANCEMENT INTEGRATION (LOCKED)

When enhancing a mundane item into a magical item:

1. Server validates eligibility (must be Mundane unless future rules add rerolling)
2. Server selects **N affixes** from the item’s allowed pools
3. Server rolls magnitudes from the tier table
4. Server applies conflicts (no duplicate groups on same item)
5. Server writes `RolledAffixes[]` and sets `ItemPowerState = Magical`

Breakage chance and success chance are defined by the Crafting/Enhancement system.

---

## OPEN QUESTIONS (PROPOSED, NOT LOCKED)

These are intentionally not locked yet:

- Exact tier counts and rarity weights
- Exact maximums for FC/LMC/SDI stacking in PvP
- Whether multiple skill bonuses may stack additively across different items, vs HighestOnly (defaults to HighestOnly here)

---

## NEXT IMPLEMENTATION ARTIFACTS

1. `AffixId` enum (append-only)
2. `AffixDef` ScriptableObject schema
3. `AffixPoolDef` ScriptableObject
4. Item enhancement resolver (server)
5. Equipment stat aggregator (server)

---

## DESIGN LOCK CONFIRMATION

This document is **authoritative**.

Any changes must:

- Increment Version
- Update Last Updated
- Call out save-data implications

