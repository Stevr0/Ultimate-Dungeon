using UnityEngine;
using Unity.Netcode;

namespace UltimateDungeon.Combat
{
    /// <summary>
    /// CombatDummyTarget
    /// -----------------
    /// A simple networked combat target for testing Combat Core.
    ///
    /// This version adds RequireComponent attributes so you can't forget
    /// to add ActorVitals / CombatActorFacade.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(CombatActorFacade))]
    [RequireComponent(typeof(ActorVitals))]
    public class CombatDummyTarget : NetworkBehaviour
    {
        [Header("Debug")]
        [Tooltip("Print HP to the console whenever it changes.")]
        [SerializeField] private bool logHpChanges = true;

        private ActorVitals _vitals;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _vitals = GetComponent<ActorVitals>();
            if (_vitals == null)
            {
                // If you ever see this, it means the prefab instance in the scene is missing components.
                // Fix: open the prefab, ensure ActorVitals exists, then Apply.
                Debug.LogError($"[CombatDummyTarget] Missing ActorVitals on '{name}'. Add ActorVitals (or adapt facade to your vitals).");
                return;
            }

            if (logHpChanges)
            {
                // Subscribe to NetworkVariable change so both server and clients can see changes.
                _vitals.CurrentHPNet.OnValueChanged += OnHpChanged;

                // Log initial value.
                Debug.Log($"[CombatDummyTarget] '{name}' spawned. HP={_vitals.CurrentHP}");
            }
        }

        public override void OnNetworkDespawn()
        {
            if (_vitals != null && logHpChanges)
            {
                _vitals.CurrentHPNet.OnValueChanged -= OnHpChanged;
            }

            base.OnNetworkDespawn();
        }

        private void OnHpChanged(int previous, int current)
        {
            Debug.Log($"[CombatDummyTarget] '{name}' HP {previous} -> {current}");

            if (current <= 0)
            {
                Debug.Log($"[CombatDummyTarget] '{name}' is DEAD.");
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 1.0f, 0.25f);
        }
#endif
    }
}
