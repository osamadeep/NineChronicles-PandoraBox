using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.State;
using Nekoyume.Model.BattleStatus;
using UnityEngine;
using Random = UnityEngine.Random;
using mixpanel;
using Nekoyume.EnumType;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.State.Subjects;
using Nekoyume.UI.Module;
using Nekoyume.UI.Module.Lobby;
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

    public class Menu : Widget
    {
        private const string FirstOpenShopKeyFormat = "Nekoyume.UI.Menu.FirstOpenShopKey_{0}";

        private const string FirstOpenCombinationKeyFormat =
            "Nekoyume.UI.Menu.FirstOpenCombinationKey_{0}";

        private const string FirstOpenRankingKeyFormat = "Nekoyume.UI.Menu.FirstOpenRankingKey_{0}";
        private const string FirstOpenQuestKeyFormat = "Nekoyume.UI.Menu.FirstOpenQuestKey_{0}";
        private const string FirstOpenMimisbrunnrKeyFormat = "Nekoyume.UI.Menu.FirstOpenMimisbrunnrKeyKey_{0}";

        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        [Header("PANDORA CUSTOM FIELDS")] [SerializeField]
        private TextMeshProUGUI arenaRemains;

        [SerializeField] private TextMeshProUGUI arenaCount;
        [SerializeField] private TextMeshProUGUI arenaCurrentPositionText;
        [SerializeField] private Button randomButton;

        private List<(int rank, ArenaInfo arenaInfo)> _weeklyCachedInfo = new List<(int rank, ArenaInfo arenaInfo)>();
        private ArenaInfoList _arenaInfoList = new ArenaInfoList();

        [Space(50)]
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
        [SerializeField] private MainMenu btnQuest = null;

        [SerializeField] private MainMenu btnCombination = null;

        [SerializeField] private MainMenu btnShop = null;

        [SerializeField] private MainMenu btnRanking = null;

        [SerializeField] private MainMenu btnMimisbrunnr = null;

        [SerializeField] private MainMenu btnStaking = null;

        [SerializeField] private SpeechBubble[] speechBubbles = null;

        [SerializeField] private GameObject shopExclamationMark = null;

        [SerializeField] private GameObject combinationExclamationMark = null;

        [SerializeField] private GameObject questExclamationMark = null;

        [SerializeField] private GameObject mimisbrunnrExclamationMark = null;

        [SerializeField] private Image stakingLevelIcon;

        [SerializeField] private GuidedQuest guidedQuest = null;

        [SerializeField] private Button playerButton;

        private Coroutine _coLazyClose;

        protected override void Awake()
        {
            base.Awake();

            speechBubbles = GetComponentsInChildren<SpeechBubble>();
            Game.Event.OnRoomEnter.AddListener(b => Show());

            CloseWidget = null;

            guidedQuest.OnClickWorldQuestCell
                .Subscribe(tuple => HackAndSlash(tuple.quest.Goal))
                .AddTo(gameObject);
            guidedQuest.OnClickCombinationEquipmentQuestCell
                .Subscribe(tuple => GoToCombinationEquipmentRecipe(tuple.quest.RecipeId))
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
                };
                buttonList.ForEach(button => button.interactable = stateType == AnimationStateType.Shown);
            }).AddTo(gameObject);

            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            //randomButton.OnClickAsObservable().Subscribe(_ => RandomArenaFight()).AddTo(gameObject);
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

            MonsterCollectionStateSubject.Level.Subscribe(level =>
                stakingLevelIcon.sprite = SpriteHelper.GetStakingIcon(level, true)).AddTo(gameObject);
        }

        // TODO: QuestPreparation.Quest(bool repeat) 와 로직이 흡사하기 때문에 정리할 여지가 있습니다.
        private void HackAndSlash(int stageId)
        {
            var sheets = Game.Game.instance.TableSheets;
            var stageRow = sheets.StageSheet.OrderedList.FirstOrDefault(row => row.Id == stageId);
            if (stageRow is null)
            {
                return;
            }

            var requiredCost = stageRow.CostAP;
            if (States.Instance.CurrentAvatarState.actionPoint < requiredCost)
            {
                OneLineSystem.Push(
                    MailType.System,
                    L10nManager.Localize("ERROR_ACTION_POINT"),
                    NotificationCell.NotificationType.Alert);
                return;
            }

            if (!sheets.WorldSheet.TryGetByStageId(stageId, out var worldRow))
            {
                return;
            }

            var worldId = worldRow.Id;

            Find<LoadingScreen>().Show();

            var stage = Game.Game.instance.Stage;
            stage.IsExitReserved = false;
            stage.IsRepeatStage = false;
            var player = stage.GetPlayer();
            player.StartRun();
            ActionCamera.instance.ChaseX(player.transform);
            ActionRenderHandler.Instance.Pending = true;
            Game.Game.instance.ActionManager.HackAndSlash(player, worldId, stageId).Subscribe();
            LocalLayerModifier.ModifyAvatarActionPoint(States.Instance.CurrentAvatarState.address,
                -requiredCost);
            var props = new Value
            {
                ["StageID"] = stageId,
            };
            Analyzer.Instance.Track("Unity/Click Guided Quest Enter Dungeon", props);
        }

        public void GoToStage(BattleLog battleLog)
        {
            Game.Event.OnStageStart.Invoke(battleLog);
            Find<LoadingScreen>().Close();
            Close(true);
        }

        private void GoToCombinationEquipmentRecipe(int recipeId)
        {
            Analyzer.Instance.Track("Unity/Click Guided Quest Combination Equipment");

            CombinationClickInternal(() => Find<Craft>().Show(recipeId));
        }

        private void UpdateButtons()
        {
            btnQuest.Update();
            btnCombination.Update();
            btnShop.Update();
            btnRanking.Update();
            btnMimisbrunnr.Update();
            btnStaking.Update();

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

                //            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
                //if (arenaInfo != null)
                //{
                //    randomButton.interactable = arenaInfo.DailyChallengeCount > 0;
                //    randomButton.GetComponentInChildren<TextMeshProUGUI>().text =
                //        "Random X" + arenaInfo.DailyChallengeCount;

                //    if (arenaInfo.DailyChallengeCount > 0)
                //        arenaCount.text = $"{arenaInfo.DailyChallengeCount}/5";
                //    else
                //        arenaCount.text = $"<color=red>{arenaInfo.DailyChallengeCount}</color>/5";
                //}
                //else
                //    arenaCount.text = "<color=red>!</color>";
                ////|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
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

            Game.Game.instance.Stage.SelectedPlayer.gameObject.SetActive(false);
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

            AudioController.PlayClick();
        }

        public void RankingClick()
        {
            if (!btnRanking.IsUnlocked)
            {
                btnRanking.JingleTheCat();
                return;
            }

            if (btnRanking is ArenaMenu { State: ArenaMenu.ViewState.LoadingArenaData })
            {
                NotificationSystem.Push(
                    MailType.System,
                    L10nManager.Localize("UI_LOBBY_ARENA_MENU_LOADING_DATA"),
                    NotificationCell.NotificationType.Information);
                return;
            }

            Close(true);
            Find<ArenaJoin>().Show();
            Analyzer.Instance.Track("Unity/Enter arena page");
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

            Game.Game.instance.Stage.SelectedPlayer.gameObject.SetActive(false);
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

            if (States.Instance.StakingLevel < 1)
            {
                Find<StakingPopupNone>().Show();
            }
            else
            {
                Find<StakingPopup>().Show();
            }
        }

        public void UpdateGuideQuest(AvatarState avatarState)
        {
            guidedQuest.UpdateList(avatarState);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
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
            PandoraBoxMaster.SetCurrentPandoraPlayer(
                PandoraBoxMaster.GetPandoraPlayer(States.Instance.CurrentAvatarState.agentAddress.ToString()));
            string tmp = "_PandoraBox_Account_LoginProfile0" + PandoraBoxMaster.LoginIndex + "_Name";

            //set max login count
            if (PlayerPrefs.GetInt("_PandoraBox_General_IsPremiumLogin") != 1)
                PlayerPrefs.SetInt("_PandoraBox_General_IsPremiumLogin",
                    System.Convert.ToInt32(PandoraBoxMaster.CurrentPandoraPlayer.IsPremium()));
            PlayerPrefs.SetString(tmp, States.Instance.CurrentAvatarState.name); //save profile name

            //load favorite items
            PandoraBoxMaster.FavItems.Clear();
            for (int i = 0; i < PandoraBoxMaster.FavItemsMaxCount; i++) //fav max count
            {
                string key = "_PandoraBox_General_FavItems0" + i + "_" + States.Instance.CurrentAvatarState.address;
                if (i > 9)
                    key = "_PandoraBox_General_FavItems" + i + "_" + States.Instance.CurrentAvatarState.address;

                if (PlayerPrefs.HasKey(key))
                    PandoraBoxMaster.FavItems.Add(PlayerPrefs.GetString(key));
            }

            //ArenaCurrentPosition();
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

            stakingLevelIcon.sprite = SpriteHelper.GetStakingIcon(States.Instance.StakingLevel, true);
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
            Find<NoticePopup>().Close(true);
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

        public void UpdatePlayerReactButton(System.Action callback)
        {
            playerButton.onClick.RemoveAllListeners();
            playerButton.onClick.AddListener(() => callback?.Invoke());
        }

        public void TutorialActionHackAndSlash() => HackAndSlash(GuidedQuest.WorldQuest?.Goal ?? 1);

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
        protected override void OnEnable()
        {
            base.OnEnable();
            //StartCoroutine(arenaRemainsTime());
            StartCoroutine(ShowWhatsNew());

            DailyBonus.IsTrying = false;
        }
        /*
        public async void RandomArenaFight()
        {
            if (!PandoraBoxMaster.CurrentPandoraPlayer.IsPremium())
            {
                OneLineSystem.Push(MailType.System,
                    "<color=green>Pandora Box</color>: this is <color=green>PREMIUM</color> feature!",
                    NotificationCell.NotificationType.Alert);
                return;
            }

            if (PandoraUtil.IsBusy())
                return;

            var currentAddress = States.Instance.CurrentAvatarState?.address;
            ArenaInfo arenaInfoTickets = null;
            if (currentAddress.HasValue)
            {
                var avatarAddress2 = currentAddress.Value;
                var infoAddress = States.Instance.WeeklyArenaState.address.Derive(avatarAddress2.ToByteArray());
                var rawInfo = await Game.Game.instance.Agent.GetStateAsync(infoAddress);
                if (rawInfo is Dictionary dictionary)
                {
                    arenaInfoTickets = new ArenaInfo(dictionary);
                }
            }

            if (arenaInfoTickets.DailyChallengeCount == 0)
            {
                randomButton.interactable = false;
                return;
            }

            OneLineSystem.Push(MailType.System,
                "<color=green>Pandora Box</color>: Random Arena Fights Started Please wait...",
                NotificationCell.NotificationType.Information);

            randomButton.interactable = false;
            randomButton.GetComponentInChildren<TextMeshProUGUI>().text = "Fighting...";
            randomButton.transform.GetChild(0).gameObject.SetActive(true);
            PandoraBoxMaster.CurrentAction = PandoraUtil.ActionType.Ranking;


            BlockHash? _cachedBlockHash = null;

            WeeklyArenaState weeklyArenaState = null;
            var agent = Game.Game.instance.Agent;
            if (!_cachedBlockHash.Equals(agent.BlockTipHash))
            {
                _cachedBlockHash = agent.BlockTipHash;
                await UniTask.Run(async () =>
                {
                    var gameConfigState = States.Instance.GameConfigState;
                    var weeklyArenaIndex = (int)agent.BlockIndex / gameConfigState.WeeklyArenaInterval;
                    var weeklyArenaAddress = WeeklyArenaState.DeriveAddress(weeklyArenaIndex);
                    weeklyArenaState =
                        new WeeklyArenaState((Bencodex.Types.Dictionary)await agent.GetStateAsync(weeklyArenaAddress));
                    States.Instance.SetWeeklyArenaState(weeklyArenaState);
                    await UpdateWeeklyCache(States.Instance.WeeklyArenaState);
                });
            }

            //var weeklyArenaState = States.Instance.WeeklyArenaState;
            if (weeklyArenaState is null)
                return;

            var avatarAddress = States.Instance.CurrentAvatarState?.address;
            if (!avatarAddress.HasValue)
                return;

            if (!_weeklyCachedInfo.Any())
            {
                //UpdateBoard();
                return;
            }

            ArenaInfo arenaInfo = _weeklyCachedInfo[0].arenaInfo;
            if (!arenaInfo.Active)
            {
                arenaInfo.Activate();
            }

            //Debug.LogError(arenaInfo.AvatarName + " " + arenaInfo.CombatPoint);

            //find myself
            var currentArenaInfo = _weeklyCachedInfo[0];
            for (int i = 0; i < _weeklyCachedInfo.Count; i++)
                if (_weeklyCachedInfo[i].arenaInfo.AvatarAddress == States.Instance.CurrentAvatarState.address)
                {
                    currentArenaInfo = _weeklyCachedInfo[i];
                    break;
                }

            //find suitable enemy
            var selectedEnemyArenaInfo = _weeklyCachedInfo[0];
            int tryLowerCP = 0; //try to find lower rank and lower cp, after this pass value we search for anyone!
            while
                (tryLowerCP++ <
                 50) //(selectedEnemyArenaInfo.arenaInfo.AvatarAddress == States.Instance.CurrentAvatarState.address)
            {
                System.Random rnd = new System.Random();
                var tmpInfo = _weeklyCachedInfo[rnd.Next(0, _weeklyCachedInfo.Count)];
                if (tmpInfo.arenaInfo.AvatarAddress != States.Instance.CurrentAvatarState.address)
                {
                    if (tmpInfo.rank < currentArenaInfo.rank && tmpInfo.arenaInfo.CombatPoint <
                        currentArenaInfo.arenaInfo.CombatPoint - 10000)
                    {
                        selectedEnemyArenaInfo = tmpInfo;
                        break;
                    }
                }

                if (tryLowerCP > 45 && tmpInfo.arenaInfo.AvatarAddress != States.Instance.CurrentAvatarState.address)
                {
                    selectedEnemyArenaInfo = tmpInfo;
                    break;
                }
            }

            //final attack
            //Widget.Find<ArenaBattleLoadingScreen>().Show(new ArenaInfo(selectedEnemyArenaInfo.arenaInfo)); <-- for visual simulate

            Game.Character.Player _player = Game.Game.instance.Stage.GetPlayer();
            var currentAvatarInventory = States.Instance.CurrentAvatarState.inventory;

            for (int i = 0; i < arenaInfoTickets.DailyChallengeCount; i++) //arenaInfoTickets.DailyChallengeCount
            {
                await Task.Delay(500);
                //yield return new WaitForSeconds(PandoraBoxMaster.ActionCooldown);
                Game.Game.instance.ActionManager.RankingBattle(
                    selectedEnemyArenaInfo.arenaInfo.AvatarAddress,
                    currentAvatarInventory.Costumes
                        .Where(i => i.equipped)
                        .Select(i => i.ItemId).ToList(),
                    currentAvatarInventory.Equipments
                        .Where(i => i.equipped)
                        .Select(i => i.ItemId).ToList()
                ).Subscribe();

                OneLineSystem.Push(MailType.System,
                    $"<color=green>Pandora Box</color>: Random selected (#<color=red>{selectedEnemyArenaInfo.rank}</color>, {selectedEnemyArenaInfo.arenaInfo.AvatarName}) <color=green>" +
                    (i + 1)
                    + "</color>/" + arenaInfoTickets.DailyChallengeCount + "!",
                    NotificationCell.NotificationType.Information);
            }
        }

        private async Task UpdateWeeklyCache(WeeklyArenaState state)
        {
            // For backward compatibility, create list based on previous week.
            if (!_arenaInfoList.Locked)
            {
                var prevWeekAddress = ArenaHelper.GetPrevWeekAddress();
                var rawWeekly = await Game.Game.instance.Agent.GetStateAsync(prevWeekAddress);
                var prevWeekly = new WeeklyArenaState(rawWeekly);
                var copiedState = new WeeklyArenaState(state.address);
                copiedState.Update(prevWeekly, state.ResetIndex);
                _arenaInfoList.Update(copiedState, true);
            }

            var rawList =
                await Game.Game.instance.Agent.GetStateAsync(
                    state.address.Derive("address_list"));
            if (rawList is List list)
            {
                List<Address> avatarAddressList = list.ToList(Nekoyume.Model.State.StateExtensions.ToAddress);
                List<Address> arenaInfoAddressList = new List<Address>();
                foreach (var avatarAddress in avatarAddressList)
                {
                    var arenaInfoAddress = state.address.Derive(avatarAddress.ToByteArray());
                    if (!arenaInfoAddressList.Contains(arenaInfoAddress))
                    {
                        arenaInfoAddressList.Add(arenaInfoAddress);
                    }
                }

                Dictionary<Address, IValue> result =
                    await Game.Game.instance.Agent.GetStateBulk(arenaInfoAddressList);
                var infoList = new List<ArenaInfo>();
                foreach (var iValue in result.Values)
                {
                    if (iValue is Dictionary dictionary)
                    {
                        var info = new ArenaInfo(dictionary);
                        infoList.Add(info);
                    }
                }

                _arenaInfoList.Update(infoList);
            }


            int upper = 10;
            int lower = 10;

            var currentAvatarAddress = States.Instance.CurrentAvatarState.address;

            var infos = _arenaInfoList.GetArenaInfos(currentAvatarAddress, upper, lower);
            // Player does not play prev & this week arena.
            if (!infos.Any() && state.OrderedArenaInfos.Any())
            {
                var address = state.OrderedArenaInfos.Last().AvatarAddress;
                infos = state.GetArenaInfos(30, 0);
            }

            infos = infos.ToImmutableHashSet().OrderBy(tuple => tuple.rank).ToList();

            var addressList = infos.Select(i => i.arenaInfo.AvatarAddress).ToList();
            var avatarStates = await Game.Game.instance.Agent.GetAvatarStates(addressList);

            _weeklyCachedInfo = infos
                .Select(tuple =>
                {
                    var avatarAddress = tuple.arenaInfo.AvatarAddress;
                    if (!avatarStates.ContainsKey(avatarAddress))
                    {
                        return (0, null);
                    }

                    var avatarState = avatarStates[avatarAddress];

                    var arenaInfo = tuple.arenaInfo;
#pragma warning disable 618
                    arenaInfo.Level = avatarState.level;
                    arenaInfo.ArmorId = avatarState.GetArmorIdForPortrait();
                    arenaInfo.CombatPoint = avatarState.GetCP();
#pragma warning restore 618
                    return tuple;
                })
                .Select(t => t)
                .Where(tuple => tuple.rank > 0)
                .ToList();
        }
        */
        public void ClearRemainingTickets()
        {
            randomButton.GetComponentInChildren<TextMeshProUGUI>().text = "Random X0";
            arenaCount.text = $"<color=red>{0}</color>/5";
            randomButton.transform.GetChild(0).gameObject.SetActive(false);
        }

        public void ForceShowMenu()
        {
            Widget.Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Main);
            Find<HeaderMenuStatic>().Show();
        }

        public void ShowChronoTool()
        {
            Find<ChronoSlotsPopup>().Show();
        }

        public void ShowMiniGame()
        {
            Find<NineRunnerPopup>().Show();
        }

        public void FastCharacterSwitch()
        {
            if (PandoraUtil.IsBusy())
                return;

            if (Game.Game.instance.IsInWorld)
            {
                NotificationSystem.Push(Nekoyume.Model.Mail.MailType.System,
                    L10nManager.Localize("UI_BLOCK_EXIT"), NotificationCell.NotificationType.Information);
                return;
            }

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

        IEnumerator ShowWhatsNew()
        {
            yield return new WaitForSeconds(2);
            if (!PandoraBoxMaster.Instance.Settings.WhatsNewShown)
            {
                AudioController.instance.PlaySfx("sfx_bgm_great_success");
                PandoraBoxMaster.Instance.UIWhatsNew.SetActive(true);
            }
        }
        /*
        public async void ArenaCurrentPosition()
        {
            //remove arena lock
            var currentAddress = States.Instance.CurrentAvatarState?.address;
            if (currentAddress.HasValue)
            {
                ArenaInfo arenaInfo = null;
                var avatarAddress = currentAddress.Value;
                var infoAddress = States.Instance.WeeklyArenaState.address.Derive(avatarAddress.ToByteArray());
                var rawInfo = await Game.Game.instance.Agent.GetStateAsync(infoAddress);
                if (rawInfo is Dictionary dictionary)
                {
                    arenaInfo = new ArenaInfo(dictionary);
                }

                if (arenaInfo.DailyChallengeCount == 0)
                {
                    PandoraBoxMaster.IsRankingSimulate = false;
                    ClearRemainingTickets();
                }
            }


            arenaCurrentPositionText.text = "";
            await Task.Delay(2000);
            if (!PandoraBoxMaster.CurrentPandoraPlayer.IsPremium())
                return;

            while (true)
            {
                BlockHash? _cachedBlockHash = null;

                WeeklyArenaState weeklyArenaState = null;
                var agent = Game.Game.instance.Agent;
                try
                { 
                    if (!_cachedBlockHash.Equals(agent.BlockTipHash))
                    {
                        _cachedBlockHash = agent.BlockTipHash;
                        await UniTask.Run(async () =>
                        {
                            var gameConfigState = States.Instance.GameConfigState;
                            var weeklyArenaIndex = (int)agent.BlockIndex / gameConfigState.WeeklyArenaInterval;
                            var weeklyArenaAddress = WeeklyArenaState.DeriveAddress(weeklyArenaIndex);
                            weeklyArenaState =
                                new WeeklyArenaState(
                                    (Bencodex.Types.Dictionary)await agent.GetStateAsync(weeklyArenaAddress));
                            States.Instance.SetWeeklyArenaState(weeklyArenaState);
                            await UpdateWeeklyCache(States.Instance.WeeklyArenaState);
                        });
                    }
                } catch { }

                //var weeklyArenaState = States.Instance.WeeklyArenaState;
                if (weeklyArenaState is null)
                    return;

                var avatarAddress = States.Instance.CurrentAvatarState?.address;
                if (!avatarAddress.HasValue)
                    return;

                if (!_weeklyCachedInfo.Any())
                {
                    //UpdateBoard();
                    return;
                }

                ArenaInfo arenaInfo = _weeklyCachedInfo[0].arenaInfo;
                if (!arenaInfo.Active)
                {
                    arenaInfo.Activate();
                }

                var currentArenaInfo = _weeklyCachedInfo[0];
                for (int i = 0; i < _weeklyCachedInfo.Count; i++)
                    if (_weeklyCachedInfo[i].arenaInfo.AvatarAddress == States.Instance.CurrentAvatarState.address)
                    {
                        currentArenaInfo = _weeklyCachedInfo[i];
                        break;
                    }

                arenaCurrentPositionText.text = "#" + currentArenaInfo.rank + $" ({currentArenaInfo.arenaInfo.Score})";

                await Task.Delay(600000); //300000 = 5 minutes
            }
        }

        IEnumerator arenaRemainsTime()
        {
            arenaRemains.text = "";
            yield return new WaitForSeconds(1);
            var gameConfigState = States.Instance.GameConfigState;
            while (true)
            {
                float maxTime = States.Instance.GameConfigState.DailyArenaInterval;
                var weeklyArenaState = States.Instance.WeeklyArenaState;
                long _resetIndex = weeklyArenaState.ResetIndex;
                float value;

                value = Game.Game.instance.Agent.BlockIndex - _resetIndex;
                var remainBlock = gameConfigState.DailyArenaInterval - value;
                var time = Util.GetBlockToTime((int)remainBlock);


                if (PandoraBoxMaster.Instance.Settings.BlockShowType == 0)
                    arenaRemains.text = time;
                else if (PandoraBoxMaster.Instance.Settings.BlockShowType == 1)
                    arenaRemains.text = $"({value}/{gameConfigState.DailyArenaInterval})";
                else
                    arenaRemains.text = $"{time} ({remainBlock})";

                yield return new WaitForSeconds(3);
            }
        }
        */

        //IEnumerator arenaRemainsCount()
        //{
        //    yield return new WaitForSeconds(2);
        //    var gameConfigState = States.Instance.GameConfigState;
        //    while (true)
        //    {
        //        while (States.Instance == null)
        //        {
        //            yield return new WaitForSeconds(2);
        //        }

        //        //current local player
        //        var currentAddress = States.Instance.CurrentAvatarState?.address;
        //        var arenaInfo = States.Instance.WeeklyArenaState.GetArenaInfo(currentAddress.Value);

        //        //Debug.LogError(States.Instance.WeeklyArenaState == null);

        //        rankingExclamationMark.gameObject.SetActive(
        //            btnRanking.IsUnlocked &&
        //            (arenaInfo == null || arenaInfo.DailyChallengeCount > 0));

        //        if (arenaInfo != null)
        //        {
        //            if (arenaInfo.DailyChallengeCount > 0)
        //                arenaCount.text = $"{arenaInfo.DailyChallengeCount}/5";
        //            else
        //                arenaCount.text = $"<color=red>{arenaInfo.DailyChallengeCount}</color>/5";
        //        }
        //        else
        //            arenaCount.text = "<color=red>!</color>";
        //        yield return new WaitForSeconds(10);
        //    }
        //}

        public void ShowPandoraSettings()
        {
            AudioController.PlayClick();
            PandoraBoxMaster.Instance.UISettings.SetActive(true);
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


#if UNITY_EDITOR
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
#endif
    }
}
