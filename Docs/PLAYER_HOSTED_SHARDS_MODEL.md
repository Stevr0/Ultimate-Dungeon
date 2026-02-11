# PLAYER_HOSTED_SHARDS_MODEL.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 1.0  
Last Updated: 2026-02-11  
Engine: Unity 6 (URP)  
Networking: Netcode for GameObjects (NGO) + **Steam Networking (Planned)**  
Authority: **Server-authoritative, player-hosted shards**  

---

## PURPOSE

Defines and **locks** the Player-Hosted Shard model for *Ultimate Dungeon*.

This model establishes:
- One persistent **player-owned world (Shard)** per player
- A **single global character** per account
- Public shard discovery via a lobby browser
- Shard-local PvP dungeons with intentional host advantage
- Physical, risk-based item transport between shards

This document is **authoritative** for multiplayer topology, identity, and shard boundaries.

---

## CORE DESIGN SUMMARY (LOCKED)

- Each account has **exactly one character**
- Each player may host **one personal shard**
- Shards are **public and discoverable** (no passwords)
- Characters may freely **visit other players’ shards**
- All dungeons run **inside the owning shard** and are **PvP-enabled**
- Items travel **only via what the character physically carries**
- Steam networking is the intended transport layer

---

## SCOPE BOUNDARIES (NO OVERLAP)

Owned elsewhere:
- Combat legality & PvP rules: `ACTOR_MODEL.md`, `TARGETING_MODEL.md`
- Combat execution: `COMBAT_CORE.md`
- Item identity & instances: `ITEMS.md`, `ITEM_DEF_SCHEMA.md`, `ITEM_CATALOG.md`
- Housing & building rules: `HOUSING_RULES.md`
- Save format & low-level persistence: `SESSION_AND_PERSISTENCE_MODEL.md`

This document does **not** define:
- UI layouts
- Anti-cheat beyond trust boundaries
- Dungeon generation algorithms

---

## DEFINITIONS

### Character (LOCKED)
- Each account owns **exactly one persistent character**.
- The character is global and **not shard-scoped**.
- Character progression, inventory, and identity persist across shard visits.

### Shard (LOCKED)
A **player-hosted persistent world**.

- Exactly one shard per player.
- Exists only while the host is running the game.
- The host is the authoritative server.
- World state is persisted locally by the host.

### Lobby Service
A lightweight discovery directory.

- Lists active shards.
- Stores **metadata only**.
- Has no gameplay authority.

### Dungeon
A PvP-enabled risk space.

- Always runs **inside the owning player’s shard**.
- Uses the shard host as authority.
- Visitors may experience higher latency; this is intentional.

---

## DESIGN LOCKS (MUST ENFORCE)

1. **Single Character per Account**
   - No character-per-shard model.

2. **One Shard per Player**
   - A player hosts one persistent world.

3. **Public Shards Only**
   - No passwords.
   - All shards are visible in the lobby.
   - Access control is handled via in-world permissions only.

4. **Shard Lifecycle**
   - Shard exists only while host is online.
   - Host disconnect = shard offline.

5. **Shard Creation Generates a World Seed**
   - A new `WorldSeed` is generated on shard creation.
   - The seed drives world and dungeon generation.
   - The same seed is reused on resume.

6. **Shard-Local PvP Dungeons**
   - Dungeons always run on the shard host.
   - PvP is always enabled in dungeon scenes.

7. **Physical Item Transport Only**
   - Items move between shards only via a character’s carried inventory.
   - No mail, bulk export, or remote trade systems.

---

## TOPOLOGY

### Runtime Model
- Host runs an NGO **Listen Server** for their shard.
- Visitors connect as clients.

### Networking (LOCKED INTENT)
- Steam networking is the planned transport layer.
- Steam handles NAT traversal and relay.
- Lobby entries resolve to Steam session identifiers.

---

## SHARD DISCOVERY (LOBBY)

### Shard Listing Fields
- `ShardSessionId`
- `HostAccountId`
- `ShardName` (default: "<PlayerName>'s Shard")
- `BuildId`
- `WorldSeed`
- `MaxPlayers`
- `CurrentPlayers`
- `Region` (optional)
- `Tags[]` (optional)

### Listing Rules
- Only active shards are listed.
- Shards are removed automatically if heartbeat expires.

---

## JOIN FLOW

1. Player selects a shard from the lobby.
2. Client connects via Steam session.
3. Shard server performs connection approval:
   - BuildId mismatch → reject
   - Duplicate login → reject
   - Shard full → reject
4. Player spawns at a visitor spawn point.

---

## PERSISTENCE MODEL

### Shard-Owned Data
- World state (terrain, housing, vendors, placed objects)
- Stored on host machine.

### Character-Owned Data
- Character stats, skills, inventory, equipment
- Persist globally with the character

### Transport Rule
- Only items currently carried by the character may move between shards.

---

## PERMISSIONS

### Roles
- **Owner**: shard host
- **CoOwner**: full building/vendor rights
- **Editor**: limited building rights
- **Visitor**: no edit rights

### Enforcement
- All permission checks are server-side on the shard host.

---

## SECURITY & TRUST MODEL

- MVP is **trust-based**.
- Host is authoritative within their shard.
- Cheating only affects that shard.

Future mitigations may include:
- Official shards
- Verified PvP environments

---

## REQUIRED DOCUMENT INDEX UPDATE

Add under **MULTIPLAYER TOPOLOGY**:

- `PLAYER_HOSTED_SHARDS_MODEL.md`
  - Player-hosted shards, single-character identity, public shard discovery, shard-local PvP dungeons, and physical item transport rules.

---

## DESIGN LOCK CONFIRMATION

This document is **locked and authoritative**.

Any change must:
- Increment Version
- Update Last Updated
- Explicitly call out impacts to identity, persistence, economy, or PvP

