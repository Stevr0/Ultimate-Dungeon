# TARGETING_MODEL.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 1.2  
Last Updated: 2026-01-30  
Engine: Unity 6 (URP)  
Networking: Netcode for GameObjects (NGO)  
Authority: Server-authoritative  

---

## PURPOSE

Defines the authoritative **targeting and interaction intent model**.

This update clarifies a key policy:
- **Range does not block hostile intent.**
- Range (and LoS, where applicable) determines whether an attack/spell can **resolve**.

---

## DESIGN LOCKS (MUST ENFORCE)

1. Clients submit intent, server validates.
2. SceneRuleContext is a hard gate.
3. Targeting is separate from execution.
4. No hidden auto-hostility.
5. **Hostile intent is not range-gated.**

---

## TARGET INTENT TYPES (LOCKED)

- `Select`
- `Interact`
- `Attack`
- `CastBeneficial`
- `CastHarmful`

---

## TARGETING VALIDATION ORDER (LOCKED)

When the server receives a `TargetIntent`, it validates in this order:

1. Resolve SceneRuleContext
2. Resolve Actor references
3. Basic Actor validity (not dead, exists)
4. Scene gates (`CombatAllowed == false` refuses `Attack` / `CastHarmful`)
5. Intent-specific legality
   - `Select`: allowed if target exists
   - `Interact`: validates interaction range/LoS
   - `Attack`: validates **hostility/legality** via `AttackLegalityResolver`
   - `Cast*`: validates spell targeting rules (self/ally/enemy) and legality
6. Commit + dispatch
   - `Attack`: starts/updates an **engagement** (AttackLoop)

---

## RANGE / LINE OF SIGHT POLICY (LOCKED)

### Selection
- `Select`: no range requirement

### Interaction
- `Interact`: range and LoS are validated at intent time

### Hostile actions
- `Attack`: intent is **not** range-gated.
  - Range/LoS is evaluated during execution (each swing resolution attempt).
  - Out of range means: **no hit resolves**, not that the intent was invalid.

- `CastHarmful`: intent is **not** range-gated (unless a spell explicitly requires it).
  - Range/LoS is evaluated during casting/execution.

Rationale:
- Supports chase/evasion: you can commit to attacking a target, and swings resume when back in range.

---

## ENGAGEMENT VS RESOLUTION (LOCKED)

### Engagement
- A validated `Attack` intent starts or updates an attacker’s **active hostile engagement**.
- Engagement persists until explicitly canceled or invalidated.

### Resolution
- A swing/spell resolves only when execution conditions are met (range/LoS/status gates).

---

## REQUIRED NETWORK EVENTS (LOCKED)

Clients must receive:
- Intent allowed/denied results
- Optional: “You are under attack” notification (first hostile intent allowed OR first hostile resolution)

---

## DESIGN LOCK CONFIRMATION

This document is authoritative.

