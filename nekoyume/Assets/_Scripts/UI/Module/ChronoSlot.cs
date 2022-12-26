using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.EnumType;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

namespace Nekoyume.UI.Module
{
    using Bencodex.Types;
    using Cysharp.Threading.Tasks.Triggers;
    using mixpanel;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.Mail;
    using Nekoyume.PandoraBox;
    using Nekoyume.State.Subjects;
    using Nekoyume.UI.Module.WorldBoss;
    using Nekoyume.UI.Scroller;
    using System.Globalization;
    using System.Threading.Tasks;
    using UniRx;

    public class ChronoSlot : MonoBehaviour
    {
        [SerializeField] private GameObject SelectedAvatarVFX;
        [SerializeField] private TextMeshProUGUI SlotNumberText;
        [SerializeField] private TextMeshProUGUI AvatarNameText;
        [SerializeField] private TextMeshProUGUI ProsperityText;
        [SerializeField] private GameObject ProsperityImage;
        [SerializeField] private TextMeshProUGUI APText;
        [SerializeField] private GameObject APImage;
        [SerializeField] private TextMeshProUGUI SweepStageText;
        //[SerializeField] private TextMeshProUGUI ArenaText;
        [SerializeField] private TextMeshProUGUI[] CraftingSlotsText;
        [SerializeField] private Image slotNotificationImage;
        [SerializeField] private Image stageNotificationImage;
        [SerializeField] private Image craftNotificationImage;
        [SerializeField] private Button SwitchButton;
        [SerializeField] private Button SettingsButton;
        private AvatarState currentAvatarState;
        private int _slotIndex;
        public bool HasNotification;

        //settings
        List<CombinationSlotState> Combinationslotstates = new List<CombinationSlotState>();
        public bool IsPrefsLoded;
        bool IsStageNotify;
        bool IsCraftNotify;
        bool IsAutoCollect;
        bool IsAutoSpend;
        int sweepStage;

        long currentBlockIndex;
        long collectProsperityCooldown;
        long spendActionPointsCooldown;
        public long craftSlotsUpdateCooldown;

        private void Awake()
        {
            SwitchButton.onClick.AddListener(() => { SwitchChar(); });
            SettingsButton.onClick.AddListener(() => { OpenSlotSettings(); });
        }

        private void OnEnable()
        {
            //GetState();
        }

        void OpenSlotSettings()
        {
            Widget.Find<ChronoSettingsPopup>().Show(currentAvatarState, _slotIndex);
        }


        public async void GetState()
        {
            var (exist, avatarState) = await States.TryGetAvatarStateAsync(States.Instance.CurrentAvatarState.address);
            currentAvatarState = avatarState;
        }

        public void CollectAP()
        {
            if (currentAvatarState.address != States.Instance.CurrentAvatarState.address)
            {
                if (!Premium.CheckPremiumFeature())
                    return;
            }

            if (currentAvatarState.actionPoint > 5)
            {
                OneLineSystem.Push(MailType.System, $"<color=green>Pandora Box</color>: There is still Action Points!", NotificationCell.NotificationType.Information);
                return;
            }

            Game.Game.instance.ActionManager.DailyRewardPandora(currentAvatarState).Subscribe();

            collectProsperityCooldown = currentBlockIndex + 4;
            OneLineSystem.Push(MailType.System, $"<color=green>Pandora Box</color>: Prosperity bar for {currentAvatarState.NameWithHash} Auto collected!", NotificationCell.NotificationType.Information);


            //var address = currentAvatarState.address;
            //if (GameConfigStateSubject.ActionPointState.ContainsKey(address))
            //{
            //    GameConfigStateSubject.ActionPointState.Remove(address);
            //}
            //GameConfigStateSubject.ActionPointState.Add(address, true);
        }

