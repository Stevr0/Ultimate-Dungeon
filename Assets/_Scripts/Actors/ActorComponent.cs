// ActorComponent.cs
// Ultimate Dungeon
//
// Purpose (ACTOR_MODEL.md):
// - The universal runtime identity surface for anything that can be targeted / interacted with / fought.
// - Present on: Player, Monster, NPC, Vendor, Pet, Summon, Object (chest/door/etc.).
// - Exposes: ActorType, FactionId, CombatState, alive/dead, and (optional) controller inheritance.
//
// Design locks (enforced here):
// - Server authoritative: server owns mutable actor state (combat state, PvP flags, etc.).
// - CombatState is server-owned and replicated for UI. Clients display only replicated state.
// - Combat/targeting systems must NOT special-case Players vs Monsters outside of Actor rules.
//
// Notes:
// - This script focuses on identity + authoritative state exposure.
// - It does NOT implement hostility rules (FactionService) or targeting legality (TargetingResolver).

using System;
using Unity.Netcode;
using UnityEngine;

namespace UltimateDungeon.Actors
{
    /// <summary>
    /// ActorType (LOCKED)
    /// Owned by ACTOR_MODEL.md.
    /// If you already have this enum elsewhere, delete this copy and reference the existing one.
    /// </summary>
    public enum ActorType : byte
    {
        Player = 0,
        Monster = 1,
        NPC = 2,
        Vendor = 3,
        Pet = 4,
        Summon = 5,
        Object = 6,
    }

    /// <summary>
    /// FactionId (LOCKED MODEL)
    /// ACTOR_MODEL.md describes faction IDs as a data-driven concept.
    /// 
    /// Implementation choice (v1): start with a small enum for determinism.
    /// Later you can replace this with a data-driven ID (string/Guid) + registry.
    /// </summary>
    public enum FactionId : ushort
    {
        Neutral = 0,
        Players = 1,
        Monsters = 2,
        Village = 3,
        Guards = 4,
    }

    /// <summary>
    /// CombatState (LOCKED semantics)
    /// Owned by ACTOR_MODEL.md.
    /// </summary>
    public enum CombatState : byte
    {
        Peaceful = 0,
        Engaged = 1,
        InCombat = 2,
        Dead = 3,
    }

