// ============================================================================
// PlayerHotbarAbilityController.cs
// ----------------------------------------------------------------------------
// Authoritative hotbar activation -> spell intent -> payload execution.
// Follows SYSTEM_INTERACTION_MODEL.md (v0.3) hotbar rules.
// ============================================================================

using Unity.Netcode;
using UnityEngine;
using UltimateDungeon.Actors;
using UltimateDungeon.Combat;
using UltimateDungeon.Items;
using UltimateDungeon.Players;
using UltimateDungeon.Progression;
using UltimateDungeon.SceneRules;
using UltimateDungeon.Targeting;

namespace UltimateDungeon.Spells
{
    [DisallowMultipleComponent]
    public sealed class PlayerHotbarAbilityController : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerEquipmentComponent equipment;
        [SerializeField] private PlayerTargeting targeting;
        [SerializeField] private ActorComponent actor;
        [SerializeField] private CombatActorFacade combatActor;
        [SerializeField] private SpellDefCatalog spellCatalog;

        public event System.Action<SpellId> OnCastConfirmed;

        private void Awake()
        {
            if (equipment == null)
                equipment = GetComponent<PlayerEquipmentComponent>();

            if (targeting == null)
                targeting = GetComponent<PlayerTargeting>();

            if (actor == null)
                actor = GetComponent<ActorComponent>();

            if (combatActor == null)
                combatActor = GetComponent<CombatActorFacade>();
        }

        // --------------------------------------------------------------------
        // Client API
        // --------------------------------------------------------------------

        /// <summary>
        /// Called by UI/input when a hotbar slot is activated.
        /// </summary>
        public void RequestCastFromHotbar(int hotbarIndex)
        {
            if (!IsOwner)
                return;

            ulong targetNetId = 0UL;

            if (targeting != null && targeting.CurrentTarget != null)
            {
                var netObj = targeting.CurrentTarget.GetComponent<NetworkObject>();
                if (netObj != null)
                    targetNetId = netObj.NetworkObjectId;
            }

            RequestCastFromHotbarServerRpc(hotbarIndex, targetNetId);
        }

        // --------------------------------------------------------------------
        // Server intent handling
        // --------------------------------------------------------------------

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void RequestCastFromHotbarServerRpc(int hotbarIndex, ulong targetNetId, RpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != OwnerClientId)
                return;

            if (!IsServer)
                return;

            if (spellCatalog == null)
            {
                SendDeniedToOwnerClientRpc((byte)HotbarCastDenyReason.Denied_NoSpellCatalog);
                return;
            }

            if (!SceneRuleRegistry.HasCurrent)
            {
                SendDeniedToOwnerClientRpc((byte)HotbarCastDenyReason.Denied_NoSceneRules);
                return;
            }

            if (actor == null || !actor.IsAlive)
            {
                SendDeniedToOwnerClientRpc((byte)HotbarCastDenyReason.Denied_ActorInvalid);
                return;
            }

            if (equipment == null)
            {
                SendDeniedToOwnerClientRpc((byte)HotbarCastDenyReason.Denied_NoEquipment);
                return;
            }

            if (!TryResolveHotbarSpell(hotbarIndex, actor.State == CombatState.InCombat,
                    out var equipSlot,
                    out var itemInstance,
                    out var itemDef,
                    out var activeGrantSlot,
                    out var spellId,
                    out var spellDef,
                    out var denyReason))
            {
                SendDeniedToOwnerClientRpc((byte)denyReason);
                return;
            }

            CastIntentType intent = GetCastIntent(spellDef);

            if (!TryResolveTargetActor(spellDef, targetNetId, out var targetActor, out var targetDeny))
            {
                SendDeniedToOwnerClientRpc((byte)targetDeny);
                return;
            }

            if (!ValidateTargeting(intent, spellDef, actor, targetActor, out var targetingDeny))
            {
                SendDeniedToOwnerClientRpc((byte)targetingDeny);
                return;
            }

