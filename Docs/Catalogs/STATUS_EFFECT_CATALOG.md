# STATUS_EFFECT_CATALOG.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 1.0  
Last Updated: 2026-01-28  
Engine: Unity 6 (URP)  
Networking: Netcode for GameObjects (NGO)  
Authority: Server-authoritative  
Determinism: Required (server-seeded)

---

## PURPOSE

Defines the **authoritative catalog of Status Effects** in *Ultimate Dungeon*.

This document is the single source of truth for:
- Stable `StatusEffectId` identifiers (append-only)
- High-level effect semantics (what the status *means*)
- Default stacking rules and dispel tags
- What systems a status is allowed to influence (combat, movement, casting, perception)

Statuses are a **core pillar**:
- Spells apply/remove statuses via `SpellDef` payloads
- Weapons procs may apply statuses via hit spells (curated)
- Combat queries status gates (stun/paralyze/disarm/etc.)

If a status is not listed here, **it does not exist**.

---

## DESIGN LOCKS (MUST ENFORCE)

1. **Stable IDs (append-only)**
   - Never reorder or rename shipped IDs without explicit migration.

2. **Server authoritative**
   - Only the server may apply/remove/tick statuses.

3. **Deterministic resolution**
   - Any chance roll (apply chance, resist chance, tick damage roll) is server-seeded.

4. **Data-driven**
   - No per-status hardcoded gameplay in random systems.
   - Systems query status flags and read structured status data.

5. **Status-first integrity**
   - Combat, movement, and casting must respect status gates defined here.

---

## STATUS EFFECT ID MODEL (AUTHORITATIVE)

Implement as `enum StatusEffectId` (recommended) or stable string IDs.

**Rule:** IDs are **append-only**.

Naming convention:
- `Stun_*`, `Control_*`, `Debuff_*`, `Buff_*`, `Dot_*`, `Utility_*`, `State_*`

---

## SHARED ENUMS / FLAGS (AUTHORITATIVE)

### StatusTags (flags)
Used for dispels, UI grouping, and general rules.

- **Buff**
- **Debuff**
- **Control** (stun/paralyze/root)
- **Dot** (damage over time)
- **Reveal** (affects stealth/invisibility)
- **Summon** (summoned entities/effects)
- **Field** (persistent ground effects)

### StackRule (authoritative semantics)
- **Refresh** — reapply resets duration, keeps magnitude
- **AddStack** — adds a stack (with optional max stacks)
- **Replace** — replaces existing instance (magnitude and duration)
- **IgnoreIfPresent** — no effect if already present

### DurationModel
- **Timed** — expires after duration
- **UntilRemoved** — persists until explicitly removed/cleansed

### TickModel
- **None** — no periodic tick
- **Interval** — ticks every N seconds

---

## GLOBAL STATUS GATES (LOCKED)

These gates are queried by Combat Core and other systems.

### Action Blocks
- **BlocksWeaponAttacks**
- **BlocksSpellcasting**
- **BlocksBandaging**
- **BlocksMovement**

### Modifiers
- **MoveSpeedMultiplier** (e.g., Slow/Haste)
- **SwingSpeedMultiplier** (optional; default 1.0)
- **CastSpeedMultiplier** (optional; default 1.0)

> Multipliers stack multiplicatively unless a status declares `HighestOnly` (not used in v1).

---

## RESIST SPELLS INTEGRATION (LOCKED)

Some statuses are resistible.

### Resistible statuses
A resistible status declares:
- `canBeResisted = true`
- `resistScalar` (0–1) describing how strongly Resist Spells affects the application

**Rule:** Resist Spells never grants full immunity unless explicitly stated.

---

## STATUS DEFINITIONS (AUTHORITATIVE CATALOG)

Each status entry defines:
- `StatusEffectId`
- Display name
- Tags
- Default duration and model
- Stack rule
- Resistible flag
- Tick behavior (if any)
- Core gameplay semantics (what systems must do)

### 001 — Control_Stunned
- **Id:** `Control_Stunned`
- **Name:** Stunned
- **Tags:** Debuff, Control
- **DurationModel:** Timed
- **Default Duration:** 2s *(proposed; can be overridden by payload)*
- **StackRule:** Refresh
- **Resistible:** Yes *(canBeResisted=true, resistScalar=0.5)*
- **Tick:** None
- **Semantics (LOCKED):**
  - BlocksWeaponAttacks
  - BlocksSpellcasting
  - BlocksBandaging
  - BlocksMovement

