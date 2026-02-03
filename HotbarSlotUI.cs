
// ============================================================================
// 2) HotbarSlotUI.cs
// ============================================================================

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UltimateDungeon.UI.Hotbar
{
    /// <summary>
    /// HotbarSlotUI
    /// -----------
    /// Visual representation of a single hotbar slot.
    ///
    /// This script is intentionally dumb:
    /// - Displays icon
    /// - Displays key label
    /// - Emits Clicked event
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HotbarSlotUI : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Button button;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text keyLabel;

        [Header("Pulse Feedback")]
        [Tooltip("Optional: a highlight image we can briefly show when activated.")]
        [SerializeField] private Image pulseOverlay;

        [Tooltip("How long the pulse overlay stays visible.")]
        [SerializeField] private float pulseSeconds = 0.08f;

        private int _slotIndex;
        private float _pulseUntilTime;

        public event Action<int> Clicked;

        private void Awake()
        {
            // Auto-find typical setup if references were not assigned.
            if (button == null) button = GetComponentInChildren<Button>(true);
            if (iconImage == null) iconImage = GetComponentInChildren<Image>(true);

            if (button == null)
                Debug.LogError("[HotbarSlotUI] Missing Button reference.");

            if (button != null)
            {
                button.onClick.RemoveListener(OnClicked);
                button.onClick.AddListener(OnClicked);
            }

            // Pulse overlay starts hidden.
            if (pulseOverlay != null)
                pulseOverlay.enabled = false;
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(OnClicked);
        }

        private void Update()
        {
            // Tiny pulse feedback without coroutines.
            if (pulseOverlay == null)
                return;

            if (pulseOverlay.enabled && Time.unscaledTime >= _pulseUntilTime)
                pulseOverlay.enabled = false;
        }

        public void SetSlotIndex(int index)
        {
            _slotIndex = index;
        }

        public void SetIcon(Sprite icon)
        {
            if (iconImage == null)
                return;

            iconImage.sprite = icon;
            iconImage.enabled = (icon != null);
        }

        public void SetKeyLabel(string label)
        {
            if (keyLabel == null)
                return;

            keyLabel.text = label;
        }

        public void SetInteractable(bool interactable)
        {
            if (button != null)
                button.interactable = interactable;
        }

        public void Pulse()
        {
            if (pulseOverlay == null)
                return;

            pulseOverlay.enabled = true;
            _pulseUntilTime = Time.unscaledTime + Mathf.Max(0.01f, pulseSeconds);
        }

        private void OnClicked()
        {
            Clicked?.Invoke(_slotIndex);
        }

        [ContextMenu("Auto-Wire References")]
        private void AutoWire()
        {
            if (button == null) button = GetComponentInChildren<Button>(true);

            // If your slot has multiple Images, you may want to manually assign
            // iconImage and pulseOverlay.
            if (iconImage == null) iconImage = GetComponentInChildren<Image>(true);

            Debug.Log("[HotbarSlotUI] AutoWire complete.");
        }
    }
}
