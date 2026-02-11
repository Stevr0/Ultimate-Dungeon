# SESSION_AND_PERSISTENCE_MODEL.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 1.0  
Last Updated: 2026-02-11  
Engine: Unity 6 (URP)  
Networking: Netcode for GameObjects (NGO) + **Steam Networking (Planned)**  
Authority: **Shard Host is authoritative while connected** (trust-based MVP)  

---

## PURPOSE

Defines the authoritative model for:
- Account identity & session establishment
- Shard hosting lifecycle
- Joining/leaving shards (visiting)
- Character save/load flow for **single-character accounts**
- Shard world persistence responsibilities
- Crash/disconnect and recovery rules

This model is aligned to:
- `PLAYER_HOSTED_SHARDS_MODEL.md`
- `HOUSING_RULES.md`

---

## DESIGN SUMMARY (LOCKED)

- Each account owns **exactly one character**
- Players host **one public shard** (no passwords)
- Characters may visit other shards
- Dungeons run inside the owning shard
- Steam is the planned transport/identity layer

Persistence is split:
- **Character persistence is player-owned** (travels with the player)
- **World persistence is shard-owned** (lives with the host)

---

## SCOPE BOUNDARIES (NO OVERLAP)

Owned elsewhere:
- Shard topology & discovery: `PLAYER_HOSTED_SHARDS_MODEL.md`
- Housing placement legality: `HOUSING_RULES.md`, `SCENE_RULE_PROVIDER.md`
- Combat, targeting, PvP legality: `COMBAT_CORE.md`, `TARGETING_MODEL.md`, `ACTOR_MODEL.md`
- Item schemas: `ITEMS.md`, `ITEM_DEF_SCHEMA.md`

This document does **not** define:
- UI layout (lobby screens, character screen)
- Anti-cheat beyond the trust boundaries
- Detailed save file schemas (only responsibilities and invariants)

---

## DEFINITIONS

### AccountId (LOCKED)
A stable identity key for a player.

- **Planned:** `AccountId = SteamId`.
- Display name is cosmetic and may change.

### Character (LOCKED)
- Exactly **one** per account.
- Persistent across shard visits.

### Shard Host
The player running the shard listen-server.

### Trust-Based MVP
During a session:
- The shard host is authoritative for gameplay simulation.
- Character persistence is stored by the player client.

> This avoids cross-shard fragmentation while keeping implementation feasible without cloud authority.

---

## AUTHORITATIVE RESPONSIBILITY SPLIT (LOCKED)

### While Connected to a Shard
- **Shard host** is authoritative for:
  - Movement, combat resolution, targeting legality
  - Loot drops and world interactions
  - Housing edits on that shard
  - Any server-side validation gates

### Persistence Ownership
- **Player client** persists:
  - Character save (stats/skills/inventory/equipment)
- **Shard host** persists:
  - Shard world save (village build state, vendors, world seed)

---

## SHARD HOSTING LIFECYCLE

### Start Hosting
When the host starts their shard:
1. Load (or create) a `ShardProfile`
2. If new shard:
   - Generate `WorldSeed` (persisted)
   - Create initial world state
3. Start NGO host (listen-server)
4. Register shard with Lobby via heartbeat

### Stop Hosting
When host stops hosting (quit/crash):
- Lobby listing expires automatically
- Visitors disconnect
- Host must flush shard world state to disk

---

## LOBBY DISCOVERY (SESSION METADATA)

The Lobby lists **active shards only**.

Shard advertisement contains (minimum):
- `ShardSessionId`
- `HostAccountId`
- `ShardName`
- `BuildId`
- `WorldSeed`
- `MaxPlayers`, `CurrentPlayers`

No passwords. Shards are always public.

---

## JOIN FLOW (VISITING)

1. Client selects a shard from Lobby.
2. Client connects (Steam session → NGO connection).
3. Shard host runs **connection approval**:
   - BuildId mismatch → reject
   - Duplicate login (same AccountId already connected) → reject
   - Shard full → reject

4. On accept, shard host requests the joining player’s **Character Snapshot**.
5. Player provides the snapshot to the shard host.
6. Shard host validates snapshot shape and applies server-side rules/caps.
7. Player spawns.

---

## CHARACTER SNAPSHOT CONTRACT (LOCKED)

### What the snapshot represents
A portable, serialized representation of the single character:
- Attributes / vitals
- Skills
- Inventory & equipped items
- Active long-lived progress flags (if any)

### Validation (MVP)
Shard host must validate:
- Required fields exist
- IDs are valid (ItemId, SkillId, SpellId, StatusEffectId)
- Values are within documented caps

If validation fails, shard host must:
- Reject the snapshot and deny entry, **or**
- Sanitize to safe defaults (policy must be consistent)

> Trust-based MVP assumes honest clients; validation is primarily for corruption prevention.

---

## SAVE EVENTS (LOCKED)

Character persistence must be updated on:
- **Leaving a shard normally** (disconnect / return to lobby)
- **Entering a shard** (optional: immediate checkpoint after successful load)
- **Death events** (if death changes inventory/state)
- **Any inventory/equipment change** (recommended: debounce + periodic flush)

Shard persistence must be updated on:
- Housing edits (place/remove)
- Vendor changes
- Periodic world checkpoint
- Host shutdown

---

## DISCONNECTS, CRASHES, AND RECOVERY

### Visitor Disconnect
- Shard host removes the player from the simulation.
- If possible, shard host sends the latest Character Snapshot to the client.

### Client Crash Mid-Session
- Client may lose the last few seconds of character state.
- Recovery rule (MVP):
  - On next launch, the client loads the last locally committed Character Snapshot.

### Host Crash
- Shard goes offline immediately.
- Visitors are disconnected.
- Shard world recovery uses the last committed shard checkpoint.

---

## DIRECTORY LAYOUT (RECOMMENDED)

### Character (Player-Owned)
- `Saves/Accounts/<AccountId>/character.json`

### Shard World (Host-Owned)
- `Saves/Shards/<HostAccountId>/<ShardProfileId>/World/world.json`
- `Saves/Shards/<HostAccountId>/<ShardProfileId>/Vendors/vendors.json` (optional split)
- `Saves/Shards/<HostAccountId>/<ShardProfileId>/Permissions/perms.json` (optional split)

`ShardProfileId` is stable locally so the host resumes the same world.

---

## TRANSPORT RULE (LOCKED)

- All items may be transported between shards **only via the character’s carried inventory**.
- There is no bulk export, mail, remote transfer, or shard-level item pipeline.

---

## SECURITY / TRUST MODEL (MVP)

### What is trusted
- The client-provided Character Snapshot is trusted enough to enable play.

### What is not protected (MVP)
- A malicious client can forge a snapshot.
- A malicious host can grief visitors in-session.

### Future Hardening (NOT LOCKED)
- Cloud-authoritative character persistence
- Snapshot signing / attestation
- Official shards / verified PvP environments

---

## REQUIRED UPDATES TO DOCUMENTS_INDEX.md (PATCH)

Update the entry for `SESSION_AND_PERSISTENCE_MODEL.md` to reflect:
- Steam identity/session join
- Single-character global persistence
- Shard-local world persistence
- No passwords

---

## DESIGN LOCK CONFIRMATION

This document is **authoritative**.

Any change must:
- Increment Version
- Update Last Updated
- Explicitly call out impacts to identity, persistence boundaries, or shard join/leave behavior

