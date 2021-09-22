using UnityEngine;
using DG.Tweening;

namespace PandoraBox
{
    public class PandoraBoxMaster : MonoBehaviour
    {
        public static PandoraBoxMaster Instance;
        public static string VersionId = "v1.0.6";
        public static string OriginalVersionId = "v100078";
        public static string SupportAddress = "0x46528E7DEdaC16951bDccb55B20303AB0c729679";
        public static float PVESpeed = 10f;
        public PandoraSettings Settings;


        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                Settings = new PandoraSettings();
            }
        }

        void LoadSettings()
        {
            DOTween.timeScale = Settings.TweenSpeed;
        }    

        public void SpeedTimeScale(bool isSpeed=true)
        {
            if (isSpeed)
            {
                Time.timeScale = PVESpeed;
                DOTween.timeScale = Settings.TweenSpeed/ PVESpeed;
            }
            else
            {
                Time.timeScale = 1;
                DOTween.timeScale = Settings.TweenSpeed;
            }
        }
    }

    public class PandoraSettings
    {

        [HideInInspector]
        public bool IsTimeOverBlock { get; set; } = true;

        [HideInInspector]
        public bool IsPVESpeed { get; set; } = true;

        [HideInInspector]
        public int ArenaListUpper { get; set; } = 90;

        [HideInInspector]
        public int ArenaListLower { get; set; } = 20;

        [HideInInspector]
        public int TweenSpeed { get; set; } = 3;

        public PandoraSettings()
        {
            Load();
        }


        public void Save()
        {
            PlayerPrefs.SetInt("_PandoraBox_General_isTimeOverBlock", System.Convert.ToInt32(IsTimeOverBlock));
            PlayerPrefs.SetInt("_PandoraBox_General_isPVESpeed", System.Convert.ToInt32(IsPVESpeed));
            PlayerPrefs.SetInt("_PandoraBox_General_TweenSpeed", TweenSpeed);
            PlayerPrefs.SetInt("_PandoraBox_PVP_ListCountLower", ArenaListLower);
            PlayerPrefs.SetInt("_PandoraBox_PVP_ListCountUpper", ArenaListUpper);
        }

        public void Load()
        {
            if (!PlayerPrefs.HasKey("_PandoraBox_General_isTimeOverBlock"))
            {
                Save();
                return;
            }
            IsTimeOverBlock = System.Convert.ToBoolean(PlayerPrefs.GetInt("_PandoraBox_General_isTimeOverBlock", System.Convert.ToInt32(IsTimeOverBlock)));
            IsPVESpeed = System.Convert.ToBoolean(PlayerPrefs.GetInt("_PandoraBox_General_isPVESpeed", System.Convert.ToInt32(IsPVESpeed)));
            TweenSpeed = PlayerPrefs.GetInt("_PandoraBox_General_TweenSpeed", TweenSpeed);
            ArenaListUpper = PlayerPrefs.GetInt("_PandoraBox_PVP_ListCountUpper", ArenaListUpper);
            ArenaListLower = PlayerPrefs.GetInt("_PandoraBox_PVP_ListCountLower", ArenaListLower);
        }


    }
}
