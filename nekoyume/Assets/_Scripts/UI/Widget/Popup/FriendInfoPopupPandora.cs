using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Nekoyume.Arena;
using Nekoyume.Battle;
using Nekoyume.EnumType;
using Nekoyume.Game.Character;
using Nekoyume.Game.Factory;
using Nekoyume.Helper;
using Nekoyume.Model.Arena;
using Nekoyume.Model.BattleStatus.Arena;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.PandoraBox;
using Nekoyume.State;
using Nekoyume.TableData;
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
        RxProps.ArenaParticipant meAP = null;
        public RxProps.ArenaParticipant enemyAP= null;
        private ArenaSheet.RoundData _roundData;


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
            for (int i = 0; i < PandoraMaster.ArenaFavTargets.Count; i++)
            {
                string key = "_PandoraBox_PVP_FavTarget0" + i + "_" + States.Instance.CurrentAvatarState.address;
                PlayerPrefs.DeleteKey(key);
            }

            PandoraMaster.ArenaFavTargets.Clear();

            OneLineSystem.Push(MailType.System,
                "<color=green>Pandora Box</color>: <color=red>Nemesis</color> list is clear Successfully!"
                , NotificationCell.NotificationType.Information);
        }

        void CopyPlayerInfo()
        {
            string playerInfo =
                "```prolog\n" +
                "Avatar Name      : " + enemyAP.AvatarState.NameWithHash + "\n" +
                "Avatar Address   : " + enemyAP.AvatarState.address + "\n" +
                "Account Address  : " + enemyAP.AvatarState.agentAddress + "\n" +
                "Date & Time      : " + System.DateTime.Now.ToUniversalTime().ToString() + " (UTC)" + "\n" +
                "Block            : #" + Game.Game.instance.Agent.BlockIndex.ToString() + "\n" +
                "```";
            ClipboardHelper.CopyToClipboard(playerInfo);
            OneLineSystem.Push(MailType.System, "<color=green>Pandora Box</color>: Player (<color=green>" +
                                                enemyAP.AvatarState.NameWithHash
                                                + "</color>) Info copy to Clipboard Successfully!",
                NotificationCell.NotificationType.Information);
        }


        public void SetNemesis()
        {
            TextMeshProUGUI text = NemesisButton.GetComponentInChildren<TextMeshProUGUI>();
            if (PandoraMaster.ArenaFavTargets.Contains(enemyAP.AvatarState.address.ToString()))
            {
                for (int i = 0; i < PandoraMaster.ArenaFavTargets.Count; i++)
                {
                    string key = "_PandoraBox_PVP_FavTarget0" + i + "_" + States.Instance.CurrentAvatarState.address;
                    PlayerPrefs.DeleteKey(key);
                    //PlayerPrefs.SetString(key, PandoraBoxMaster.ArenaFavTargets[i]);
                }

                PandoraMaster.ArenaFavTargets.Remove(enemyAP.AvatarState.address.ToString());
                for (int i = 0; i < PandoraMaster.ArenaFavTargets.Count; i++)
                {
                    string key = "_PandoraBox_PVP_FavTarget0" + i + "_" + States.Instance.CurrentAvatarState.address;
                    PlayerPrefs.SetString(key, PandoraMaster.ArenaFavTargets[i]);
                }

                OneLineSystem.Push(MailType.System, "<color=green>Pandora Box</color>: " + enemyAP.AvatarState.NameWithHash
                    + " removed from your nemesis list!", NotificationCell.NotificationType.Information);
            }
            else
            {
                int maxCount = 2;
                if (Premium.CurrentPandoraPlayer.PremiumEndBlock > Game.Game.instance.Agent.BlockIndex)
                    maxCount = 9;

                if (PandoraMaster.ArenaFavTargets.Count > maxCount)
                    OneLineSystem.Push(MailType.System,
                        "<color=green>Pandora Box</color>: You reach <color=red>Maximum</color> number of nemesis, please remove some!"
                        , NotificationCell.NotificationType.Information);
                else
                {
                    PandoraMaster.ArenaFavTargets.Add(enemyAP.AvatarState.address.ToString());
                    OneLineSystem.Push(MailType.System,
                        "<color=green>Pandora Box</color>: " + enemyAP.AvatarState.NameWithHash +
                        " added to your nemesis list!"
                        , NotificationCell.NotificationType.Information);
                    for (int i = 0; i < PandoraMaster.ArenaFavTargets.Count; i++)
                    {
                        string key = "_PandoraBox_PVP_FavTarget0" + i + "_" +
                                     States.Instance.CurrentAvatarState.address;
                        PlayerPrefs.SetString(key, PandoraMaster.ArenaFavTargets[i]);
                    }
                }
            }

            text.text = PandoraMaster.ArenaFavTargets.Contains(enemyAP.AvatarState.address.ToString())
                ? "Remove Nemesis"
                : "Set Nemesis";
        }

        public void Show(ArenaSheet.RoundData roundData, RxProps.ArenaParticipant APenemy, RxProps.ArenaParticipant APme, bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            enemyAP = APenemy;
            meAP = APme;
            _roundData = roundData;
            //Debug.LogError($"{enemyAP.AvatarState.name} + {enemyAP.AvatarState.ToArenaAvatarState().}");

            multipleSimulateButton.interactable = true;
            multipleSimulateButton.GetComponentInChildren<TextMeshProUGUI>().text = "100 X Simulate";
            rateText.text = "Win Rate :";

            InitializePlayer(enemyAP.AvatarState);
            UpdateSlotView(enemyAP.AvatarState);
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

        void SoloSimulate()
        {
            Premium.SoloSimulate(meAP.AvatarAddr, enemyAP.AvatarAddr, meAP.AvatarState, enemyAP.AvatarState);
        }

        async void MultipleSimulate()
        {
            rateText.text = "Win Rate :" + "..."; //prevent old value
            Premium.CheckPremium();
            rateText.text = "Win Rate :" + await Premium.WinRatePVP(meAP.AvatarAddr, enemyAP.AvatarAddr, meAP.AvatarState, enemyAP.AvatarState,100);
            multipleSimulateButton.interactable = true;
            multipleSimulateButton.GetComponentInChildren<TextMeshProUGUI>().text = "100 X Simulate";
        }

        protected override void OnCompleteOfCloseAnimationInternal()
        {
            TerminatePlayer();
        }

        #endregion

        private void InitializePlayer(AvatarState avatarState)
        {
            _player = Util.CreatePlayer(avatarState, NPCPosition);
            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            _player.avatarAddress = avatarState.address.ToString();
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
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
            //Find<RankingBoard>().avatarLoadingImage.SetActive(false);
            Player _currentPlayer;
            _currentPlayer = PlayerFactory.Create(States.Instance.CurrentAvatarState).GetComponent<Player>();
            _currentPlayer.avatarAddress = States.Instance.CurrentAvatarState.address.ToString();
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
            _currentPlayer.gameObject.SetActive(false);

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
