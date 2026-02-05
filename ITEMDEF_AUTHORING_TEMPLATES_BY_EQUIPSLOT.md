# ITEMDEF_AUTHORING_TEMPLATES_BY_EQUIPSLOT.md — Ultimate Dungeon

Version: 0.1 (PROPOSED)
Last Updated: 2026-02-05
Status: **PROPOSED — NOT LOCKED**

---

## PURPOSE

Provides **copy/paste authoring templates** for `ItemDef` assets, organized by **EquipSlot**.

These templates are designed to:
- Conform to `ITEM_DEF_SCHEMA.md`
- Match the current **EquipSlot → (Primary/Secondary/Utility) spell grouping**
- Keep authoring consistent and fast

> NOTE: These templates are **authoring guidance**, not hard rules.
> The authoritative schema remains `ITEM_DEF_SCHEMA.md`.

---

## GLOBAL AUTHORING RULES (LOCKED BY SCHEMA)

- `ItemFamily` determines allowed equip slot(s).
- `Granted abilities are identity`, not affixes.
- `allowedSpellIds` should be **1–3 entries** per grant slot.
- `SelectedSpellId` lives on **ItemInstance**, and selection changes are **blocked during combat**.

---

## TEMPLATE FORMAT

Each template below is a **single ItemDef** expressed as a structured block you can map directly into your ScriptableObject inspector.

Fields not relevant to the item can be omitted.

---

# EQUIPSLOT: MAINHAND — Weapon ItemDef Template

**Use for:** swords, daggers, maces, bows, etc.

```yaml
ItemDef:
  # Identity
  itemDefId: "mainhand_<weapon_id>"
  displayName: "<Weapon Name>"
  family: Mainhand
  iconAddress: "Icons/<path>"   # optional

  # Inventory
  weight: <float>
  isStackable: false

  # Durability
  usesDurability: true
  durabilityMax: <float>

  # Affix Pools (optional)
  affixPoolRefs: ["Weapon_Common", "Weapon_Rare"]

  # Equipment
  equipmentData:
    isEquippable: true
    equipSlot: Mainhand

  # Granted Abilities (pick 1–3 slots)
  grantedAbilitySlots:
    - slot: Primary
      allowedSpellIds: [MagicArrow, Harm, Fireball, Lightning, EnergyBolt, Flamestrike]
      defaultSpellId: MagicArrow

    - slot: Secondary
      allowedSpellIds: [Weaken, Clumsy, Feeblemind, Curse, Poison]
      defaultSpellId: Weaken

    - slot: Utility
      allowedSpellIds: [Dispel]
      defaultSpellId: Dispel

  # WeaponData (required)
  weaponData:
    handedness: MainHand   # or TwoHanded
    damageType: Physical
    minDamage: <int>
    maxDamage: <int>
    swingSpeedSeconds: <float>
    staminaCostPerSwing: <int>
    requiredCombatSkill: <SkillId>
    rangeMeters: <float>     # optional
    ammoType: None           # None/Arrow/Bolt
```

---

# EQUIPSLOT: OFFHAND — Shield ItemDef Template

**Use for:** shields only.

```yaml
ItemDef:
  itemDefId: "offhand_shield_<id>"
  displayName: "<Shield Name>"
  family: Offhand
  iconAddress: "Icons/<path>"

  weight: <float>
  isStackable: false

  usesDurability: true
  durabilityMax: <float>

  affixPoolRefs: ["Shield_Common", "Shield_Rare"]

  equipmentData:
    isEquippable: true
    equipSlot: Offhand

  grantedAbilitySlots:
    - slot: Primary
      allowedSpellIds: [Protection, ReactiveArmor, MagicReflection]
      defaultSpellId: Protection

    - slot: Secondary
      allowedSpellIds: [Paralyze, MassDispel]
      defaultSpellId: Paralyze

    - slot: Utility
      allowedSpellIds: [ArchProtection, Dispel]
      defaultSpellId: Dispel

  shieldData:
    blockType: <BlockType>
```

---

# EQUIPSLOT: OFFHAND — Ring (Jewelry) ItemDef Template

**Use for:** rings (locked: rings equip into Offhand).

```yaml
ItemDef:
  itemDefId: "offhand_ring_<id>"
  displayName: "<Ring Name>"
  family: Offhand
  iconAddress: "Icons/<path>"

  weight: <float>
  isStackable: false

  usesDurability: true
  durabilityMax: <float>

  affixPoolRefs: ["Jewelry_Common", "Jewelry_Rare"]

  equipmentData:
    isEquippable: true
    equipSlot: Offhand

  grantedAbilitySlots:
    - slot: Primary
      allowedSpellIds: [Protection, ReactiveArmor]
      defaultSpellId: Protection

    - slot: Utility
      allowedSpellIds: [Dispel]
      defaultSpellId: Dispel

  jewelryData:
    slot: Ring
```

---

