using System;
using Nekoyume.Battle;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Scroller
{
    using Nekoyume.PandoraBox;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using UniRx;

    public class ArenaRankCell : RectCell<
        ArenaRankCell.ViewModel,
        ArenaRankScroll.ContextModel>
    {
        public class ViewModel
        {
            public int rank;
            public ArenaInfo arenaInfo;
            public ArenaInfo currentAvatarArenaInfo;
            public bool currentAvatarCanBattle;
        }

        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        [Header("PANDORA CUSTOM FIELDS")]
        //[SerializeField] private Sprite[] Banners = null;

        //[SerializeField] private GameObject challengeButton = null;
        [SerializeField] private Transform bannerHolder = null;
        [SerializeField] private Image rarityMockupImage = null;

        //[SerializeField] private GameObject playerBanner = null;
        [SerializeField] private TextMeshProUGUI gainPointText = null;
        [SerializeField] private TextMeshProUGUI gainRealPointText = null;
        [SerializeField] private TextMeshProUGUI extraInfoText = null;
        [SerializeField] private TextMeshProUGUI winRateText = null;
        [SerializeField] private GameObject FavTarget = null;
        public GameObject BlinkSelected = null;

        //for simulate
        //ViewModel currentViewModel = null; //we will get this when cell show
        ArenaInfo currentAvatarArenaInfo = null;

        //guild info
        GuildPlayer enemyGuildPlayer;

        [Space(50)]
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
        [SerializeField]
        private Image backgroundImage = null;

        [SerializeField] private bool controlBackgroundImage = false;

        [SerializeField] private GameObject rankImageContainer = null;

        [SerializeField] private Image rankImage = null;

        [SerializeField] private GameObject rankTextContainer = null;

        [SerializeField] private TextMeshProUGUI rankText = null;

        [SerializeField] private DetailedCharacterView characterView = null;

        [SerializeField] private TextMeshProUGUI nameText = null;

        [SerializeField] private TextMeshProUGUI scoreText = null;

        [SerializeField] private TextMeshProUGUI cpText = null;

        [SerializeField] private GameObject challengeCountTextContainer = null;

        [SerializeField] private TextMeshProUGUI challengeCountText = null;

        [SerializeField] private Button avatarInfoButton = null;

        [SerializeField] private ConditionalButton challengeButton = null;

        private RectTransform _rectTransformCache;
        private bool _isCurrentUser;
        private ViewModel _viewModel;
        private readonly Subject<ArenaRankCell> _onClickAvatarInfo = new Subject<ArenaRankCell>();
        private readonly Subject<ArenaRankCell> _onClickChallenge = new Subject<ArenaRankCell>();

        public RectTransform RectTransform => _rectTransformCache
            ? _rectTransformCache
            : _rectTransformCache = GetComponent<RectTransform>();

        public ArenaInfo ArenaInfo { get; private set; }

        public IObservable<ArenaRankCell> OnClickAvatarInfo => _onClickAvatarInfo;

        public IObservable<ArenaRankCell> OnClickChallenge => _onClickChallenge;

        private static bool IsLoadingAvatarState = false;

        private void Awake()
        {
            characterView.OnClickCharacterIcon
                .Subscribe(async avatarState =>
                {
                    //|||||||||||||| PANDORA START CODE |||||||||||||||||||
                    Widget.Find<RankingBoard>().avatarLoadingImage.SetActive(true);
                    Widget.Find<FriendInfoPopupPandora>().Close(true);
                    //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

                    if (IsLoadingAvatarState)
                    {
                        return;
                    }

                    if (avatarState is null)
                    {
                        IsLoadingAvatarState = true;
                        var (exist, state) =
                            await States.TryGetAvatarStateAsync(ArenaInfo.AvatarAddress);
                        avatarState = exist ? state : null;
                        IsLoadingAvatarState = false;
                        if (avatarState is null)
                        {
                            return;
                        }
                    }

                    //|||||||||||||| PANDORA START CODE |||||||||||||||||||currentViewModel
                    var currentAddress = States.Instance.CurrentAvatarState?.address;
                    //var arenaInfo = States.Instance.WeeklyArenaState.GetArenaInfo(currentAddress.Value);
                    //Widget.Find<FriendInfoPopupPandora>().Show(avatarState, ArenaInfo, arenaInfo, true);

                    //clear selected cells and select the new one.
                    Transform clc = Widget.Find<RankingBoard>().CellsListContainer;
                    foreach (Transform item in clc)
                        item.GetComponent<ArenaRankCell>().BlinkSelected.SetActive(false);
                    BlinkSelected.SetActive(true);

                    //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

                    //Widget.Find<FriendInfoPopup>().Show(avatarState);
                })
                .AddTo(gameObject);

            avatarInfoButton.OnClickAsObservable()
                .ThrottleFirst(new TimeSpan(0, 0, 1))
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    Context.OnClickAvatarInfo.OnNext(this);
                    _onClickAvatarInfo.OnNext(this);
                })
                .AddTo(gameObject);

            challengeButton.OnSubmitSubject
                .ThrottleFirst(new TimeSpan(0, 0, 1))
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    //Context.OnClickChallenge.OnNext(this);
                    //_onClickChallenge.OnNext(this);

                    //|||||||||||||| PANDORA START CODE |||||||||||||||||||
                    ChallangeRemainingTickets();
                    //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
                })
                .AddTo(gameObject);

            challengeButton.OnClickDisabledSubject
                .ThrottleFirst(TimeSpan.FromSeconds(2f))
                .Subscribe(_ =>
                {
                    OneLineSystem.Push(MailType.System, L10nManager.Localize("UI_EQUIP_FAILED"),
                        NotificationCell.NotificationType.Alert);
                }).AddTo(gameObject);

            Game.Event.OnUpdatePlayerEquip
                .Where(_ => _isCurrentUser)
                .Subscribe(player =>
                {
                    characterView.SetByPlayer(player);

                    cpText.text = CPHelper.GetCPV2(
                        States.Instance.CurrentAvatarState,
                        Game.Game.instance.TableSheets.CharacterSheet,
                        Game.Game.instance.TableSheets.CostumeStatSheet).ToString();
                })
                .AddTo(gameObject);
        }

        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        public void ChallangeRemainingTickets()
        {
            Widget.Find<RankingBoard>().OldScore = _viewModel.currentAvatarArenaInfo.Score;
            AudioController.PlayClick();
            if (PandoraBoxMaster.CurrentPandoraPlayer.IsPremium())
            {
                StartCoroutine(StartFightCount());
            }
            else
            {
                if (PandoraBoxMaster.ArenaTicketsToUse > 1)
                {
                    OneLineSystem.Push(MailType.System,
                        "<color=green>Pandora Box</color>: This is Premium Feature! Please select 1 Fight",
                        NotificationCell.NotificationType.Alert);
                }
                else
                {
                    if (PandoraBoxMaster.ArenaTicketsToUse == 1)
                    {
                        Widget.Find<FriendInfoPopupPandora>().Close(true);
                        Context.OnClickChallenge.OnNext(this);
                        _onClickChallenge.OnNext(this);
                    }
                }
            }
        }

        IEnumerator StartFightCount()
        {
            Widget.Find<FriendInfoPopupPandora>().Close(true);

            var currentAddress = States.Instance.CurrentAvatarState?.address;
            //var arenaInfo = States.Instance.WeeklyArenaState.GetArenaInfo(currentAddress.Value);

            //Debug.LogError("Fights Count: " + PandoraBoxMaster.ArenaTicketsToUse);
            //for (int i = 0; i < arenaInfo.DailyChallengeCount; i++)

            var currentAvatarInventory = States.Instance.CurrentAvatarState.inventory;


            Widget.Find<ArenaBattleLoadingScreen>().Show(ArenaInfo);


            for (int i = 0;
                 i < Mathf.Clamp(PandoraBoxMaster.ArenaTicketsToUse, 0,
                     _viewModel.currentAvatarArenaInfo.DailyChallengeCount);
                 i++)
            {
                //Context.OnClickChallenge.OnNext(this);
                //_onClickChallenge.OnNext(this);

                yield return new WaitForSeconds(PandoraBoxMaster.ActionCooldown);

                Game.Game.instance.ActionManager.RankingBattle(
                    ArenaInfo.AvatarAddress,
                    currentAvatarInventory.Costumes
                        .Where(i => i.equipped)
                        .Select(i => i.ItemId).ToList(),
                    currentAvatarInventory.Equipments
                        .Where(i => i.equipped)
                        .Select(i => i.ItemId).ToList()
                ).Subscribe();

                OneLineSystem.Push(MailType.System, "<color=green>Pandora Box</color>: Fight Arena <color=green>" +
                                                    (i + 1)
                                                    + "</color>/" + Mathf.Clamp(PandoraBoxMaster.ArenaTicketsToUse, 0,
                                                        _viewModel.currentAvatarArenaInfo.DailyChallengeCount) + "!",
                    NotificationCell.NotificationType.Information);
            }
        }

        private class LocalRandom : System.Random, Libplanet.Action.IRandom
        {
            public LocalRandom(int Seed)
                : base(Seed)
            {
            }

            public int Seed => throw new NotImplementedException();
        }

        public void SimulateOnce()
        {
            var enemyState = Widget.Find<RankingBoard>().avatarStatesPandora
                .FirstOrDefault(t => t.Value.address == ArenaInfo.AvatarAddress);

            var simulator = new RankingSimulator(
                new Cheat.DebugRandom(),
                States.Instance.CurrentAvatarState,
                enemyState.Value,
                new List<Guid>(),
                Game.Game.instance.TableSheets.GetRankingSimulatorSheets(),
                999999
            );

            //System.Random rnd = new System.Random();
            //var simulator = new RankingSimulator(
            //    new LocalRandom(rnd.Next(-1000000000, 1000000000)),
            //    States.Instance.CurrentAvatarState,
            //    enemyState.Value,
            //    new List<Guid>(),
            //    Game.Game.instance.TableSheets.GetRankingSimulatorSheets(),
            //    Action.RankingBattle.StageId,
            //    currentAvatarArenaInfo,
            //    ArenaInfo,
            //    Game.Game.instance.TableSheets.CostumeStatSheet
            //);
            simulator.Simulate();
            var log = simulator.Log;

            Widget.Find<FriendInfoPopupPandora>().Close(true);
            PandoraBoxMaster.IsRankingSimulate = true;
            Widget.Find<RankingBoard>().GoToStage(log);
        }

        IEnumerator GetEnemyState()
        {
            winRateText.text = ".?.";

            var enemyState = Widget.Find<RankingBoard>().avatarStatesPandora
                .FirstOrDefault(t => t.Value.address == ArenaInfo.AvatarAddress);

            //current local player
            var currentAddress = States.Instance.CurrentAvatarState?.address;
            //var arenaInfo = States.Instance.WeeklyArenaState.GetArenaInfo(currentAddress.Value);
            //


            int totalSimulations = 5;
            int win = 0;
            for (int i = 0; i < totalSimulations; i++)
            {
                var simulator = new RankingSimulator(
                    new Cheat.DebugRandom(),
                    States.Instance.CurrentAvatarState,
                    enemyState.Value,
                    new List<Guid>(),
                    Game.Game.instance.TableSheets.GetRankingSimulatorSheets(),
                    999999
                );
                simulator.Simulate();
                var log = simulator.Log;
                if (log.result.ToString().ToUpper() == "WIN")
                    win++;
                yield return new WaitForSeconds(0.05f);
            }

            //gainRealPointText value
            int AccumilatedEnemyScore = 0;
            int AccumilatedPlayerScore = 0;
            for (int i = 0; i < PandoraBoxMaster.ArenaTicketsToUse; i++)
            {
                var simulator = new RankingSimulator(
                    new Cheat.DebugRandom(),
                    States.Instance.CurrentAvatarState,
                    enemyState.Value,
                    new List<Guid>(),
                    Game.Game.instance.TableSheets.GetRankingSimulatorSheets(),
                    999999
                );
                simulator.Simulate();
                var log = simulator.Log;
                var (challengerScore, defenderScore) = ArenaScoreHelper.GetScore(
                    _viewModel.currentAvatarArenaInfo.Score + AccumilatedPlayerScore,
                    _viewModel.arenaInfo.Score + AccumilatedEnemyScore, log.result);
                AccumilatedEnemyScore += defenderScore;
                AccumilatedPlayerScore += challengerScore;
                yield return new WaitForSeconds(0.05f);
            }


            //Debug.LogError(battleLogDebug);

            float finalRatio = (float)win / (float)totalSimulations;
            float FinalValue = (int)(finalRatio * 100f);

            //if (finalRatio == 1)
            //    effect.SetActive(true);

            if (finalRatio <= 0.5f)
                winRateText.text = $"<color=red>{FinalValue}</color>%";
            else if (finalRatio > 0.5f && finalRatio <= 0.75f)
                winRateText.text = $"<color=#FF4900>{FinalValue}</color>%";
            else
                winRateText.text = $"<color=green>{FinalValue}</color>%";


            if (AccumilatedPlayerScore / (PandoraBoxMaster.ArenaTicketsToUse * 20) >= 1)
                gainRealPointText.text = "<color=green>+" + AccumilatedPlayerScore + "</color>";
            else if (((float)AccumilatedPlayerScore / (float)(PandoraBoxMaster.ArenaTicketsToUse * 20f)) < 0)
            {
                gainRealPointText.text = "<color=red>" + AccumilatedPlayerScore + "</color>";
            }
            else
            {
                gainRealPointText.text = "<color=red>+" + AccumilatedPlayerScore + "</color>";
            }

            gainRealPointText.gameObject.SetActive(true);


            //if (Widget.Find<ArenaBattleLoadingScreen>().IsActive())
            //{
            //    Widget.Find<RankingBoard>().GoToStage(log);
            //}
        }

        public void ShowGuildInfo()
        {
            if (enemyGuildPlayer is null)
                return;
            Widget.Find<GuildInfo>().Show(enemyGuildPlayer.Guild);
        }

        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

        public void Show((
            int rank,
            ArenaInfo arenaInfo,
            ArenaInfo currentAvatarArenaInfo,
            bool currentAvatarCanBattle) itemData)
        {
            Show(new ViewModel
            {
                rank = itemData.rank,
                arenaInfo = itemData.arenaInfo,
                currentAvatarArenaInfo = itemData.currentAvatarArenaInfo,
                currentAvatarCanBattle = itemData.currentAvatarCanBattle,
            });
        }

        private void Start()
        {
            Context?.UpdateConditionalStateOfChallengeButtons
                .Subscribe(UpdateChallengeButton)
                .AddTo(gameObject);
        }

        public void ShowMyDefaultInfo()
        {
            UpdateRank(-1);

            var currentAvatarState = States.Instance.CurrentAvatarState;
            characterView.SetByAvatarState(currentAvatarState);
            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            ForceChangePortraitImage(currentAvatarState.address.ToString());
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
            nameText.text = currentAvatarState.NameWithHash;
            scoreText.text = "-";
            cpText.text = "-";

            challengeCountTextContainer.SetActive(true);
            challengeButton.gameObject.SetActive(false);
            challengeCountText.text =
                $"<color=orange>{GameConfig.ArenaChallengeCountMax}</color>/{GameConfig.ArenaChallengeCountMax}";
        }

        public override void UpdateContent(ViewModel itemData)
        {
            _viewModel = itemData;
            if (_viewModel is null)
            {
                Debug.LogError($"Argument is null. {nameof(itemData)}");
                return;
            }

            ArenaInfo = _viewModel.arenaInfo ?? throw new ArgumentNullException(nameof(_viewModel.arenaInfo));
            var currentAvatarArenaInfo = _viewModel.currentAvatarArenaInfo;
            _isCurrentUser = currentAvatarArenaInfo is { } &&
                             ArenaInfo.AvatarAddress == currentAvatarArenaInfo.AvatarAddress;

            if (controlBackgroundImage)
            {
                backgroundImage.enabled = Index % 2 == 1;
            }

            UpdateRank(_viewModel.rank);


            //|||||||||||||| PANDORA START CODE |||||||||||||||||||

            //set background color depend on rank
            if (_viewModel.rank % 2 == 0)
            {
                GetComponent<Image>().color = new Color(14f / 256f, 14f / 256f, 14f / 256f);
            }
            else
            {
                GetComponent<Image>().color = new Color(51f / 256f, 47f / 256f, 37f / 256f);
            }
                rarityMockupImage.color = new Color(14f / 256f, 14f / 256f, 14f / 256f);

            if (Widget.Find<FriendInfoPopupPandora>().enemyArenaInfo is null)
                BlinkSelected.SetActive(false);
            else
                BlinkSelected.SetActive(itemData.arenaInfo.AvatarAddress ==
                                        Widget.Find<FriendInfoPopupPandora>().enemyArenaInfo.AvatarAddress);


            PandoraPlayer enemyPan = PandoraBoxMaster.GetPandoraPlayer(ArenaInfo.AgentAddress.ToString());
            enemyGuildPlayer = null;
            enemyGuildPlayer =
                PandoraBoxMaster.PanDatabase.GuildPlayers.Find(x => x.IsEqual(ArenaInfo.AvatarAddress.ToString()));

            if (enemyGuildPlayer is null)
                nameText.text = ArenaInfo.AvatarName.Split('<')[0];
            else
            {
                if (PandoraBoxMaster.CurrentGuildPlayer is null)
                {
                    nameText.text =
                        $"<color=#8488BC>[</color>{enemyGuildPlayer.Guild}</color><color=#8488BC>]</color> {ArenaInfo.AvatarName.Split('<')[0]}";
                }
                else
                {
                    if (enemyGuildPlayer.Guild == PandoraBoxMaster.CurrentGuildPlayer.Guild)
                        nameText.text =
                            $"<color=#8488BC>[</color><color=green>{enemyGuildPlayer.Guild}</color><color=#8488BC>]</color> {ArenaInfo.AvatarName.Split('<')[0]}";
                    else
                        nameText.text =
                            $"<color=#8488BC>[</color>{enemyGuildPlayer.Guild}<color=#8488BC>]</color> {ArenaInfo.AvatarName.Split('<')[0]}";
                }
            }

            scoreText.text = ArenaInfo.Score.ToString();
            FavTarget.SetActive(PandoraBoxMaster.ArenaFavTargets.Contains(ArenaInfo.AvatarAddress.ToString()));

            //arena banner

            NFTOwner currentNFTOwner = new NFTOwner();
            //Debug.LogError(avatarAddress);
            currentNFTOwner = PandoraBoxMaster.PanDatabase.NFTOwners.Find(x => x.AvatarAddress.ToLower() == ArenaInfo.AvatarAddress.ToString().ToLower());
            if (!(currentNFTOwner is null) && currentNFTOwner.OwnedItems.Count > 0)
            {
                if (!string.IsNullOrEmpty(currentNFTOwner.CurrentArenaBanner))
                {
                    NFTItem arenaBanner = PandoraBoxMaster.PanDatabase.NFTItems.Find(x => x.ItemID == currentNFTOwner.CurrentArenaBanner);
                    if (bannerHolder.childCount > 0)
                    {
                        PandoraArenaBanner currentBanner = bannerHolder.GetChild(0).GetComponent<PandoraArenaBanner>();
                        if (currentBanner.ItemName != arenaBanner.ItemName)
                        {
                            Destroy(bannerHolder.GetChild(0).gameObject);
                            Instantiate(Resources.Load(arenaBanner.PrefabLocation) as GameObject, bannerHolder);
                        }
                    }
                    else
                    {
                        Instantiate(Resources.Load(arenaBanner.PrefabLocation) as GameObject, bannerHolder);
                    }
                }
                else
                {
                    //clear arena slot
                    if (bannerHolder.childCount > 0)
                        Destroy(bannerHolder.GetChild(0).gameObject);
                }
            }
            else
            {
                if (bannerHolder.childCount > 0)
                    Destroy(bannerHolder.GetChild(0).gameObject);
            }

            //playerBanner.SetActive(enemyPan.ArenaBanner != 0);
            //if (enemyPan.ArenaBanner != 0)
            //{
            //    playerBanner.transform.Find("Bg").GetComponent<Image>().sprite = Banners[enemyPan.ArenaBanner];
            //    playerBanner.transform.Find("Bg/Bg_add").GetComponent<Image>().sprite = Banners[enemyPan.ArenaBanner];
            //}

            int he, me;
            me = CPHelper.GetCPV2(
                States.Instance.CurrentAvatarState, Game.Game.instance.TableSheets.CharacterSheet,
                Game.Game.instance.TableSheets.CostumeStatSheet);
            he = int.Parse(GetCP(ArenaInfo));

            if (he > me + 10000)
                cpText.text = "<color=red>" + GetCP(ArenaInfo) + "</color>";
            else if (he <= me + 10000 && he > me)
                cpText.text = "<color=#FF4900>" + GetCP(ArenaInfo) + "</color>";
            else if (he <= me && he > me - 10000)
                cpText.text = "<color=#4CA94C>" + GetCP(ArenaInfo) + "</color>";
            else
                cpText.text = "<color=green>" + GetCP(ArenaInfo) + "</color>";

            gainPointText.gameObject.SetActive(!_isCurrentUser);

            string tempTxt =
                $"(<color=green>{ArenaInfo.ArenaRecord.Win}</color>/<color=red>{ArenaInfo.ArenaRecord.Lose}</color>)";
            if (ArenaInfo.DailyChallengeCount > 0)
                tempTxt += $" - (<color=green>{ArenaInfo.DailyChallengeCount}</color>)";
            else
                tempTxt += $" - (<color=red>{ArenaInfo.DailyChallengeCount}</color>)";
            extraInfoText.text = tempTxt;

            me = currentAvatarArenaInfo is null ? 0 : currentAvatarArenaInfo.Score;
            he = ArenaInfo.Score;

            StartCoroutine(UpdateGainText(me, he));
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

            //nameText.text = ArenaInfo.AvatarName;
            //scoreText.text = ArenaInfo.Score.ToString();
            //cpText.text = GetCP(ArenaInfo);

            challengeCountTextContainer.SetActive(_isCurrentUser);
            challengeButton.gameObject.SetActive(!_isCurrentUser);

            if (_isCurrentUser)
            {
                var player = Game.Game.instance.Stage.SelectedPlayer;
                if (player is null)
                {
                    player = Game.Game.instance.Stage.GetPlayer();
                    characterView.SetByPlayer(player);
                    //|||||||||||||| PANDORA START CODE |||||||||||||||||||
                    ForceChangePortraitImage(player.avatarAddress.ToString());
                    //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
                    player.gameObject.SetActive(false);
                }
                else
                {
                    characterView.SetByPlayer(player);
                }

                challengeCountText.text =
                    $"<color=orange>{ArenaInfo.DailyChallengeCount}</color>/{GameConfig.ArenaChallengeCountMax}";
            }
            else
            {
                characterView.SetByArenaInfo(ArenaInfo);
                characterView.AvatarAddress = ArenaInfo.AvatarAddress.ToString().ToLower();
                //|||||||||||||| PANDORA START CODE |||||||||||||||||||
                //ForceChangePortraitImage(ArenaInfo.AvatarAddress.ToString());
                //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
                UpdateChallengeButton(_viewModel.currentAvatarCanBattle);
            }

            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            gainRealPointText.gameObject.SetActive(false);
            if (PandoraBoxMaster.CurrentPandoraPlayer.IsPremium())
            {
                StartCoroutine(GetEnemyState());
            }

            winRateText.gameObject.SetActive(PandoraBoxMaster.CurrentPandoraPlayer.IsPremium());
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

            characterView.Show();
        }

        //|||||||||||||| PANDORA START CODE |||||||||||||||||||

        void ForceChangePortraitImage(string address)
        {
            NFTOwner currentNFTOwner = new NFTOwner();
            currentNFTOwner = PandoraBoxMaster.PanDatabase.NFTOwners.Find(x => x.AvatarAddress.ToLower() == address.ToLower());
            if (!(currentNFTOwner is null) && currentNFTOwner.OwnedItems.Count > 0)
            {
                if (!string.IsNullOrEmpty(currentNFTOwner.CurrentPortrait))
                {
                    NFTItem portrait = PandoraBoxMaster.PanDatabase.NFTItems.Find(x => x.ItemID == currentNFTOwner.CurrentPortrait);
                    if ((portrait is null))
                    {
                        {
                            var image = Resources.Load<Sprite>(portrait.PrefabLocation);
                            characterView.transform.GetChild(0).GetComponent<Image>().overrideSprite = image;
                        }
                    }
                }
            }
        }

        IEnumerator UpdateGainText(int myScore, int hisScore)
        {
            while (true)
            {
                yield return new WaitForSeconds(0.2f);
                int sum = 0;
                for (int i = 0; i < PandoraBoxMaster.ArenaTicketsToUse; i++)
                {
                    int prevoiusPoints = 0; //prevoius accumilated points
                    for (int j = 0; j < i; j++)
                    {
                        prevoiusPoints += AccumulatedPoints(myScore + (prevoiusPoints), hisScore);
                    }

                    sum += AccumulatedPoints(myScore + (prevoiusPoints), hisScore);
                }

                gainPointText.text = "[" + sum + "]";

                //if (sum / (PandoraBoxMaster.ArenaTicketsToUse * 20) >= 1)
                //    gainPointText.text = "<color=green>+" + sum + "</color>";
                //else
                //    gainPointText.text = "<color=red>+" + sum + "</color>";
            }
        }

        int AccumulatedPoints(int me, int he)
        {
            //var  = scoreGetter(Score, enemyInfo.Score, result);
            var (challengerScore, defenderScore) =
                ArenaScoreHelper.GetScore(me, he, Nekoyume.Model.BattleStatus.BattleLog.Result.Win);
            return challengerScore;
        }
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

        private void UpdateRank(int rank)
        {
            switch (rank)
            {
                case -1:
                    rankImageContainer.SetActive(false);
                    rankTextContainer.SetActive(true);
                    rankText.text = "-";
                    break;
                case 1:
                case 2:
                case 3:
                    rankImageContainer.SetActive(true);
                    rankTextContainer.SetActive(false);
                    rankImage.overrideSprite = SpriteHelper.GetRankIcon(rank);
                    break;
                default:
                    rankImageContainer.SetActive(false);
                    rankTextContainer.SetActive(true);
                    rankText.text = rank.ToString();
                    break;
            }
        }

        private void UpdateChallengeButton(bool canBattle)
        {
            if (_viewModel != null)
            {
                _viewModel.currentAvatarCanBattle = canBattle;
            }

            if (_viewModel?.currentAvatarArenaInfo is null)
            {
                challengeButton.SetConditionalState(canBattle);
            }
            else
            {
                challengeButton.SetConditionalState(_viewModel.currentAvatarArenaInfo.DailyChallengeCount > 0 &&
                                                    canBattle);
            }
        }

        private static string GetCP(ArenaInfo arenaInfo)
        {
            if (States.Instance.CurrentAvatarState?.address == arenaInfo.AvatarAddress)
            {
                return CPHelper.GetCPV2(States.Instance.CurrentAvatarState,
                    Game.Game.instance.TableSheets.CharacterSheet,
                    Game.Game.instance.TableSheets.CostumeStatSheet).ToString();
            }

            return arenaInfo.CombatPoint.ToString();
        }
    }
}
