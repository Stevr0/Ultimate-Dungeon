using UnityEngine;
using Unity.Netcode;

namespace UltimateDungeon.Combat
{
    /// <summary>
    /// CombatActorFacade
    /// -----------------
    /// Adapter that exposes a combat-facing surface for any attackable actor.
    ///
    /// Why this exists:
    /// - Keeps Combat Core code working against a small interface (ICombatActor)
    /// - Lets us bolt combat onto Players/Monsters/NPCs without special-casing
    ///
    /// Back-compat note (IMPORTANT):
    /// Some older/legacy scripts (e.g., early Monster AI) may call RequestAttack(...).
    /// The authoritative entry point for player attacks is PlayerCombatController,
    /// but Monsters are server-owned, so a small server-only helper is provided here
    /// to keep older code compiling while we refactor AI to use the newer pipeline.
    /// </summary>
    [DisallowMultipleComponent]
    public class CombatActorFacade : NetworkBehaviour, ICombatActor
    {
        [Header("v0.1 Skeleton Defaults")]
        [SerializeField] private float baseSwingTimeSeconds = 2.0f;


        private NetworkObject _netObject;
        private ActorVitals _actorVitals;


        public NetworkObject NetObject => _netObject != null ? _netObject : (_netObject = GetComponent<NetworkObject>());
        public ulong NetId => NetObject != null ? NetObject.NetworkObjectId : 0;
        public Transform Transform => transform;


        public bool IsAlive
        {
            get
            {
                if (_actorVitals == null)
                    _actorVitals = GetComponent<ActorVitals>();


                if (_actorVitals == null)
                {
                    Debug.LogWarning($"[CombatActorFacade] No ActorVitals on '{name}'. Treating as alive.");
                    return true;
                }


                return _actorVitals.CurrentHP > 0;
            }
        }


        public bool CanAttack
        {
            get
            {
                if (TryGetComponent(out UltimateDungeon.Players.PlayerCombatStatsServer combatStats))
                    return combatStats.GetCanAttack();

                return true; // v0.1 always true (status gates come later via aggregated stats)
            }
        }


        public float GetBaseSwingTimeSeconds()
        {
            if (TryGetComponent(out UltimateDungeon.Players.PlayerCombatStatsServer combatStats))
                return Mathf.Max(0.05f, combatStats.GetSwingTimeSeconds());

            // Defensive floor so we never end up with 0 or negative wait times.
            return Mathf.Max(0.05f, baseSwingTimeSeconds);
        }


        public void ApplyDamageServer(int amount, DamageType damageType, ICombatActor source)
        {
            // Combat is server-authoritative.
            if (!IsServer)
                return;


            if (_actorVitals == null)
                _actorVitals = GetComponent<ActorVitals>();


            if (_actorVitals == null)
            {
                Debug.LogError($"[CombatActorFacade] '{name}' took damage but has no ActorVitals.");
                return;
            }


            _actorVitals.TakeDamage(Mathf.Max(0, amount));
        }


        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _netObject = GetComponent<NetworkObject>();
            _actorVitals = GetComponent<ActorVitals>();
        }


        // --------------------------------------------------------------------------------------
        // BACK-COMPAT API: RequestAttack
        // --------------------------------------------------------------------------------------
        // Many monster AI implementations are server-side and want a single call like:
        // combatActor.RequestAttack(targetCombatActor);
        // or:
        // combatActor.RequestAttack(targetNetId);
        //
        // The modern / preferred flow is:
        // - Players: PlayerCombatController (client intent -> server validation -> AttackLoop)
        // - Monsters: server-only AI sets/updates AttackLoop target after it has decided
        //
        // These helpers simply start/retarget AttackLoop on the SERVER.
        // They intentionally do NOT do full legality validation yet.
        // (Monsters are usually hostile-by-design; we can add Actor legality checks later.)


        /// <summary>
        /// Server-only helper: begin/retarget attacking a target actor.
        /// </summary>
        public void RequestAttack(ICombatActor target)
        {
            if (!IsServer)
                return;


            if (target == null)
                return;


            var loop = GetComponent<AttackLoop>();
            if (loop == null)
            {
                Debug.LogError($"[CombatActorFacade] '{name}' cannot RequestAttack: missing AttackLoop.");
                return;
            }


            loop.StartLoopServer(target);
        }


        /// <summary>
        /// Overload for scripts that pass another CombatActorFacade.
        /// </summary>
        public void RequestAttack(CombatActorFacade targetFacade)
        {
            RequestAttack(targetFacade as ICombatActor);
        }


        /// <summary>
        /// Overload for scripts that store targets as NetworkObjectId.
        /// Resolves the spawned object and starts/retargets the loop.
        /// </summary>
        public void RequestAttack(ulong targetNetId)
        {
            if (!IsServer)
                return;


            if (targetNetId == 0UL)
                return;


            if (NetworkManager.Singleton == null)
                return;


            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetId, out var netObj))
                return;


            var targetCombat = netObj.GetComponent<ICombatActor>();
            if (targetCombat == null)
                return;


            RequestAttack(targetCombat);
        }
    }
}
