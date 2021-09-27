using TMPro;
using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Libplanet;
using Libplanet.Crypto;
using Nekoyume.Helper;
using Nekoyume.L10n;
using UniRx;
using UnityEngine.Audio;

namespace Nekoyume.UI
{
    public class Settings : PopupWidget
    {
        public TextMeshProUGUI addressTitleText;
        public TMP_InputField addressContentInputField;
        public Button addressCopyButton;
        public TextMeshProUGUI privateKeyTitleText;
        public TMP_InputField privateKeyContentInputField;
        public Button privateKeyCopyButton;
        public TextMeshProUGUI warningText;
        //public TextMeshProUGUI volumeMasterText;
        //public Slider volumeMasterSlider;
        //public Toggle volumeMasterToggle;

        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        public TextMeshProUGUI volumeMusicText;
        public Slider volumeMusicSlider;
        public Toggle volumeMusicToggle;
        public TextMeshProUGUI volumeSfxText;
        public Slider volumeSfxSlider;
        public Toggle volumeSfxToggle;
        public AudioMixer masterMixer;
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

        public List<TextMeshProUGUI> muteTexts;
        public TextMeshProUGUI resetKeyStoreText;
        public TextMeshProUGUI resetStoreText;
        public TextMeshProUGUI confirmText;
        public TextMeshProUGUI redeemCodeText;
        public Blur blur;
        public RedeemCode redeemCode;
        public Dropdown resolutionDropdown;
        public Toggle windowedToggle;

        private PrivateKey _privateKey;

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            addressTitleText.text = L10nManager.Localize("UI_YOUR_ADDRESS");
            privateKeyTitleText.text = L10nManager.Localize("UI_YOUR_PRIVATE_KEY");
            warningText.text = L10nManager.Localize("UI_ACCOUNT_WARNING");

            //volumeMasterSlider.onValueChanged.AddListener(SetVolumeMaster);
            //volumeMasterToggle.onValueChanged.AddListener(SetVolumeMasterMute);
            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            volumeMusicSlider.onValueChanged.AddListener(SetVolumeMusic);
            volumeMusicToggle.onValueChanged.AddListener(SetVolumeMusicMute);
            volumeSfxSlider.onValueChanged.AddListener(SetVolumeSfx);
            volumeSfxToggle.onValueChanged.AddListener(SetVolumeSfxMute);
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

            resetStoreText.text = L10nManager.Localize("UI_CONFIRM_RESET_STORE_TITLE");
            resetKeyStoreText.text = L10nManager.Localize("UI_CONFIRM_RESET_KEYSTORE_TITLE");
            confirmText.text = L10nManager.Localize("UI_CLOSE");
            redeemCodeText.text = L10nManager.Localize("UI_REDEEM_CODE");

            addressCopyButton.OnClickAsObservable().Subscribe(_ => CopyAddressToClipboard());
            privateKeyCopyButton.OnClickAsObservable().Subscribe(_ => CopyPrivateKeyToClipboard());
            redeemCode.OnRequested.AddListener(() =>
            {
                Close();
            });
            redeemCode.Close();

            InitResolution();
        }


        void InitResolution()
        {
            var settings = Nekoyume.Settings.Instance;
            var options = settings.Resolutions.Select(resolution => $"{resolution.Width} x {resolution.Height}").ToList();
            resolutionDropdown.onValueChanged.AddListener(SetResolution);
            resolutionDropdown.AddOptions(options);
            resolutionDropdown.value = settings.resolutionIndex;
            resolutionDropdown.RefreshShownValue();

            windowedToggle.onValueChanged.AddListener(SetWindowed);
        }
        #endregion

        public override void Show(bool ignoreStartAnimation = false)
        {
            if (!(_privateKey is null))
            {
                addressContentInputField.text = _privateKey.ToAddress().ToString();
                privateKeyContentInputField.text = ByteUtil.Hex(_privateKey.ByteArray);
            }
            else
            {
                if (Game.Game.instance.Agent.PrivateKey is null)
                {
                    addressContentInputField.text = string.Empty;
                    privateKeyContentInputField.text = string.Empty;
                }
                else
                {
                    addressContentInputField.text = Game.Game.instance.Agent.Address.ToString();
                    privateKeyContentInputField.text = ByteUtil.Hex(Game.Game.instance.Agent.PrivateKey.ByteArray);
                }
            }

            var muteString = L10nManager.Localize("UI_MUTE_AUDIO");
            foreach (var text in muteTexts)
            {
                text.text = muteString;
            }

            var settings = Nekoyume.Settings.Instance;
            UpdateSoundSettings();

            //volumeMasterSlider.value = settings.volumeMaster;
            //volumeMasterToggle.isOn = settings.isVolumeMasterMuted;
            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            volumeMusicSlider.value = settings.volumeMusic;
            volumeMusicToggle.isOn = settings.isVolumeMusicMuted;
            volumeSfxSlider.value = settings.volumeSfx;
            volumeSfxToggle.isOn = settings.isVolumeSfxMuted;
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

            windowedToggle.isOn = settings.isWindowed;

            base.Show(ignoreStartAnimation);

            if (blur)
            {
                blur.Show();
            }
        }

        public void ApplyCurrentSettings()
        {
            Nekoyume.Settings.Instance.ApplyCurrentSettings();
            Close();
        }

        public void RevertSettings()
        {
            Nekoyume.Settings.Instance.ReloadSettings();
            UpdateSoundSettings();
            Close();
        }

