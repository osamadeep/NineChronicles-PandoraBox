using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.BlockChain;
using Nekoyume.EnumType;
using Nekoyume.Extensions;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.Model.BattleStatus;
using Nekoyume.State;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using mixpanel;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.EnumType;
using Nekoyume.TableData;
using EventType = Nekoyume.EnumType.EventType;
using Toggle = Nekoyume.UI.Module.Toggle;

namespace Nekoyume.UI
{
    using Nekoyume.Model.State;
    using Nekoyume.PandoraBox;
    using Nekoyume.UI.Scroller;
    using Scroller;
    using UniRx;

    public class BattlePreparation : Widget
    {
        [Serializable]
        public class EventDungeonBg
        {
            public EventType eventType;
            public GameObject background;
        }

        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        [Header("PANDORA CUSTOM FIELDS")]
        public TextMeshProUGUI[] winStarTexts = null;
        public Button MultipleSimulateButton = null;
        [SerializeField] private Button soloSimulateButton = null;
        [SerializeField] private GameObject stageWinRate;
        [SerializeField] private Button maxDungeonBtn = null;

        [SerializeField] private BonusBuffViewDataScriptableObject bonusBuffViewData;
        [SerializeField] private Image simulatorGradeIconImage; //SS, S , A , ...
        [SerializeField] private Image simulatorGradeBgImage;
        [SerializeField] private Image simulatorBuffIconImage;
        [SerializeField] private Button showSimulatorBuffBtn;

        [Space(50)]
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

        [SerializeField]
        private AvatarInformation information;

        [SerializeField] private TextMeshProUGUI closeButtonText;

        [SerializeField] private ParticleSystem[] particles;

        [SerializeField]
        private ConditionalCostButton startButton;

        [SerializeField]
        private BonusBuffButton randomBuffButton;

        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private Transform buttonStarImageTransform;

        [SerializeField] private Toggle repeatToggle; // It is not currently in use

        [SerializeField, Range(.5f, 3.0f)] private float animationTime = 1f;

        [SerializeField] private bool moveToLeft = false;

        [SerializeField, Range(0f, 10f),
         Tooltip("Gap between start position X and middle position X")]
        private float middleXGap = 1f;

        [SerializeField] private GameObject coverToBlockClick = null;

        [SerializeField] private Button sweepPopupButton;

        [SerializeField]
        private TextMeshProUGUI sweepButtonText;

        [SerializeField]
        private TextMeshProUGUI enemyCp;

        [SerializeField]
        private Button boostPopupButton;

        [SerializeField] private GameObject mimisbrunnrBg;

        [SerializeField]
        private EventDungeonBg[] eventDungeonBgs;

        [SerializeField]
        private GameObject hasBg;

        [SerializeField] private GameObject blockStartingTextObject;

        [SerializeField]
        private GameObject enemyCpContainer;

        private StageType _stageType;
        private int? _scheduleId;
        private int _worldId;
        private int _stageId;
        private int _requiredCost;
        private bool _trackGuideQuest;

        private readonly List<IDisposable> _disposables = new();

        public override bool CanHandleInputEvent =>
            base.CanHandleInputEvent &&
            (startButton.Interactable || !EnoughToPlay);

        private bool EnoughToPlay => _stageType switch
        {
            StageType.EventDungeon =>
                RxProps.EventDungeonTicketProgress.Value.currentTickets >= _requiredCost,
            _ =>
                States.Instance.CurrentAvatarState.actionPoint >= _requiredCost,
        };

        private bool IsFirstStage => _stageType switch
        {
            StageType.EventDungeon => _stageId.ToEventDungeonStageNumber() == 1,
            _ => _stageId == 1,
        };

        #region override

        protected override void Awake()
        {
            closeButton.onClick.AddListener(() =>
            {
                Close(true);
                AudioController.PlayClick();
            });

            CloseWidget = () => Close(true);
            base.Awake();
        }

