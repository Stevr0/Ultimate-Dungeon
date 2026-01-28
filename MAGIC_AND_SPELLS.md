# MAGIC_AND_SPELLS.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 1.0  
Last Updated: 2026-01-28

---

## PURPOSE

This document defines the **authoritative Magic & Spellcasting system** for *Ultimate Dungeon*, based heavily on **Ultima Online** spell mechanics and adapted for a **server-authoritative Unity 6 multiplayer environment**.

Magic must be fully defined **before combat math** is implemented. All combat, survival, and PvP interactions involving magic must derive from the rules in this document.

If a spell, rule, or interaction is not defined here, **it does not exist**.

---

## CORE DESIGN PILLARS (LOCKED)

1. **Ultima Online–Style Magic**
   - Circle-based spell progression
   - Reagents required for most spells
   - Mana-based casting
   - Cast times (not instant)

2. **Server-Authoritative Casting**
   - All validation, timing, interruption, and resolution occurs on the server
   - Clients submit *intent*, never outcomes

3. **Spells Are Actions**
   - Casting consumes time
   - Casting exposes risk
   - Casting may be interrupted

4. **Magic Is Deterministic**
   - No client RNG
   - All rolls are server-seeded and reproducible

5. **Direct Damage Is Allowed**
   - Some spells deal immediate damage
   - Damage still resolves through the combat/damage pipeline

---

## MAGIC SKILLS (LOCKED)

The following skills govern all spellcasting behavior:

- **Magery** — ability to cast spells and determine success chance
- **Evaluating Intelligence** — modifies spell effectiveness and damage
- **Meditation** — mana regeneration rate
- **Resist Spells** — mitigates spell effects and magical damage

These skills already exist in `SKILLS.md` and are not redefined here.

---

## SPELL CIRCLES (LOCKED MODEL)

Spells are grouped into **Circles**, following Ultima Online tradition.

- Circle 1 (Weak utility / damage)
- Circle 2
- Circle 3
- Circle 4
- Circle 5
- Circle 6
- Circle 7
- Circle 8 (Powerful / dangerous magic)

### Circle Rules

- Higher circles:
  - Require higher **Magery** skill
  - Have longer cast times
  - Consume more mana
  - Use rarer reagents
  - Are easier to interrupt

---

## SPELL KNOWLEDGE (LOCKED)

- Spells are **not learned automatically**
- Players must acquire **spell scrolls** to learn spells
- Once learned, the spell is permanently available in the player’s spellbook
- Spellbooks are **account-bound**, not item-bound

> Losing a spellbook item does NOT remove learned spells

---

## SPELL DEFINITION MODEL

Each spell is defined via a `SpellDef` ScriptableObject.

### Required Fields

- `SpellId` (stable enum)
- `DisplayName`
- `Circle`
- `RequiredMagery`
- `RequiredEvaluatingIntelligence` (optional)
- `ManaCost`
- `BaseCastTimeSeconds`
- `CooldownSeconds`
- `Reagents[]`
- `TargetingType`
- `Range`

### Effect Fields

A spell may define **one or more effects**:

- Direct Damage
- Status Effect Application
- Attribute Modifier
- Resistance Modifier
- Movement Restriction
- Utility Effect (reveal, teleport, unlock, etc.)

Effects are resolved **in order**, server-side.

---

## DIRECT DAMAGE SPELLS (LOCKED)

Some spells deal **immediate damage** on successful cast.

### Damage Resolution Rules

- Damage is applied instantly on cast completion
- Damage is:
  - Modified by **Evaluating Intelligence**
  - Mitigated by target resistances
  - Reduced by **Resist Spells** skill

### Example

- *Fireball*:
  - Base Damage: 12–16
  - Damage Type: Fire
  - Modified by Eval Int
  - Mitigated by Fire Resistance

---

## CAST TIME & CAST SPEED (LOCKED)

### Base Cast Time

Each spell has a base cast time defined by:

- Spell circle
- Spell complexity

### Cast Speed Modifiers

Cast time may be modified by:

- Dexterity (minor)
- Items (e.g. Faster Casting)
- Status effects

> Cast time may never be reduced below **50% of base**

---

## COOLDOWNS (LOCKED MODEL)

- Each spell has an individual cooldown
- Cooldowns prevent repeated instant casting
- Cooldowns are tracked server-side per spell

Cooldowns do NOT:
- Block other spells
- Block movement

---

## SPELL INTERRUPTION (LOCKED)

Casting can be interrupted if:

- The caster takes damage
- The caster is stunned, paralyzed, or silenced
- The caster moves

### Interruption Rules

- Interrupted casts:
  - Consume **time**
  - Do **not** consume mana
  - Do **not** consume reagents

Higher-circle spells have **higher interruption chance**.

---

## TARGETING RULES (LOCKED)

Each spell defines exactly one targeting mode:

- Self
- Single Target
- Ground Target
- Area (around target)
- Area (around caster)

Target validation occurs server-side:

- Line of sight
- Range
- Valid target type

---

## STATUS EFFECT INTEGRATION (LOCKED)

Spells may apply status effects defined in `STATUS_EFFECT_CATALOG.md`.

Rules:

- Status effects are independent systems
- Spells may apply, refresh, or stack statuses
- Damage-over-time spells are implemented as status effects

---

## SKILL GAIN FROM SPELLCASTING

On successful spell cast:

- **Magery** gain check
- **Evaluating Intelligence** gain check (if applicable)
- **Meditation** gain check (out-of-combat or passive)

Skill gains are resolved via `SkillGainSystem`.

---

## FAILURE & FIZZLE (LOCKED)

A spell may fail to cast if:

- Magery skill is too low
- Random failure roll occurs

On failure:

- Mana is consumed
- Reagents are consumed
- Spell does not resolve

---

## PVP & BALANCE RULES (LOCKED)

- All spells function identically in PvE and PvP
- No PvP-only scaling
- Balance is achieved through:
  - Cast time
  - Interruptibility
  - Resource pressure

---

## DESIGN CONSEQUENCES (IMPORTANT)

With this document locked:

- Combat math can now be defined cleanly
- Status effect system can be implemented safely
- Item affixes like Faster Casting, Lower Mana Cost, Spell Damage Increase are well-defined

---

## NEXT DEPENDENCIES (UNBLOCKED)

The following can now be implemented:

1. `SpellId` enum
2. `SpellDef` ScriptableObject
3. Spellbook UI (read-only)
4. Spellcasting state machine
5. Combat core (hit / damage / death)

---

## DESIGN LOCK CONFIRMATION

This document is **authoritative**.

Any future changes must:
- Increment the version
- Explicitly call out changed rules
- Avoid breaking save data or combat math

---

**Magic is power, time, and risk.**  
**Spells are choices, not buttons.**

