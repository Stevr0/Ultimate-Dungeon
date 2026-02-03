// ============================================================================
// InventoryUIBinder.cs — Ultimate Dungeon
// ----------------------------------------------------------------------------
// Purpose:
// - Bind the local player's server-auth inventory model to the inventory UI.
// - Paint slot icons + stack counts.
// - Provide click hooks for showing item details.
//
// Compile-fix notes:
// - The previous version stored equipment as 'Component' to avoid compile coupling,
//   but then called _playerEquipment.GetEquippedForUI(...), which cannot compile.
//   This version uses reflection to *optionally* call a compatible method if it exists.
//
// IMPORTANT:
// - This binder is UI only. It should never mutate gameplay state.
// - Server authority still lives in inventory/equipment runtime components.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;

namespace UltimateDungeon.UI
{
    using UltimateDungeon.Items;

    [DisallowMultipleComponent]
    public sealed class InventoryUIBinder : MonoBehaviour
    {
        [Header("UI Roots")]
        [Tooltip("Parent transform that contains Slot_00, Slot_01, ... children with InventorySlotViewUI.")]
        [SerializeField] private Transform slotsRoot;

        [Header("Required")]
        [Tooltip("Catalog used to resolve ItemDef data for UI (iconAddress, stackable rules, etc.).")]
        [SerializeField] private ItemDefCatalog itemDefCatalog;

        [SerializeField] private ItemDetailsPanelUI itemDetailsPanel;

        [Header("Binding")]
        [Tooltip("How often to retry binding while waiting for the local player spawn.")]
        [SerializeField] private float bindPollSeconds = 0.25f;

        // Cache of slot index -> view component.
        private readonly Dictionary<int, InventorySlotViewUI> _slotViews = new Dictionary<int, InventorySlotViewUI>();

        // Bound runtime components
        [NonSerialized] private PlayerInventoryComponent _invComponent;
        [NonSerialized] private InventoryRuntimeModel _model;

        // Optional: equipment component (unknown concrete type in this file)
        // We keep it as Component, but NEVER call methods directly on it.
        [NonSerialized] private Component _playerEquipment;

        private float _nextPollTime;

        public static InventoryUIBinder Instance { get; private set; }

        private void Awake()
        {
            // Singleton-style convenience (common for UI roots)
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[InventoryUIBinder] Duplicate Instance - destroying.");
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (slotsRoot == null)
                slotsRoot = transform;

            CacheSlotViews();

            // Item details panel needs the catalog to render names, stats, etc.
            if (itemDetailsPanel != null)
                itemDetailsPanel.BindCatalog(itemDefCatalog);
        }

        private void OnEnable()
        {
            CacheSlotViews();
            TryBind();
        }

        private void OnDisable()
        {
            Unbind();
        }

        private void Update()
        {
            // If we haven't found the local player yet, keep polling.
            if (_invComponent == null && Time.unscaledTime >= _nextPollTime)
            {
                _nextPollTime = Time.unscaledTime + Mathf.Max(0.05f, bindPollSeconds);
                TryBind();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void ForceRefresh() => RefreshAllSlots();

        // --------------------------------------------------------------------
        // Binding
        // --------------------------------------------------------------------

        private void TryBind()
        {
            if (_invComponent != null)
                return;

            var localPlayer = FindLocalPlayerObject();
            if (localPlayer == null)
                return;

            // Inventory component (strongly typed; we do have the class).
            _invComponent = localPlayer.GetComponentInChildren<PlayerInventoryComponent>(includeInactive: true);
            if (_invComponent == null)
                return;

            _model = _invComponent.Inventory;
            if (_model == null)
            {
                _invComponent = null;
                return;
            }

            // Optional: find a component that looks like an Equipment provider.
            // We only use it for UI details.
            _playerEquipment = FindEquipmentComponent(localPlayer);

            _model.OnChanged += HandleInventoryChanged;

            // Initial paint so you see already-seeded items.
            RefreshAllSlots();
        }

        private void Unbind()
        {
            if (_model != null)
                _model.OnChanged -= HandleInventoryChanged;

            _invComponent = null;
            _model = null;
            _playerEquipment = null;
        }

        private GameObject FindLocalPlayerObject()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null || nm.SpawnManager == null)
                return null;

            // Fast path: NGO already knows the local player object.
            var local = nm.SpawnManager.GetLocalPlayerObject();
            if (local != null)
                return local.gameObject;

            // Slow path: scan network objects for a player object owned by this client.
            ulong localClientId = nm.LocalClientId;
            foreach (var no in FindObjectsByType<NetworkObject>(FindObjectsSortMode.None))
            {
                if (no == null) continue;
                if (!no.IsSpawned) continue;
                if (!no.IsPlayerObject) continue;
                if (no.OwnerClientId != localClientId) continue;
                return no.gameObject;
            }

            return null;
        }

        // --------------------------------------------------------------------
        // Slot view caching
        // --------------------------------------------------------------------

