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
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.PandoraBox;
using Nekoyume.State;
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

        //STAGE
        [SerializeField] Image stageNotifyOnImage;
        [SerializeField] Image stageNotifyOffImage;

        [SerializeField] Image collectOnImage;
        [SerializeField] Image collectOffImage;

        [SerializeField] Image spendOnImage;
        [SerializeField] Image spendOffImage;

        [SerializeField] TMP_InputField sweepStageInput;


        //CRAFT
        [SerializeField] Image craftNotifyOnImage;
        [SerializeField] Image craftNotifyOffImage;


        private AvatarState currentAvatarState;
        //settings
        string addressKey;
        int index;
        bool IsStageNotify;
        bool IsCraftNotify;
        bool IsAutoCollect;
        bool IsAutoSpend;
        int sweepStage;

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
            PlayerPrefs.SetInt(addressKey + "_IsStageNotify", System.Convert.ToInt32(IsStageNotify));
            PlayerPrefs.SetInt(addressKey + "_IsCraftNotify", System.Convert.ToInt32(IsCraftNotify));
            PlayerPrefs.SetInt(addressKey + "_IsAutoCollect", System.Convert.ToInt32(IsAutoCollect));
            PlayerPrefs.SetInt(addressKey + "_IsAutoSpend", System.Convert.ToInt32(IsAutoSpend));
            PlayerPrefs.SetInt(addressKey + "_SweepStage", sweepStage);
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

            //reflect on UI
            AvatarNameText.text = currentAvatarState.NameWithHash + " : " + currentAvatarState.address.ToString();
            LoadAutoCollect();
            LoadAutoSpend();
            LoadSweepStage();
            LoadStageNotify();
            LoadCraftNotify();
            SelectTab(0);

            base.Show();
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