# EQUIPSLOT: CHEST — Armor ItemDef Template

**Use for:** tunics, chestplates, robes.

```yaml
ItemDef:
  itemDefId: "chest_<armor_id>"
  displayName: "<Chest Name>"
  family: Chest
  iconAddress: "Icons/<path>"

  weight: <float>
  isStackable: false

  usesDurability: true
  durabilityMax: <float>

  affixPoolRefs: ["Armor_Common", "Armor_Rare"]

  equipmentData:
    isEquippable: true
    equipSlot: Chest

  grantedAbilitySlots:
    - slot: Primary
      allowedSpellIds: [Bless, Strength]
      defaultSpellId: Bless

    - slot: Secondary
      allowedSpellIds: [ManaDrain, ManaVampire]
      defaultSpellId: ManaDrain

    - slot: Utility
      allowedSpellIds: [GreaterHeal]
      defaultSpellId: GreaterHeal

  armorData:
    material: Leather
    slot: Torso
    resistPhysical: <int>
    resistFire: <int>
    resistCold: <int>
    resistPoison: <int>
    resistEnergy: <int>
    dexPenalty: <int>
```

---

# EQUIPSLOT: HEAD — Armor ItemDef Template

**Use for:** helms, hats, hoods.

```yaml
ItemDef:
  itemDefId: "head_<id>"
  displayName: "<Head Item Name>"
  family: Head
  iconAddress: "Icons/<path>"

  weight: <float>
  isStackable: false

  usesDurability: true
  durabilityMax: <float>

  affixPoolRefs: ["Armor_Common", "Armor_Rare"]

  equipmentData:
    isEquippable: true
    equipSlot: Head

  grantedAbilitySlots:
    - slot: Primary
      allowedSpellIds: [NightSight]
      defaultSpellId: NightSight

    - slot: Secondary
      allowedSpellIds: [Feeblemind, Clumsy]
      defaultSpellId: Feeblemind

    - slot: Utility
      allowedSpellIds: [Reveal]
      defaultSpellId: Reveal

  armorData:
    material: Cloth
    slot: Head
    resistPhysical: <int>
    resistFire: <int>
    resistCold: <int>
    resistPoison: <int>
    resistEnergy: <int>
    dexPenalty: <int>
```

---

# EQUIPSLOT: NECK — Cape/Cloak ItemDef Template

**Use for:** capes/cloaks (armor-like), also accepts earrings/amulets via Jewelry template below.

```yaml
ItemDef:
  itemDefId: "neck_cape_<id>"
  displayName: "<Cape Name>"
  family: Neck
  iconAddress: "Icons/<path>"

  weight: <float>
  isStackable: false

  usesDurability: true
  durabilityMax: <float>

  affixPoolRefs: ["Cape_Common", "Cape_Rare"]

  equipmentData:
    isEquippable: true
    equipSlot: Neck

  grantedAbilitySlots:
    - slot: Primary
      allowedSpellIds: [Invisibility]
      defaultSpellId: Invisibility

    - slot: Secondary
      allowedSpellIds: [Incognito, Polymorph]
      defaultSpellId: Incognito

    - slot: Utility
      allowedSpellIds: [Teleport]
      defaultSpellId: Teleport

  armorData:
    material: Cloth
    slot: Back
    resistPhysical: <int>
    resistFire: <int>
    resistCold: <int>
    resistPoison: <int>
    resistEnergy: <int>
    dexPenalty: <int>
```

---

# EQUIPSLOT: NECK — Earring/Amulet (Jewelry) ItemDef Template

**Use for:** earrings, amulets (locked: earrings/amulets equip into Neck).

```yaml
ItemDef:
  itemDefId: "neck_jewelry_<id>"
  displayName: "<Jewelry Name>"
  family: Neck
  iconAddress: "Icons/<path>"

  weight: <float>
  isStackable: false

  usesDurability: true
  durabilityMax: <float>

  affixPoolRefs: ["Jewelry_Common", "Jewelry_Rare"]

  equipmentData:
    isEquippable: true
    equipSlot: Neck

  grantedAbilitySlots:
    - slot: Primary
      allowedSpellIds: [Invisibility]
      defaultSpellId: Invisibility

    - slot: Utility
      allowedSpellIds: [Teleport]
      defaultSpellId: Teleport

  jewelryData:
    slot: Earring   # or Amulet
```

---

# EQUIPSLOT: FOOT — Boots ItemDef Template

**Use for:** boots/shoes.

