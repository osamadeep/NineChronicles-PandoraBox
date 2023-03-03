using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using Nekoyume.EnumType;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.UI;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ShopItem = Nekoyume.UI.Model.ShopItem;
using Toggle = UnityEngine.UI.Toggle;
using Vector3 = UnityEngine.Vector3;

namespace Nekoyume
{
    using Nekoyume.PandoraBox;
    using UniRx;

    public class BuyView : ShopView
    {
        public enum BuyMode
        {
            Single,
            Multiple,
        }

        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        [Header("PANDORA CUSTOM FIELDS")] public UnityEngine.UI.Toggle PriceToggle;
        [SerializeField] private UnityEngine.UI.Toggle SpellToggle;
        [SerializeField] private TMP_InputField AddressTxt = null;

        [SerializeField] private TMP_InputField priceValueTxt = null;
        [SerializeField] private TMP_Dropdown IsLessDrop = null;
        [SerializeField] private TMP_Dropdown ItemElementType = null;
        [SerializeField] private TMP_Dropdown StarCountsDrop = null;

        [SerializeField] private GameObject SelectedOriginalVFX;
        [SerializeField] private GameObject SelectedPandoraVFX;
        [SerializeField] private Button sortPandoraButton;
        [SerializeField] private Button sortOrderPandoraButton;
        [SerializeField] private RectTransform sortOrderPandoraIcon = null;
        private Animator _sortPandoraAnimator;
        private Animator _sortOrderPandoraAnimator;
        private TextMeshProUGUI _sortPandoraText;


        int PandoraExtraIndexStart = 4;

        [Space(50)]
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
        [SerializeField]
        private CartView cartView;

        public List<ToggleDropdown> toggleDropdowns = new List<ToggleDropdown>(); // [SerializeField] private

        [SerializeField] private Button sortButton;

        [SerializeField] private Button sortOrderButton;

        [SerializeField] private Button searchButton;

        [SerializeField] private Button resetButton;

        [SerializeField] private Button historyButton;

        [SerializeField] private Button showCartButton;

        [SerializeField] private Toggle levelLimitToggle;

        [SerializeField] private RectTransform sortOrderIcon = null;

        [SerializeField] private TMP_InputField inputField = null;

        [SerializeField] private Transform inputPlaceholder = null;

        [SerializeField] private GameObject loading;

        private readonly List<ItemSubTypeFilter> _toggleTypes = new List<ItemSubTypeFilter>()
        {
            ItemSubTypeFilter.Equipment,
            ItemSubTypeFilter.Food,
            ItemSubTypeFilter.Materials,
            ItemSubTypeFilter.Costume,
        };

        private readonly Dictionary<ItemSubTypeFilter, List<ItemSubTypeFilter>> _toggleSubTypes =
            new()
            {
                {
                    ItemSubTypeFilter.Equipment, new List<ItemSubTypeFilter>()
                    {
                        ItemSubTypeFilter.Weapon,
                        ItemSubTypeFilter.Armor,
                        ItemSubTypeFilter.Belt,
                        ItemSubTypeFilter.Necklace,
                        ItemSubTypeFilter.Ring,
                    }
                },
                {
                    ItemSubTypeFilter.Food, new List<ItemSubTypeFilter>()
                    {
                        ItemSubTypeFilter.Food_HP,
                        ItemSubTypeFilter.Food_ATK,
                        ItemSubTypeFilter.Food_DEF,
                        ItemSubTypeFilter.Food_CRI,
                        ItemSubTypeFilter.Food_HIT,
                    }
                },
                {
                    ItemSubTypeFilter.Materials, new List<ItemSubTypeFilter>()
                },
                {
                    ItemSubTypeFilter.Costume, new List<ItemSubTypeFilter>()
                    {
                        ItemSubTypeFilter.FullCostume,
                        ItemSubTypeFilter.HairCostume,
                        ItemSubTypeFilter.EarCostume,
                        ItemSubTypeFilter.EyeCostume,
                        ItemSubTypeFilter.TailCostume,
                        ItemSubTypeFilter.Title,
                    }
                },
            };

        private readonly List<ShopItem> _selectedItems = new();
        private readonly List<int> _itemIds = new List<int>();
        private readonly int _hashNormal = Animator.StringToHash("Normal");
        private readonly int _hashDisabled = Animator.StringToHash("Disabled");
        private const int CartMaxCount = 20;

