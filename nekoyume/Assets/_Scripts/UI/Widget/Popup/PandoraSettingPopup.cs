using TMPro;
using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Libplanet;
using Libplanet.Crypto;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using UniRx;
using TimeSpan = System.TimeSpan;
using Nekoyume.UI.Scroller;
using Nekoyume.PandoraBox;

namespace Nekoyume.UI
{
    public class PandoraSettingPopup : PopupWidget
    {
        [Header("PANDORA PRIME")]
        [SerializeField] private TMP_InputField cAmount;
        [SerializeField] private TMP_InputField cAaddress;
        [SerializeField] private TMP_InputField cMemo;
        [SerializeField] private TextMeshProUGUI cLog;

        [Space(50)]

        public Transform tabHolder;

        [SerializeField]
        Transform tabContentHolder;

        //node connected
        [SerializeField]
        TextMeshProUGUI nodeText;

        //Time Scale Elements
        [SerializeField]
        Image timeImage;

        [SerializeField]
        Image blockImage;

        [SerializeField]
        Image bothImage;

        //Menu Speed Elements
        [SerializeField]
        TextMeshProUGUI menuSpeedText;

        [SerializeField]
        Slider menuSpeedSlider;

        //Fight Speed Elements
        [SerializeField]
        TextMeshProUGUI fightSpeedText;

        [SerializeField]
        Slider fightSpeedSlider;

        //Arena Speed Elements
        [SerializeField]
        TextMeshProUGUI arenaUpText;
        [SerializeField]
        TextMeshProUGUI arenaLoText;

        [SerializeField]
        Slider arenaUpSlider;
        [SerializeField]
        Slider arenaLoSlider;

        //multiple login
        [SerializeField]
        Image multiLogOnImage;

        [SerializeField]
        Image multiLogOffImage;

        //intro story
        [SerializeField]
        Image introStoryOnImage;

        [SerializeField]
        Image introStoryOffImage;

        //arena multi method
        [SerializeField]
        Image arenaConfirmImage;

        [SerializeField]
        Image arenaPushImage;

        //Arena Push Step Count
        [SerializeField]
        TextMeshProUGUI pushStepText;

        [SerializeField]
        Slider pushStepSlider;

        int blockShowType;

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void OnEnable()
        {
            if (PandoraMaster.Instance == null)
                return;


            try
            { nodeText.text = "Connected Node: <color=green>" + Game.Game.instance._options.RpcServerHost + "</color>"; }
            catch { }

            //Load settings
            blockShowType = PandoraMaster.Instance.Settings.BlockShowType;
            LoadTimeScale();
            menuSpeedSlider.value = PandoraMaster.Instance.Settings.MenuSpeed;
            LoadMenuSpeed();
            fightSpeedSlider.value = PandoraMaster.Instance.Settings.FightSpeed;
            LoadFightSpeed();
            pushStepSlider.value = PandoraMaster.Instance.Settings.ArenaPushStep;
            LoadArenaPushSteps();
            arenaUpSlider.value = PandoraMaster.Instance.Settings.ArenaListUpper;
            LoadArenaUp();
            arenaLoSlider.value = PandoraMaster.Instance.Settings.ArenaListLower;
            LoadArenaLo();
            LoadMultipleLogin();
            LoadIntroStory();
            LoadArenaMethod();

            SubmitWidget = () => Close(true);
            CloseWidget = () => Close(true);
            base.OnEnable();
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            PandoraMaster.Instance.Settings.BlockShowType = blockShowType;
            PandoraMaster.Instance.Settings.MenuSpeed = (int)menuSpeedSlider.value;
            PandoraMaster.Instance.Settings.FightSpeed = (int)fightSpeedSlider.value;
            PandoraMaster.Instance.Settings.ArenaPushStep = (int)pushStepSlider.value;
            PandoraMaster.Instance.Settings.ArenaListUpper = (int)arenaUpSlider.value;
            PandoraMaster.Instance.Settings.ArenaListLower = (int)arenaLoSlider.value;
            PandoraMaster.Instance.Settings.Save();
            base.Close(ignoreCloseAnimation);
            AudioController.PlayClick();
        }

        public override void Show(bool ignoreStartAnimation = false)
        {
            base.Show(true);
        }

