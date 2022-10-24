using System;
using System.Collections.Generic;
using Libplanet;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.UI.Module;
using UnityEngine;

namespace Nekoyume.UI
{
    using UniRx;

    public class CombinationSlotsPopup : XTweenPopupWidget
    {
        [SerializeField] private List<CombinationSlot> slots;

        private readonly List<IDisposable> _disposablesOfOnEnable = new();

        protected override void OnEnable()
        {
            base.OnEnable();
            Game.Game.instance.Agent.BlockIndexSubject
                .ObserveOnMainThread()
                .Subscribe(UpdateSlots)
                .AddTo(_disposablesOfOnEnable);
        }

        protected override void OnDisable()
        {
            _disposablesOfOnEnable.DisposeAllAndClear();
            base.OnDisable();
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            UpdateSlots(Game.Game.instance.Agent.BlockIndex);
            HelpTooltip.HelpMe(100008, true);
        }

        public void SetCaching(
            Address avatarAddress,
            int slotIndex,
            bool value,
            long requiredBlockIndex = 0,
            CombinationSlot.SlotType slotType = CombinationSlot.SlotType.Appraise,
            ItemUsable itemUsable = null)
        {
            slots[slotIndex].SetCached(avatarAddress, value, requiredBlockIndex, slotType, itemUsable);
            UpdateSlots(Game.Game.instance.Agent.BlockIndex);
        }

        public bool TryGetEmptyCombinationSlot(out int slotIndex)
        {
            UpdateSlots(Game.Game.instance.Agent.BlockIndex);
            for (var i = 0; i < slots.Count; i++)
            {
                if (slots[i].Type != CombinationSlot.SlotType.Empty)
                {
                    continue;
                }

                slotIndex = i;
                return true;
            }

            slotIndex = -1;
            return false;
        }

        private void UpdateSlots(long blockIndex)
        {
            var avatarState = States.Instance.CurrentAvatarState;
            var states = States.Instance.GetCombinationSlotState(avatarState, blockIndex);
            for (var i = 0; i < slots.Count; i++)
            {
                if (states.ContainsKey(i))
                {
                    if (states.TryGetValue(i, out var state))
                    {
                        slots[i].SetSlot(avatarState.address, blockIndex, i, state);
                    }
                    else
                    {
                        slots[i].SetSlot(avatarState.address, blockIndex, i);
                    }
                }
                else
                {
                    slots[i].SetSlot(avatarState.address, blockIndex, i);
                }
            }
        }
    }
}
