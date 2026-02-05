# COMBAT_DISENGAGE_RULES.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 1.1  
Last Updated: 2026-01-30  
Engine: Unity 6 (URP)  
Networking: Netcode for GameObjects (NGO)  
Authority: Server-authoritative  

---

## PURPOSE

Defines the authoritative rules for **ending combat** (disengage) in *Ultimate Dungeon*.

---

## DEPENDENCIES (MUST ALIGN)

- `ACTOR_MODEL.md`
- `TARGETING_MODEL.md`
- `COMBAT_CORE.md`
- `SCENE_RULE_PROVIDER.md`

---

## DESIGN LOCKS (MUST ENFORCE)

1. **Server authoritative** (server decides start/extend/end)
2. **Scene rules are hard gates** (safe scenes cannot be `InCombat`)
3. **Single timer per Actor** (`combatUntilTime`)
4. **No hidden exceptions** (only the events listed here extend combat)

---

## AUTHORITATIVE CONSTANTS

- `DISENGAGE_SECONDS = 10.0`

---

## CORE MODEL

Each Actor tracks:
- `combatUntilTime` (server time)
- `hasActiveHostileEngagement` (server truth; attacker-side only)

Meaning:
- `InCombat` if **either**:
  - `hasActiveHostileEngagement == true` *(attacker pursuing/auto-attacking a valid target)*
  - `now < combatUntilTime` *(recently involved in hostile resolution)*

- Combat ends when **both** are false:
  - `hasActiveHostileEngagement == false`
  - `now >= combatUntilTime`

---

## COMBAT-EXTENDING EVENTS (LOCKED)

When any listed event occurs, the server refreshes:

`combatUntilTime = max(combatUntilTime, now + DISENGAGE_SECONDS)`

### 1) Validated hostile intent (attacker only)
When the server validates a hostile intent as **Allowed**:
- `Attack`
- `CastHarmful` *(when magic exists)*

Effects:
- Refresh combat for the **attacker**
- Set `hasActiveHostileEngagement = true` for the attacker

**Design lock:** Range does **not** block intent validation. Range only controls whether a swing/spell can **resolve**.

### 2) Hostile resolution (attacker + victim)
When a hostile outcome resolves:
- Swing timer completes and results in `Hit` or `Miss`
- A `DamagePacket` is applied

Effects:
- Refresh combat for **attacker** and **victim**

### 3) Engagement ends (attacker only)
When the attacker no longer has an active hostile engagement:
- Player cancels attack
- Target becomes invalid (dead/despawned)
- Target becomes illegal (scene gate, hostility gate)

Effects:
- Set `hasActiveHostileEngagement = false`
- Do **not** refresh `combatUntilTime`
- Disengage will occur naturally when `now >= combatUntilTime`

### Selection rule
Selection alone never refreshes combat.

---

## DISENGAGE TRANSITION (LOCKED)

When combat ends (see Core Model):
- Transition `InCombat → Peaceful`
- Cancel attack loops
- Clear hostile selection (attack-driven only)

---

## SAFE SCENE OVERRIDES (LOCKED)

If `CombatAllowed == false`:
- Actor may never be `InCombat`
- On entering the scene, force:
  - `CombatState = Peaceful`
  - `combatUntilTime = 0`
  - `hasActiveHostileEngagement = false`
  - cancel attack loops
  - clear hostile selection

---

## IMPLEMENTATION CHECKLIST

- Track `combatUntilTime` using server time
- Track attacker engagement (`hasActiveHostileEngagement`) based on AttackLoop/intent state
- Refresh only at the listed hook points
- Force clear on safe-scene entry

---

## DESIGN LOCK CONFIRMATION

This document is authoritative.

