// ============================================================================
// SpellDef.cs
// ----------------------------------------------------------------------------
// ScriptableObject definition for spells.
//
// Aligns with:
// - SPELL_DEF_SCHEMA.md (v1.0)
// - SPELL_ID_CATALOG.md (v1.0)
// ============================================================================

using System;
using UnityEngine;
using UltimateDungeon.Combat;
using UltimateDungeon.StatusEffects;

namespace UltimateDungeon.Spells
{
    /// <summary>
    /// Pure data describing a spell.
    /// </summary>
    [CreateAssetMenu(menuName = "Ultimate Dungeon/Spells/Spell Definition", fileName = "SpellDef_")]
    public class SpellDef : ScriptableObject
    {
        // --------------------------------------------------------------------
        // Identity
        // --------------------------------------------------------------------

        [Header("Identity")]
        [Tooltip("Must match a value in the SpellId enum.")]
        public SpellId spellId;

        [Tooltip("Human-readable name shown in UI (e.g., 'Fireball').")]
        public string displayName;

        [Range(1, 8)]
        [Tooltip("Spell circle (1–8).")]
        public int circle = 1;

        [TextArea(2, 6)]
        [Tooltip("Short description for tooltips / help panels.")]
        public string shortDescription;

        // --------------------------------------------------------------------
        // Requirements
        // --------------------------------------------------------------------

        [Header("Requirements")]
        [Tooltip("Required Magery skill.")]
        public float requiredMagery;

        [Tooltip("Required Evaluating Intelligence skill (0 if unused).")]
        public float requiredEvaluatingIntelligence;

        [Tooltip("Mana cost to cast.")]
        public int manaCost;

        [Tooltip("Reagents consumed on cast.")]
        public ReagentCost[] reagents = Array.Empty<ReagentCost>();

        // --------------------------------------------------------------------
        // Timing
        // --------------------------------------------------------------------

        [Header("Timing")]
        [Tooltip("Base cast time in seconds.")]
        public float baseCastTimeSeconds;

        [Tooltip("Cooldown duration in seconds.")]
        public float cooldownSeconds;

        [Tooltip("When cooldown starts.")]
        public CooldownStartPolicy cooldownStartPolicy = CooldownStartPolicy.OnSuccessOrFizzle;

        // --------------------------------------------------------------------
        // Targeting
        // --------------------------------------------------------------------

        [Header("Targeting")]
        public SpellTargetingMode targetingMode = SpellTargetingMode.SingleTarget;

        [Tooltip("Range in meters (used for targeted spells).")]
        public float rangeMeters = 10f;

        [Tooltip("Area radius in meters (AoE modes).")]
        public float areaRadiusMeters = 3f;

        [Tooltip("If true, requires line of sight.")]
        public bool requiresLineOfSight = true;

        [Tooltip("Filter allowed targets.")]
        public TargetFilter targetFilter = TargetFilter.AnyActor;

        // --------------------------------------------------------------------
        // Interruptibility
        // --------------------------------------------------------------------

        [Header("Interruptibility")]
        public bool interruptedByDamage = true;
        public bool interruptedByMovement = true;
        public bool interruptedByStunOrParalyze = true;

        [Tooltip("Minimum damage required to interrupt (0 = any damage).")]
        public float damageInterruptThreshold;

        // --------------------------------------------------------------------
        // Failure / Fizzle
        // --------------------------------------------------------------------

        [Header("Failure / Fizzle")]
        [Range(0f, 1f)]
        public float baseFizzleChance;

        public bool consumeManaOnFizzle = true;
        public bool consumeReagentsOnFizzle = true;

        // --------------------------------------------------------------------
        // Resolution Payload
        // --------------------------------------------------------------------

        [Header("Payload")]
        public SpellPayloadEntry[] payload = Array.Empty<SpellPayloadEntry>();

        // --------------------------------------------------------------------
        // Validation Helpers
        // --------------------------------------------------------------------

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = spellId.ToString();
            }