        private readonly ReactiveProperty<ItemSubTypeFilter> _selectedSubTypeFilter =
            new(ItemSubTypeFilter.All);

        private readonly ReactiveProperty<Nekoyume.EnumType.ShopSortFilter> _selectedSortFilter =
            new ReactiveProperty<Nekoyume.EnumType.ShopSortFilter>(Nekoyume.EnumType.ShopSortFilter.Class);
        //private readonly ReactiveProperty<ShopSortFilter> _selectedSortFilter =
        //    new(ShopSortFilter.CP);

        private readonly ReactiveProperty<List<int>> _selectedItemIds = new(new List<int>());

        private readonly ReactiveProperty<bool> _isAscending = new();
        private readonly ReactiveProperty<bool> _levelLimit = new();

        private readonly ReactiveProperty<BuyMode> _mode = new(BuyMode.Single);

        private Action<List<ShopItem>> _onBuyMultiple;

        private Animator _sortAnimator;
        private Animator _sortOrderAnimator;
        private Animator _levelLimitAnimator;
        private Animator _resetAnimator;
        private TextMeshProUGUI _sortText;

        public bool IsFocused => inputField.isFocused;
        public bool IsDoneLoadItem { get; set; }
        public bool IsCartEmpty => !_selectedItems.Any();

        public void ClearSelectedItems()
        {
            foreach (var model in _selectedItems)
            {
                model?.Selected.SetValueAndForceNotify(false);
            }

            _selectedItems.Clear();
            cartView.UpdateCart(_selectedItems);
        }

        public void SetAction(Action<List<ShopItem>> onBuyMultiple)
        {
            _onBuyMultiple = onBuyMultiple;
        }

        protected override void OnAwake()
        {
            _sortAnimator = sortButton.GetComponent<Animator>();
            _sortOrderAnimator = sortOrderButton.GetComponent<Animator>();
            _levelLimitAnimator = levelLimitToggle.GetComponent<Animator>();
            _resetAnimator = resetButton.GetComponent<Animator>();

            _sortText = sortButton.GetComponentInChildren<TextMeshProUGUI>();
            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            _sortPandoraAnimator = sortPandoraButton.GetComponent<Animator>();
            _sortOrderPandoraAnimator = sortOrderPandoraButton.GetComponent<Animator>();
            _sortPandoraText = sortPandoraButton.GetComponentInChildren<TextMeshProUGUI>();
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
            var tableSheets = Game.Game.instance.TableSheets;
            _itemIds.AddRange(tableSheets.EquipmentItemSheet.Values.Select(x => x.Id));
            _itemIds.AddRange(tableSheets.ConsumableItemSheet.Values.Select(x => x.Id));
            _itemIds.AddRange(tableSheets.CostumeItemSheet.Values.Select(x => x.Id));
            _itemIds.AddRange(tableSheets.MaterialItemSheet.Values.Select(x => x.Id));

            historyButton.onClick.AddListener(() =>
            {
                Widget.Find<Alert>().Show("UI_ALERT_NOT_IMPLEMENTED_TITLE",
                    "UI_ALERT_NOT_IMPLEMENTED_CONTENT");
            });

            showCartButton.onClick.AddListener(() => { _mode.SetValueAndForceNotify(BuyMode.Multiple); });

            cartView.Set(() =>
                {
                    if (_selectedItems.Exists(x => x.Expired.Value))
                    {
                        OneLineSystem.Push(MailType.System,
                            L10nManager.Localize("UI_SALE_PERIOD_HAS_EXPIRED"),
                            NotificationCell.NotificationType.Alert);
                    }
                    else
                    {
                        _onBuyMultiple?.Invoke(_selectedItems);
                    }
                },
                () =>
                {
                    if (_selectedItems.Any())
                    {
                        Widget.Find<TwoButtonSystem>().Show(
                            L10nManager.Localize("UI_CLOSE_BUY_WISH_LIST"),
                            L10nManager.Localize("UI_YES"),
                            L10nManager.Localize("UI_NO"),
                            () => _mode.SetValueAndForceNotify(BuyMode.Single));
                    }
                    else
                    {
                        _mode.SetValueAndForceNotify(BuyMode.Single);
                    }
                });

            Game.Game.instance.Agent.BlockIndexSubject.Subscribe(_ => UpdateView(false))
                .AddTo(gameObject);

            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            PriceToggle.onValueChanged.AddListener(_ => { UpdateView(); });
            SpellToggle.onValueChanged.AddListener(_ => { UpdateView(); });
            priceValueTxt.onValueChanged.AddListener(_ =>
            {
                if (priceValueTxt.text.Length > 0)
                    UpdateView();
            });
            IsLessDrop.onValueChanged.AddListener(_ => { UpdateView(); });
            ItemElementType.onValueChanged.AddListener(_ => { UpdateView(); });
            StarCountsDrop.onValueChanged.AddListener(_ => { UpdateView(); });
            AddressTxt.onValueChanged.AddListener(_ => { UpdateView(); });
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
        }

