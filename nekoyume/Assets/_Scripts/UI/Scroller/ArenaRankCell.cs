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
    using PandoraBox;
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
        private SubmitButton challengeButton = null;

        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        [SerializeField]
        private Sprite[] Banners = null;

        [SerializeField]
        private SubmitButton maxChallengeButton = null;

        [SerializeField]
        private GameObject playerBanner = null;

        [SerializeField]
        private TextMeshProUGUI gainPointText = null;

        [SerializeField]
        private TextMeshProUGUI extraInfoText = null;

        [SerializeField]
        private GameObject FavTarget = null;
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

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

            challengeButton.OnSubmitClick
                .ThrottleFirst(new TimeSpan(0, 0, 1))
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    Context.OnClickChallenge.OnNext(this);
                    _onClickChallenge.OnNext(this);
                })
                .AddTo(gameObject);

            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            for (int i = 0; i < 5; i++)
            //for (int i = 0; i < ArenaInfo.DailyChallengeCount; i++)
            {
                maxChallengeButton.OnSubmitClick
               .Subscribe(_ =>
               {
                   AudioController.PlayClick();
                   Context.OnClickChallenge.OnNext(this);
                   _onClickChallenge.OnNext(this);
               })
               .AddTo(gameObject);
            }
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

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


            maxChallengeButton.gameObject.SetActive(!_isCurrentUser);
            gainPointText.gameObject.SetActive(!_isCurrentUser);

            string tempTxt = $"(<color=green>{ArenaInfo.ArenaRecord.Win}</color>/<color=red>{ArenaInfo.ArenaRecord.Lose}</color>)";
            if (ArenaInfo.DailyChallengeCount > 0)
                tempTxt += $" - (<color=green>{ArenaInfo.DailyChallengeCount}</color>)";
            else
                tempTxt += $" - (<color=red>{ArenaInfo.DailyChallengeCount}</color>)";
            extraInfoText.text = tempTxt;

            me = currentAvatarArenaInfo is null? 0:currentAvatarArenaInfo.Score;
            he = ArenaInfo.Score;

            if (he > me + 500)
            {
                gainPointText.text = "<color=green>+60</color>";
            }
            else if (he > me + 400 && he <= me + 500)
            {
                gainPointText.text = "<color=green>+50</color>";
            }
            else if (he > me + 300 && he <= me + 400)
            {
                gainPointText.text = "<color=#FFA200>+40</color>";
            }
            else if (he > me + 200 && he <= me + 300)
            {
                gainPointText.text = "<color=#FFA200>+30</color>";
            }
            else if (he > me + 100 && he <= me + 200)
            {
                gainPointText.text = "<color=#FFA200>+25</color>";
            }
            else if (he > me && he <= me + 100)
            {
                gainPointText.text = "<color=#FFA200>+20</color>";
            }
            else if (he <= me && he > me - 100)
            {
                gainPointText.text = "<color=red>+15</color>";
            }
            else if (he <= me - 100 && he > me - 200)
            {
                gainPointText.text = "<color=red>+15</color>";
            }
            else if (he <= me - 200 && he > me - 300)
            {
                gainPointText.text = "<color=red>+8</color>";
            }
            else if (he <= me - 300 && he > me - 400)
            {
                gainPointText.text = "<color=red>+4</color>";
            }
            else if (he <= me - 400 && he > me - 500)
            {
                gainPointText.text = "<color=red>+2</color>";
            }
            else if (he <= me - 500)
            {
                gainPointText.text = "<color=#FFA200>+1</color>";
            }

            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

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
                    challengeButton.SetSubmittable(true);
                    //|||||||||||||| PANDORA START CODE |||||||||||||||||||
                    maxChallengeButton.SetSubmittable(true);
                    //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
                }
                else
                {
                    challengeButton.SetSubmittable(itemData.currentAvatarArenaInfo.DailyChallengeCount > 0);
                    //|||||||||||||| PANDORA START CODE |||||||||||||||||||
                    maxChallengeButton.SetSubmittable(itemData.currentAvatarArenaInfo.DailyChallengeCount > 0);
                    //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
                }
            }
            //Debug.LogError(ArenaInfo.AgentAddress);
            if (panPlayer.ArenaIcon == 1)
                characterView.SetIcon(PandoraBoxMaster.Instance.CosmicIcon);
            characterView.Show();
        }

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