            if (circle < 1)
            {
                circle = 1;
            }
            else if (circle > 8)
            {
                circle = 8;
            }
        }
    }

    // ------------------------------------------------------------------------
    // Core Types
    // ------------------------------------------------------------------------

    public enum SpellTargetingMode
    {
        Self = 0,
        SingleTarget = 1,
        GroundTarget = 2,
        AreaAroundTarget = 3,
        AreaAroundCaster = 4
    }

    public enum TargetFilter
    {
        AnyActor = 0,
        FriendlyOnly = 1,
        HostileOnly = 2,
        SelfOnly = 3,
        WorldOnly = 4
    }

    public enum CooldownStartPolicy
    {
        OnSuccessOrFizzle = 0,
        OnAnyEnd = 1
    }

    public enum SpellPayloadType
    {
        DirectDamage = 0,
        Heal = 1,
        ApplyStatus = 2,
        RemoveStatus = 3,
        Dispel = 4,
        Teleport = 5,
        FieldSpawn = 6,
        Summon = 7,
        Reveal = 8,
        Unlock = 9,
        Lock = 10,
        UtilityCustom = 99
    }

    public enum TargetingOverride
    {
        UseSpellTargeting = 0,
        Caster = 1,
        PrimaryTarget = 2,
        AllInArea = 3
    }

    public enum StackRule
    {
        Refresh = 0,
        AddStack = 1,
        Replace = 2,
        IgnoreIfPresent = 3
    }

    public enum RemoveRule
    {
        RemoveAllStacks = 0,
        RemoveOneStack = 1,
        RemoveIfMagnitudeBelow = 2,
        RemoveIfMagnitudeAbove = 3
    }

    public enum DispelStrength
    {
        Lesser = 0,
        Normal = 1,
        Greater = 2
    }

    public enum TargetTag
    {
        Summon = 0,
        Field = 1,
        Buff = 2,
        Debuff = 3
    }

    public enum TeleportMode
    {
        ToGroundPoint = 0,
        ToMarkedLocation = 1,
        RecallHome = 2,
        GateTravel = 3
    }

    public enum FieldType
    {
        FireField = 0,
        PoisonField = 1,
        ParalyzeField = 2,
        EnergyField = 3,
        WallOfStone = 4
    }

    public enum SummonId
    {
        BladeSpirits = 0,
        SummonCreature = 1,
        EnergyVortex = 2,
        AirElemental = 3,
        Daemon = 4,
        EarthElemental = 5,
        FireElemental = 6,
        WaterElemental = 7
    }

    // ------------------------------------------------------------------------
    // Payload Definitions
    // ------------------------------------------------------------------------

    [Serializable]
    public struct ReagentCost
    {
        public ReagentId reagentId;
        public int amount;
    }

    [Serializable]
    public struct SpellPayloadEntry
    {
        public SpellPayloadType type;
        public TargetingOverride targetOverride;

        public DirectDamagePayload directDamage;
        public HealPayload heal;
        public ApplyStatusPayload applyStatus;
        public RemoveStatusPayload removeStatus;
        public DispelPayload dispel;
        public TeleportPayload teleport;
        public FieldSpawnPayload fieldSpawn;
        public SummonPayload summon;
        public RevealPayload reveal;
        public LockPayload lockTarget;
        public UnlockPayload unlock;
    }

    [Serializable]
    public struct DirectDamagePayload
    {
        public DamageType damageType;
        public int minDamage;
        public int maxDamage;
        public float evalIntScaling;
        public float mageryScaling;
        public bool canBeResisted;
    }

    [Serializable]
    public struct HealPayload
    {
        public int minHeal;
        public int maxHeal;
        public float evalIntScaling;
        public float mageryScaling;
    }

    [Serializable]
    public struct ApplyStatusPayload
    {
        public StatusEffectId statusId;
        public int baseDurationSeconds;
        public int baseMagnitude;
        public StackRule stackRule;
        public bool canBeResisted;
        [Range(0f, 1f)]
        public float resistScalar;
    }

    [Serializable]
    public struct RemoveStatusPayload
    {
        public StatusEffectId statusId;
        public RemoveRule removeRule;
    }

    [Serializable]
    public struct DispelPayload
    {
        public DispelStrength strength;
        public TargetTag[] dispellableTags;
    }

    [Serializable]
    public struct TeleportPayload
    {
        public TeleportMode mode;
        public bool blockedByNoTeleportZones;
    }

    [Serializable]
    public struct FieldSpawnPayload
    {
        public FieldType fieldType;
        public float fieldLengthMeters;
        public int fieldDurationSeconds;
        public bool blocksMovement;
        public bool damagesOnTouch;
    }

    [Serializable]
    public struct SummonPayload
    {
        public SummonId summonId;
        public int durationSeconds;
        public bool controllable;
    }

    [Serializable]
    public struct RevealPayload
    {
        public float radiusMeters;
        public int revealDurationSeconds;
    }

    [Serializable]
    public struct UnlockPayload
    {
        public int power;
        public bool worksOnPlayerContainers;
    }

    [Serializable]
    public struct LockPayload
    {
        public int power;
        public bool worksOnPlayerContainers;
    }
}