        protected override void InitInteractiveUI()
        {
            inputPlaceholder.SetAsLastSibling();

            foreach (var toggleDropdown in toggleDropdowns)
            {
                var index = toggleDropdowns.IndexOf(toggleDropdown);
                var toggleType = _toggleTypes[index];
                toggleDropdown.onValueChanged.AddListener((value) =>
                {
                    if (!value)
                    {
                        return;
                    }

                    if (_toggleSubTypes[toggleType].Count > 0)
                    {
                        _selectedSubTypeFilter.Value = _toggleSubTypes[toggleType].First();
                        toggleDropdown.items.First().isOn = true;
                    }
                    else
                    {
                        _selectedSubTypeFilter.Value = ItemSubTypeFilter.Materials;
                    }

                    UpdateView();
                });
                toggleDropdown.onClickToggle.AddListener(AudioController.PlayClick);

                var subItems = toggleDropdown.items;

                foreach (var item in subItems)
                {
                    var subIndex = subItems.IndexOf(item);
                    var subTypes = _toggleSubTypes[toggleType];
                    var subToggleType = subTypes[subIndex];
                    item.onValueChanged.AddListener((value) =>
                    {
                        if (!value)
                        {
                            return;
                        }

                        _selectedSubTypeFilter.Value = subToggleType;
                        UpdateView();
                    });
                    item.onClickToggle.AddListener(AudioController.PlayClick);
                }
            }

            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            sortPandoraButton.onClick.AddListener(() =>
            {
                var count = Enum.GetNames(typeof(Nekoyume.EnumType.ShopSortFilter)).Length;
                int selected = (int)_selectedSortFilter.Value;
                //_selectedSortFilter.Value = (int)_selectedSortFilter.Value < (count + PandoraExtraIndexStart) - 1
                //    ? _selectedSortFilter.Value + 1
                //    : ShopSortFilter.Time;
                _selectedSortFilter.Value = (Nekoyume.EnumType.ShopSortFilter)UnityEngine.Mathf.Clamp(
                    selected < count - 1 ? selected + 1 : PandoraExtraIndexStart, PandoraExtraIndexStart,
                    Enum.GetNames(typeof(Nekoyume.EnumType.ShopSortFilter)).Length);
                SelectedOriginalVFX.SetActive((int)_selectedSortFilter.Value < PandoraExtraIndexStart);
                SelectedPandoraVFX.SetActive((int)_selectedSortFilter.Value >= PandoraExtraIndexStart);
            });
            sortOrderPandoraButton.onClick.AddListener(() => { _isAscending.Value = !_isAscending.Value; });
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||


            sortButton.onClick.AddListener(() =>
            {
                var count = Enum.GetNames(typeof(Nekoyume.EnumType.ShopSortFilter)).Length;
                //_selectedSortFilter.Value = (int)_selectedSortFilter.Value < count - 1
                //    ? _selectedSortFilter.Value + 1
                //    : 0;
                //|||||||||||||| PANDORA START CODE |||||||||||||||||||
                int selected = (int)_selectedSortFilter.Value;
                _selectedSortFilter.Value = (Nekoyume.EnumType.ShopSortFilter)UnityEngine.Mathf.Clamp(
                    selected < (count - PandoraExtraIndexStart) ? selected + 1 : 0, 0, count - PandoraExtraIndexStart);

                SelectedOriginalVFX.SetActive((int)_selectedSortFilter.Value < PandoraExtraIndexStart);
                SelectedPandoraVFX.SetActive((int)_selectedSortFilter.Value >= PandoraExtraIndexStart);
                //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
            });
            sortOrderButton.onClick.AddListener(() => { _isAscending.Value = !_isAscending.Value; });
            searchButton.onClick.AddListener(OnSearch);
            resetButton.onClick.AddListener(() =>
            {
                inputField.text = string.Empty;
                OnSearch();
            });
            inputField.onSubmit.AddListener(_ => OnSearch());
            inputField.onValueChanged.AddListener(_ =>
                searchButton.gameObject.SetActive(inputField.text.Length > 0));
            levelLimitToggle.onValueChanged.AddListener(value => _levelLimit.Value = value);
        }

