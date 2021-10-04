using mixpanel;
using Nekoyume.L10n;
using PandoraBox;
using TMPro;
using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;

namespace Nekoyume.UI
{
    public class Intro : LoadingScreen
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
        }

        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        IEnumerator CheckVersion()
        {
            string url = URLAntiCacheRandomizer("https://6wrni.com/9c.pandora");
            UnityWebRequest www = UnityWebRequest.Get(url);
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                ErrorWindow.Find("Message").GetComponent<TextMeshProUGUI>().text = "This version of Pandora Mod is outdated. please visit us for more information!";
                ErrorWindow.gameObject.SetActive(true);
            }
            else
            {
                PandoraVersion myObject = new PandoraVersion();
                try
                { myObject = JsonUtility.FromJson<PandoraVersion>(www.downloadHandler.text); }
                catch { }

                if (myObject.ID == PandoraBoxMaster.Instance.Settings.VersionId)
                    if (myObject.IsAvailable)
                    {
                        var w = Find<LoginPopup>();
                        w.Show(_keyStorePath, _privateKey);
                        yield break;
                    }

                string temp = "";
                if (myObject.Reason == "")
                    temp = "This version of Pandora Mod is outdated. please visit us for more information!";
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