        void LoadSettings()
        {
            if (!IsPrefsLoded)
            {
                IsPrefsLoded = true; //to read it once
                string addressKey = "_PandoraBox_Chrono_" + currentAvatarState.address;
                if (PlayerPrefs.HasKey(addressKey))
                {
                    IsStageNotify = System.Convert.ToBoolean(PlayerPrefs.GetInt(addressKey + "_IsStageNotify", 1));
                    IsCraftNotify = System.Convert.ToBoolean(PlayerPrefs.GetInt(addressKey + "_IsCraftNotify", 1));
                    IsAutoCollect = System.Convert.ToBoolean(PlayerPrefs.GetInt(addressKey + "_IsAutoCollect", 1));
                    IsAutoSpend = System.Convert.ToBoolean(PlayerPrefs.GetInt(addressKey + "_IsAutoSpend", 0));
                    sweepStage = PlayerPrefs.GetInt(addressKey + "_SweepStage", 0);
                }
                else
                {
                    //register variables
                    IsStageNotify = true;
                    IsCraftNotify = true;
                    IsAutoCollect = true;
                    IsAutoSpend = false;
                    sweepStage = 0;
                }

                var states = States.Instance.AvatarStates;
                SwitchButton.gameObject.SetActive(states.Where(x => x.Value.address == currentAvatarState.address).Count() > 0);
                SlotNumberText.text = "#" + (_slotIndex + 1);
            }

            //check for premium
            if (!Premium.CurrentPandoraPlayer.IsPremium())
            {
                try
                {
                    if (currentAvatarState.address != States.Instance.CurrentAvatarState.address)
                    {
                        IsAutoCollect = false;
                        IsAutoSpend = false;
                        sweepStage = 0;
                    }
                }
                catch { }
            }


            //ui fill
            try
            {SelectedAvatarVFX.SetActive(currentAvatarState.address == States.Instance.CurrentAvatarState.address);}
            catch { SelectedAvatarVFX.SetActive(false); }

            AvatarNameText.text = currentAvatarState.NameWithHash + " : " + currentAvatarState.address.ToString();
            ProsperityImage.SetActive(IsAutoCollect);
            APImage.SetActive(IsAutoSpend);
            SweepStageText.text = "Sweep > " + sweepStage.ToString(); //change sweep method later
        }

        public void SetSlot(long _currentBlockIndex, int slotIndex, AvatarState state = null)
        {
            currentBlockIndex = _currentBlockIndex;
            _slotIndex = slotIndex;
            currentAvatarState = state;
            LoadSettings();
            UpdateInformation();
        }

        private void UpdateInformation()
        {
            //STAGE SECTION
            var prosperityBlocks = Mathf.Clamp(currentBlockIndex - currentAvatarState.dailyRewardReceivedIndex + 1,0,1700);
            int prosperityPercentage = (int)((prosperityBlocks / 1700f) * 100);
            ProsperityText.text = prosperityPercentage ==100? $"<color=red>{prosperityPercentage}</color>%": prosperityPercentage+"%";
            string actionString = currentAvatarState.actionPoint > 5 ? $"<color=red>{currentAvatarState.actionPoint}</color>": currentAvatarState.actionPoint.ToString();
            APText.text = actionString + "/120";
            bool stageNotification = IsStageNotify && (prosperityPercentage == 100 || currentAvatarState.actionPoint > 5);

            if (IsAutoCollect && prosperityBlocks >= 1700 && currentAvatarState.actionPoint < 5)
            {
                try //in case actionManager is not ready yet
                {
                    //prevent spam txs
                    if (collectProsperityCooldown < currentBlockIndex)
                        CollectAP();
                }catch { }
            }

            if (IsAutoSpend && sweepStage != 0 && currentAvatarState.actionPoint >= 5)
            {
                try //in case actionManager is not ready yet
                {
                    //prevent spam txs
                    if (spendActionPointsCooldown < currentBlockIndex)
                    {
                        //sweep
                        SweepLevel().Forget();
                    }
                }
                catch { }
            }

            //CRAFT SECTION
            bool craftNotification = false;


            //try //in case the slots is not ready yet!
            {
                
                if (craftSlotsUpdateCooldown < currentBlockIndex)
                    SetCombinationSlotStatesAsync().Forget();
                else
                    craftNotification = UpdateCraftingSlots() && IsCraftNotify;
            }

            //SLOT NOTIFICATION
            HasNotification = stageNotification || craftNotification;
            slotNotificationImage.enabled = HasNotification;
            stageNotificationImage.enabled = stageNotification;
            craftNotificationImage.enabled = craftNotification;
        }

        public async UniTaskVoid SweepLevel()
        {
            if (!Premium.CheckPremiumFeature())
                return;

            int worldID = 0;
            if (sweepStage < 51)
                worldID = 1;
            else if (sweepStage > 50 && sweepStage < 101)
                worldID = 2;
            else if (sweepStage > 100 && sweepStage < 151)
                worldID = 3;
            else if (sweepStage > 150 && sweepStage < 201)
                worldID = 4;
            else if (sweepStage > 200 && sweepStage < 251)
                worldID = 5;
            else if (sweepStage > 250 && sweepStage < 301)
                worldID = 6;

            try //in case actionManager is not ready yet
            {
                var (itemSlotStates, runeSlotStates) = await currentAvatarState.GetSlotStatesAsync();
                if (itemSlotStates.Count < 1)
                {
                    OneLineSystem.Push(MailType.System, $"<color=green>Pandora Box</color>: You need to Setup Equipments Builds!", NotificationCell.NotificationType.Alert);
                    return;
                }
                Game.Game.instance.ActionManager.HackAndSlashSweepOther(
                currentAvatarState,
                itemSlotStates[0].Costumes,
                itemSlotStates[0].Equipments,
                runeSlotStates[0].GetEquippedRuneSlotInfos(),
                0,
                currentAvatarState.actionPoint,
                worldID,
                sweepStage,
                currentAvatarState.actionPoint).Subscribe();
                spendActionPointsCooldown = currentBlockIndex +10;
                OneLineSystem.Push(MailType.System, $"<color=green>Pandora Box</color>: {currentAvatarState.NameWithHash} Sweep <color=green>{currentAvatarState.actionPoint}</color> AP for stage {sweepStage}!", NotificationCell.NotificationType.Information);
            }
            catch { }
        }

