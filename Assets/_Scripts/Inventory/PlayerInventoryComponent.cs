using System;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;

namespace UltimateDungeon.Items
{
    [DisallowMultipleComponent]
    public sealed class PlayerInventoryComponent : NetworkBehaviour
    {
        [Header("Required")]
        [SerializeField] private ItemDefCatalog itemDefCatalog;

        [Header("Inventory")]
        [SerializeField] private int slotCount = InventoryRuntimeModel.DefaultSlotCount;

        [System.NonSerialized] private InventoryRuntimeModel _inventory;

        public InventoryRuntimeModel Inventory => _inventory;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _inventory = new InventoryRuntimeModel(slotCount);

            if (IsServer)
            {
                LoadFromPersistenceServer();
                _inventory.NotifyFullRefresh();
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
                ServerPersistNow();

            base.OnNetworkDespawn();
        }

        public void RequestMoveOrSwap(int fromSlot, int toSlot)
        {
            if (!IsOwner)
                return;

            MoveOrSwapServerRpc(fromSlot, toSlot);
        }

        [ServerRpc]
        private void MoveOrSwapServerRpc(int fromSlot, int toSlot)
        {
            if (!IsServer)
                return;

            var result = ServerTryMoveOrSwap(fromSlot, toSlot);
            if (result == InventoryOpResult.Success)
                ServerPersistNow();
        }

        public InventoryOpResult ServerTryAdd(ItemInstance item, out int placedSlot)
        {
            placedSlot = -1;

            if (!IsServer)
            {
                Debug.LogWarning("[PlayerInventoryComponent] ServerTryAdd called on a client. Ignored.");
                return InventoryOpResult.Failed;
            }

            if (itemDefCatalog == null)
            {
                Debug.LogError("[PlayerInventoryComponent] Missing ItemDefCatalog reference.");
                return InventoryOpResult.UnknownItemDef;
            }

            var result = _inventory.TryAdd(item, itemDefCatalog, out placedSlot);
            if (result == InventoryOpResult.Success)
                ServerPersistNow();

            return result;
        }

        public InventoryOpResult ServerTryPlaceIntoEmptySlot(int toSlot, ItemInstance item)
        {
            if (!IsServer)
            {
                Debug.LogWarning("[PlayerInventoryComponent] ServerTryPlaceIntoEmptySlot called on a client. Ignored.");
                return InventoryOpResult.Failed;
            }

            if (itemDefCatalog == null)
            {
                Debug.LogError("[PlayerInventoryComponent] Missing ItemDefCatalog reference.");
                return InventoryOpResult.UnknownItemDef;
            }

            var result = _inventory.TryPlaceIntoEmptySlot(toSlot, item, itemDefCatalog);
            if (result == InventoryOpResult.Success)
                ServerPersistNow();

            return result;
        }

        public InventoryOpResult ServerTryMoveOrSwap(int fromSlot, int toSlot)
        {
            if (!IsServer)
            {
                Debug.LogWarning("[PlayerInventoryComponent] ServerTryMoveOrSwap called on a client. Ignored.");
                return InventoryOpResult.Failed;
            }

            if (itemDefCatalog == null)
            {
                Debug.LogError("[PlayerInventoryComponent] Missing ItemDefCatalog reference.");
                return InventoryOpResult.UnknownItemDef;
            }

            var result = _inventory.TryMoveOrSwap(fromSlot, toSlot, itemDefCatalog);
            if (result == InventoryOpResult.Success)
                ServerPersistNow();

            return result;
        }

        public InventoryOpResult ServerTrySplit(int fromSlot, int toSlot, int splitAmount)
        {
            if (!IsServer)
            {
                Debug.LogWarning("[PlayerInventoryComponent] ServerTrySplit called on a client. Ignored.");
                return InventoryOpResult.Failed;
            }

            if (itemDefCatalog == null)
            {
                Debug.LogError("[PlayerInventoryComponent] Missing ItemDefCatalog reference.");
                return InventoryOpResult.UnknownItemDef;
            }

            var result = _inventory.TrySplit(fromSlot, toSlot, splitAmount, itemDefCatalog);
            if (result == InventoryOpResult.Success)
                ServerPersistNow();

            return result;
        }

