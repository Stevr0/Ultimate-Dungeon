// ============================================================================
// UIDragGhost.cs
// ----------------------------------------------------------------------------
// Purpose:
// - Simple singleton that shows a sprite following the cursor during drag.
// - Keeps drag visuals separate from gameplay.
//
// Setup:
// - Create a GameObject under your UI Canvas:
//     UI_DragGhost
// - Add this component.
// - Assign:
//   - rootCanvas (the canvas this lives under)
//   - iconImage (an Image component)
// - Ensure the Image has Raycast Target = false (so it doesn't block drops).
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;


namespace UltimateDungeon.UI
{
    [DisallowMultipleComponent]
    public sealed class UIDragGhost : MonoBehaviour
    {
        public static UIDragGhost Instance { get; private set; }

        [Header("Required")]
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private Image iconImage;

        private RectTransform _rect;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _rect = transform as RectTransform;

            if (iconImage != null)
                iconImage.raycastTarget = false;

            Hide();
        }

        private void Update()
        {
            if (iconImage == null || !iconImage.enabled)
                return;

            // Follow cursor.
            // For Screen Space Overlay canvas, we can directly use screen position.
            // For Camera/World Space, we convert via RectTransformUtility.
            // NEW INPUT SYSTEM: read pointer position from Mouse (or generic Pointer fallback).
            Vector2 screenPos = Vector2.zero;

            if (Mouse.current != null)
                screenPos = Mouse.current.position.ReadValue();
            else if (Pointer.current != null)
                screenPos = Pointer.current.position.ReadValue();


            if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rootCanvas.transform as RectTransform,
                    screenPos,
                    rootCanvas.worldCamera,
                    out var local
                );

                _rect.anchoredPosition = local;
            }
            else
            {
                // Overlay
                _rect.position = screenPos;
            }
        }

        public void Show(Sprite sprite)
        {
            if (iconImage == null)
                return;

            iconImage.sprite = sprite;
            iconImage.enabled = (sprite != null);
        }

        public void Hide()
        {
            if (iconImage == null)
                return;

            iconImage.sprite = null;
            iconImage.enabled = false;
        }
    }
}