        private void CacheSlotViews()
        {
            _slotViews.Clear();

            if (slotsRoot == null)
                return;

            var views = slotsRoot.GetComponentsInChildren<InventorySlotViewUI>(includeInactive: true);
            foreach (var v in views)
            {
                if (v == null) continue;

                int idx = v.SlotIndex;
                if (idx < 0) continue;

                _slotViews[idx] = v;
            }
        }

        // --------------------------------------------------------------------
        // UI refresh
        // --------------------------------------------------------------------

        private void HandleInventoryChanged(InventoryChangeType changeType, int slotIndex)
        {
            if (_model == null)
                return;

            if (changeType == InventoryChangeType.FullRefresh || slotIndex < 0)
            {
                RefreshAllSlots();
                return;
            }

            RefreshSlot(slotIndex);
        }

        private void RefreshAllSlots()
        {
            if (_model == null)
                return;

            foreach (var kvp in _slotViews)
                RefreshSlot(kvp.Key);
        }

        private void RefreshSlot(int slotIndex)
        {
            if (_model == null)
                return;

            if (!_slotViews.TryGetValue(slotIndex, out var view) || view == null)
                return;

            var data = _model.GetSlot(slotIndex);
            if (data.IsEmpty || data.item == null)
            {
                view.SetEmpty();
                return;
            }

            int stack = Math.Max(1, data.item.stackCount);

            // Resolve icon sprite from ItemDef.
            Sprite icon = null;
            if (itemDefCatalog != null)
            {
                var def = TryResolveItemDef(itemDefCatalog, data.item.itemDefId);
                icon = ItemIconResolver.Resolve(def);
            }

            view.SetFilled(icon, stack);
        }

        // --------------------------------------------------------------------
        // ItemDefCatalog compatibility helper
        // --------------------------------------------------------------------
        // We don't assume a specific API shape for ItemDefCatalog.
        // This avoids compile breaks if your catalog uses TryGet/Get/Find/etc.
        // --------------------------------------------------------------------

        private static ItemDef TryResolveItemDef(ItemDefCatalog catalog, string itemDefId)
        {
            if (catalog == null) return null;
            if (string.IsNullOrWhiteSpace(itemDefId)) return null;

            var t = catalog.GetType();

            // 1) Try common method names: TryGet / TryGetById / Get / GetById / Find
            var methodNames = new[] { "TryGet", "TryGetById", "Get", "GetById", "Find", "FindById" };
            foreach (var name in methodNames)
            {
                var m = t.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (m == null) continue;

                var pars = m.GetParameters();

                // Pattern A: ItemDef Get(string id)
                if (pars.Length == 1 && pars[0].ParameterType == typeof(string) && m.ReturnType == typeof(ItemDef))
                {
                    try { return (ItemDef)m.Invoke(catalog, new object[] { itemDefId }); }
                    catch { /* ignore */ }
                }

                // Pattern B: bool TryGet(string id, out ItemDef def)
                if (pars.Length == 2 && pars[0].ParameterType == typeof(string) && pars[1].IsOut)
                {
                    if (pars[1].ParameterType == typeof(ItemDef).MakeByRefType() && m.ReturnType == typeof(bool))
                    {
                        object[] args = new object[] { itemDefId, null };
                        try
                        {
                            bool ok = (bool)m.Invoke(catalog, args);
                            return ok ? (ItemDef)args[1] : null;
                        }
                        catch { /* ignore */ }
                    }
                }
            }

            // 2) Try common field/property names that may hold a dictionary
            var dictCandidates = new[] { "_byId", "byId", "defs", "DefById", "Items", "items" };
            foreach (var name in dictCandidates)
            {
                var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (f != null)
                {
                    var value = f.GetValue(catalog);
                    if (value is IDictionary<string, ItemDef> dict)
                        return dict.TryGetValue(itemDefId, out var def) ? def : null;
                }

                var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (p != null)
                {
                    var value = p.GetValue(catalog);
                    if (value is IDictionary<string, ItemDef> dict)
                        return dict.TryGetValue(itemDefId, out var def) ? def : null;
                }
            }

            return null;
        }

        // --------------------------------------------------------------------
        // Item details hooks (called by UI elements)
        // --------------------------------------------------------------------

        public void ShowItemDetailsFromInventory(int slotIndex)
        {
            if (_invComponent == null || itemDetailsPanel == null)
                return;

            var slot = _invComponent.Inventory.GetSlot(slotIndex);
            if (slot.IsEmpty || slot.item == null)
                return;

            itemDetailsPanel.ShowFromInventory(slot.item);
        }

