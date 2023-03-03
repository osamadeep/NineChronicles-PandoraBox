using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Libplanet;
using Nekoyume.Battle;
using Nekoyume.Helper;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using Unity.Mathematics;
using UnityEngine;

namespace Nekoyume.UI
{
    using Nekoyume.Game.Factory;
    using Nekoyume.Model.Mail;
    using Nekoyume.PandoraBox;
    using Nekoyume.State;
    using Nekoyume.UI.Scroller;
    using System.Globalization;
    using UniRx;

    public class FriendInfoPopupPandora : PopupWidget
    {
        private const string NicknameTextFormat = "<color=#B38271>Lv.{0}</color=> {1}";

        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        [Header("PANDORA CUSTOM FIELDS")] [SerializeField]
        private AvatarStats currentAvatarStats = null;

        [SerializeField] private TextMeshProUGUI rateText = null;
        [SerializeField] private UnityEngine.UI.Button multipleSimulateButton = null;
        [SerializeField] private UnityEngine.UI.Button soloSimulateButton = null;
        [SerializeField] private UnityEngine.UI.Button NemesisButton = null;
        [SerializeField] private UnityEngine.UI.Button ResetNemesisButton = null;

        [SerializeField] private UnityEngine.UI.Button copyButton = null;
        //AvatarState tempAvatarState;

        //for simulate
        //RxProps.ArenaParticipant meAP = null;
        //public AvatarState enemyAvatarState = null;
        //private ArenaSheet.RoundData _roundData;


        [Space(50)]
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
        [SerializeField]
        private TextMeshProUGUI nicknameText;

        [SerializeField] private Transform titleSocket;

        [SerializeField] private TextMeshProUGUI cpText;

        [SerializeField] private EquipmentSlots costumeSlots;

        [SerializeField] private EquipmentSlots equipmentSlots;

        [SerializeField] private RuneSlots runeSlots;

        [SerializeField] private AvatarStats avatarStats = null;

        [SerializeField] private CategoryTabButton adventureButton;

        [SerializeField] private CategoryTabButton arenaButton;

        [SerializeField] private CategoryTabButton raidButton;

        private GameObject _cachedCharacterTitle;
        public AvatarState _avatarState;
        private readonly ToggleGroup _toggleGroup = new();
        private readonly Dictionary<BattleType, List<Equipment>> _equipments = new();
        private readonly Dictionary<BattleType, List<Costume>> _costumes = new();
        private readonly Dictionary<BattleType, RuneSlotState> _runes = new();
        private readonly List<RuneState> _runeStates = new();


        protected override void Awake()
        {
            _toggleGroup.RegisterToggleable(adventureButton);
            _toggleGroup.RegisterToggleable(arenaButton);
            _toggleGroup.RegisterToggleable(raidButton);

            adventureButton.OnClick
                .Subscribe(b => { OnClickPresetTab(b, BattleType.Adventure); })
                .AddTo(gameObject);
            arenaButton.OnClick
                .Subscribe(b => { OnClickPresetTab(b, BattleType.Arena); })
                .AddTo(gameObject);
            raidButton.OnClick
                .Subscribe(b => { OnClickPresetTab(b, BattleType.Raid); })
                .AddTo(gameObject);

            base.Awake();

            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            multipleSimulateButton.OnClickAsObservable().Subscribe(_ => MultipleSimulate()).AddTo(gameObject);
            soloSimulateButton.OnClickAsObservable().Subscribe(_ => SoloSimulate()).AddTo(gameObject);
            copyButton.OnClickAsObservable().Subscribe(_ => CopyPlayerInfo()).AddTo(gameObject);
            NemesisButton.OnClickAsObservable().Subscribe(_ => SetNemesis()).AddTo(gameObject);
            ResetNemesisButton.OnClickAsObservable().Subscribe(_ => ResetAllNemesis()).AddTo(gameObject);
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
        }

        //|||||||||||||| PANDORA START CODE |||||||||||||||||||

        void SoloSimulate()
        {
            Premium.PVP_SoloSimulate(States.Instance.CurrentAvatarState, _avatarState);
        }

