using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.Model.State;
using Nekoyume.State;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

namespace Nekoyume.UI.Module
{
    using Bencodex.Types;
    using Nekoyume.Extensions;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.Event;
    using Nekoyume.Model.Mail;
    using Nekoyume.PandoraBox;
    using Nekoyume.State.Subjects;
    using Nekoyume.TableData;
    using Nekoyume.TableData.Event;
    using Nekoyume.UI.Module.Common;
    using Nekoyume.UI.Module.WorldBoss;
    using Nekoyume.UI.Scroller;
    using System.Globalization;
    using System.Threading.Tasks;
    using UniRx;

    public class ChronoSlot : MonoBehaviour
    {
        [Header("GENERAL FIELDS")] [SerializeField]
        private GameObject SelectedAvatarVFX;

        [SerializeField] private GameObject DataLoading;

        [SerializeField] private TextMeshProUGUI AccountAddressText;
        [SerializeField] private TextMeshProUGUI AvatarNameText;
        [SerializeField] private GameObject slotNotificationImage;
        [SerializeField] private Button SwitchButton;
        [SerializeField] private Button SettingsButton;
        [SerializeField] private Button CheckNowButton;

        public bool IsBusy;
        bool IsPrefsLoded;
        public bool HasNotification;
        AvatarState currentAvatarState;
        int _slotIndex;
        long currentBlockIndex;
        ChronoAvatarSetting currentChronoAvatarSetting;

        int updateAvatarInterval = 90; //blocks to periodly update avatar
        int updateCraftInterval = 200; //blocks to periodly update craft slots
        int updateEventInterval = 200; //blocks to periodly update event EventDungeonInfo
        int updateBossInterval = 200; //blocks to periodly update boss
        int urgentUpdateInterval = 15; //blocks to update if there is an action (auto collect,craft ...)

        [Space(5)] [Header("STAGE")] [SerializeField]
        private GameObject stageModule;

        [SerializeField] private GameObject stageNotificationImage;
        [SerializeField] private TextMeshProUGUI ProsperityText;
        [SerializeField] private GameObject ProsperityImage;
        [SerializeField] private TextMeshProUGUI APText;
        [SerializeField] private GameObject APImage;
        [SerializeField] private TextMeshProUGUI SweepStageText;
        [SerializeField] private TextMeshProUGUI stageCooldownText;
        [SerializeField] private Image stageCooldownImage;

        public long stageCooldown;

        [Space(5)] [Header("CRAFT")] [SerializeField]
        private GameObject craftModule;

        [SerializeField] private GameObject craftNotificationImage;
        [SerializeField] private Image isCraftedImage;
        [SerializeField] private GameObject premiumSign;
        [SerializeField] private TextMeshProUGUI[] CraftingSlotsText;
        [SerializeField] private TextMeshProUGUI craftCooldownText;
        [SerializeField] private Image craftCooldownImage;

        List<CombinationSlotState> Combinationslotstates = new List<CombinationSlotState>();
        long craftCooldown;

        [Space(5)] [Header("EVENT")] [SerializeField]
        private GameObject eventModule;

        [SerializeField] private GameObject eventNotificationImage;
        [SerializeField] private TextMeshProUGUI eventTicketsText;
        [SerializeField] private TextMeshProUGUI eventLevelText;
        [SerializeField] private TextMeshProUGUI eventRemainsText;
        [SerializeField] private TextMeshProUGUI eventCooldownText;
        [SerializeField] private Image eventCooldownImage;

        long eventCooldown;
        Address eventAddress;
        EventDungeonInfo eventDungeonInfo;

        [Space(5)] [Header("BOSS")] [SerializeField]
        private GameObject bossModule;

        [SerializeField] private GameObject bossNotificationImage;
        [SerializeField] private TextMeshProUGUI bossTicketsText;
        [SerializeField] private TextMeshProUGUI bossRemainsText;
        [SerializeField] private TextMeshProUGUI bossCooldownText;
        [SerializeField] private Image bossCooldownImage;

        long bossCooldown;
        RaiderState raider;
        WorldBossListSheet.Row raidRow;

