using UnityEngine;
using DG.Tweening;
using UnityEngine.Audio;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Networking;
using TMPro;

namespace PandoraBox
{
    public class PandoraBoxMaster : MonoBehaviour
    {
        public static PandoraBoxMaster Instance;

        //Unsaved Reg Settings 
        public static string OriginalVersionId = "v100098";
        public static string VersionId = "010024";
        public static PanDatabase PanDatabase;
        public static PanPlayer CurrentPanPlayer;
        public static string SupportAddress = "0x46528E7DEdaC16951bDccb55B20303AB0c729679";
        public static int ActionCooldown = 3;
        public static bool MarketPriceHelper = false;
        public static string MarketPriceValue;
        public static int NumberOfProfiles=4;
        public static int LoginIndex;
        public static int ArenaTicketsToUse=1;
        public static List<string> ArenaFavTargets = new List<string>();
        public static bool IsSimulate;

        //Objects
        public PandoraSettings Settings;
        public GameObject UIErrorWindow;
        public GameObject UISettings;
        public GameObject UIWhatsNew;
        public AudioMixer Audiomixer;
        public GameObject CosmicSword;
        public Sprite CosmicIcon;

        private void Awake()
        {
            if (Instance == null)
            {
                CurrentPanPlayer = new PanPlayer();
                Instance = this;
                Settings = new PandoraSettings();
                Settings.Load();
                StartCoroutine(GetDatabase());
            }
        }

        public void SpeedTimeScale(int speed=1)
        {
            if (speed != 1)
            {
                Time.timeScale = Settings.FightSpeed;
                DOTween.timeScale = Settings.MenuSpeed/ Settings.FightSpeed;
            }
            else
            {
                Time.timeScale = 1;
                DOTween.timeScale = 1;
            }
        }

        //public bool IsPremium(string address)
        //{
        //    List<string> addresses = new List<string>();
        //    addresses.Add("0x46528E7DEdaC16951bDccb55B20303AB0c729679"); //s
        //    addresses.Add("0x1012041FF2254f43d0a938aDF89c3f11867A2A58"); //lambo

        //    return addresses.Contains(address);
        //}

        //public bool IsRBG(string address)
        //{
        //    List<string> addresses = new List<string>();
        //    addresses.Add("0x1012041FF2254f43d0a938aDF89c3f11867A2A58"); //lambo
        //    addresses.Add("0xC0bA278CB8379683E66C28928fa0Aa8bfF3D95E6"); //Wabbs

        //    return addresses.Contains(address);
        //}

        public static PanPlayer GetPanPlayer(string address)
        {
            foreach (PanPlayer player in PanDatabase.Players)
            {
                if (player.Address.ToLower() == address.ToLower())
                    return player;
            }
            return new PanPlayer();
        }

        //public bool IsHalloween(string address)
        //{
        //    List<string> addresses = new List<string>();
        //    addresses.Add("0xd7ECE10ddAFc34e964c61Ad11c199C3BF41Dc403"); //bmcdee

        //    return addresses.Contains(address);
        //}

        public void ShowError(int errorNumber, string text)
        {
            //404 cannot get the link
            //16 cannot cast the link content
            //5 old version
            //101 player banned
            UIErrorWindow.transform.Find("TitleTxt").GetComponent<TextMeshProUGUI>().text = $"Error <color=red>{errorNumber}</color>!";
            UIErrorWindow.transform.Find("MessageTxt").GetComponent<TextMeshProUGUI>().text = text;
            UIErrorWindow.SetActive(true);
        }

        IEnumerator GetDatabase()
        {
#if !UNITY_EDITOR
            string url = URLAntiCacheRandomizer("https://6wrni.com/9c.pandora");
#else
            string url = URLAntiCacheRandomizer("https://6wrni.com/9c.pandora");//9cdev.pandora
#endif
            UnityWebRequest www = UnityWebRequest.Get(url);
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                ShowError(404,"Cannot connect to Pandora Server, please visit us for more information!");
            }
            else
            {
                try
                {
                    PanDatabase = JsonUtility.FromJson<PanDatabase>(www.downloadHandler.text);
                }// Debug.LogError(JsonUtility.ToJson(PanDatabase)); }
                catch { ShowError(16, "Something wrong, please visit us for more information!"); }
            }
        }

