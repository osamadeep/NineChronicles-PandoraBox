using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DG.Tweening;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.State;
using Nekoyume.Model.BattleStatus;
using UnityEngine;
using Random = UnityEngine.Random;
using mixpanel;
using Nekoyume.EnumType;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.State.Subjects;
using Nekoyume.UI.Module;
using Nekoyume.UI.Module.Lobby;
using Nekoyume.UI.Module.WorldBoss;
using TMPro;
using UnityEngine.UI;
using StateExtensions = Nekoyume.Model.State.StateExtensions;

namespace Nekoyume.UI
{
    using Cysharp.Threading.Tasks;
    using Libplanet;
    using Libplanet.Blocks;
    using Nekoyume.Helper;
    using Nekoyume.UI.Model;
    using Nekoyume.UI.Scroller;
    using PandoraBox;
    using System.Collections.Immutable;
    using System.Threading.Tasks;
    using TMPro;
    using Scroller;
    using UniRx;
    using PlayFab;
    using PlayFab.ClientModels;

    public class Menu : Widget
    {
        private const string FirstOpenShopKeyFormat = "Nekoyume.UI.Menu.FirstOpenShopKey_{0}";

        private const string FirstOpenCombinationKeyFormat =
            "Nekoyume.UI.Menu.FirstOpenCombinationKey_{0}";

        private const string FirstOpenRankingKeyFormat = "Nekoyume.UI.Menu.FirstOpenRankingKey_{0}";
        private const string FirstOpenQuestKeyFormat = "Nekoyume.UI.Menu.FirstOpenQuestKey_{0}";
        private const string FirstOpenMimisbrunnrKeyFormat = "Nekoyume.UI.Menu.FirstOpenMimisbrunnrKeyKey_{0}";

        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        [Header("PANDORA CUSTOM FIELDS")]
        [SerializeField] private Transform shopCrystalChanges;

        //Extra UI Buttons
        [SerializeField] private Button fastSwitchButton;
        [SerializeField] private Button updateAvatarButton;
        [SerializeField] private Button runnerButton;
        [SerializeField] private Button labButton;
        public Button chronoButton;

        private List<(int rank, ArenaInfo arenaInfo)> _weeklyCachedInfo = new List<(int rank, ArenaInfo arenaInfo)>();
        private ArenaInfoList _arenaInfoList = new ArenaInfoList();

        [Space(50)]
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
        [SerializeField] private MainMenu btnQuest;

        [SerializeField]
        private MainMenu btnCombination;

        [SerializeField]
        private MainMenu btnShop;

        [SerializeField]
        private MainMenu btnRanking;

        [SerializeField]
        private MainMenu btnMimisbrunnr;

        [SerializeField]
        private MainMenu btnStaking;

        [SerializeField]
        private MainMenu btnWorldBoss = null;

        [SerializeField]
        private SpeechBubble[] speechBubbles = null;

        [SerializeField]
        private GameObject shopExclamationMark;

        [SerializeField]
        private GameObject combinationExclamationMark;

        [SerializeField]
        private GameObject questExclamationMark;

        [SerializeField]
        private GameObject mimisbrunnrExclamationMark;

        [SerializeField]
        private GameObject eventDungeonExclamationMark;

        [SerializeField]
        private TextMeshProUGUI eventDungeonTicketsText;

        [SerializeField]
        private Image stakingLevelIcon;

        [SerializeField]
        private GuidedQuest guidedQuest;

        [SerializeField]
        private Button playerButton;

        [SerializeField]
        private StakeIconDataScriptableObject stakeIconData;

        [SerializeField]
        private RectTransform player;

        [SerializeField]
        private Transform titleSocket;

        private Coroutine _coLazyClose;

        private readonly List<IDisposable> _disposablesAtShow = new();
        private GameObject _cachedCharacterTitle;