        private void Awake()
        {
            SwitchButton.onClick.AddListener(() => { SwitchChar(); });
            SettingsButton.onClick.AddListener(() => { OpenSlotSettings(); });
            CheckNowButton.onClick.AddListener(() => { CheckNowSlot().Forget(); });
        }

        void OpenSlotSettings()
        {
            Widget.Find<ChronoSettingsPopup>().Show(currentAvatarState, _slotIndex);
        }

        public async UniTask CheckNowSlot()
        {
            DataLoading.SetActive(true);
            currentAvatarState = await UpdateAvatar();
            while (currentAvatarState is null)
                await Task.Delay(1000);
            DataLoading.SetActive(false);

            await GetCombinationSlotStates();
            IsPrefsLoded = false;
            IsBusy = false;
            LoadSettings();
            UpdateInformation();
        }

        public void SetSlot(long _currentBlockIndex, int slotIndex, AvatarState state)
        {
            SelectedAvatarVFX.SetActive(false);
            currentBlockIndex = _currentBlockIndex;
            _slotIndex = slotIndex;
            currentAvatarState = state;
            LoadSettings();
            UpdateInformation();
        }


        async void LoadSettings()
        {
            if (IsPrefsLoded)
                return;

            while (currentAvatarState is null)
                await Task.Delay(1000);

            IsPrefsLoded = true; //to read it once
            string addressKey = "_PandoraBox_Chrono_" + currentAvatarState.address;

            var settings = new ChronoAvatarSettings();
            settings.LoadSettings();
            currentChronoAvatarSetting = settings.GetSettings(currentAvatarState.address.ToString());

            //those settings will set only on game start or setting changes once
            //general
            AvatarNameText.text =
                $"#{(_slotIndex + 1)} Lv.{currentAvatarState.level} {currentAvatarState.NameWithHash} : {currentAvatarState.address}";
            AccountAddressText.text = "";
            if (currentAvatarState.agentAddress != States.Instance.AgentState.address)
            {
                AccountAddressText.text = currentAvatarState.agentAddress.ToString();
                var result = await GetAgentBalance();
                AvatarNameText.text += result;
            }

            //stage
            stageModule.SetActive(currentChronoAvatarSetting.Stage);
            if (currentChronoAvatarSetting.Stage)
            {
                ProsperityImage.SetActive(currentChronoAvatarSetting.StageIsAutoCollectProsperity);
                APImage.SetActive(currentChronoAvatarSetting.StageIsAutoSpendProsperity);
                if (currentChronoAvatarSetting.StageIsSweepAP)
                    SweepStageText.text = "Sweep > " + currentChronoAvatarSetting.StageSweepLevelIndex;
                else
                {
                    int _stageId = 1;
                    currentAvatarState.worldInformation.TryGetLastClearedStageId(out _stageId);
                    _stageId = Math.Clamp(_stageId + 1, 1, 300);

                    SweepStageText.text = "Progress > " + _stageId;
                }
            }

            //craft
            craftModule.SetActive(currentChronoAvatarSetting.Craft);
            if (currentChronoAvatarSetting.Craft)
            {
                var tableSheets = Game.TableSheets.Instance;
                premiumSign.SetActive(currentChronoAvatarSetting.CraftIsPremium);
                isCraftedImage.gameObject.SetActive(currentChronoAvatarSetting.CraftIsAutoCombine);
                if (currentChronoAvatarSetting.CraftIsAutoCombine)
                {
                    if (tableSheets.EquipmentItemRecipeSheet.TryGetValue(currentChronoAvatarSetting.CraftItemID,
                            out var equipRow))
                        isCraftedImage.sprite = SpriteHelper.GetItemIcon(equipRow.ResultEquipmentId);
                    else if (tableSheets.ConsumableItemRecipeSheet.TryGetValue(currentChronoAvatarSetting.CraftItemID,
                                 out var consumableRow))
                        isCraftedImage.sprite = SpriteHelper.GetItemIcon(consumableRow.ResultConsumableItemId);
                    else if (tableSheets.EventConsumableItemRecipeSheet.TryGetValue(
                                 currentChronoAvatarSetting.CraftItemID, out var eventConsumableRow))
                        isCraftedImage.sprite = SpriteHelper.GetItemIcon(eventConsumableRow.ResultConsumableItemId);
                }
            }

            //event
            eventModule.SetActive(currentChronoAvatarSetting.Event);
            if (currentChronoAvatarSetting.Event)
            {
                var tableSheets = Game.TableSheets.Instance;
                if (!tableSheets.EventScheduleSheet.TryGetRowForDungeon(currentBlockIndex, out var scheduleRow) ||
                    scheduleRow.DungeonEndBlockIndex == currentBlockIndex)
                {
                    try
                    {
                        eventAddress = Nekoyume.Model.Event.EventDungeonInfo.DeriveAddress(currentAvatarState.address,
                            RxProps.EventDungeonRow.Id);
                    }
                    catch (Exception ex)
                    {
                        PandoraUtil.ShowSystemNotification("ChronoSlot/LoadSettings > " + ex.Message,
                            NotificationCell.NotificationType.Alert);
                    }
                }
                else
                {
                    OneLineSystem.Push(MailType.System, $"<color=green>Pandora Box</color>: Cannot read Event Data!",
                        NotificationCell.NotificationType.Alert);
                }
            }

            //Boss
            bossModule.SetActive(currentChronoAvatarSetting.Boss);


            //update States
            stageCooldown = 0;
            craftCooldown = 0;
            eventCooldown = 0;
            bossCooldown = 0;
        }


