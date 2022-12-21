using System;
using System.Collections.Generic;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.UI.Module;
using UnityEngine;

namespace Nekoyume.UI
{
    using UniRx;

    public class ChronoSlotsPopup : XTweenPopupWidget
    {
        public List<ChronoSlot> slots;

        private readonly List<IDisposable> _disposablesOfOnEnable = new List<IDisposable>();

        //protected override void OnEnable()
        //{
        //    base.OnEnable();

        //}

        protected override void Awake()
        {
            base.Awake();
            try
            {
                Game.Game.instance.Agent.BlockIndexSubject.ObserveOnMainThread()
                .Subscribe(SubscribeBlockIndex);
                //.AddTo(_disposablesOfOnEnable);
            }
            catch { }
        }

        protected override void OnDisable()
        {
            _disposablesOfOnEnable.DisposeAllAndClear();
            base.OnDisable();
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            //UpdateSlots(Game.Game.instance.Agent.BlockIndex);
        }

        private void SubscribeBlockIndex(long blockIndex)
        {
            UpdateSlots(blockIndex);
        }

        private void UpdateSlots(long blockIndex)
        {
            var states = States.Instance.AvatarStates;

            for (var i = 0; i < slots.Count; i++)
            {
                if (states != null && states.TryGetValue(i, out var state))
                {
                    slots[i].SetSlot(blockIndex, i, state);
                }
                else
                {
                    slots[i].gameObject.SetActive(false);
                }
            }

            bool isNotify = false;
            foreach (var item in slots)
            {
                if (item.hasNotificationImage.enabled)
                {
                    isNotify = true;
                    break;
                }
            }
            Widget.Find<Menu>().chronoButton.transform.Find("MarkImage").gameObject.SetActive(isNotify);
        }
    }
}