        public void ShowItemDetailsFromEquipment(EquipmentSlotId slotId)
        {
            if (itemDetailsPanel == null)
                return;

            // If you don't have equipment wired yet, just no-op safely.
            if (_playerEquipment == null)
            {
                Debug.LogWarning("[InventoryUIBinder] No equipment component found; cannot show equipment details.");
                return;
            }

            // Try to call GetEquippedForUI(EquipmentSlotId) reflectively.
            // This prevents compile breaks if you later rename/move the equipment class.
            if (!TryGetEquippedForUI(_playerEquipment, slotId, out string equippedItemDefId))
                return;

            // Resolve ItemDef and show.
            var def = TryResolveItemDef(itemDefCatalog, equippedItemDefId);
            if (def == null)
                return;

            itemDetailsPanel.ShowFromEquipment(def);
        }

        // --------------------------------------------------------------------
        // Equipment reflection helpers
        // --------------------------------------------------------------------

        /// <summary>
        /// Attempts to find a component on the local player that looks like it can provide
        /// equipment info for UI.
        ///
        /// We detect it by looking for a public instance method named "GetEquippedForUI".
        /// </summary>
        private static Component FindEquipmentComponent(GameObject localPlayer)
        {
            if (localPlayer == null) return null;

            // Search children too, because equipment might live under PlayerCore.
            var comps = localPlayer.GetComponentsInChildren<Component>(includeInactive: true);
            foreach (var c in comps)
            {
                if (c == null) continue;

                var m = c.GetType().GetMethod(
                    "GetEquippedForUI",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (m == null) continue;

                // We found something that *claims* it can provide equipped items.
                return c;
            }

            return null;
        }

        /// <summary>
        /// Calls equipment.GetEquippedForUI(slotId) via reflection and tries to read an itemDefId.
        ///
        /// Supported return shapes (any of these):
        /// - A struct/class with:
        ///     - bool IsEmpty {get;}
        ///     - string itemDefId {get;}
        /// - A struct/class with fields:
        ///     - bool IsEmpty
        ///     - string itemDefId
        ///
        /// If your equipment code returns a different shape, we can adapt this helper.
        /// </summary>
        private static bool TryGetEquippedForUI(Component equipment, EquipmentSlotId slotId, out string itemDefId)
        {
            itemDefId = null;
            if (equipment == null) return false;

            var t = equipment.GetType();
            var m = t.GetMethod("GetEquippedForUI", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (m == null) return false;

            try
            {
                object result = m.Invoke(equipment, new object[] { slotId });
                if (result == null) return false;

                // 1) Check IsEmpty
                if (TryReadBool(result, "IsEmpty", out bool isEmpty) && isEmpty)
                    return false;

                // 2) Read itemDefId
                if (TryReadString(result, "itemDefId", out string id) && !string.IsNullOrWhiteSpace(id))
                {
                    itemDefId = id;
                    return true;
                }

                // Some implementations might name it ItemDefId.
                if (TryReadString(result, "ItemDefId", out id) && !string.IsNullOrWhiteSpace(id))
                {
                    itemDefId = id;
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[InventoryUIBinder] Failed to call GetEquippedForUI via reflection: {e.Message}");
            }

            return false;
        }

        private static bool TryReadBool(object obj, string name, out bool value)
        {
            value = false;
            if (obj == null) return false;

            var t = obj.GetType();

            // Property first
            var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null && p.PropertyType == typeof(bool))
            {
                value = (bool)p.GetValue(obj);
                return true;
            }

            // Field fallback
            var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null && f.FieldType == typeof(bool))
            {
                value = (bool)f.GetValue(obj);
                return true;
            }

            return false;
        }

        private static bool TryReadString(object obj, string name, out string value)
        {
            value = null;
            if (obj == null) return false;

            var t = obj.GetType();

            // Property first
            var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null && p.PropertyType == typeof(string))
            {
                value = (string)p.GetValue(obj);
                return true;
            }

            // Field fallback
            var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null && f.FieldType == typeof(string))
            {
                value = (string)f.GetValue(obj);
                return true;
            }

            return false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            bindPollSeconds = Mathf.Max(0.05f, bindPollSeconds);
        }
#endif
    }
}

// ============================================================================
// PATCH NOTES FOR OTHER ERRORS
// ----------------------------------------------------------------------------
// You also reported:
//   EquipmentSlotViewUI.cs(98,17):   CS0103: The name 'IsEmpty' does not exist
//   InventorySlotViewUI.cs(160,17):  CS0103: The name 'IsEmpty' does not exist
//
// That means those classes are calling "IsEmpty" but they don't define it.
//
// Quick, safe fix in BOTH classes:
// - Add a helper property named IsEmpty in the class that matches what the code expects.
// - Implement it using your existing UI state (icon sprite and/or stack count).
//
// Example implementation (paste inside each class):
//
//     // True when this slot has no item displayed.
//     // We treat a null sprite as empty.
//     private bool IsEmpty
//     {
//         get
//         {
//             // Replace _iconImage with your actual Image field name.
//             if (_iconImage == null) return true;
//             return _iconImage.sprite == null;
//         }
//     }
//
// If your slot view does not keep a reference to the icon Image, an alternate approach:
// - Track a private bool _isEmpt