```yaml
ItemDef:
  itemDefId: "foot_<id>"
  displayName: "<Boots Name>"
  family: Foot
  iconAddress: "Icons/<path>"

  weight: <float>
  isStackable: false

  usesDurability: true
  durabilityMax: <float>

  affixPoolRefs: ["Boots_Common", "Boots_Rare"]

  equipmentData:
    isEquippable: true
    equipSlot: Foot

  grantedAbilitySlots:
    - slot: Primary
      allowedSpellIds: [Teleport]
      defaultSpellId: Teleport

    - slot: Secondary
      allowedSpellIds: [Paralyze, Weaken]
      defaultSpellId: Paralyze

    - slot: Utility
      allowedSpellIds: [Agility]
      defaultSpellId: Agility

  armorData:
    material: Leather
    slot: Feet
    resistPhysical: <int>
    resistFire: <int>
    resistCold: <int>
    resistPoison: <int>
    resistEnergy: <int>
    dexPenalty: <int>
```

---

# EQUIPSLOT: BELT A/B — UtilityItem Template (Potion/Food/Bandage)

**Use for:** anything equippable into `BeltA` or `BeltB` (family must be `UtilityItem`).

```yaml
ItemDef:
  itemDefId: "utility_<id>"
  displayName: "<Utility Item Name>"
  family: UtilityItem
  iconAddress: "Icons/<path>"

  weight: <float>
  isStackable: true
  stackMax: <int>

  usesDurability: false

  affixPoolRefs: []

  equipmentData:
    isEquippable: true
    equipSlot: BeltA   # OR BeltB (author one asset per belt variant if needed)

  grantedAbilitySlots:
    - slot: Primary
      allowedSpellIds: [Heal, GreaterHeal, Cure]
      defaultSpellId: <one_of_above>

    - slot: Secondary
      allowedSpellIds: [ArchCure, ArchProtection]
      defaultSpellId: <one_of_above>

    - slot: Utility
      allowedSpellIds: [CreateFood]
      defaultSpellId: CreateFood

  consumableData:
    type: Potion   # Potion/Food/Bandage/Torch/etc.
    isUsable: true
    useTimeSeconds: <float>
```

> Authoring tip: You may create separate ItemDefs like `utility_greater_heal_potion` and `utility_bandage_heal` with narrower `allowedSpellIds`.

---

# EQUIPSLOT: BAG — Backpack (Equipped Container) Template

**Use for:** equipped backpacks (locked: always 48 slots).

```yaml
ItemDef:
  itemDefId: "bag_backpack_<id>"
  displayName: "<Backpack Name>"
  family: Bag
  iconAddress: "Icons/<path>"

  weight: <float>
  isStackable: false

  usesDurability: true
  durabilityMax: <float>

  affixPoolRefs: ["Bag_Common", "Bag_Rare"]

  equipmentData:
    isEquippable: true
    equipSlot: Bag

  grantedAbilitySlots:
    - slot: Primary
      allowedSpellIds: [Telekinesis, Unlock, MagicLock]
      defaultSpellId: Telekinesis

    - slot: Secondary
      allowedSpellIds: [WallOfStone]
      defaultSpellId: WallOfStone

    - slot: Utility
      allowedSpellIds: [CreateFood]
      defaultSpellId: CreateFood

  containerData:
    capacitySlots: 48
    allowNestedContainers: true
    carryWeightBonusKg: <float>
```

---

# EQUIPSLOT: MOUNT — Mount Template

**Use for:** mount items (horses, lizards, etc.).

```yaml
ItemDef:
  itemDefId: "mount_<id>"
  displayName: "<Mount Name>"
  family: Mount
  iconAddress: "Icons/<path>"

  weight: 0
  isStackable: false

  usesDurability: true
  durabilityMax: <float>

  affixPoolRefs: ["Mount_Common", "Mount_Rare"]

  equipmentData:
    isEquippable: true
    equipSlot: Mount

  grantedAbilitySlots:
    - slot: Primary
      allowedSpellIds: [Recall]
      defaultSpellId: Recall

    - slot: Secondary
      allowedSpellIds: [GateTravel]
      defaultSpellId: GateTravel
```

---

## OPTIONAL: “RARE POWER” TEMPLATE (High-End / Late Game)

Use this pattern when you intentionally want an item to grant **big spells** (AoE, fields, summons).

Guidance:
- Keep **1 high-impact spell** as Primary
- Put fields/summons into Utility
- Expect durability risk + mana pressure to be the main limiter

```yaml
grantedAbilitySlots:
  - slot: Primary
    allowedSpellIds: [MeteorSwarm, ChainLightning, Earthquake]
    defaultSpellId: MeteorSwarm

  - slot: Secondary
    allowedSpellIds: [FireField, PoisonField, ParalyzeField, EnergyField]
    defaultSpellId: FireField

  - slot: Utility
    allowedSpellIds: [BladeSpirits, SummonCreature, EnergyVortex]
    defaultSpellId: BladeSpirits
```

---

## NEXT STEPS

1. Use these templates to author 1–2 starter items per slot
2. Decide which spell pools should become **LOCKED whitelists**
3. Add an editor validator that enforces:
   - Family ↔ EquipSlot match
   - AllowedSpellIds count 1–3 per grant slot
   - DefaultSpellId must be inside AllowedSpellIds

