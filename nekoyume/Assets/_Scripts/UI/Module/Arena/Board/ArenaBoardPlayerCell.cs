using System;
using System.Globalization;
using Nekoyume.Helper;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace Nekoyume.UI.Module.Arena.Board
{
    using Nekoyume.Arena;
    using Nekoyume.Battle;
    using Nekoyume.BlockChain;
    using Nekoyume.Game;
    using Nekoyume.Model.Arena;
    using Nekoyume.Model.Mail;
    using Nekoyume.Model.State;
    using Nekoyume.PandoraBox;
    using Nekoyume.State;
    using Nekoyume.UI.Scroller;
    using System.Collections;
    using UniRx;
    using static Nekoyume.State.RxProps;

    [Serializable]
    public class ArenaBoardPlayerItemData
    {
        public string name;
        public int level;
        public int fullCostumeOrArmorId;
        public int? titleId;
        public int cp;
        public int score;
        public int rank;
        public int expectWinDeltaScore;
        public bool interactableChoiceButton;
    }

    public class ArenaBoardPlayerScrollContext : FancyScrollRectContext
    {
        public int selectedIndex = -1;
        public Action<int> onClickCharacterView;
        public Action<int> onClickChoice;
    }

    public class ArenaBoardPlayerCell
        : FancyScrollRectCell<ArenaBoardPlayerItemData, ArenaBoardPlayerScrollContext>
    {
        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        [Header("PANDORA CUSTOM FIELDS")]
        [SerializeField] private GameObject cannotAttackImg = null;
        [SerializeField] private TextMeshProUGUI extraInfoText = null;
        [SerializeField] private TextMeshProUGUI rateText = null;

        [SerializeField] private Transform bannerHolder = null;
        [SerializeField] private Image rarityMockupImage = null;
        [SerializeField] private GameObject FavTarget = null;
        [SerializeField] private GameObject bannedObj = null;
        public GameObject BlinkSelected = null;

        //arena
        ArenaParticipant meAP = null;
        ArenaParticipant selectedAP = null;

        //guild info
        PandoraPlayer selectedPan = null;
        GuildPlayer enemyGuildPlayer = null;

        [Space(50)]
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

        [SerializeField]
        private Image _rankImage;

        [SerializeField]
        private GameObject _rankImageContainer;

        [SerializeField]
        private TextMeshProUGUI _rankText;

        [SerializeField]
        private GameObject _rankTextContainer;

        [SerializeField]
        private DetailedCharacterView _characterView;

        [SerializeField]
        private TextMeshProUGUI _nameText;

        [SerializeField]
        private TextMeshProUGUI _ratingText;

        [SerializeField]
        private TextMeshProUGUI _cpText;

        [SerializeField]
        private TextMeshProUGUI _plusRatingText;

        [SerializeField]
        private ConditionalButton _choiceButton;

        private ArenaBoardPlayerItemData _currentData;

#if UNITY_EDITOR
        [ReadOnly]
        public float _normalizedPosition;
#else
        private float _normalizedPosition;
#endif

        private void Awake()
        {
            _characterView.OnClickCharacterIcon
                .Subscribe(_ =>
                {
                    Context.onClickCharacterView?.Invoke(Index);
                    foreach (Transform item in transform.parent)
                    {
                        item.GetComponent<ArenaBoardPlayerCell>().BlinkSelected.SetActive(false);
                    }
                    BlinkSelected.SetActive(true);
                })
                .AddTo(gameObject);
            
            _choiceButton.OnClickSubject
                .Subscribe(_ => Context.onClickChoice?.Invoke(Index))
                .AddTo(gameObject);
        }

        public override void UpdateContent(ArenaBoardPlayerItemData itemData)
        {
            _currentData = itemData;
            _characterView.SetByFullCostumeOrArmorId(
                _currentData.fullCostumeOrArmorId,
                _currentData.titleId,
                _currentData.level.ToString("N0", CultureInfo.CurrentCulture));
            _nameText.text = _currentData.name;
            _cpText.text =
                _currentData.cp.ToString("N0", CultureInfo.CurrentCulture);



            _ratingText.text =
                _currentData.score.ToString("N0", CultureInfo.CurrentCulture);
            _plusRatingText.text =
                _currentData.expectWinDeltaScore.ToString(
                    "N0",
                    CultureInfo.CurrentCulture);
            _choiceButton.Interactable = _currentData.interactableChoiceButton;
            UpdateRank();
            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            selectedAP = Widget.Find<ArenaBoard>()._boundedData[Index];
            meAP = PlayersArenaParticipant.Value;
            selectedPan = PandoraBoxMaster.GetPandoraPlayer(selectedAP.AvatarAddr.ToString());
            enemyGuildPlayer = PandoraBoxMaster.PanDatabase.GuildPlayers.Find(x => x.IsEqual(selectedAP.AvatarAddr.ToString()));

            if (Widget.Find<FriendInfoPopupPandora>().enemyAP is null)
                BlinkSelected.SetActive(false);
            else
                BlinkSelected.SetActive(selectedAP.AvatarAddr ==
                                        Widget.Find<FriendInfoPopupPandora>().enemyAP.AvatarAddr);
            FavTarget.SetActive(PandoraBoxMaster.ArenaFavTargets.Contains(selectedAP.AvatarAddr.ToString()));
            bannedObj.SetActive(selectedPan.IsBanned);

            if (bannerHolder.childCount > 0)
                foreach (Transform item in bannerHolder)
                    Destroy(item.gameObject);

            SetName();
            SetCP();
            SetBanner();
            SetArenaInfo();

            if (PandoraBoxMaster.CurrentPandoraPlayer.IsPremium())
            {
                try
                {
                    WinRate();
                }
                catch
                {
                    //since they do quick navigate, it will cause error, so this to remove the error
                }
            }

            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
         }



        protected override void UpdatePosition(float normalizedPosition, float localPosition)
        {
            _normalizedPosition = normalizedPosition;
            base.UpdatePosition(_normalizedPosition, localPosition);
        }

        private void UpdateRank()
        {
            switch (_currentData.rank)
            {
                case -1:
                    _rankImageContainer.SetActive(false);
                    _rankText.text = "-";
                    _rankTextContainer.SetActive(true);
                    break;
                case 1:
                case 2:
                case 3:
                    _rankImage.overrideSprite = SpriteHelper.GetRankIcon(_currentData.rank);
                    _rankImageContainer.SetActive(true);
                    _rankTextContainer.SetActive(false);
                    break;
                default:
                    _rankImageContainer.SetActive(false);
                    _rankText.text =
                        _currentData.rank.ToString("N0", CultureInfo.CurrentCulture);
                    _rankTextContainer.SetActive(true);
                    break;
            }
        }

        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        public void GetGuildInfo()
        {
            if (enemyGuildPlayer is null)
                return;
            Widget.Find<GuildInfo>().Show(enemyGuildPlayer.Guild);
        }

        void SetName()
        {
            if (enemyGuildPlayer is null)
                _nameText.text = _currentData.name.Split('<')[0];
            else
            {
                if (PandoraBoxMaster.CurrentGuildPlayer is null)
                {
                    _nameText.text =
                        $"<color=#8488BC>[</color>{enemyGuildPlayer.Guild}</color><color=#8488BC>]</color> {_currentData.name.Split('<')[0]}";
                }
                else
                {
                    if (enemyGuildPlayer.Guild == PandoraBoxMaster.CurrentGuildPlayer.Guild)
                        _nameText.text =
                            $"<color=#8488BC>[</color><color=green>{enemyGuildPlayer.Guild}</color><color=#8488BC>]</color> {_currentData.name.Split('<')[0]}";
                    else
                        _nameText.text =
                            $"<color=#8488BC>[</color>{enemyGuildPlayer.Guild}<color=#8488BC>]</color> {_currentData.name.Split('<')[0]}";
                }
            }
        }

        void SetCP()
        {
            int he, me;
            me = CPHelper.GetCPV2(
                meAP.AvatarState, Game.instance.TableSheets.CharacterSheet,
                Game.instance.TableSheets.CostumeStatSheet);
            he = _currentData.cp;

            Color selectedColor = new Color();

            if (he > me + 20000)
                ColorUtility.TryParseHtmlString("#FF0000", out selectedColor);
            else if (he <= me + 20000 && he > me)
                ColorUtility.TryParseHtmlString("#FF4900", out selectedColor);
            else if (he <= me && he > me - 20000)
                ColorUtility.TryParseHtmlString("#4CA94C", out selectedColor);
            else
                ColorUtility.TryParseHtmlString("#00FF00", out selectedColor);
            _cpText.color = selectedColor;



            var arenaSheet = TableSheets.Instance.ArenaSheet;
            var currentBlockIndex = Game.instance.Agent.BlockIndex;
            if (arenaSheet.TryGetCurrentRound(currentBlockIndex, out var currentRoundData))
            {
                //cannotAttackImg.SetActive(false); //false on offseason
                //if (currentRoundData.ArenaType == Nekoyume.Model.EnumType.ArenaType.Championship || currentRoundData.ArenaType == Nekoyume.Model.EnumType.ArenaType.Season)
                cannotAttackImg.SetActive(_currentData.score > meAP.Score + 100 || meAP.Score > _currentData.score + 100);
            }
        }

        void SetBanner()
        {
            NFTOwner currentNFTOwner = new NFTOwner();
            //Debug.LogError(avatarAddress);
            currentNFTOwner = PandoraBoxMaster.PanDatabase.NFTOwners.Find(x => x.AvatarAddress.ToLower() == selectedAP.AvatarAddr.ToString().ToLower());
            if (!(currentNFTOwner is null) && currentNFTOwner.OwnedItems.Count > 0)
            {
                if (currentNFTOwner.CurrentArenaBanner != "")
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
        }

        async void SetArenaInfo()
        {
            var arenaSheet = TableSheets.Instance.ArenaSheet;
            var currentBlockIndex = Game.instance.Agent.BlockIndex;
            if (arenaSheet.TryGetCurrentRound(currentBlockIndex, out var currentRoundData))
            {
                var arenaInformationAddr = ArenaInformation.DeriveAddress(selectedAP.AvatarAddr, currentRoundData.ChampionshipId, currentRoundData.Round);
                var agent = Game.instance.Agent;

                var sa = await agent.GetStateAsync(arenaInformationAddr);
                if (sa is Bencodex.Types.List serializedArenaInformationList &&
                    serializedArenaInformationList.Count >= 3)
                {
                    var win = (int)(Bencodex.Types.Integer)serializedArenaInformationList[1];
                    var lose = (int)(Bencodex.Types.Integer)serializedArenaInformationList[2];
                    var a1 = (int)(Bencodex.Types.Integer)serializedArenaInformationList[3];
                    var a2 = (int)(Bencodex.Types.Integer)serializedArenaInformationList[4];
                    var a3 = (int)(Bencodex.Types.Integer)serializedArenaInformationList[5];
                    extraInfoText.text = $"W.L: <color=green>{win}</color>/<color=red>{lose}</color>\nLeft: {a1}\nBought: {a3}";
                }
            }
        }

        async void WinRate()
        {
            var myArenaAvatarStateAddr = ArenaAvatarState.DeriveAddress(meAP.AvatarAddr);
            var myArenaAvatarState = await Game.instance.Agent.GetStateAsync(myArenaAvatarStateAddr) is Bencodex.Types.List serialized
                ? new ArenaAvatarState(serialized)
                : null;

            var enArenaAvatarStateAddr = ArenaAvatarState.DeriveAddress(selectedAP.AvatarAddr);
            var enArenaAvatarState = await Game.instance.Agent.GetStateAsync(enArenaAvatarStateAddr) is Bencodex.Types.List enSerialized
                ? new ArenaAvatarState(enSerialized)
                : null;


            var tableSheets = Game.instance.TableSheets;
            ArenaPlayerDigest myDigest = new ArenaPlayerDigest(meAP.AvatarState, myArenaAvatarState);
            ArenaPlayerDigest enemyDigest = new ArenaPlayerDigest(selectedAP.AvatarState, enArenaAvatarState);

            IEMultipleSimulate(myDigest, enemyDigest);
        }


        void IEMultipleSimulate(ArenaPlayerDigest mD, ArenaPlayerDigest eD)
        {
            rateText.text = "...";

            int totalSimulations = 10;
            int win = 0;


            var tableSheets = Game.instance.TableSheets;

            for (int i = 0; i < totalSimulations; i++)
            {
                var simulator = new ArenaSimulator(new Cheat.DebugRandom());
                var log = simulator.Simulate(
                    mD,
                    eD,
                    tableSheets.GetArenaSimulatorSheets());

                if (log.Result == Nekoyume.Model.BattleStatus.Arena.ArenaLog.ArenaResult.Win)
                    win++;
                //yield return new WaitForSeconds(0.05f);
            }

            float finalRatio = (float)win / (float)totalSimulations;
            float FinalValue = (int)(finalRatio * 100f);

            if (finalRatio <= 0.5f)
                rateText.text = $"<color=red>{FinalValue}</color>%";
            else if (finalRatio > 0.5f && finalRatio <= 0.75f)
                rateText.text = $"<color=#FF4900>{FinalValue}</color>%";
            else
                rateText.text = $"<color=green>{FinalValue}</color>%";

        }

        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||


    }
}
