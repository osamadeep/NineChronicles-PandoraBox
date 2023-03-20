using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Nekoyume.PandoraBox
{
    [System.Serializable]
    public class FodderPrices
    {
        public List<FodderPrice> Fodders;
        private const string FileName = "FodderPrices.json";

        public void LoadSettings()
        {
            var filePath = Path.Combine(Application.persistentDataPath, FileName);
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                Fodders = JsonUtility.FromJson<FodderPrices>(json).Fodders;
            }
            else
                Fodders = new List<FodderPrice>();
        }

        public void SaveSettings(FodderPrice setting)
        {
            var existingRecord =
                Fodders.FindIndex(s => s.ID == setting.ID);
            if (existingRecord >= 0)
                Fodders[existingRecord] = setting;
            else
                Fodders.Add(setting);

            string json = JsonUtility.ToJson(this, true);
            string filePath = Path.Combine(Application.persistentDataPath, FileName);
            File.WriteAllText(filePath, json);
        }

        public FodderPrice GetRecord(int id)
        {
            var record = Fodders.Find(s => s.ID == id);
            if (record != null)
                return record;
            else
                return new FodderPrice { ID = id };
        }
    }

    [System.Serializable]
    public class FodderPrice
    {
        public int ID;
        public float Price;
    }
}