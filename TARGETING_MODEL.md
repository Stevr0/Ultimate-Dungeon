# TARGETING_MODEL.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 1.0  
Last Updated: 2026-01-29  
Engine: Unity 6 (URP)  
Networking: Netcode for GameObjects (NGO)  
Authority: Server-authoritative (legality) / Client-authoritative (selection UI)  
Determinism: Required (server validation)

---

## PURPOSE

Defines the **authoritative Targeting model** for *Ultimate Dungeon*, aligned with:
- `ACTOR_MODEL.md` (Actor identity, factions, hostility, PvP legality)
- `COMBAT_CORE.md` (combat execution consumes validated targets)
- `STATUS_EFFECT_CATALOG.md` (invisibility/reveal and other targetability gates)

Targeting is split into two layers:

1) **Local Selection (Client UX)**
- The client selects a target for UI/intent convenience.

2) **Server Validation (Gameplay Authority)**
- The server validates target eligibility and legality at action time.

If a targeting rule is not defined here (or in referenced authoritative docs), **it does not exist**.

---

## SCOPE BOUNDARIES (NO OVERLAP)

### This doc owns
- What a “Target” is (data contract)
- Client selection rules (raycast selection, clearing, UI feedback)
- Server validation rules (eligibility + disposition + legality)
- Disposition-driven UI contract (ring/tint/frame)

### This doc does NOT own
- Faction/hostility/PvP legality definitions *(see `ACTOR_MODEL.md`)*
- Combat execution and scheduling *(see `COMBAT_CORE.md`)*
- Status effect semantics *(see `STATUS_EFFECT_CATALOG.md`)*
- Interaction systems beyond “has a target” *(see interaction docs / `IInteractable`)*

---

## DESIGN LOCKS (MUST ENFORCE)

1. **Client can select; server decides**
   - A selected target is never proof of legality.

2. **Actor-first targeting**
   - Targets are Actors (per `ACTOR_MODEL.md`).

3. **One resolver**
   - Eligibility + Disposition + AttackLegality must be resolved by a single pure rules service.

4. **Status-first targetability**
   - Invisibility and similar states can block targeting.

5. **No duplication**
   - Do not re-define faction or PvP rules here; call Actor rules.

---

## CORE TERMS

### Selected Target (Client)
The actor the player currently has selected locally for UI and intent.

### Validated Target (Server)
The actor reference the server has re-validated at the moment an action is attempted.

### TargetingDisposition
The viewer-relative relationship result:
- Self / Friendly / Neutral / Hostile / Invalid

Defined in `ACTOR_MODEL.md`.

---

## TARGET REFERENCE CONTRACT (LOCKED)

A target reference must be a **stable network identity**.

### Required
- `NetworkObjectId targetNetId` *(or equivalent stable id)*

### Optional (recommended)
- `ActorId targetActorId` *(if separate from net id later)*

**Rule:** The client may only send stable IDs.
The server resolves IDs to runtime Actor objects.

---

## CLIENT TARGET SELECTION (UX) — LOCKED

### Selection rules
- **Left click** selects an Actor under cursor (via raycast).
- **Click empty space** clears target.
- Selection is **local only** (does not replicate).

### Selection filtering (recommended)
Client may filter raycast hits to “selectable” objects only:
- Must have an `ActorComponent` (or equivalent) on root
- Must be alive (optional on client; server will enforce)

> Client filtering is convenience only.
> Server validation is still mandatory.

---

## TARGET CLEARING (LOCKED)

Target must clear when any are true:
- Local player despawns
- Target despawns
- Target becomes invalid by server feedback (e.g., dead or un-targetable)

---

## SERVER VALIDATION (AUTHORITATIVE)

The server validates targeting whenever a gameplay action is attempted:
- Attack start / swing completion
- Harmful spell cast start and completion
- Beneficial spell cast start and completion
- Interaction intent

### Validation outputs
Server validation produces:

1) **Eligibility**
- Is the target targetable right now?

2) **Disposition**
- Friendly / Neutral / Hostile relative to the viewer

3) **Legality**
- For attacks: `AttackLegalityResult`

All of the above are resolved by Actor rules.

---

## TARGET ELIGIBILITY (LOCKED)

Eligibility is resolved by Actor rules but targeting systems must respect these gates:

- Target must exist and be resolvable from the provided ID
- Target must be alive (unless a specific system allows dead targets, e.g., resurrection)
- Target must pass status-based gates:
  - Invisible blocks hostile targeting unless revealed/allowed

**Source of truth:**
- Eligibility semantics live in `ACTOR_MODEL.md` and `STATUS_EFFECT_CATALOG.md`.

---

## TARGET DISPOSITION (LOCKED)

Disposition is computed by Actor rules and is used for:
- Target ring tint
- Target frame iconography
- Whether attack/beneficial actions are likely valid

**Important:**
- A Hostile disposition does not guarantee legality (region rules may still block PvP).

---

## ATTACK LEGALITY GATE (LOCKED)

Combat and harmful spells must request:
- `AttackLegalityResult` (from `ACTOR_MODEL.md`)

If not `Allowed`:
- Action is cancelled
- No resources consumed
- Server may optionally notify client with a short reason code

---

## VISUAL FEEDBACK CONTRACT (LOCKED)

UI visuals are driven by **disposition** and **combat state**, both server-owned values.

### Required UI signals
- Target ring visible when a local target is selected
- Ring tint uses `TargetingDisposition`:
  - Friendly / Neutral / Hostile / Self / Invalid
- Optional: ring pulse when `CombatState.InCombat`

### Important rule
UI can respond instantly to local selection, but must update when server replicates:
- Disposition changes (e.g., criminal flag, summon controller changes)
- CombatState changes (in combat / out of combat)

---

## REQUIRED IMPLEMENTATION ARTIFACTS (NEXT)

1. `ActorComponent`
   - Exposes Actor identity fields required by selection and server resolution.

2. `TargetingResolver` (pure rules)
   - `EvaluateEligibility(viewer, target)`
   - `EvaluateDisposition(viewer, target)`
   - `EvaluateAttackLegality(viewer, target, regionRule)`

3. `PlayerTargeting` (client)
   - Stores selected target id
   - Emits local events for UI

4. `ServerTargetValidator` (server)
   - Resolves IDs to Actors
   - Calls `TargetingResolver`
   - Returns short result codes to action systems

5. UI consumers
   - `TargetRingFactionTint` reads disposition
   - `TargetRingPulse` reads combat state

---

## OPEN QUESTIONS (PROPOSED — NOT LOCKED)

- Whether beneficial spells can target neutrals by default
- Whether dead targets remain selectable for looting / resurrection UX
- Whether the client should be allowed to select invisible targets (likely no)

---

## DESIGN LOCK CONFIRMATION

This document is **authoritative**.

Any change must:
- Increment Version
- Update Last Updated
- Call out impacted dependent systems (Combat, Magic, UI)