### 002 — Control_Paralyzed
- **Id:** `Control_Paralyzed`
- **Name:** Paralyzed
- **Tags:** Debuff, Control
- **DurationModel:** Timed
- **Default Duration:** 6s *(proposed)*
- **StackRule:** Refresh
- **Resistible:** Yes *(resistScalar=0.7)*
- **Tick:** None
- **Semantics (LOCKED):**
  - BlocksWeaponAttacks
  - BlocksSpellcasting
  - BlocksBandaging
  - BlocksMovement

### 003 — Control_Rooted
- **Id:** `Control_Rooted`
- **Name:** Rooted
- **Tags:** Debuff, Control
- **DurationModel:** Timed
- **Default Duration:** 4s *(proposed)*
- **StackRule:** Refresh
- **Resistible:** Yes *(resistScalar=0.5)*
- **Tick:** None
- **Semantics (LOCKED):**
  - BlocksMovement
  - Does **not** block weapon attacks, spellcasting, or bandaging

### 004 — Debuff_Silenced
- **Id:** `Debuff_Silenced`
- **Name:** Silenced
- **Tags:** Debuff
- **DurationModel:** Timed
- **Default Duration:** 6s *(proposed)*
- **StackRule:** Refresh
- **Resistible:** Yes *(resistScalar=0.6)*
- **Tick:** None
- **Semantics (LOCKED):**
  - BlocksSpellcasting
  - Does **not** block weapon attacks or bandaging

### 005 — Debuff_Disarmed
- **Id:** `Debuff_Disarmed`
- **Name:** Disarmed
- **Tags:** Debuff
- **DurationModel:** Timed
- **Default Duration:** 5s *(proposed)*
- **StackRule:** Refresh
- **Resistible:** No
- **Tick:** None
- **Semantics (LOCKED):**
  - Cannot perform weapon attacks with equipped weapons
  - May fall back to unarmed if allowed by Combat Core

### 006 — Dot_Poisoned
- **Id:** `Dot_Poisoned`
- **Name:** Poisoned
- **Tags:** Debuff, Dot
- **DurationModel:** Timed
- **Default Duration:** 10s *(proposed)*
- **StackRule:** Replace
- **Resistible:** Yes *(resistScalar=0.8)*
- **Tick:** Interval (every 2s)
- **Tick Damage Type:** Poison
- **Magnitude Meaning:** Poison tier (1–5)
- **Semantics (LOCKED):**
  - Applies periodic poison damage via DamagePackets
  - Stronger tier increases tick damage (exact numbers are balance)

### 007 — Dot_Bleeding
- **Id:** `Dot_Bleeding`
- **Name:** Bleeding
- **Tags:** Debuff, Dot
- **DurationModel:** Timed
- **Default Duration:** 8s *(proposed)*
- **StackRule:** Refresh
- **Resistible:** No
- **Tick:** Interval (every 2s)
- **Tick Damage Type:** Physical
- **Magnitude Meaning:** Bleed strength (1–3)
- **Semantics (LOCKED):**
  - Applies periodic physical damage via DamagePackets

### 008 — Debuff_Slowed
- **Id:** `Debuff_Slowed`
- **Name:** Slowed
- **Tags:** Debuff
- **DurationModel:** Timed
- **Default Duration:** 6s *(proposed)*
- **StackRule:** Refresh
- **Resistible:** Yes *(resistScalar=0.5)*
- **Tick:** None
- **MoveSpeedMultiplier:** 0.70 *(30% slow; proposed)*
- **Semantics (LOCKED):**
  - Reduces move speed

### 009 — Buff_Hasted
- **Id:** `Buff_Hasted`
- **Name:** Hasted
- **Tags:** Buff
- **DurationModel:** Timed
- **Default Duration:** 6s *(proposed)*
- **StackRule:** Refresh
- **Resistible:** No
- **Tick:** None
- **MoveSpeedMultiplier:** 1.15 *(15% haste; proposed)*
- **Semantics (LOCKED):**
  - Increases move speed