        async void UpdateInformation()
        {
            if (States.Instance.CurrentAvatarState is null || IsBusy)
                return;

            IsBusy = true;
            SelectedAvatarVFX.SetActive(currentAvatarState.address == States.Instance.CurrentAvatarState.address);


            //STAGE SECTION
            bool stageNotification = await UpdateStageSection();

            //CRAFT SECTION
            bool craftNotification = await UpdateCraftSection();


            //Event
            bool eventNotification = false;
            if (currentChronoAvatarSetting.Event)
            {
                if (!(eventDungeonInfo is null))
                {
                    var current =
                        eventDungeonInfo.GetRemainingTicketsConsiderReset(RxProps.EventScheduleRowForDungeon.Value,
                            currentBlockIndex);
                    var resetIntervalBlockRange =
                        RxProps.EventScheduleRowForDungeon.Value.DungeonTicketsResetIntervalBlockRange;
                    var progressedBlockRange =
                        (currentBlockIndex - RxProps.EventScheduleRowForDungeon.Value.StartBlockIndex) %
                        resetIntervalBlockRange;
                    var blocksRemains = resetIntervalBlockRange - progressedBlockRange;

                    //urgent upgrade if there is any tickets
                    if (currentChronoAvatarSetting.EventIsAutoSpendTickets && current > 0 &&
                        eventCooldown > currentBlockIndex + urgentUpdateInterval)
                        eventCooldown = currentBlockIndex + urgentUpdateInterval;

                    string value = "";
                    switch (PandoraMaster.Instance.Settings.BlockShowType)
                    {
                        case 1:
                        {
                            value = blocksRemains.ToString();
                            break;
                        }
                        default:
                        {
                            value = Util.GetBlockToTime(blocksRemains);
                            break;
                        }
                    }

                    eventRemainsText.text = value;
                    eventNotification = currentChronoAvatarSetting.EventNotification && current > 0;
                    eventTicketsText.text = current.ToString();
                    eventLevelText.text = currentChronoAvatarSetting.EventLevelIndex.ToString();

                    var cooldownBar = Mathf.Clamp(updateEventInterval - (eventCooldown - currentBlockIndex), 1,
                        updateEventInterval);
                    eventCooldownText.text = cooldownBar.ToString();
                    eventCooldownImage.fillAmount = (updateEventInterval - cooldownBar) * 1f / updateEventInterval;
                }

                if (eventCooldown < currentBlockIndex)
                    await SetEventDungeon();
            }

            //BOSS SECTION
            bool bossNotification = false;
            if (currentChronoAvatarSetting.Boss)
            {
                if (!(raider is null) && !(raidRow is null))
                {
                    var RemainTicket = WorldBossFrontHelper.GetRemainTicket(raider, currentBlockIndex);
                    bossTicketsText.text = RemainTicket.ToString();
                    bossNotification =
                        RemainTicket > 0 && currentChronoAvatarSetting.BossNotification;
                    var start = raidRow.StartedBlockIndex;
                    var reminder = (currentBlockIndex - start) % WorldBossHelper.RefillInterval;
                    var remain = WorldBossHelper.RefillInterval - reminder;
                    string value = "";
                    switch (PandoraMaster.Instance.Settings.BlockShowType)
                    {
                        case 1:
                        {
                            value = remain.ToString();
                            break;
                        }
                        default:
                        {
                            value = Util.GetBlockToTime(remain);
                            break;
                        }
                    }

                    bossRemainsText.text = value;

                    //urgent upgrade if there is any tickets
                    if (currentChronoAvatarSetting.BosstIsAutoSpendTickets && RemainTicket > 0 &&
                        bossCooldown > currentBlockIndex + urgentUpdateInterval)
                        bossCooldown = currentBlockIndex + urgentUpdateInterval;
                }

                var cooldownBar = Mathf.Clamp(updateBossInterval - (bossCooldown - currentBlockIndex), 1,
                    updateBossInterval);
                bossCooldownText.text = cooldownBar.ToString();
                bossCooldownImage.fillAmount = (updateBossInterval - cooldownBar) * 1f / updateBossInterval;

                if (bossCooldown < currentBlockIndex)
                {
                    DataLoading.SetActive(true);
                    currentAvatarState = await UpdateAvatar();
                    while (currentAvatarState is null)
                        await Task.Delay(1000);
                    DataLoading.SetActive(false);
                    await SetBossRaidData();
                }
            }


            //SLOT NOTIFICATION
            HasNotification = stageNotification || craftNotification || eventNotification || bossNotification;
            slotNotificationImage.SetActive(HasNotification);
            stageNotificationImage.SetActive(stageNotification);
            craftNotificationImage.SetActive(craftNotification);
            eventNotificationImage.SetActive(eventNotification);
            bossNotificationImage.SetActive(bossNotification);

            IsBusy = false;
        }

