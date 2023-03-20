using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Nekoyume.PandoraBox
{
    [System.Serializable]
    public class LoginSlotsSettings
    {
        public List<LoginSlotSettings> Slots;
        private const string FileName = "LoginSlotsSettings.json";

        public void LoadSettings()
        {
            var filePath = Path.Combine(Application.persistentDataPath, FileName);
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                Slots = JsonUtility.FromJson<LoginSlotsSettings>(json).Slots;
            }
            else
            {
                Slots = new List<LoginSlotSettings>();
            }
        }

        public void SaveSettings(LoginSlotSettings setting)
        {
            var existingSettingIndex =
                Slots.FindIndex(s => s.Index == setting.Index);
            if (existingSettingIndex >= 0)
            {
                // Update existing setting
                Slots[existingSettingIndex] = setting;
            }
            else
            {
                // Add new setting to the list
                Slots.Add(setting);
            }

            // Serialize the entire list, not just the setting object
            string json = JsonUtility.ToJson(this, true);
            // Write the JSON string to file
            string filePath = Path.Combine(Application.persistentDataPath, FileName);
            File.WriteAllText(filePath, json);
        }

        public LoginSlotSettings GetSettings(int index)
        {
            var settingsForAvatar = Slots.Find(s => s.Index == index);
            if (settingsForAvatar != null)
            {
                return settingsForAvatar;
            }
            else
            {
                return new LoginSlotSettings { Index = index };
            }
        }
    }

    [System.Serializable]
    public class LoginSlotSettings
    {
        public int Index;
        public string Email;
        public string Password;
        public string Username;
        public string DisplayText;
        public string AddressPassword;
        public bool IsRemember;
        public bool IsAutoLogin;
    }
}