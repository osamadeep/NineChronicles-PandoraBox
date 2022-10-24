using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Nekoyume.Game.Controller;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.TableData;
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
        [Header("PANDORA CUSTOM FIELDS")]
        [SerializeField] private TextMeshProUGUI extraInfoText = null;
        [SerializeField] private TextMeshProUGUI lastUpdateText = null;
        public TextMeshProUGUI ExpectedTicketsText = null;
        public TextMeshProUGUI ExpectedRankText = null;
        public TextMeshProUGUI LastFightText = null;
        public Button ExpectedTicketsBtn = null;
        [SerializeField] private Button GoMyPlaceBtn = null;
        [SerializeField] private Button RefreshBtn = null;
        [SerializeField] private Button CancelMultiBtn = null;
        [SerializeField] private UnityEngine.UI.Toggle OnlyLowerTgl = null;
        [SerializeField] private GameObject RefreshObj = null;
        public Slider MultipleSlider = null;

        public int OldScore;
        int LastBlockUpdate;
        [Space(50)]
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
#if UNITY_EDITOR
        [SerializeField]
        private bool _useSo;

        [SerializeField]
        private ArenaBoardSO _so;
#endif

        [SerializeField]
        private ArenaBoardBillboard _billboard;

        public ArenaBoardPlayerScroll _playerScroll;

        [SerializeField]
        private GameObject _noJoinedPlayersGameObject;

        [SerializeField]
        private Button _backButton;

        public ArenaSheet.RoundData _roundData;

        public RxProps.ArenaParticipant[] _boundedData;

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
            GoMyPlaceBtn.OnClickAsObservable().Subscribe(_=> GoMyPlace()).AddTo(gameObject);
            RefreshBtn.OnClickAsObservable().Subscribe(_ => {
                RefreshList().Forget();
                StartCoroutine(RefreshCooldown());
            }).AddTo(gameObject);
            CancelMultiBtn.OnClickAsObservable().Subscribe(_ => CancelMultiArena()).AddTo(gameObject);
            ExpectedTicketsBtn.OnClickAsObservable().Subscribe(_ => ExpectedTicketsToReach()).AddTo(gameObject);
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
        }

        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        void GoMyPlace()
        {
            UpdateScrolls();
        }

        public async UniTaskVoid RefreshList()
        {
            SetLastUpdate();
            RefreshObj.SetActive(true);
            await UniTask.WhenAll(
            RxProps.ArenaInfoTuple.UpdateAsync(),
            RxProps.ArenaParticipantsOrderedWithScore.UpdateAsync())
            .AsUniTask();

            _boundedData = RxProps.ArenaParticipantsOrderedWithScore.Value;
            UpdateBillboard();
            UpdateScrolls();
            RefreshObj.SetActive(false);
        }

        System.Collections.IEnumerator RefreshCooldown()
        {
            RefreshBtn.interactable = false;
            int cooldown = 25;
            if (Premium.IsPremium)
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
            bool redFliker=false;
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
        public void ShowAsyncPandora(bool ignoreShowAnimation = false)
        {
            MultipleSlider.gameObject.SetActive(Premium.ArenaRemainsBattle > 0);
            SetLastUpdate();
            Show(
                RxProps.ArenaParticipantsOrderedWithScore.Value,
                ignoreShowAnimation);
        }

        public void ExpectedTicketsToReach()
        {
            Premium.ExpectedTicketsToReach(ExpectedTicketsText,ExpectedRankText);
        }

        public void CancelMultiArena()
        {
            Premium.CancelMultiArena();
        }
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

        public async UniTaskVoid ShowAsync(bool ignoreShowAnimation = false)
        {
            var loading = Find<DataLoadingScreen>();
            loading.Show();
            await UniTask.WaitWhile(() =>
                RxProps.ArenaParticipantsOrderedWithScore.IsUpdating);
            loading.Close();
            Show(
                RxProps.ArenaParticipantsOrderedWithScore.Value,
                ignoreShowAnimation);
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

            _roundData = roundData;
            _boundedData = arenaParticipants;
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
            //StartCoroutine(RefreshCooldown());
            StartCoroutine(LastUpdateCounter());
            RefreshObj.SetActive(false);
            if (!PandoraMaster.Instance.Settings.ArenaPush)
                Premium.CheckForArenaQueue();
            else
                Premium.PushArenaFight();
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

            _billboard.SetData(
                "season",
                player.Rank,
                player.CurrentArenaInfo.Win,
                player.CurrentArenaInfo.Lose,
                player.CP,
                player.Score);

            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            OldScore = player.Score;
            extraInfoText.text = $"<color=green>{player.CurrentArenaInfo.Win}</color>/<color=red>{player.CurrentArenaInfo.Lose}</color>" +
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
                    var data = _boundedData[index];
                    //Find<FriendInfoPopup>().Show(data.AvatarState);
                    //|||||||||||||| PANDORA START CODE |||||||||||||||||||
                    Find<FriendInfoPopupPandora>().Close(true);
                    Find<FriendInfoPopupPandora>().Show(_roundData, data, RxProps.PlayersArenaParticipant.Value, true);
                    //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
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
                    Close();
                    Find<ArenaBattlePreparation>().Show(
                        _roundData,
                        data.AvatarState);
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
            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            var _boundedDataPandora = _boundedData;
            if (OnlyLowerTgl.isOn)
                _boundedDataPandora = _boundedData.Where(y => y.AvatarState.GetCP() <= RxProps.PlayersArenaParticipant.Value.AvatarState.GetCP()).ToArray();
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

            var currentAvatarAddr = States.Instance.CurrentAvatarState.address;
            var scrollData =
                _boundedDataPandora.Select(e =>
                {
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
                        cp = e.AvatarState.GetCP(),
                        score = e.Score,
                        rank = e.Rank,
                        expectWinDeltaScore = e.ExpectDeltaScore.win,
                        interactableChoiceButton = !e.AvatarAddr.Equals(currentAvatarAddr),
                    };
                }).ToList();
            
            for (var i = 0; i < _boundedDataPandora.Length; i++)
            {
                var data = _boundedDataPandora[i];
                if (data.AvatarAddr.Equals(currentAvatarAddr))
                {
                    return (scrollData, i);
                }
            }

            return (scrollData, 0);
        }
    }
}