        public InventoryOpResult ServerTryRemoveAt(int slot, out ItemInstance removed)
        {
            removed = null;

            if (!IsServer)
            {
                Debug.LogWarning("[PlayerInventoryComponent] ServerTryRemoveAt called on a client. Ignored.");
                return InventoryOpResult.Failed;
            }

            var result = _inventory.TryRemoveAt(slot, out removed);
            if (result == InventoryOpResult.Success)
                ServerPersistNow();

            return result;
        }

        public void ServerPersistNow()
        {
            if (!IsServer || _inventory == null)
                return;

            string accountId = ResolveAccountId();
            var save = PlayerInventoryPersistence.BuildSaveData(accountId, OwnerClientId, _inventory);
            PlayerInventoryPersistence.Save(save);
        }

        private void LoadFromPersistenceServer()
        {
            string accountId = ResolveAccountId();
            if (!PlayerInventoryPersistence.TryLoad(accountId, out var save) || save == null)
                return;

            _inventory = PlayerInventoryPersistence.BuildRuntimeModel(save, slotCount, itemDefCatalog);
        }

        private string ResolveAccountId()
        {
            // Prefer the server-authenticated account identity (normalized username).
            // This keeps persistence keyed to the user account instead of a transport client id.
            if (TryResolveAuthoritativeAccountId(out string accountId))
                return accountId;

            // Fallback for local/dev sessions where auth wiring is not present yet.
            // Keep this loud so shipping with fallback-only identity is obvious.
            string fallback = $"Client_{OwnerClientId}";
            Debug.LogError($"[PlayerInventoryComponent] Missing authoritative AccountId for owner {OwnerClientId}. " +
                           $"Falling back to '{fallback}'. Wire session/login AccountId on the player identity before shipping.");
            return fallback;
        }

        private bool TryResolveAuthoritativeAccountId(out string accountId)
        {
            accountId = null;

            // Current networking glue exposes player identity via PlayerNetIdentity.
            // We read string IDs reflectively so this component can compile while auth fields
            // are being iterated independently.
            if (!TryGetComponent(out PlayerNetIdentity netIdentity) || netIdentity == null)
                return false;

            const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var candidateNames = new[] { "AccountId", "accountId", "NormalizedAccountId", "username", "UserName", "PlayerName" };

            Type identityType = typeof(PlayerNetIdentity);

            foreach (string name in candidateNames)
            {
                var prop = identityType.GetProperty(name, Flags);
                if (prop != null && prop.PropertyType == typeof(string))
                {
                    accountId = NormalizeAccountId(prop.GetValue(netIdentity) as string);
                    if (!string.IsNullOrWhiteSpace(accountId))
                        return true;
                }
            }

            foreach (string name in candidateNames)
            {
                var field = identityType.GetField(name, Flags);
                if (field != null && field.FieldType == typeof(string))
                {
                    accountId = NormalizeAccountId(field.GetValue(netIdentity) as string);
                    if (!string.IsNullOrWhiteSpace(accountId))
                        return true;
                }
            }

            return false;
        }

        private static string NormalizeAccountId(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            string normalized = raw.Trim().ToLowerInvariant();
            char[] invalidPathChars = System.IO.Path.GetInvalidFileNameChars();
            var buffer = new char[normalized.Length];
            int count = 0;

            for (int i = 0; i < normalized.Length; i++)
            {
                char c = normalized[i];
                if (Array.IndexOf(invalidPathChars, c) >= 0)
                    continue;

                if (char.IsWhiteSpace(c))
                    c = '_';

                buffer[count++] = c;
            }

            if (count == 0)
                return null;

            return new string(buffer, 0, count);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (slotCount < 0) slotCount = 0;
            if (slotCount > 300) slotCount = 300;
        }
#endif
    }
}
