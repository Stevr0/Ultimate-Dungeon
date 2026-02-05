# PROGRESSION — SkillUseResolver + StatGainSystem (AUTHORITATIVE)

Version: 0.1  
Last Updated: 2026-01-27  
Project: **Ultimate Dungeon** (Unity 6, NGO, Server-authoritative)

---

## Purpose

This document defines the **server-authoritative bridge** between:

- **Gameplay actions** (combat swings, spell casts, bandages, crafting, etc.)
- **Skill checks** (success/failure math)
- **Progression hooks**:
  - `SkillGainSystem` (already exists)
  - `StatGainSystem` (new)

This is the missing layer that allows gameplay systems to say:

> “The player meaningfully used *Swords* against difficulty X and succeeded.”

…and have the server:

1. Resolve the skill check result deterministically
2. Attempt a skill gain using existing cap/lock laws
3. Optionally attempt a stat gain (STR/DEX/INT)

---

## Design Locks (Must Not Break)

1. **Server is the only writer** for skill values and base stats.
2. **No classes / no levels / no XP.**
3. **Skills are use-based**.
4. **Total skill cap = 700** (enforced by `SkillGainSystem`).
5. **Manual cap management** via skill lock states (+ / − / 🔒).
6. **Deterministic math**: identical inputs must produce identical outputs.

Source alignment:
- `PLAYER_MODEL.md`
- `PLAYER_DEFINITION.md`
- `SKILLS.md`

---

## System Overview

### What this adds

- **`SkillUseResolver`** (new)
  - Pure server logic to resolve skill checks + dispatch progression attempts.

- **`StatGainSystem`** (new)
  - Pure server logic to apply **base STR/DEX/INT** gains.

### What this does NOT do

- It does **not** decide *when* a skill is used.
  - Combat/Crafting/Magic systems decide that.
- It does **not** implement combat.
  - Combat will call into this.

---

## Data Flow

1. **Gameplay system** (e.g., combat swing) determines:
   - Which `SkillId` was meaningfully used
   - A numeric difficulty/opposition
   - The action outcome context (combat/crafting/etc.)

2. Gameplay system calls:
   - `SkillUseResolver.ResolveAndProgress(...)`

3. Server returns:
   - `SkillUseResult` (success/fail + rolled chance + progression results)

4. Server replicates updated skills/stats through existing Net sync.

---

## “Meaningful Use” Rules (Progression Gate)

A gain attempt is allowed only when ALL are true:

- The skill lock state is **Increase (+)**
- The action is a **meaningful use** of that skill (caller asserts)
- The action is not blocked by a **per-skill cooldown** (optional)
- The action outcome is **Success** or **Partial Success** (caller passes)

> NOTE: Skill gain *chance curves* are **not locked** yet.
> This doc provides safe scaffolding + tuning points.

---

## Determinism Rules

### Why determinism matters

If skill checks and gains are not deterministic, you will see:
- Client/server divergence (if clients ever predict)
- Desync in replays or debugging
- Hard-to-reproduce progression bugs

### Determinism contract

- Skill check RNG and gain RNG are derived from a **single server seed**.
- Seed must be based on stable values:
  - Player NetworkObjectId (or OwnerClientId)
  - Server tick / server time step index
  - SkillId
  - A caller-provided “event nonce” (optional)

This ensures:
- Same event → same roll
- Different event → different roll

---

## Stat Gains (UO-like) — Proposed (Not Yet Locked)

To “tie in stats and skills gains”, we implement stat gain attempts driven by skill use.

### Proposed mapping

- **STR-aligned**: Swords, Macing, Wrestling, Tactics, Anatomy, Parrying
- **DEX-aligned**: Fencing, Archery, Hiding, Stealth, Lockpicking, Healing
- **INT-aligned**: Magery, Meditation, Evaluating Intelligence, Resist Spells, Alchemy

> This mapping is **tuning data**, not hardcoded law.

---