        async void MultipleSimulate()
        {
            rateText.text = "Win Rate :" + "..."; //prevent old value
            if (Premium.PANDORA_CheckPremium())
                rateText.text = "Win Rate :" +
                                await Premium.PVP_WinRate(States.Instance.CurrentAvatarState, _avatarState, 1000);
            multipleSimulateButton.interactable = true;
            multipleSimulateButton.GetComponentInChildren<TextMeshProUGUI>().text = "1000 X Simulate";
        }

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
                "Avatar Name      : " + _avatarState.NameWithHash + "\n" +
                "Avatar Address   : " + _avatarState.address + "\n" +
                "Account Address  : " + _avatarState.agentAddress + "\n" +
                "Date & Time      : " + System.DateTime.Now.ToUniversalTime().ToString() + " (UTC)" + "\n" +
                "Block            : #" + Game.Game.instance.Agent.BlockIndex.ToString() + "\n" +
                "```";
            ClipboardHelper.CopyToClipboard(playerInfo);
            OneLineSystem.Push(MailType.System, "<color=green>Pandora Box</color>: Player (<color=green>" +
                                                _avatarState.NameWithHash
                                                + "</color>) Info copy to Clipboard Successfully!",
                NotificationCell.NotificationType.Information);
        }


        public void SetNemesis()
        {
            TextMeshProUGUI text = NemesisButton.GetComponentInChildren<TextMeshProUGUI>();
            if (PandoraMaster.ArenaFavTargets.Contains(_avatarState.address.ToString()))
            {
                for (int i = 0; i < PandoraMaster.ArenaFavTargets.Count; i++)
                {
                    string key = "_PandoraBox_PVP_FavTarget0" + i + "_" + States.Instance.CurrentAvatarState.address;
                    PlayerPrefs.DeleteKey(key);
                    //PlayerPrefs.SetString(key, PandoraBoxMaster.ArenaFavTargets[i]);
                }

                PandoraMaster.ArenaFavTargets.Remove(_avatarState.address.ToString());
                for (int i = 0; i < PandoraMaster.ArenaFavTargets.Count; i++)
                {
                    string key = "_PandoraBox_PVP_FavTarget0" + i + "_" + States.Instance.CurrentAvatarState.address;
                    PlayerPrefs.SetString(key, PandoraMaster.ArenaFavTargets[i]);
                }

                OneLineSystem.Push(MailType.System, "<color=green>Pandora Box</color>: " + _avatarState.NameWithHash
                    + " removed from your nemesis list!", NotificationCell.NotificationType.Information);
            }
            else
            {
                int maxCount = 2;
                if (Premium.PandoraProfile.IsPremium())
                    maxCount = 9;

                if (PandoraMaster.ArenaFavTargets.Count > maxCount)
                    OneLineSystem.Push(MailType.System,
                        "<color=green>Pandora Box</color>: You reach <color=red>Maximum</color> number of nemesis, please remove some!"
                        , NotificationCell.NotificationType.Information);
                else
                {
                    PandoraMaster.ArenaFavTargets.Add(_avatarState.address.ToString());
                    OneLineSystem.Push(MailType.System,
                        "<color=green>Pandora Box</color>: " + _avatarState.NameWithHash +
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

            text.text = PandoraMaster.ArenaFavTargets.Contains(_avatarState.address.ToString())
                ? "Remove Nemesis"
                : "Set Nemesis";
        }
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

        private void OnClickPresetTab(
            IToggleable toggle,
            BattleType battleType)
        {
            _toggleGroup.SetToggledOffAll();
            toggle.SetToggledOn();

            Game.Game.instance.Lobby.FriendCharacter.Set(
                _avatarState,
                _costumes[battleType],
                _equipments[battleType]);

            UpdateCp(_avatarState, battleType);
            UpdateName(_avatarState);
            UpdateTitle(battleType);
            UpdateSlotView(_avatarState, battleType);
            UpdateStatViews(_avatarState, battleType);
        }

        public async UniTaskVoid ShowAsync(
            AvatarState avatarState,
            BattleType battleType,
            bool ignoreShowAnimation = false)
        {
            _avatarState = avatarState;
            var (itemSlotStates, runeSlotStates) = await avatarState.GetSlotStatesAsync();
            var runeStates = await avatarState.GetRuneStatesAsync();
            SetItems(avatarState, itemSlotStates, runeSlotStates, runeStates);

            base.Show(ignoreShowAnimation);
            switch (battleType)
            {
                case BattleType.Adventure:
                    OnClickPresetTab(adventureButton, battleType);
                    break;
                case BattleType.Arena:
                    OnClickPresetTab(arenaButton, battleType);
                    break;
                case BattleType.Raid:
                    OnClickPresetTab(raidButton, battleType);
                    break;
            }

            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            rateText.text = "Win Rate :" + "..."; //prevent old value
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
        }

        private void SetItems(
            AvatarState avatarState,
            List<ItemSlotState> itemSlotStates,
            List<RuneSlotState> runeSlotStates,
            List<RuneState> runeStates)
        {
            _equipments.Clear();
            _costumes.Clear();
            _equipments.Add(BattleType.Adventure, new List<Equipment>());
            _equipments.Add(BattleType.Arena, new List<Equipment>());
            _equipments.Add(BattleType.Raid, new List<Equipment>());
            _costumes.Add(BattleType.Adventure, new List<Costume>());
            _costumes.Add(BattleType.Arena, new List<Costume>());
            _costumes.Add(BattleType.Raid, new List<Costume>());
            foreach (var state in itemSlotStates)
            {
                var equipments = state.Equipments
                    .Select(guid =>
                        avatarState.inventory.Equipments.FirstOrDefault(x => x.ItemId == guid))
                    .Where(item => item != null).ToList();
                _equipments[state.BattleType] = equipments;

                var costumes = state.Costumes
                    .Select(guid =>
                        avatarState.inventory.Costumes.FirstOrDefault(x => x.ItemId == guid))
                    .Where(item => item != null).ToList();
                _costumes[state.BattleType] = costumes;
            }

            _runes.Clear();
            _runes.Add(BattleType.Adventure, new RuneSlotState(BattleType.Adventure));
            _runes.Add(BattleType.Arena, new RuneSlotState(BattleType.Arena));
            _runes.Add(BattleType.Raid, new RuneSlotState(BattleType.Raid));
            foreach (var state in runeSlotStates)
            {
                _runes[state.BattleType] = state;
            }

            _runeStates.Clear();
            _runeStates.AddRange(runeStates);
        }

        private void UpdateCp(AvatarState avatarState, BattleType battleType)
        {
            var level = avatarState.level;
            var characterSheet = Game.Game.instance.TableSheets.CharacterSheet;
            if (!characterSheet.TryGetValue(avatarState.characterId, out var row))
            {
                throw new SheetRowNotFoundException("CharacterSheet", avatarState.characterId);
            }

            var equippedRuneStates = new List<RuneState>();
            foreach (var slot in _runes[battleType].GetRuneSlot())
            {
                if (!slot.RuneId.HasValue)
                {
                    continue;
                }

                var runeState = _runeStates.FirstOrDefault(x => x.RuneId == slot.RuneId);
                if (runeState != null)
                {
                    equippedRuneStates.Add(runeState);
                }
            }

            var costumeSheet = Game.Game.instance.TableSheets.CostumeStatSheet;
            var equipments = _equipments[battleType];
            var costumes = _costumes[battleType];
            var runeOptionSheet = Game.Game.instance.TableSheets.RuneOptionSheet;
            var runeOptions = Util.GetRuneOptions(equippedRuneStates, runeOptionSheet);
            var cp = CPHelper.TotalCP(equipments, costumes, runeOptions, level, row, costumeSheet);
            cpText.text = $"{cp}";
        }

        private void UpdateName(AvatarState avatarState)
        {
            nicknameText.text = string.Format(
                NicknameTextFormat,
                avatarState.level,
                avatarState.NameWithHash);
        }

        private void UpdateTitle(BattleType battleType)
        {
            var costumes = _costumes[battleType];
            Destroy(_cachedCharacterTitle);
            var title = costumes.FirstOrDefault(costume => costume.ItemSubType == ItemSubType.Title);
            if (title == null)
            {
                return;
            }

            var clone = ResourcesHelper.GetCharacterTitle(title.Grade,
                title.GetLocalizedNonColoredName(false));
            _cachedCharacterTitle = Instantiate(clone, titleSocket);
        }

        private void UpdateSlotView(AvatarState avatarState, BattleType battleType)
        {
            var level = avatarState.level;
            var equipments = _equipments[battleType];
            var costumes = _costumes[battleType];
            var runeSlot = _runes[battleType].GetRuneSlot();
            costumeSlots.SetPlayerCostumes(level, costumes, ShowTooltip, null);
            equipmentSlots.SetPlayerEquipments(level, equipments, ShowTooltip, null);
            runeSlots.Set(runeSlot, _runeStates, ShowRuneTooltip);
        }

        private void UpdateStatViews(AvatarState avatarState, BattleType battleType)
        {
            var equipments = _equipments[battleType];
            var costumes = _costumes[battleType];
            var runeOptionSheet = Game.Game.instance.TableSheets.RuneOptionSheet;
            var runeStates = _runeStates;
            var equipmentSetEffectSheet = Game.Game.instance.TableSheets.EquipmentItemSetEffectSheet;
            var characterSheet = Game.Game.instance.TableSheets.CharacterSheet;
            var costumeSheet = Game.Game.instance.TableSheets.CostumeStatSheet;
            if (!characterSheet.TryGetValue(avatarState.characterId, out var row))
            {
                return;
            }

            var characterStats = new CharacterStats(row, avatarState.level);
            characterStats.SetAll(
                avatarState.level,
                equipments,
                costumes,
                null,
                equipmentSetEffectSheet,
                costumeSheet);

            foreach (var runeState in runeStates)
            {
                if (!runeOptionSheet.TryGetValue(runeState.RuneId, out var statRow) ||
                    !statRow.LevelOptionMap.TryGetValue(runeState.Level, out var statInfo))
                {
                    continue;
                }

                var statModifiers = new List<StatModifier>();
                statModifiers.AddRange(
                    statInfo.Stats.Select(x =>
                        new StatModifier(
                            x.statMap.StatType,
                            x.operationType,
                            x.statMap.ValueAsInt)));

                characterStats.AddOption(statModifiers);
                characterStats.EqualizeCurrentHPWithHP();
            }

            avatarStats.SetData(characterStats);

            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            var myAvatarState = States.Instance.CurrentAvatarState;
            var avatarSlotIndex = States.Instance.AvatarStates
                .FirstOrDefault(x => x.Value.address == myAvatarState.address).Key;
            var itemSlotState = States.Instance.ItemSlotStates[avatarSlotIndex][battleType];
            var myEquipments = itemSlotState.Equipments.Select(guid =>
                    myAvatarState.inventory.Equipments.FirstOrDefault(x => x.ItemId == guid))
                .Where(item => item != null)
                .ToList();
            ;
            var myCostumes = itemSlotState.Costumes.Select(guid =>
                    myAvatarState.inventory.Costumes.FirstOrDefault(x => x.ItemId == guid)).Where(item => item != null)
                .ToList();
            ;
            var myRuneStates = States.Instance.GetEquippedRuneStates(battleType);

            if (!characterSheet.TryGetValue(myAvatarState.characterId, out var myRow))
            {
                return;
            }

            var myCharacterStats = new CharacterStats(myRow, myAvatarState.level);
            myCharacterStats.SetAll(
                myAvatarState.level,
                myEquipments,
                myCostumes,
                null,
                equipmentSetEffectSheet,
                costumeSheet);

            foreach (var myRuneState in myRuneStates)
            {
                if (!runeOptionSheet.TryGetValue(myRuneState.RuneId, out var myStatRow) ||
                    !myStatRow.LevelOptionMap.TryGetValue(myRuneState.Level, out var myStatInfo))
                {
                    continue;
                }

                var myStatModifiers = new List<StatModifier>();
                myStatModifiers.AddRange(
                    myStatInfo.Stats.Select(x =>
                        new StatModifier(
                            x.statMap.StatType,
                            x.operationType,
                            x.statMap.ValueAsInt)));

                myCharacterStats.AddOption(myStatModifiers);
                myCharacterStats.EqualizeCurrentHPWithHP();
            }

            currentAvatarStats.SetData(myCharacterStats);

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
            if (slot.Item == null)
            {
                return;
            }

            var item = new InventoryItem(slot.Item, 1, false, true);
            var tooltip = ItemTooltip.Find(item.ItemBase.ItemType);
            tooltip.Show(item, string.Empty, false, null);
        }

        private void ShowRuneTooltip(RuneSlotView slot)
        {
            if (!slot.RuneSlot.RuneId.HasValue)
            {
                return;
            }

            var runeState = _runeStates.FirstOrDefault(x => x.RuneId == slot.RuneSlot.RuneId.Value);
            if (runeState == null)
            {
                return;
            }

            Find<RuneTooltip>().ShowForDisplay(runeState);
        }
    }
}