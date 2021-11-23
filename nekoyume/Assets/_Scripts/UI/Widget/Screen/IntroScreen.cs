using Nekoyume.Game.Controller;
using PandoraBox;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

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
            //AudioController.instance.PlayMusic(AudioController.MusicCode.PandoraIntro); //|||||||||||||| PANDORA CODE |||||||||||||||||||
            StartLoading();
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);
            indicator.Close();
        }

        private void StartLoading()
        {
//#if !UNITY_EDITOR
//                        StartCoroutine(CheckVersion()); //|||||||||||||| PANDORA CODE |||||||||||||||||||
//#else
//            var w = Find<LoginSystem>();
//            w.Show(_keyStorePath, _privateKey);
//#endif
            StartCoroutine(CheckVersion()); //|||||||||||||| PANDORA CODE |||||||||||||||||||
            //var w = Find<LoginSystem>();
            //w.Show(_keyStorePath, _privateKey);
        }

        IEnumerator CheckVersion()
        {
            while (PandoraBoxMaster.PanDatabase == null)
            {
                yield return new WaitForSeconds(0.5f);
            }

            if (PandoraBoxMaster.PanDatabase.VersionID == PandoraBoxMaster.VersionId)
            {
                var w = Find<LoginSystem>();
                w.Show(_keyStorePath, _privateKey);
                yield break;
            }

            PandoraBoxMaster.Instance.ShowError(5, "This version is obsolete, please visit us for more information!");

        }

        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
    }
}
