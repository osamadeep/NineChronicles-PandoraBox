using UnityEngine;
using DG.Tweening;
using UnityEngine.Audio;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Networking;
using TMPro;
using Nekoyume.State;

namespace Nekoyume.PandoraBox
{
    public class PandoraBoxMaster : MonoBehaviour
    {
        public static PandoraBoxMaster Instance;

        //Unsaved Reg Settings 
        public static string OriginalVersionId = "v100150";
        public static string VersionId = "010038";

        //Pandora Database
        public static PanDatabase PanDatabase;
        public static PandoraPlayer CurrentPandoraPlayer; //data for local player since we use it alot
        public static Guild CurrentGuild; //data for local player since we use it alot
        public static GuildPlayer CurrentGuildPlayer; //data for local player since we use it alot

        //General
        public static int ActionCooldown = 4;
        public static bool MarketPriceHelper = false;
        public static string MarketPriceValue;
        public static int NumberOfProfiles=4;
        public static int LoginIndex;
        public static int ArenaTicketsToUse=1;
        public static List<string> ArenaFavTargets = new List<string>();
        public static bool IsRanking; //raid ranking battle
        public static bool IsRankingSimulate; //simulate ranking battle
        public static bool IsHackAndSlash;  //raid h&s
        public static bool IsHackAndSlashSimulate; //simulate h&s

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
                //CurrentPandoraPlayer = new PandoraPlayer();
                Instance = this;
                Settings = new PandoraSettings();
                Settings.Load();
                StartCoroutine(PandoraDB.GetDatabase());
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

        public static PandoraPlayer GetPandoraPlayer(string address)
        {
            foreach (PandoraPlayer player in PanDatabase.Players)
            {
                if (player.Address.ToLower() == address.ToLower())
                    return player;
            }
            return new PandoraPlayer();
        }

        public static void SetCurrentPandoraPlayer(PandoraPlayer player)
        {
            //Initilize Current player for all Pandora information
            CurrentPandoraPlayer = player;

            //Check for all Errors
            if (CurrentPandoraPlayer.IsBanned)
                Instance.ShowError(101, "This address is Banned, please visit us for more information!");


            CurrentGuildPlayer = null;
            CurrentGuild = null;

            CurrentGuildPlayer = PanDatabase.GuildPlayers.Find(x => x.IsEqual(States.Instance.CurrentAvatarState.address.ToString()));
            if (CurrentGuildPlayer is null)
                return;
            CurrentGuild = PanDatabase.Guilds.Find(x => x.Tag == CurrentGuildPlayer.Guild);


        }

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




    }

    public class PandoraSettings
    {
        //General
        [HideInInspector]
        public bool WhatsNewShown { get; set; } = false;
        [HideInInspector]
        public bool IsStory { get; set; } = true;
        [HideInInspector]
        public bool IsMultipleLogin { get; set; } = true;

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
        public int RaidCooldown { get; set; } = 30;
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
            PlayerPrefs.SetInt("_PandoraBox_General_IsStory", System.Convert.ToInt32(IsStory));
            PlayerPrefs.SetInt("_PandoraBox_General_IsMultipleLogin", System.Convert.ToInt32(IsMultipleLogin));
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
            if (int.Parse(PandoraBoxMaster.VersionId.Substring(0,5)) > int.Parse(PlayerPrefs.GetString("_PandoraBox_Ver").Substring(0, 5)))
            {
                WhatsNewShown = false;
                PlayerPrefs.SetString("_PandoraBox_Ver", PandoraBoxMaster.VersionId);
                //PlayerPrefs.SetInt("_PandoraBox_General_WhatsNewShown", 0); //false

                PlayerPrefs.SetInt("_PandoraBox_General_IsStory", System.Convert.ToInt32(true));
            }

            //General
            //TempVersionId = PlayerPrefs.GetString("_PandoraBox_Ver", TempVersionId);
            WhatsNewShown = System.Convert.ToBoolean(PlayerPrefs.GetInt("_PandoraBox_General_WhatsNewShown", System.Convert.ToInt32(WhatsNewShown)));
            IsStory = System.Convert.ToBoolean(PlayerPrefs.GetInt("_PandoraBox_General_IsStory", System.Convert.ToInt32(IsStory)));
            IsMultipleLogin = System.Convert.ToBoolean(PlayerPrefs.GetInt("_PandoraBox_General_IsMultipleLogin", System.Convert.ToInt32(IsMultipleLogin)));
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
}
