using System;
using System.Collections.Generic;
using Nekoyume.Helper;
using Nekoyume.UI.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    using Coffee.UIEffects;
    using Lib9c.Model.Order;
    using Nekoyume.Helper;
    using PandoraBox;
    using UniRx;

    public class ShopItemView : CountableItemView<ShopItem>
    {
        public GameObject priceGroup;
        public TextMeshProUGUI priceText;
        [SerializeField] private GameObject expired;
        [SerializeField] private Image remainsTime; //|||||||||||||| PANDORA CODE |||||||||||||||||||
        [SerializeField] private Material RedMaterial; //|||||||||||||| PANDORA CODE |||||||||||||||||||
        [SerializeField] private UIShiny shiny;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private long _expiredBlockIndex;

        public override void SetData(ShopItem model)
        {
            if (model is null)
            {
                Clear();
                return;
            }
            remainsTime.gameObject.SetActive(true);
            base.SetData(model);
            SetBg(1f);
            SetLevel(model.ItemBase.Value.Grade, model.Level.Value);
            priceGroup.SetActive(true);
            priceText.text = model.Price.Value.GetQuantityString();
            Model.View = this;

            //|||||||||||||| PANDORA CODE |||||||||||||||||||
            float x= ((model.ExpiredBlockIndex.Value - Game.Game.instance.Agent.BlockIndex)) * 1f / (Order.ExpirationInterval * 1f);
            remainsTime.fillAmount = x;
            if (x < 0.05f)
                remainsTime.gameObject.SetActive(false);

            if (expired)
            {
                _expiredBlockIndex = model.ExpiredBlockIndex.Value;
                SetExpired(Game.Game.instance.Agent.BlockIndex);
                Game.Game.instance.Agent.BlockIndexSubject
                    .Subscribe(SetExpired)
                    .AddTo(_disposables);
            }
        }

        public override void Clear()
        {
            if (Model != null)
            {
                Model.Selected.Value = false;
            }

            base.Clear();

            SetBg(0f);
            SetLevel(0, 0);
            priceGroup.SetActive(false);
            if (expired != null)
            {
                expired.SetActive(false);
            }
            _disposables.DisposeAllAndClear();
            remainsTime.gameObject.SetActive(false);
            shiny.enabled = false;
        }

        private void SetBg(float alpha)
        {
            var a = alpha;
            var color = backgroundImage.color;
            color.a = a;
            backgroundImage.color = color;
        }

        private void SetLevel(int grade, int level)
        {
            if (level > 0)
            {
                enhancementText.text = $"+{level}";
                enhancementText.enabled = true;
            }

            if (level >= Util.VisibleEnhancementEffectLevel)
            {
                var data = itemViewData.GetItemViewData(grade);

                enhancementImage.GetComponent<Image>().material = data.EnhancementMaterial;
                enhancementImage.SetActive(true);
            }


            if (Model != null)
            {
                //var order = Util.GetOrder(Model.OrderId.Value);
                
                //PanPlayer player = PandoraBoxMaster.GetPanPlayer(order.Result.SellerAvatarAddress.ToString());
                //if (player.IsPremium)
                //{
                //    enhancementImage.GetComponent<Image>().material = RedMaterial;
                //    enhancementImage.SetActive(true);
                //    shiny.enabled = true;
                //}
                //else
                //{
                //    shiny.enabled = false;
                //}
            }
        }

        private void SetExpired(long blockIndex)
        {
            if (expired)
            {
                expired.SetActive(_expiredBlockIndex - blockIndex <= 0);
            }
        }
    }
}