        protected override void Awake()
        {
            base.Awake();

            speechBubbles = GetComponentsInChildren<SpeechBubble>();
            Game.Event.OnRoomEnter.AddListener(b => Show());

            CloseWidget = null;

            playerButton.onClick.AddListener(() =>
            {
                Game.Game.instance.Lobby.Character.Touch();
            });
            guidedQuest.OnClickWorldQuestCell
                .Subscribe(tuple => HackAndSlash(tuple.quest.Goal))
                .AddTo(gameObject);
            guidedQuest.OnClickCombinationEquipmentQuestCell
                .Subscribe(tuple => GoToCombinationEquipmentRecipe(tuple.quest.RecipeId))
                .AddTo(gameObject);
            guidedQuest.OnClickEventDungeonQuestCell
                .Subscribe(tuple => EventDungeonBattle(tuple.quest.Goal))
                .AddTo(gameObject);
            guidedQuest.CnClickCraftEventItemQuestCell
                .Subscribe(tuple => GoToCraftWithToggleType(2))
                .AddTo(gameObject);
            AnimationState.Subscribe(stateType =>
            {
                var buttonList = new List<Button>
                {
                    btnCombination.GetComponent<Button>(),
                    btnMimisbrunnr.GetComponent<Button>(),
                    btnQuest.GetComponent<Button>(),
                    btnRanking.GetComponent<Button>(),
                    btnShop.GetComponent<Button>(),
                    btnStaking.GetComponent<Button>(),
                    btnWorldBoss.GetComponent<Button>(),
                };
                buttonList.ForEach(button => button.interactable = stateType == AnimationStateType.Shown);
            }).AddTo(gameObject);

            MonsterCollectionStateSubject.Level
                .Subscribe(level =>
                    stakingLevelIcon.sprite = stakeIconData.GetIcon(level, IconType.Bubble))
                .AddTo(gameObject);

            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            fastSwitchButton.onClick.AddListener(() => { FastCharacterSwitch(); });
            updateAvatarButton.onClick.AddListener(() => { UpdateAvatar(); });
            runnerButton.onClick.AddListener(() => { ShowRunner(); });
            labButton.onClick.AddListener(() => { FastShowEvent(); });
            chronoButton.onClick.AddListener(() => { Widget.Find<ChronoSlotsPopup>().Show(); });
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
        }

        private void HackAndSlash(int stageId)
        {
            if (TableSheets.Instance.WorldSheet.TryGetByStageId(stageId, out var worldRow) &&
                ShortcutHelper.CheckConditionOfShortcut(ShortcutHelper.PlaceType.Stage,
                    stageId))
            {
                CloseWithOtherWidgets();
                ShortcutHelper.ShortcutActionForStage(worldRow.Id, stageId, true);
            }
            else if(ShortcutHelper.CheckUIStateForUsingShortcut(ShortcutHelper.PlaceType.Stage))
            {
                Find<Menu>().QuestClick();
            }
        }

        private void EventDungeonBattle(int eventDungeonStageId)
        {
            if (RxProps.EventScheduleRowForDungeon.Value is null)
            {
                NotificationSystem.Push(
                    MailType.System,
                    L10nManager.Localize("UI_EVENT_NOT_IN_PROGRESS"),
                    NotificationCell.NotificationType.Information);
                return;
            }

            if (ShortcutHelper.CheckConditionOfShortcut(ShortcutHelper.PlaceType.EventDungeonStage,
                    eventDungeonStageId))
            {
                CloseWithOtherWidgets();
                ShortcutHelper.ShortcutActionForEventStage(eventDungeonStageId, true);
            }
            else if (ShortcutHelper.CheckUIStateForUsingShortcut(ShortcutHelper.PlaceType
                         .EventDungeonStage))
            {
                Find<Menu>().QuestClick();
            }
        }

        public void UpdateTitle(Costume title)
        {
            Destroy(_cachedCharacterTitle);
            if (title == null)
            {
                return;
            }

            var clone = ResourcesHelper.GetCharacterTitle(title.Grade,
                title.GetLocalizedNonColoredName(false));
            _cachedCharacterTitle = Instantiate(clone, titleSocket);
        }

        public void EnterRoom()
        {
            player.localPosition = new Vector3(-700, -149, 0);
            player.DOLocalMoveX(-470, 1.0f);
        }

        public void GoToStage(BattleLog battleLog)
        {
            Game.Event.OnStageStart.Invoke(battleLog);
            Find<LoadingScreen>().Close();
            Close(true);
        }

