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
        public Transform ErrorWindow; //|||||||||||||| PANDORA CODE |||||||||||||||||||

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
            AudioController.instance.PlayMusic(AudioController.MusicCode.PandoraIntro); //|||||||||||||| PANDORA CODE |||||||||||||||||||
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
//            StartCoroutine(CheckVersion()); //|||||||||||||| PANDORA CODE |||||||||||||||||||
//#else
//            var w = Find<LoginSystem>();
//            w.Show(_keyStorePath, _privateKey);
//#endif
StartCoroutine(CheckVersion()); //|||||||||||||| PANDORA CODE |||||||||||||||||||
            //var w = Find<LoginSystem>();
            //w.Show(_keyStorePath, _privateKey);
        }

        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        IEnumerator CheckVersion()
        {
            string url = URLAntiCacheRandomizer("https://6wrni.com/9c.pandora");
            UnityWebRequest www = UnityWebRequest.Get(url);
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                ErrorWindow.Find("Message").GetComponent<TextMeshProUGUI>().text = "Something is wrong. please visit us for more information!";
                ErrorWindow.gameObject.SetActive(true);
            }
            else
            {
                PandoraVersion myObject = new PandoraVersion();
                try
                { myObject = JsonUtility.FromJson<PandoraVersion>(www.downloadHandler.text); }
                catch { }

                if (myObject.ID == PandoraBoxMaster.VersionId)
                    if (myObject.IsAvailable)
                    {
                        var w = Find<LoginSystem>();
                        w.Show(_keyStorePath, _privateKey);
                        yield break;
                    }

                string temp = "";
                if (myObject.Reason == "")
                    temp = "Something is wrong. please visit us for more information!";
                else
                    temp = myObject.Reason;

                ErrorWindow.Find("Message").GetComponent<TextMeshProUGUI>().text = temp;
                ErrorWindow.gameObject.SetActive(true);
            }
        }
        public string URLAntiCacheRandomizer(string url)
        {
            string r = "";
            r += UnityEngine.Random.Range(
                          1000000, 8000000).ToString();
            r += UnityEngine.Random.Range(
                          1000000, 8000000).ToString();
            string result = url + "?p=" + r;
            return result;
        }
    }

    [System.Serializable]
    public class PanVersions
    {
        public List<PandoraVersion> Versions;
    }

    [System.Serializable]
    public class PandoraVersion
    {
        public string ID;
        public bool IsAvailable;
        public string Reason;
    }
    //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
}
