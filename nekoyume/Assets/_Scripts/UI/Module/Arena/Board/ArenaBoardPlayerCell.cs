using System;
using System.Globalization;
using Nekoyume.Helper;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace Nekoyume.UI.Module.Arena.Board
{
    using Nekoyume.Battle;
    using Nekoyume.BlockChain;
    using Nekoyume.Game;
    using Nekoyume.Model.Arena;
    using Nekoyume.PandoraBox;
    using Nekoyume.State;
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

        [SerializeField] private Transform bannerHolder = null;
        [SerializeField] private Image rarityMockupImage = null;
        [SerializeField] private GameObject FavTarget = null;
        public GameObject BlinkSelected = null;

        //guild info
        GuildPlayer enemyGuildPlayer;

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
                .Subscribe(_ => Context.onClickCharacterView?.Invoke(Index))
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

            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            var currentAP = Widget.Find<ArenaBoard>()._boundedData[Index];
            FavTarget.SetActive(PandoraBoxMaster.ArenaFavTargets.Contains(currentAP.AvatarAddr.ToString()));

            PandoraPlayer enemyPan = PandoraBoxMaster.GetPandoraPlayer(currentAP.AvatarAddr.ToString());
            enemyGuildPlayer = null;
            enemyGuildPlayer =
                PandoraBoxMaster.PanDatabase.GuildPlayers.Find(x => x.IsEqual(currentAP.AvatarAddr.ToString()));

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


            int he, me;
            me = CPHelper.GetCPV2(
                States.Instance.CurrentAvatarState, Game.instance.TableSheets.CharacterSheet,
                Game.instance.TableSheets.CostumeStatSheet);
            he = _currentData.cp;

            Color selectedColor = new Color();
            
            if (he > me + 10000)
                ColorUtility.TryParseHtmlString("#FF0000", out selectedColor);
            else if (he <= me + 10000 && he > me)
                ColorUtility.TryParseHtmlString("#FF4900", out selectedColor);
            else if (he <= me && he > me - 10000)
                ColorUtility.TryParseHtmlString("#4CA94C", out selectedColor);
            else
                ColorUtility.TryParseHtmlString("#00FF00", out selectedColor);
            _cpText.color = selectedColor;

            var player = RxProps.PlayersArenaParticipant.Value;
            cannotAttackImg.SetActive(_currentData.score > player.Score + 100 || player.Score > _currentData.score + 100);

            //arena banner

            NFTOwner currentNFTOwner = new NFTOwner();
            //Debug.LogError(avatarAddress);
            currentNFTOwner = PandoraBoxMaster.PanDatabase.NFTOwners.Find(x => x.AvatarAddress.ToLower() == currentAP.AvatarAddr.ToString().ToLower());
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


            GetExtraData();



            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

            _ratingText.text =
                _currentData.score.ToString("N0", CultureInfo.CurrentCulture);
            _plusRatingText.text =
                _currentData.expectWinDeltaScore.ToString(
                    "N0",
                    CultureInfo.CurrentCulture);
            _choiceButton.Interactable = _currentData.interactableChoiceButton;
            UpdateRank();
        }

        async void GetExtraData()
        {
            var currentAP = Widget.Find<ArenaBoard>()._boundedData[Index];

            var arenaSheet = TableSheets.Instance.ArenaSheet;
            var currentBlockIndex = Game.instance.Agent.BlockIndex;
            if (arenaSheet.TryGetCurrentRound(currentBlockIndex, out var currentRoundData))
            {
                // currentRoundData.ChampionshipId
                // currentRoundData.Round
            }

            var arenaInformationAddr = ArenaInformation.DeriveAddress(currentAP.AvatarAddr, currentRoundData.ChampionshipId, currentRoundData.Round);
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
            //winLoseText.text = arenaInformationAddr.ToString();
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



        public void ShowGuildInfo()
        {
            if (enemyGuildPlayer is null)
                return;
            Widget.Find<GuildInfo>().Show(enemyGuildPlayer.Guild);
        }
    }
}