        public void SwitchTab(int currentTab)
        {
            foreach (Transform item in tabHolder)
                item.GetComponentInChildren<TextMeshProUGUI>().color = new Color(1, 1, 1, 0.5f);
            foreach (Transform item in tabContentHolder)
                item.gameObject.SetActive(false);

            tabHolder.GetChild(currentTab).GetComponentInChildren<TextMeshProUGUI>().color = new Color(1, 1, 1, 1);
            tabContentHolder.GetChild(currentTab).gameObject.SetActive(true);
        }

        public void ResetDefault()
        {
            PandoraMaster.Instance.Settings = new PandoraSettings();
            PandoraMaster.Instance.Settings.Save();

            //Load settings
            blockShowType = PandoraMaster.Instance.Settings.BlockShowType;
            LoadTimeScale();
            menuSpeedSlider.value = PandoraMaster.Instance.Settings.MenuSpeed;
            LoadMenuSpeed();
            fightSpeedSlider.value = PandoraMaster.Instance.Settings.FightSpeed;
            LoadFightSpeed();
            pushStepSlider.value = PandoraMaster.Instance.Settings.ArenaPushStep;
            LoadArenaPushSteps();
            arenaUpSlider.value = PandoraMaster.Instance.Settings.ArenaListUpper;
            LoadArenaUp();
            arenaLoSlider.value = PandoraMaster.Instance.Settings.ArenaListLower;
            LoadArenaLo();
            LoadMultipleLogin();
            LoadIntroStory();
            LoadArenaMethod();
        }

        public void ChangeTimeScale(int value)
        {
            blockShowType = value;
            LoadTimeScale();
        }

        void LoadTimeScale()
        {
            timeImage.color = blockShowType == 0 ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            blockImage.color = blockShowType == 1 ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            bothImage.color = blockShowType == 2 ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        }

        public void ChangeMultipleLogin(bool value)
        {
            PandoraMaster.Instance.Settings.IsMultipleLogin = value;
            LoadMultipleLogin();
        }

        void LoadMultipleLogin()
        {
            multiLogOnImage.color = PandoraMaster.Instance.Settings.IsMultipleLogin ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            multiLogOffImage.color = !PandoraMaster.Instance.Settings.IsMultipleLogin ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        }

        public void ChangeIntroStory(bool value)
        {
            PandoraMaster.Instance.Settings.IsStory = value;
            LoadIntroStory();
        }

        void LoadIntroStory()
        {
            introStoryOnImage.color = PandoraMaster.Instance.Settings.IsStory ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            introStoryOffImage.color = !PandoraMaster.Instance.Settings.IsStory ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        }

        public void ChangeArenaMethod(bool value)
        {
            PandoraMaster.Instance.Settings.ArenaPush = value;
            LoadArenaMethod();
        }

        void LoadArenaMethod()
        {
            arenaPushImage.color = PandoraMaster.Instance.Settings.ArenaPush ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            arenaConfirmImage.color = !PandoraMaster.Instance.Settings.ArenaPush ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        }

        public void ChangeMenuSpeed()
        {
            LoadMenuSpeed();
        }

        public void LoadMenuSpeed()
        {
            menuSpeedText.text = "Menu Speed : " + (int)(menuSpeedSlider.value * 100) + "%";
        }

        public void ChangeFightSpeed()
        {
            LoadFightSpeed();
        }

        public void LoadFightSpeed()
        {
            fightSpeedText.text = "Fight Speed : X" + (int)fightSpeedSlider.value;
        }

        public void ChangeArenaPushSteps()
        {
            LoadArenaPushSteps();
        }

        public void LoadArenaPushSteps()
        {
            pushStepText.text = "Arena Push Blocks : " + (int)pushStepSlider.value;
        }

        public void ChangeArenaUp()
        {
            LoadArenaUp();
        }
        public void LoadArenaUp()
        {
            arenaUpText.text = (10 + (PandoraMaster.Instance.Settings.ArenaListStep * (int)arenaUpSlider.value)).ToString();
        }

        public void ChangeArenaLo()
        {
            LoadArenaLo();
        }

        public void LoadArenaLo()
        {
            arenaLoText.text = (10 + (PandoraMaster.Instance.Settings.ArenaListStep * (int)arenaLoSlider.value)).ToString();
        }
    }
}
