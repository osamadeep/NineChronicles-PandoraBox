using System;
using System.Collections.Generic;
using Nekoyume.EnumType;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using UnityEngine;
using ShopItem = Nekoyume.UI.Model.ShopItem;

namespace Nekoyume.UI
{
    using Nekoyume.Game;
    using Nekoyume.Model.Mail;
    using Nekoyume.Model.State;
    using Nekoyume.PandoraBox;
    using Nekoyume.State;
    using Nekoyume.UI.Scroller;
    using System.Collections;
    using TMPro;
    using UniRx;
    using UnityEngine.UI;

    public abstract class ItemTooltip : NewVerticalTooltipWidget
    {
        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        [Header("PANDORA CUSTOM FIELDS")]
        [SerializeField] private TextMeshProUGUI OwnerName;
        [SerializeField] private TextMeshProUGUI CrystalValueTxt;
        public TextMeshProUGUI MarketPriceText;
        [SerializeField] private RectTransform DiscordHolder;
        [SerializeField] private GameObject ExtraInfo;
        ShopItem currentShopItem;
        ItemBase currentItemBase; //for copy item info

        [Space(50)]
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
        [SerializeField]
        protected ItemTooltipDetail detail;

        [SerializeField] protected ConditionalButton submitButton;

        [SerializeField]
        private Button enhancementButton;

        [SerializeField]
        protected ItemTooltipBuy buy;

        [SerializeField] protected ItemTooltipSell sell;

        [SerializeField] protected Scrollbar scrollbar;

        [SerializeField] protected List<AcquisitionPlaceButton> acquisitionPlaceButtons;

        [SerializeField] protected Button descriptionButton;

        [SerializeField]
        protected GameObject submitButtonContainer;

        [SerializeField]
        protected AcquisitionPlaceDescription acquisitionPlaceDescription;

        private readonly List<IDisposable> _disposablesForModel = new();

        private RectTransform _descriptionButtonRectTransform;

        private System.Action _onSubmit;
        private System.Action _onClose;
        private System.Action _onBlocked;
        private System.Action _onEnhancement;

        public bool _isPointerOnScrollArea;
        public bool _isClickedButtonArea;

