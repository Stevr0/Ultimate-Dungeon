// ============================================================================
// PlayerSkillBookNet.cs
// ----------------------------------------------------------------------------
// NGO replication glue for PlayerSkillBook.
//
// Why this exists:
// - PlayerSkillBook is a MonoBehaviour holding authoritative runtime skill state.
// - NGO cannot directly sync Dictionaries.
// - We replicate skills using a NetworkList of a small serializable struct.
//
// What it replicates:
// - SkillId (int)
// - Current value (float)
// - Lock state (+ / - / locked) (byte)
//
// Authority:
// - Server writes.
// - Everyone reads.
//
// Recommended folder:
// Assets/Scripts/Players/Networking/PlayerSkillBookNet.cs
// ============================================================================

using Unity.Netcode;
using UnityEngine;

namespace UltimateDungeon.Players.Networking
{
    /// <summary>
    /// Compact network representation of one skill.
    ///
    /// NOTE:
    /// - We store SkillId as int to avoid enum type coupling in this net struct.
    /// - We store lock state as byte.
    /// - We store value as float (supports 0.1 increments).
    /// </summary>
    public struct SkillNetState : INetworkSerializable, System.IEquatable<SkillNetState>
    {
        public int skillId;
        public float value;
        public byte lockState;

        public SkillNetState(int skillId, float value, byte lockState)
        {
            this.skillId = skillId;
            this.value = value;
            this.lockState = lockState;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref skillId);
            serializer.SerializeValue(ref value);
            serializer.SerializeValue(ref lockState);
        }

        public bool Equals(SkillNetState other)
        {
            // Exact compare is fine here because we only ever write values
            // that are already quantized (0.1 step). If you later allow
            // arbitrary floats, consider an epsilon compare.
            return skillId == other.skillId && value.Equals(other.value) && lockState == other.lockState;
        }

        public override bool Equals(object obj)
        {
            return obj is SkillNetState other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = skillId;
                hash = (hash * 397) ^ value.GetHashCode();
                hash = (hash * 397) ^ lockState.GetHashCode();
                return hash;
            }
        }
    }

    /// <summary>
    /// PlayerSkillBookNet
    /// ------------------
    /// Attach to the same Player prefab as PlayerSkillBook.
    ///
    /// Server:
    /// - Initializes the NetworkList with all skills once.
    /// - Updates entries only when values change.
    ///
    /// Clients:
    /// - Read the NetworkList to drive UI.
    /// - Can subscribe to Skills.OnListChanged.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerSkillBookNet : NetworkBehaviour
    {
        // --------------------------------------------------------------------
        // References
        // --------------------------------------------------------------------

        [Header("References")]
        [Tooltip("Gameplay skill book component. Server reads from this.")]
        [SerializeField] private UltimateDungeon.Players.PlayerSkillBook skillBook;

        // --------------------------------------------------------------------
        // NetworkList (Server Write)
        // --------------------------------------------------------------------

        public NetworkList<SkillNetState> Skills;

        // --------------------------------------------------------------------
        // Local index map (SkillId -> list index)
        // --------------------------------------------------------------------
        // This makes updates O(1) instead of searching the list.

        private readonly System.Collections.Generic.Dictionary<int, int> _indexBySkillId =
            new System.Collections.Generic.Dictionary<int, int>();

        private bool _initializedOnServer;

        private void Awake()
        {
            // NetworkList constructor signature is:
            //   NetworkList(IEnumerable<T> values = null,
            //              NetworkVariableReadPermission readPerm = Everyone,
            //              NetworkVariableWritePermission writePerm = Server)
            //
            // So: pass null for initial values, then set permissions.
            Skills = new NetworkList<SkillNetState>(
                null,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server
            );
        }


        private void Reset()
        {
            if (skillBook == null)
                skillBook = GetComponentInChildren<UltimateDungeon.Players.PlayerSkillBook>(true);

            if (skillBook == null)
                skillBook = GetComponentInParent<UltimateDungeon.Players.PlayerSkillBook>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (skillBook == null)
                skillBook = GetComponentInChildren<UltimateDungeon.Players.PlayerSkillBook>(true);

            if (IsServer)
            {
                InitializeListIfNeeded();
                PushAllSkills(force: true);
            }
        }

        private void Update()
        {
            // Only server writes.
            if (!IsServer)
                return;

            if (skillBook == null)
                return;

            InitializeListIfNeeded();

            // For v1 we poll each frame.
            // Later, you can optimize by pushing updates only when a skill changes.
            PushAllSkills(force: false);
        }

        /// <summary>
        /// Server-side: ensure the NetworkList contains an entry for every skill.
        /// </summary>
        private void InitializeListIfNeeded()
        {
            if (_initializedOnServer)
                return;

            // Build deterministic index mapping.
            _indexBySkillId.Clear();
            Skills.Clear();

            // IMPORTANT:
            // PlayerSkillBook initializes ALL skills from SkillId enum.
            // We mirror that same set here by iterating the enum.
            var values = System.Enum.GetValues(typeof(UltimateDungeon.Skills.SkillId));

            for (int i = 0; i < values.Length; i++)
            {
                var idEnum = (UltimateDungeon.Skills.SkillId)values.GetValue(i);
                int idInt = (int)idEnum;

                // Start with whatever value the book currently has.
                float v = skillBook.GetValue(idEnum);
                byte lockState = (byte)skillBook.GetLockState(idEnum);

                int index = Skills.Count;
                Skills.Add(new SkillNetState(idInt, v, lockState));
                _indexBySkillId[idInt] = index;
            }

            _initializedOnServer = true;
        }

        /// <summary>
        /// Server-side: push all skills into the NetworkList.
        ///
        /// If force=false, only updates entries that changed.
        /// </summary>
        private void PushAllSkills(bool force)
        {
            var values = System.Enum.GetValues(typeof(UltimateDungeon.Skills.SkillId));

            for (int i = 0; i < values.Length; i++)
            {
                var idEnum = (UltimateDungeon.Skills.SkillId)values.GetValue(i);
                int idInt = (int)idEnum;

                if (!_indexBySkillId.TryGetValue(idInt, out int index))
                {
                    // Should not happen once initialized.
                    continue;
                }

                float v = skillBook.GetValue(idEnum);
                byte lockState = (byte)skillBook.GetLockState(idEnum);

                SkillNetState newState = new SkillNetState(idInt, v, lockState);

                if (!force)
                {
                    SkillNetState existing = Skills[index];
                    if (existing.Equals(newState))
                        continue;
                }

                // NetworkList supports index assignment.
                Skills[index] = newState;
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            _initializedOnServer = false;
            _indexBySkillId.Clear();
        }

        // --------------------------------------------------------------------
        // Client helpers
        // --------------------------------------------------------------------

        /// <summary>
        /// Client/UI helper: try get skill state by SkillId.
        /// Uses a linear scan (fine for UI). If you want O(1) on clients,
        /// build a client-side dictionary from Skills.OnListChanged.
        /// </summary>
        public bool TryGetSkillState(UltimateDungeon.Skills.SkillId id, out SkillNetState state)
        {
            int idInt = (int)id;

            for (int i = 0; i < Skills.Count; i++)
            {
                if (Skills[i].skillId == idInt)
                {
                    state = Skills[i];
                    return true;
                }
            }

            state = default;
            return false;
        }
    }
}
