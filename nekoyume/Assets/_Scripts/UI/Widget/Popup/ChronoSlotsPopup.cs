using System;
using System.Collections.Generic;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.UI.Module;
using UnityEngine;

namespace Nekoyume.UI
{
    using Cysharp.Threading.Tasks;
    using Nekoyume.PandoraBox;
    using UniRx;

    public class ChronoSlotsPopup : XTweenPopupWidget
    {
        public List<ChronoSlot> slots;
        public GameObject slotPrefab;

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
            //if (!PandoraMaster.TryGetCustomAvatarStatesDone)
            //{
            //    var states = States.Instance.AvatarStates;
            //    for (var i = 0; i < slots.Count; i++)
            //    {
            //        if (states != null && states.TryGetValue(i, out var state))
            //        {
            //            slots[i].gameObject.SetActive(true);
            //            slots[i].SetSlot(blockIndex, i, state);
            //        }
            //        else
            //            slots[i].gameObject.SetActive(false);
            //    }
            //}
            //else
            {
                var states = States.Instance.AvatarStates;
                for (int i = 0; i < states.Count; i++)
                {
                    if (slots.Count < i + 1) //add new slot
                    {
                        var newSlot = Instantiate(slotPrefab, slots[0].transform.parent);
                        slots.Add(newSlot.GetComponent<ChronoSlot>());
                    }

                    if (States.Instance.AvatarStates.TryGetValue(i, out var state))
                    {
                        slots[i].gameObject.SetActive(true);
                        slots[i].SetSlot(blockIndex, i, state);
                    }
                    else
                        slots[i].gameObject.SetActive(false);

                }
            }
            slots[0].transform.parent.GetComponent<RectTransform>().sizeDelta = new Vector2(550, 110 * States.Instance.AvatarStates.Count);

            bool isNotify = false;
            foreach (var item in slots)
            {
                if (item.HasNotification)
                {
                    isNotify = true;
                    break;
                }
            }
            Widget.Find<Menu>().chronoButton.transform.Find("MarkImage").gameObject.SetActive(isNotify);
        }
    }
}
