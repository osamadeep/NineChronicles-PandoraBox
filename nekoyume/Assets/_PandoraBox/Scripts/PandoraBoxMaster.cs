using UnityEngine;
using DG.Tweening;
using UnityEngine.Audio;

namespace PandoraBox
{
    public class PandoraBoxMaster : MonoBehaviour
    {
        public static PandoraBoxMaster Instance;

        //Unsaved Reg Settings 
        public static string OriginalVersionId = "v100080";
        public static string SupportAddress = "0x46528E7DEdaC16951bDccb55B20303AB0c729679";
        public static int ActionCooldown = 2;

        //Objects
        public PandoraSettings Settings;
        public GameObject UISettings;
        public GameObject UIWhatsNew;
        public AudioMixer Audiomixer;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                Settings = new PandoraSettings();
                Settings.Load();
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
    }

    public class PandoraSettings
    {
        //General
        [HideInInspector]
        public string VersionId { get; private set; } = "010009"; // parse v1.0.#
        [HideInInspector]
        public bool WhatsNewShown { get; set; } = false;

        [HideInInspector]
        public int BlockShowType { get; set; } = 0;
        [HideInInspector]
        public int MenuSpeed { get; set; } = 3;

        [HideInInspector]
        public float MusicVolume { get; set; } = 0.7f;

        [HideInInspector]
        public float SfxVolume { get; set; } = 0.7f;

        [HideInInspector]
        public bool IsMusicMuted { get; set; } = false;

        [HideInInspector]
        public bool IsSfxMuted { get; set; } = false;

        [HideInInspector]
        public int ResolutionIndex { get; set; } = 2;



        //PVE
        [HideInInspector]
        public int FightSpeed { get; set; } = 1;
        [HideInInspector]
        public int RaidCooldown { get; set; } = 150;

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
            PlayerPrefs.SetString("_PandoraBox_Ver", VersionId);
            PlayerPrefs.SetInt("_PandoraBox_General_WhatsNewShown", System.Convert.ToInt32(WhatsNewShown));
            PlayerPrefs.SetInt("_PandoraBox_General_BlockShowType", BlockShowType);
            PlayerPrefs.SetInt("_PandoraBox_General_MenuSpeed", MenuSpeed);
            PlayerPrefs.SetFloat("_PandoraBox_General_MusicVolume", MusicVolume);
            PlayerPrefs.SetInt("_PandoraBox_General_IsMusicMuted", System.Convert.ToInt32(IsMusicMuted));
            PlayerPrefs.SetFloat("_PandoraBox_General_SfxVolume", SfxVolume);
            PlayerPrefs.SetInt("_PandoraBox_General_IsSfxMuted", System.Convert.ToInt32(IsSfxMuted));
            PlayerPrefs.SetInt("_PandoraBox_General_ResolutionIndex", ResolutionIndex);

            //PVE
            PlayerPrefs.SetInt("_PandoraBox_PVE_FightSpeed", FightSpeed);
            PlayerPrefs.SetInt("_PandoraBox_PVE_RaidCooldown", RaidCooldown);

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
            if (int.Parse(VersionId) > int.Parse(PlayerPrefs.GetString("_PandoraBox_Ver")))
            {
                WhatsNewShown = false;
                PlayerPrefs.SetString("_PandoraBox_Ver", VersionId);
                PlayerPrefs.SetInt("_PandoraBox_General_WhatsNewShown", 0); //false

                //revert Original client setting to defauly
                PlayerPrefs.SetFloat("SETTINGS_VOLUME_MASTER", 0.7f);
                PlayerPrefs.SetInt("SETTINGS_VOLUME_MASTER_ISMUTED", 0);
                PlayerPrefs.SetInt("SETTINGS_RESOLUTION_INDEX", 0);

                //fix speed issue later
                PlayerPrefs.SetInt("_PandoraBox_PVE_FightSpeed", 1);
            }

            //General
            VersionId = PlayerPrefs.GetString("_PandoraBox_Ver", VersionId);
            WhatsNewShown = System.Convert.ToBoolean(PlayerPrefs.GetInt("_PandoraBox_General_WhatsNewShown", System.Convert.ToInt32(WhatsNewShown)));
            IsMusicMuted = System.Convert.ToBoolean(PlayerPrefs.GetInt("_PandoraBox_General_IsMusicMuted", System.Convert.ToInt32(IsMusicMuted)));
            IsSfxMuted = System.Convert.ToBoolean(PlayerPrefs.GetInt("_PandoraBox_General_IsSfxMuted", System.Convert.ToInt32(IsSfxMuted)));
            MusicVolume = PlayerPrefs.GetFloat("_PandoraBox_General_MusicVolume", MusicVolume);
            SfxVolume = PlayerPrefs.GetFloat("_PandoraBox_General_SfxVolume", SfxVolume);
            BlockShowType = PlayerPrefs.GetInt("_PandoraBox_General_BlockShowType", BlockShowType);
            MenuSpeed = PlayerPrefs.GetInt("_PandoraBox_General_MenuSpeed", MenuSpeed);
            ResolutionIndex = PlayerPrefs.GetInt("_PandoraBox_General_ResolutionIndex", ResolutionIndex);

            //PVE
            FightSpeed = PlayerPrefs.GetInt("_PandoraBox_PVE_FightSpeed", FightSpeed);
            RaidCooldown = PlayerPrefs.GetInt("_PandoraBox_PVE_RaidCooldown", RaidCooldown);

            //PVP
            ArenaListUpper = PlayerPrefs.GetInt("_PandoraBox_PVP_ListCountUpper", ArenaListUpper);
            ArenaListLower = PlayerPrefs.GetInt("_PandoraBox_PVP_ListCountLower", ArenaListLower);
            ArenaListStep = PlayerPrefs.GetInt("_PandoraBox_PVP_ListCountStep", ArenaListStep);


            //Load ingame changes
            DOTween.timeScale = MenuSpeed;
            PandoraBoxMaster.Instance.Audiomixer.SetFloat("MusicVolume", IsMusicMuted ? -80f : Mathf.Lerp(-20f, 0, MusicVolume));
            PandoraBoxMaster.Instance.Audiomixer.SetFloat("SfxVolume", IsSfxMuted ? -80f : Mathf.Lerp(-20f, 0, SfxVolume));
        }


    }
}