        protected override PivotPresetType TargetPivotPresetType => PivotPresetType.TopRight;


        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        protected override void Update()
        {
            base.Update();

            if (Input.GetKeyDown(KeyCode.M))
            {
                if (PandoraMaster.CurrentShopSellerAvatar != null)
                {
                    PandoraPlayer PandoraSellerPlayer = Premium.Pandoraplayers.Find(y =>
                                        y.Address.ToLower() == PandoraMaster.CurrentShopSellerAvatar.agentAddress.ToString().ToLower());
                    if (!(PandoraSellerPlayer is null)  && PandoraSellerPlayer.IsPremium())
                    {
                        Application.OpenURL("https://discordapp.com/users/" + "00000"); // PandoraSellerPlayer.DiscordID);
                    }
                    else
                        OneLineSystem.Push(MailType.System, "<color=green>Pandora Box</color>: Player Not Premium!",
                            NotificationCell.NotificationType.Alert);
                }
                else
                    OneLineSystem.Push(MailType.System, "<color=green>Pandora Box</color>: This item Belong to you!",
                        NotificationCell.NotificationType.Alert);
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                ClipboardHelper.CopyToClipboard(GetItemInfo());
                OneLineSystem.Push(MailType.System,
                    "<color=green>Pandora Box</color>: Item Info copy to Clipboard Successfully!",
                    NotificationCell.NotificationType.Information);
            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                //check client-side if cost is enough
                if (PandoraMaster.PlayFabInventory.VirtualCurrency["PG"] < PandoraMaster.PanDatabase.ShopFeaturePrice)
                {
                    NotificationSystem.Push(Nekoyume.Model.Mail.MailType.System, $"PandoraBox: You dont have {PandoraMaster.PanDatabase.ShopFeaturePrice} Pandora Gems!"
                        , NotificationCell.NotificationType.Alert);
                }
                else
                {
                    if (currentItemBase is ITradableItem tradableItem)
                    {
                        
                        if (currentItemBase is INonFungibleItem nonFungibleItem)
                        {
                            string content = $"Are you sure to spend <color=#76F3FE><b>{PandoraMaster.PanDatabase.ShopFeaturePrice} " +
                                $"Pandora Gems</b></color> to Feature this item in market for 36500 Blocks(5 days)?";
                            Find<TwoButtonSystem>().Show(content, "Yes", "No",
                            (() => {
                                Premium.BuyFeatureShop(nonFungibleItem.NonFungibleId.ToString());
                            }));
                        }
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.I))
            {
                if (!Premium.CurrentPandoraPlayer.IsPremium())
                {
                    OneLineSystem.Push(MailType.System,
                        "<color=green>Pandora Box</color>: This is Premium Feature!",
                        NotificationCell.NotificationType.Alert);
                    return;
                }

                //if (currentItemBase is INonFungibleItem nonFungibleItem)
                //{
                //    var nonFungibleId = nonFungibleItem.NonFungibleId;
                //    //Debug.LogError(nonFungibleId);
                //}

                //States.Instance.CurrentAvatarState.inventory.AddItem(currentItemBase);
                //InventoryItem xx = new InventoryItem(currentItemBase,1,true,false,true);
                if (PandoraMaster.CurrentShopSellerAvatar is null)
                {
                    States.Instance.CurrentAvatarState.inventory.AddItem(currentItemBase);
                    OneLineSystem.Push(MailType.System,
                        "<color=green>Pandora Box</color>: Item Added to your inventory Successfully!",
                        NotificationCell.NotificationType.Information);
                }
                else
                {
                    OneLineSystem.Push(MailType.System,
                        "<color=green>Pandora Box</color>: Coming Soon!",
                        NotificationCell.NotificationType.Information);
                }
            }

            if (Input.GetKeyDown(KeyCode.F))
            {
                AddRemoveFavoriteItem();
            }
        }

        public async void SetItemOwner(Guid guid)
        {
            OwnerName.text = await Premium.GetItemOwnerName(guid);
            OwnerName.gameObject.SetActive(!string.IsNullOrEmpty(OwnerName.text));
        }

        string GetItemInfo()
        {
            AvatarState ownerAvatarState = PandoraMaster.CurrentShopSellerAvatar == null ? States.Instance.CurrentAvatarState : PandoraMaster.CurrentShopSellerAvatar;

            string itemString = "===== Pandora Item Information =====";

            if (Premium.CurrentPandoraPlayer.IsPremium())
            {
                PandoraPlayer currentPandoraPlayer = PandoraMaster.GetPandoraPlayer(ownerAvatarState.agentAddress.ToString());
                itemString += "\nOwner Avatar Name    : " + ownerAvatarState.NameWithHash;
                itemString += "\nOwner Agent  Address : " + ownerAvatarState.agentAddress;
                itemString += "\nOwner Avatar Address : " + ownerAvatarState.address;
            }
            else
            {
                itemString += "\nOwner Avatar Name    : PREMIUM FEATURE";
                itemString += "\nOwner Agent  Address : PREMIUM FEATURE";
                itemString += "\nOwner Avatar Address : PREMIUM FEATURE";
            }

            itemString += "\nItem Localized Name  : " + currentItemBase.GetLocalizedName(false);
            itemString += "\nItem ID              : " + currentItemBase.Id;
            try
            {
                itemString += "\nItem Main Stats      : " + GetItemMainStats();
            }
            catch
            {
            }

            try
            {
                itemString += "\nItem Skills          : " + GetItemSkills();
            }
            catch
            {
            }

            if (currentItemBase is ITradableItem tradableItem)
            {
                if (currentItemBase is INonFungibleItem nonFungibleItem)
                {
                    itemString += "\nItem Unique ID       : " + nonFungibleItem.NonFungibleId;
                }
                try
                {
                    itemString += "\nItem Shop ID         : " + currentShopItem.OrderDigest.OrderId;
                    itemString += "\nItem Price           : " + currentShopItem.OrderDigest.Price;
                    if (currentShopItem.OrderDigest.ItemCount > 1)
                        itemString += "\nItem Count           : " + currentShopItem.OrderDigest.ItemCount;
                }
                catch { }
            }

            itemString += "\nCurrent Time (Utc)   : " + DateTime.UtcNow;

            return itemString;
        }

        void EnableShopTool()
        {
            MarketPriceText.text = PandoraMaster.MarketPriceValue;
            MarketPriceText.gameObject.SetActive(true);
            panel.GetComponent<Image>().enabled = false;
            ExtraInfo.SetActive(false);
            panel.Find("ViewGroup_Item/Group/Footer").gameObject.SetActive(false);
            panel.Find("ViewGroup_Item/Group/Content/ScrollArea/Scroll View/Viewport/Content/ItemDescriptionText")
                .GetComponent<TextMeshProUGUI>().text = "";

            panel.sizeDelta = new Vector2(380, 415);
            if (panel.anchoredPosition.y < -180) //fix when block # cover the helper tool 
                panel.anchoredPosition = new Vector2(panel.anchoredPosition.x, -180);
            DiscordHolder.gameObject.SetActive(true);
        }

        void DisableShopTool()
        {
            MarketPriceText.gameObject.SetActive(false);
            panel.GetComponent<Image>().enabled = true;
            ExtraInfo.SetActive(true);
            panel.Find("ViewGroup_Item/Group/Footer").gameObject.SetActive(true);
            panel.sizeDelta = new Vector2(380, 600);
            panel.anchoredPosition = new Vector2(panel.anchoredPosition.x, -10);
            //panel.Find("ViewGroup_Item/Group/Content/ScrollArea/Scroll View/Viewport/Content/ItemDescriptionText").GetComponent<TextMeshProUGUI>().text ="";
            //panel.sizeDelta = new Vector2(380, 600);
            DiscordHolder.gameObject.SetActive(false);
            //UpdatePosition(panel);
        }

        string GetItemMainStats()
        {
            //ToDO: Not Optimize way to get the stats
            Transform content = panel
                .Find("ViewGroup_Item/Group/Content/ScrollArea/Scroll View/Viewport/Content/OptionArea").transform;
            string tmp = "";
            StatView statView;

            if (content.Find("StatView_01").gameObject.activeInHierarchy)
            {
                statView = content.Find("StatView_01").GetComponent<StatView>();
                tmp += statView.statTypeText.text + ": " + statView.valueText.text;
            }

            if (content.Find("StatView_02").gameObject.activeInHierarchy)
            {
                statView = content.Find("StatView_02").GetComponent<StatView>();
                tmp += " || " + statView.statTypeText.text + ": " + statView.valueText.text;
            }

            if (content.Find("StatView_03").gameObject.activeInHierarchy)
            {
                statView = content.Find("StatView_03").GetComponent<StatView>();
                tmp += " || " + statView.statTypeText.text + ": " + statView.valueText.text;
            }

            if (content.Find("StatView_04").gameObject.activeInHierarchy)
            {
                statView = content.Find("StatView_04").GetComponent<StatView>();
                tmp += " || " + statView.statTypeText.text + ": " + statView.valueText.text;
            }

            return tmp;
        }

        string GetItemSkills()
        {
            //ToDO: Not Optimize way to get the stats
            Transform content = panel
                .Find("ViewGroup_Item/Group/Content/ScrollArea/Scroll View/Viewport/Content/OptionArea").transform;
            string tmp = "";
            Module.SkillView skillView;
            bool isSkill = false;

            if (content.Find("SkillView").gameObject.activeInHierarchy)
            {
                skillView = content.Find("SkillView").GetComponent<Module.SkillView>();
                tmp += skillView.nameText.text + ":( " + skillView.powerText.text + ", " + skillView.chanceText.text +
                       " )";
                isSkill = true;
            }

            if (content.Find("SkillView (1)").gameObject.activeInHierarchy)
            {
                skillView = content.Find("SkillView (1)").GetComponent<Module.SkillView>();
                tmp += " || " + skillView.nameText.text + ":( " + skillView.powerText.text + ", " +
                       skillView.chanceText.text + " )";
                isSkill = true;
            }

            if (content.Find("SkillView (2)").gameObject.activeInHierarchy)
            {
                skillView = content.Find("SkillView (2)").GetComponent<Module.SkillView>();
                tmp += " || " + skillView.nameText.text + ":( " + skillView.powerText.text + ", " +
                       skillView.chanceText.text + " )";
                isSkill = true;
            }

            if (content.Find("SkillView (3)").gameObject.activeInHierarchy)
            {
                skillView = content.Find("SkillView (3)").GetComponent<Module.SkillView>();
                tmp += " || " + skillView.nameText.text + ":( " + skillView.powerText.text + ", " +
                       skillView.chanceText.text + " )";
                isSkill = true;
            }

            if (isSkill)
                return tmp;
            else
                return "No Skill!";
        }

        public void AddRemoveFavoriteItem()
        {
            if (currentItemBase is INonFungibleItem nonFungibleItem)
            {
                var nonFungibleId = nonFungibleItem.NonFungibleId;

                if (PandoraMaster.FavItems.Contains(nonFungibleId.ToString()))
                {
                    for (int i = 0; i < PandoraMaster.FavItems.Count; i++)
                    {
                        string key = "_PandoraBox_General_FavItems0" + i + "_" +
                                     States.Instance.CurrentAvatarState.address;
                        if (i > 9)
                            key = "_PandoraBox_General_FavItems" + i + "_" + States.Instance.CurrentAvatarState.address;
                        PlayerPrefs.DeleteKey(key);
                    }

                    PandoraMaster.FavItems.Remove(nonFungibleId.ToString());
                    for (int i = 0; i < PandoraMaster.FavItems.Count; i++)
                    {
                        string key = "_PandoraBox_General_FavItems0" + i + "_" +
                                     States.Instance.CurrentAvatarState.address;
                        if (i > 9)
                            key = "_PandoraBox_General_FavItems" + i + "_" + States.Instance.CurrentAvatarState.address;
                        PlayerPrefs.SetString(key, PandoraMaster.FavItems[i]);
                    }

                    OneLineSystem.Push(MailType.System, "<color=green>Pandora Box</color>: " +
                                                        currentItemBase.GetLocalizedName()
                                                        + " Removed from your Favorite list!",
                        NotificationCell.NotificationType.Information);
                }
                else
                {
                    int maxCount = 2;
                    if (Premium.CurrentPandoraPlayer.IsPremium())
                        maxCount = 15;

                    if (PandoraMaster.FavItems.Count > maxCount)
                        OneLineSystem.Push(MailType.System,
                            "<color=green>Pandora Box</color>: You reach <color=red>Maximum</color> number of Favorite, please remove some!"
                            , NotificationCell.NotificationType.Information);
                    else
                    {
                        PandoraMaster.FavItems.Add(nonFungibleId.ToString());
                        OneLineSystem.Push(MailType.System,
                            "<color=green>Pandora Box</color>: " + currentItemBase.GetLocalizedName() +
                            " added to your Favorite list!"
                            , NotificationCell.NotificationType.Information);
                        for (int i = 0; i < PandoraMaster.FavItems.Count; i++)
                        {
                            string key = "_PandoraBox_General_FavItems0" + i + "_" +
                                         States.Instance.CurrentAvatarState.address;
                            if (i > 9)
                                key = "_PandoraBox_General_FavItems" + i + "_" +
                                      States.Instance.CurrentAvatarState.address;
                            PlayerPrefs.SetString(key, PandoraMaster.FavItems[i]);
                        }
                    }
                }
            }
        }

        void CheckCrystal(ItemBase item)
        {
            CrystalValueTxt.gameObject.SetActive(item.ItemType == ItemType.Equipment);
            if (item.ItemType == ItemType.Equipment)
            {
                List<Equipment> grindList = new List<Equipment>();
                grindList.Add(item as Equipment);
                CrystalValueTxt.text = "<color=green>+" + CrystalCalculator.CalculateCrystal(grindList,
                    false,
                    TableSheets.Instance.CrystalEquipmentGrindingSheet,
                    TableSheets.Instance.CrystalMonsterCollectionMultiplierSheet,
                    States.Instance.StakingLevel).MajorUnit + "</color>";
            }
        }


        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||


        protected override void Awake()
        {
            base.Awake();
            submitButton.OnSubmitSubject.Subscribe(_ =>
            {
                _onSubmit?.Invoke();
                Close();
            }).AddTo(gameObject);
            submitButton.OnClickDisabledSubject.Subscribe(_ => _onBlocked?.Invoke())
                .AddTo(gameObject);
            enhancementButton.onClick.AddListener(() =>
            {
                _onEnhancement?.Invoke();
                Close(true);
            });
            CloseWidget = () => Close();
            SubmitWidget = () =>
            {
                if (!submitButton.IsSubmittable)
                    return;

                AudioController.PlayClick();
                _onSubmit?.Invoke();
                Close();
            };

            if (descriptionButton != null)
            {
                _descriptionButtonRectTransform = descriptionButton.GetComponent<RectTransform>();
                descriptionButton.onClick.AddListener(() =>
                {
                    acquisitionPlaceDescription.Show(panel, _descriptionButtonRectTransform);
                });
            }
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            _onClose?.Invoke();
            _isPointerOnScrollArea = false;
            _isClickedButtonArea = false;
            _disposablesForModel.DisposeAllAndClear();
            base.Close(ignoreCloseAnimation);
        }

        //Show from: UI_ShopSell_MaterialView
        public virtual void Show(
            ItemBase item,
            string submitText,
            bool interactable,
            System.Action onSubmit,
            System.Action onClose = null,
            System.Action onBlocked = null,
            int itemCount = 0)
        {
            buy.gameObject.SetActive(false);
            sell.gameObject.SetActive(false);
            acquisitionPlaceButtons.ForEach(button => button.gameObject.SetActive(false));
            detail.Set(
                item,
                itemCount,
                !Util.IsUsableItem(item) &&
                (item.ItemType == ItemType.Equipment ||
                 item.ItemType == ItemType.Costume));

            submitButtonContainer.SetActive(onSubmit != null);
            submitButton.Interactable = interactable;
            submitButton.Text = submitText;
            _onSubmit = onSubmit;
            _onClose = onClose;
            _onBlocked = onBlocked;

            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            CheckCrystal(item);
            PandoraMaster.CurrentShopSellerAvatar = null;
            OwnerName.gameObject.SetActive(false);
            currentItemBase = item;
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
            scrollbar.value = 1f;
            base.Show();
            StartCoroutine(CoUpdate(submitButtonContainer.gameObject));

            //|||||||||||||| PANDORA CODE |||||||||||||||||||
            if (PandoraMaster.MarketPriceHelper)
                EnableShopTool();
            else
                DisableShopTool();
        }

        //Show from: UI_AvatarInfoPopup,UI_BattlePreparation,UI_RankingBoard,UI_FriendInfoPopupPandora,UI_ShopSell_LeftSide
        public virtual void Show(
            InventoryItem item,
            string submitText,
            bool interactable,
            System.Action onSubmit,
            System.Action onClose = null,
            System.Action onBlocked = null,
            System.Action onEnhancement = null)
        {
            buy.gameObject.SetActive(false);
            sell.gameObject.SetActive(false);
            acquisitionPlaceButtons.ForEach(button => button.gameObject.SetActive(false));
            detail.Set(
                item.ItemBase,
                item.Count.Value,
                !Util.IsUsableItem(item.ItemBase) &&
                (item.ItemBase.ItemType == ItemType.Equipment ||
                 item.ItemBase.ItemType == ItemType.Costume));

            submitButtonContainer.SetActive(onSubmit != null);
            submitButton.Interactable = interactable;
            submitButton.Text = submitText;
            _onSubmit = onSubmit;
            _onClose = onClose;
            _onBlocked = onBlocked;
            _onEnhancement = onEnhancement;
            enhancementButton.gameObject.SetActive(onEnhancement != null);

            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            CheckCrystal(item.ItemBase);
            PandoraMaster.CurrentShopSellerAvatar = null;
            OwnerName.gameObject.SetActive(false);
            currentItemBase = item.ItemBase;
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

            scrollbar.value = 1f;
            base.Show();
            StartCoroutine(CoUpdate(submitButtonContainer.gameObject));
            //|||||||||||||| PANDORA CODE |||||||||||||||||||
            if (PandoraMaster.MarketPriceHelper)
                EnableShopTool();
            else
                DisableShopTool();
        }

        //Show from: UI_ShopSell,UI_ShopSell_MaterialView
        public virtual void Show(
            ShopItem item,
            System.Action onRegister,
            System.Action onSellCancellation,
            System.Action onClose)
        {
            submitButtonContainer.SetActive(false);
            buy.gameObject.SetActive(false);
            sell.gameObject.SetActive(true);
            sell.Set(item.OrderDigest.ExpiredBlockIndex,
                () =>
                {
                    onSellCancellation?.Invoke();
                    Close();
                }, () =>
                {
                    onRegister?.Invoke();
                    Close();
                });
            detail.Set(
                item.ItemBase,
                item.OrderDigest.ItemCount,
                !Util.IsUsableItem(item.ItemBase) &&
                (item.ItemBase.ItemType == ItemType.Equipment ||
                 item.ItemBase.ItemType == ItemType.Costume));
            _onClose = onClose;


            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            CheckCrystal(item.ItemBase);
            currentShopItem = item;
            currentItemBase = item.ItemBase;
            var order = Util.GetOrder(item.OrderDigest.OrderId);
            OwnerName.gameObject.SetActive(false);
#if UNITY_EDITOR
            Debug.LogError(item.OrderDigest.OrderId);
#endif
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||


            scrollbar.value = 1f;
            base.Show();
            StartCoroutine(CoUpdate(sell.gameObject));
            //|||||||||||||| PANDORA CODE |||||||||||||||||||
            if (PandoraMaster.MarketPriceHelper)
                EnableShopTool();
            else
                DisableShopTool();
        }

        //Show from: UI_ShopBuy,UI_ShopBuy_MaterialView
        public virtual void Show(
            ShopItem item,
            System.Action onBuy,
            System.Action onClose)
        {
            submitButtonContainer.SetActive(false);
            sell.gameObject.SetActive(false);
            buy.gameObject.SetActive(true);
            buy.Set(item.OrderDigest.ExpiredBlockIndex,
                item.OrderDigest.Price,
                () =>
                {
                    onBuy?.Invoke();
                    Close();
                });

            detail.Set(
                item.ItemBase,
                item.OrderDigest.ItemCount,
                !Util.IsUsableItem(item.ItemBase) &&
                (item.ItemBase.ItemType == ItemType.Equipment ||
                 item.ItemBase.ItemType == ItemType.Costume));
            _onClose = onClose;

            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            CheckCrystal(item.ItemBase);
            currentShopItem = item;
            OwnerName.gameObject.SetActive(false);
            currentItemBase = item.ItemBase;
            SetItemOwner(item.OrderDigest.OrderId);
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

            scrollbar.value = 1f;
            base.Show();
            StartCoroutine(CoUpdate(buy.gameObject));

            //|||||||||||||| PANDORA CODE |||||||||||||||||||
            if (PandoraMaster.MarketPriceHelper)
                EnableShopTool();
            else
                DisableShopTool();
        }

        //Show from: UI_Enhancement
        public virtual void Show(
            EnhancementInventoryItem item,
            string submitText,
            bool interactable,
            System.Action onSubmit,
            System.Action onClose = null,
            System.Action onBlocked = null)
        {
            enhancementButton.gameObject.SetActive(false);
            buy.gameObject.SetActive(false);
            sell.gameObject.SetActive(false);
            detail.Set(
                item.ItemBase,
                1,
                !Util.IsUsableItem(item.ItemBase) &&
                (item.ItemBase.ItemType == ItemType.Equipment ||
                 item.ItemBase.ItemType == ItemType.Costume));

            submitButtonContainer.SetActive(onSubmit != null);
            submitButton.Interactable = interactable;
            submitButton.Text = submitText;
            _onSubmit = onSubmit;
            _onClose = onClose;
            _onBlocked = onBlocked;

            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            CheckCrystal(item.ItemBase);
            OwnerName.gameObject.SetActive(false);
            PandoraMaster.CurrentShopSellerAvatar = null;
            currentItemBase = item.ItemBase;
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
            scrollbar.value = 1f;
            base.Show();
            StartCoroutine(CoUpdate(submitButtonContainer.gameObject));
        }

        public static ItemTooltip Find(ItemType type)
        {
            return type switch
            {
                ItemType.Consumable => Find<ConsumableTooltip>(),
                ItemType.Costume => Find<CostumeTooltip>(),
                ItemType.Equipment => Find<EquipmentTooltip>(),
                ItemType.Material => Find<MaterialTooltip>(),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, $"invalid ItemType : {type}")
            };
        }

