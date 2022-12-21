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
    using Nekoyume.Model.Mail;
    using Nekoyume.PandoraBox;
    using Nekoyume.State.Subjects;
    using Nekoyume.UI.Module.WorldBoss;
    using Nekoyume.UI.Scroller;
    using System.Globalization;
    using UniRx;

    public class ChronoSlot : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI AvatarNameText;
        [SerializeField] private TextMeshProUGUI ProsperityText;
        [SerializeField] private GameObject ProsperityImage;
        [SerializeField] private TextMeshProUGUI APText;
        [SerializeField] private GameObject APImage;
        //[SerializeField] private TextMeshProUGUI AutoStageText;
        //[SerializeField] private TextMeshProUGUI ArenaText;
        [SerializeField] private TextMeshProUGUI[] CraftingSlotsText;
        public Image hasNotificationImage;
        [SerializeField] private Button AutoCollectButton;
        [SerializeField] private Button SwitchButton;
        private AvatarState currentAvatarState;
        private int _slotIndex;

        //settings
        bool IsAutoCollect;
        int AutoStage; //if its 0 then no auto sweep

        long currentBlockBeforeTry = 0;

        private void Awake()
        {
            AutoCollectButton.onClick.AddListener(() => { CollectAP(); });
            SwitchButton.onClick.AddListener(() => { SwitchChar(); });
        }

        private void OnEnable()
        {
            //GetState();
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
                if (!Premium.CurrentPandoraPlayer.IsPremium())
                {
                    OneLineSystem.Push(MailType.System,
                        "<color=green>Pandora Box</color>: This is Premium Feature!",
                        NotificationCell.NotificationType.Alert);
                    return;
                }
            }

            if (currentAvatarState.address == States.Instance.CurrentAvatarState.address)
                Game.Game.instance.ActionManager.DailyReward().Subscribe();
            else
                Game.Game.instance.ActionManager.DailyRewardPandora(currentAvatarState).Subscribe();

            var address = currentAvatarState.address;
            if (GameConfigStateSubject.ActionPointState.ContainsKey(address))
            {
                GameConfigStateSubject.ActionPointState.Remove(address);
            }
            GameConfigStateSubject.ActionPointState.Add(address, true);
        }

        void LoadSettings()
        {
            string addressKey = "_PandoraBox_Chrono_" + currentAvatarState.address;
            if (PlayerPrefs.HasKey(addressKey))
            {
                IsAutoCollect = System.Convert.ToBoolean(PlayerPrefs.GetInt(addressKey + "_IsAutoCollect", 1));
                AutoStage = PlayerPrefs.GetInt(addressKey + "_AutoStage", 0);
            }
            else
            {
                //register variables
                IsAutoCollect = true;
                AutoStage = 0;
            }

            //check for premium
            if (!Premium.CurrentPandoraPlayer.IsPremium())
            {
                if (currentAvatarState.address != States.Instance.CurrentAvatarState.address)
                {
                    IsAutoCollect = false;
                    AutoStage = 0;
                }
            }


            //ui fill
            AvatarNameText.text = currentAvatarState.NameWithHash + " | " + currentAvatarState.address.ToString();
            ProsperityImage.SetActive(IsAutoCollect);
            APImage.SetActive(AutoStage != 0);
            //AutoCollectText.text = IsAutoCollect ? "<color=green>TRUE" : "<color=red>FALSE";
            //AutoStageText.text = AutoStage !=0 ? AutoStage.ToString() : "-";
            AutoCollectButton.interactable = !IsAutoCollect;
            AutoCollectButton.GetComponentInChildren<TextMeshProUGUI>().text = IsAutoCollect ? "Auto" : "Collect";
        }

        public void SetSlot(long currentBlockIndex, int slotIndex, AvatarState state = null)
        {
            _slotIndex = slotIndex;
            currentAvatarState = state;
            LoadSettings();
            UpdateInformation(currentBlockIndex);
        }

        private void UpdateInformation(long currentBlockIndex)
        {
            var blockCount = Mathf.Clamp(Game.Game.instance.Agent.BlockIndex - currentAvatarState.dailyRewardReceivedIndex + 1,0,1700);
            ProsperityText.text = (int)((blockCount / 1700f) * 100) + "%";
            string actionString = currentAvatarState.actionPoint > 5 ? $"<color=red>{currentAvatarState.actionPoint}</color>": currentAvatarState.actionPoint.ToString();
            //string apString = blockCount >= 1700 ? $"<color=red>{blockCount}</color>" : blockCount.ToString();
            APText.text = actionString + "/120";

            if (IsAutoCollect && blockCount >= 1700 && currentAvatarState.actionPoint < 5)
            {
                try //in case actionManager is not ready yet
                {
                    //prevent spam txs
                    if (currentBlockBeforeTry + 4 < Game.Game.instance.Agent.BlockIndex)
                    {
                        currentBlockBeforeTry = currentBlockIndex;
                        //Debug.LogError($"[{currentBlockIndex}]Chrono: {state.address} , {blockCount} , {state.actionPoint}, Colledted!");
                        OneLineSystem.Push(MailType.System, $"<color=green>Pandora Box</color>: Prosperity bar for {currentAvatarState.NameWithHash} Auto collected!", NotificationCell.NotificationType.Information);
                        CollectAP();
                    }
                }
                catch{ }
            }

            //UpdateArena(state, currentBlockIndex);
            UpdateNotification((int)blockCount);

            //craft
            SetCombinationSlotStatesAsync(currentBlockIndex).Forget();
            //var states = States.Instance.GetCombinationSlotState(currentAvatarState, currentBlockIndex);
            //for (var i = 0; i < 4; i++)
            //{
            //    if (states.ContainsKey(i))
            //    {
            //        if (states.TryGetValue(i, out var state))
            //        {
            //            var diff = Math.Max(state.UnlockBlockIndex - currentBlockIndex, 1);
            //            CraftingSlotsText[i].text = diff.ToString();
            //        }
            //        else
            //        {
            //            //slots[i].SetSlot(avatarState.address, blockIndex, i);
            //            CraftingSlotsText[i].text = "?";
            //            var diff = Math.Max(state.UnlockBlockIndex - currentBlockIndex, 1);
            //            CraftingSlotsText[i].text = diff.ToString();
            //        }
            //    }
            //    else
            //    {
            //        CraftingSlotsText[i].text = "??";
            //        //slots[i].SetSlot(avatarState.address, blockIndex, i);
            //    }
            //}
        }

        public async UniTask SetCombinationSlotStatesAsync(long currentBlockIndex)
        {
            for (var i = 0; i < currentAvatarState.combinationSlotAddresses.Count; i++)
            {
                var slotAddress = currentAvatarState.address.Derive(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        CombinationSlotState.DeriveFormat,
                        i
                    )
                );
                var stateValue = await Game.Game.instance.Agent.GetStateAsync(slotAddress);
                var state = new CombinationSlotState((Dictionary)stateValue);
                //UpdateCombinationSlotState(avatarState.address, i, state);
                var maxValue = Math.Max(state.UnlockBlockIndex - state.StartBlockIndex, 1);
                var diff = currentBlockIndex - state.StartBlockIndex;
                //CraftingSlotsText[i].text = $"[{i}]" + (int)((diff / maxValue) * 100) + "%";
                if (currentBlockIndex > state.UnlockBlockIndex)
                    CraftingSlotsText[i].text = $"[{i + 1}] -";
                else
                    CraftingSlotsText[i].text = $"[{i+1}]" + (int)((diff * 1f / maxValue * 1f) * 100f) + "%";
            }
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
        }

        private void UpdateNotification(int blockCount)
        {
            hasNotificationImage.enabled = currentAvatarState.actionPoint > 0 || blockCount >=1700;
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

        //    value = Game.Game.instance.Agent.BlockIndex - _resetIndex;
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
