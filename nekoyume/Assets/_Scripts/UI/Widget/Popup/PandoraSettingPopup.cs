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
using Nekoyume.Model.Item;
using Nekoyume.State;
using PlayFab;
using PlayFab.ClientModels;

namespace Nekoyume.UI
{
    public class PandoraSettingPopup : PopupWidget
    {
        [Header("PANDORA CUSTOM FIELDS")] public Transform tabHolder;
        [SerializeField] Transform tabContentHolder;

        [Space(20)] [Header("ACCOUNT")] [SerializeField]
        private TextMeshProUGUI accountDisplayText;

        [SerializeField] Button accountDisplayButton;
        [SerializeField] private TextMeshProUGUI accountNewDisplayText;
        [SerializeField] private TextMeshProUGUI accountEmailText;
        [SerializeField] private Toggle rememberToggle;
        [SerializeField] private Toggle autoLoginToggle;
        [SerializeField] TextMeshProUGUI nodeText;
        PandoraAccountSlot currentPandoraAccountSlot;

        [Space(20)]
        [Header("GENERAL")]
        //Time Scale Elements
        [SerializeField]
        Image timeImage;

        [SerializeField] Image blockImage;

        [SerializeField] Image bothImage;

        //Time Scale Elements
        [SerializeField] Image ncgImage;
        [SerializeField] Image dollarImage;

        [SerializeField] Image bothNcgDollarImage;

        //intro story
        [SerializeField] Image introStoryOnImage;

        [SerializeField] Image introStoryOffImage;

        //random node
        [SerializeField] Image randomNodeOnImage;

        [SerializeField] Image randomNodeOffImage;

        //itembase id 
        [SerializeField] TMP_InputField itemIDText;
        [SerializeField] Button itemIDButton;

        [Space(20)]
        [Header("BATTLE")]
        //Arena list count 
        [SerializeField]
        TextMeshProUGUI arenaUpText;

        [SerializeField] TextMeshProUGUI arenaLoText;
        [SerializeField] Slider arenaUpSlider;

        [SerializeField] Slider arenaLoSlider;

        //arena multi method
        [SerializeField] Image arenaConfirmImage;

        [SerializeField] Image arenaPushImage;

        //Arena Push Step Count
        [SerializeField] TextMeshProUGUI pushStepText;

        [SerializeField] Slider pushStepSlider;

        //arena validator
        [SerializeField] Image arenaValidatorOnImage;
        [SerializeField] Image arenaValidatorOffImage;


        [Header("DEBUG")] [SerializeField] private TMP_InputField cAmount;
        [SerializeField] private TMP_InputField cAaddress;
        [SerializeField] private TMP_InputField cMemo;
        [SerializeField] private TextMeshProUGUI cLog;


        protected override void Awake()
        {
            base.Awake();
            itemIDButton.onClick.AddListener(() => { ShowBaseItemTooltip(); });
            SubmitWidget = () => Close(true);
            CloseWidget = () => Close(true);
        }


        public override void Close(bool ignoreCloseAnimation = false)
        {
            PandoraMaster.Instance.Settings.ArenaPushStep = (int)pushStepSlider.value;
            PandoraMaster.Instance.Settings.ArenaListUpper = (int)arenaUpSlider.value;
            PandoraMaster.Instance.Settings.ArenaListLower = (int)arenaLoSlider.value;
            PandoraMaster.Instance.Settings.Save();
            //save slot data
            PlayerPrefs.SetInt("_PandoraBox_Account_LoginAccount_" + PandoraMaster.SelectedLoginAccountIndex +
                               "_IsRemember", System.Convert.ToInt32(rememberToggle.isOn));
            PlayerPrefs.SetInt("_PandoraBox_Account_LoginAccount_" + PandoraMaster.SelectedLoginAccountIndex
                                                                   + "_IsAutoLogin",
                System.Convert.ToInt32(autoLoginToggle.isOn));

            base.Close(ignoreCloseAnimation);
            AudioController.PlayClick();
        }