        async UniTask<bool> UpdateStageSection()
        {
            if (!currentChronoAvatarSetting.Stage) return false;

            var prosperityBlocks =
                Mathf.Clamp(currentBlockIndex - currentAvatarState.dailyRewardReceivedIndex + 1, 0, 1700);
            bool isFull = prosperityBlocks == 1700;

            //force collect Prosperity
            if (currentChronoAvatarSetting.StageIsAutoCollectProsperity && isFull &&
                stageCooldown > currentBlockIndex + urgentUpdateInterval)
                stageCooldown = currentBlockIndex + urgentUpdateInterval;

            string value = PandoraMaster.Instance.Settings.BlockShowType == 1
                ? ((long)(1700 - prosperityBlocks)).ToString()
                : Util.GetBlockToTime((long)(1700 - prosperityBlocks));
            ProsperityText.text = isFull ? $"<color=red>FULL</color>" : value;
            APText.text = currentAvatarState.actionPoint > 5
                ? $"<color=red>{currentAvatarState.actionPoint}</color>"
                : currentAvatarState.actionPoint.ToString();

            //force spend AP
            if (currentChronoAvatarSetting.StageIsAutoSpendProsperity && currentAvatarState.actionPoint > 5 &&
                stageCooldown > currentBlockIndex + urgentUpdateInterval)
                stageCooldown = currentBlockIndex + urgentUpdateInterval;

            var cooldownBar = Mathf.Clamp(updateAvatarInterval - (stageCooldown - currentBlockIndex), 1,
                updateAvatarInterval);
            stageCooldownText.text = cooldownBar.ToString();
            stageCooldownImage.fillAmount = (updateAvatarInterval - cooldownBar) * 1f / updateAvatarInterval;

            if (stageCooldown <= currentBlockIndex) await DoStageActions();

            return currentChronoAvatarSetting.StageNotification && (isFull || currentAvatarState.actionPoint > 5);
        }