        private void GoToCraftWithToggleType(int toggleIndex)
        {
            AudioController.PlayClick();
            Analyzer.Instance.Track("Unity/Click Guided Quest Combination Equipment");
            CombinationClickInternal(() =>
                Find<Craft>().ShowWithToggleIndex(toggleIndex));
        }

        private void GoToCombinationEquipmentRecipe(int recipeId)
        {
            AudioController.PlayClick();
            Analyzer.Instance.Track("Unity/Click Guided Quest Combination Equipment", new Dictionary<string, Value>()
            {
                ["AvatarAddress"] = States.Instance.CurrentAvatarState.address.ToString(),
                ["AgentAddress"] = States.Instance.CurrentAvatarState.agentAddress.ToString(),
            });
            CombinationClickInternal(() =>
                Find<Craft>().ShowWithEquipmentRecipeId(recipeId));
        }

        private void GoToMarket(TradeType tradeType)
        {
            Close();
            Find<HeaderMenuStatic>()
                .UpdateAssets(HeaderMenuStatic.AssetVisibleState.Shop);
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

        private void UpdateButtons()
        {
            btnQuest.Update();
            btnCombination.Update();
            btnShop.Update();
            btnRanking.Update();
            btnMimisbrunnr.Update();
            btnStaking.Update();
            btnWorldBoss.Update();

            var addressHex = States.Instance.CurrentAvatarState.address.ToHex();
            var firstOpenCombinationKey
                = string.Format(FirstOpenCombinationKeyFormat, addressHex);
            var firstOpenShopKey
                = string.Format(FirstOpenShopKeyFormat, addressHex);
            var firstOpenQuestKey
                = string.Format(FirstOpenQuestKeyFormat, addressHex);
            var firstOpenMimisbrunnrKey
                = string.Format(FirstOpenMimisbrunnrKeyFormat, addressHex);

            combinationExclamationMark.gameObject.SetActive(
                btnCombination.IsUnlocked
                && (PlayerPrefs.GetInt(firstOpenCombinationKey, 0) == 0 ||
                    Craft.SharedModel.HasNotification));
            shopExclamationMark.gameObject.SetActive(

            btnShop.IsUnlocked
            && PlayerPrefs.GetInt(firstOpenShopKey, 0) == 0);

            var worldMap = Find<WorldMap>();
            worldMap.UpdateNotificationInfo();
            var hasNotificationInWorldMap = worldMap.HasNotification;
            questExclamationMark.gameObject.SetActive(
                (btnQuest.IsUnlocked
                 && PlayerPrefs.GetInt(firstOpenQuestKey, 0) == 0)
                || hasNotificationInWorldMap);
            mimisbrunnrExclamationMark.gameObject.SetActive(
                btnMimisbrunnr.IsUnlocked
                && PlayerPrefs.GetInt(firstOpenMimisbrunnrKey, 0) == 0);
        }

        private void HideButtons()
        {
            btnQuest.gameObject.SetActive(false);
            btnCombination.gameObject.SetActive(false);
            btnShop.gameObject.SetActive(false);
            btnRanking.gameObject.SetActive(false);
            btnMimisbrunnr.gameObject.SetActive(false);
            btnStaking.gameObject.SetActive(false);
        }

        public void ShowWorld()
        {
            Show();
            HideButtons();
        }

        public void QuestClick()
        {
            if (!btnQuest.IsUnlocked)
            {
                btnQuest.JingleTheCat();
                return;
            }

            if (questExclamationMark.gameObject.activeSelf)
            {
                var addressHex = States.Instance.CurrentAvatarState.address.ToHex();
                var key = string.Format(FirstOpenQuestKeyFormat, addressHex);
                PlayerPrefs.SetInt(key, 1);
            }

            Close();
            var avatarState = States.Instance.CurrentAvatarState;
            Find<WorldMap>().Show(avatarState.worldInformation);
            AudioController.PlayClick();
        }

        public void ShopClick()
        {
            if (!btnShop.IsUnlocked)
            {
                btnShop.JingleTheCat();
                return;
            }

            if (shopExclamationMark.gameObject.activeSelf)
            {
                var addressHex = States.Instance.CurrentAvatarState.address.ToHex();
                var key = string.Format(FirstOpenShopKeyFormat, addressHex);
                PlayerPrefs.SetInt(key, 1);
            }

            Close();
            Find<ShopBuy>().Show();
            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Shop);
            AudioController.PlayClick();
        }

