using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Nekoyume.Action;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Quest;
using Nekoyume.Model.State;
using Nekoyume.PandoraBox;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.TableData.Event;
using Nekoyume.UI.Scroller;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class ChronoSettingsPopup : PopupWidget
    {
        [Header("GENERAL")] [SerializeField] List<Image> settingsTabs;
        [SerializeField] List<GameObject> settingsTabsArea;
        [SerializeField] TextMeshProUGUI AvatarNameText;

        [Space(5)] [Header("STAGE")] [SerializeField]
        GameObject stageModule;

        [SerializeField] Image stageOnImage;
        [SerializeField] Image stageOffImage;

        [SerializeField] Image stageNotifyOnImage;
        [SerializeField] Image stageNotifyOffImage;

        [SerializeField] Image collectOnImage;
        [SerializeField] Image collectOffImage;

        [SerializeField] Image spendOnImage;
        [SerializeField] Image spendOffImage;

        [SerializeField] Image sweepOnImage;
        [SerializeField] Image sweepOffImage;

        [SerializeField] TMP_InputField sweepStageInput;

        [Space(5)] [Header("CRAFT")] [SerializeField]
        GameObject craftModule;

        [SerializeField] Image craftOnImage;
        [SerializeField] Image craftOffImage;

        [SerializeField] Image craftNotifyOnImage;
        [SerializeField] Image craftNotifyOffImage;

        [SerializeField] Image AutoCraftOnImage;
        [SerializeField] Image AutoCraftOffImage;

        [SerializeField] Image CraftFillCrystalOnImage;
        [SerializeField] Image CraftFillCrystalOffImage;

        [SerializeField] TMP_InputField craftIDInput;
        [SerializeField] Image CraftIconIDImage;

        [SerializeField] Image PremiumCraftOnImage;
        [SerializeField] Image PremiumCraftOffImage;

        [Space(5)] [Header("EVENT")] [SerializeField]
        GameObject eventModule;

        [SerializeField] Image eventOnImage;
        [SerializeField] Image eventOffImage;

        [SerializeField] Image eventNotifyOnImage;
        [SerializeField] Image eventNotifyOffImage;

        [SerializeField] Image eventFightOnImage;
        [SerializeField] Image eventFightOffImage;

        [SerializeField] TMP_InputField eventLevelInput;

        [Space(5)] [Header("World Boss")] [SerializeField]
        GameObject bossModule;

        [SerializeField] Image bossOnImage;
        [SerializeField] Image bossOffImage;

        [SerializeField] Image bossNotifyOnImage;
        [SerializeField] Image bossNotifyOffImage;

        [SerializeField] Image bossRewardsOnImage;
        [SerializeField] Image bossRewardsOffImage;

        [SerializeField] Image bossFightOnImage;
        [SerializeField] Image bossFightOffImage;

        AvatarState currentAvatarState;
        int index;
        ChronoAvatarSetting currentChronoAvatarSetting;

        protected override void Awake()
        {
            base.Awake();
        }

        public void SelectTab(int index)
        {
            for (int i = 0; i < settingsTabs.Count; i++)
            {
                settingsTabs[i].color = i == index ? new Color(1, 1, 1, 1) : new Color(1, 1, 1, 0.5f);
                settingsTabsArea[i].SetActive(i == index);
            }
        }

        public void SaveSettings()
        {
            var settings = new ChronoAvatarSettings();
            settings.LoadSettings();
            settings.SaveSettings(currentChronoAvatarSetting);
            PandoraUtil.ShowSystemNotification(
                $"<color=red>{currentAvatarState.name}</color> Chrono settings saved Successfully!",
                NotificationCell.NotificationType.Information);
            AudioController.PlayClick();
            Widget.Find<ChronoSlotsPopup>().slots[index].CheckNowSlot().Forget();
            Close();
        }

        public void Show(AvatarState avatarState, int _index)
        {
            currentAvatarState = avatarState;
            index = _index;

            var settings = new ChronoAvatarSettings();
            settings.LoadSettings();
            if (currentAvatarState is null)
            {
                PandoraUtil.ShowSystemNotification("Please Wait while slot settnigs is ready...",
                    NotificationCell.NotificationType.Information);
                return;
            }

            currentChronoAvatarSetting = settings.GetSettings(currentAvatarState.address.ToString());

            //reflect on UI
            AvatarNameText.text = currentAvatarState.NameWithHash + " : " + currentAvatarState.address.ToString();
            SelectTab(0);

            //STAGE
            LoadStage(currentChronoAvatarSetting.Stage);
            LoadStageNotify(currentChronoAvatarSetting.StageNotification);
            LoadAutoCollect(currentChronoAvatarSetting.StageIsAutoCollectProsperity);
            LoadAutoSpend(currentChronoAvatarSetting.StageIsAutoSpendProsperity);
            LoadSweep(currentChronoAvatarSetting.StageIsSweepAP);
            LoadSweepStage(currentChronoAvatarSetting.StageSweepLevelIndex);

            //CRAFT
            LoadCraft(currentChronoAvatarSetting.Craft);
            LoadCraftNotify(currentChronoAvatarSetting.CraftNotification);
            LoadAutoCraft(currentChronoAvatarSetting.CraftIsAutoCombine);
            LoadCraftFillCrystal(currentChronoAvatarSetting.CraftIsUseCrystal);
            LoadCraftID(currentChronoAvatarSetting.CraftItemID);
            LoadPremiumCraft(currentChronoAvatarSetting.CraftIsPremium);

            //EVENT
            LoadEvent(currentChronoAvatarSetting.Event);
            LoadEventAutoFight(currentChronoAvatarSetting.EventIsAutoSpendTickets);
            LoadEventNotify(currentChronoAvatarSetting.EventNotification);
            LoadEventLevel(currentChronoAvatarSetting.EventLevelIndex.ToString());

            //BOSS
            LoadBoss(currentChronoAvatarSetting.Boss);
            LoadBossAutoCollect(currentChronoAvatarSetting.BosstIsAutoCollectRewards);
            LoadBossAutoFight(currentChronoAvatarSetting.BosstIsAutoSpendTickets);
            LoadBossNotify(currentChronoAvatarSetting.BossNotification);

            base.Show();
        }

        void LoadBoss(bool boss)
        {
            bossOnImage.color = boss ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            bossOffImage.color = !boss ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            bossModule.SetActive(boss);
        }

        public void ChangeBoss(bool value)
        {
            currentChronoAvatarSetting.Boss = value;
            LoadBoss(value);
        }

        void LoadBossAutoCollect(bool IsAuto)
        {
            bossRewardsOnImage.color = IsAuto ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            bossRewardsOffImage.color = !IsAuto ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        }

        public void ChangeBossAutoCollect(bool value)
        {
            currentChronoAvatarSetting.BosstIsAutoCollectRewards = value;
            LoadBossAutoCollect(value);
        }

        void LoadBossAutoFight(bool IsAuto)
        {
            bossFightOnImage.color = IsAuto ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            bossFightOffImage.color = !IsAuto ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        }

        public void ChangeBossAutoFight(bool value)
        {
            if (value && !Premium.PANDORA_CheckPremium())
                return;
            currentChronoAvatarSetting.BosstIsAutoSpendTickets = value;
            LoadBossAutoFight(value);
        }

        void LoadBossNotify(bool notify)
        {
            bossNotifyOnImage.color = notify ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            bossNotifyOffImage.color = !notify ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        }

        public void ChangeBossNotify(bool value)
        {
            currentChronoAvatarSetting.BossNotification = value;
            LoadBossNotify(value);
        }

        void LoadEvent(bool isEvent)
        {
            eventOnImage.color = isEvent ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            eventOffImage.color = !isEvent ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            eventModule.SetActive(isEvent);
        }

        public void ChangeEvent(bool value)
        {
            currentChronoAvatarSetting.Event = value;
            LoadEvent(value);
        }

        void LoadEventAutoFight(bool IsAutoEvent)
        {
            eventFightOnImage.color = IsAutoEvent ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            eventFightOffImage.color = !IsAutoEvent ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        }

        public void ChangeEventAutoFight(bool value)
        {
            if (value && !Premium.PANDORA_CheckPremium())
                return;
            currentChronoAvatarSetting.EventIsAutoSpendTickets = value;
            LoadEventAutoFight(value);
        }

        void LoadEventNotify(bool IsEventNotify)
        {
            eventNotifyOnImage.color = IsEventNotify ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            eventNotifyOffImage.color = !IsEventNotify ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        }

        public void ChangeEventNotify(bool value)
        {
            currentChronoAvatarSetting.EventNotification = value;
            LoadEventNotify(value);
        }

        void LoadEventLevel(string eventLevel)
        {
            eventLevelInput.text = eventLevel;
        }

        public void ChangeEventLevel()
        {
            if (string.IsNullOrEmpty(eventLevelInput.text))
                return;

            int newLevel = int.Parse(eventLevelInput.text);
            if (newLevel < 0 || newLevel > 20)
            {
                OneLineSystem.Push(MailType.System, "<color=green>Pandora Box</color>: Event Stage Not Correct!",
                    NotificationCell.NotificationType.Alert);
                return;
            }

            currentChronoAvatarSetting.EventLevelIndex = newLevel;
        }

        void LoadCraftID(int craftID)
        {
            craftIDInput.text = craftID.ToString();
            var tableSheets = Game.TableSheets.Instance;
            if (tableSheets.EquipmentItemRecipeSheet.TryGetValue(craftID, out var equipRow))
                CraftIconIDImage.sprite = SpriteHelper.GetItemIcon(equipRow.ResultEquipmentId);
            else if (tableSheets.ConsumableItemRecipeSheet.TryGetValue(craftID, out var consumableRow))
                CraftIconIDImage.sprite = SpriteHelper.GetItemIcon(consumableRow.ResultConsumableItemId);
            else if (tableSheets.EventConsumableItemRecipeSheet.TryGetValue(craftID, out var eventConsumableRow))
                CraftIconIDImage.sprite = SpriteHelper.GetItemIcon(eventConsumableRow.ResultConsumableItemId);
        }

        public void ChangeCraftID()
        {
            try
            {
                if (!string.IsNullOrEmpty(craftIDInput.text))
                {
                    int tempCraftID = int.Parse(craftIDInput.text);
                    CraftIconIDImage.sprite = SpriteHelper.GetItemIcon(0);

                    var tableSheets = Game.TableSheets.Instance;
                    if (tableSheets.EquipmentItemRecipeSheet.TryGetValue(tempCraftID, out var equipRow))
                    {
                        if (currentAvatarState.worldInformation.IsStageCleared(equipRow.UnlockStage) ||
                            (tempCraftID == 158 || tempCraftID == 159 || tempCraftID == 160))
                        {
                            currentChronoAvatarSetting.CraftItemID = tempCraftID; //equip
                            CraftIconIDImage.sprite = SpriteHelper.GetItemIcon(equipRow.ResultEquipmentId);
                        }
                    }
                    else if (tableSheets.ConsumableItemRecipeSheet.TryGetValue(tempCraftID, out var consumableRow))
                    {
                        currentChronoAvatarSetting.CraftItemID = tempCraftID; //consumable
                        CraftIconIDImage.sprite = SpriteHelper.GetItemIcon(consumableRow.ResultConsumableItemId);
                    }
                    else if (tableSheets.EventConsumableItemRecipeSheet.TryGetValue(tempCraftID,
                                 out var eventConsumableRow))
                    {
                        currentChronoAvatarSetting.CraftItemID = tempCraftID; //event consumable
                        CraftIconIDImage.sprite = SpriteHelper.GetItemIcon(eventConsumableRow.ResultConsumableItemId);
                    }
                }
            }
            catch
            {
            }
        }

        void LoadPremiumCraft(bool IsPremium)
        {
            PremiumCraftOnImage.color = IsPremium ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            PremiumCraftOffImage.color = !IsPremium ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        }

        public void ChangePremiumCraft(bool value)
        {
            currentChronoAvatarSetting.CraftIsPremium = value;
            LoadPremiumCraft(value);
        }

        void LoadCraftFillCrystal(bool IsCraftFillCrystal)
        {
            CraftFillCrystalOnImage.color = IsCraftFillCrystal ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            CraftFillCrystalOffImage.color = !IsCraftFillCrystal ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        }

        public void ChangeCraftFillCrystal(bool value)
        {
            currentChronoAvatarSetting.CraftIsUseCrystal = value;
            LoadCraftFillCrystal(value);
        }

        void LoadAutoCraft(bool IsAutoCraft)
        {
            AutoCraftOnImage.color = IsAutoCraft ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            AutoCraftOffImage.color = !IsAutoCraft ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        }

        public void ChangeAutoCraft(bool value)
        {
            if (value && !Premium.PANDORA_CheckPremium())
                return;
            currentChronoAvatarSetting.CraftIsAutoCombine = value;
            LoadAutoCraft(value);
        }

        void LoadStage(bool Stage)
        {
            stageOnImage.color = Stage ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            stageOffImage.color = !Stage ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            stageModule.SetActive(Stage);
        }

        public void ChangeStage(bool value)
        {
            currentChronoAvatarSetting.Stage = value;
            LoadStage(value);
        }

        void LoadStageNotify(bool IsStageNotify)
        {
            stageNotifyOnImage.color = IsStageNotify ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            stageNotifyOffImage.color = !IsStageNotify ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        }

        public void ChangeStageNotify(bool value)
        {
            currentChronoAvatarSetting.StageNotification = value;
            LoadStageNotify(value);
        }

        void LoadCraft(bool IsCraft)
        {
            craftOnImage.color = IsCraft ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            craftOffImage.color = !IsCraft ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            craftModule.SetActive(IsCraft);
        }

        public void ChangeCraft(bool value)
        {
            currentChronoAvatarSetting.Craft = value;
            LoadCraft(value);
        }

        void LoadCraftNotify(bool IsCraftNotify)
        {
            craftNotifyOnImage.color = IsCraftNotify ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            craftNotifyOffImage.color = !IsCraftNotify ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        }

        public void ChangeCraftNotify(bool value)
        {
            currentChronoAvatarSetting.CraftNotification = value;
            LoadCraftNotify(value);
        }

        void LoadAutoCollect(bool IsAutoCollect)
        {
            collectOnImage.color = IsAutoCollect ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            collectOffImage.color = !IsAutoCollect ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        }

        public void ChangeAutoCollect(bool value)
        {
            currentChronoAvatarSetting.StageIsAutoCollectProsperity = value;
            LoadAutoCollect(value);
        }

        void LoadSweep(bool IsSweep)
        {
            sweepOnImage.color = IsSweep ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            sweepOffImage.color = !IsSweep ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            sweepStageInput.transform.parent.gameObject.SetActive(IsSweep);
        }

        public void ChangeSweep(bool value)
        {
            if (value && !Premium.PANDORA_CheckPremium())
                return;
            currentChronoAvatarSetting.StageIsSweepAP = value;
            sweepStageInput.transform.parent.gameObject.SetActive(value);
            LoadSweep(value);
        }

        void LoadAutoSpend(bool IsAutoSpend)
        {
            spendOnImage.color = IsAutoSpend ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            spendOffImage.color = !IsAutoSpend ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        }

        public void ChangeAutoSpend(bool value)
        {
            if (value && !Premium.PANDORA_CheckPremium())
                return;
            currentChronoAvatarSetting.StageIsAutoSpendProsperity = value;
            LoadAutoSpend(value);
        }

        void LoadSweepStage(int sweepStage)
        {
            sweepStageInput.text = sweepStage.ToString();
        }

        public void ChangeSweepStage()
        {
            if (!string.IsNullOrEmpty(sweepStageInput.text))
                currentChronoAvatarSetting.StageSweepLevelIndex = int.Parse(sweepStageInput.text);
            if (!currentAvatarState.worldInformation.IsStageCleared(currentChronoAvatarSetting.StageSweepLevelIndex))
                OneLineSystem.Push(MailType.System, "<color=green>Pandora Box</color>: Stage Not Cleared!",
                    NotificationCell.NotificationType.Alert);
        }
    }
}