        async UniTask<bool> UpdateCraftSection()
        {
            if (!currentChronoAvatarSetting.Craft) return false;

            if (Combinationslotstates.Count == 0)
                await GetCombinationSlotStates();

            while (Combinationslotstates.Count != 4)
                await Task.Delay(1000);

            bool isEmptySlot = false;
            try
            {
                for (var i = 0; i < Combinationslotstates.Count; i++)
                {
                    var state = Combinationslotstates[i];
                    var maxValue = Math.Max(state.UnlockBlockIndex - state.StartBlockIndex, 1);
                    var diff = currentBlockIndex - state.StartBlockIndex;
                    if (currentBlockIndex >= state.UnlockBlockIndex)
                    {
                        if (!isEmptySlot)
                            isEmptySlot = true;
                        CraftingSlotsText[i].text = $"<color=red>-</color>";

                        //urgent upgrade
                        if (currentChronoAvatarSetting.CraftIsAutoCombine &&
                            craftCooldown > currentBlockIndex + urgentUpdateInterval)
                            craftCooldown = currentBlockIndex + urgentUpdateInterval;
                    }
                    else
                    {
                        string value = "";
                        switch (PandoraMaster.Instance.Settings.BlockShowType)
                        {
                            case 1:
                            {
                                value = (maxValue - diff).ToString();
                                break;
                            }
                            default:
                            {
                                value = Util.GetBlockToTime(maxValue - diff);
                                break;
                            }
                        }

                        CraftingSlotsText[i].text = value;
                    }
                }
            }
            catch (Exception ex)
            {
                PandoraUtil.ShowSystemNotification("ChronoSlot/IsAvailableCraftingSlots > " + ex.Message,
                    NotificationCell.NotificationType.Alert);
                return false;
            }

            var cooldownBar = Mathf.Clamp(updateCraftInterval - (craftCooldown - currentBlockIndex), 1,
                updateCraftInterval);
            craftCooldownText.text = cooldownBar.ToString();
            craftCooldownImage.fillAmount = (updateCraftInterval - cooldownBar) * 1f / updateCraftInterval;


            if (craftCooldown <= currentBlockIndex) await SetCombinationSlotStatesAsync();

            return isEmptySlot && currentChronoAvatarSetting.CraftNotification;
        }

        async UniTask SetBossRaidData()
        {
            if (currentChronoAvatarSetting.Boss)
            {
                var bossSheet = Game.Game.instance.TableSheets.WorldBossListSheet;

                //check rewards
                if (currentChronoAvatarSetting.BosstIsAutoCollectRewards &&
                    WorldBossStates.IsReceivingGradeRewards(currentAvatarState.address))
                {
                    WorldBossStates.SetReceivingGradeRewards(currentAvatarState.address, true);
                    WorldBossStates.SetHasGradeRewards(currentAvatarState.address, false);

                    var action = new ClaimRaidReward(currentAvatarState.address);
                    BlockChain.ActionManager.Instance.PreProcessAction(action, currentAvatarState);
                    PandoraUtil.ShowSystemNotification(
                        $"Raid Rewards request for <color=red>{currentAvatarState.name}</color> sent Successfully!",
                        NotificationCell.NotificationType.Notification);
                    Debug.LogError(
                        $"Raid Rewards request for <color=red>{currentAvatarState.name}</color> sent Successfully!");
                }


                try
                {
                    raidRow = bossSheet.FindRowByBlockIndex(currentBlockIndex);
                    if (raidRow is null)
                        return;

                    var raiderAddress = Addresses.GetRaiderAddress(currentAvatarState.address, raidRow.Id);
                    var raiderState = await Game.Game.instance.Agent.GetStateAsync(raiderAddress);
                    raider = raiderState is Bencodex.Types.List raiderList ? new RaiderState(raiderList) : null;
                }
                catch (Exception ex)
                {
                    PandoraUtil.ShowSystemNotification("ChronoSlot/SetBossRaidData > " + ex.Message,
                        NotificationCell.NotificationType.Alert);
                    return;
                }

                bossCooldown = currentBlockIndex + updateBossInterval;

                //send fights
                if (currentChronoAvatarSetting.BosstIsAutoSpendTickets &&
                    WorldBossFrontHelper.GetRemainTicket(raider, currentBlockIndex) > 0)
                {
                    await Premium.PVE_AutoWorldBoss(currentAvatarState, raider);
                    bossCooldown = currentBlockIndex + urgentUpdateInterval;
                }
            }
        }

