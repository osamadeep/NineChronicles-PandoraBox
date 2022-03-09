using Nekoyume.Game.Controller;
using PandoraBox;
using System.Collections;
using UnityEngine;

namespace Nekoyume.UI
{
    public class IntroScreen : LoadingScreen
    {
        private string _keyStorePath;
        private string _privateKey;

        protected override void Awake()
        {
            base.Awake();
            indicator.Close();
        }

        public void Show(string keyStorePath, string privateKey)
        {
            indicator.Show("Verifying transaction..");
            _keyStorePath = keyStorePath;
            _privateKey = privateKey;
            AudioController.instance.PlayMusic(AudioController.MusicCode.Title);
            StartLoading();
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);
            indicator.Close();
        }

        private void StartLoading()
        {
            StartCoroutine(CheckVersion()); //|||||||||||||| PANDORA CODE |||||||||||||||||||
            //var w = Find<LoginSystem>();
            //w.Show(_keyStorePath, _privateKey);
        }

        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        IEnumerator CheckVersion()
        {
            while (PandoraBoxMaster.PanDatabase == null)
            {
                yield return new WaitForSeconds(0.5f);
            }

            bool isAllowedVer = false;
            foreach (string item in PandoraBoxMaster.PanDatabase.AllowedVersions)
            {
                if (item == PandoraBoxMaster.VersionId)
                    isAllowedVer = true;
            }

            if (isAllowedVer)
            {
                var w = Find<LoginSystem>();
                w.Show(_keyStorePath, _privateKey);
                yield break;
            }

            PandoraBoxMaster.Instance.ShowError(5, "This version is obsolete, please visit us for more information!");

        }
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
    }
}
