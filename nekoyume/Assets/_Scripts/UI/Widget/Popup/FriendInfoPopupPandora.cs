using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Nekoyume.Battle;
using Nekoyume.EnumType;
using Nekoyume.Game.Character;
using Nekoyume.Game.Factory;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.PandoraBox;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class FriendInfoPopupPandora : PopupWidget
    {
        private const string NicknameTextFormat = "<color=#B38271>Lv.{0}</color=> {1}";

        private static readonly Vector3 NPCPosition = new Vector3(2000f, 1999.2f, 2.15f);
        private static readonly Vector3 NPCPositionInLobbyCamera = new Vector3(5000f, 4999.13f, 0f);

        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        [Header("PANDORA CUSTOM FIELDS")] [SerializeField]
        private GameObject paidMember = null;

        [SerializeField] private AvatarStats currentAvatarStats = null;
        [SerializeField] private TextMeshProUGUI rateText = null;
        [SerializeField] private Button multipleSimulateButton = null;
        [SerializeField] private Button soloSimulateButton = null;
        [SerializeField] private Button NemesisButton = null;
        [SerializeField] private Button ResetNemesisButton = null;

        [SerializeField] private Button copyButton = null;
        //AvatarState tempAvatarState;

        //for simulate
        [HideInInspector] public ArenaInfo enemyArenaInfo = null;
        ArenaInfo currentAvatarArenaInfo = null;
        AvatarState avatarState;

        [Space(50)]
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
        [SerializeField]
        private TextMeshProUGUI nicknameText = null;

        [SerializeField] private Transform titleSocket = null;

        [SerializeField] private TextMeshProUGUI cpText = null;

        [SerializeField] private EquipmentSlots costumeSlots = null;

        [SerializeField] private EquipmentSlots equipmentSlots = null;

        [SerializeField] private AvatarStats avatarStats = new AvatarStats();

        [SerializeField] private RawImage playerRawImage;

        [SerializeField] private RawImage playerRawImageInLobbyCamera;

        private CharacterStats _tempStats;
        private GameObject _cachedCharacterTitle;
        private Player _player;

        #region Override

        protected override void Awake()
        {
            base.Awake();

            costumeSlots.gameObject.SetActive(false);
            equipmentSlots.gameObject.SetActive(true);

            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            multipleSimulateButton.OnClickAsObservable().Subscribe(_ => MultipleSimulate()).AddTo(gameObject);
            soloSimulateButton.OnClickAsObservable().Subscribe(_ => SoloSimulate()).AddTo(gameObject);
            copyButton.OnClickAsObservable().Subscribe(_ => CopyPlayerInfo()).AddTo(gameObject);
            NemesisButton.OnClickAsObservable().Subscribe(_ => SetNemesis()).AddTo(gameObject);
            ResetNemesisButton.OnClickAsObservable().Subscribe(_ => ResetAllNemesis()).AddTo(gameObject);
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
        }

        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        public void ResetAllNemesis()
        {
            for (int i = 0; i < PandoraBoxMaster.ArenaFavTargets.Count; i++)
            {
                string key = "_PandoraBox_PVP_FavTarget0" + i + "_" + States.Instance.CurrentAvatarState.address;
                PlayerPrefs.DeleteKey(key);
            }

            PandoraBoxMaster.ArenaFavTargets.Clear();

            OneLineSystem.Push(MailType.System,
                "<color=green>Pandora Box</color>: <color=red>Nemesis</color> list is clear Successfully!"
                , NotificationCell.NotificationType.Information);
        }

        void CopyPlayerInfo()
        {
            string playerInfo =
                "```prolog\n" +
                "Avatar Name      : " + avatarState.NameWithHash + "\n" +
                "Avatar Address   : " + avatarState.address + "\n" +
                "Account Address  : " + avatarState.agentAddress + "\n" +
                "Date & Time      : " + System.DateTime.Now.ToUniversalTime().ToString() + " (UTC)" + "\n" +
                "Block            : #" + Game.Game.instance.Agent.BlockIndex.ToString() + "\n" +
                "```";
            ClipboardHelper.CopyToClipboard(playerInfo);
            OneLineSystem.Push(MailType.System, "<color=green>Pandora Box</color>: Player (<color=green>" +
                                                avatarState.NameWithHash
                                                + "</color>) Info copy to Clipboard Successfully!",
                NotificationCell.NotificationType.Information);
        }


        public void SetNemesis()
        {
            TextMeshProUGUI text = NemesisButton.GetComponentInChildren<TextMeshProUGUI>();
            if (PandoraBoxMaster.ArenaFavTargets.Contains(avatarState.address.ToString()))
            {
                for (int i = 0; i < PandoraBoxMaster.ArenaFavTargets.Count; i++)
                {
                    string key = "_PandoraBox_PVP_FavTarget0" + i + "_" + States.Instance.CurrentAvatarState.address;
                    PlayerPrefs.DeleteKey(key);
                    //PlayerPrefs.SetString(key, PandoraBoxMaster.ArenaFavTargets[i]);
                }

                PandoraBoxMaster.ArenaFavTargets.Remove(avatarState.address.ToString());
                for (int i = 0; i < PandoraBoxMaster.ArenaFavTargets.Count; i++)
                {
                    string key = "_PandoraBox_PVP_FavTarget0" + i + "_" + States.Instance.CurrentAvatarState.address;
                    PlayerPrefs.SetString(key, PandoraBoxMaster.ArenaFavTargets[i]);
                }

                OneLineSystem.Push(MailType.System, "<color=green>Pandora Box</color>: " + avatarState.NameWithHash
                    + " removed from your nemesis list!", NotificationCell.NotificationType.Information);
            }
            else
            {
                int maxCount = 2;
                if (PandoraBoxMaster.CurrentPandoraPlayer.PremiumEndBlock > Game.Game.instance.Agent.BlockIndex)
                    maxCount = 9;

                if (PandoraBoxMaster.ArenaFavTargets.Count > maxCount)
                    OneLineSystem.Push(MailType.System,
                        "<color=green>Pandora Box</color>: You reach <color=red>Maximum</color> number of nemesis, please remove some!"
                        , NotificationCell.NotificationType.Information);
                else
                {
                    PandoraBoxMaster.ArenaFavTargets.Add(avatarState.address.ToString());
                    OneLineSystem.Push(MailType.System,
                        "<color=green>Pandora Box</color>: " + avatarState.NameWithHash +
                        " added to your nemesis list!"
                        , NotificationCell.NotificationType.Information);
                    for (int i = 0; i < PandoraBoxMaster.ArenaFavTargets.Count; i++)
                    {
                        string key = "_PandoraBox_PVP_FavTarget0" + i + "_" +
                                     States.Instance.CurrentAvatarState.address;
                        PlayerPrefs.SetString(key, PandoraBoxMaster.ArenaFavTargets[i]);
                    }
                }
            }

            text.text = PandoraBoxMaster.ArenaFavTargets.Contains(avatarState.address.ToString())
                ? "Remove Nemesis"
                : "Set Nemesis";
        }

        public void Show(AvatarState avatarStt, ArenaInfo enemyAI, ArenaInfo currentAvatarAI,
            bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            enemyArenaInfo = enemyAI;
            currentAvatarArenaInfo = currentAvatarAI;
            avatarState = avatarStt;

            multipleSimulateButton.interactable = true;
            multipleSimulateButton.GetComponentInChildren<TextMeshProUGUI>().text = "100 X Simulate";
            rateText.text = "Win Rate :";

            InitializePlayer(avatarState);
            UpdateSlotView(avatarState);
            UpdateStatViews();
        }

        private class LocalRandom : System.Random, Libplanet.Action.IRandom
        {
            public LocalRandom(int Seed)
                : base(Seed)
            {
            }

            public int Seed => throw new System.NotImplementedException();
        }


        public void SoloSimulate()
        {
            if (!PandoraBoxMaster.CurrentPandoraPlayer.IsPremium())
            {
                OneLineSystem.Push(MailType.System,
                    "<color=green>Pandora Box</color>: this is <color=green>PREMIUM</color> feature!",
                    NotificationCell.NotificationType.Alert);
                return;
            }
            PandoraBoxMaster.CurrentArenaEnemyAddress = avatarState.address.ToString().ToLower();
            var simulator = new RankingSimulator(
                new Cheat.DebugRandom(),
                States.Instance.CurrentAvatarState,
                avatarState,
                new List<System.Guid>(),
                Game.Game.instance.TableSheets.GetRankingSimulatorSheets(),
                999999
            );

            //System.Random rnd = new System.Random();
            //var simulator = new RankingSimulator(
            //    new LocalRandom(rnd.Next(-1000000000, 1000000000)),
            //    States.Instance.CurrentAvatarState,
            //    avatarState,
            //    new List<System.Guid>(),
            //    Game.Game.instance.TableSheets.GetRankingSimulatorSheets(),
            //    Action.RankingBattle.StageId,
            //    currentAvatarArenaInfo,
            //    enemyArenaInfo,
            //    Game.Game.instance.TableSheets.CostumeStatSheet
            //);
            simulator.Simulate();
            var log = simulator.Log;

            Widget.Find<FriendInfoPopupPandora>().Close(true);
            PandoraBoxMaster.IsRankingSimulate = true;
            Widget.Find<RankingBoard>().GoToStage(log);
        }

        public void MultipleSimulate()
        {
            if (!PandoraBoxMaster.CurrentPandoraPlayer.IsPremium())
            {
                OneLineSystem.Push(MailType.System,
                    "<color=green>Pandora Box</color>: this is <color=green>PREMIUM</color> feature!",
                    NotificationCell.NotificationType.Alert);
                return;
            }

            StartCoroutine(GetEnemyState());
        }


        IEnumerator GetEnemyState()
        {
            multipleSimulateButton.interactable = false;
            multipleSimulateButton.GetComponentInChildren<TextMeshProUGUI>().text = "Simulating...";
            rateText.text = "Win Rate :";

            int totalSimulations = 100;
            int win = 0;
            for (int i = 0; i < totalSimulations; i++)
            {
                var simulator = new RankingSimulator(
                    new Cheat.DebugRandom(),
                    States.Instance.CurrentAvatarState,
                    avatarState,
                    new List<System.Guid>(),
                    Game.Game.instance.TableSheets.GetRankingSimulatorSheets(),
                    999999
                );

                //System.Random rnd = new System.Random();
                //var simulator = new RankingSimulator(
                //    new LocalRandom(rnd.Next(-1000000000, 1000000000)),
                //    States.Instance.CurrentAvatarState,
                //    avatarState,
                //    new List<System.Guid>(),
                //    Game.Game.instance.TableSheets.GetRankingSimulatorSheets(),
                //    Action.RankingBattle.StageId,
                //    currentAvatarArenaInfo,
                //    enemyArenaInfo,
                //    Game.Game.instance.TableSheets.CostumeStatSheet
                //);
                simulator.Simulate();
                var log = simulator.Log;

                if (log.result.ToString().ToUpper() == "WIN")
                    win++;
                yield return new WaitForSeconds(0.05f);
            }

            float finalRatio = (float)win / (float)totalSimulations;
            float FinalValue = (int)(finalRatio * 100f);

            if (finalRatio <= 0.5f)
                rateText.text = $"Win Rate : <color=red>{FinalValue}</color>%";
            else if (finalRatio > 0.5f && finalRatio <= 0.75f)
                rateText.text = $"Win Rate : <color=#FF4900>{FinalValue}</color>%";
            else
                rateText.text = $"Win Rate : <color=green>{FinalValue}</color>%";

            multipleSimulateButton.interactable = true;
            multipleSimulateButton.GetComponentInChildren<TextMeshProUGUI>().text = "100 X Simulate";
        }


        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

        //public override void Show(bool ignoreShowAnimation = false)
        //{
        //    var currentAvatarState = Game.Game.instance.States.CurrentAvatarState;
        //    Show(currentAvatarState, ignoreShowAnimation);
        //}

        protected override void OnCompleteOfCloseAnimationInternal()
        {
            TerminatePlayer();
        }

        #endregion

        //public void Show(AvatarState avatarState, bool ignoreShowAnimation = false)
        //{
        //    base.Show(ignoreShowAnimation);

        //    InitializePlayer(avatarState);
        //    UpdateSlotView(avatarState);
        //    UpdateStatViews();

        //    //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        //    multipleSimulateButton.interactable = true;
        //    multipleSimulateButton.GetComponentInChildren<TextMeshProUGUI>().text = "100 X Simulate";
        //    rateText.text = "Win Rate :";
        //    //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
        //}

        private void InitializePlayer(AvatarState avatarState)
        {
            _player = PlayerFactory.Create(avatarState).GetComponent<Player>();
            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            _player.avatarAddress = avatarState.address.ToString();
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
            var t = _player.transform;
            t.localScale = Vector3.one;

            var playerInLobby = Find<Menu>().isActiveAndEnabled;
            if (playerInLobby)
            {
                t.position = NPCPosition;
                playerRawImage.gameObject.SetActive(true);
                playerRawImageInLobbyCamera.gameObject.SetActive(false);
            }
            else
            {
                t.position = NPCPositionInLobbyCamera;
                playerRawImage.gameObject.SetActive(false);
                playerRawImageInLobbyCamera.gameObject.SetActive(true);
            }
        }

        private void TerminatePlayer()
        {
            var t = _player.transform;
            t.SetParent(Game.Game.instance.Stage.transform);
            t.localScale = Vector3.one;
            _player.gameObject.SetActive(false);
            _player = null;
        }

        private void UpdateSlotView(AvatarState avatarState)
        {
            var game = Game.Game.instance;
            var playerModel = _player.Model;

            nicknameText.text = string.Format(
                NicknameTextFormat,
                avatarState.level,
                avatarState.NameWithHash);

            var title = avatarState.inventory.Costumes.FirstOrDefault(costume =>
                costume.ItemSubType == ItemSubType.Title &&
                costume.equipped);

            if (!(title is null))
            {
                Destroy(_cachedCharacterTitle);
                var clone = ResourcesHelper.GetCharacterTitle(title.Grade,
                    title.GetLocalizedNonColoredName(false));
                _cachedCharacterTitle = Instantiate(clone, titleSocket);
            }

            cpText.text = CPHelper
                .GetCPV2(avatarState, game.TableSheets.CharacterSheet,
                    game.TableSheets.CostumeStatSheet)
                .ToString();

            costumeSlots.SetPlayerCostumes(playerModel, ShowTooltip, null);
            equipmentSlots.SetPlayerEquipments(playerModel, ShowTooltip, null);
        }

        private void UpdateStatViews()
        {
            _tempStats = _player.Model.Stats.Clone() as CharacterStats;
            var equipments = equipmentSlots
                .Where(slot => !slot.IsLock && !slot.IsEmpty)
                .Select(slot => slot.Item as Equipment)
                .Where(item => !(item is null))
                .ToList();

            var costumes = costumeSlots
                .Where(slot => !slot.IsLock && !slot.IsEmpty)
                .Select(slot => slot.Item as Costume)
                .Where(item => !(item is null))
                .ToList();

            var equipEffectSheet = Game.Game.instance.TableSheets.EquipmentItemSetEffectSheet;
            var costumeSheet = Game.Game.instance.TableSheets.CostumeStatSheet;
            var stats = _tempStats.SetAll(_tempStats.Level, equipments, costumes, null, equipEffectSheet, costumeSheet);
            avatarStats.SetData(stats);

            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            Find<RankingBoard>().avatarLoadingImage.SetActive(false);
            Player _currentPlayer;
            _currentPlayer = PlayerFactory.Create(States.Instance.CurrentAvatarState).GetComponent<Player>();
            _currentPlayer.avatarAddress = States.Instance.CurrentAvatarState.address.ToString();
            _currentPlayer.gameObject.SetActive(false);
            _tempStats = _currentPlayer.Model.Stats.Clone() as CharacterStats;
            stats = _tempStats.SetAll(_tempStats.Level, _currentPlayer.Equipments, _currentPlayer.Costumes, null,
                equipEffectSheet, costumeSheet);
            currentAvatarStats.SetData(stats);

            //color fields
            for (int i = 0; i < 6; i++)
            {
                if (i == 3)
                    continue;
                DetailedStatView enemyST = avatarStats.transform.GetChild(i).GetComponent<DetailedStatView>();
                DetailedStatView currentST = currentAvatarStats.transform.GetChild(i).GetComponent<DetailedStatView>();
                if (float.Parse(enemyST.valueText.text, CultureInfo.InvariantCulture) >
                    float.Parse(currentST.valueText.text, CultureInfo.InvariantCulture))
                    currentST.valueText.text = $"<color=red>{currentST.valueText.text}</color>";
                else
                    currentST.valueText.text = $"<color=green>{currentST.valueText.text}</color>";
            }

            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
        }

        private static void ShowTooltip(EquipmentSlot slot)
        {
            var item = new InventoryItem(slot.Item, 1, true, false, true);
            var tooltip = ItemTooltip.Find(item.ItemBase.ItemType);
            tooltip.Show(item, string.Empty, false, null, target: slot.RectTransform);
        }
    }
}
