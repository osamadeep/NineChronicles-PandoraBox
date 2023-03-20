using System;
using System.Collections.Generic;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using UnityEngine;
using ShopItem = Nekoyume.UI.Model.ShopItem;

namespace Nekoyume.UI.Module
{
    using Nekoyume.PandoraBox;
    using Nekoyume.State;
    using UniRx;
    using UnityEngine.UI;

    [RequireComponent(typeof(BaseItemView))]
    public class ShopItemView : MonoBehaviour
    {
        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        [Header("PANDORA CUSTOM FIELDS")] [SerializeField]
        private Image remainingTimeImage;

        [Space(50)]
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
        [SerializeField]
        private BaseItemView baseItemView;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        public void Set(ShopItem model, Action<ShopItem> onClick)
        {
            if (model == null)
            {
                baseItemView.Container.SetActive(false);
                return;
            }

            _disposables.DisposeAllAndClear();
            baseItemView.Container.SetActive(true);
            baseItemView.EmptyObject.SetActive(false);
            baseItemView.EnoughObject.SetActive(false);
            baseItemView.MinusObject.SetActive(false);
            baseItemView.SelectBaseItemObject.SetActive(false);
            baseItemView.SelectMaterialItemObject.SetActive(false);
            baseItemView.LockObject.SetActive(false);
            baseItemView.NotificationObject.SetActive(false);
            baseItemView.FocusObject.SetActive(false);
            baseItemView.TradableObject.SetActive(false);
            baseItemView.DimObject.SetActive(false);
            baseItemView.EquippedObject.SetActive(false);
            baseItemView.LoadingObject.SetActive(false);

            baseItemView.ItemImage.overrideSprite = BaseItemView.GetItemIcon(model.ItemBase);

            var data = baseItemView.GetItemViewData(model.ItemBase);

            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            //Guild items

            GuildPlayer guildPlayer = PandoraMaster.PanDatabase.GuildPlayers.Find(x =>
                x.Address.ToString() == model.OrderDigest.SellerAgentAddress.ToString());
            if (!(guildPlayer is null) && !(PandoraMaster.CurrentGuild is null))
            {
                if (PandoraMaster.CurrentGuild.Tag == guildPlayer.Guild && model.OrderDigest.SellerAgentAddress !=
                    States.Instance.CurrentAvatarState.address)
                {
                    baseItemView.GuildObj.SetActive(true);
                }
            }

            // my item
            baseItemView.myItem.SetActive(model.OrderDigest.SellerAgentAddress ==
                                          States.Instance.CurrentAvatarState.agentAddress);

            remainingTimeImage.fillAmount =
                ((float)model.OrderDigest.ExpiredBlockIndex - (float)Game.Game.instance.Agent.BlockIndex) / 36000f;
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

            baseItemView.GradeImage.overrideSprite = data.GradeBackground;
            baseItemView.GradeHsv.range = data.GradeHsvRange;
            baseItemView.GradeHsv.hue = data.GradeHsvHue;
            baseItemView.GradeHsv.saturation = data.GradeHsvSaturation;
            baseItemView.GradeHsv.value = data.GradeHsvValue;

            if (model.ItemBase is Equipment equipment && equipment.level > 0)
            {
                baseItemView.EnhancementText.gameObject.SetActive(true);
                baseItemView.EnhancementText.text = $"+{equipment.level}";
                if (equipment.level >= Util.VisibleEnhancementEffectLevel)
                {
                    baseItemView.EnhancementImage.material = data.EnhancementMaterial;
                    baseItemView.EnhancementImage.gameObject.SetActive(true);
                }
                else
                {
                    baseItemView.EnhancementImage.gameObject.SetActive(false);
                }
            }
            else
            {
                baseItemView.EnhancementText.gameObject.SetActive(false);
                baseItemView.EnhancementImage.gameObject.SetActive(false);
            }

            baseItemView.LevelLimitObject.SetActive(model.LevelLimited);

            baseItemView.OptionTag.Set(model.ItemBase);

            baseItemView.CountText.gameObject.SetActive(model.ItemBase.ItemType == ItemType.Material);
            baseItemView.CountText.text = model.OrderDigest.ItemCount.ToString();

            if (model.OrderDigest.ItemCount > 1 &&
                decimal.TryParse(model.OrderDigest.Price.GetQuantityString(), out var price))
            {
                var priceText = decimal.Round(price / model.OrderDigest.ItemCount, 3);
                //baseItemView.PriceText.text = $"{model.OrderDigest.Price.GetQuantityString()}({priceText})";               
                baseItemView.PriceText.text = $"{model.OrderDigest.Price.GetQuantityString()}({priceText})";
            }
            else
            {
                baseItemView.PriceText.text = model.OrderDigest.Price.GetQuantityString();
                //|||||||||||||| PANDORA START CODE |||||||||||||||||||
                if (PandoraBox.PandoraMaster.Instance.Settings.CurrencyType == 0)
                {
                    baseItemView.PriceText.text = model.OrderDigest.Price.GetQuantityString();
                }
                else if ((int)((int)model.OrderDigest.Price.MajorUnit * PandoraMaster.WncgPrice) != 0)
                {
                    string dollarValue =
                        $" <color=green>$</color>{(int)((int)model.OrderDigest.Price.MajorUnit * PandoraBox.PandoraMaster.WncgPrice)}";
                    if (PandoraBox.PandoraMaster.Instance.Settings.CurrencyType == 1)
                    {
                        baseItemView.PriceText.text = dollarValue;
                    }
                    else
                    {
                        baseItemView.PriceText.text = model.OrderDigest.Price.GetQuantityString() + dollarValue;
                    }
                }
                //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
            }

            model.Selected.Subscribe(b => baseItemView.SelectObject.SetActive(b)).AddTo(_disposables);
            model.Expired.Subscribe(b => baseItemView.ExpiredObject.SetActive(b)).AddTo(_disposables);
            model.Loading.Subscribe(b => baseItemView.LoadingObject.SetActive(b)).AddTo(_disposables);

            baseItemView.TouchHandler.OnClick.Select(_ => model)
                .Subscribe(onClick).AddTo(_disposables);
        }
    }
}