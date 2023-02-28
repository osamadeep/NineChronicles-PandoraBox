using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Nekoyume.PandoraBox
{
    // Wrapper class to serialize/deserialize the list of settings to/from JSON
    [System.Serializable]
    public class ChronoAvatarSettings
    {
        public List<ChronoAvatarSetting> AvatarSettings;
        private const string FileName = "ChronoAvatarSettings.json";

        public void LoadSettings()
        {
            var filePath = Path.Combine(Application.persistentDataPath, FileName);
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                AvatarSettings = JsonUtility.FromJson<ChronoAvatarSettings>(json).AvatarSettings;
            }
            else
            {
                AvatarSettings = new List<ChronoAvatarSetting>();
            }
        }

        public void SaveSettings(ChronoAvatarSetting setting)
        {
            var existingSettingIndex =
                AvatarSettings.FindIndex(s => s.AvatarAddress.ToLower() == setting.AvatarAddress.ToLower());
            if (existingSettingIndex >= 0)
            {
                // Update existing setting
                AvatarSettings[existingSettingIndex] = setting;
            }
            else
            {
                // Add new setting to the list
                AvatarSettings.Add(setting);
            }

            // Serialize the entire list, not just the setting object
            string json = JsonUtility.ToJson(this, true);
            // Write the JSON string to file
            string filePath = Path.Combine(Application.persistentDataPath, FileName);
            File.WriteAllText(filePath, json);
        }

        public ChronoAvatarSetting GetSettings(string address)
        {
            var settingsForAvatar = AvatarSettings.Find(s => s.AvatarAddress.ToLower() == address.ToLower());
            if (settingsForAvatar != null)
            {
                return settingsForAvatar;
            }
            else
            {
                return new ChronoAvatarSetting { AvatarAddress = address };
            }
        }
    }

    [System.Serializable]
    public class ChronoAvatarSetting
    {
        public string AvatarAddress;

        //STAGE
        public int Stage;
        public int StageNotification;
        public int StageIsAutoCollectProsperity;
        public int StageIsAutoSpendProsperity;
        public int StageIsSweepAP;
        public int StageSweepLevelIndex;

        //CRAFT
        public int Craft;
        public int CraftNotification;
        public int CraftIsAutoCombine;
        public int CraftIsUseCrystal;
        public int CraftIsPremium;
        public int CraftItemID;

        //EVENT
        public int Event;
        public int EventNotification;
        public int EventIsAutoSpendTickets;
        public int EventLevelIndex;

        //BOSS
        public int Boss;
        public int BossNotification;
        public int BosstIsAutoSpendTickets;
    }
}