    /// <summary>
    /// ActorComponent
    /// --------------
    /// Attach this to every Actor prefab.
    ///
    /// It is safe for clients to READ these values.
    /// Only the server may WRITE to mutable values.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    public sealed class ActorComponent : NetworkBehaviour
    {
        // ---------------------------
        // Inspector-authored identity
        // ---------------------------
        [Header("Identity (Authored on Prefab)")]
        [SerializeField] private ActorType _actorType = ActorType.NPC;
        [SerializeField] private FactionId _factionId = FactionId.Neutral;

        [Header("Controller Inheritance (Pets / Summons)")]
        [Tooltip("If set (non-zero) the actor inherits some legality rules from its controller (owner).")]
        [SerializeField] private bool _usesControllerInheritance = false;

        [SerializeField, Tooltip("DEBUG ONLY – replicated combat state")]
        private CombatState _debugCombatState;

        // ---------------------------
        // Replicated state (server-owned)
        // ---------------------------

        // ActorType + FactionId are replicated mainly for client UI (rings/tints/inspect).
        // In v1 we keep them server-written (so the server can correct prefab mistakes).
        private readonly NetworkVariable<byte> _netActorType = new(
            (byte)ActorType.NPC,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<ushort> _netFactionId = new(
            (ushort)FactionId.Neutral,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<byte> _netCombatState = new(
            (byte)CombatState.Peaceful,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        // Controller network object id for Pet/Summon inheritance.
        // 0 means "no controller".
        private readonly NetworkVariable<ulong> _netControllerNetworkId = new(
            0UL,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        // PvP flags (minimum viable v1) - only meaningful for Players.
        // Kept here so legality code can read it without needing a Player-only component.
        private readonly NetworkVariable<bool> _netPvpEnabled = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<bool> _netIsCriminal = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<bool> _netIsMurderer = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        // ---------------------------
        // Public read API
        // ---------------------------

        public ActorType Type => (ActorType)_netActorType.Value;
        public FactionId Faction => (FactionId)_netFactionId.Value;
        public CombatState State => (CombatState)_netCombatState.Value;
        public bool IsAlive => State != CombatState.Dead;

        /// <summary>
        /// Returns 0 if there is no controller.
        /// </summary>
        public ulong ControllerNetworkObjectId => _netControllerNetworkId.Value;

        public bool UsesControllerInheritance => _usesControllerInheritance;

        // PvP flags (meaningful when Type == Player)
        public bool PvpEnabled => _netPvpEnabled.Value;
        public bool IsCriminal => _netIsCriminal.Value;
        public bool IsMurderer => _netIsMurderer.Value;

        // ---------------------------
        // Events (useful for UI/debug)
        // ---------------------------

        public event Action<CombatState, CombatState> CombatStateChanged;
        public event Action<FactionId, FactionId> FactionChanged;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // Hook events for local consumers.
            _netCombatState.OnValueChanged += HandleCombatStateChanged;
            _netFactionId.OnValueChanged += HandleFactionChanged;

            if (IsServer)
            {
                // On the server, initialize replicated identity from prefab-authored fields.
                // This allows the server to be authoritative even if a client has a mismatched prefab.
                _netActorType.Value = (byte)_actorType;
                _netFactionId.Value = (ushort)_factionId;

                // Default combat state: alive and peaceful.
                // Even in Dungeon, you start Idle until aggression.
                _netCombatState.Value = (byte)CombatState.Peaceful;

                // Controller inheritance only becomes meaningful if someone sets the controller id.
                if (!_usesControllerInheritance)
                {
                    _netControllerNetworkId.Value = 0UL;
                }

                // PvP flags default (v1 recommendation: no consensual toggle yet)
                // If you later add a player toggle, set this from PlayerCore.
                _netPvpEnabled.Value = false;
                _netIsCriminal.Value = false;
                _netIsMurderer.Value = false;
            }
        }

        public override void OnNetworkDespawn()
        {
            _netCombatState.OnValueChanged -= HandleCombatStateChanged;
            _netFactionId.OnValueChanged -= HandleFactionChanged;

            base.OnNetworkDespawn();
        }

        // ---------------------------
        // Server write API
        // ---------------------------

        /// <summary>
        /// Server-only: set combat state.
        /// CombatStateTracker should be the only system that calls this in normal gameplay.
        /// </summary>
        public void ServerSetCombatState(CombatState newState)
        {
            if (!IsServer)
            {
                Debug.LogWarning("[ActorComponent] ServerSetCombatState called on a non-server instance.");
                return;
            }

            // Minimal guard: do not resurrect by accident.
            if ((CombatState)_netCombatState.Value == CombatState.Dead && newState != CombatState.Dead)
            {
                Debug.LogWarning("[ActorComponent] Refusing to set combat state away from Dead. Use ServerRevive().");
                return;
            }

            _netCombatState.Value = (byte)newState;

        }

        /// <summary>
        /// Server-only: mark the actor as dead.
        /// Combat Core should call this only when HP hits 0.
        /// </summary>
        public void ServerKill()
        {
            if (!IsServer)
            {
                Debug.LogWarning("[ActorComponent] ServerKill called on a non-server instance.");
                return;
            }

            _netCombatState.Value = (byte)CombatState.Dead;
        }

        /// <summary>
        /// Server-only: revive the actor.
        /// This is out-of-scope for v1 combat slice, but included for completeness.
        /// </summary>
        public void ServerRevive()
        {
            if (!IsServer)
            {
                Debug.LogWarning("[ActorComponent] ServerRevive called on a non-server instance.");
                return;
            }

            _netCombatState.Value = (byte)CombatState.Peaceful;
            _netIsCriminal.Value = false;
            _netIsMurderer.Value = false;
        }

        /// <summary>
        /// Server-only: set the controller object for Pets/Summons.
        /// Pass 0 to clear.
        /// </summary>
        public void ServerSetController(ulong controllerNetworkObjectId)
        {
            if (!IsServer)
            {
                Debug.LogWarning("[ActorComponent] ServerSetController called on a non-server instance.");
                return;
            }

            if (!_usesControllerInheritance)
            {
                // If this actor isn't supposed to inherit, keep it deterministic.
                _netControllerNetworkId.Value = 0UL;
                return;
            }

            _netControllerNetworkId.Value = controllerNetworkObjectId;
        }

        /// <summary>
        /// Server-only: set PvP flags.
        /// These should be driven by your eventual crime/flagging system.
        /// </summary>
        public void ServerSetPvpFlags(bool pvpEnabled, bool isCriminal, bool isMurderer)
        {
            if (!IsServer)
            {
                Debug.LogWarning("[ActorComponent] ServerSetPvpFlags called on a non-server instance.");
                return;
            }

            // Only meaningful for players, but we keep it permissive so you can reuse for testing.
            _netPvpEnabled.Value = pvpEnabled;
            _netIsCriminal.Value = isCriminal;
            _netIsMurderer.Value = isMurderer;
        }

        /// <summary>
        /// Server-only: change faction.
        /// Useful for temporary charm/convert mechanics later.
        /// </summary>
        public void ServerSetFaction(FactionId newFaction)
        {
            if (!IsServer)
            {
                Debug.LogWarning("[ActorComponent] ServerSetFaction called on a non-server instance.");
                return;
            }

            _netFactionId.Value = (ushort)newFaction;
        }

        // ---------------------------
        // NetVar change handlers
        // ---------------------------

        private void HandleCombatStateChanged(byte previous, byte current)
        {
            CombatStateChanged?.Invoke((CombatState)previous, (CombatState)current);
            _debugCombatState = (CombatState)current;
        }

        private void HandleFactionChanged(ushort previous, ushort current)
        {
            FactionChanged?.Invoke((FactionId)previous, (FactionId)current);
        }

        // ---------------------------
        // Debug helpers
        // ---------------------------

        private void OnValidate()
        {
            // Keep authored defaults sensible.
            // (No runtime writes here; this is editor-only.)
            if (_actorType == ActorType.Player)
            {
                // Players should not default to Neutral.
                if (_factionId == FactionId.Neutral)
                    _factionId = FactionId.Players;
            }

            if (_actorType == ActorType.Monster)
            {
                if (_factionId == FactionId.Neutral)
                    _factionId = FactionId.Monsters;
            }
        }

        public override string ToString()
        {
            return $"Actor(Type={Type}, Faction={Faction}, State={State}, NetId={NetworkObjectId})";
        }
    }
}