        //protected void UpdatePosition(RectTransform target)
        //{
        //    LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
        //    panel.SetAnchorAndPivot(AnchorPresetType.TopLeft, PivotPresetType.TopLeft);
        //    LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)verticalLayoutGroup.transform);
        //    if (target)
        //    {
        //        panel.MoveToRelatedPosition(target, TargetPivotPresetType, OffsetFromTarget);
        //    }
        //    else
        //    {
        //        panel.SetAnchor(AnchorPresetType.MiddleCenter);
        //        panel.anchoredPosition =
        //            new Vector2(-(panel.sizeDelta.x / 2), panel.sizeDelta.y / 2);
        //    }

        //    panel.MoveInsideOfParent(MarginFromParent);

        //    if (!(target is null) && panel.position.x - target.position.x < 0)
        //    {
        //        panel.SetAnchorAndPivot(AnchorPresetType.TopRight, PivotPresetType.TopRight);
        //        panel.MoveToRelatedPosition(target, TargetPivotPresetType.ReverseX(),
        //            DefaultOffsetFromTarget.ReverseX());
        //        UpdateAnchoredPosition(target);
        //    }
        //}

        protected IEnumerator CoUpdate(GameObject target)
        {
            var selectedGameObjectCache = TouchHandler.currentSelectedGameObject;
            while (selectedGameObjectCache is null)
            {
                selectedGameObjectCache = TouchHandler.currentSelectedGameObject;
                yield return null;
            }

            var positionCache = selectedGameObjectCache.transform.position;

            while (enabled)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    _isClickedButtonArea = _isPointerOnScrollArea;
                }

                var current = TouchHandler.currentSelectedGameObject;
                if (current == selectedGameObjectCache)
                {
                    if (!Input.GetMouseButton(0) &&
                        Input.mouseScrollDelta == default)
                    {
                        yield return null;
                        continue;
                    }

                    if (!_isClickedButtonArea)
                    {
                        Close();
                        yield break;
                    }
                }
                else
                {
                    if (current == target)
                    {
                        yield break;
                    }

                    if (!_isClickedButtonArea)
                    {
                        Close();
                        yield break;
                    }
                }

                yield return null;
            }
        }

        public void OnEnterButtonArea(bool value)
        {
            _isPointerOnScrollArea = value;
        }
    }
}
