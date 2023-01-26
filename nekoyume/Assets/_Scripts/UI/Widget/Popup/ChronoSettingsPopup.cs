using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
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
        [Header("GENERAL")]
        [SerializeField] List<Image> settingsTabs;
        [SerializeField] List<GameObject> settingsTabsArea;
        [SerializeField] TextMeshProUGUI AvatarNameText;

        AvatarState currentAvatarState;
        string addressKey;
        int index;

        [Space(5)]

        [Header("STAGE")]
        [SerializeField] GameObject stageModule;
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

        bool IsStage;
        bool IsStageNotify;
        bool IsAutoCollect;
        bool IsAutoSpend;
        bool IsSweep;
        int sweepStage;
        [Space(5)]



        [Header("CRAFT")]
        [SerializeField] GameObject craftModule;
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

        [SerializeField] Image BasicCraftOnImage;
        [SerializeField] Image BasicCraftOffImage;

        bool IsCraft;
        bool IsCraftNotify;
        bool IsAutoCraft;
        bool IsCraftFillCrystal;
        bool IsBasicCraft;
        int craftID;
        [Space(5)]



        [Header("EVENT")]
        [SerializeField] GameObject eventModule;
        [SerializeField] Image eventOnImage;
        [SerializeField] Image eventOffImage;

        [SerializeField] Image eventNotifyOnImage;
        [SerializeField] Image eventNotifyOffImage;

        [SerializeField] Image eventFightOnImage;
        [SerializeField] Image eventFightOffImage;

        [SerializeField] TMP_InputField eventLevelInput;
        bool IsEvent;
        bool IsEventNotify;
        bool IsAutoEvent;
        int eventLevel;
        [Space(5)]

        [Header("World Boss")]
        [SerializeField] GameObject bossModule;
        [SerializeField] Image bossOnImage;
        [SerializeField] Image bossOffImage;

        [SerializeField] Image bossNotifyOnImage;
        [SerializeField] Image bossNotifyOffImage;

        [SerializeField] Image bossFightOnImage;
        [SerializeField] Image bossFightOffImage;
        bool IsBoss;
        bool IsBossNotify;
        bool IsAutoBoss;

        protected override void Awake()
        {
            base.Awake();
        }

        public void SelectTab(int index)
        {
            for (int i = 0; i < settingsTabs.Count; i++)
            {
                settingsTabs[i].color = i == index? new Color(1, 1, 1, 1): new Color(1, 1, 1, 0.5f);
                settingsTabsArea[i].SetActive(i == index);
            }
        }

        public void SaveSettings()
        {
            PlayerPrefs.SetInt(addressKey, index);
            //STAGE
            PlayerPrefs.SetInt(addressKey + "_IsStage", System.Convert.ToInt32(IsStage));
            PlayerPrefs.SetInt(addressKey + "_IsStageNotify", System.Convert.ToInt32(IsStageNotify));
            PlayerPrefs.SetInt(addressKey + "_IsAutoCollect", System.Convert.ToInt32(IsAutoCollect));
            PlayerPrefs.SetInt(addressKey + "_IsAutoSpend", System.Convert.ToInt32(IsAutoSpend));
            PlayerPrefs.SetInt(addressKey + "_IsSweep", System.Convert.ToInt32(IsSweep));
            PlayerPrefs.SetInt(addressKey + "_SweepStage", sweepStage);

            //CRAFT
            PlayerPrefs.SetInt(addressKey + "_IsCraft", System.Convert.ToInt32(IsCraft));
            PlayerPrefs.SetInt(addressKey + "_IsCraftNotify", System.Convert.ToInt32(IsCraftNotify));
            PlayerPrefs.SetInt(addressKey + "_IsAutoCraft", System.Convert.ToInt32(IsAutoCraft));
            PlayerPrefs.SetInt(addressKey + "_IsCraftFillCrystal", System.Convert.ToInt32(IsCraftFillCrystal));
            PlayerPrefs.SetInt(addressKey + "_IsBasicCraft", System.Convert.ToInt32(IsBasicCraft));
            PlayerPrefs.SetInt(addressKey + "_CraftID", craftID);

            //EVENT
            PlayerPrefs.SetInt(addressKey + "_IsEvent", System.Convert.ToInt32(IsEvent));
            PlayerPrefs.SetInt(addressKey + "_IsEventNotify", System.Convert.ToInt32(IsEventNotify));
            PlayerPrefs.SetInt(addressKey + "_IsAutoEvent", System.Convert.ToInt32(IsAutoEvent));
            PlayerPrefs.SetInt(addressKey + "_EventLevel", eventLevel);

            //BOSS
            PlayerPrefs.SetInt(addressKey + "_IsBoss", System.Convert.ToInt32(IsBoss));
            PlayerPrefs.SetInt(addressKey + "_IsBossNotify", System.Convert.ToInt32(IsBossNotify));
            PlayerPrefs.SetInt(addressKey + "_IsAutoBoss", System.Convert.ToInt32(IsAutoBoss));

            AudioController.PlayClick();
            Widget.Find<ChronoSlotsPopup>().slots[index].CheckNowSlot();
            Close();
        }

        public void Show(AvatarState avatarState,int _index)
        {
            currentAvatarState = avatarState;
            addressKey = "_PandoraBox_Chrono_" + currentAvatarState.address;
            index = _index;

            if (PlayerPrefs.HasKey(addressKey))
            {
                //STAGE
                IsStage = System.Convert.ToBoolean(PlayerPrefs.GetInt(addressKey + "_IsStage", 1));
                IsStageNotify = System.Convert.ToBoolean(PlayerPrefs.GetInt(addressKey + "_IsStageNotify", 1));
                IsAutoCollect = System.Convert.ToBoolean(PlayerPrefs.GetInt(addressKey + "_IsAutoCollect", 1));
                IsAutoSpend = System.Convert.ToBoolean(PlayerPrefs.GetInt(addressKey + "_IsAutoSpend", 0));
                IsSweep = System.Convert.ToBoolean(PlayerPrefs.GetInt(addressKey + "_IsSweep", 1));
                sweepStage = PlayerPrefs.GetInt(addressKey + "_SweepStage", 0);

                //CRAFT
                IsCraft = System.Convert.ToBoolean(PlayerPrefs.GetInt(addressKey + "_IsCraft", 0));
                IsCraftNotify = System.Convert.ToBoolean(PlayerPrefs.GetInt(addressKey + "_IsCraftNotify", 1));
                IsAutoCraft = System.Convert.ToBoolean(PlayerPrefs.GetInt(addressKey + "_IsAutoCraft", 0));
                IsCraftFillCrystal = System.Convert.ToBoolean(PlayerPrefs.GetInt(addressKey + "_IsCraftFillCrystal", 0));
                IsBasicCraft = System.Convert.ToBoolean(PlayerPrefs.GetInt(addressKey + "_IsBasicCraft", 1));
                craftID = PlayerPrefs.GetInt(addressKey + "_CraftID", 0);

                //EVENT
                IsEvent = System.Convert.ToBoolean(PlayerPrefs.GetInt(addressKey + "_IsEvent", 0));
                IsEventNotify = System.Convert.ToBoolean(PlayerPrefs.GetInt(addressKey + "_IsEventNotify", 1));
                IsAutoEvent = System.Convert.ToBoolean(PlayerPrefs.GetInt(addressKey + "_IsAutoEvent", 0));
                eventLevel = PlayerPrefs.GetInt(addressKey + "_EventLevel", 0);

                //BOSS
                IsBoss = System.Convert.ToBoolean(PlayerPrefs.GetInt(addressKey + "_IsBoss", 0));
                IsBossNotify = System.Convert.ToBoolean(PlayerPrefs.GetInt(addressKey + "_IsBossNotify", 1));
                IsAutoBoss = System.Convert.ToBoolean(PlayerPrefs.GetInt(addressKey + "_IsAutoBoss", 0));
            }
            else
            {
                //register STAGE variables
                IsStage = true;
                IsStageNotify = true;
                IsAutoCollect = true;
                IsAutoSpend = false;
                IsSweep = true;
                sweepStage = 0;

                //register CRAFT variables
                IsCraft = false;
                IsCraftNotify = true;
                IsAutoCraft = false;
                IsCraftFillCrystal = false;
                IsBasicCraft = true;
                craftID =0;

                //register EVENT variables
                IsEvent = false;
                IsEventNotify = true;
                IsAutoEvent = false;
                eventLevel = 1;

                //register BOSS variables
                IsBoss = false;
                IsBossNotify = true;
                IsAutoBoss = false;
            }

            //reflect on UI
            AvatarNameText.text = currentAvatarState.NameWithHash + " : " + currentAvatarState.address.ToString();
            SelectTab(0);

            //STAGE
            LoadStage();
            LoadAutoCollect();
            LoadAutoSpend();
            LoadSweep();
            LoadSweepStage();
            LoadStageNotify();

            //CRAFT
            LoadCraft();
            LoadCraftNotify();
            LoadAutoCraft();
            LoadCraftFillCrystal();
            LoadCraftID();
            LoadBasicCraft();

            //EVENT
            LoadEvent();
            LoadEventAutoFight();
            LoadEventNotify();
            LoadEventLevel();

            //EVENT
            LoadBoss();
            LoadBossAutoFight();
            LoadBossNotify();

            base.Show();
        }

        void LoadBoss()
        {
            bossOnImage.color = IsBoss ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            bossOffImage.color = !IsBoss ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            bossModule.SetActive(IsBoss);
        }

        public void ChangeBoss(bool value)
        {
            IsBoss = value;
            LoadBoss();
        }

        void LoadBossAutoFight()
        {
            bossFightOnImage.color = IsAutoBoss ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            bossFightOffImage.color = !IsAutoBoss ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        }

        public void ChangeBossAutoFight(bool value)
        {
            if (value && !Premium.PANDORA_CheckPremium())
                return;
            IsAutoBoss = value;
            LoadBossAutoFight();
        }

        void LoadBossNotify()
        {
            bossNotifyOnImage.color = IsBossNotify ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            bossNotifyOffImage.color = !IsBossNotify ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        }

        public void ChangeBossNotify(bool value)
        {
            IsBossNotify = value;
            LoadBossNotify();
        }

        void LoadEvent()
        {
            eventOnImage.color = IsEvent ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            eventOffImage.color = !IsEvent ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            eventModule.SetActive(IsEvent);
        }

        public void ChangeEvent(bool value)
        {
            IsEvent = value;
            LoadEvent();
        }

        void LoadEventAutoFight()
        {
            eventFightOnImage.color = IsAutoEvent ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            eventFightOffImage.color = !IsAutoEvent ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        }

        public void ChangeEventAutoFight(bool value)
        {
            if (value && !Premium.PANDORA_CheckPremium())
                return;
            IsAutoEvent = value;
            LoadEventAutoFight();
        }

        void LoadEventNotify()
        {
            eventNotifyOnImage.color = IsEventNotify ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            eventNotifyOffImage.color = !IsEventNotify ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        }

        public void ChangeEventNotify(bool value)
        {
            IsEventNotify = value;
            LoadEventNotify();
        }

        void LoadEventLevel()
        {
            eventLevelInput.text = eventLevel.ToString();
        }

        public void ChangeEventLevel()
        {
            if (string.IsNullOrEmpty(eventLevelInput.text))
                return;

            int newLevel = int.Parse(eventLevelInput.text);
            if (newLevel < 0 || newLevel > 20)
            {
                OneLineSystem.Push(MailType.System, "<color=green>Pandora Box</color>: Event Stage Not Correct!", NotificationCell.NotificationType.Alert);
                return;
            }
            eventLevel = newLevel;
        }

        void LoadCraftID()
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
                        if (currentAvatarState.worldInformation.IsStageCleared(equipRow.UnlockStage))
                        {
                            craftID = tempCraftID; //equip
                            CraftIconIDImage.sprite = SpriteHelper.GetItemIcon(equipRow.ResultEquipmentId);
                        }
                    }
                    else if (tableSheets.ConsumableItemRecipeSheet.TryGetValue(tempCraftID, out var consumableRow))
                    {
                        craftID = tempCraftID; //consumable
                        CraftIconIDImage.sprite = SpriteHelper.GetItemIcon(consumableRow.ResultConsumableItemId);
                    }
                    else if (tableSheets.EventConsumableItemRecipeSheet.TryGetValue(tempCraftID, out var eventConsumableRow))
                    {
                        craftID = tempCraftID; //event consumable
                        CraftIconIDImage.sprite = SpriteHelper.GetItemIcon(eventConsumableRow.ResultConsumableItemId);
                    }
                }
            }
            catch { }
        }

        void LoadBasicCraft()
        {
            BasicCraftOnImage.color = IsBasicCraft ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            BasicCraftOffImage.color = !IsBasicCraft ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        }

        public void ChangeBasicCraft(bool value)
        {
            IsBasicCraft = value;
            LoadBasicCraft();
        }

        void LoadCraftFillCrystal()
        {
            CraftFillCrystalOnImage.color = IsCraftFillCrystal ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            CraftFillCrystalOffImage.color = !IsCraftFillCrystal ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        }

        public void ChangeCraftFillCrystal(bool value)
        {
            IsCraftFillCrystal = value;
            LoadCraftFillCrystal();
        }

        void LoadAutoCraft()
        {
            AutoCraftOnImage.color = IsAutoCraft ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            AutoCraftOffImage.color = !IsAutoCraft ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        }

        public void ChangeAutoCraft(bool value)
        {
            if (value && !Premium.PANDORA_CheckPremium())
                return;
            IsAutoCraft = value;
            LoadAutoCraft();
        }

        void LoadStage()
        {
            stageOnImage.color = IsStage ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            stageOffImage.color = !IsStage ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            stageModule.SetActive(IsStage);
        }

        public void ChangeStage(bool value)
        {
            IsStage = value;
            LoadStage();
        }

        void LoadStageNotify()
        {
            stageNotifyOnImage.color = IsStageNotify ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            stageNotifyOffImage.color = !IsStageNotify ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        }

        public void ChangeStageNotify(bool value)
        {
            IsStageNotify = value;
            LoadStageNotify();
        }

        void LoadCraft()
        {
            craftOnImage.color = IsCraft ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            craftOffImage.color = !IsCraft ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            craftModule.SetActive(IsCraft);
        }

        public void ChangeCraft(bool value)
        {
            IsCraft = value;
            LoadCraft();
        }

        void LoadCraftNotify()
        {
            craftNotifyOnImage.color = IsCraftNotify ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            craftNotifyOffImage.color = !IsCraftNotify ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        }

        public void ChangeCraftNotify(bool value)
        {
            IsCraftNotify = value;
            LoadCraftNotify();
        }

        void LoadAutoCollect()
        {
            collectOnImage.color = IsAutoCollect ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            collectOffImage.color = !IsAutoCollect ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        }

        public void ChangeAutoCollect(bool value)
        {
            IsAutoCollect = value;
            LoadAutoCollect();
        }

        void LoadSweep()
        {
            sweepOnImage.color = IsSweep ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            sweepOffImage.color = !IsSweep ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            sweepStageInput.transform.parent.gameObject.SetActive(IsSweep);
        }

        public void ChangeSweep(bool value)
        {
            if (value && !Premium.PANDORA_CheckPremium())
                return;
            IsSweep = value;
            sweepStageInput.transform.parent.gameObject.SetActive(IsSweep);
            LoadSweep();
        }

        void LoadAutoSpend()
        {
            spendOnImage.color = IsAutoSpend ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            spendOffImage.color = !IsAutoSpend ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        }

        public void ChangeAutoSpend(bool value)
        {
            if (value && !Premium.PANDORA_CheckPremium())
                return;
            IsAutoSpend = value;
            LoadAutoSpend();
        }

        void LoadSweepStage()
        {
            sweepStageInput.text = sweepStage.ToString();
        }

        public void ChangeSweepStage()
        {
            if (!string.IsNullOrEmpty(sweepStageInput.text))
                sweepStage = int.Parse(sweepStageInput.text);
            if (!currentAvatarState.worldInformation.IsStageCleared(sweepStage))
                OneLineSystem.Push(MailType.System, "<color=green>Pandora Box</color>: Stage Not Cleared!", NotificationCell.NotificationType.Alert);
        }

    }
}
