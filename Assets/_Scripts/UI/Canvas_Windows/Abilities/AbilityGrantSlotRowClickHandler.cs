using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UltimateDungeon.UI
{
    using UltimateDungeon.Items;

    [DisallowMultipleComponent]
    public sealed class AbilityGrantSlotRowClickHandler : MonoBehaviour, IPointerDownHandler
    {
        private AbilityGrantSlot _slot;
        private Action<AbilityGrantSlot> _onClicked;

        public void Configure(AbilityGrantSlot slot, Action<AbilityGrantSlot> onClicked)
        {
            _slot = slot;
            _onClicked = onClicked;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
                return;

            _onClicked?.Invoke(_slot);
        }
    }
}