        async UniTask<string> GetAgentBalance()
        {
            var ncgCurrency =
                Libplanet.Assets.Currency.Legacy("NCG", 2, new Address("47d082a115c63e7b58b1532d20e631538eafadde"));
            var crystalCurrency = Libplanet.Assets.Currency.Legacy("CRYSTAL", 18, minters: null);
            var goldbalance =
                await Game.Game.instance.Agent.GetBalanceAsync(currentAvatarState.agentAddress, ncgCurrency);
            var crystalbalance =
                await Game.Game.instance.Agent.GetBalanceAsync(currentAvatarState.agentAddress, crystalCurrency);
            return
                $", N:{PandoraUtil.ToLongNumberNotation(goldbalance.MajorUnit)} C:{PandoraUtil.ToLongNumberNotation(crystalbalance.MajorUnit)}";
        }

        async UniTask<AvatarState> UpdateAvatar()
        {
            return await States.Instance.AddOrReplaceAvatarStateAsync(currentAvatarState.address, _slotIndex);
        }

        async UniTask DoStageActions()
        {
            stageCooldown = currentBlockIndex + updateAvatarInterval;
            DataLoading.SetActive(true);
            currentAvatarState = await UpdateAvatar();
            while (currentAvatarState is null)
                await Task.Delay(1000);
            DataLoading.SetActive(false);

            //check for prosperity
            var prosperityBlocks =
                Mathf.Clamp(currentBlockIndex - currentAvatarState.dailyRewardReceivedIndex + 1, 0, 1700);
            if (currentAvatarState.actionPoint >= 5 &&
                currentChronoAvatarSetting.StageIsAutoSpendProsperity)
            {
                //check for sweep, we used else if to not doing twice 
                if (currentChronoAvatarSetting.StageIsSweepAP &&
                    currentChronoAvatarSetting.StageSweepLevelIndex != 0)
                {
                    try //in case actionManager is not ready yet
                    {
                        var result = await Premium.PVE_AutoStageSweep(currentAvatarState,
                            currentChronoAvatarSetting.StageSweepLevelIndex);
                        if (string.IsNullOrEmpty(result))
                            PandoraUtil.ShowSystemNotification(
                                $"{currentAvatarState.NameWithHash} Sweep <color=green>{currentAvatarState.actionPoint}</color> AP for stage {currentChronoAvatarSetting.StageSweepLevelIndex}!"
                                , NotificationCell.NotificationType.Notification);
                        else
                            PandoraUtil.ShowSystemNotification(
                                $"{currentAvatarState.NameWithHash} <b>Failed</b> to send Sweep because {result}"
                                , NotificationCell.NotificationType.Alert);
                        stageCooldown = currentBlockIndex + urgentUpdateInterval;
                    }
                    catch (Exception ex)
                    {
                        PandoraUtil.ShowSystemNotification("ChronoSlot/UpdateStage > " + ex.Message,
                            NotificationCell.NotificationType.Alert);
                        return;
                    }
                }
                //check for repeat, we used else if to not doing twice 
                else if (!currentChronoAvatarSetting.StageIsSweepAP)
                {
                    try //in case actionManager is not ready yet
                    {
                        var result = await Premium.PVE_AutoStageRepeat(currentAvatarState);
                        if (string.IsNullOrEmpty(result))
                        {
                            int _stageId = 1;
                            currentAvatarState.worldInformation.TryGetLastClearedStageId(out _stageId);
                            _stageId = Math.Clamp(_stageId + 1, 1, 300);
                            PandoraUtil.ShowSystemNotification(
                                $"{currentAvatarState.NameWithHash} Repeat <color=green>{currentAvatarState.actionPoint}</color> AP for stage {_stageId}!",
                                NotificationCell.NotificationType.Notification);
                        }
                        else
                            PandoraUtil.ShowSystemNotification(
                                $"{currentAvatarState.NameWithHash} <b>Failed</b> to send Repeat because {result}",
                                NotificationCell.NotificationType.Alert);

                        stageCooldown = currentBlockIndex + urgentUpdateInterval;
                    }
                    catch (Exception ex)
                    {
                        PandoraUtil.ShowSystemNotification("ChronoSlot/UpdateStage > " + ex.Message,
                            NotificationCell.NotificationType.Alert);
                        return;
                    }
                }
            }
            else
            {
                if (currentChronoAvatarSetting.StageIsAutoCollectProsperity &&
                    prosperityBlocks >= 1700 && currentAvatarState.actionPoint < 5)
                {
                    try //in case actionManager is not ready yet
                    {
                        CollectAP();
                        stageCooldown = currentBlockIndex + urgentUpdateInterval;
                    }
                    catch (Exception ex)
                    {
                        PandoraUtil.ShowSystemNotification("ChronoSlot/UpdateStage > " + ex.Message,
                            NotificationCell.NotificationType.Alert);
                    }
                }
            }
        }