        public override void Show(bool ignoreStartAnimation = false)
        {
            LoadSetting();
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

        void LoadSetting()
        {
            //ACCOUNT
            accountDisplayText.text = Premium.PandoraProfile.Profile.DisplayName;
            string newName = States.Instance.CurrentAvatarState.name + " #" +
                             States.Instance.CurrentAvatarState.address.ToHex().Substring(0, 5);
            accountNewDisplayText.text = "Change > " + newName;
            accountDisplayButton.gameObject.SetActive(accountDisplayText.text != newName);
            currentPandoraAccountSlot = Widget.Find<LoginSystem>().pandoraLogin
                .PandoraAccounts[PandoraMaster.SelectedLoginAccountIndex];
            currentPandoraAccountSlot.LoadData(PandoraMaster.SelectedLoginAccountIndex);
            accountEmailText.text = HiddenEmail(currentPandoraAccountSlot.Email);
            rememberToggle.isOn = currentPandoraAccountSlot.IsRemember;
            autoLoginToggle.isOn = currentPandoraAccountSlot.IsAutoLogin;
            nodeText.text = "Connected Node: <color=green>" + Game.Game.instance._options.RpcServerHost + "</color>";

            //GENERAL
            LoadTimeScale();
            LoadCurrency();
            LoadIntroStory();
            LoadRandomNode();


            //BATTLE
            pushStepSlider.value = PandoraMaster.Instance.Settings.ArenaPushStep;
            arenaUpSlider.value = PandoraMaster.Instance.Settings.ArenaListUpper;
            arenaLoSlider.value = PandoraMaster.Instance.Settings.ArenaListLower;
            LoadArenaUp();
            LoadArenaLo();
            LoadArenaMethod();
            LoadArenaPushSteps();
            LoadArenaValidator();
        }

        public void ResetDefault()
        {
            PandoraMaster.Instance.Settings = new PandoraSettings();
            PandoraMaster.Instance.Settings.Save();
            //reset profile slot sata
            ResetAccountSlot();
            LoadSetting();
        }

        public void Logout()
        {
            string content = $"Are you certain you want to remove your login details and close the game?";
            Find<TwoButtonSystem>().Show(content, "Yes", "No",
                (() =>
                {
                    //delete all keys
                    string key = "_PandoraBox_Account_LoginAccount_" + PandoraMaster.SelectedLoginAccountIndex;
                    PlayerPrefs.DeleteKey(key + "_Email");
                    PlayerPrefs.DeleteKey(key + "_Password");
                    PlayerPrefs.DeleteKey(key + "_Username");
                    PlayerPrefs.DeleteKey(key + "_DisplayText");
                    PlayerPrefs.DeleteKey(key + "_IsRemember");
                    PlayerPrefs.DeleteKey(key + "_IsAutoLogin");
                    PlayerPrefs.DeleteKey(key + "_AddressPassword");
                    Application.Quit();
                }));
        }

        void ResetAccountSlot()
        {
            currentPandoraAccountSlot.SaveData(currentPandoraAccountSlot.Email, "", "", false);
            PlayerPrefs.SetInt("_PandoraBox_Account_LoginAccount_" + PandoraMaster.SelectedLoginAccountIndex
                                                                   + "_IsAutoLogin", System.Convert.ToInt32(false));
        }

        public void ChangeDisplayName()
        {
            string displayNameKey = "_PandoraBox_Account_LoginAccount_" + PandoraMaster.SelectedLoginAccountIndex +
                                    "_DisplayText";
            var currentName = States.Instance.CurrentAvatarState.name + " #" +
                              States.Instance.CurrentAvatarState.address.ToHex().Substring(0, 5);
            PlayerPrefs.SetString(displayNameKey, currentName); //save profile name

            PlayFabClientAPI.UpdateUserTitleDisplayName(
                new UpdateUserTitleDisplayNameRequest { DisplayName = currentName },
                success =>
                {
                    Premium.PandoraProfile.Profile.DisplayName = currentName;
                    currentPandoraAccountSlot.LoadData(PandoraMaster.SelectedLoginAccountIndex);
                    accountDisplayText.text = Premium.PandoraProfile.Profile.DisplayName;
                    accountDisplayButton.gameObject.SetActive(false);
                    PandoraUtil.ShowSystemNotification("Display Chaned Successfully!",
                        NotificationCell.NotificationType.Information);
                },
                failed =>
                {
                    PandoraUtil.ShowSystemNotification(failed.ErrorMessage, NotificationCell.NotificationType.Alert);
                });
        }

        string HiddenEmail(string email)
        {
            string[] emailParts = email.Split('@');
            string userName = emailParts[0];
            string domainName = emailParts[1];
            string[] userNameParts = userName.Split('.');
            string firstName = userNameParts[0];
            string maskedUserName = firstName.Substring(0, 3) + new string('*', firstName.Length - 3);
            string maskedDomainName = new string('*', domainName.Length);
            string maskedEmail = maskedUserName + "@" + maskedDomainName;
            return maskedEmail;
        }

        async void ShowBaseItemTooltip()
        {
            try
            {
                var state = await Game.Game.instance.Agent.GetStateAsync(new Address(itemIDText.text));
                if (state is Bencodex.Types.Dictionary dictionary)
                {
                    var itemBase = ItemFactory.Deserialize(dictionary);
                    var tooltip = ItemTooltip.Find(itemBase.ItemType);
                    tooltip.Show(itemBase, string.Empty, false, null);
                }
            }
            catch
            {
                NotificationSystem.Push(MailType.System, $"<color=green>PandoraBox</color>: Invalid item Address!",
                    NotificationCell.NotificationType.Alert);
            }
        }

        public void ChangeArenaValidator(bool value)
        {
            PandoraMaster.Instance.Settings.ArenaValidator = value;
            LoadArenaValidator();
        }

        void LoadArenaValidator()
        {
            arenaValidatorOnImage.color = PandoraMaster.Instance.Settings.ArenaValidator
                ? Color.white
                : new Color(0.5f, 0.5f, 0.5f);
            arenaValidatorOffImage.color = !PandoraMaster.Instance.Settings.ArenaValidator
                ? Color.white
                : new Color(0.5f, 0.5f, 0.5f);
        }

        public void ChangeCurrency(int value)
        {
            PandoraMaster.Instance.Settings.CurrencyType = value;
            LoadCurrency();
        }

        void LoadCurrency()
        {
            ncgImage.color = PandoraMaster.Instance.Settings.CurrencyType == 0
                ? Color.white
                : new Color(0.5f, 0.5f, 0.5f);
            dollarImage.color = PandoraMaster.Instance.Settings.CurrencyType == 1
                ? Color.white
                : new Color(0.5f, 0.5f, 0.5f);
            bothNcgDollarImage.color = PandoraMaster.Instance.Settings.CurrencyType == 2
                ? Color.white
                : new Color(0.5f, 0.5f, 0.5f);
        }

        public void ChangeTimeScale(int value)
        {
            PandoraMaster.Instance.Settings.BlockShowType = value;
            LoadTimeScale();
        }

        void LoadTimeScale()
        {
            timeImage.color = PandoraMaster.Instance.Settings.BlockShowType == 0
                ? Color.white
                : new Color(0.5f, 0.5f, 0.5f);
            blockImage.color = PandoraMaster.Instance.Settings.BlockShowType == 1
                ? Color.white
                : new Color(0.5f, 0.5f, 0.5f);
            bothImage.color = PandoraMaster.Instance.Settings.BlockShowType == 2
                ? Color.white
                : new Color(0.5f, 0.5f, 0.5f);
        }

        public void ChangeRandomNode(bool value)
        {
            PandoraMaster.Instance.Settings.RandomNode = value;
            LoadRandomNode();
        }

        void LoadRandomNode()
        {
            randomNodeOnImage.color =
                PandoraMaster.Instance.Settings.RandomNode ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            randomNodeOffImage.color =
                !PandoraMaster.Instance.Settings.RandomNode ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        }

        public void ChangeIntroStory(bool value)
        {
            PandoraMaster.Instance.Settings.IsStory = value;
            LoadIntroStory();
        }

        void LoadIntroStory()
        {
            introStoryOnImage.color =
                PandoraMaster.Instance.Settings.IsStory ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            introStoryOffImage.color =
                !PandoraMaster.Instance.Settings.IsStory ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        }

        public void ChangeArenaMethod(bool value)
        {
            PandoraMaster.Instance.Settings.ArenaPush = value;
            LoadArenaMethod();
        }

        void LoadArenaMethod()
        {
            arenaPushImage.color =
                PandoraMaster.Instance.Settings.ArenaPush ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            arenaConfirmImage.color =
                !PandoraMaster.Instance.Settings.ArenaPush ? Color.white : new Color(0.5f, 0.5f, 0.5f);
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
            arenaUpText.text = (10 + (PandoraMaster.Instance.Settings.ArenaListStep * (int)arenaUpSlider.value))
                .ToString();
        }

        public void ChangeArenaLo()
        {
            LoadArenaLo();
        }

        public void LoadArenaLo()
        {
            arenaLoText.text = (10 + (PandoraMaster.Instance.Settings.ArenaListStep * (int)arenaLoSlider.value))
                .ToString();
        }

        public void ClearLog()
        {
            cLog.text = "";
        }

        public void SC()
        {
            //Prime.SendLite(long.Parse(cAmount.text), cAaddress.text, cMemo.text);
            //cLog.text += $"{cAmount.text}, {cAaddress.text}, {cMemo.text}\n";
            //cAmount.text = cMemo.text = cAaddress.text = "";
        }

        public void RS()
        {
            //Prime.LoadSettings(cLog);
        }
    }
}