using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Nekoyume.Action;
using Nekoyume.Game;
using Nekoyume.Battle;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.TableData.GrandFinale;
using Nekoyume.UI.Module;
using Nekoyume.UI.Module.Arena.Board;
using Nekoyume.UI.Scroller;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;
    using TMPro;
    using Nekoyume.PandoraBox;
    using Nekoyume.Helper;

    public class ArenaBoard : Widget
    {
        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        [Header("PANDORA CUSTOM FIELDS")] [SerializeField]
        private TextMeshProUGUI extraInfoText = null;

        [SerializeField] private TextMeshProUGUI lastUpdateText = null;
        [SerializeField] private TextMeshProUGUI AttackStatusText = null;

        public TextMeshProUGUI ExpectedTicketsText = null;
        public TextMeshProUGUI ExpectedRankText = null;
        public TextMeshProUGUI LastFightText = null;
        public Button ExpectedTicketsBtn = null;
        [SerializeField] private Button GoMyPlaceBtn = null;
        [SerializeField] private Button RefreshBtn = null;

        [SerializeField] private Button CancelMultiBtn = null;

        //[SerializeField] private UnityEngine.UI.Toggle OnlyLowerTgl = null;
        [SerializeField] private GameObject RefreshObj = null;
        public Slider MultipleSlider = null;

        public int OldScore;
        int LastBlockUpdate;
        public long myLastBattle;

        public List<string> CompletedBattlesResult; //TODO , show win/lose on arena button

        [Space(50)]
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
#if UNITY_EDITOR
        [SerializeField]
        private bool _useSo;

        [SerializeField]
        private ArenaBoardSO _so;
#endif

        [SerializeField]
        public ArenaBoardBillboard _billboard;

        public ArenaBoardPlayerScroll _playerScroll;

        [SerializeField] private GameObject _noJoinedPlayersGameObject;

        [SerializeField] private Button _backButton;

        [SerializeField] private GameObject grandFinaleLogoObject;

        public ArenaSheet.RoundData _roundData;
        public RxProps.ArenaParticipant[] _boundedData;
        public GrandFinaleScheduleSheet.Row _grandFinaleScheduleRow;
        public GrandFinaleStates.GrandFinaleParticipant[] _grandFinaleParticipants;
        public bool _useGrandFinale;

        protected override void Awake()
        {
            base.Awake();

            InitializeScrolls();

            _backButton.OnClickAsObservable().Subscribe(_ =>
            {
                AudioController.PlayClick();
                Find<FriendInfoPopupPandora>().Close(true);
                Find<ArenaJoin>().Show();
                Close();
            }).AddTo(gameObject);

            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            GoMyPlaceBtn.OnClickAsObservable().Subscribe(_ => GoMyPlace()).AddTo(gameObject);
            RefreshBtn.OnClickAsObservable().Subscribe(_ =>
            {
                if (_useGrandFinale)
                {
                }
                else
                {
                    RefreshList().Forget();
                    StartCoroutine(RefreshCooldown());
                }
            }).AddTo(gameObject);
            CancelMultiBtn.OnClickAsObservable().Subscribe(_ => CancelMultiArena()).AddTo(gameObject);
            ExpectedTicketsBtn.OnClickAsObservable().Subscribe(_ => ExpectedTicketsToReach()).AddTo(gameObject);
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
        }

        //|||||||||||||| PANDORA START CODE |||||||||||||||||||

        async UniTaskVoid GetDifferenceAttack()
        {
            while (true)
            {
                try
                {
                    long diff = 0;
                    if (_useGrandFinale)
                    {
                        diff = 999;
                    }
                    else
                    {
                        var myArenaAvatarStateAddr =
                            Nekoyume.Model.State.ArenaAvatarState.DeriveAddress(RxProps.PlayersArenaParticipant.Value
                                .AvatarAddr);
                        var myArenaAvatarState =
                            await Game.Game.instance.Agent.GetStateAsync(myArenaAvatarStateAddr) is Bencodex.Types.List
                                serialized
                                ? new Nekoyume.Model.State.ArenaAvatarState(serialized)
                                : null;
                        myLastBattle = myArenaAvatarState.LastBattleBlockIndex;
                        var blockValidator = PandoraMaster.Instance.Settings.ArenaValidator
                            ? Find<VersionSystem>().NodeBlockIndex
                            : Game.Game.instance.Agent.BlockIndex;
                        diff = (long)Mathf.Clamp(blockValidator - myLastBattle, 0, 999);
                    }


                    if (diff < 3)
                    {
                        AttackStatusText.text = $"<color=red>Attack will Fail!</color>({diff})";
                        foreach (Transform item in AttackStatusText.transform)
                            item.gameObject.SetActive(false);
                        AttackStatusText.transform.GetChild(2).gameObject.SetActive(true);
                    }
                    else if (diff >= 3 && diff < 6)
                    {
                        AttackStatusText.text = $"<color=#FF6B00>Risk To Fight!</color>({diff})";
                        foreach (Transform item in AttackStatusText.transform)
                            item.gameObject.SetActive(false);
                        AttackStatusText.transform.GetChild(1).gameObject.SetActive(true);
                    }
                    else
                    {
                        AttackStatusText.text = $"<color=green>Safe To Attack!</color>({diff})";
                        foreach (Transform item in AttackStatusText.transform)
                            item.gameObject.SetActive(false);
                        AttackStatusText.transform.GetChild(0).gameObject.SetActive(true);
                    }

                    await Task.Delay(System.TimeSpan.FromSeconds(15));
                }
                catch
                {
                }

                await Task.Delay(System.TimeSpan.FromSeconds(.1f));
            }
        }

        void GoMyPlace()
        {
            if (_useGrandFinale)
                UpdateScrollsForGrandFinale();
            else
                UpdateScrolls();
        }

        public async UniTaskVoid RefreshList()
        {
            SetLastUpdate();
            RefreshObj.SetActive(true);
            if (_useGrandFinale)
            {
            }
            else
            {
                await UniTask.WhenAll(
                        RxProps.ArenaInfoTuple.UpdateAsync(),
                        RxProps.ArenaParticipantsOrderedWithScore.UpdateAsync())
                    .AsUniTask();
                _boundedData = RxProps.ArenaParticipantsOrderedWithScore.Value;
                UpdateBillboard();
                UpdateScrolls();
            }

            RefreshObj.SetActive(false);
        }

        public System.Collections.IEnumerator RefreshCooldown()
        {
            RefreshBtn.interactable = false;
            int cooldown = 25;
            if (Premium.PandoraProfile.IsPremium())
                cooldown = 10;
            TextMeshProUGUI buttonText = RefreshBtn.GetComponentInChildren<TextMeshProUGUI>();

            for (int i = 0; i < cooldown; i++)
            {
                buttonText.text = (cooldown - i).ToString();
                yield return new WaitForSeconds(1);
            }

            buttonText.text = "Refresh";
            RefreshBtn.interactable = true;
        }

        System.Collections.IEnumerator LastUpdateCounter()
        {
            bool redFliker = false;
            while (true)
            {
                yield return new WaitForSeconds(1);
                //Debug.LogError((int)Game.Game.instance.Agent.BlockIndex + "    " + LastBlockUpdate);
                int differenceBlock = (int)Game.Game.instance.Agent.BlockIndex - LastBlockUpdate;
                var time = Util.GetBlockToTime((int)differenceBlock);

                if (differenceBlock < 10)
                    lastUpdateText.text = $"Last Update: <color=green>{time}</color> ({differenceBlock})";
                else
                {
                    redFliker = !redFliker;
                    if (redFliker)
                        lastUpdateText.text = $"Last Update: <color=red>{time}</color> ({differenceBlock})";
                    else
                        lastUpdateText.text = $"Last Update: {time} ({differenceBlock})";
                }
            }
        }

        public void SetLastUpdate()
        {
            LastBlockUpdate = (int)Game.Game.instance.Agent.BlockIndex;
        }


        //avoid loading screen
        public void ShowPandora(bool ignoreShowAnimation = false)
        {
            MultipleSlider.gameObject.SetActive(Premium.ArenaRemainsBattle > 0);
            SetLastUpdate();
            Show(RxProps.ArenaParticipantsOrderedWithScore.Value, ignoreShowAnimation);

            RefreshList().Forget();
            StartCoroutine(RefreshCooldown());
        }

        public void ExpectedTicketsToReach()
        {
            Premium.PVP_ExpectedTicketsToReach(ExpectedTicketsText, ExpectedRankText);
        }

        public void CancelMultiArena()
        {
            Premium.PVP_CancelPendingFights();
        }
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

        public async UniTaskVoid ShowAsync(bool ignoreShowAnimation = false)
        {
            var loading = Find<DataLoadingScreen>();
            loading.Show();
            if (!_useGrandFinale)
            {
                await UniTask.WaitWhile(() =>
                    RxProps.ArenaParticipantsOrderedWithScore.IsUpdating);
                loading.Close();
                Show(
                    RxProps.ArenaParticipantsOrderedWithScore.Value,
                    ignoreShowAnimation);
            }
            else
            {
                await UniTask.WaitWhile(() => States.Instance.GrandFinaleStates.IsUpdating);
                loading.Close();
                Show(
                    _grandFinaleScheduleRow,
                    States.Instance.GrandFinaleStates.GrandFinaleParticipants);
            }
        }

        public void Show(
            RxProps.ArenaParticipant[] arenaParticipants,
            bool ignoreShowAnimation = false) =>
            Show(_roundData,
                arenaParticipants,
                ignoreShowAnimation);

        public void Show(
            ArenaSheet.RoundData roundData,
            RxProps.ArenaParticipant[] arenaParticipants,
            bool ignoreShowAnimation = false)
        {
            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            PandoraMaster.ArenaFavTargets.Clear();
            for (int i = 0; i < 10; i++) //fav max count
            {
                string key = "_PandoraBox_PVP_FavTarget0" + i + "_" + States.Instance.CurrentAvatarState.address;
                if (PlayerPrefs.HasKey(key))
                    PandoraMaster.ArenaFavTargets.Add(PlayerPrefs.GetString(key));
            }

            MultipleSlider.gameObject.SetActive(Premium.ArenaRemainsBattle > 0);
            RefreshObj.SetActive(false);
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

            _useGrandFinale = false;
            _roundData = roundData;
            _boundedData = arenaParticipants;
            grandFinaleLogoObject.SetActive(false);
            Find<HeaderMenuStatic>().Show(HeaderMenuStatic.AssetVisibleState.Arena);
            UpdateBillboard();
            UpdateScrolls();

            // NOTE: This code assumes that '_playerScroll.Data' contains local player
            //       If `_playerScroll.Data` does not contains local player, change `2` in the line below to `1`.
            //       Not use `_boundedData` here because there is the case to
            //       use the mock data from `_so`.
            _noJoinedPlayersGameObject.SetActive(_playerScroll.Data.Count < 2);


            base.Show(ignoreShowAnimation);
            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            GetDifferenceAttack().Forget();
            //StartCoroutine(RefreshCooldown());
            StartCoroutine(LastUpdateCounter());
            RefreshObj.SetActive(false);
            if (!PandoraMaster.Instance.Settings.ArenaPush)
                Premium.PVP_CheckPendingConfirmFights();
            else
                Premium.PVP_PushFight();
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
        }

        private void UpdateBillboard()
        {
#if UNITY_EDITOR
            if (_useSo && _so)
            {
                _billboard.SetData(
                    _so.SeasonText,
                    _so.Rank,
                    _so.WinCount,
                    _so.LoseCount,
                    _so.CP,
                    _so.Rating);
                return;
            }
#endif
            var player = RxProps.PlayersArenaParticipant.Value;
            if (player is null)
            {
                Debug.Log($"{nameof(RxProps.PlayersArenaParticipant)} is null");
                _billboard.SetData();
                return;
            }

            if (player.CurrentArenaInfo is null)
            {
                Debug.Log($"{nameof(player.CurrentArenaInfo)} is null");
                _billboard.SetData();
                return;
            }

            var equipments = player.ItemSlotState.Equipments
                .Select(guid =>
                    player.AvatarState.inventory.Equipments.FirstOrDefault(x => x.ItemId == guid))
                .Where(item => item != null).ToList();

            var costumes = player.ItemSlotState.Costumes
                .Select(guid =>
                    player.AvatarState.inventory.Costumes.FirstOrDefault(x => x.ItemId == guid))
                .Where(item => item != null).ToList();
            var runeOptionSheet = Game.Game.instance.TableSheets.RuneOptionSheet;
            var runeOptions = Util.GetRuneOptions(player.RuneStates, runeOptionSheet);
            var lv = player.AvatarState.level;
            var characterSheet = Game.Game.instance.TableSheets.CharacterSheet;
            var costumeSheet = Game.Game.instance.TableSheets.CostumeStatSheet;
            if (!characterSheet.TryGetValue(player.AvatarState.characterId, out var row))
            {
                return;
            }

            var cp = CPHelper.TotalCP(equipments, costumes, runeOptions, lv, row, costumeSheet);
            _billboard.SetData(
                "season",
                player.Rank,
                player.CurrentArenaInfo.Win,
                player.CurrentArenaInfo.Lose,
                cp,
                player.Score);

            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            OldScore = player.Score;
            extraInfoText.text =
                $"<color=green>{player.CurrentArenaInfo.Win}</color>/<color=red>{player.CurrentArenaInfo.Lose}</color>" +
                $"\n{player.CurrentArenaInfo.Ticket}\n{player.CurrentArenaInfo.PurchasedTicketCount}";
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
        }

        private void InitializeScrolls()
        {
            _playerScroll.OnClickCharacterView.Subscribe(index =>
                {
#if UNITY_EDITOR
                    if (_useSo && _so)
                    {
                        NotificationSystem.Push(
                            MailType.System,
                            "Cannot open when use mock data in editor mode",
                            NotificationCell.NotificationType.Alert);
                        return;
                    }
#endif
                    //var data = _boundedData[index];
                    //Find<FriendInfoPopup>().Show(data.AvatarState);
                    //|||||||||||||| PANDORA START CODE |||||||||||||||||||
                    Find<FriendInfoPopupPandora>().Close(true);
                    var avatarState = _useGrandFinale
                        ? _grandFinaleParticipants[index].AvatarState
                        : _boundedData[index].AvatarState;

                    Find<FriendInfoPopupPandora>().ShowAsync(avatarState, BattleType.Arena, true).Forget();
                    //Find<FriendInfoPopup>().ShowAsync(avatarState, BattleType.Arena).Forget();
                })
                .AddTo(gameObject);

            _playerScroll.OnClickChoice.Subscribe(index =>
                {
#if UNITY_EDITOR
                    if (_useSo && _so)
                    {
                        NotificationSystem.Push(
                            MailType.System,
                            "Cannot battle when use mock data in editor mode",
                            NotificationCell.NotificationType.Alert);
                        return;
                    }
#endif

                    //|||||||||||||| PANDORA START CODE |||||||||||||||||||
                    Find<FriendInfoPopupPandora>().Close(true);
                    //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
                    var data = _boundedData[index];

                    var equipments = data.ItemSlotState.Equipments
                        .Select(guid =>
                            data.AvatarState.inventory.Equipments.FirstOrDefault(x => x.ItemId == guid))
                        .Where(item => item != null).ToList();
                    var costumes = data.ItemSlotState.Costumes
                        .Select(guid =>
                            data.AvatarState.inventory.Costumes.FirstOrDefault(x => x.ItemId == guid))
                        .Where(item => item != null).ToList();
                    var runeOptionSheet = Game.Game.instance.TableSheets.RuneOptionSheet;
                    var runeOptions = Util.GetRuneOptions(data.RuneStates, runeOptionSheet);
                    var lv = data.AvatarState.level;
                    var costumeSheet = Game.Game.instance.TableSheets.CostumeStatSheet;
                    var characterSheet = Game.Game.instance.TableSheets.CharacterSheet;
                    if (!characterSheet.TryGetValue(data.AvatarState.characterId, out var row))
                    {
                        throw new SheetRowNotFoundException("CharacterSheet",
                            $"{data.AvatarState.characterId}");
                    }

                    Close();

                    if (_useGrandFinale)
                    {
                        Find<ArenaBattlePreparation>().Show(
                            _grandFinaleScheduleRow?.Id ?? 0,
                            _grandFinaleParticipants[index].AvatarState,
                            equipments,
                            costumes,
                            CPHelper.TotalCP(equipments, costumes, runeOptions, lv, row, costumeSheet));
                    }
                    else
                    {
                        Find<ArenaBattlePreparation>().Show(
                            _roundData,
                            _boundedData[index].AvatarState,
                            equipments,
                            costumes,
                            CPHelper.TotalCP(equipments, costumes, runeOptions, lv, row, costumeSheet));
                    }
                })
                .AddTo(gameObject);
        }


        private void UpdateScrolls()
        {
            var (scrollData, playerIndex) =
                GetScrollData();
            _playerScroll.SetData(scrollData, playerIndex);
        }

        private (List<ArenaBoardPlayerItemData> scrollData, int playerIndex)
            GetScrollData()
        {
#if UNITY_EDITOR
            if (_useSo && _so)
            {
                return (_so.ArenaBoardPlayerScrollData, 0);
            }
#endif
            var currentAvatarAddr = States.Instance.CurrentAvatarState.address;
            var characterSheet = Game.Game.instance.TableSheets.CharacterSheet;
            var costumeSheet = Game.Game.instance.TableSheets.CostumeStatSheet;
            var runeOptionSheet = Game.Game.instance.TableSheets.RuneOptionSheet;
            var scrollData =
                _boundedData.Select(e =>
                {
                    var equipments = e.ItemSlotState.Equipments
                        .Select(guid =>
                            e.AvatarState.inventory.Equipments.FirstOrDefault(x => x.ItemId == guid))
                        .Where(item => item != null).ToList();
                    var costumes = e.ItemSlotState.Costumes
                        .Select(guid =>
                            e.AvatarState.inventory.Costumes.FirstOrDefault(x => x.ItemId == guid))
                        .Where(item => item != null).ToList();
                    var runeOptions = Util.GetRuneOptions(e.RuneStates, runeOptionSheet);
                    var lv = e.AvatarState.level;
                    var titleId = costumes.FirstOrDefault(costume =>
                        costume.ItemSubType == ItemSubType.Title && costume.Equipped)?.Id;

                    var portrait = GameConfig.DefaultAvatarArmorId;

                    var armor = equipments.FirstOrDefault(x => x.ItemSubType == ItemSubType.Armor);
                    if (armor != null)
                    {
                        portrait = armor.Id;
                    }

                    var fullCostume = costumes.FirstOrDefault(x => x.ItemSubType == ItemSubType.FullCostume);
                    if (fullCostume != null)
                    {
                        portrait = fullCostume.Id;
                    }

                    if (!characterSheet.TryGetValue(e.AvatarState.characterId, out var row))
                    {
                        throw new SheetRowNotFoundException("CharacterSheet",
                            $"{e.AvatarState.characterId}");
                    }

                    return new ArenaBoardPlayerItemData
                    {
                        name = e.AvatarState.NameWithHash,
                        level = e.AvatarState.level,
                        fullCostumeOrArmorId = portrait,
                        titleId = titleId,
                        cp = CPHelper.TotalCP(equipments, costumes, runeOptions, lv, row, costumeSheet),
                        score = e.Score,
                        rank = e.Rank,
                        expectWinDeltaScore = e.ExpectDeltaScore.win,
                        interactableChoiceButton = !e.AvatarAddr.Equals(currentAvatarAddr),
                        canFight = true,
                    };
                }).ToList();

            for (var i = 0; i < _boundedData.Length; i++)
            {
                var data = _boundedData[i];
                if (data.AvatarAddr.Equals(currentAvatarAddr))
                {
                    return (scrollData, i);
                }
            }

            return (scrollData, 0);
        }

        #region For GrandFinale

        public void Show(
            GrandFinaleScheduleSheet.Row scheduleRow,
            GrandFinaleStates.GrandFinaleParticipant[] arenaParticipants,
            bool ignoreShowAnimation = false)
        {
            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            PandoraMaster.ArenaFavTargets.Clear();
            for (int i = 0; i < 10; i++) //fav max count
            {
                string key = "_PandoraBox_PVP_FavTarget0" + i + "_" + States.Instance.CurrentAvatarState.address;
                if (PlayerPrefs.HasKey(key))
                    PandoraMaster.ArenaFavTargets.Add(PlayerPrefs.GetString(key));
            }

            MultipleSlider.gameObject.SetActive(Premium.ArenaRemainsBattle > 0);
            RefreshObj.SetActive(false);
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

            _useGrandFinale = true;
            _grandFinaleScheduleRow = scheduleRow;
            grandFinaleLogoObject.SetActive(true);
            _grandFinaleParticipants = GetOrderedGrandFinaleParticipants(arenaParticipants.ToList());
            Find<HeaderMenuStatic>().Show(HeaderMenuStatic.AssetVisibleState.Battle);
            UpdateBillboardForGrandFinale();
            UpdateScrollsForGrandFinale();

            _noJoinedPlayersGameObject.SetActive(_playerScroll.Data.Count < 1);
            base.Show(ignoreShowAnimation);
            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            GetDifferenceAttack().Forget();
            //StartCoroutine(RefreshCooldown());
            StartCoroutine(LastUpdateCounter());
            RefreshObj.SetActive(false);
            if (!PandoraMaster.Instance.Settings.ArenaPush)
                Premium.PVP_CheckPendingConfirmFights();
            else
                Premium.PVP_PushFight();
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
        }

        /// <summary>
        /// Participants with battle record are moved back.
        /// If States.Instance.GrandFinaleStates.GrandFinalePlayer is null, return participants.
        /// </summary>
        /// <param name="participants"></param>
        private GrandFinaleStates.GrandFinaleParticipant[] GetOrderedGrandFinaleParticipants(
            List<GrandFinaleStates.GrandFinaleParticipant> participants)
        {
            var grandFinalePlayer = States.Instance.GrandFinaleStates.GrandFinalePlayer;
            if (States.Instance.GrandFinaleStates.GrandFinalePlayer is null)
            {
                return participants.ToArray();
            }

            bool IsFoughtPlayer(GrandFinaleStates.GrandFinaleParticipant data)
            {
                return grandFinalePlayer.CurrentInfo.TryGetBattleRecord(data.AvatarAddr, out _);
            }

            var foughtPlayers = participants.Where(IsFoughtPlayer).ToList();
            participants.RemoveAll(IsFoughtPlayer);
            participants.AddRange(foughtPlayers);
            return participants.ToArray();
        }

        private void UpdateBillboardForGrandFinale()
        {
#if UNITY_EDITOR
            if (_useSo && _so)
            {
                _billboard.SetData(
                    _so.SeasonText,
                    _so.Rank,
                    _so.WinCount,
                    _so.LoseCount,
                    _so.CP,
                    _so.Rating);
                return;
            }
#endif

            var player = States.Instance.GrandFinaleStates.GrandFinalePlayer;
            if (player is null)
            {
                Debug.Log($"{nameof(RxProps.PlayersArenaParticipant)} is null");
                _billboard.SetData();
                return;
            }

            if (player.CurrentInfo is null)
            {
                Debug.Log($"{nameof(player.CurrentInfo)} is null");
                _billboard.SetData();
                return;
            }

            var battleRecords = player.CurrentInfo.GetBattleRecordList();
            var winCount = battleRecords.Count(pair => pair.Value);
            var equipments = player.ItemSlotState.Equipments
                .Select(guid =>
                    player.AvatarState.inventory.Equipments.FirstOrDefault(x => x.ItemId == guid))
                .Where(item => item != null).ToList();

            var costumes = player.ItemSlotState.Costumes
                .Select(guid =>
                    player.AvatarState.inventory.Costumes.FirstOrDefault(x => x.ItemId == guid))
                .Where(item => item != null).ToList();
            var runeOptionSheet = Game.Game.instance.TableSheets.RuneOptionSheet;
            var runeOptions = Util.GetRuneOptions(player.RuneStates, runeOptionSheet);
            var lv = player.AvatarState.level;
            var characterSheet = Game.Game.instance.TableSheets.CharacterSheet;
            var costumeSheet = Game.Game.instance.TableSheets.CostumeStatSheet;
            if (!characterSheet.TryGetValue(player.AvatarState.characterId, out var row))
            {
                return;
            }

            var cp = CPHelper.TotalCP(equipments, costumes, runeOptions, lv, row, costumeSheet);

            _billboard.SetData(
                "GrandFinale",
                player.Rank,
                winCount,
                battleRecords.Count - winCount,
                cp,
                player.Score);
        }

        private void UpdateScrollsForGrandFinale()
        {
            var (scrollData, playerIndex) =
                GetScrollDataForGrandFinale();
            _playerScroll.SetData(scrollData, playerIndex);
        }

        private (List<ArenaBoardPlayerItemData> scrollData, int playerIndex)
            GetScrollDataForGrandFinale()
        {
#if UNITY_EDITOR
            if (_useSo && _so)
            {
                return (_so.ArenaBoardPlayerScrollData, 0);
            }
#endif

            var isParticipant = States.Instance.GrandFinaleStates.GrandFinalePlayer is not null;
            var currentAvatarAddr = States.Instance.CurrentAvatarState.address;
            var characterSheet = Game.Game.instance.TableSheets.CharacterSheet;
            var costumeSheet = Game.Game.instance.TableSheets.CostumeStatSheet;
            var runeOptionSheet = Game.Game.instance.TableSheets.RuneOptionSheet;
            var scrollData =
                _grandFinaleParticipants.Select(e =>
                {
                    var hasBattleRecord = false;
                    var win = false;
                    if (isParticipant)
                    {
                        hasBattleRecord = States.Instance.GrandFinaleStates.GrandFinalePlayer
                            .CurrentInfo
                            .TryGetBattleRecord(e.AvatarAddr, out win);
                    }

                    var equipments = e.ItemSlotState.Equipments
                        .Select(guid =>
                            e.AvatarState.inventory.Equipments.FirstOrDefault(x => x.ItemId == guid))
                        .Where(item => item != null).ToList();
                    var costumes = e.ItemSlotState.Costumes
                        .Select(guid =>
                            e.AvatarState.inventory.Costumes.FirstOrDefault(x => x.ItemId == guid))
                        .Where(item => item != null).ToList();
                    var runeOptions = Util.GetRuneOptions(e.RuneStates, runeOptionSheet);
                    var lv = e.AvatarState.level;
                    if (!characterSheet.TryGetValue(e.AvatarState.characterId, out var row))
                    {
                        throw new SheetRowNotFoundException("CharacterSheet",
                            $"{e.AvatarState.characterId}");
                    }

                    return new ArenaBoardPlayerItemData
                    {
                        name = e.AvatarState.NameWithHash,
                        level = e.AvatarState.level,

                        fullCostumeOrArmorId =
                            e.AvatarState.inventory.GetEquippedFullCostumeOrArmorId(),
                        titleId = e.AvatarState.inventory.Costumes
                            .FirstOrDefault(costume =>
                                costume.ItemSubType == ItemSubType.Title
                                && costume.Equipped)?
                            .Id,
                        cp = CPHelper.TotalCP(equipments, costumes, runeOptions, lv, row, costumeSheet),
                        score = e.Score,
                        rank = e.Rank,
                        expectWinDeltaScore = BattleGrandFinale.WinScore,
                        interactableChoiceButton = !e.AvatarAddr.Equals(currentAvatarAddr),
                        canFight = isParticipant,
                        winAtGrandFinale = hasBattleRecord ? win : null,
                    };
                }).ToList();

            for (var i = 0; i < _grandFinaleParticipants.Length; i++)
            {
                var data = _grandFinaleParticipants[i];
                if (data.AvatarAddr.Equals(currentAvatarAddr))
                {
                    return (scrollData, i);
                }
            }

            return (scrollData, 0);
        }

        #endregion
    }
}