# SESSION_AND_PERSISTENCE_MODEL.md — Ultimate Dungeon (AUTHORITATIVE)

Version: 0.1  
Last Updated: 2026-02-09  
Engine: Unity 6 (URP)  
Networking: Netcode for GameObjects (NGO)  
Authority: Server-authoritative  

---

## PURPOSE

Defines the **minimum viable live-server flow** for friends connecting to a **Windows-hosted dedicated server**, including:

- Connection approval ("login")
- Character identity + creation gate
- Spawn/session lifecycle
- Persistence rules (what is saved, when, and by whom)

This is the **single source of truth** for session/auth/persistence behavior.

---

## SCOPE BOUNDARIES (NO OVERLAP)

Owned elsewhere:
- Combat legality + targeting: `ACTOR_MODEL.md`, `TARGETING_MODEL.md`
- Combat math/execution: `COMBAT_CORE.md`
- Item identity/instances: `ITEMS.md`, `ITEM_DEF_SCHEMA.md`, `ITEM_CATALOG.md`
- Status definitions: `STATUS_EFFECT_CATALOG.md`
- Scene-specific rules: `SCENE_RULE_PROVIDER.md`

This document **does not** define:
- Specific combat math
- Item schema fields
- Spell payload rules

---

## DESIGN GOALS

- **Friends-only server**: simple, reliable, low-ops.
- **Server is source of truth**: clients never author gameplay state.
- **Deterministic ownership**: anything persistent is written by server only.
- **Safe iteration**: allow future upgrade to stronger auth or database without rewriting gameplay.

---

## TERMS

- **Server**: the dedicated authoritative instance (Windows PC initially).
- **Client**: player build.
- **AccountId**: stable identifier for a human user.
- **CharacterId**: stable identifier for a character owned by an account.
- **Session**: a connected client instance from connect → disconnect.

---

## AUTH / LOGIN MODEL (MVP)

### 1) Login Inputs (Client)
Client provides the following to the server at connect time:

- `username` (string)
- `serverPassword` (string) — optional, but recommended for friends-only
- `clientBuildId` (string) — required (see Version Gating)

### 2) Account Identity
**MVP rule:** `AccountId = normalized(username)`.

Normalization rules:
- Trim whitespace
- Lowercase
- Collapse multiple spaces

> **Design lock (MVP):** 1 account per username. No email/Steam/OAuth yet.

### 3) Server Password
If server password is enabled:
- Connection is rejected when password mismatch.

### 4) Version Gating
Server maintains a single string:
- `RequiredClientBuildId`

Rules:
- If `clientBuildId != RequiredClientBuildId` → reject connection.

This prevents "it connects but everything is broken" when you ship a new build.

---

## CHARACTER MODEL (MVP)

### 1) Character Slots
**MVP rule:** 1 character per account.

(We can expand later to multiple characters per account, but persistence format should be written so it can support it.)

### 2) Character Creation Fields (MVP)
Required fields:
- `CharacterName` (string)

Optional fields (future):
- appearance, gender, cosmetics

### 3) Character Name Rules
- Length: 3–16 characters (MVP)
- Allowed characters: letters, numbers, spaces, apostrophe (MVP)
- Must not be all spaces

Uniqueness (MVP):
- Unique **per server** (not just per account)

---

## SESSION FLOW (CONNECT → PLAY)

### Overview
1) Client opens **Connect UI** and enters `username`, `password` (if enabled), `server address`, `port`.
2) Client connects to server.
3) Server performs **connection approval**.
4) On success, server determines if this `AccountId` has a character save.
5) If no character save exists → client enters **Character Creation UI**.
6) Client submits `CharacterName`.
7) Server validates name; on success creates Character + initial state.
8) Server spawns the player actor and begins normal play.

### Connection Approval (Server)
Server MUST reject connection if:
- Password mismatch (when enabled)
- BuildId mismatch
- Username invalid (empty after normalization)
- Account already logged in (MVP anti-dup)

### Duplicate Login Rule (MVP)
If the same `AccountId` is already connected:
- Reject new connection.

(Alternative policy later: kick old session, allow new.)

---

## PERSISTENCE MODEL (MVP)

### 1) Authority
**Design lock:** Only the server writes persistent data.

Clients may REQUEST actions, but do not write saves.

### 2) Storage Type (MVP)
**MVP storage:** JSON files on disk.

Directory layout (recommended):
- `Saves/Accounts/<AccountId>/character.json`
- `Saves/Accounts/<AccountId>/meta.json` (optional)

### 3) What Must Be Saved (MVP)
The server must persist enough state to rejoin after a restart.

Minimum required:

**Identity**
- AccountId
- CharacterId
- CharacterName
- CreatedAt, LastSeenAt

**Location**
- SceneId (or SceneName)
- Position (Vector3)
- Rotation (Yaw or Quaternion)

**Progression**
- Base attributes (STR/DEX/INT) and any permanent advancement if applicable
- Skill values (or skill progression state)
- Currency wallet

**Inventory / Equipment**
- Inventory contents (ItemInstances)
- Equipped slots (ItemInstances)
- Item instance runtime fields that matter (e.g., durability, affixes, blessed/cursed state)

**Statuses (MVP policy)**
- Persistent/long-term statuses MAY be saved.
- Short-lived combat statuses SHOULD NOT be saved unless explicitly marked persistent.

> If a status effect requires persistence, it MUST declare itself "persistent" (owned by the status system), and persistence writes only those.

### 4) What Must NOT Be Saved (MVP)
- Transient combat state (current target, swing timers, cast timers)
- Temporary buffs/debuffs unless flagged persistent
- UI selections

### 5) Save Triggers
Server writes saves on:
- **Disconnect** (always)
- **Periodic autosave** (recommended: every 2–5 minutes)
- **Critical events** (recommended):
  - Item equip/unequip
  - Item moved in/out of inventory
  - Currency change

### 6) Save Atomicity / Safety
MVP file safety rules:
- Write to `character.json.tmp`
- Flush/close
- Replace `character.json`

This reduces corruption on crash.

### 7) Load Rules
On successful connect:
- If save exists: load it and spawn
- If save missing: require character creation
- If save corrupt/unreadable:
  - Move corrupt file aside with timestamp suffix
  - Treat as missing (character creation) OR optionally fallback to last known good backup (future)

---

## INITIAL CHARACTER STATE (MVP)

When a new character is created, server assigns:

- Spawn scene and spawn point (server-owned rule)
- Starter kit (items/currency) as defined by item catalogs/rules

This doc does not define starter items; that belongs in item catalogs/rules.

---

## UI REQUIREMENTS (MVP)

### Connect Screen
- Server Address
- Port
- Username
- Password (optional)
- Button: Connect
- Error label (shows rejection reason)

### Character Creation Screen
- Character Name
- Button: Create
- Error label (invalid/duplicate)

### Character Select Screen
Not required for MVP (because 1 character per account).

---

## SECURITY NOTES (MVP)

This is not "secure" against a determined attacker (no encryption/auth provider). It is acceptable for friends-only hosting.

If/when the game goes public, upgrade path:
- Real auth (Steam/UGS/Auth)
- Transport security/encryption
- Rate limiting and abuse controls

---

## OPEN QUESTIONS (NOT LOCKED)

1) Do we prefer "kick old session, accept new" for duplicate logins?
2) Should character names be unique per server (current) or per account?
3) Which statuses are persistent (we need a flag in status runtime/def)?
4) Spawn policy: fixed town spawn vs last-known position vs safe fallback.

---

## CHANGELOG

- v0.1 (2026-02-09): Initial authoritative session/login/persistence model for Windows-hosted friends server.