        public void CollectAP()
        {
            if (currentAvatarState.address != States.Instance.CurrentAvatarState.address)
                if (!Premium.PandoraProfile.IsPremium())
                    return;

            if (currentAvatarState.actionPoint > 5)
            {
                OneLineSystem.Push(MailType.System, $"<color=green>Pandora Box</color>: There is still Action Points!",
                    NotificationCell.NotificationType.Information);
                return;
            }

            if (currentAvatarState.address == States.Instance.CurrentAvatarState.address)
                Game.Game.instance.ActionManager.DailyReward().Subscribe();
            else
                Game.Game.instance.ActionManager.DailyRewardPandora(currentAvatarState.address);

            OneLineSystem.Push(MailType.System,
                $"<color=green>Pandora Box</color>: Prosperity bar for {currentAvatarState.NameWithHash} Auto collected!",
                NotificationCell.NotificationType.Information);
        }

        public async UniTask SetEventDungeon()
        {
            try
            {
                eventDungeonInfo = await Game.Game.instance.Agent.GetStateAsync(eventAddress)
                    is Bencodex.Types.List serialized
                    ? new EventDungeonInfo(serialized)
                    : null;
                if (eventDungeonInfo is null)
                    return;
            }
            catch (Exception ex)
            {
                PandoraUtil.ShowSystemNotification("ChronoSlot/SetEventDungeon > " + ex.Message,
                    NotificationCell.NotificationType.Alert);
                return;
            }


            eventCooldown = currentBlockIndex + updateEventInterval;
            var ticketsCount = eventDungeonInfo.GetRemainingTicketsConsiderReset(
                RxProps.EventScheduleRowForDungeon.Value, currentBlockIndex);
            if (ticketsCount > 0 && currentChronoAvatarSetting.EventIsAutoSpendTickets)
            {
                int realLevelID = 0; //10030001 - 10030020 winter event
                if (currentChronoAvatarSetting.EventLevelIndex.ToString().Length == 1)
                    realLevelID = int.Parse("1003000" + currentChronoAvatarSetting.EventLevelIndex.ToString());
                else
                    realLevelID = int.Parse("100300" + currentChronoAvatarSetting.EventLevelIndex.ToString());

                await Premium.PVE_AutoEventDungeon(currentAvatarState, realLevelID, ticketsCount);
                eventCooldown = currentBlockIndex + urgentUpdateInterval;
            }
        }

        async UniTask GetCombinationSlotStates()
        {
            Combinationslotstates.Clear();
            while (currentAvatarState is null)
                await Task.Delay(1000);
            for (var i = 0; i < currentAvatarState.combinationSlotAddresses.Count; i++)
            {
                var slotAddress = currentAvatarState.address.Derive(string.Format(CultureInfo.InvariantCulture,
                    CombinationSlotState.DeriveFormat, i));
                var stateValue = await Game.Game.instance.Agent.GetStateAsync(slotAddress);
                Combinationslotstates.Add(new CombinationSlotState((Dictionary)stateValue));
            }
        }

