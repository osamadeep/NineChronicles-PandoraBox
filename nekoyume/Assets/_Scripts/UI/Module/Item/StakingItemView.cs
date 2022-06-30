using System;
using System.Collections.Generic;
using System.Numerics;
using Nekoyume.Model.Item;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    using UniRx;
    [RequireComponent(typeof(BaseItemView))]
    public class StakingItemView : MonoBehaviour
    {
        [SerializeField] private BaseItemView baseItemView;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        public void Set(ItemBase itemBase, BigInteger count, Action<ItemBase> onClick)
        {
            if (itemBase == null)
            {
                return;
            }
            _disposables.DisposeAllAndClear();
            baseItemView.Container.SetActive(true);
            baseItemView.TouchHandler.gameObject.SetActive(true);
            baseItemView.ItemImage.sprite = BaseItemView.GetItemIcon(itemBase);
            baseItemView.CountText.text = count.ToString();

            baseItemView.TouchHandler.OnClick
                .Select(_ => itemBase).Subscribe(onClick).AddTo(_disposables);
        }
    }
}