        public override void Initialize()
        {
            base.Initialize();

            information.Initialize();

            startButton.OnSubmitSubject
                .Where(_ => !Game.Game.instance.IsInWorld)
                .ThrottleFirst(TimeSpan.FromSeconds(1f))
                .Subscribe(_ => OnClickBattle())
                .AddTo(gameObject);

            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            //sweepButton.OnClickAsObservable().Subscribe(_ => Sweep()).AddTo(gameObject);
            //fillButton.OnClickAsObservable().Subscribe(_ => AvailableAP()).AddTo(gameObject);
            showSimulatorBuffBtn.OnClickAsObservable().Subscribe(_ => OpenSimulatorBuff()).AddTo(gameObject);
            soloSimulateButton.OnClickAsObservable().Subscribe(_ => SimulateBattlePandora()).AddTo(gameObject);
            MultipleSimulateButton.OnClickAsObservable().Subscribe(_ => MultipleSimulate()).AddTo(gameObject);
            maxDungeonBtn.OnClickAsObservable().Subscribe(_ => StartCoroutine(MaxDungeon())).AddTo(gameObject);
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

            sweepPopupButton.OnClickAsObservable()
                .Where(_ => !IsFirstStage)
                .Subscribe(_ => Find<SweepPopup>().Show(_worldId, _stageId, SendBattleAction));

            boostPopupButton.OnClickAsObservable()
                .Where(_ => EnoughToPlay && !Game.Game.instance.IsInWorld)
                .Subscribe(_ => ShowBoosterPopup());

            boostPopupButton.OnClickAsObservable().Where(_ => !EnoughToPlay && !Game.Game.instance.IsInWorld)
                .ThrottleFirst(TimeSpan.FromSeconds(1f))
                .Subscribe(_ =>
                    OneLineSystem.Push(
                        MailType.System,
                        L10nManager.Localize("ERROR_ACTION_POINT"),
                        NotificationCell.NotificationType.Alert))
                .AddTo(gameObject);

            Game.Event.OnRoomEnter.AddListener(b => Close());
        }