        public async UniTask SetCombinationSlotStatesAsync()
        {
            craftCooldown = currentBlockIndex + updateCraftInterval;
            await GetCombinationSlotStates();
            var tableSheets = Game.TableSheets.Instance;

            for (int i = 0; i < Combinationslotstates.Count; i++)
            {
                var combinationSlotAddress = Combinationslotstates[i];
                if (!currentChronoAvatarSetting.CraftIsAutoCombine ||
                    currentBlockIndex <= combinationSlotAddress.UnlockBlockIndex)
                    continue;

                craftCooldown = currentBlockIndex + urgentUpdateInterval;

                //update the avatar to get last material inventory
                DataLoading.SetActive(true);
                currentAvatarState = await UpdateAvatar();
                while (currentAvatarState is null)
                    await Task.Delay(1000);
                DataLoading.SetActive(false);


                //check if its consumable or equipment
                if (tableSheets.EquipmentItemRecipeSheet.TryGetValue(currentChronoAvatarSetting.CraftItemID,
                        out var equipRow))
                {
                    if (equipRow != null)
                    {
                        await Premium.CRAFT_AutoCraftEquipment(currentAvatarState, i, equipRow,
                            currentChronoAvatarSetting.CraftIsUseCrystal,
                            currentChronoAvatarSetting.CraftIsPremium, _slotIndex);
                    }

                    break; //enforce break to wait new craft count the hammer points
                }
                else if (tableSheets.ConsumableItemRecipeSheet.TryGetValue(currentChronoAvatarSetting.CraftItemID,
                             out var consumableRow))
                {
                    if (consumableRow != null)
                    {
                        Premium.CRAFT_AutoCraftConsumable(currentAvatarState, i, consumableRow);
                    }
                }
                else if (tableSheets.EventConsumableItemRecipeSheet.TryGetValue(currentChronoAvatarSetting.CraftItemID,
                             out var eventConsumableRow))
                {
                    if (eventConsumableRow != null)
                    {
                        Premium.CRAFT_AutoCraftEventConsumable(currentAvatarState, i, eventConsumableRow);
                    }
                }
            }
        }


        //public async UniTask SetCombinationSlotStatesAsync()
        //{
        //    await CheckNowSlot();
        //    craftCooldown = currentBlockIndex + updateCraftInterval;
        //    for (var i = 0; i < currentAvatarState.combinationSlotAddresses.Count; i++)
        //    {
        //        if (currentBlockIndex > Combinationslotstates[i].UnlockBlockIndex &&
        //            Convert.ToBoolean(currentChronoAvatarSetting.CraftIsAutoCombine))
        //        {
        //            //update the avatar to get last material inventory
        //            await UpdateAvatar();
        //            craftCooldown = currentBlockIndex + urgentUpdateInterval;

        //            //check if its consumable or equipment
        //            var tableSheets = Game.TableSheets.Instance;
        //            if (tableSheets.EquipmentItemRecipeSheet.TryGetValue(currentChronoAvatarSetting.CraftItemID,
        //                    out var equipRow))
        //            {
        //                if (equipRow != null)
        //                {
        //                    await Premium.CRAFT_AutoCraftEquipment(currentAvatarState, i, equipRow,
        //                        Convert.ToBoolean(currentChronoAvatarSetting.CraftIsUseCrystal),
        //                        Convert.ToBoolean(currentChronoAvatarSetting.CraftIsPremium), _slotIndex);
        //                }

        //                break; //enforce break to wait new craft count the hammer points
        //            }
        //            else if (tableSheets.ConsumableItemRecipeSheet.TryGetValue(currentChronoAvatarSetting.CraftItemID,
        //                         out var consumableRow))
        //            {
        //                if (consumableRow != null)
        //                    Premium.CRAFT_AutoCraftConsumable(currentAvatarState, i, consumableRow);
        //            }
        //            else if (tableSheets.EventConsumableItemRecipeSheet.TryGetValue(
        //                         currentChronoAvatarSetting.CraftItemID, out var eventConsumableRow))
        //            {
        //                if (eventConsumableRow != null)
        //                    Premium.CRAFT_AutoCraftEventConsumable(currentAvatarState, i, eventConsumableRow);
        //            }
        //        }
        //    }
        //}


        async void SwitchChar()
        {
            //try
            {
                var loadingScreen = Widget.Find<GrayLoadingScreen>();
                loadingScreen.Show();

                var results = await RxProps.SelectAvatarAsync(_slotIndex);
                await WorldBossStates.Set(currentAvatarState.address);
                await States.Instance.InitRuneSlotStates();
                await States.Instance.InitItemSlotStates();
                loadingScreen.Close();
                Util.SaveAvatarSlotIndex(_slotIndex);
                Game.Event.OnRoomEnter.Invoke(false);
                Game.Event.OnUpdateAddresses.Invoke();
                Widget.Find<ChronoSlotsPopup>().Close();
                AudioController.PlayClick();
            }
            //catch
            //{
            //    PandoraMaster.Instance.ShowError(405);
            //}
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