            if (intent == CastIntentType.CastHarmful)
            {
                RefreshCombatForHostileIntent(actor, targetActor);
            }

            ExecuteSpellPayload(intent, spellDef, spellId, targetActor);

            SendCastConfirmedToOwnerClientRpc((int)spellId);
            OnCastConfirmed?.Invoke(spellId);
        }

        private bool TryResolveHotbarSpell(
            int hotbarIndex,
            bool inCombat,
            out EquipSlot equipSlot,
            out ItemInstance instance,
            out ItemDef def,
            out AbilityGrantSlot activeGrantSlot,
            out SpellId spellId,
            out SpellDef spellDef,
            out HotbarCastDenyReason denyReason)
        {
            equipSlot = EquipSlot.None;
            instance = null;
            def = null;
            activeGrantSlot = AbilityGrantSlot.Primary;
            spellId = SpellId.None;
            spellDef = null;
            denyReason = HotbarCastDenyReason.Denied_Unknown;

            if (!TryMapHotbarIndexToEquipSlot(hotbarIndex, out equipSlot))
            {
                denyReason = HotbarCastDenyReason.Denied_InvalidHotbarIndex;
                return false;
            }

            if (!equipment.TryGetEquippedItem(equipSlot, out instance, out def))
            {
                denyReason = HotbarCastDenyReason.Denied_NoEquippedItem;
                return false;
            }

            bool allowUpdate = !inCombat;
            if (!instance.TryResolveActiveGrantSlot(def, allowUpdate, out activeGrantSlot))
            {
                denyReason = inCombat
                    ? HotbarCastDenyReason.Denied_ConfigFrozenInCombat
                    : HotbarCastDenyReason.Denied_NoActiveGrantSlot;
                return false;
            }

            spellId = instance.GetSelectedSpellId(def, activeGrantSlot);
            if (spellId == SpellId.None)
            {
                denyReason = HotbarCastDenyReason.Denied_NoSpellSelected;
                return false;
            }

            if (!spellCatalog.TryGet(spellId, out spellDef) || spellDef == null)
            {
                denyReason = HotbarCastDenyReason.Denied_SpellNotFound;
                return false;
            }

            return true;
        }

        private static bool TryMapHotbarIndexToEquipSlot(int hotbarIndex, out EquipSlot equipSlot)
        {
            equipSlot = EquipSlot.None;

            switch (hotbarIndex)
            {
                case 0:
                    equipSlot = EquipSlot.Bag;
                    return true;
                case 1:
                    equipSlot = EquipSlot.Head;
                    return true;
                case 2:
                    equipSlot = EquipSlot.Neck;
                    return true;
                case 3:
                    equipSlot = EquipSlot.Mainhand;
                    return true;
                case 4:
                    equipSlot = EquipSlot.Chest;
                    return true;
                case 5:
                    equipSlot = EquipSlot.Offhand;
                    return true;
                case 6:
                    equipSlot = EquipSlot.BeltA;
                    return true;
                case 7:
                    equipSlot = EquipSlot.BeltB;
                    return true;
                case 8:
                    equipSlot = EquipSlot.Foot;
                    return true;
                case 9:
                    equipSlot = EquipSlot.Mount;
                    return true;
                default:
                    return false;
            }
        }

        private CastIntentType GetCastIntent(SpellDef spellDef)
        {
            if (spellDef.targetFilter == TargetFilter.HostileOnly)
                return CastIntentType.CastHarmful;

            return CastIntentType.CastBeneficial;
        }

        private bool TryResolveTargetActor(
            SpellDef spellDef,
            ulong targetNetId,
            out ActorComponent targetActor,
            out HotbarCastDenyReason denyReason)
        {
            targetActor = null;
            denyReason = HotbarCastDenyReason.Denied_NoTarget;

            if (spellDef.targetingMode == SpellTargetingMode.Self ||
                spellDef.targetingMode == SpellTargetingMode.AreaAroundCaster)
            {
                targetActor = actor;
                denyReason = HotbarCastDenyReason.None;
                return true;
            }

            if (spellDef.targetingMode == SpellTargetingMode.SingleTarget ||
                spellDef.targetingMode == SpellTargetingMode.AreaAroundTarget)
            {
                if (targetNetId == 0UL)
                {
                    denyReason = HotbarCastDenyReason.Denied_NoTarget;
                    return false;
                }

                if (NetworkManager.Singleton == null ||
                    !NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetId, out var netObj))
                {
                    denyReason = HotbarCastDenyReason.Denied_NoTarget;
                    return false;
                }

                if (!netObj.TryGetComponent(out targetActor) || targetActor == null)
                {
                    denyReason = HotbarCastDenyReason.Denied_NoTarget;
                    return false;
                }

                denyReason = HotbarCastDenyReason.None;
                return true;
            }

            denyReason = HotbarCastDenyReason.Denied_TargetingModeUnsupported;
            return false;
        }

        private bool ValidateTargeting(
            CastIntentType intent,
            SpellDef spellDef,
            ActorComponent caster,
            ActorComponent target,
            out HotbarCastDenyReason denyReason)
        {
            denyReason = HotbarCastDenyReason.None;

            if (caster == null || target == null)
            {
                denyReason = HotbarCastDenyReason.Denied_ActorInvalid;
                return false;
            }

            if (!caster.IsAlive || !target.IsAlive)
            {
                denyReason = HotbarCastDenyReason.Denied_TargetDead;
                return false;
            }

            SceneRuleFlags flags = SceneRuleRegistry.Current.Flags;

            if (intent == CastIntentType.CastHarmful)
            {
                var attackQuery = new AttackQuery(
                    attacker: caster,
                    target: target,
                    sceneFlags: flags,
                    isInRangeAndLoS: true,
                    attackerCanAttack: true,
                    attackerCanSeeTarget: true);

                if (!TargetingResolver.CanAttack(attackQuery, out _))
                {
                    denyReason = HotbarCastDenyReason.Denied_TargetingIllegal;
                    return false;
                }

                return true;
            }

            bool isSelf = ReferenceEquals(caster, target);
            var disposition = TargetingResolver.ResolveDisposition(new DispositionQuery(
                viewer: caster,
                target: target,
                sceneFlags: flags,
                viewerCanSeeTarget: true,
                requireRangeGate: false,
                isInRange: true));

            if (!disposition.IsEligible)
            {
                denyReason = HotbarCastDenyReason.Denied_TargetingIllegal;
                return false;
            }

            switch (spellDef.targetFilter)
            {
                case TargetFilter.SelfOnly:
                    if (!isSelf)
                    {
                        denyReason = HotbarCastDenyReason.Denied_TargetingIllegal;
                        return false;
                    }
                    return true;
                case TargetFilter.FriendlyOnly:
                    if (disposition.Disposition != TargetingDisposition.Friendly &&
                        disposition.Disposition != TargetingDisposition.Self)
                    {
                        denyReason = HotbarCastDenyReason.Denied_TargetingIllegal;
                        return false;
                    }
                    return true;
                case TargetFilter.AnyActor:
                    if (disposition.Disposition == TargetingDisposition.Hostile)
                    {
                        denyReason = HotbarCastDenyReason.Denied_TargetingIllegal;
                        return false;
                    }
                    return true;
                case TargetFilter.HostileOnly:
                    denyReason = HotbarCastDenyReason.Denied_TargetingIllegal;
                    return false;
                case TargetFilter.WorldOnly:
                    denyReason = HotbarCastDenyReason.Denied_TargetingModeUnsupported;
                    return false;
                default:
                    denyReason = HotbarCastDenyReason.Denied_TargetingIllegal;
                    return false;
            }
        }

        private void ExecuteSpellPayload(
            CastIntentType intent,
            SpellDef spellDef,
            SpellId spellId,
            ActorComponent targetActor)
        {
            if (combatActor == null)
                combatActor = GetComponent<CombatActorFacade>();

            if (combatActor == null)
                return;

            var targetCombat = targetActor.GetComponent<CombatActorFacade>();
            var targetVitals = targetActor.GetComponent<ActorVitals>();

            for (int i = 0; i < spellDef.payload.Length; i++)
            {
                var entry = spellDef.payload[i];

                switch (entry.type)
                {
                    case SpellPayloadType.DirectDamage:
                        if (intent != CastIntentType.CastHarmful || targetCombat == null)
                            continue;

                        int damage = RollMagnitude(
                            entry.directDamage.minDamage,
                            entry.directDamage.maxDamage,
                            (int)spellId,
                            i);

                        CombatResolver.ResolveSpellDamage(
                            combatActor,
                            targetCombat,
                            damage,
                            entry.directDamage.damageType);
                        break;
                    case SpellPayloadType.Heal:
                        if (targetVitals == null)
                            continue;

                        int heal = RollMagnitude(
                            entry.heal.minHeal,
                            entry.heal.maxHeal,
                            (int)spellId,
                            i);

                        targetVitals.Heal(heal);
                        break;
                    default:
                        // Payload types beyond damage/heal are not wired in this slice.
                        break;
                }
            }
        }

        private int RollMagnitude(int min, int max, int spellSeed, int payloadIndex)
        {
            if (max < min)
                max = min;

            int seed = DeterministicRng.CombineSeed((int)OwnerClientId, spellSeed, payloadIndex);
            var rng = new DeterministicRng(seed);
            return rng.NextInt(min, max + 1);
        }

        private static void RefreshCombatForHostileIntent(ActorComponent caster, ActorComponent target)
        {
            if (caster == null || target == null)
                return;

            if (caster.TryGetComponent(out CombatStateTracker casterTracker))
                casterTracker.ServerNotifyStartedHostileAction(target.NetworkObjectId);

            if (target.TryGetComponent(out CombatStateTracker targetTracker))
                targetTracker.ServerNotifyReceivedHostileAction(caster.NetworkObjectId);
        }

        // --------------------------------------------------------------------
        // Owner-only UI callbacks
        // --------------------------------------------------------------------

        [ClientRpc]
        private void SendCastConfirmedToOwnerClientRpc(int spellId, ClientRpcParams clientRpcParams = default)
        {
            if (!IsOwner)
                return;

            OnCastConfirmed?.Invoke((SpellId)spellId);
        }

        [ClientRpc]
        private void SendDeniedToOwnerClientRpc(byte denyReason, ClientRpcParams clientRpcParams = default)
        {
            if (!IsOwner)
                return;

            Debug.Log($"[PlayerHotbarAbilityController] Cast denied. Reason={(HotbarCastDenyReason)denyReason}");
        }
    }

    public enum CastIntentType : byte
    {
        CastBeneficial = 0,
        CastHarmful = 1
    }

    public enum HotbarCastDenyReason : byte
    {
        None = 0,
        Denied_Unknown = 1,
        Denied_InvalidHotbarIndex = 2,
        Denied_NoEquippedItem = 3,
        Denied_NoActiveGrantSlot = 4,
        Denied_NoSpellSelected = 5,
        Denied_SpellNotFound = 6,
        Denied_NoTarget = 7,
        Denied_TargetingIllegal = 8,
        Denied_TargetDead = 9,
        Denied_TargetingModeUnsupported = 10,
        Denied_NoSceneRules = 11,
        Denied_ActorInvalid = 12,
        Denied_NoSpellCatalog = 13,
        Denied_NoEquipment = 14,
        Denied_ConfigFrozenInCombat = 15
    }
}
