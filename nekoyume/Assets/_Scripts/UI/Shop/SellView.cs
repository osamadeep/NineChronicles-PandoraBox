using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.EnumType;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using ShopItem = Nekoyume.UI.Model.ShopItem;

namespace Nekoyume
{
    using UniRx;

    public class SellView : ShopView
    {
        [SerializeField]
        private TMP_Dropdown itemSubTypeFilter;

        [SerializeField]
        private TMP_Dropdown sortFilter;

        private ShopItem _selectedItem;

        // search condition
        private readonly ReactiveProperty<ItemSubTypeFilter> _selectedSubTypeFilter =
            new(ItemSubTypeFilter.All);

        private readonly ReactiveProperty<Nekoyume.EnumType.ShopSortFilter> _selectedSortFilter =
            new(Nekoyume.EnumType.ShopSortFilter.CP);

        public void ClearSelectedItem()
        {
            _selectedItem?.Selected.SetValueAndForceNotify(false);
            _selectedItem = null;
        }

        protected override void OnAwake()
        {
        }

        protected override void InitInteractiveUI()
        {
            itemSubTypeFilter.AddOptions(ItemSubTypeFilterExtension.Filters
                .Select(type => type.TypeToString(true)).ToList());
            itemSubTypeFilter.onValueChanged.AsObservable()
                .Select(index =>
                {
                    try
                    {
                        return (ItemSubTypeFilter)index;
                    }
                    catch
                    {
                        return ItemSubTypeFilter.All;
                    }
                })
                .Subscribe(filter => _selectedSubTypeFilter.Value = filter).AddTo(gameObject);

            sortFilter.AddOptions(ShopSortFilterExtension.ShopSortFilters
                .Select(type => L10nManager.Localize($"UI_{type.ToString().ToUpper()}")).ToList());
            sortFilter.onValueChanged.AsObservable()
                .Select(index =>
                {
                    try
                    {
                        return (Nekoyume.EnumType.ShopSortFilter)index;
                    }
                    catch
                    {
                        return Nekoyume.EnumType.ShopSortFilter.Class;
                    }
                })
                .Subscribe(filter => _selectedSortFilter.Value = filter).AddTo(gameObject);
        }

        protected override void SubscribeToSearchConditions()
        {
            _selectedSubTypeFilter.Subscribe(_ => UpdateView()).AddTo(gameObject);
            _selectedSortFilter.Subscribe(_ => UpdateView()).AddTo(gameObject);
        }

        protected override void OnClickItem(ShopItem item)
        {
            if (_selectedItem == null)
            {
                _selectedItem = item;
                _selectedItem.Selected.SetValueAndForceNotify(true);
                ClickItemAction?.Invoke(_selectedItem); // Show tooltip popup
            }
            else
            {
                if (_selectedItem.Equals(item))
                {
                    _selectedItem.Selected.SetValueAndForceNotify(false);
                    _selectedItem = null;
                }
                else
                {
                    _selectedItem.Selected.SetValueAndForceNotify(false);
                    _selectedItem = item;
                    _selectedItem.Selected.SetValueAndForceNotify(true);
                    ClickItemAction?.Invoke(_selectedItem); // Show tooltip popup
                }
            }
        }

        protected override void Reset()
        {
            itemSubTypeFilter.SetValueWithoutNotify(0);
            sortFilter.SetValueWithoutNotify(0);
            _selectedSubTypeFilter.Value = ItemSubTypeFilter.All;
            _selectedSortFilter.Value = Nekoyume.EnumType.ShopSortFilter.Class;
            _selectedItem = null;
        }

        protected override IEnumerable<ShopItem> GetSortedModels(
            Dictionary<ItemSubTypeFilter, List<ShopItem>> items)
        {
            var models = items[_selectedSubTypeFilter.Value].Distinct();
            return _selectedSortFilter.Value switch
            {
                Nekoyume.EnumType.ShopSortFilter.CP => models.OrderByDescending(x => x.OrderDigest.CombatPoint)
                    .ToList(),
                Nekoyume.EnumType.ShopSortFilter.Price => models.OrderByDescending(x => x.OrderDigest.Price).ToList(),
                Nekoyume.EnumType.ShopSortFilter.Class => models.OrderByDescending(x => x.Grade)
                    .ThenByDescending(x => x.ItemBase.ItemType).ToList(),
                ShopSortFilter.Crystal => models.OrderByDescending(x => x.ItemBase.ItemType == ItemType.Equipment
                    ? CrystalCalculator.CalculateCrystal(
                            new[] {(Equipment) x.ItemBase},
                            false,
                            TableSheets.Instance.CrystalEquipmentGrindingSheet,
                            TableSheets.Instance.CrystalMonsterCollectionMultiplierSheet,
                            States.Instance.StakingLevel).DivRem(x.OrderDigest.Price.MajorUnit)
                        .Quotient
                        .MajorUnit
                    : 0),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}
