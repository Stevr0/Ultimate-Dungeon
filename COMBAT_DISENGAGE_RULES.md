# COMBAT_DISENGAGE_RULES.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 1.0  
Last Updated: 2026-01-30  
Engine: Unity 6 (URP)  
Networking: Netcode for GameObjects (NGO)  
Authority: Server-authoritative  

---

## PURPOSE

Defines the authoritative rules for **ending combat** (disengage) in *Ultimate Dungeon*.

This document answers:
- When does an Actor leave `CombatState.InCombat`?
- What events extend combat?
- What must be cleared when combat ends?
- How do **safe scenes** guarantee peace?

If a disengage rule is not defined here, **it does not exist**.

---

## DEPENDENCIES (MUST ALIGN)

- `ACTOR_MODEL.md`
  - Defines `CombatState` values and the rule that safe scenes may never enter `InCombat`.
- `TARGETING_MODEL.md`
  - Defines legal hostile intents and deny reasons.
- `COMBAT_CORE.md`
  - Defines combat scheduling/resolution and the requirement to notify combat state tracking on hostile actions.
- `SCENE_RULE_PROVIDER.md`
  - Defines scene transitions that must clear illegal state (combat, selection, timers).

---

## SCOPE BOUNDARIES (NO OVERLAP)

### This document owns
- The **disengage timer** model
- The definition of **combat-extending events**
- Combat exit side-effects (what must be cleared)
- Scene-based overrides for safe scenes

### This document does NOT own
- Hostility/PvP legality (owned by `ACTOR_MODEL.md`)
- Hit/miss/damage math or order-of-operations (owned by `COMBAT_CORE.md`)
- Status definitions (owned by `STATUS_EFFECT_CATALOG.md`)
- Death/respawn/corpse/insurance rules (owned by death + player rules docs)

---

## DESIGN LOCKS (MUST ENFORCE)

1. **Server authoritative**
   - Only the server may start, extend, or end combat state.

2. **Scene rules are hard gates**
   - If `CombatAllowed == false`, an Actor may never be `InCombat`.
   - Entering a safe scene forces `Peaceful` immediately.

3. **Single timer model per Actor**
   - Each Actor has a single authoritative `combatUntilTime` (or equivalent).

4. **No hidden exceptions**
   - If an event should extend combat, it must be listed in this document.

---

## TERMS

### Combat Engagement Window
A time window during which an Actor is considered **in combat**.

- An Actor is `InCombat` if `now < combatUntilTime`.
- When `now >= combatUntilTime`, the Actor returns to `Peaceful`.

### Disengage Duration
The time added to the engagement window when a combat-extending event occurs.

---

## AUTHORITATIVE CONSTANTS

> These constants are authoritative policy values.

### Disengage Duration
- `DISENGAGE_SECONDS = 10.0`

Meaning:
- After the last combat-extending event affecting an Actor, they remain `InCombat` for **10 seconds**.

> If later tuning is needed, this value changes here and must be applied consistently everywhere.

---

## COMBAT STATE MODEL (RECAP)

This doc assumes the `CombatState` enum exists in `ACTOR_MODEL.md`:
- `Peaceful`
- `InCombat`
- `Dead`

Rules:
- `Dead` overrides everything: dead actors are never set to `Peaceful` until the death/respawn pipeline transitions them.
- Safe scenes (`CombatAllowed == false`) override everything: actors are forced to `Peaceful` and cannot re-enter combat.

---

## COMBAT-EXTENDING EVENTS (LOCKED)

When any of the following events occurs, the server must **refresh** combat for the relevant Actor(s) by setting:

`combatUntilTime = max(combatUntilTime, now + DISENGAGE_SECONDS)`

### 1) Hostile intent validated (start of aggression)
When the server validates a hostile intent as **Allowed**:
- `TargetIntentType.Attack`
- `TargetIntentType.CastHarmful` *(when Magic exists)*

Refresh combat for:
- Attempter (attacker/caster)

Notes:
- This event must not fire if legality fails.
- This event must not fire in safe scenes.

