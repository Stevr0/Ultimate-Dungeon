using Unity.Netcode;
using UnityEngine;

namespace UltimateDungeon.Items
{
    /// <summary>
    /// InventoryDebugSeeder
    /// ====================
    ///
    /// Temporary dev tool.
    ///
    /// Purpose:
    /// - When the LOCAL player spawns on the server, seed a couple of items into
    ///   the player's inventory so you can see the UI working.
    ///
    /// IMPORTANT:
    /// - This should NOT ship. Wrap with a scripting define later if desired.
    /// - This is server-only. Clients do not mutate inventory.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InventoryDebugSeeder : NetworkBehaviour
    {
        [Header("Required")]
        [SerializeField] private ItemDefCatalog itemDefCatalog;

        [Header("Seed ItemDefIds")]
        [SerializeField]
        private string[] seedItemDefIds = new string[]
        {
            // Update these to match your ITEM_CATALOG ids.
            "weapon_sword_dagger",
            "weapon_sword_shortsword",
        };

        [Header("Options")]
        [Tooltip("Only seed on the local player (OwnerClientId == LocalClientId).")]
        [SerializeField] private bool onlyLocalPlayer = true;

        private bool _seeded;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!IsServer)
                return;

            if (_seeded)
                return;

            if (onlyLocalPlayer)
            {
                var nm = NetworkManager.Singleton;
                if (nm != null && OwnerClientId != nm.LocalClientId)
                    return;
            }

            if (itemDefCatalog == null)
            {
                Debug.LogError("[InventoryDebugSeeder] Missing ItemDefCatalog reference.");
                return;
            }

            var inv = GetComponent<PlayerInventoryComponent>();
            if (inv == null)
            {
                Debug.LogError("[InventoryDebugSeeder] No PlayerInventoryComponent on this object.");
                return;
            }

            _seeded = true;

            for (int i = 0; i < seedItemDefIds.Length; i++)
            {
                string id = seedItemDefIds[i];
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                if (!itemDefCatalog.TryGet(id, out var def) || def == null)
                {
                    Debug.LogWarning($"[InventoryDebugSeeder] Unknown itemDefId '{id}'. Skipped.");
                    continue;
                }

                // Create an instance and init durability/stack defaults.
                var inst = new ItemInstance(def.itemDefId);
                inst.InitFromDef(def);

                // Add it to inventory.
                var result = inv.ServerTryAdd(inst, out int placedSlot);

                if (result == InventoryOpResult.Success)
                    Debug.Log($"[InventoryDebugSeeder] Seeded '{id}' into slot {placedSlot}.");
                else
                    Debug.LogWarning($"[InventoryDebugSeeder] Failed to seed '{id}'. Result={result}");
            }

            // Force a UI refresh (useful if binder subscribed after we seeded).
            var binder = FindFirstObjectByType<UltimateDungeon.UI.InventoryUIBinder>();
            if (binder != null)
                binder.ForceRefresh();
        }
    }
}
