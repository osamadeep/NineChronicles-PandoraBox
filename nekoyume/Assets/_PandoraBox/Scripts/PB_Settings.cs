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

namespace PandoraBox
{
    public class PB_Settings : Nekoyume.UI.PopupWidget
    {
        public TMP_InputField addressContentInputField;
        public Button addressCopyButton;
        public Nekoyume.UI.Blur blur;
        //public Toggle windowedToggle;

        #region Mono

        protected override void Awake()
        {
            base.Awake();
            addressCopyButton.OnClickAsObservable().Subscribe(_ => CopyAddressToClipboard());
        }
        #endregion

        public override void Show(bool ignoreStartAnimation = false)
        {
            //addressContentInputField.text = PandoraBoxMaster.supportAddress;
            base.Show(ignoreStartAnimation);
            if (blur)
            {
                blur.Show();
            }
        }

        public void ApplyCurrentSettings()
        {
            //Nekoyume.Settings.Instance.ApplyCurrentSettings();
            Close();
        }

        public void RevertSettings()
        {
            //Nekoyume.Settings.Instance.ReloadSettings();
            //UpdateSoundSettings();
            Close();
        }

        private void CopyAddressToClipboard()
        {
            ClipboardHelper.CopyToClipboard(addressContentInputField.text);

            // todo: 복사되었습니다. 토스트.
        }


        public void SetWindowed(bool value)
        {
            //var settings = Nekoyume.Settings.Instance;
            //settings.isWindowed = value;
            //settings.ApplyCurrentResolution();
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