        public void UpdateSoundSettings()
        {
            var settings = Nekoyume.Settings.Instance;
            //SetVolumeMaster(settings.volumeMaster);
            //SetVolumeMasterMute(settings.isVolumeMasterMuted);
            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            SetVolumeMusic(settings.volumeMusic);
            SetVolumeMusicMute(settings.isVolumeMusicMuted);
            SetVolumeSfx(settings.volumeSfx);
            SetVolumeSfxMute(settings.isVolumeSfxMuted);
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
        }

        public void UpdateResolution()
        {

        }

        public void UpdatePrivateKey(string privateKeyHex)
        {
            if (!string.IsNullOrEmpty(privateKeyHex))
            {
                _privateKey = new PrivateKey(ByteUtil.ParseHex(privateKeyHex));
            }
        }

        private void CopyAddressToClipboard()
        {
            ClipboardHelper.CopyToClipboard(addressContentInputField.text);

            // todo: 복사되었습니다. 토스트.
        }

        private void CopyPrivateKeyToClipboard()
        {
            ClipboardHelper.CopyToClipboard(privateKeyContentInputField.text);

            // todo: 복사되었습니다. 토스트.
        }

        //private void SetVolumeMaster(float value)
        //{
        //    var settings = Nekoyume.Settings.Instance;
        //    settings.volumeMaster = value;
        //    AudioListener.volume = settings.isVolumeMasterMuted ? 0f : settings.volumeMaster;
        //    UpdateVolumeMasterText();
        //}

        //private void SetVolumeMasterMute(bool value)
        //{
        //    var settings = Nekoyume.Settings.Instance;
        //    settings.isVolumeMasterMuted = value;
        //    AudioListener.volume = value ? 0f : settings.volumeMaster;
        //    UpdateVolumeMasterText();
        //}

        //private void UpdateVolumeMasterText()
        //{
        //    var volumeString = Mathf.Approximately(AudioListener.volume, 0.0f) ?
        //        L10nManager.Localize("UI_MUTE_AUDIO") : $"{Mathf.CeilToInt(AudioListener.volume * 100.0f)}%";
        //    //volumeMasterText.text = $"{L10nManager.Localize("UI_MASTER_VOLUME")} : {volumeString}";
        //}

        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        private void SetVolumeMusic(float value)
        {
            var settings = Nekoyume.Settings.Instance;
            settings.volumeMusic = value;
            if (!settings.isVolumeMusicMuted)
                masterMixer.SetFloat("MusicVolume", Mathf.Lerp(-20f,0, value));
            UpdateVolumeMusicText(settings.isVolumeMusicMuted ? 0f : settings.volumeMusic);
        }

        private void SetVolumeMusicMute(bool value)
        {
            var settings = Nekoyume.Settings.Instance;
            settings.isVolumeMusicMuted = value;
            masterMixer.SetFloat("MusicVolume", value ? -80f : settings.volumeMusic);
            UpdateVolumeMusicText(value ? 0f : settings.volumeMusic);
        }

        private void UpdateVolumeMusicText(float value)
        {
            var volumeString = Mathf.Approximately(value, 0.0f) ?
                L10nManager.Localize("UI_MUTE_AUDIO") : $"{Mathf.CeilToInt(value * 100.0f)}%";
            volumeMusicText.text = $"Music Volume : {volumeString}";
        }

        private void SetVolumeSfx(float value)
        {
            var settings = Nekoyume.Settings.Instance;
            settings.volumeSfx = value;
            if (!settings.isVolumeSfxMuted)
                masterMixer.SetFloat("SfxVolume", Mathf.Lerp(-20f, 0, value));
            UpdateVolumeSfxText(settings.isVolumeSfxMuted ? 0f : settings.volumeSfx);
        }

        private void SetVolumeSfxMute(bool value)
        {
            var settings = Nekoyume.Settings.Instance;
            settings.isVolumeSfxMuted = value;
            masterMixer.SetFloat("SfxVolume", value ? -80f : settings.volumeMusic);
            UpdateVolumeSfxText(value ? 0f : settings.volumeSfx);
        }

        private void UpdateVolumeSfxText(float value)
        {
            var volumeString = Mathf.Approximately(value, 0.0f) ?
                L10nManager.Localize("UI_MUTE_AUDIO") : $"{Mathf.CeilToInt(value * 100.0f)}%";
            volumeSfxText.text = $"SoundFX Volume : {volumeString}";
        }
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

        //private void SetVolumeSfx(float value)
        //{
        //    var settings = Nekoyume.Settings.Instance;
        //    settings.volumeSfx = value;
        //}

        //private void SetVolumeSfxMute(bool value)
        //{
        //    var settings = Nekoyume.Settings.Instance;
        //    settings.isVolumeSfxMuted = value;
        //}

        public void SetResolution(int index)
        {
            var settings = Nekoyume.Settings.Instance;
            settings.resolutionIndex = index;
            settings.ApplyCurrentResolution();
        }

        public void SetWindowed(bool value)
        {
            var settings = Nekoyume.Settings.Instance;
            settings.isWindowed = value;
            settings.ApplyCurrentResolution();
        }

        public void ResetStore()
        {
            Game.Game.instance.ResetStore();
        }

        public void ResetKeyStore()
        {
            Game.Game.instance.ResetKeyStore();
        }

        public void RedeemCode()
        {
            redeemCode.Show();
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            if (blur)
            {
                blur.Close();
            }

            base.Close(ignoreCloseAnimation);
        }
    }
}
