using PandoraBox;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume
{
    public class Settings
    {
        public static Settings Instance => (_instance is null) ? _instance = new Settings() : _instance;
        private static Settings _instance;

        private const string VolumeMasterKey = "SETTINGS_VOLUME_MASTER";
        private const string VolumeMasterIsMutedKey = "SETTINGS_VOLUME_MASTER_ISMUTED";
        private const string VolumeMusicKey = "_PandoraBox_General_MusicVolume";
        private const string VolumeMusicIsMutedKey = "_PandoraBox_General_IsMusicMuted";
        private const string VolumeSfxKey = "_PandoraBox_General_SfxVolume";
        private const string VolumeSfxIsMutedKey = "_PandoraBox_General_IsSfxMuted";
        private const string ResolutionIndexKey = "_PandoraBox_General_ResolutionIndex";
        private const string ResolutionWindowedKey = "SETTINGS_WINDOWED";

        //public float volumeMaster;
        //public float volumeMusic;
        //public float volumeSfx;
        public int resolutionIndex = 0;

        //public bool isVolumeMasterMuted;
        //public bool isVolumeMusicMuted;
        //public bool isVolumeSfxMuted;
        public bool isWindowed = true;

        public class Resolution
        {
            public int Width { get; }
            public int Height { get; }

            public Resolution(int width, int height)
            {
                Width = width;
                Height = height;
            }
        }

        //|||||||||||||| PANDORA CODE |||||||||||||||||||
        public readonly List< Resolution> Resolutions = new List< Resolution>()
        {
            {new Resolution(640, 360)},
            {new Resolution(960, 540)},
            {new Resolution(1176, 664)},
            {new Resolution(1280, 720)},
            {new Resolution(1366, 768)},
            {new Resolution(1600, 900)},
            {new Resolution(1920, 1080)},
            {new Resolution(2560, 1440)},
            {new Resolution(3840, 2160)},
        };

        /// <summary>
        /// 무조건 메인 스레드에서 동작해야 함.
        /// </summary>
        public Settings()
        {
            ReloadSettings();
        }

        public void ReloadSettings()
        {
            ////volumeMaster = PlayerPrefs.GetFloat(VolumeMasterKey, 1f);
            //volumeMusic = PandoraBoxMaster.Instance.Settings.MusicVolume;
            //volumeSfx = PlayerPrefs.GetFloat("_PandoraBox_General_SfxVolume", 1f);

            ////isVolumeMasterMuted = PlayerPrefs.GetInt(VolumeMasterIsMutedKey, 0) == 0 ? false : true;
            //isVolumeMusicMuted = PlayerPrefs.GetInt(VolumeMusicIsMutedKey, 0) == 0 ? false : true;
            //isVolumeSfxMuted = PlayerPrefs.GetInt(VolumeSfxIsMutedKey, 0) == 0 ? false : true;

            resolutionIndex = PlayerPrefs.GetInt(ResolutionIndexKey, 0);
            isWindowed = PlayerPrefs.GetInt(ResolutionWindowedKey, 1) == 1 ? true : false;
            SetResolution();
        }

        public void ApplyCurrentSettings()
        {
            //PlayerPrefs.SetFloat(VolumeMasterKey, volumeMaster);
            PlayerPrefs.SetFloat(VolumeMusicKey, PandoraBoxMaster.Instance.Settings.MusicVolume);
            PlayerPrefs.SetFloat(VolumeSfxKey, PandoraBoxMaster.Instance.Settings.SfxVolume);

            //PlayerPrefs.SetInt(VolumeMasterIsMutedKey, isVolumeMasterMuted ? 1 : 0);
            PlayerPrefs.SetInt(VolumeMusicIsMutedKey, PandoraBoxMaster.Instance.Settings.IsMusicMuted ? 1 : 0);
            PlayerPrefs.SetInt(VolumeSfxIsMutedKey, PandoraBoxMaster.Instance.Settings.IsSfxMuted ? 1 : 0);
        }

        public void ApplyCurrentResolution()
        {
            PlayerPrefs.SetInt(ResolutionIndexKey, PandoraBoxMaster.Instance.Settings.ResolutionIndex);
            PlayerPrefs.SetInt(ResolutionWindowedKey, isWindowed ? 1 : 0);
            SetResolution();
        }

        private void SetResolution()
        {
            Screen.SetResolution(Resolutions[PandoraBoxMaster.Instance.Settings.ResolutionIndex].Width, Resolutions[PandoraBoxMaster.Instance.Settings.ResolutionIndex].Height, !isWindowed);
        }
    }
}