### 010 — Utility_Invisible
- **Id:** `Utility_Invisible`
- **Name:** Invisible
- **Tags:** Buff, Utility
- **DurationModel:** Timed
- **Default Duration:** 10s *(proposed)*
- **StackRule:** Refresh
- **Resistible:** No
- **Tick:** None
- **Semantics (LOCKED):**
  - Actor cannot be targeted by hostile spells/attacks unless revealed
  - Any offensive action breaks invisibility (attack, harmful spell)

### 011 — Utility_Revealed
- **Id:** `Utility_Revealed`
- **Name:** Revealed
- **Tags:** Debuff, Reveal
- **DurationModel:** Timed
- **Default Duration:** 8s *(proposed)*
- **StackRule:** Refresh
- **Resistible:** No
- **Tick:** None
- **Semantics (LOCKED):**
  - Removes invisibility/hidden states
  - Prevents re-entering invisibility/hidden while active

### 012 — Buff_ReactiveArmor
- **Id:** `Buff_ReactiveArmor`
- **Name:** Reactive Armor
- **Tags:** Buff
- **DurationModel:** Timed
- **Default Duration:** 120s *(proposed)*
- **StackRule:** Refresh
- **Resistible:** No
- **Tick:** None
- **Semantics (LOCKED):**
  - Provides a defensive modifier (exact numbers live in Buff modifier data)

### 013 — Buff_MagicReflection
- **Id:** `Buff_MagicReflection`
- **Name:** Magic Reflection
- **Tags:** Buff
- **DurationModel:** Timed
- **Default Duration:** 120s *(proposed)*
- **StackRule:** Refresh
- **Resistible:** No
- **Tick:** None
- **Semantics (LOCKED):**
  - Reflects or mitigates spell payloads per magic rules (exact behavior specified in Magic system)

---

## DISPEL / CURE MAPPING (LOCKED)

These mappings allow Magic spells (Dispel/Cure/Arch Cure) to work generically.

### Cure (removes poison)
- Cure removes: `Dot_Poisoned`

### Remove debuffs (future)
- Some “cleanse” effects may remove:
  - `Dot_Bleeding`
  - `Debuff_Slowed`
  - `Debuff_Silenced`

### Dispel (summons/fields/buffs)
Dispel targets tags:
- Summon
- Field
- Certain Buff tags (curated)

> The actual Dispel selection rules are implemented by the spell payload using tags.

---

## COMBAT CORE REQUIREMENTS (LOCKED)

Combat Core must treat these as authoritative:

- `Control_Stunned` and `Control_Paralyzed`:
  - BlockWeaponAttacks, BlockSpellcasting, BlockBandaging, BlockMovement

- `Control_Rooted`:
  - BlocksMovement only

- `Debuff_Silenced`:
  - BlocksSpellcasting only

- `Debuff_Disarmed`:
  - BlocksWeaponAttacks with equipped weapons

- `Dot_Poisoned`, `Dot_Bleeding`:
  - Emit periodic DamagePackets

- `Utility_Invisible`, `Utility_Revealed`:
  - Targeting legality gates

---

## IMPLEMENTATION NOTES (NEXT)

1. Create `StatusEffectId.cs` from this catalog (append-only)
2. Implement `StatusEffectDef` ScriptableObject schema (duration/stack/tick/modifiers/tags)
3. Implement `StatusEffectRegistry` (StatusEffectId → Def)
4. Implement server `StatusEffectSystem`
   - Apply/Remove/Refresh/Stack
   - Tick scheduler (interval-based)
   - Event emission for UI/VFX
5. Integrate with:
   - Combat Core gates
   - Spell payload ApplyStatus / RemoveStatus
   - Dispel/Cure selection by tags

---

## OPEN QUESTIONS (PROPOSED — NOT LOCKED)

- Final numeric balance for:
  - Default durations
  - Tick damage values
  - Slow/Haste multipliers
- Whether Stun and Paralyze should differ in interruption semantics (currently identical)
- Whether Disarm can forcibly unequip or simply blocks usage (currently blocks usage)

---

## DESIGN LOCK CONFIRMATION

This document is **authoritative**.

Any change must:
- Increment Version
- Update Last Updated
- Call out save-data and combat implications

