// ============================================================================
// ScrollRectWheelOnly.cs
// ----------------------------------------------------------------------------
// Purpose:
// - Prevents left-click dragging from scrolling a ScrollRect.
// - Keeps scrolling via mouse wheel / touchpad scroll working.
//
// Why:
// - You want left-click drag for inventory drag/drop.
// - Default Unity ScrollRect consumes drag and scrolls content.
//
// Setup:
// - Replace your ScrollRect component with this (inherits ScrollRect), OR
// - Add this to the same GameObject and remove the original ScrollRect.
//
// Notes:
// - We intentionally DO NOT call base.OnBeginDrag/OnDrag/OnEndDrag.
// - Wheel scrolling still works because ScrollRect uses IScrollHandler.
// ============================================================================

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UltimateDungeon.UI
{
    public class ScrollRectWheelOnly : ScrollRect
    {
        public override void OnInitializePotentialDrag(PointerEventData eventData)
        {
            // Still let ScrollRect initialize internal state.
            // This does not start dragging; it just prepares.
            base.OnInitializePotentialDrag(eventData);

            // Unity sets useDragThreshold = true by default; keep it.
        }

        public override void OnBeginDrag(PointerEventData eventData)
        {
            // Intentionally do nothing.
            // Prevents content from scrolling when the user drags with left mouse.
        }

        public override void OnDrag(PointerEventData eventData)
        {
            // Intentionally do nothing.
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            // Intentionally do nothing.
        }
    }
}