        public void CombinationClick() =>
            CombinationClickInternal(() => Find<CombinationMain>().Show());

        private void CombinationClickInternal(System.Action showAction)
        {
            if (showAction is null)
            {
                return;
            }

            if (!btnCombination.IsUnlocked)
            {
                btnCombination.JingleTheCat();
                return;
            }

            if (combinationExclamationMark.gameObject.activeSelf)
            {
                var addressHex = States.Instance.CurrentAvatarState.address.ToHex();
                var key = string.Format(FirstOpenCombinationKeyFormat, addressHex);
                PlayerPrefs.SetInt(key, 1);
            }

            Close();
            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Combination);
            showAction();
        }

        public void RankingClick()
        {
            if (!btnRanking.IsUnlocked)
            {
                btnRanking.JingleTheCat();
                return;
            }

            Close(true);
            Find<ArenaJoin>().ShowAsync().Forget();
            Analyzer.Instance.Track("Unity/Enter arena page", new Dictionary<string, Value>()
            {
                ["AvatarAddress"] = States.Instance.CurrentAvatarState.address.ToString(),
                ["AgentAddress"] = States.Instance.CurrentAvatarState.agentAddress.ToString(),
            });
            AudioController.PlayClick();
        }

        public void MimisbrunnrClick()
        {
            if (!btnMimisbrunnr.IsUnlocked)
            {
                btnMimisbrunnr.JingleTheCat();
                return;
            }

            const int worldId = GameConfig.MimisbrunnrWorldId;
            var worldSheet = Game.Game.instance.TableSheets.WorldSheet;
            var worldRow =
                worldSheet.OrderedList.FirstOrDefault(
                    row => row.Id == worldId);
            if (worldRow is null)
            {
                NotificationSystem.Push(MailType.System,
                    L10nManager.Localize("ERROR_WORLD_DOES_NOT_EXIST"),
                    NotificationCell.NotificationType.Information);
                return;
            }

            var wi = States.Instance.CurrentAvatarState.worldInformation;
            if (!wi.TryGetWorld(worldId, out var world))
            {
                LocalLayerModifier.AddWorld(
                    States.Instance.CurrentAvatarState.address,
                    worldId);

                if (!wi.TryGetWorld(worldId, out world))
                {
                    // Do nothing.
                    return;
                }
            }

            if (!world.IsUnlocked)
            {
                // Do nothing.
                return;
            }

            var SharedViewModel = new WorldMap.ViewModel
            {
                WorldInformation = wi,
            };

            if (mimisbrunnrExclamationMark.gameObject.activeSelf)
            {
                var addressHex = States.Instance.CurrentAvatarState.address.ToHex();
                var key = string.Format(FirstOpenMimisbrunnrKeyFormat, addressHex);
                PlayerPrefs.SetInt(key, 1);
            }

            Close();
            AudioController.PlayClick();

            SharedViewModel.SelectedWorldId.SetValueAndForceNotify(world.Id);
            SharedViewModel.SelectedStageId.SetValueAndForceNotify(world.GetNextStageId());
            var stageInfo = Find<UI.StageInformation>();
            stageInfo.Show(SharedViewModel, worldRow, StageType.Mimisbrunnr);
            var status = Find<Status>();
            status.Close(true);
            Find<EventBanner>().Close(true);
            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Battle);
            HelpTooltip.HelpMe(100019, true);
        }

        public void StakingClick()
        {
            if (!btnStaking.IsUnlocked)
            {
                btnStaking.JingleTheCat();
                return;
            }

            Find<StakingPopup>().Show();
        }

        public void WorldBossClick()
        {
            if (!btnWorldBoss.IsUnlocked)
            {
                btnWorldBoss.JingleTheCat();
                return;
            }

            AudioController.PlayClick();

            var currentBlockIndex = Game.Game.instance.Agent.BlockIndex;
            var curStatus = WorldBossFrontHelper.GetStatus(currentBlockIndex);
            if (curStatus == WorldBossStatus.OffSeason)
            {
                if (!WorldBossFrontHelper.TryGetNextRow(currentBlockIndex, out _))
                {
                    OneLineSystem.Push(
                        MailType.System,
                        "There is no world boss schedule.",
                        NotificationCell.NotificationType.Alert);
                    return;
                }
            }

            Close(true);
            Find<WorldBoss>().ShowAsync().Forget();
            Analyzer.Instance.Track("Unity/Enter world boss page");
        }

        public void UpdateGuideQuest(AvatarState avatarState)
        {
            guidedQuest.UpdateList(avatarState);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            SubscribeAtShow();

            if (!(_coLazyClose is null))
            {
                StopCoroutine(_coLazyClose);
                _coLazyClose = null;
            }

            guidedQuest.Hide(true);
            base.Show(ignoreShowAnimation);

            StartCoroutine(CoStartSpeeches());
            UpdateButtons();

            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            //PandoraMaster.SetCurrentPandoraPlayer(PandoraMaster.GetPandoraPlayer(States.Instance.CurrentAvatarState.agentAddress.ToString()));
            string tmp = "_PandoraBox_Account_LoginProfile0" + PandoraMaster.LoginIndex + "_Name";

            PlayerPrefs.SetString(tmp, States.Instance.CurrentAvatarState.name); //save profile name
            //set name to playfab
            if (string.IsNullOrEmpty(PandoraMaster.PlayFabPlayerProfile.DisplayName))
            {
                var currentName = States.Instance.CurrentAvatarState.name + " #" + States.Instance.CurrentAvatarState.address.ToHex().Substring(0, 4);
                //PlayFabClientAPI.UpdateUserTitleDisplayName(request, OnChangePlayFabNameSuccess, OnChangePlayFabNameError);
                PlayFabClientAPI.UpdateUserTitleDisplayName(new UpdateUserTitleDisplayNameRequest { DisplayName = currentName},
                    success => { PandoraMaster.PlayFabPlayerProfile.DisplayName = currentName;},failed => { Debug.LogError(failed.GenerateErrorReport()); });
            }

            //load favorite items
            PandoraMaster.FavItems.Clear();
            for (int i = 0; i < PandoraMaster.FavItemsMaxCount; i++) //fav max count
            {
                string key = "_PandoraBox_General_FavItems0" + i + "_" + States.Instance.CurrentAvatarState.address;
                if (i > 9)
                    key = "_PandoraBox_General_FavItems" + i + "_" + States.Instance.CurrentAvatarState.address;

                if (PlayerPrefs.HasKey(key))
                    PandoraMaster.FavItems.Add(PlayerPrefs.GetString(key));
            }

            //set guild data
            PandoraMaster.CurrentGuildPlayer = null;
            PandoraMaster.CurrentGuild = null;

            PandoraMaster.CurrentGuildPlayer = PandoraMaster.PanDatabase.GuildPlayers.
                Find(x => x.AvatarAddress.ToLower() == States.Instance.CurrentAvatarState.address.ToString().ToLower());
            if (!(PandoraMaster.CurrentGuildPlayer is null))
                PandoraMaster.CurrentGuild = PandoraMaster.PanDatabase.Guilds.Find(x => x.Tag == PandoraMaster.CurrentGuildPlayer.Guild);

            //check crystal changes
            ChangeCrystalRatio();
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
            stakingLevelIcon.sprite =
                stakeIconData.GetIcon(States.Instance.StakingLevel, IconType.Bubble);
        }

        private void SubscribeAtShow()
        {
            _disposablesAtShow.DisposeAllAndClear();
            RxProps.EventScheduleRowForDungeon.Subscribe(value =>
            {
                eventDungeonTicketsText.text =
                    RxProps.EventDungeonTicketProgress.Value
                        .currentTickets.ToString(CultureInfo.InvariantCulture);
                eventDungeonExclamationMark.gameObject
                    .SetActive(value is not null);
            }).AddTo(_disposablesAtShow);
            RxProps.EventDungeonTicketProgress.Subscribe(value =>
            {
                eventDungeonTicketsText.text =
                    value.currentTickets.ToString(CultureInfo.InvariantCulture);
            }).AddTo(_disposablesAtShow);
        }

        protected override void OnCompleteOfShowAnimationInternal()
        {
            base.OnCompleteOfShowAnimationInternal();
            Find<DialogPopup>().Show(1, PlayTutorial);
            StartCoroutine(CoHelpPopup());
        }

        private void PlayTutorial()
        {
            var worldInfo = Game.Game.instance.States.CurrentAvatarState.worldInformation;
            if (worldInfo is null)
            {
                Debug.LogError("[Menu.PlayTutorial] : worldInformation is null");
                return;
            }

            var clearedStageId = worldInfo.TryGetLastClearedStageId(out var id) ? id : 1;
            Game.Game.instance.Stage.TutorialController.Run(clearedStageId);
        }

        private IEnumerator CoHelpPopup()
        {
            var dialog = Find<DialogPopup>();
            while (dialog.IsActive())
            {
                yield return null;
            }

            guidedQuest.Show(States.Instance.CurrentAvatarState);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            _disposablesAtShow.DisposeAllAndClear();
            Destroy(_cachedCharacterTitle);
            Find<EventReleaseNotePopup>().Close(true);
            StopSpeeches();
            guidedQuest.Hide(true);
            Find<Status>().Close(true);
            Find<EventBanner>().Close(true);
            base.Close(ignoreCloseAnimation);
        }

        private IEnumerator CoStartSpeeches()
        {
            yield return new WaitForSeconds(2.0f);

            while (AnimationState.Value == AnimationStateType.Shown)
            {
                var n = speechBubbles.Length;
                while (n > 1)
                {
                    n--;
                    var k = Mathf.FloorToInt(Random.value * (n + 1));
                    (speechBubbles[k], speechBubbles[n]) = (speechBubbles[n], speechBubbles[k]);
                }

                foreach (var bubble in speechBubbles)
                {
                    yield return StartCoroutine(bubble.CoShowText());
                    yield return new WaitForSeconds(Random.Range(2.0f, 4.0f));
                }
            }
        }

        private void StopSpeeches()
        {
            StopCoroutine(CoStartSpeeches());
            foreach (var bubble in speechBubbles)
            {
                bubble.Hide();
            }
        }

        public void TutorialActionHackAndSlash()
        {
            HackAndSlash(GuidedQuest.WorldQuest?.Goal ?? 1);
        }

        // Invoke from TutorialController.PlayAction()
        public void TutorialActionGoToFirstRecipeCellView()
        {
            var firstRecipeRow = Game.Game.instance.TableSheets.EquipmentItemRecipeSheet.OrderedList
                .FirstOrDefault(row => row.UnlockStage == 3);
            if (firstRecipeRow is null)
            {
                Debug.LogError("TutorialActionGoToFirstRecipeCellView() firstRecipeRow is null");
                return;
            }

            Craft.SharedModel.DummyLockedRecipes.Add(firstRecipeRow.Id);
            GoToCombinationEquipmentRecipe(firstRecipeRow.Id);
        }

        // Invoke from TutorialController.PlayAction()
        public void TutorialActionClickGuidedQuestWorldStage2()
        {
            var player = Game.Game.instance.Stage.GetPlayer();
            player.DisableHudContainer();
            HackAndSlash(GuidedQuest.WorldQuest?.Goal ?? 4);
        }

        public void ShowGuild(string tag)
        {
            Widget.Find<GuildInfo>().Show(tag);
        }


        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        void ChangeCrystalRatio()
        {
            var difference = PandoraMaster.PanDatabase.Crystal - PandoraMaster.PanDatabase.CrystalOld;
            var percentage = Mathf.Round(((float)difference / (float)PandoraMaster.PanDatabase.CrystalOld) * 100f) ;
            string percentageString = difference > 0 ? "<color=green>" + percentage + "</color>" : "<color=red>" + Mathf.Abs(percentage) + "</color>";
            shopCrystalChanges.GetChild(0).GetComponent<TextMeshProUGUI>().text = "%" + percentageString;
            shopCrystalChanges.GetChild(1).gameObject.SetActive(difference > 0); //green
            shopCrystalChanges.GetChild(2).gameObject.SetActive(difference < 0); //red
        }


        public void PandoraShop()
        {
            Widget.Find<PandoraShopPopup>().Show();
        }

        public void FastShowEvent()
        {
            //var worldMap = Find<WorldMap>();
            //worldMap.Show(States.Instance.CurrentAvatarState.worldInformation, true);
            //worldMap.ShowEventDungeonStage(RxProps.EventDungeonRow, false);
            Find<PandoraLab>().Show(PandoraLab.ToggleType.Information);
            AudioController.PlayClick();
        }

        public async void UpdateAvatar()
        {
            updateAvatarButton.interactable = false;
            OneLineSystem.Push(MailType.System, "<color=green>Pandora Box</color>: Updating Avatar...", NotificationCell.NotificationType.Information);
            await UpdateAvatarState(States.Instance.CurrentAvatarState, States.Instance.CurrentAvatarKey);
            await States.Instance.SetCombinationSlotStatesAsync(States.Instance.CurrentAvatarState);
            await ActionRenderHandler.Instance.UpdateCurrentAvatarStateAsync(States.Instance.CurrentAvatarState);
            updateAvatarButton.interactable = true;
            PandoraMaster.CurrentAction = PandoraUtil.ActionType.Idle;
        }

        private static UniTask UpdateAvatarState(AvatarState avatarState, int index) =>
        States.Instance.AddOrReplaceAvatarStateAsync(avatarState, index);


        protected override void OnEnable()
        {
            base.OnEnable();
            DailyBonus.IsTrying = false;
        }

        public void ShowRunner()
        {
            if (!PandoraMaster.PanDatabase.RunnerSettings.Available)
            {
                OneLineSystem.Push(MailType.System, "<color=green>Pandora Box</color>: Under maintenance!",
                NotificationCell.NotificationType.Information);
                return;
            }
            Find<RunnerTown>().Show(true);
            return;
        }

        public void FastCharacterSwitch()
        {
#if !UNITY_EDITOR
            if (PandoraUtil.IsBusy())
                return;

            if (Game.Game.instance.IsInWorld)
            {
                NotificationSystem.Push(Nekoyume.Model.Mail.MailType.System,
                    L10nManager.Localize("UI_BLOCK_EXIT"), NotificationCell.NotificationType.Information);
                return;
            }
#endif 
            Game.Event.OnNestEnter.Invoke();

            var deletableWidgets = FindWidgets().Where(widget =>
                !(widget is SystemWidget) && !(widget is QuitSystem) &&
                !(widget is MessageCatTooltip) && widget.IsActive());
            foreach (var widget in deletableWidgets)
            {
                widget.Close(true);
            }

            Find<Login>().Show();
            Close();
        }

        public void ShowPandoraSettings()
        {
            AudioController.PlayClick();
            Widget.Find<PandoraSettingPopup>().Show();
        }

        public void ShowNFT()
        {
            Application.OpenURL("https://discord.gg/yfcP5GzqQ7");
        }

        public void FastArenaEnter()
        {
            Close(true);
            //Find<RankingBoard>().gameObject.SetActive(true);
            AudioController.instance.PlayMusic(AudioController.MusicCode.PVPBattle);
            AudioController.PlayClick();
        }

        public void FastShopEnter()
        {
            Close(true);
            Find<ShopBuy>().gameObject.SetActive(true);
            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Shop);
            AudioController.instance.PlayMusic(AudioController.MusicCode.Shop);
            AudioController.PlayClick();
        }

        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||


//#if UNITY_EDITOR
        protected override void Update()
        {
            base.Update();

            if (!Find<CombinationResultPopup>().gameObject.activeSelf &&
                !Find<EnhancementResultPopup>().gameObject.activeSelf &&
                Input.GetKey(KeyCode.LeftControl))
            {
                if (Input.GetKeyDown(KeyCode.C))
                {
                    Find<CombinationResultPopup>().ShowWithEditorProperty();
                }
                else if (Input.GetKeyDown(KeyCode.E))
                {
                    Find<EnhancementResultPopup>().ShowWithEditorProperty();
                }
            }
        }
//#endif
    }
}
