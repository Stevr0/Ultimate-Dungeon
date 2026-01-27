# GAME DESIGN DOCUMENT (GDD)
## Ultimate Dungeon

**Version:** 1.0  
**Last Updated:** 2026-01-27  
**Engine:** Unity 6 (URP)  
**Perspective:** Top-Down / Isometric 3D  
**Genre:** Sandbox MMORPG / Dungeon Survival RPG  
**Platform:** PC (initial)  
**Multiplayer:** Online Multiplayer (Server-Authoritative)

---

## 1. High Concept

**Ultimate Dungeon** is a classless, skill-based multiplayer RPG inspired by *Ultima Online*, rebuilt as a modern **top-down 3D Unity game**.

All players begin in a **shared village located inside a massive volcanic crater** at the peak of **Mount Hotnow**. Beneath the village lies a **single, enormous, interconnected dungeon ecosystem**—caves, ruins, catacombs, fungal caverns, lava tunnels, and forgotten civilizations—extending ever downward.

There is **no endgame reset**, no traditional instancing, and no classes. Progression is driven by **skills, risk, items, and player choice**.

The dungeon is the world.

---

## 2. Core Pillars (Design Locks)

These pillars are non-negotiable. Every system must reinforce them.

### 2.1 Classless, Skill-Based Progression
- No classes
- No character levels
- Skills improve through **use**, not XP bars
- Players specialize naturally through behavior

### 2.2 One Persistent World
- Shared dungeon space
- Minimal instancing (technical-only if unavoidable)
- Player actions permanently affect the world

### 2.3 Item-Driven Power
- Items matter more than raw stats
- Randomized properties on gear
- Loss has weight, but is not overly punitive

### 2.4 Player Interdependence
- Crafting, economy, and survival require cooperation
- No player can efficiently master all systems

### 2.5 Knowledge Is Power
- The dungeon is dangerous and opaque
- Maps, rumors, and experience are valuable
- Veterans gain advantage through understanding, not numeric scaling

---

## 3. Setting & World

### 3.1 The Ultimate Dungeon

The **Ultimate Dungeon** is a colossal underground complex beneath Mount Hotnow.

Characteristics:
- Vertically layered
- Horizontally interconnected
- Thematically segmented by depth

Depth does **not** equal linear difficulty:
- Some shallow areas are extremely dangerous
- Some deep areas are relatively safe but resource-scarce

---

### 3.2 Mount Hotnow & Crater Village

All players begin in **Crater Village**, located inside the collapsed caldera at the mountain’s peak.

Village Features:
- Player housing
- NPC shops
- Crafting stations
- Banks and storage
- Taverns and rumor hubs
- Multiple dungeon access points

Village Rules:
- Core zones are safe (no PvP)
- Social and economic hub
- Player-driven economy

---

## 4. Camera & Controls

### 4.1 Perspective
- Top-down / isometric 3D
- Fixed or softly rotatable camera
- Readability prioritized over cinematic presentation

### 4.2 Controls
- Mouse-driven click-to-move
- Contextual interaction system
- Hotbar for items, abilities, and spells
- Low reliance on twitch reflexes

---

## 5. Multiplayer Model

### 5.1 Networking Philosophy
- Server-authoritative gameplay
- Persistent shared world state
- Deterministic resolution where possible

### 5.2 Player Interaction
- Cooperative dungeon delving
- Optional PvP zones
- Player trading
- Player housing and shared spaces

### 5.3 Death & Risk
- Death has consequences
- Partial item loss
- Corpse recovery mechanics
- Risk increases with dungeon depth

---

## 6. Character Progression

### 6.1 Skills (No Classes)

Players improve skills by **using them**.

Skill Categories:
- Combat (Swords, Maces, Archery, Unarmed)
- Magic (Elemental, Necromancy, Rituals)
- Crafting (Smithing, Tailoring, Alchemy)
- Utility (Lockpicking, Stealth, Tracking)
- Survival (Cooking, Foraging, Fishing)

Rules:
- No hard skill cap
- Diminishing returns at high values
- Time and opportunity cost enforce specialization

---

### 6.2 Attributes

Attributes are derived from skills and equipment:
- Strength
- Dexterity
- Intelligence
- Vitality
- Willpower

Attributes are secondary systems, not primary progression drivers.

---

## 7. Combat System

### 7.1 Combat Philosophy
- Tactical and deliberate
- Positioning matters
- Preparation outweighs reflex speed

### 7.2 Combat Types
- Melee
- Ranged
- Magic
- Hybrid builds

### 7.3 Status Effects

Examples:
- Bleed
- Poison
- Fear
- Stun
- Burn
- Curse

Status effects are often more lethal than raw damage and must be respected.

---

## 8. Items & Equipment

### 8.1 Item Design

Every item has:
- Base type
- Quality tier
- Random modifiers
- Durability
- Weight

### 8.2 Random Properties (Examples)
- Damage bonuses
- Life leech
- Resistance modifiers
- Skill bonuses
- On-hit effects

### 8.3 Item Loss
- Items can be lost on death
- Durability decay encourages item circulation
- Crafted gear remains relevant at all stages

---

## 9. Crafting & Economy

### 9.1 Player-Driven Economy
- NPCs provide baseline goods only
- Best equipment is player-crafted
- Resource scarcity increases with depth

### 9.2 Crafting Professions
- Blacksmithing
- Tailoring
- Alchemy
- Enchanting
- Cooking

Crafting quality depends on:
- Skill level
- Material quality
- Environment
- Risk taken to acquire resources

---

## 10. Housing & Social Systems

### 10.1 Player Housing
- Houses located in Crater Village
- Storage
- Crafting bonuses
- Decoration
- Social prestige

### 10.2 Social Systems
- Guilds
- Shared housing
- Organized dungeon expeditions
- Player-run markets

---

## 11. Dungeon Design

### 11.1 Dungeon Structure
- Hand-crafted macro layout
- Procedural micro-variation
- Unlockable shortcuts and connections

### 11.2 Environmental Hazards
- Traps
- Poison gas
- Lava
- Structural collapses
- Darkness and visibility constraints

The dungeon itself is an active threat.

---

## 12. Art Direction

### 12.1 Visual Style
- Stylized realism
- High readability
- Strong silhouettes
- Atmospheric lighting

### 12.2 UI Style
- Minimalist
- Information-dense
- Diegetic where appropriate

---

## 13. Audio Design

- Ambient dungeon soundscapes
- Directional audio cues for danger
- Sparse, tension-driven music
- Strong sound feedback for combat and hazards

---

## 14. Technical Foundations

- Unity 6 (URP)
- Netcode for GameObjects or equivalent
- Modular, state-driven architecture
- ScriptableObject-driven data definitions
- Server-validated player actions

---

## 15. Long-Term Vision

- Expansion into deeper dungeon layers
- New biomes and factions
- Additional skills (never classes)
- Seasonal world events
- Emergent player-driven history

---

## 16. One-Sentence Pitch

**Ultimate Dungeon** is a classless, multiplayer, top-down RPG where every player begins together at the peak of a mountain and must choose how deep into a shared, living dungeon they dare to descend—knowing that power, knowledge, and loss all come from the same place.