        protected override void SubscribeToSearchConditions()
        {
            _selectedSubTypeFilter.Subscribe(_ => UpdateView()).AddTo(gameObject);
            _selectedSortFilter.Subscribe(filter =>
            {
                //_sortText.text = L10nManager.Localize($"UI_{filter.ToString().ToUpper()}");
                //|||||||||||||| PANDORA START CODE |||||||||||||||||||
                if ((int)filter < PandoraExtraIndexStart)
                    _sortText.text = L10nManager.Localize($"UI_{filter.ToString().ToUpper()}");
                else
                    _sortPandoraText.text = L10nManager.Localize($"UI_{filter.ToString().ToUpper()}");
                //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

                UpdateView();
            }).AddTo(gameObject);
            _selectedItemIds.Subscribe(_ => UpdateView()).AddTo(gameObject);
            _isAscending.Subscribe(v =>
            {
                sortOrderIcon.localScale = new Vector3(1, v ? 1 : -1, 1);
                sortOrderPandoraIcon.localScale = new Vector3(1, v ? 1 : -1, 1); //|||||||||||||| PANDORA
                UpdateView();
            }).AddTo(gameObject);
            _levelLimit.Subscribe(_ => UpdateView()).AddTo(gameObject);

            _mode.Subscribe(x =>
            {
                ClearSelectedItems();
                switch (_mode.Value)
                {
                    case BuyMode.Single:
                        cartView.gameObject.SetActive(false);
                        break;
                    case BuyMode.Multiple:
                        cartView.gameObject.SetActive(true);
                        break;
                }
            }).AddTo(gameObject);
        }