        public async UniTask SetCombinationSlotStatesAsync()
        {
            Combinationslotstates.Clear();
            for (var i = 0; i < currentAvatarState.combinationSlotAddresses.Count; i++)
            {
                var slotAddress = currentAvatarState.address.Derive(string.Format(CultureInfo.InvariantCulture,CombinationSlotState.DeriveFormat,i));
                var stateValue = await Game.Game.instance.Agent.GetStateAsync(slotAddress);
                Combinationslotstates.Add( new CombinationSlotState((Dictionary)stateValue));
            }
            craftSlotsUpdateCooldown = currentBlockIndex + 10;
        }

        bool UpdateCraftingSlots()
        {
            bool hasNotification=false;
            try
            {
                for (var i = 0; i < Combinationslotstates.Count; i++)
                {
                    var state = Combinationslotstates[i];
                    var maxValue = Math.Max(state.UnlockBlockIndex - state.StartBlockIndex, 1);
                    var diff = currentBlockIndex - state.StartBlockIndex;
                    var craftPercentage = (int)((diff * 1f / maxValue * 1f) * 100f);
                    if (!hasNotification && currentBlockIndex > state.UnlockBlockIndex)
                        hasNotification = true;
                    CraftingSlotsText[i].text = currentBlockIndex > state.UnlockBlockIndex ? $"{i + 1}] <color=red>-</color>" : $"{i + 1}]" + craftPercentage + "%";
                }
            }catch { }

            return hasNotification;
        }


        async void SwitchChar()
        {
            var loadingScreen = Widget.Find<GrayLoadingScreen>();
            loadingScreen.Show();
            var results = await RxProps.SelectAvatarAsync(_slotIndex);
            await WorldBossStates.Set(States.Instance.CurrentAvatarState.address);
            await States.Instance.InitRuneStoneBalance();
            await States.Instance.InitRuneStates();
            await States.Instance.InitRuneSlotStates();
            await States.Instance.InitItemSlotStates();
            loadingScreen.Close();
            Util.SaveAvatarSlotIndex(_slotIndex);
            Game.Event.OnRoomEnter.Invoke(false);
            Game.Event.OnUpdateAddresses.Invoke();
            Widget.Find<ChronoSlotsPopup>().Close();
            AudioController.PlayClick();
        }

        //private async void UpdateArena(AvatarState state, long currentBlockIndex)
        //{
        //    var currentAddress = state?.address;
        //    ArenaInfo arenaInfo = null;
        //    if (currentAddress.HasValue)
        //    {
        //        var avatarAddress2 = currentAddress.Value;
        //        var infoAddress = States.Instance.WeeklyArenaState.address.Derive(avatarAddress2.ToByteArray());
        //        var rawInfo = await Game.Game.instance.Agent.GetStateAsync(infoAddress);
        //        if (rawInfo is Dictionary dictionary)
        //        {
        //            arenaInfo = new ArenaInfo(dictionary);
        //        }
        //    }

        //    if (currentAddress == null || arenaInfo == null)
        //        return;
        //    //Debug.LogError(arenaInfo.AvatarName + "  " + arenaInfo.DailyChallengeCount);

        //    var gameConfigState = States.Instance.GameConfigState;
        //    float maxTime = States.Instance.GameConfigState.DailyArenaInterval;
        //    var weeklyArenaState = States.Instance.WeeklyArenaState;
        //    long _resetIndex = weeklyArenaState.ResetIndex;
        //    float value;

        //    value = currentBlockIndex - _resetIndex;
        //    var remainBlock = gameConfigState.DailyArenaInterval - value;
        //    var time = Util.GetBlockToTime((int)remainBlock);


        //    if (PandoraBoxMaster.Instance.Settings.BlockShowType == 0)
        //        ArenaText.text = "Arena : " + time + $" ({arenaInfo.DailyChallengeCount.ToString()}/5)";
        //    else if (PandoraBoxMaster.Instance.Settings.BlockShowType == 1)
        //        ArenaText.text = "Arena : " + $"({value}/{gameConfigState.DailyArenaInterval})" +
        //                         $" ({arenaInfo.DailyChallengeCount.ToString()}/5)";
        //    else
        //        ArenaText.text = "Arena : " + $"{time} ({remainBlock})" +
        //                         $" ({arenaInfo.DailyChallengeCount.ToString()}/5)";
        //}
    }
}
