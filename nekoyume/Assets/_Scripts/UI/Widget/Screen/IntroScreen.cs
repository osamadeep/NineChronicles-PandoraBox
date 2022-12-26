using Nekoyume.Game.Controller;
using Nekoyume.PandoraBox;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class IntroScreen : LoadingScreen
    {
        [SerializeField]
        private Image _background;

        private string _keyStorePath;
        private string _privateKey;

        protected override void Awake()
        {
            base.Awake();
            indicator.Close();
            //_background.sprite = EventManager.GetEventInfo().Intro;
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
            while (PandoraMaster.PanDatabase == null)
            {
                yield return new WaitForSeconds(0.5f);
            }

            bool isAllowedVer = false;
            foreach (string item in PandoraMaster.PanDatabase.AllowedVersions)
            {
                if (item == PandoraMaster.VersionId)
                    isAllowedVer = true;
            }

            if (isAllowedVer)
            {
                var w = Find<LoginSystem>();
                w.Show(_keyStorePath, _privateKey);
                yield break;
            }

            PandoraMaster.Instance.ShowError(5);

        }
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
    }
}