        protected override void OnClickItem(ShopItem item)
        {
            switch (_mode.Value)
            {
                case BuyMode.Single:
                    if (_selectedItems.Any())
                    {
                        if (_selectedItems.Exists(x =>
                                x.OrderDigest.OrderId.Equals(item.OrderDigest.OrderId)))
                        {
                            ClearSelectedItems();
                        }
                        else
                        {
                            ClearSelectedItems();
                            item.Selected.SetValueAndForceNotify(true);
                            _selectedItems.Add(item);
                            ClickItemAction?.Invoke(item); // Show tooltip popup
                        }
                    }
                    else
                    {
                        item.Selected.SetValueAndForceNotify(true);
                        _selectedItems.Add(item);
                        ClickItemAction?.Invoke(item); // Show tooltip popup
                    }

                    break;

                case BuyMode.Multiple:
                    cartView.gameObject.SetActive(true);
                    var selectedItem = _selectedItems.FirstOrDefault(x =>
                        x.OrderDigest.OrderId.Equals(item.OrderDigest.OrderId));
                    if (selectedItem == null)
                    {
                        if (item.Expired.Value)
                        {
                            OneLineSystem.Push(MailType.System,
                                L10nManager.Localize("UI_SALE_PERIOD_HAS_EXPIRED"),
                                NotificationCell.NotificationType.Alert);
                            return;
                        }

                        if (_selectedItems.Count() < CartMaxCount)
                        {
                            item.Selected.SetValueAndForceNotify(true);
                            _selectedItems.Add(item);
                        }
                        else
                        {
                            OneLineSystem.Push(MailType.System,
                                L10nManager.Localize("NOTIFICATION_BUY_WISHLIST_FULL"),
                                NotificationCell.NotificationType.Alert);
                        }
                    }
                    else
                    {
                        selectedItem.Selected.SetValueAndForceNotify(false);
                        _selectedItems.Remove(selectedItem);
                    }

                    cartView.UpdateCart(_selectedItems);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected override void Reset()
        {
            cartView.gameObject.SetActive(false);
            toggleDropdowns.First().isOn = true;
            toggleDropdowns.First().items.First().isOn = true;
            inputField.text = string.Empty;
            resetButton.interactable = false;
            _resetAnimator.Play(_hashDisabled);

            //_selectedSubTypeFilter.SetValueAndForceNotify(ItemSubTypeFilter.Weapon);
            //_selectedSortFilter.SetValueAndForceNotify(Nekoyume.EnumType.ShopSortFilter.Class);
            _selectedItemIds.Value.Clear();
            _isAscending.SetValueAndForceNotify(false);
            _levelLimit.SetValueAndForceNotify(levelLimitToggle.isOn);
            _mode.SetValueAndForceNotify(BuyMode.Single);

            ClearSelectedItems();
            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            PriceToggle.isOn = false;
            ItemElementType.value = 5; //all elements
            StarCountsDrop.value = 0;
            SpellToggle.isOn = false;
            AddressTxt.text = "";
            PandoraBox.Premium.SHOP_FirstFilter(_selectedSortFilter);
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
        }

        protected override IEnumerable<ShopItem> GetSortedModels(
            Dictionary<ItemSubTypeFilter, List<ShopItem>> items)
        {
            var models = items[_selectedSubTypeFilter.Value];
            models = models.Where(x => !x.Expired.Value).ToList();

            if (IsLoading(models))
            {
                return new List<ShopItem>();
            }

            if (_selectedItemIds.Value.Any()) // _selectedItemIds
            {
                models = models.Where(x =>
                    _selectedItemIds.Value.Exists(y => x.ItemBase.Id.Equals(y))).ToList();
            }

            if (_levelLimit.Value)
            {
                models = models.Where(x => Util.IsUsableItem(x.ItemBase)).ToList();
            }

            BigInteger GetCrystalPerPrice(ShopItem item)
            {
                return item.ItemBase.ItemType == ItemType.Equipment
                    ? CrystalCalculator.CalculateCrystal(
                            new[] { (Equipment)item.ItemBase },
                            false,
                            TableSheets.Instance.CrystalEquipmentGrindingSheet,
                            TableSheets.Instance.CrystalMonsterCollectionMultiplierSheet,
                            States.Instance.StakingLevel).DivRem(item.OrderDigest.Price.MajorUnit)
                        .Quotient
                        .MajorUnit
                    : 0;
            }

            //|||||||||||||| PANDORA START CODE |||||||||||||||||||


            if (PriceToggle.isOn)
            {
                switch (IsLessDrop.value)
                {
                    case 0:
                        models = models.Where(x => x.OrderDigest.Price.MajorUnit <= int.Parse(priceValueTxt.text))
                            .ToList();
                        break;
                    case 1:
                        models = models.Where(x => x.OrderDigest.Price.MajorUnit == int.Parse(priceValueTxt.text))
                            .ToList();
                        break;
                    case 2:
                        models = models.Where(x => x.OrderDigest.Price.MajorUnit >= int.Parse(priceValueTxt.text))
                            .ToList();
                        break;
                }
            }

            if (ItemElementType.value != 5)
            {
                models = models.Where(x =>
                    x.ItemBase.ElementalType == (Nekoyume.Model.Elemental.ElementalType)ItemElementType.value).ToList();
            }

            if (SpellToggle.isOn)
            {
                models = models.Where(x =>
                    x.ItemBase.ItemType == Model.Item.ItemType.Equipment &&
                    (x.ItemBase as Model.Item.Equipment).Skills.Count > 0).ToList();
            }

            if (StarCountsDrop.value != 0)
            {
                models = models.Where(x => x.ItemBase.ItemType == Model.Item.ItemType.Equipment &&
                                           ((x.ItemBase as Model.Item.Equipment).Skills.Count +
                                               (new ItemOptionInfo(x.ItemBase as Model.Item.Equipment).StatOptions.Sum(
                                                   y => y.count)) == StarCountsDrop.value)).ToList();
            }

            if (!string.IsNullOrEmpty(AddressTxt.text))
            {
                if (Premium.PandoraProfile.IsPremium())
                    models = models.Where(x =>
                        x.OrderDigest.SellerAgentAddress.ToString().ToLower() == AddressTxt.text.ToLower()).ToList();
            }


            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

            return _selectedSortFilter.Value switch
            {
                Nekoyume.EnumType.ShopSortFilter.CP => _isAscending.Value
                    ? models.OrderBy(x => x.OrderDigest.CombatPoint).ToList()
                    : models.OrderByDescending(x => x.OrderDigest.CombatPoint).ToList(),
                Nekoyume.EnumType.ShopSortFilter.Price => _isAscending.Value
                    ? models.OrderBy(x =>
                        ((int)x.OrderDigest.Price.RawValue / 100f) / ((int)x.OrderDigest.ItemCount / 100f)).ToList()
                    : models.OrderByDescending(x =>
                        ((int)x.OrderDigest.Price.RawValue / 100f) / ((int)x.OrderDigest.ItemCount / 100f)).ToList(),
                Nekoyume.EnumType.ShopSortFilter.Class => _isAscending.Value
                    ? models.OrderBy(x => x.Grade)
                        .ThenByDescending(x => x.ItemBase.ItemType)
                        .ToList()
                    : models.OrderByDescending(x => x.Grade)
                        .ThenByDescending(x => x.ItemBase.ItemType)
                        .ToList(),
                Nekoyume.EnumType.ShopSortFilter.Crystal => _isAscending.Value
                    ? models.OrderBy(GetCrystalPerPrice).ToList()
                    : models.OrderByDescending(GetCrystalPerPrice).ToList(),
                Nekoyume.EnumType.ShopSortFilter.Time => PandoraBox.Premium.SHOP_TimeFilter(_isAscending, models),
                Nekoyume.EnumType.ShopSortFilter.Level => _isAscending.Value
                    ? models.OrderBy(x => x.OrderDigest.Level).ToList()
                    : models.OrderByDescending(x => x.OrderDigest.Level).ToList(),
                Nekoyume.EnumType.ShopSortFilter.PandoraScore => PandoraBox.Premium.SHOP_PandoraScoreFilter(
                    _isAscending, models),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private bool IsLoading(ICollection models)
        {
            if (IsDoneLoadItem)
            {
                loading.SetActive(false);
                inputField.interactable = true;
                levelLimitToggle.interactable = true;
                _sortAnimator.Play(_hashNormal);
                _sortOrderAnimator.Play(_hashNormal);
                _levelLimitAnimator.Play(_hashNormal);
                //|||||||||||||| PANDORA START CODE |||||||||||||||||||
                _sortPandoraAnimator.Play(_hashNormal);
                _sortOrderPandoraAnimator.Play(_hashNormal);
                //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
            }
            else
            {
                loading.SetActive(models.Count == 0);
                inputField.interactable = models.Count > 0;
                levelLimitToggle.interactable = models.Count > 0;
                var hash = models.Count > 0 ? _hashNormal : _hashDisabled;
                _sortAnimator.Play(hash);
                _sortOrderAnimator.Play(hash);
                _levelLimitAnimator.Play(hash);
                //|||||||||||||| PANDORA START CODE |||||||||||||||||||
                _sortPandoraAnimator.Play(hash);
                _sortOrderPandoraAnimator.Play(hash);
                //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
            }

            return loading.activeSelf;
        }

        protected override void UpdateView(bool resetPage = true, int page = 0)
        {
            var expiredItems = _selectedItems.Where(x => x.Expired.Value).ToList();
            foreach (var item in expiredItems)
            {
                _selectedItems.Remove(item);
            }

            switch (_mode.Value)
            {
                case BuyMode.Single:
                    break;
                case BuyMode.Multiple:
                    cartView.UpdateCart(_selectedItems);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            base.UpdateView(resetPage, page);
        }

        private void OnSearch()
        {
            resetButton.interactable = inputField.text.Length > 0;
            _resetAnimator.Play(inputField.text.Length > 0 ? _hashNormal : _hashDisabled);

            var containItemIds = new List<int>();
            foreach (var id in _itemIds)
            {
                var itemName = L10nManager.LocalizeItemName(id);
                if (Regex.IsMatch(itemName, inputField.text, RegexOptions.IgnoreCase))
                {
                    containItemIds.Add(id);
                }
            }

            _selectedItemIds.Value = containItemIds;
        }

        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        public void OnPriceToggle()
        {
            OnSearch();
        }
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
    }
}