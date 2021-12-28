using System;
using Nekoyume.Battle;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Scroller
{
    using Nekoyume.Model.Mail;
    using PandoraBox;
    using System.Collections;
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
        }

        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        [Header("PANDORA CUSTOM FIELDS")]
        [SerializeField] private Sprite[] Banners = null;
        [SerializeField] private GameObject playerBanner = null;
        [SerializeField] private TextMeshProUGUI gainPointText = null;
        [SerializeField] private TextMeshProUGUI extraInfoText = null;
        [SerializeField] private GameObject FavTarget = null;
        [Space(50)]
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

        [SerializeField]
        private Image backgroundImage = null;

        [SerializeField]
        private bool controlBackgroundImage = false;

        [SerializeField]
        private GameObject rankImageContainer = null;

        [SerializeField]
        private Image rankImage = null;

        [SerializeField]
        private GameObject rankTextContainer = null;

        [SerializeField]
        private TextMeshProUGUI rankText = null;

        [SerializeField]
        private DetailedCharacterView characterView = null;

        [SerializeField]
        private TextMeshProUGUI nameText = null;

        [SerializeField]
        private TextMeshProUGUI scoreText = null;

        [SerializeField]
        private TextMeshProUGUI cpText = null;

        [SerializeField]
        private GameObject challengeCountTextContainer = null;

        [SerializeField]
        private TextMeshProUGUI challengeCountText = null;

        [SerializeField]
        private Button avatarInfoButton = null;

        [SerializeField]
        private ConditionalButton challengeButton = null;

        private RectTransform _rectTransformCache;
        private bool _isCurrentUser;
        private readonly Subject<ArenaRankCell> _onClickAvatarInfo = new Subject<ArenaRankCell>();
        private readonly Subject<ArenaRankCell> _onClickChallenge = new Subject<ArenaRankCell>();

        public RectTransform RectTransform => _rectTransformCache
            ? _rectTransformCache
            : _rectTransformCache = GetComponent<RectTransform>();

        public ArenaInfo ArenaInfo { get; private set; }

        public IObservable<ArenaRankCell> OnClickAvatarInfo => _onClickAvatarInfo;

        public IObservable<ArenaRankCell> OnClickChallenge => _onClickChallenge;

        private void Awake()
        {
            characterView.OnClickCharacterIcon
                .Subscribe(async avatarState =>
                {
                    if (avatarState is null)
                    {
                        var (exist, state) = await States.TryGetAvatarStateAsync(ArenaInfo.AvatarAddress);
                        avatarState = exist ? state : null;
                        if (avatarState is null)
                        {
                            return;
                        }
                    }
                    Widget.Find<FriendInfoPopup>().Show(avatarState);
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
                    ChallangeRemainingTickets();
                })
                .AddTo(gameObject);

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
            PandoraBoxMaster.CurrentPanPlayer = PandoraBoxMaster.GetPanPlayer(States.Instance.CurrentAvatarState.agentAddress.ToString());
            //Debug.LogError(PandoraBoxMaster.CurrentPanPlayer.PremiumEndBlock + "  -  " + Game.Game.instance.Agent.BlockIndex);
            if (PandoraBoxMaster.CurrentPanPlayer.PremiumEndBlock > Game.Game.instance.Agent.BlockIndex)
            {
                StartCoroutine(StartFightCount());
            }
            else
            {
                if (PandoraBoxMaster.ArenaTicketsToUse > 1)
                {
                    OneLineSystem.Push(MailType.System, "<color=green>Pandora Box</color>: This is Premium Feature! Please select 1 Fight", NotificationCell.NotificationType.Alert);
                }
                else
                {
                    if (PandoraBoxMaster.ArenaTicketsToUse == 1)
                    {
                        Context.OnClickChallenge.OnNext(this);
                        _onClickChallenge.OnNext(this);
                    }
                }
            }


        }

        IEnumerator StartFightCount()
        {
            var currentAddress = States.Instance.CurrentAvatarState?.address;
            var arenaInfo = States.Instance.WeeklyArenaState.GetArenaInfo(currentAddress.Value);

            //Debug.LogError("Fights Count: " + PandoraBoxMaster.ArenaTicketsToUse);
            //for (int i = 0; i < arenaInfo.DailyChallengeCount; i++)


            for (int i = 0; i < Mathf.Clamp(PandoraBoxMaster.ArenaTicketsToUse, 0, arenaInfo.DailyChallengeCount); i++)
            {
                Context.OnClickChallenge.OnNext(this);
                _onClickChallenge.OnNext(this);
                OneLineSystem.Push(MailType.System, "<color=green>Pandora Box</color>: Fight Arena <color=green>" + (i + 1)
                    + "</color>/" + Mathf.Clamp(PandoraBoxMaster.ArenaTicketsToUse, 0, arenaInfo.DailyChallengeCount) + "!", NotificationCell.NotificationType.Information);
                yield return new WaitForSeconds(3);
            }
        }
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

        public void Show((
            int rank,
            ArenaInfo arenaInfo,
            ArenaInfo currentAvatarArenaInfo) itemData)
        {
            Show(new ViewModel
            {
                rank = itemData.rank,
                arenaInfo = itemData.arenaInfo,
                currentAvatarArenaInfo = itemData.currentAvatarArenaInfo
            });
        }

        public void ShowMyDefaultInfo()
        {
            UpdateRank(-1);

            var currentAvatarState = States.Instance.CurrentAvatarState;
            characterView.SetByAvatarState(currentAvatarState);
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
            if (itemData is null)
            {
                Debug.LogError($"Argument is null. {nameof(itemData)}");
                return;
            }

            ArenaInfo = itemData.arenaInfo ?? throw new ArgumentNullException(nameof(itemData.arenaInfo));
            var currentAvatarArenaInfo = itemData.currentAvatarArenaInfo;
            _isCurrentUser = currentAvatarArenaInfo is null ?
                false : ArenaInfo.AvatarAddress == currentAvatarArenaInfo.AvatarAddress;

            if (controlBackgroundImage)
            {
                backgroundImage.enabled = Index % 2 == 1;
            }

            UpdateRank(itemData.rank);

            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            nameText.text = ArenaInfo.AvatarName;
            scoreText.text = ArenaInfo.Score.ToString();
            FavTarget.SetActive(PandoraBoxMaster.ArenaFavTargets.Contains(ArenaInfo.AvatarAddress.ToString()));


            PanPlayer panPlayer = PandoraBoxMaster.GetPanPlayer(ArenaInfo.AgentAddress.ToString());
            playerBanner.SetActive(panPlayer.ArenaBanner != 0);
            if (panPlayer.ArenaBanner != 0)
            {
                playerBanner.transform.Find("Bg").GetComponent<Image>().sprite = Banners[panPlayer.ArenaBanner];
                playerBanner.transform.Find("Bg/Bg_add").GetComponent<Image>().sprite = Banners[panPlayer.ArenaBanner];
            }

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

            string tempTxt = $"(<color=green>{ArenaInfo.ArenaRecord.Win}</color>/<color=red>{ArenaInfo.ArenaRecord.Lose}</color>)";
            if (ArenaInfo.DailyChallengeCount > 0)
                tempTxt += $" - (<color=green>{ArenaInfo.DailyChallengeCount}</color>)";
            else
                tempTxt += $" - (<color=red>{ArenaInfo.DailyChallengeCount}</color>)";
            extraInfoText.text = tempTxt;

            me = currentAvatarArenaInfo is null ? 0 : currentAvatarArenaInfo.Score;
            he = ArenaInfo.Score;

            StartCoroutine(UpdateGainText(me, he));
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||


            nameText.text = ArenaInfo.AvatarName;
            scoreText.text = ArenaInfo.Score.ToString();
            cpText.text = GetCP(ArenaInfo);

            challengeCountTextContainer.SetActive(_isCurrentUser);
            challengeButton.gameObject.SetActive(!_isCurrentUser);

            if (_isCurrentUser)
            {
                var player = Game.Game.instance.Stage.selectedPlayer;
                if (player is null)
                {
                    player = Game.Game.instance.Stage.GetPlayer();
                    characterView.SetByPlayer(player);
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

                if (itemData.currentAvatarArenaInfo is null)
                {
                    challengeButton.SetConditionalState(true);
                }
                else
                {
                    challengeButton.SetConditionalState(itemData.currentAvatarArenaInfo.DailyChallengeCount > 0);
                }
            }

            characterView.Show();
        }

        //|||||||||||||| PANDORA START CODE |||||||||||||||||||

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
                if (sum / (PandoraBoxMaster.ArenaTicketsToUse * 20) >= 1)
                    gainPointText.text = "<color=green>+" + sum + "</color>";
                else
                    gainPointText.text = "<color=red>+" + sum + "</color>";
            }
        }

        int AccumulatedPoints(int me, int he)
        {
            if (he > me + 500)
            {
                return 60;
            }
            else if (he > me + 400 && he <= me + 500)
            {
                return 45;
            }
            else if (he > me + 300 && he <= me + 400)
            {
                return 35;
            }
            else if (he > me + 200 && he <= me + 300)
            {
                return 25;
            }
            else if (he > me + 100 && he <= me + 200)
            {
                return 22;
            }
            else if (he > me && he <= me + 100)
            {
                return 20;
            }
            else if (he <= me && he > me - 100)
            {
                return 15;
            }
            else if (he <= me - 100 && he > me - 200)
            {
                return 10;
            }
            else if (he <= me - 200 && he > me - 300)
            {
                return 8;
            }
            else if (he <= me - 300 && he > me - 400)
            {
                return 4;
            }
            else if (he <= me - 400 && he > me - 500)
            {
                return 2;
            }
            else if (he <= me - 500)
            {
                return 1;
            }
            else
                return 0;
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
