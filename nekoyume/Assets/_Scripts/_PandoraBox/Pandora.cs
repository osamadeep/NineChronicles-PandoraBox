using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Nekoyume.PandoraBox
{
    public class Pandora : MonoBehaviour
    {
        public static Pandora Instance;

        public enum GameNetwork { Oden=0,Heimdall=1 };

        //Unsaved Reg Settings
        public static string OriginalVersionId = "v200131";
        public static string VersionId = "020001";

        //General
        public static GameNetwork SelectedNetwork = 0; //Odin,Heimdall
        public static int SelectedLoginAccountIndex;
        public static int SelectedRPC = 0;

        //Objects
        public Settings Settings;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            SelectedNetwork = (GameNetwork)PlayerPrefs.GetInt("_PandoraBox_General_SelectedNetwork", 0);
            SelectedRPC = PlayerPrefs.GetInt("_PandoraBox_General_SelectedRPC", 0);
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        public void SaveToFile<T>(string filePath, T data)
        {
            string jsonData = JsonUtility.ToJson(data);
            File.WriteAllText(filePath, jsonData);
            Debug.Log("File saved: " + filePath);
        }

        public T LoadFromFile<T>(string filePath) where T : new()
        {
            if (File.Exists(filePath))
            {
                string jsonData = File.ReadAllText(filePath);
                return JsonUtility.FromJson<T>(jsonData);
            }
            else
            {
                Debug.LogWarning("File not found: " + filePath);
                return new T();
            }
        }
    }
}
