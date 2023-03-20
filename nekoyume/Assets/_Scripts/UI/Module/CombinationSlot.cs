using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.EnumType;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    using Cysharp.Threading.Tasks;
    using Nekoyume.PandoraBox;
    using Nekoyume.UI.Scroller;
    using UniRx;

    public class CombinationSlot : MonoBehaviour
    {
        private const int UnlockStage =
            GameConfig.RequireClearedStageLevel.CombinationEquipmentAction;

        public enum SlotType
        {
            Lock,
            Empty,
            Appraise,
            Working,
            WaitingReceive,
        }

        public enum CacheType
        {
            Appraise,
            WaitingReceive,
        }

        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        [Header("PANDORA CUSTOM FIELDS")] [SerializeField]
        private Button showItemButton;

        [Space(50)]
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
        [SerializeField]
        private SimpleItemView itemView;

        [SerializeField] private SimpleItemView waitingReceiveItemView;

        [SerializeField] private TouchHandler touchHandler;

        [SerializeField] private Slider progressBar;

        [SerializeField] private Image hasNotificationImage;

        [SerializeField] private TextMeshProUGUI lockText;

        [SerializeField] private TextMeshProUGUI requiredBlockIndexText;

        [SerializeField] private TextMeshProUGUI itemNameText;

        [SerializeField] private TextMeshProUGUI hourglassCountText;

        [SerializeField] private TextMeshProUGUI preparingText;

        [SerializeField] private TextMeshProUGUI waitingReceiveText;

        [SerializeField] private GameObject lockContainer;

        [SerializeField] private GameObject baseContainer;

        [SerializeField] private GameObject noneContainer;

        [SerializeField] private GameObject preparingContainer;

        [SerializeField] private GameObject workingContainer;

        [SerializeField] private GameObject waitReceiveContainer;


        private CombinationSlotState _state;
        private CacheType _cachedType;
        private int _slotIndex;

        private readonly List<IDisposable> _disposablesOfOnEnable = new();

        private readonly Dictionary<Address, bool> cached = new();

        public SlotType Type { get; private set; } = SlotType.Empty;

        public void SetCached(
            Address avatarAddress,
            bool value,
            long requiredBlockIndex,
            SlotType slotType,
            ItemUsable itemUsable = null)
        {
            if (cached.ContainsKey(avatarAddress))
            {
                cached[avatarAddress] = value;
            }
            else
            {
                cached.Add(avatarAddress, value);
            }

            var currentBlockIndex = Game.Game.instance.Agent.BlockIndex;
            switch (slotType)
            {
                case SlotType.Appraise:
                    if (itemUsable == null)
                    {
                        break;
                    }

                    _cachedType = CacheType.Appraise;
                    UpdateItemInformation(itemUsable, slotType);
                    UpdateRequiredBlockInformation(
                        requiredBlockIndex + currentBlockIndex,
                        currentBlockIndex,
                        currentBlockIndex);
                    break;

                case SlotType.WaitingReceive:
                    _cachedType = CacheType.WaitingReceive;
                    UpdateInformation(Type, currentBlockIndex, _state, IsCached(avatarAddress));
                    break;
            }
        }

        private bool IsCached(Address avatarAddress)
        {
            return cached.ContainsKey(avatarAddress) && cached[avatarAddress];
        }

        private void Awake()
        {
            touchHandler.OnClick.Subscribe(_ =>
            {
                AudioController.PlayClick();
                OnClickSlot(Type, _state, _slotIndex, Game.Game.instance.Agent.BlockIndex);
            }).AddTo(gameObject);

            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            showItemButton.onClick.AddListener(() => { ShowItem(); });
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
        }

        private void OnEnable()
        {
            Game.Game.instance.Agent.BlockIndexSubject
                .ObserveOnMainThread()
                .Subscribe(SubscribeOnBlockIndex)
                .AddTo(_disposablesOfOnEnable);
            ReactiveAvatarState.Inventory
                .Select(_ => Game.Game.instance.Agent.BlockIndex)
                .Subscribe(SubscribeOnBlockIndex)
                .AddTo(_disposablesOfOnEnable);
        }

        private void OnDisable()
        {
            _disposablesOfOnEnable.DisposeAllAndClear();
        }

        public void SetSlot(
            Address avatarAddress,
            long currentBlockIndex,
            int slotIndex,
            CombinationSlotState state = null)
        {
            _slotIndex = slotIndex;
            _state = state;
            Type = GetSlotType(state, currentBlockIndex, IsCached(avatarAddress));
            UpdateInformation(Type, currentBlockIndex, state, IsCached(avatarAddress));
        }

        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        void ShowItem()
        {
            if (_state is null)
            {
                PandoraUtil.ShowSystemNotification("Item Not Ready Yet, please try again in few seconds!",
                    NotificationCell.NotificationType.Alert);
                return;
            }

            PandoraUtil.ShowBaseItemTooltip(new Address(), _state.Result.itemUsable).Forget();
        }
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||


        private void SubscribeOnBlockIndex(long currentBlockIndex)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            Type = GetSlotType(_state, currentBlockIndex, IsCached(avatarAddress));
            UpdateInformation(Type, currentBlockIndex, _state, IsCached(avatarAddress));
        }

        private void UpdateInformation(
            SlotType type,
            long currentBlockIndex,
            CombinationSlotState state,
            bool isCached)
        {
            switch (type)
            {
                case SlotType.Lock:
                    SetContainer(true, false, false, false);
                    var text = L10nManager.Localize("UI_UNLOCK_CONDITION_STAGE");
                    lockText.text = string.Format(text, UnlockStage);
                    break;

                case SlotType.Empty:
                    SetContainer(false, false, true, false);
                    itemView.Clear();
                    break;

                case SlotType.Appraise:
                    SetContainer(false, true, false, false);
                    preparingContainer.gameObject.SetActive(true);
                    workingContainer.gameObject.SetActive(false);
                    if (state != null)
                    {
                        UpdateItemInformation(state.Result.itemUsable, type);
                        UpdateHourglass(state, currentBlockIndex);
                        UpdateRequiredBlockInformation(
                            state.UnlockBlockIndex,
                            state.StartBlockIndex,
                            currentBlockIndex);
                    }

                    hasNotificationImage.enabled = false;
                    break;

                case SlotType.Working:
                    SetContainer(false, true, false, false);
                    preparingContainer.gameObject.SetActive(false);
                    workingContainer.gameObject.SetActive(true);
                    UpdateItemInformation(state.Result.itemUsable, type);
                    UpdateHourglass(state, currentBlockIndex);
                    UpdateRequiredBlockInformation(
                        state.UnlockBlockIndex,
                        state.StartBlockIndex,
                        currentBlockIndex);
                    UpdateNotification(state, currentBlockIndex, isCached);
                    break;

                case SlotType.WaitingReceive:
                    SetContainer(false, false, false, true);
                    waitingReceiveItemView.SetData(new Item(state.Result.itemUsable));
                    waitingReceiveText.text = string.Format(
                        L10nManager.Localize("UI_SENDING_THROUGH_MAIL"),
                        state.Result.itemUsable.GetLocalizedName(
                            useElementalIcon: false,
                            ignoreLevel: true));
                    break;
            }
        }

        private void SetContainer(
            bool isLock,
            bool isWorking,
            bool isEmpty,
            bool isWaitingReceive)
        {
            lockContainer.gameObject.SetActive(isLock);
            baseContainer.gameObject.SetActive(isWorking);
            noneContainer.gameObject.SetActive(isEmpty);
            waitReceiveContainer.gameObject.SetActive(isWaitingReceive);
        }

        private SlotType GetSlotType(
            CombinationSlotState state,
            long currentBlockIndex,
            bool isCached)
        {
            var isLock = !States.Instance
                .CurrentAvatarState?.worldInformation.IsStageCleared(UnlockStage) ?? true;
            if (isLock)
            {
                return SlotType.Lock;
            }

            if (isCached)
            {
                return _cachedType == CacheType.Appraise
                    ? SlotType.Appraise
                    : SlotType.WaitingReceive;
            }

            if (state?.Result is null)
            {
                return SlotType.Empty;
            }

            return currentBlockIndex < state.StartBlockIndex +
                States.Instance.GameConfigState.RequiredAppraiseBlock
                    ? SlotType.Appraise
                    : SlotType.Working;
        }

        private void UpdateRequiredBlockInformation(
            long unlockBlockIndex,
            long startBlockIndex,
            long currentBlockIndex)
        {
            progressBar.maxValue = Math.Max(unlockBlockIndex - startBlockIndex, 1);
            var diff = Math.Max(unlockBlockIndex - currentBlockIndex, 1);
            progressBar.value = diff;
            //requiredBlockIndexText.text = $"{diff}.";
            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            if (PandoraBox.PandoraMaster.Instance.Settings.BlockShowType == 0)
            {
                var timeR = Util.GetBlockToTime((int)diff);
                requiredBlockIndexText.text = timeR;
            }
            else if (PandoraBox.PandoraMaster.Instance.Settings.BlockShowType == 1)
            {
                requiredBlockIndexText.text = $"{diff}";
            }
            else
            {
                var timeR = Util.GetBlockToTime((int)diff);
                requiredBlockIndexText.text = $"{timeR} ({diff})";
            }
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
        }

        private void UpdateNotification(
            CombinationSlotState state,
            long currentBlockIndex,
            bool isCached)
        {
            if (GetSlotType(state, currentBlockIndex, isCached) != SlotType.Working)
            {
                hasNotificationImage.enabled = false;
                return;
            }

            var gameConfigState = Game.Game.instance.States.GameConfigState;
            var diff = state.RequiredBlockIndex - currentBlockIndex;
            var cost = RapidCombination0.CalculateHourglassCount(gameConfigState, diff);
            var row = Game.Game.instance.TableSheets.MaterialItemSheet
                .OrderedList
                .First(r => r.ItemSubType == ItemSubType.Hourglass);
            var isEnough = States.Instance.CurrentAvatarState.inventory
                .HasFungibleItem(row.ItemId, currentBlockIndex, cost);
            hasNotificationImage.enabled = isEnough;
        }

        private void UpdateHourglass(CombinationSlotState state, long currentBlockIndex)
        {
            var diff = state.UnlockBlockIndex - currentBlockIndex;
            var cost =
                RapidCombination0.CalculateHourglassCount(States.Instance.GameConfigState, diff);
            var inventory = States.Instance.CurrentAvatarState.inventory;
            var count = Util.GetHourglassCount(inventory, currentBlockIndex);
            hourglassCountText.text = cost.ToString();
            hourglassCountText.color = count >= cost
                ? Palette.GetColor(ColorType.ButtonEnabled)
                : Palette.GetColor(ColorType.TextDenial);
        }

        private void UpdateItemInformation(ItemUsable item, SlotType slotType)
        {
            if (slotType == SlotType.Working)
            {
                itemView.SetData(new Item(item));
            }
            else
            {
                itemView.SetDataExceptOptionTag(new Item(item));
            }

            itemNameText.text = TextHelper.GetItemNameInCombinationSlot(item);
            preparingText.text = string.Format(
                L10nManager.Localize("UI_COMBINATION_SLOT_IDENTIFYING"),
                item.GetLocalizedName(useElementalIcon: false, ignoreLevel: true));
        }

        private static void OnClickSlot(
            SlotType type,
            CombinationSlotState state,
            int slotIndex,
            long currentBlockIndex)
        {
            switch (type)
            {
                case SlotType.Empty:
                    if (Game.Game.instance.IsInWorld)
                    {
                        UI.NotificationSystem.Push(
                            Nekoyume.Model.Mail.MailType.System,
                            L10nManager.Localize("UI_BLOCK_EXIT"),
                            NotificationCell.NotificationType.Alert);
                        return;
                    }

                    var widgetLayerRoot = MainCanvas.instance
                        .GetLayerRootTransform(WidgetType.Widget);
                    var statusWidget = Widget.Find<Status>();
                    foreach (var widget in MainCanvas.instance.Widgets
                                 .Where(widget =>
                                     widget.isActiveAndEnabled
                                     && widget.transform.parent.Equals(widgetLayerRoot)
                                     && !widget.Equals(statusWidget)))
                    {
                        widget.Close(true);
                    }

                    Widget.Find<Craft>()?.gameObject.SetActive(false);
                    Widget.Find<Enhancement>()?.gameObject.SetActive(false);
                    Widget.Find<HeaderMenuStatic>()
                        .UpdateAssets(HeaderMenuStatic.AssetVisibleState.Combination);
                    Widget.Find<CombinationMain>().Show();
                    Widget.Find<CombinationSlotsPopup>().Close();
                    break;

                case SlotType.Working:
                    Widget.Find<CombinationSlotPopup>().Show(state, slotIndex, currentBlockIndex);
                    break;

                case SlotType.Appraise:
                    Widget.Find<CombinationSlotPopup>().Show(state, slotIndex, currentBlockIndex);
                    //NotificationSystem.Push(
                    //    Nekoyume.Model.Mail.MailType.System,
                    //    L10nManager.Localize("UI_COMBINATION_NOTIFY_IDENTIFYING"),
                    //    NotificationCell.NotificationType.Information);
                    break;
            }
        }
    }
}