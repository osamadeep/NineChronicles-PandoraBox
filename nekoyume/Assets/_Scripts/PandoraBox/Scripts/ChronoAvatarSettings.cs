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
        public bool Stage;
        public bool StageNotification;
        public bool StageIsAutoCollectProsperity;
        public bool StageIsAutoSpendProsperity;
        public bool StageIsSweepAP;
        public int StageSweepLevelIndex;

        //CRAFT
        public bool Craft;
        public bool CraftNotification;
        public bool CraftIsAutoCombine;
        public bool CraftIsUseCrystal;
        public bool CraftIsPremium;
        public int CraftItemID;

        //EVENT
        public bool Event;
        public bool EventNotification;
        public bool EventIsAutoSpendTickets;
        public int EventLevelIndex;

        //BOSS
        public bool Boss;
        public bool BossNotification;
        public bool BosstIsAutoSpendTickets;
        public bool BosstIsAutoCollectRewards;
    }
}