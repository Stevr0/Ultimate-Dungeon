# PVP_RULES.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 0.1  
Last Updated: 2026-01-30  
Engine: Unity 6 (URP)  
Networking: Netcode for GameObjects (NGO)  
Authority: Server-authoritative  
Design Lineage: Ultima Online–style PvP

---

## PURPOSE

Defines the **authoritative Player vs Player (PvP) ruleset** for *Ultimate Dungeon*.

This document answers:
- *When* players may engage in PvP
- *How* hostility is established and cleared
- *What consequences* apply to PvP actions and deaths

This document is **policy-level only**.
It does **not** define:
- Combat math *(see `COMBAT_CORE.md`)*
- Targeting validation order *(see `TARGETING_MODEL.md`)*
- Item loss mechanics *(see `PLAYER_DEFINITION.md`)*

If a PvP rule is not defined here, **it does not exist**.

---

## DESIGN GOALS (LOCKED)

1. **Ultima Online–style risk and consequence**
2. **Geography-based safety** (safe zones vs danger zones)
3. **Player-driven conflict** (no global PvP toggle)
4. **Server-authoritative enforcement**
5. **PvP and PvE share the same combat pipeline**

---

## SCOPE BOUNDARIES

### This document owns
- Player-on-player hostility policy
- Criminal / murderer flagging rules
- PvP legality outcomes
- PvP-related state durations

### This document does NOT own
- Scene rule definitions *(see `ACTOR_MODEL.md`)*
- Faction relationship tables *(see `ACTOR_MODEL.md`)*
- Combat execution *(see `COMBAT_CORE.md`)*
- Corpse, loot, and insurance logic *(see `PLAYER_DEFINITION.md`)*

---

## SCENE-BASED PvP AVAILABILITY (LOCKED)

PvP availability is **strictly gated by SceneRuleContext**.

| SceneRuleContext | PvP Allowed | Notes |
|---|---:|---|
| MainlandHousing | ❌ | Absolute safe zone |
| HotnowVillage | ❌ | Absolute safe hub |
| Dungeon | ✅ | PvP subject to Actor legality |

**Design lock:**
If `PvPAllowed == false`, the server must refuse:
- `Attack` intents targeting Players
- `CastHarmful` intents targeting Players

---

## PLAYER DISPOSITION MODEL (UO-STYLE)

Players use a **criminal flagging model**, inspired by classic Ultima Online.

### PlayerDisposition (AUTHORITATIVE)

Each Player Actor has one disposition state:

- `Innocent`
- `Criminal`
- `Murderer`

Disposition is **server-owned**, time-aware, and replicated for UI only.

---

## DEFAULT RELATIONSHIPS

In `Dungeon` scenes:
- Players are **Neutral** to other Players by default
- Neutral Players may not be attacked **unless legality allows it**

**Important:**
PvP is not “always-on free-for-all”.
Hostility must be *earned*.

---

## HOSTILITY & CRIMINAL ACTIONS (LOCKED)

### Criminal Actions
A Player becomes **Criminal** when they:

- Initiate a hostile action (`Attack` or `CastHarmful`) against an **Innocent Player**

Effects:
- Disposition becomes `Criminal`
- Criminal flag duration begins
- Target becomes legally hostile to the attacker

**Design lock:**
Only the *initiator* is flagged Criminal.

---

### Murder Actions
If a Player kills another Player **while Criminal**, they accumulate a **Murder Count**.

Rules:
- Each Criminal PvP kill increments `MurderCount`
- If `MurderCount >= MURDER_THRESHOLD`, disposition becomes `Murderer`

**Locked:**
- `MURDER_THRESHOLD = 5`

---

## DISPOSITION EFFECTS

### Innocent
- Cannot be attacked by other Innocent Players
- May legally defend against Criminals and Murderers

### Criminal
- May be attacked freely by Innocent Players
- Guards may respond (future system)
- Flag expires after a duration

### Murderer
- Always hostile to Innocent Players
- May be attacked freely by anyone
- Flag is long-duration or permanent (design choice)

---

## FLAG DURATIONS (LOCKED)

Criminal flag duration scales with **current MurderCount** until the Murderer threshold is reached.

### Criminal Flag Duration Scaling (LOCKED)

| MurderCount | Criminal Flag Duration |
|---:|---:|
| 0 | 0 minutes (no criminal flag) |
| 1 | 1 minute |
| 2 | 2 minutes |
| 3 | 3 minutes |
| 4 | 4 minutes |
| 5+ | N/A (Player is a Murderer) |

**Rules:**
- Criminal flag duration is evaluated **when the criminal act occurs**.
- Additional criminal acts while flagged **refresh the timer** using the *current* MurderCount.
- Once `MurderCount >= 5`, the player transitions to `Murderer` and no longer uses Criminal timing.

**Design lock:** Dungeons have **no guards**. Flagging exists to drive legality, consequences, and bounty systems — not guard AI.

---|---:|---|
| Criminal | 2–5 minutes | Timer expires, no hostile actions |
| Murderer | Long / Persistent | Special systems (future) |

> Duration values are intentionally **not locked** in v0.1.

---

## ATTACK LEGALITY IN PvP

`AttackLegalityResolver` must enforce:

