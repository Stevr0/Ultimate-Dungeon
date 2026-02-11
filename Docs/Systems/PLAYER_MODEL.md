# PLAYER_MODEL.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 1.0  
Last Updated: 2026-02-11  
Engine: Unity 6 (URP)  
Networking: Netcode for GameObjects (NGO) + Steam Networking (Planned)  
Authority: Server-authoritative (Shard Host while connected)  

---

## PURPOSE

Defines the **Player runtime and persistence model** for *Ultimate Dungeon*.

This document locks:
- What a Player is (and is not)
- Player identity and lifecycle
- Relationship between Player, Character, and Shard
- High-level invariants relied on by combat, UI, inventory, and persistence systems

This document intentionally avoids combat math, UI layout, and item schemas.

---

## SCOPE BOUNDARIES (NO OVERLAP)

Owned elsewhere:
- Shard topology & hosting: `PLAYER_HOSTED_SHARDS_MODEL.md`
- Session flow & persistence mechanics: `SESSION_AND_PERSISTENCE_MODEL.md`
- Actor legality & scene rules: `ACTOR_MODEL.md`, `SCENE_RULE_PROVIDER.md`
- Combat execution: `COMBAT_CORE.md`
- Item identity & instances: `ITEMS.md`, `ITEM_DEF_SCHEMA.md`

This document does **not** define:
- Combat formulas
- Input bindings
- UI panels or hotkeys
- Save file formats

---

## CORE DEFINITIONS

### Account (LOCKED)

- A real-world user identity.
- **Planned:** backed by Steam (`SteamId`).
- One account may log in from one client at a time.

---

### Character (LOCKED)

- Exactly **one Character per Account**.
- The Character represents all long-term progression:
  - Attributes
  - Skills
  - Inventory
  - Equipment
  - Learned spells

- The Character is **global**:
  - It is not owned by any shard.
  - It persists across shard visits.

---

### Player (Runtime Entity)

- A Player is the **runtime representation** of a Character inside a shard session.
- Created when a client successfully joins a shard.
- Destroyed when the client leaves or disconnects.

> A Player is not a save file and not an account. It is a runtime actor.

---

## DESIGN LOCKS (MUST ENFORCE)

1. **Single Character Rule**
   - Each Account owns exactly one Character.

2. **Character Persistence Is Global**
   - Progression is never shard-scoped.

3. **Shard Authority While Connected**
   - While inside a shard, the shard host is authoritative over the Player runtime entity.

4. **Client Supplies Character Snapshot**
   - The client provides its Character Snapshot when joining a shard.

5. **No Server-Owned Characters (MVP)**
   - Characters are not stored centrally or per-shard.

---

## PLAYER LIFECYCLE

### Login (Client-Side)

1. Client authenticates with Steam.
2. Client loads local Character data.
3. Client enters Lobby.

---

### Join Shard

1. Client selects a shard from the Lobby.
2. Client connects via Steam session.
3. Client sends Character Snapshot to shard host.
4. Shard host validates snapshot shape and caps.
5. Player runtime entity is spawned.

---

### In-Shard Runtime

While connected:
- Player exists as an **Actor** (`ActorType.Player`).
- Player is subject to:
  - SceneRuleContext legality
  - Actor legality rules
  - Housing permissions (if applicable)

All gameplay intents flow:
> Client → Server (Shard Host) → Validation → Execution

---

### Leave Shard

On voluntary leave or disconnect:
1. Shard host sends final Character Snapshot to client (best effort).
2. Client commits snapshot locally.
3. Player runtime entity is destroyed.

---

## PLAYER VS SHARD RESPONSIBILITIES

### Player-Owned (Character)

- Attributes & stats
- Skills & progression
- Inventory & equipment
- Learned abilities

### Shard-Owned (World)

- Village build state
- Vendors
- NPCs and monsters
- Dungeon instances

---

## MULTI-SHARD VISITING (LOCKED)

- A Character may visit any active shard.
- Inventory travels with the Character.
- Only carried items move between shards.
- There is no shard-level inventory or stash.

---

## FAILURE & RECOVERY (SUMMARY)

- Client crash:
  - Character recovers from last committed local snapshot.

- Host crash:
  - Player is disconnected.
  - Character recovers locally.

Detailed mechanics are owned by `SESSION_AND_PERSISTENCE_MODEL.md`.

---

## REQUIRED UPDATES TO DOCUMENTS_INDEX.md (PATCH)

Ensure `PLAYER_MODEL.md` is listed under **CORE PLAYER & IDENTITY** (or equivalent) with description:

> Defines Player runtime entity, single-character-per-account rule, and character vs shard ownership boundaries.

---

## DESIGN LOCK CONFIRMATION

This document is **authoritative**.

Any change must:
- Increment Version
- Update Last Updated
- Explicitly call out impacts to identity, persistence, or shard authority

