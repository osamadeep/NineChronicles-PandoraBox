using UnityEngine;
using DG.Tweening;

namespace PandoraBox
{
    public class PandoraBoxMaster : MonoBehaviour
    {
        public static PandoraBoxMaster Instance;
        public static string OriginalVersionId = "v100080";
        public static string SupportAddress = "0x46528E7DEdaC16951bDccb55B20303AB0c729679";
        public PandoraSettings Settings;
        public GameObject UISettings;
        public GameObject UIWhatsNew;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                Settings = new PandoraSettings();
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
        
        public string VersionId { get; private set; } = "010008"; // parse v1.0.8
        [HideInInspector]
        public bool WhatsNewShown { get; set; } = false;

        [HideInInspector]
        public bool IsTimeOverBlock { get; set; } = true;

        [HideInInspector]
        public int FightSpeed { get; set; } = 10;
        [HideInInspector]
        public int RaidCooldown { get; set; } = 150;

        [HideInInspector]
        public int ArenaListUpper { get; set; } = 0;

        [HideInInspector]
        public int ArenaListLower { get; set; } = 0;

        public int ArenaListStep { get; set; } = 50;

        [HideInInspector]
        public int MenuSpeed { get; set; } = 3;

        public PandoraSettings()
        {
            Load();
        }


        public void Save()
        {
            PlayerPrefs.SetString("_PandoraBox_Ver", VersionId);
            PlayerPrefs.SetInt("_PandoraBox_General_WhatsNewShown", System.Convert.ToInt32(WhatsNewShown));
            PlayerPrefs.SetInt("_PandoraBox_General_isTimeOverBlock", System.Convert.ToInt32(IsTimeOverBlock));
            PlayerPrefs.SetInt("_PandoraBox_General_MenuSpeed", MenuSpeed);
            PlayerPrefs.SetInt("_PandoraBox_PVE_FightSpeed", FightSpeed);
            PlayerPrefs.SetInt("_PandoraBox_PVE_RaidCooldown", RaidCooldown);
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
            }

            VersionId = PlayerPrefs.GetString("_PandoraBox_Ver", VersionId);
            WhatsNewShown = System.Convert.ToBoolean(PlayerPrefs.GetInt("_PandoraBox_General_WhatsNewShown", System.Convert.ToInt32(WhatsNewShown)));
            IsTimeOverBlock = System.Convert.ToBoolean(PlayerPrefs.GetInt("_PandoraBox_General_isTimeOverBlock", System.Convert.ToInt32(IsTimeOverBlock)));
            MenuSpeed = PlayerPrefs.GetInt("_PandoraBox_General_MenuSpeed", MenuSpeed);
            FightSpeed = PlayerPrefs.GetInt("_PandoraBox_PVE_FightSpeed", FightSpeed);
            RaidCooldown = PlayerPrefs.GetInt("_PandoraBox_PVE_RaidCooldown", RaidCooldown);
            ArenaListUpper = PlayerPrefs.GetInt("_PandoraBox_PVP_ListCountUpper", ArenaListUpper);
            ArenaListLower = PlayerPrefs.GetInt("_PandoraBox_PVP_ListCountLower", ArenaListLower);
            ArenaListStep = PlayerPrefs.GetInt("_PandoraBox_PVP_ListCountStep", ArenaListStep);


            //Load ingame changes
            DOTween.timeScale = MenuSpeed;
        }


    }
}