        //IEnumerator CheckVersion()
        //{
        //    string url = URLAntiCacheRandomizer("https://6wrni.com/9c.pandora");
        //    UnityWebRequest www = UnityWebRequest.Get(url);
        //    yield return www.SendWebRequest();

        //    if (www.result != UnityWebRequest.Result.Success)
        //    {
        //        UIErrorWindow.transform.Find("Message").GetComponent<TextMeshProUGUI>().text = "Something is wrong. please visit us for more information!";
        //        ErrorWindow.gameObject.SetActive(true);
        //    }
        //    else
        //    {
        //        PanDatabase myObject = new PanDatabase();
        //        try
        //        { myObject = JsonUtility.FromJson<PanDatabase>(www.downloadHandler.text); }
        //        catch { }

        //        if (myObject.VersionID == PandoraBoxMaster.VersionId)
        //        {
        //            var w = Find<LoginSystem>();
        //            w.Show(_keyStorePath, _privateKey);
        //            yield break;
        //        }

        //        string temp = "Something is wrong. please visit us for more information!";

        //        ErrorWindow.Find("Message").GetComponent<TextMeshProUGUI>().text = temp;
        //        ErrorWindow.gameObject.SetActive(true);
        //    }
        //}
        public string URLAntiCacheRandomizer(string url)
        {
            string r = "";
            r += UnityEngine.Random.Range(
                          1000000, 8000000).ToString();
            string result = url + "?p=" + r;
            return result;
        }

    }

    public class PandoraSettings
    {
        //General
        //[HideInInspector]
        //public string TempVersionId { get; private set; } //value come from Online settings
        [HideInInspector]
        public bool WhatsNewShown { get; set; } = false;

        [HideInInspector]
        public int BlockShowType { get; set; } = 0;
        [HideInInspector]
        public int MenuSpeed { get; set; } = 3;

        //[HideInInspector]
        //public float MusicVolume { get; set; } = 0.7f;

        //[HideInInspector]
        //public float SfxVolume { get; set; } = 0.7f;

        //[HideInInspector]
        //public bool IsMusicMuted { get; set; } = false;

        //[HideInInspector]
        //public bool IsSfxMuted { get; set; } = false;

        //[HideInInspector]
        //public int ResolutionIndex { get; set; } = 2;



        //PVE
        [HideInInspector]
        public int FightSpeed { get; set; } = 1;
        [HideInInspector]
        public int RaidCooldown { get; set; } = 150;
        [HideInInspector]
        public bool RaidMethodIsSweep { get; set; }

        //PVP

        [HideInInspector]
        public int ArenaListUpper { get; set; } = 0;

        [HideInInspector]
        public int ArenaListLower { get; set; } = 0;

        [HideInInspector]
        public int ArenaListStep { get; set; } = 90;

        public void Save()
        {
            //General
            PlayerPrefs.SetString("_PandoraBox_Ver", PandoraBoxMaster.VersionId);
            PlayerPrefs.SetInt("_PandoraBox_General_WhatsNewShown", System.Convert.ToInt32(WhatsNewShown));
            PlayerPrefs.SetInt("_PandoraBox_General_BlockShowType", BlockShowType);
            PlayerPrefs.SetInt("_PandoraBox_General_MenuSpeed", MenuSpeed);
            //PlayerPrefs.SetFloat("_PandoraBox_General_MusicVolume", MusicVolume);
            //PlayerPrefs.SetInt("_PandoraBox_General_IsMusicMuted", System.Convert.ToInt32(IsMusicMuted));
            //PlayerPrefs.SetFloat("_PandoraBox_General_SfxVolume", SfxVolume);
            //PlayerPrefs.SetInt("_PandoraBox_General_IsSfxMuted", System.Convert.ToInt32(IsSfxMuted));
            //PlayerPrefs.SetInt("_PandoraBox_General_ResolutionIndex", ResolutionIndex);

            //PVE
            PlayerPrefs.SetInt("_PandoraBox_PVE_FightSpeed", FightSpeed);
            PlayerPrefs.SetInt("_PandoraBox_PVE_RaidCooldown", RaidCooldown);
            PlayerPrefs.SetInt("_PandoraBox_PVE_RaidMethodIsSweep", System.Convert.ToInt32(RaidMethodIsSweep));

            //PVP
            PlayerPrefs.SetInt("_PandoraBox_PVP_ListCountLower", ArenaListLower);
            PlayerPrefs.SetInt("_PandoraBox_PVP_ListCountUpper", ArenaListUpper);
            PlayerPrefs.SetInt("_PandoraBox_PVP_ListCountStep", ArenaListStep);

            //apply ingame changes
            DOTween.timeScale = MenuSpeed;
        }

        public void Load()
        {
            if (!PlayerPrefs.HasKey("_PandoraBox_Ver"))
            {
                Save();
                return;
            }

            //check difference
            if (int.Parse(PandoraBoxMaster.VersionId) > int.Parse(PlayerPrefs.GetString("_PandoraBox_Ver")))
            {
                WhatsNewShown = false;
                PlayerPrefs.SetString("_PandoraBox_Ver", PandoraBoxMaster.VersionId);
                PlayerPrefs.SetInt("_PandoraBox_General_WhatsNewShown", 0); //false

            }

            //General
            //TempVersionId = PlayerPrefs.GetString("_PandoraBox_Ver", TempVersionId);
            WhatsNewShown = System.Convert.ToBoolean(PlayerPrefs.GetInt("_PandoraBox_General_WhatsNewShown", System.Convert.ToInt32(WhatsNewShown)));
            //IsMusicMuted = System.Convert.ToBoolean(PlayerPrefs.GetInt("_PandoraBox_General_IsMusicMuted", System.Convert.ToInt32(IsMusicMuted)));
            //IsSfxMuted = System.Convert.ToBoolean(PlayerPrefs.GetInt("_PandoraBox_General_IsSfxMuted", System.Convert.ToInt32(IsSfxMuted)));
            //MusicVolume = PlayerPrefs.GetFloat("_PandoraBox_General_MusicVolume", MusicVolume);
            //SfxVolume = PlayerPrefs.GetFloat("_PandoraBox_General_SfxVolume", SfxVolume);
            //ResolutionIndex = PlayerPrefs.GetInt("_PandoraBox_General_ResolutionIndex", ResolutionIndex);
            BlockShowType = PlayerPrefs.GetInt("_PandoraBox_General_BlockShowType", BlockShowType);
            MenuSpeed = PlayerPrefs.GetInt("_PandoraBox_General_MenuSpeed", MenuSpeed);

            //PVE
            FightSpeed = PlayerPrefs.GetInt("_PandoraBox_PVE_FightSpeed", FightSpeed);
            RaidCooldown = PlayerPrefs.GetInt("_PandoraBox_PVE_RaidCooldown", RaidCooldown);
            RaidMethodIsSweep = System.Convert.ToBoolean(PlayerPrefs.GetInt("_PandoraBox_PVE_RaidMethodIsSweep", System.Convert.ToInt32(RaidMethodIsSweep)));

            //PVP
            ArenaListUpper = PlayerPrefs.GetInt("_PandoraBox_PVP_ListCountUpper", ArenaListUpper);
            ArenaListLower = PlayerPrefs.GetInt("_PandoraBox_PVP_ListCountLower", ArenaListLower);
            ArenaListStep = PlayerPrefs.GetInt("_PandoraBox_PVP_ListCountStep", ArenaListStep);


            //Load ingame changes
            DOTween.timeScale = MenuSpeed;
        }


    }

[System.Serializable]
public class PanDatabase
{
    public string VersionID;
    public List<PanPlayer> Players;
}

    [System.Serializable]
    public class PanPlayer
    {
        public string Address;
        public bool IsBanned;
        public bool IsPremium;
        public bool IsProtected;
        public bool IsIgnoringMessage;
        public string DiscordID;
        public int PremiumEndBlock;
        public int ArenaBanner;
        public int ArenaIcon;
        public int SwordSkin;
        public int FriendViewSkin;
    }
}
