using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.State;
using Nekoyume.UI.Module;
using Nekoyume.Model.BattleStatus;
using UnityEngine;
using Random = UnityEngine.Random;
using mixpanel;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using Nekoyume.Helper;
    using Nekoyume.UI.Scroller;
    using PandoraBox;
    using TMPro;
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
        [Header("PANDORA CUSTOM FIELDS")]
        [SerializeField] private TextMeshProUGUI arenaRemains;
        [SerializeField] private TextMeshProUGUI arenaCount;
        [Space(50)]
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

        [SerializeField]
        private MainMenu btnQuest = null;

        [SerializeField]
        private MainMenu btnCombination = null;

        [SerializeField]
        private MainMenu btnShop = null;

        [SerializeField]
        private MainMenu btnRanking = null;

        [SerializeField]
        private MainMenu btnMimisbrunnr = null;

        [SerializeField]
        private SpeechBubble[] speechBubbles = null;

        [SerializeField]
        private GameObject shopExclamationMark = null;

        [SerializeField]
        private GameObject combinationExclamationMark = null;

        [SerializeField]
        private GameObject rankingExclamationMark = null;

        [SerializeField]
        private GameObject questExclamationMark = null;

        [SerializeField]
        private GameObject mimisbrunnrExclamationMark = null;

        [SerializeField]
        private GuidedQuest guidedQuest = null;

        [SerializeField]
        private Button playerButton;

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
                    btnShop.GetComponent<Button>()
                };
                buttonList.ForEach(button => button.interactable = stateType == AnimationStateType.Shown);
            }).AddTo(gameObject);
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
            Game.Game.instance.ActionManager.HackAndSlash(player, worldId, stageId, 1).Subscribe();
            LocalLayerModifier.ModifyAvatarActionPoint(States.Instance.CurrentAvatarState.address,
                - requiredCost);
            var props = new Value
            {
                ["StageID"] = stageId,
            };
            Analyzer.Instance.Track("Unity/Click Guided Quest Enter Dungeon", props);
        }

        public void GoToStage(BattleLog battleLog)
        {
            Debug.LogError("HACK: STEP 11");
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

            var addressHex = States.Instance.CurrentAvatarState.address.ToHex();
            var firstOpenCombinationKey = string.Format(FirstOpenCombinationKeyFormat, addressHex);
            var firstOpenShopKey = string.Format(FirstOpenShopKeyFormat, addressHex);
            var firstOpenQuestKey = string.Format(FirstOpenQuestKeyFormat, addressHex);
            var firstOpenMimisbrunnrKey = string.Format(FirstOpenMimisbrunnrKeyFormat, addressHex);

            combinationExclamationMark.gameObject.SetActive(
                btnCombination.IsUnlocked &&
                (PlayerPrefs.GetInt(firstOpenCombinationKey, 0) == 0 ||
                 Craft.SharedModel.HasNotification));
            shopExclamationMark.gameObject.SetActive(
                btnShop.IsUnlocked &&
                PlayerPrefs.GetInt(firstOpenShopKey, 0) == 0);

            var currentAddress = States.Instance.CurrentAvatarState?.address;
            if (currentAddress != null)
            {
                var arenaInfo = States.Instance.WeeklyArenaState.GetArenaInfo(currentAddress.Value);
                rankingExclamationMark.gameObject.SetActive(
                    btnRanking.IsUnlocked &&
                    (arenaInfo == null || arenaInfo.DailyChallengeCount > 0));
            }

            var worldMap = Find<WorldMap>();
            worldMap.UpdateNotificationInfo();
            var hasNotificationInWorldMap = worldMap.HasNotification;

            questExclamationMark.gameObject.SetActive((btnQuest.IsUnlocked && PlayerPrefs.GetInt(firstOpenQuestKey, 0) == 0) || hasNotificationInWorldMap);
            mimisbrunnrExclamationMark.gameObject.SetActive((btnMimisbrunnr.IsUnlocked && PlayerPrefs.GetInt(firstOpenMimisbrunnrKey, 0) == 0));
        }

        private void HideButtons()
        {
            btnQuest.gameObject.SetActive(false);
            btnCombination.gameObject.SetActive(false);
            btnShop.gameObject.SetActive(false);
            btnRanking.gameObject.SetActive(false);
            btnMimisbrunnr.gameObject.SetActive(false);
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
            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Battle);
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

            Close(true);
            Find<RankingBoard>().Show();
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
            stageInfo.Show(SharedViewModel, worldRow, StageInformation.StageType.Mimisbrunnr);
            var status = Find<Status>();
            status.Close(true);
            Find<EventBanner>().Close(true);
            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Battle);
            HelpTooltip.HelpMe(100019, true);
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
            PandoraBoxMaster.SetCurrentPandoraPlayer(PandoraBoxMaster.GetPandoraPlayer(States.Instance.CurrentAvatarState.agentAddress.ToString()));
            string tmp = "_PandoraBox_Account_LoginProfile0" + PandoraBoxMaster.LoginIndex + "_Name";
            PlayerPrefs.SetString(tmp, States.Instance.CurrentAvatarState.name); //save profile name
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
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

            // Temporarily lock tutorial recipe.
            var skipMap = Craft.SharedModel.RecipeVFXSkipList;
            if (skipMap.Contains(firstRecipeRow.Id))
            {
                skipMap.Remove(firstRecipeRow.Id);
            }
            Craft.SharedModel.SaveRecipeVFXSkipList();
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
            StartCoroutine(arenaRemainsTime());
            StartCoroutine(arenaRemainsCount());
            StartCoroutine(ShowWhatsNew());

            DailyBonus.IsTrying = false;
        }



        public void MiniGameShow()
        {
            OneLineSystem.Push(MailType.System, "<color=green>Pandora Box</color>: This Feature coming soon!", NotificationCell.NotificationType.Information);
            return;
        }

        public void ShowSelectCharacter()
        {
            if (Game.Game.instance.Stage.IsInStage)
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

        IEnumerator arenaRemainsTime()
        {
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

        IEnumerator arenaRemainsCount()
        {
            yield return new WaitForSeconds(2);
            var gameConfigState = States.Instance.GameConfigState;
            while (true)
            {
                while (States.Instance == null)
                {
                    yield return new WaitForSeconds(2);
                }

                //current local player
                var currentAddress = States.Instance.CurrentAvatarState?.address;
                var arenaInfo = States.Instance.WeeklyArenaState.GetArenaInfo(currentAddress.Value);

                //Debug.LogError(States.Instance.WeeklyArenaState == null);

                rankingExclamationMark.gameObject.SetActive(
                    btnRanking.IsUnlocked &&
                    (arenaInfo == null || arenaInfo.DailyChallengeCount > 0));

                if (arenaInfo != null)
                {
                    if (arenaInfo.DailyChallengeCount > 0)
                        arenaCount.text = $"{arenaInfo.DailyChallengeCount}/5";
                    else
                        arenaCount.text = $"<color=red>{arenaInfo.DailyChallengeCount}</color>/5";
                }
                else
                    arenaCount.text = "<color=red>!</color>";
                yield return new WaitForSeconds(10);
            }
        }

        public void ShowPandoraSettings()
        {
            AudioController.PlayClick();
            PandoraBoxMaster.Instance.UISettings.SetActive(true);
        }

        public void ShowNFT()
        {
            Application.OpenURL("https://discord.gg/yfcP5GzqQ7");
        }

        public void FastCharacterSwitch()
        {
            Game.Game.instance.BackToNest();
            AudioController.PlayClick();
        }

        public void FastArenaEnter()
        {
            Close(true);
            Find<RankingBoard>().gameObject.SetActive(true);
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
