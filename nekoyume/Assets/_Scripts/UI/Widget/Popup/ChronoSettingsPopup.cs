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
using Nekoyume.Model.State;
using Nekoyume.PandoraBox;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI.Scroller;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class ChronoSettingsPopup : PopupWidget
    {
        [SerializeField] List<Image> settingsTabs;
        [SerializeField] List<GameObject> settingsTabsArea;

        [SerializeField] TextMeshProUGUI AvatarNameText;

        [Header("Chrono Stage Settings")]
        //STAGE
        [SerializeField] Image stageNotifyOnImage;
        [SerializeField] Image stageNotifyOffImage;

        [SerializeField] Image collectOnImage;
        [SerializeField] Image collectOffImage;

        [SerializeField] Image spendOnImage;
        [SerializeField] Image spendOffImage;

        [SerializeField] TMP_InputField sweepStageInput;

        [Space(50)]
        [Header("Chrono Craft Settings")]
        //CRAFT
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


        private AvatarState currentAvatarState;

        //settings Satge
        string addressKey;
        int index;
        bool IsStageNotify;
        bool IsAutoCollect;
        bool IsAutoSpend;
        int sweepStage;

        //Setting Craft
        bool IsCraftNotify;
        bool IsAutoCraft;
        bool IsCraftFillCrystal;
        bool IsBasicCraft;
        int craftID;

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
            PlayerPrefs.SetInt(addressKey + "_IsStageNotify", System.Convert.ToInt32(IsStageNotify));
            PlayerPrefs.SetInt(addressKey + "_IsAutoCollect", System.Convert.ToInt32(IsAutoCollect));
            PlayerPrefs.SetInt(addressKey + "_IsAutoSpend", System.Convert.ToInt32(IsAutoSpend));
            PlayerPrefs.SetInt(addressKey + "_SweepStage", sweepStage);

            //CRAFT
            PlayerPrefs.SetInt(addressKey + "_IsCraftNotify", System.Convert.ToInt32(IsCraftNotify));
            PlayerPrefs.SetInt(addressKey + "_IsAutoCraft", System.Convert.ToInt32(IsAutoCraft));
            PlayerPrefs.SetInt(addressKey + "_IsCraftFillCrystal", System.Convert.ToInt32(IsCraftFillCrystal));
            PlayerPrefs.SetInt(addressKey + "_IsBasicCraft", System.Convert.ToInt32(IsBasicCraft));
            PlayerPrefs.SetInt(addressKey + "_CraftID", craftID);

            AudioController.PlayClick();
            Widget.Find<ChronoSlotsPopup>().slots[index].IsPrefsLoded = false;
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
                IsStageNotify = System.Convert.ToBoolean(PlayerPrefs.GetInt(addressKey + "_IsStageNotify", 1));
                IsAutoCollect = System.Convert.ToBoolean(PlayerPrefs.GetInt(addressKey + "_IsAutoCollect", 1));
                IsAutoSpend = System.Convert.ToBoolean(PlayerPrefs.GetInt(addressKey + "_IsAutoSpend", 0));
                sweepStage = PlayerPrefs.GetInt(addressKey + "_SweepStage", 0);

                //CRAFT
                IsCraftNotify = System.Convert.ToBoolean(PlayerPrefs.GetInt(addressKey + "_IsCraftNotify", 1));
                IsAutoCraft = System.Convert.ToBoolean(PlayerPrefs.GetInt(addressKey + "_IsAutoCraft", 0));
                IsCraftFillCrystal = System.Convert.ToBoolean(PlayerPrefs.GetInt(addressKey + "_IsCraftFillCrystal", 0));
                IsBasicCraft = System.Convert.ToBoolean(PlayerPrefs.GetInt(addressKey + "_IsBasicCraft", 1));
                craftID = PlayerPrefs.GetInt(addressKey + "_CraftID", 0);
            }
            else
            {
                //register STAGE variables
                IsStageNotify = true;
                IsAutoCollect = true;
                IsAutoSpend = false;
                sweepStage = 0;

                //register CRAFT variables
                IsCraftNotify = true;
                IsAutoCraft = false;
                IsCraftFillCrystal = false;
                IsBasicCraft = true;
                craftID =0;
            }

            //reflect on UI
            AvatarNameText.text = currentAvatarState.NameWithHash + " : " + currentAvatarState.address.ToString();
            SelectTab(0);

            //STAGE
            LoadAutoCollect();
            LoadAutoSpend();
            LoadSweepStage();
            LoadStageNotify();

            //CRAFT
            LoadCraftNotify();
            LoadAutoCraft();
            LoadCraftFillCrystal();
            LoadCraftID();
            LoadBasicCraft();


            base.Show();
        }

        void LoadCraftID()
        {
            craftIDInput.text = craftID.ToString();
            CraftIconIDImage.sprite = SpriteHelper.GetItemIcon(craftID);
        }

        public void ChangeCraftID()
        {
            if (!string.IsNullOrEmpty(craftIDInput.text) && craftIDInput.text.Length == 8)
            {
                int tempCraftID = int.Parse(craftIDInput.text);
                var tableSheets = Game.TableSheets.Instance;
                var itemSheet = tableSheets.EquipmentItemRecipeSheet;
                var itemRow = itemSheet.First(x => x.Value.ResultEquipmentId == tempCraftID).Value;
                if (itemRow != null && currentAvatarState.worldInformation.IsStageCleared(itemRow.UnlockStage))
                {
                    craftID = tempCraftID;
                }
            }
            CraftIconIDImage.sprite = SpriteHelper.GetItemIcon(craftID);
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
            if (value && !Premium.CheckPremiumFeature())
                return;
            IsAutoCraft = value;
            LoadAutoCraft();
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

        void LoadAutoSpend()
        {
            spendOnImage.color = IsAutoSpend ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            spendOffImage.color = !IsAutoSpend ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        }

        public void ChangeAutoSpend(bool value)
        {
            if (value && !Premium.CheckPremiumFeature())
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