### 2) Hostile resolution occurs (impact)
When combat resolves a hostile outcome:
- A swing timer completes and results in `Hit` or `Miss`
- A `DamagePacket` is applied *(from any source: weapon, spell, DoT)*

Refresh combat for:
- Attacker (source)
- Victim (receiver)

Notes:
- For DoT ticks, the **source** is the applier of the DoT status.
- Scene must be re-gated (if combat/damage not allowed, do not refresh).

### 3) Receiving hostile targeting while legal (optional, v1 OFF)
This is explicitly **disabled in v1**.

Meaning:
- Simply being selected/targeted does not extend combat.
- Only validated hostile actions and resolved hostile outcomes extend combat.

Rationale:
- Prevents griefing by “perma-combat” targeting.

---

## DISENGAGE TRANSITION (LOCKED)

### When combat ends
On the server tick/update (or as part of a periodic state evaluation), for each Actor:
- If `CombatState == InCombat` and `now >= combatUntilTime`:
  - transition to `Peaceful`

### Side-effects on exit
When transitioning `InCombat → Peaceful`, the server must:

1. **Clear auto-attack scheduling**
   - Cancel swing timers / `AttackLoop` for this Actor

2. **Clear hostile “current target” if it was an attack target** *(recommended, v1 ON)*
   - If the Actor’s current selection was set by an `Attack` intent, clear selection
   - If selection was set by `Select` or `Interact`, keep it

3. **Clear aggression bookkeeping**
   - Any “last hostile actor” references used only for combat state display should be cleared

4. **Do not modify movement**
   - Combat ending does not force-stop movement

---

## SAFE SCENE OVERRIDES (LOCKED)

In `MainlandHousing` and `HotnowVillage`:

1. `CombatAllowed == false` means:
   - Actors may never enter `InCombat`
   - Any attempt to refresh combat is ignored

2. On entering a safe scene (server transition hook):
   - Force `CombatState = Peaceful`
   - Set `combatUntilTime = 0`
   - Cancel swing timers / attacks
   - Clear hostile selection

This must happen even if:
- Timers are pending
- CombatUntilTime is in the future

---

## DUNGEON SCENE RULES (LOCKED)

In `Dungeon` scenes:
- Normal disengage rules apply.
- PvE/PvP share the same disengage model.

---

## UI / REPLICATION CONTRACT (NON-AUTHORITATIVE BUT REQUIRED)

Clients should be able to display a correct combat indicator.

Minimum replicated data per Actor:
- `CombatState` (Peaceful/InCombat/Dead)
- Optionally: `combatRemainingSeconds` for UI only

Notes:
- Clients must not compute combat state locally.

---

## FAILURE MODES (LOCKED)

The server must prevent these:

1. **Perma-combat from selection**
   - Selection alone never refreshes combat (v1).

2. **Safe scene combat leaks**
   - If `CombatAllowed == false`, ignore refresh requests and force Peaceful.

3. **Timer desync**
   - Combat state must be derived from server time, not client time.

---

## IMPLEMENTATION CHECKLIST (NEXT)

1. Add fields to server combat state tracker:
   - `double combatUntilTime` (server time)

2. Add server APIs:
   - `RefreshCombatForActor(actor)`
   - `ForcePeaceful(actor)`

3. Wire refresh calls at exactly these points:
   - After TargetIntent validation for Allowed hostile intents
   - On CombatResolver resolution (hit/miss) and DamagePacket apply

4. Add periodic evaluation:
   - If now >= combatUntilTime and state is InCombat → set Peaceful + side-effects

5. Add scene transition hook:
   - On entering safe scenes, ForcePeaceful + cancel timers + clear hostile selection

---

## DESIGN LOCK CONFIRMATION

This document is **authoritative**.

Any change must:
- Increment Version
- Update Last Updated
- Call out impacted systems (`ACTOR_MODEL`, `COMBAT_CORE`, `TARGETING_MODEL`, `SCENE_RULE_PROVIDER`, AI)

