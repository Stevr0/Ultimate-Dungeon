// ============================================================================
// Ultimate Dungeon — Hotbar UI v0.1 (UGUI)
// ----------------------------------------------------------------------------
// Spike Goal:
//   Provide a simple 10-slot hotbar UI that responds to keys 1-0.
//
// Success Criteria:
//   - 10 slots visible.
//   - Pressing 1..0 triggers the matching slot.
//   - Slots can show an icon + a key label.
//   - Emits an event (slotIndex 0..9) so gameplay systems can hook in later.
//
// Delete When:
//   Replaced by a full Ability/Item activation system (AbilityDefs + Hotbar binding).
//
// Dependencies / Assumptions:
//   - Uses Unity UGUI (Canvas + Images + Buttons).
//   - Uses TextMeshPro for slot key labels (TMP_Text).
//   - Supports BOTH Input backends:
//       ENABLE_INPUT_SYSTEM -> Keyboard.current
//       otherwise -> Input.GetKeyDown
//
// File Layout:
//   1) HotbarUI.cs
//   2) HotbarSlotUI.cs
//   3) HotbarInputRouter.cs
//
// Recommended folders:
//   Assets/_Scripts/UI/Hotbar/HotbarUI.cs
//   Assets/_Scripts/UI/Hotbar/HotbarSlotUI.cs
//   Assets/_Scripts/UI/Hotbar/HotbarInputRouter.cs
// ============================================================================

// ============================================================================
// 1) HotbarUI.cs
// ============================================================================

using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UltimateDungeon.UI.Hotbar
{
    /// <summary>
    /// HotbarUI
    /// --------
    /// Scene-level controller for a 10-slot hotbar.
    ///
    /// Responsibilities:
    /// - Holds references to 10 slot UI views
    /// - Receives input events (slot pressed) and dispatches activation events
    /// - Provides simple API to set slot icons (later: bind items/abilities)
    ///
    /// Networking:
    /// - This is UI only. It does NOT perform server actions.
    /// - Later, a HotbarActionDispatcher will translate a slot press into
    ///   a server-validated "UseAbility" or "UseItem" request.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HotbarUI : MonoBehaviour
    {
        public const int SlotCount = 10;

        [Header("Slots (size must be 10)")]
        [Tooltip("Assign 10 slot views in order: 1,2,3,4,5,6,7,8,9,0")]
        [SerializeField] private HotbarSlotUI[] slots = new HotbarSlotUI[SlotCount];

        /// <summary>
        /// Fired when the local player activates a hotbar slot.
        /// slotIndex is 0..9, where 0 maps to key '1' and 9 maps to key '0'.
        /// </summary>
        public event Action<int> SlotActivated;

        private void Awake()
        {
            // Defensive validation so setup issues are obvious.
            if (slots == null || slots.Length != SlotCount)
            {
                Debug.LogError($"[HotbarUI] Slots array must have exactly {SlotCount} entries.");
                return;
            }

            // Wire each slot's click handler.
            for (int i = 0; i < slots.Length; i++)
            {
                int capturedIndex = i; // capture loop var

                if (slots[i] == null)
                {
                    Debug.LogError($"[HotbarUI] Slot {i} is null. Assign all 10 slot references.");
                    continue;
                }

                slots[i].SetSlotIndex(capturedIndex);
                slots[i].Clicked -= HandleSlotClicked; // avoid double-subscribe
                slots[i].Clicked += HandleSlotClicked;

                // Set default key label (1..9,0)
                slots[i].SetKeyLabel(KeyLabelForIndex(capturedIndex));
            }
        }

        private void OnDestroy()
        {
            // Clean up delegates.
            if (slots == null) return;

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) continue;
                slots[i].Clicked -= HandleSlotClicked;
            }
        }

        /// <summary>
        /// Called by HotbarInputRouter or by clicking a slot.
        /// </summary>
        public void ActivateSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SlotCount)
                return;

            // Flash / highlight for immediate feedback.
            if (slots != null && slotIndex < slots.Length && slots[slotIndex] != null)
                slots[slotIndex].Pulse();

            SlotActivated?.Invoke(slotIndex);
        }

        /// <summary>
        /// Set an icon for a given slot.
        /// (Later this will come from ItemDef/AbilityDef.)
        /// </summary>
        public void SetSlotIcon(int slotIndex, Sprite icon)
        {
            if (slots == null || slotIndex < 0 || slotIndex >= slots.Length)
                return;

            slots[slotIndex].SetIcon(icon);
        }

        /// <summary>
        /// Clear a slot (no icon).
        /// </summary>
        public void ClearSlot(int slotIndex)
        {
            if (slots == null || slotIndex < 0 || slotIndex >= slots.Length)
                return;

            slots[slotIndex].SetIcon(null);
        }

        private void HandleSlotClicked(int slotIndex)
        {
            ActivateSlot(slotIndex);
        }

        /// <summary>
        /// Maps 0..9 to key label: 1..9,0
        /// </summary>
        public static string KeyLabelForIndex(int slotIndex)
        {
            // index 0 -> "1" ... index 8 -> "9" ... index 9 -> "0"
            if (slotIndex >= 0 && slotIndex <= 8)
                return (slotIndex + 1).ToString();

            if (slotIndex == 9)
                return "0";

            return "?";
        }

        [ContextMenu("Auto-Find Slots In Children")]
        private void AutoFindSlots()
        {
            // Convenience: find slots in children order.
            // WARNING: This uses hierarchy order. You still should double-check
            // it matches 1..0 ordering.
            slots = GetComponentsInChildren<HotbarSlotUI>(true);
            Debug.Log($"[HotbarUI] AutoFindSlots found {slots?.Length ?? 0} slot views.");
        }
    }
}
