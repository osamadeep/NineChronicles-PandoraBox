using System;
using System.Collections.Generic;
using Nekoyume.Helper;
using Nekoyume.UI.Model;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    using UniRx;

    [RequireComponent(typeof(BaseItemView))]
    public class CollectionItemView : MonoBehaviour
    {
        [SerializeField]
        private BaseItemView baseItemView;

        [SerializeField]
        private Color requiredColor;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        public void Set(CollectionMaterial model, Action<CollectionMaterial> onClick)
        {
            _disposables.DisposeAllAndClear();

            baseItemView.Container.SetActive(true);
            baseItemView.EmptyObject.SetActive(false);
            baseItemView.MinusObject.gameObject.SetActive(false);

            var data = baseItemView.GetItemViewData(model.Grade);
            baseItemView.GradeImage.overrideSprite = data.GradeBackground;
            baseItemView.GradeHsv.range = data.GradeHsvRange;
            baseItemView.GradeHsv.hue = data.GradeHsvHue;
            baseItemView.GradeHsv.saturation = data.GradeHsvSaturation;
            baseItemView.GradeHsv.value = data.GradeHsvValue;

            baseItemView.TouchHandler.gameObject.SetActive(true);
            baseItemView.TouchHandler.OnClick.Select(_ => model).Subscribe(onClick).AddTo(_disposables);
            baseItemView.ItemImage.overrideSprite = SpriteHelper.GetItemIcon(model.Row.ItemId);

            baseItemView.EnhancementText.gameObject.SetActive(model.CheckLevel);
            baseItemView.EnhancementText.text = $"+{model.Row.Level.ToString()}";
            baseItemView.EnhancementText.color = model.EnoughCount ? Color.white : requiredColor;
            baseItemView.EnhancementText.enableVertexGradient = model.EnoughCount;

            baseItemView.CountText.gameObject.SetActive(!model.CheckLevel);
            baseItemView.CountText.text = model.Row.Count.ToString();
            baseItemView.CountText.color = model.EnoughCount ? Color.white : requiredColor;

            baseItemView.OptionTag.gameObject.SetActive(false);
            // baseItemView.OptionTag.Set(itemBase);

            baseItemView.EnoughObject.SetActive(model.Enough);
            baseItemView.TradableObject.SetActive(!model.HasItem);
            baseItemView.SelectCollectionObject.SetActive(false);
            baseItemView.SelectArrowObject.SetActive(false);

            baseItemView.SpineItemImage.gameObject.SetActive(false);
            baseItemView.EnhancementImage.gameObject.SetActive(false);
            baseItemView.EquippedObject.SetActive(false);
            baseItemView.PriceText.gameObject.SetActive(false);
            baseItemView.NotificationObject.SetActive(false);
            baseItemView.MinusObject.SetActive(false);
            baseItemView.FocusObject.SetActive(false);
            baseItemView.ExpiredObject.SetActive(false);
            baseItemView.DimObject.SetActive(false);
            baseItemView.LevelLimitObject.SetActive(false);
            baseItemView.SelectObject.SetActive(false);
            baseItemView.SelectBaseItemObject.SetActive(false);
            baseItemView.SelectMaterialItemObject.SetActive(false);
            baseItemView.LockObject.SetActive(false);
            baseItemView.ShadowObject.SetActive(false);
            baseItemView.LoadingObject.SetActive(false);
            baseItemView.GrindingCountObject.SetActive(false);
            baseItemView.RuneNotificationObj.SetActive(false);
            baseItemView.RuneSelectMove.SetActive(false);

            model.Selected
                .Subscribe(b => baseItemView.SelectCollectionObject.SetActive(b)).AddTo(_disposables);
        }
    }
}