        public void Show(
            StageType stageType,
            int worldId,
            int stageId,
            string closeButtonName,
            bool ignoreShowAnimation = false,
            bool showByGuideQuest = false)
        {
            base.Show(ignoreShowAnimation);
            _trackGuideQuest = showByGuideQuest;
            Analyzer.Instance.Track("Unity/Click Stage", new Dictionary<string, Value>()
            {
                ["AvatarAddress"] = States.Instance.CurrentAvatarState.address.ToString(),
                ["AgentAddress"] = States.Instance.CurrentAvatarState.agentAddress.ToString(),
            });

            repeatToggle.isOn = false;
            repeatToggle.interactable = true;

            _stageType = stageType;
            _worldId = worldId;
            _stageId = stageId;

            UpdateBackground();

            UpdateStartButton();
            var cp = UpdateCp();
            information.UpdateInventory(BattleType.Adventure, cp);
            UpdateRequiredCostByStageId();
            UpdateRandomBuffButton();

            closeButtonText.text = closeButtonName;
            sweepButtonText.text =
                States.Instance.CurrentAvatarState.worldInformation.IsStageCleared(stageId)
                    ? "Sweep"
                    : "Repeat";
            startButton.gameObject.SetActive(true);
            startButton.Interactable = true;
            coverToBlockClick.SetActive(false);
            ShowHelpTooltip(_stageType);

            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            stageWinRate.SetActive(false);
            maxDungeonBtn.gameObject.SetActive(_stageType == StageType.EventDungeon);
            maxDungeonBtn.interactable = (_stageType == StageType.EventDungeon);
            maxDungeonBtn.GetComponentInChildren<TextMeshProUGUI>().text = $"Max ({RxProps.EventDungeonTicketProgress.Value.currentTickets})";
            UpdateSimulateBuff();
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

            switch (_stageType)
            {
                case StageType.HackAndSlash:
                    stageWinRate.SetActive(true);//|||||||||||||| PANDORA CODE |||||||||||||||||||
                    break;
                case StageType.Mimisbrunnr:
                    ReactiveAvatarState.ActionPoint
                        .Subscribe(_ => UpdateStartButton())
                        .AddTo(_disposables);
                    break;
                case StageType.EventDungeon:
                    RxProps.EventScheduleRowForDungeon
                        .Subscribe(value => _scheduleId = value?.Id)
                        .AddTo(_disposables);
                    RxProps.EventDungeonTicketProgress
                        .Subscribe(_ => UpdateStartButton())
                        .AddTo(_disposables);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            ReactiveAvatarState.Inventory.Subscribe(_ => UpdateStartButton()).AddTo(_disposables);
            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            //sweepButton.gameObject.SetActive(true);

            //var sprite1 = Resources.Load<Sprite>("Character/PlayerSpineTexture/Weapon/10151001");
            //if (PandoraBoxMaster.CurrentPandoraPlayer.SwordSkin == 1)
            //    _player.SpineController.UpdateWeapon(10151001, sprite1, PandoraBoxMaster.Instance.CosmicSword);

            foreach (var item in winStarTexts)
            {
                item.text = "?";
            }

            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
        }

        //|||||||||||||| PANDORA START CODE |||||||||||||||||||

        public void OpenSimulatorBuff()
        {
            if (!Premium.PANDORA_CheckPremium())
                return;
            Widget.Find<BuffBonusPopup>().Show();
            Widget.Find<BuffBonusPopup>().OnClickBuffListButton();
        }

        public void UpdateSimulateBuff()
        {
            var skillId = PlayerPrefs.GetInt("_PandoraBox_PVE_SelectedCrystalBuffSkillId", -1);
            simulatorBuffIconImage.transform.parent.gameObject.SetActive(skillId != -1);
            if (skillId == -1)
            {
                simulatorGradeBgImage.sprite = bonusBuffViewData.GetBonusBuffGradeData(TableData.Crystal.CrystalRandomBuffSheet.Row.BuffRank.B).BgSprite;
            }
            else
            {
                var skillSheet = Game.Game.instance.TableSheets.SkillSheet;
                if (skillSheet.TryGetValue(skillId, out var skillRow))
                {
                    
                    var skillRank = (TableData.Crystal.CrystalRandomBuffSheet.Row.BuffRank)(PlayerPrefs.GetInt("_PandoraBox_PVE_SelectedCrystalBuffRank", 1));

                    simulatorBuffIconImage.sprite = bonusBuffViewData.GetBonusBuffIcon(skillRow.SkillCategory);
                    var gradeData = bonusBuffViewData.GetBonusBuffGradeData(skillRank);
                    simulatorGradeBgImage.sprite = gradeData.BgSprite;
                    simulatorGradeIconImage.sprite = gradeData.IconSprite;
                }
            }

        }

        IEnumerator MaxDungeon()
        {
            maxDungeonBtn.interactable = false;
            var itemSlotState = States.Instance.ItemSlotStates[BattleType.Adventure];
            var costumes = itemSlotState.Costumes;
            var equipments = itemSlotState.Equipments;
            var runeInfos = States.Instance.RuneSlotStates[BattleType.Adventure]
                .GetEquippedRuneSlotInfos();
            var consumables = information.GetEquippedConsumables();

            int count = RxProps.EventDungeonTicketProgress.Value.currentTickets;
            for (int i = 0; i < count; i++)
            {
                //ActionManager.Instance.EventDungeonBattle(
                //_scheduleId.Value,
                //_worldId,
                //_stageId,
                //equipments,
                //costumes,
                //consumables,
                //false).Subscribe();
                ActionManager.Instance.EventDungeonBattle(
                _scheduleId.Value,
                _worldId,
                _stageId,
                equipments,
                costumes,
                consumables,
                runeInfos,
                false,
                _trackGuideQuest).Subscribe();

                OneLineSystem.Push(MailType.System,
                "<color=green>Pandora Box</color>: Sending Dungeon <color=red>" + _stageId
                + "</color> <color=green>" + (i + 1) + "</color>/" + count + "...",
                NotificationCell.NotificationType.Information);
                yield return new WaitForSeconds(1);
            }
        }
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

        private int? UpdateCp()
        {
            switch (_stageType)
            {
                case StageType.HackAndSlash:
                    var sweepRequiredCpSheet = TableSheets.Instance.SweepRequiredCPSheet;
                    if (!sweepRequiredCpSheet.TryGetValue(_stageId, out var row))
                    {
                        return null;
                    }

                    enemyCpContainer.gameObject.SetActive(true);
                    var cp = row.RequiredCP;
                    enemyCp.text = $"{cp}";
                    return cp;
                case StageType.Mimisbrunnr:
                case StageType.EventDungeon:
                    enemyCpContainer.gameObject.SetActive(false);
                    return null;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void UpdateInventory()
        {
            var cp = UpdateCp();
            information.UpdateInventory(BattleType.Adventure, cp);
        }

        public void UpdateInventoryView()
        {
            var cp = UpdateCp();
            information.UpdateInventory(BattleType.Adventure, cp);
            information.UpdateView(BattleType.Adventure);
        }

        private void UpdateRandomBuffButton()
        {
            if (_stageType == StageType.EventDungeon)
            {
                randomBuffButton.gameObject.SetActive(false);
                return;
            }

            randomBuffButton.SetData(States.Instance.CrystalRandomSkillState, _stageId);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            _disposables.DisposeAllAndClear();
            base.Close(ignoreCloseAnimation);
        }

        #endregion

        private void UpdateBackground()
        {
            switch (_stageType)
            {
                case StageType.HackAndSlash:
                    hasBg.SetActive(true);
                    mimisbrunnrBg.SetActive(false);
                    foreach (var eventDungeonBg in eventDungeonBgs)
                    {
                        eventDungeonBg.background.SetActive(false);
                    }
                    break;
                case StageType.Mimisbrunnr:
                    hasBg.SetActive(false);
                    mimisbrunnrBg.SetActive(true);
                    foreach (var eventDungeonBg in eventDungeonBgs)
                    {
                        eventDungeonBg.background.SetActive(false);
                    }
                    break;
                case StageType.EventDungeon:
                    hasBg.SetActive(false);
                    mimisbrunnrBg.SetActive(false);
                    foreach (var eventDungeonBg in eventDungeonBgs)
                    {
                        eventDungeonBg.background.SetActive(false);
                    }

                    var eventType = EventManager.GetEventInfo().EventType;
                    var eventDungeonBackground = eventDungeonBgs
                        .FirstOrDefault(bg => bg.eventType == eventType)?.background;
                    if (eventDungeonBackground != null)
                    {
                        eventDungeonBackground.SetActive(true);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void UpdateRequiredCostByStageId()
        {
            switch (_stageType)
            {
                case StageType.HackAndSlash:
                case StageType.Mimisbrunnr:
                {
                    TableSheets.Instance.StageSheet.TryGetValue(
                        _stageId, out var stage, true);
                    _requiredCost = stage.CostAP;
                    var stakingLevel = States.Instance.StakingLevel;
                    if (_stageType is StageType.HackAndSlash && stakingLevel > 0)
                    {
                        _requiredCost =
                            TableSheets.Instance.StakeActionPointCoefficientSheet
                                .GetActionPointByStaking(
                                    _requiredCost,
                                    1,
                                    stakingLevel);
                    }

                    startButton.SetCost(CostType.ActionPoint, _requiredCost);
                    break;
                }
                case StageType.EventDungeon:
                {
                    _requiredCost = 1;
                    startButton.SetCost(CostType.EventDungeonTicket, _requiredCost);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void OnClickBattle()
        {
            AudioController.PlayClick();

            if (Game.Game.instance.IsInWorld)
            {
                return;
            }

            switch (_stageType)
            {
                case StageType.HackAndSlash:
                {
                    StartCoroutine(CoBattleStart(_stageType, CostType.ActionPoint));
                    break;
                }
                case StageType.Mimisbrunnr:
                {
                    if (!CheckEquipmentElementalType())
                    {
                        NotificationSystem.Push(
                            MailType.System,
                            L10nManager.Localize("UI_MIMISBRUNNR_START_FAILED"),
                            NotificationCell.NotificationType.UnlockCondition);
                        return;
                    }

                    StartCoroutine(CoBattleStart(
                        _stageType,
                        CostType.ActionPoint));
                    break;
                }
                case StageType.EventDungeon:
                {
                    if (!_scheduleId.HasValue)
                    {
                        NotificationSystem.Push(
                            MailType.System,
                            L10nManager.Localize("UI_EVENT_NOT_IN_PROGRESS"),
                            NotificationCell.NotificationType.Information);

                        return;
                    }

                    if (RxProps.EventDungeonTicketProgress.Value.currentTickets >=
                        _requiredCost)
                    {
                        StartCoroutine(CoBattleStart(
                            _stageType,
                            CostType.EventDungeonTicket));
                        break;
                    }

                    var balance = States.Instance.GoldBalanceState.Gold;
                    var cost = RxProps.EventScheduleRowForDungeon.Value
                        .GetDungeonTicketCost(
                            RxProps.EventDungeonInfo.Value?.NumberOfTicketPurchases ?? 0,
                            States.Instance.GoldBalanceState.Gold.Currency);
                    var purchasedCount = RxProps.EventDungeonInfo.Value?.NumberOfTicketPurchases ?? 0;

                    Find<TicketPurchasePopup>().Show(
                        CostType.EventDungeonTicket,
                        CostType.NCG,
                        balance,
                        cost,
                        purchasedCount,
                        1,
                        () => StartCoroutine(CoBattleStart(_stageType, CostType.NCG, true)),
                        () => GoToMarket(TradeType.Sell),
                        true
                    );
                    return;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            repeatToggle.interactable = false;
            coverToBlockClick.SetActive(true);
        }

        private IEnumerator CoBattleStart(
            StageType stageType,
            CostType costType,
            bool buyTicketIfNeeded = false)
        {
            var game = Game.Game.instance;
            game.IsInWorld = true;
            game.Stage.IsShowHud = true;

            var headerMenuStatic = Find<HeaderMenuStatic>();
            var currencyImage = costType switch
            {
                CostType.NCG => headerMenuStatic.Gold.IconImage,
                CostType.ActionPoint => headerMenuStatic.ActionPoint.IconImage,
                CostType.Hourglass => headerMenuStatic.Hourglass.IconImage,
                CostType.Crystal => headerMenuStatic.Crystal.IconImage,
                CostType.ArenaTicket => headerMenuStatic.ArenaTickets.IconImage,
                CostType.WorldBossTicket => headerMenuStatic.WorldBossTickets.IconImage,
                CostType.EventDungeonTicket => headerMenuStatic.EventDungeonTickets.IconImage,
                _ or CostType.None => throw new ArgumentOutOfRangeException(
                    nameof(costType), costType, null)
            };
            var itemMoveAnimation = ItemMoveAnimation.Show(
                currencyImage.sprite,
                currencyImage.transform.position,
                buttonStarImageTransform.position,
                Vector2.one,
                moveToLeft,
                true,
                animationTime,
                middleXGap);
            yield return new WaitWhile(() => itemMoveAnimation.IsPlaying);

            SendBattleAction(
                stageType,
                buyTicketIfNeeded: buyTicketIfNeeded);
        }

        private void ShowBoosterPopup()
        {
            if (_stageType == StageType.Mimisbrunnr && !CheckEquipmentElementalType())
            {
                NotificationSystem.Push(
                    MailType.System,
                    L10nManager.Localize("UI_MIMISBRUNNR_START_FAILED"),
                    NotificationCell.NotificationType.UnlockCondition);
                return;
            }

            var itemSlotState = States.Instance.CurrentItemSlotStates[BattleType.Adventure];
            var costumes = itemSlotState.Costumes;
            var equipments = itemSlotState.Equipments;
            var runeInfos = States.Instance.CurrentRuneSlotStates[BattleType.Adventure]
                .GetEquippedRuneSlotInfos();
            var consumables = information.GetEquippedConsumables();
            var stage = Game.Game.instance.Stage;
            stage.IsExitReserved = false;
            stage.foodCount = consumables.Count;
            ActionRenderHandler.Instance.Pending = true;

            Find<UtilitiePopup>().Show(
                stage,
                costumes,
                equipments,
                consumables,
                runeInfos,
                GetBoostMaxCount(_stageId),
                _worldId,
                _stageId);
        }

        private void SendBattleAction(
            StageType stageType,
            int playCount = 1,
            int apStoneCount = 0,
            bool buyTicketIfNeeded = false)
        {
            Find<WorldMap>().Close(true);
            Find<StageInformation>().Close(true);
            Find<LoadingScreen>().Show();

            startButton.gameObject.SetActive(false);
            var itemSlotState = States.Instance.CurrentItemSlotStates[BattleType.Adventure];
            var costumes = itemSlotState.Costumes;
            var equipments = itemSlotState.Equipments;
            var runeInfos = States.Instance.CurrentRuneSlotStates[BattleType.Adventure]
                .GetEquippedRuneSlotInfos();
            var consumables = information.GetEquippedConsumables();

            var stage = Game.Game.instance.Stage;
            stage.IsExitReserved = false;
            stage.foodCount = consumables.Count;
            ActionRenderHandler.Instance.Pending = true;

            switch (stageType)
            {
                case StageType.HackAndSlash:
                {
                    var skillState = States.Instance.CrystalRandomSkillState;
                    var avatarAddress = States.Instance.CurrentAvatarState.address;
                    var key = string.Format("HackAndSlash.SelectedBonusSkillId.{0}", avatarAddress);
                    var skillId = PlayerPrefs.GetInt(key, 0);
                    if (skillId == 0)
                    {
                        if (skillState == null ||
                            !skillState.SkillIds.Any())
                        {
                            ActionManager.Instance.HackAndSlash(
                                costumes,
                                equipments,
                                consumables,
                                runeInfos,
                                _worldId,
                                _stageId,
                                playCount: playCount,
                                apStoneCount: apStoneCount,
                                trackGuideQuest: _trackGuideQuest
                            ).Subscribe();
                            break;
                        }

                        skillId = skillState.SkillIds
                            .Select(buffId =>
                                TableSheets.Instance.CrystalRandomBuffSheet
                                    .TryGetValue(buffId, out var bonusBuffRow)
                                    ? bonusBuffRow
                                    : null)
                            .Where(x => x != null)
                            .OrderBy(x => x.Rank)
                            .ThenBy(x => x.Id)
                            .First()
                            .Id;
                    }

                    ActionManager.Instance.HackAndSlash(
                        costumes,
                        equipments,
                        consumables,
                        runeInfos,
                        _worldId,
                        _stageId,
                        skillId,
                        playCount,
                        apStoneCount,
                        _trackGuideQuest
                    ).Subscribe();
                    PlayerPrefs.SetInt(key, 0);
                    break;
                }
                case StageType.Mimisbrunnr:
                {
                    ActionManager.Instance.MimisbrunnrBattle(
                        costumes,
                        equipments,
                        consumables,
                        runeInfos,
                        _worldId,
                        _stageId,
                        1
                    ).Subscribe();
                    break;
                }
                case StageType.EventDungeon:
                {
                    if (!_scheduleId.HasValue)
                    {
                        NotificationSystem.Push(
                            MailType.System,
                            L10nManager.Localize("UI_EVENT_NOT_IN_PROGRESS"),
                            NotificationCell.NotificationType.Information);
                        break;
                    }

                    ActionManager.Instance.EventDungeonBattle(
                            _scheduleId.Value,
                            _worldId,
                            _stageId,
                            equipments,
                            costumes,
                            consumables,
                            runeInfos,
                            buyTicketIfNeeded,
                            _trackGuideQuest)
                        .Subscribe();
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(stageType), stageType, null);
            }
        }

        public void GoToStage(BattleLog battleLog)
        {
            Game.Event.OnStageStart.Invoke(battleLog);
            Find<LoadingScreen>().Close();
            Close(true);
        }

        private void GoToMarket(TradeType tradeType)
        {
            Close(true);
            Find<WorldMap>().Close(true);
            Find<StageInformation>().Close(true);
            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Shop);
            switch (tradeType)
            {
                case TradeType.Buy:
                    Find<ShopBuy>().Show();
                    break;
                case TradeType.Sell:
                    Find<ShopSell>().Show();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(tradeType), tradeType, null);
            }
        }

        private static int GetBoostMaxCount(int stageId)
        {
            if (!TableSheets.Instance.GameConfigSheet.TryGetValue(
                    "action_point_max",
                    out var ap))
            {
                return 1;
            }

            var stage = TableSheets.Instance.StageSheet.OrderedList
                .FirstOrDefault(i => i.Id == stageId);
            if (stage is null)
            {
                return 1;
            }

            var maxActionPoint = TableExtensions.ParseInt(ap.Value);
            return maxActionPoint / stage.CostAP;
        }

        private static void ShowHelpTooltip(StageType stageType)
        {
            switch (stageType)
            {
                case StageType.HackAndSlash:
                    HelpTooltip.HelpMe(100004, true);
                    break;
                case StageType.Mimisbrunnr:
                    HelpTooltip.HelpMe(100020, true);
                    break;
                case StageType.EventDungeon:
                    // ignore.
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(stageType), stageType, null);
            }
        }

        private bool CheckEquipmentElementalType()
        {
            var (equipments, _) = States.Instance.GetEquippedItems(BattleType.Adventure);
            var elementalTypes = GetElementalTypes();
            return equipments.All(x =>
                elementalTypes.Contains(x.ElementalType));
        }

        private void UpdateStartButton()
        {
            startButton.UpdateObjects();
            foreach (var particle in particles)
            {
                if (startButton.IsSubmittable)
                {
                    particle.Play();
                }
                else
                {
                    particle.Stop();
                }
            }

            var (equipments, costumes) = States.Instance.GetEquippedItems(BattleType.Adventure);
            var runes = States.Instance.GetEquippedRuneStates(BattleType.Adventure)
                .Select(x=> x.RuneId).ToList();
            var consumables = information.GetEquippedConsumables().Select(x=> x.Id).ToList();
            var canBattle = Util.CanBattle(equipments, costumes, consumables);
            startButton.gameObject.SetActive(canBattle);

            switch (_stageType)
            {
                case StageType.HackAndSlash:
                    boostPopupButton.gameObject.SetActive(false);
                    sweepPopupButton.gameObject.SetActive(!IsFirstStage);
                    break;
                case StageType.Mimisbrunnr:
                    boostPopupButton.gameObject.SetActive(canBattle);
                    sweepPopupButton.gameObject.SetActive(false);
                    break;
                case StageType.EventDungeon:
                    boostPopupButton.gameObject.SetActive(false);
                    sweepPopupButton.gameObject.SetActive(false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            blockStartingTextObject.SetActive(!canBattle);
        }

        public List<ElementalType> GetElementalTypes()
        {
            if (_stageType != StageType.Mimisbrunnr)
            {
                return ElementalTypeExtension.GetAllTypes();
            }

            var mimisbrunnrSheet = TableSheets.Instance.MimisbrunnrSheet;
            return mimisbrunnrSheet.TryGetValue(_stageId, out var mimisbrunnrSheetRow)
                ? mimisbrunnrSheetRow.ElementalTypes
                : ElementalTypeExtension.GetAllTypes();
        }


        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        public void SimulateBattlePandora()
        {
            StartCoroutine(SimulatorIE());
        }

        IEnumerator SimulatorIE()
        {
            Find<WorldMap>().Close(true);
            Find<StageInformation>().Close(true);
            Find<LoadingScreen>().Show();
            yield return new WaitForSeconds(2);

            PandoraMaster.IsHackAndSlashSimulate = true;
            var consumables = information.GetEquippedConsumables().Select(x => x.ItemId).ToList();
            var skillId = PlayerPrefs.GetInt("_PandoraBox_PVE_SelectedCrystalBuff", -1);
            var simulator = Premium.PVE_SoloSimulate(_worldId, _stageId, consumables, skillId);
            simulator.Simulate();
            GoToStage(simulator.Log);
        }

        public void MultipleSimulate()
        {
            var consumables = information.GetEquippedConsumables().Select(x => x.ItemId).ToList();
            var skillId = PlayerPrefs.GetInt("_PandoraBox_PVE_SelectedCrystalBuff", -1);
            Premium.PVE_MultiSimulate(_worldId, _stageId, consumables, skillId);
        }
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
    }
}