1. Scene allows PvP
2. Attacker is alive
3. Target is alive
4. One of the following is true:
   - Target is `Criminal`
   - Target is `Murderer`
   - Attacker is already hostile to target

Otherwise:
- Return `Denied_PvPNotAllowed`

---

## SELF-DEFENSE RULE (LOCKED)

If a Player is attacked illegally:
- The defender may freely retaliate
- Retaliation does **not** flag the defender Criminal

This mirrors Ultima Online’s self-defense model.

---

## COMBAT STATE INTERACTION

- Entering PvP combat transitions Actors to `CombatState.InCombat`
- Combat state clears normally via `CombatStateTracker`
- Scene transitions always clear PvP hostility

**Design lock:**
PvP flags may not persist across scene transitions into safe scenes.

---

## DEATH IN PvP

On Player death due to PvP:
- Death pipeline is identical to PvE
- Corpse creation, item loss, and coin loss are governed by `PLAYER_DEFINITION.md`

**Explicit rule:**
There are **no PvP-only loot exceptions**.

### Victim-driven bounty prompt (LOCKED)
On PvP death in the Dungeon, the server must run this flow:

1. **Server determines legality outcome**
   - Was the kill a *criminal kill*? If yes → `MurderCount` increments (automatic; not optional)

2. **Victim UI appears (client display only)**
   - Message: “You were slain by <AttackerName>”
   - Option: **Set a Bounty**

3. **Victim sets bounty (optional)**
   - Victim inputs a coin amount (Banked Coins only)
   - Victim confirms (irreversible)

4. **Server withdraws funds**
   - Coins are withdrawn from the victim’s **Banked Coins** (up to available balance)

5. **Server credits attacker’s bounty pool**
   - Withdrawn coins are added to the attacker’s **Funded Bounty Pool**
   - The murderer head reward value increases accordingly

This creates player-driven bounties without weakening murder count enforcement.

---

## UI / VISUAL REQUIREMENTS (NON-AUTHORITATIVE)

Clients may display:
- Name hue / icon based on disposition
- Warning prompts when attacking Innocent Players

UI is **informational only**.

---

## IMPLEMENTATION HOOKS (REQUIRED)

1. `PlayerDispositionComponent` (server-owned)
2. `CriminalFlagTimer`
3. `MurderCountTracker`
4. `AttackLegalityResolver` integration
5. Scene transition hook clears flags on safe entry

---

## BOUNTY SYSTEM (LOCKED)

Murder creates an automatic **bounty** on the Murderer.

### Bounty Components (LOCKED)
Bounty is tracked as a single pool for payout:
- **Funded Bounty Pool** (coins already paid into the system; always safe to pay out)

> This document only defines the Funded Bounty Pool. Any future “infamy/escrow” expansion must be defined explicitly and kept non-inflationary.

---

### Automatic bounty funding (LOCKED)
- Each murder adds a bounty obligation.
- Base bounty = **100 Banked Coins**
- Each additional murder increases bounty by **+10% (compound)**

Formula (per murder count):
- `Bounty = 100 * (1.1 ^ (MurderCount - 1))`

Funding rule:
- Bounties are sourced from the Murderer’s **Banked Coins**.
- If insufficient funds exist, withdraw what is available (down to 0).
- The withdrawn amount is added to the **Funded Bounty Pool**.

---

### Victim-paid bounty funding (LOCKED)
When a player dies to PvP in the Dungeon, they may optionally set an additional bounty:
- Bounty is paid from the victim’s **Banked Coins**
- The server withdraws the confirmed amount (up to available balance)
- The withdrawn amount is added to the attacker’s **Funded Bounty Pool**

This enables player-driven revenge bounties without introducing inflation.

---

### Bounty Claiming (LOCKED)
- When a Murderer is killed, a **Head** item is created on the corpse
- Returning the head to a valid bounty turn-in grants the reward
- Reward equals the head’s stored funded bounty value at time of creation

**Anti-abuse (PROPOSED — Not Locked):**
- The player who placed a victim-paid bounty may not claim that specific bounty.

---

### Banked Coin Exhaustion Penalty (LOCKED)

If a Murderer’s **Banked Coins reach 0**:
- They may no longer purchase **item insurance**
- Existing insured items remain insured

---

## TOWN ACCESS RESTRICTIONS (LOCKED)

- Criminals and Murderers **may not enter**:
  - `HotnowVillage`
  - `MainlandHousing`

- Entry attempts are server-refused

---

## MURDERER HIDEOUT (LOCKED)

- Murderers have access to a dedicated **Hideout** inside the dungeon
- Hideout is:
  - Deep enough to be dangerous
  - Safe from guards (none exist in dungeon)
  - A hub for outlaw gameplay

---

## NEW PLAYER PROTECTION

None.

Entering the dungeon implies full acceptance of PvP risk.

---

## DESIGN LOCK CONFIRMATION

This document is **authoritative**.

Any change must:
- Increment Version
- Update Last Updated
- Call out impacted systems (Actor, Targeting, Combat, Economy, UI)

---

## DESIGN LOCK CONFIRMATION

This document is **authoritative**.

Any change must:
- Increment Version
- Update Last Updated
- Call out impacted systems (Actor, Targeting, Combat, UI)

