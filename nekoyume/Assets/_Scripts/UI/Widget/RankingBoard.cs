using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Libplanet;
using Libplanet.Blocks;
using Nekoyume.Game.Controller;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using Nekoyume.Model.BattleStatus;
    using PandoraBox;
    using UniRx;

    public class RankingBoard : Widget
    {
        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        [Header("PANDORA CUSTOM FIELDS")]
        [SerializeField] private TextMeshProUGUI FightCountTxt = null;
        [SerializeField] private Button RefreshButton = null;
        [SerializeField] private Slider FightCountSldr = null;
        public Transform CellsListContainer = null;
        public GameObject waitingForLaodBlocker;
        public GameObject LoadingImage;
        [HideInInspector] public Dictionary<Address, AvatarState> avatarStatesPandora;
        [Space(50)]
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private ArenaRankScroll arenaRankScroll = null;

        [SerializeField]
        private ArenaRankCell currentAvatarCellView = null;

        [SerializeField]
        private SpeechBubble speechBubble = null;

        private Nekoyume.Model.State.RankingInfo[] _avatarRankingStates;

        private List<(int rank, ArenaInfo arenaInfo)> _weeklyCachedInfo =
            new List<(int rank, ArenaInfo arenaInfo)>();

        private BlockHash? _cachedBlockHash;

        private readonly List<IDisposable> _disposablesFromShow = new List<IDisposable>();

        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        public void ChangeSliderArenaCount()
        {
            FightCountTxt.text = FightCountSldr.value.ToString();
            PandoraBoxMaster.ArenaTicketsToUse = int.Parse(FightCountTxt.text);
        }

        public async void RefreshBoard()
        {
            waitingForLaodBlocker.SetActive(true);
            RefreshButton.interactable = false;
            RefreshButton.GetComponentInChildren<TextMeshProUGUI>().text = "...";

            //clear fav list, not optimal
            PandoraBoxMaster.ArenaFavTargets.Clear();
            for (int i = 0; i < 10; i++) //fav max count
            {
                string key = "_PandoraBox_PVP_FavTarget0" + i + "_" + States.Instance.CurrentAvatarState.address;
                if (PlayerPrefs.HasKey(key))
                    PandoraBoxMaster.ArenaFavTargets.Add(PlayerPrefs.GetString(key));
            }

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

            UpdateArena();
        }

        System.Collections.IEnumerator RefreshCooldown()
        {
            RefreshButton.interactable = false;
            int cooldown = 20;
            if (PandoraBoxMaster.CurrentPandoraPlayer.IsPremium())
                cooldown = 5;
            TextMeshProUGUI buttonText = RefreshButton.GetComponentInChildren<TextMeshProUGUI>();

            for (int i = 0; i < cooldown; i++)
            {
                buttonText.text = (cooldown - i).ToString();
                yield return new WaitForSeconds(1);
            }
            buttonText.text = "";
            RefreshButton.interactable = true;
        }
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

        protected override void Awake()
        {
            base.Awake();

            arenaRankScroll.OnClickAvatarInfo
                .Subscribe(cell => OnClickAvatarInfo(
                    cell.RectTransform,
                    cell.ArenaInfo.AvatarAddress))
                .AddTo(gameObject);
            arenaRankScroll.OnClickChallenge.Subscribe(OnClickChallenge).AddTo(gameObject);
            currentAvatarCellView.OnClickAvatarInfo
                .Subscribe(cell => OnClickAvatarInfo(
                    cell.RectTransform,
                    cell.ArenaInfo.AvatarAddress))
                .AddTo(gameObject);

            closeButton.onClick.AddListener(() =>
            {
                Close(true);
                Find<FriendInfoPopupPandora>().Close(true);
                Game.Event.OnRoomEnter.Invoke(true);
            });

            CloseWidget = () =>
            {
                Close(true);
                Find<FriendInfoPopupPandora>().Close(true);
                Game.Event.OnRoomEnter.Invoke(true);
            };
            SubmitWidget = null;
        }

        public void Show(WeeklyArenaState weeklyArenaState = null) => ShowAsync(weeklyArenaState);

        private async void ShowAsync(WeeklyArenaState weeklyArenaState = null)
        {
            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            LoadingImage.SetActive(false);
            waitingForLaodBlocker.SetActive(false);
            PandoraBoxMaster.ArenaFavTargets.Clear();
            for (int i = 0; i < 10; i++) //fav max count
            {
                string key = "_PandoraBox_PVP_FavTarget0" + i + "_" + States.Instance.CurrentAvatarState.address;
                if (PlayerPrefs.HasKey(key))
                    PandoraBoxMaster.ArenaFavTargets.Add(PlayerPrefs.GetString(key));
            }
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

            Find<DataLoadingScreen>().Show();

            var stage = Game.Game.instance.Stage;
            stage.LoadBackground("ranking");
            stage.GetPlayer().gameObject.SetActive(false);

            if (weeklyArenaState is null)
            {
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
            }
            else
            {
                await UpdateWeeklyCache(weeklyArenaState);
            }

            base.Show(true);

            Find<DataLoadingScreen>().Close();
            AudioController.instance.PlayMusic(AudioController.MusicCode.Ranking);
            HelpTooltip.HelpMe(100015, true);
            speechBubble.SetKey("SPEECH_RANKING_BOARD_GREETING_");
            StartCoroutine(speechBubble.CoShowText());

            Find<HeaderMenuStatic>().Show(HeaderMenuStatic.AssetVisibleState.Battle);
            UpdateArena();

            StartCoroutine(RefreshCooldown());
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            Widget.Find<FriendInfoPopup>().Close(true);
            Widget.Find<FriendInfoPopupPandora>().Close(true);
            _disposablesFromShow.DisposeAllAndClear();
            base.Close(ignoreCloseAnimation);
            speechBubble.Hide();
        }

        private void UpdateArena()
        {
            var weeklyArenaState = States.Instance.WeeklyArenaState;
            if (weeklyArenaState is null)
            {
                return;
            }

            var avatarAddress = States.Instance.CurrentAvatarState?.address;
            if (!avatarAddress.HasValue)
            {
                return;
            }

            if (!_weeklyCachedInfo.Any())
            {
                currentAvatarCellView.ShowMyDefaultInfo();

                UpdateBoard();
                return;
            }

            var arenaInfo = _weeklyCachedInfo[0].arenaInfo;
            if (!arenaInfo.Active)
            {
                currentAvatarCellView.ShowMyDefaultInfo();
                arenaInfo.Activate();
            }

            UpdateBoard();
        }

        private void UpdateBoard()
        {
            var weeklyArenaState = States.Instance.WeeklyArenaState;
            if (weeklyArenaState is null)
            {
                arenaRankScroll.ClearData();
                arenaRankScroll.Show();
                return;
            }

            var currentAvatarAddress = States.Instance.CurrentAvatarState?.address;
            if (!currentAvatarAddress.HasValue ||
                !weeklyArenaState.ContainsKey(currentAvatarAddress.Value))
            {
                currentAvatarCellView.ShowMyDefaultInfo();

                arenaRankScroll.Show(_weeklyCachedInfo
                    .Select(tuple => new ArenaRankCell.ViewModel
                    {
                        rank = tuple.rank,
                        arenaInfo = tuple.arenaInfo,
                    }).ToList(), true);
                // NOTE: If you want to test many arena cells, use below instead of above.
                // arenaRankScroll.Show(Enumerable
                //     .Range(1, 1000)
                //     .Select(rank => new ArenaRankCell.ViewModel
                //     {
                //         rank = rank,
                //         arenaInfo = new ArenaInfo(
                //             States.Instance.CurrentAvatarState,
                //             Game.Game.instance.TableSheets.CharacterSheet,
                //             true)
                //         {
                //             ArmorId = States.Instance.CurrentAvatarState.GetPortraitId()
                //         },
                //         currentAvatarArenaInfo = null
                //     }).ToList(), true);

                return;
            }

            var (currentAvatarRank, currentAvatarArenaInfo) = _weeklyCachedInfo
                .FirstOrDefault(info =>
                    info.arenaInfo.AvatarAddress.Equals(currentAvatarAddress));
            if (currentAvatarArenaInfo is null)
            {
                currentAvatarRank = -1;
                currentAvatarArenaInfo = new ArenaInfo(
                    States.Instance.CurrentAvatarState,
                    Game.Game.instance.TableSheets.CharacterSheet,
                    false);
            }

            currentAvatarCellView.Show((
                currentAvatarRank,
                currentAvatarArenaInfo,
                currentAvatarArenaInfo));

            arenaRankScroll.Show(_weeklyCachedInfo
                .Select(tuple => new ArenaRankCell.ViewModel
                {
                    rank = tuple.rank,
                    arenaInfo = tuple.arenaInfo,
                    currentAvatarArenaInfo = currentAvatarArenaInfo,
                }).ToList(), true);

            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            waitingForLaodBlocker.SetActive(false);
            StartCoroutine(RefreshCooldown());
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
        }

        private static void OnClickAvatarInfo(RectTransform rectTransform, Address address)
        {
            // NOTE: 블록 익스플로러 연결 코드. 이후에 참고하기 위해 남겨 둡니다.
            // Application.OpenURL(string.Format(GameConfig.BlockExplorerLinkFormat, avatarAddress));
            Find<AvatarTooltip>().Show(rectTransform, address);
        }

        private void OnClickChallenge(ArenaRankCell arenaRankCell)
        {
            var currentAvatarInventory = States.Instance.CurrentAvatarState.inventory;

            Game.Game.instance.ActionManager.RankingBattle(
                arenaRankCell.ArenaInfo.AvatarAddress,
                currentAvatarInventory.Costumes
                    .Where(i => i.equipped)
                    .Select(i => i.ItemId).ToList(),
                currentAvatarInventory.Equipments
                    .Where(i => i.equipped)
                    .Select(i => i.ItemId).ToList()
            ).Subscribe();
            Find<ArenaBattleLoadingScreen>().Show(arenaRankCell.ArenaInfo);
        }

        private void SubscribeBackButtonClick(HeaderMenuStatic headerMenuStatic)
        {
            var avatarInfo = Find<AvatarInfoPopup>();
            var friendInfoPopup = Find<FriendInfoPopup>();
            var friendInfoPopupPandora = Find<FriendInfoPopupPandora>();
            if (avatarInfo.gameObject.activeSelf)
            {
                avatarInfo.Close();
            }
            else if (friendInfoPopup.gameObject.activeSelf)
            {
                friendInfoPopup.Close();
                friendInfoPopupPandora.Close();
            }
            else
            {
                if (!CanClose)
                {
                    return;
                }

                Close(true);
                Game.Event.OnRoomEnter.Invoke(true);
            }
        }

        public void GoToStage(BattleLog log)
        {
            Game.Event.OnRankingBattleStart.Invoke(log);
            Close();
        }

        private async Task UpdateWeeklyCache(WeeklyArenaState state)
        {
            int topPlayer = 20;
            if (PandoraBoxMaster.CurrentPandoraPlayer.IsPremium())
                topPlayer = 100;

            var infos = state.GetArenaInfos(1, topPlayer); //3
            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            int upper = 50 + (PandoraBoxMaster.Instance.Settings.ArenaListUpper * PandoraBoxMaster.Instance.Settings.ArenaListStep);
            int lower = 20 + (PandoraBoxMaster.Instance.Settings.ArenaListLower * PandoraBoxMaster.Instance.Settings.ArenaListStep);
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

            if (States.Instance.CurrentAvatarState != null)
            {
                var currentAvatarAddress = States.Instance.CurrentAvatarState.address;
                var infos2 = state.GetArenaInfos(currentAvatarAddress, upper, lower);
                // Player does not play prev & this week arena.
                if (!infos2.Any() && state.OrderedArenaInfos.Any())
                {
                    var address = state.OrderedArenaInfos.Last().AvatarAddress;
                    infos2 = state.GetArenaInfos(address, 90, 0);
                }

                infos.AddRange(infos2);
                infos = infos.ToImmutableHashSet().OrderBy(tuple => tuple.rank).ToList();
            }

            var addressList = infos.Select(i => i.arenaInfo.AvatarAddress).ToList();
            var avatarStates = await Game.Game.instance.Agent.GetAvatarStates(addressList);
            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            avatarStatesPandora = avatarStates;
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
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
    }
}
