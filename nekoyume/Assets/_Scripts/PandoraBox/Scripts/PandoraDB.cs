using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Nekoyume.PandoraBox
{
    public class PandoraDB
    {
        public static string DBPath;

        public static IEnumerator GetDatabase()
        {
            DBPath = DatabasePath.PandoraDatabasePath;
            string url = URLAntiCacheRandomizer($"{DBPath}9c.pandora");
            UnityWebRequest www = UnityWebRequest.Get(url);
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                PandoraMaster.Instance.ShowError(404);
            }
            else
            {
                try
                {
                    PandoraMaster.PanDatabase = JsonUtility.FromJson<PanDatabase>(www.downloadHandler.text);
                } // Debug.LogError(JsonUtility.ToJson(PanDatabase)); }
                catch
                {
                    PandoraMaster.Instance.ShowError(16);
                }
            }
        }

        public static string URLAntiCacheRandomizer(string url)
        {
            string r = "";
            r += UnityEngine.Random.Range(
                1000000, 8000000).ToString();
            string result = url + "?p=" + r;
            return result;
        }
    }

    [System.Serializable]
    public class PanDatabase
    {
        //public string VersionID;
        public List<string> AllowedVersions;
        public List<Guild> Guilds;
        public List<GuildPlayer> GuildPlayers;
        public List<FeatureItem> FeatureItems;
        public List<NFTItem> NFTItems;
        public List<NFTOwner> NFTOwners;
        public List<PandoraMembership> PandoraMemberships;
        public RunnerSettings RunnerSettings;
        public string AnalyzeKey;
        public int DiceRoll;
        public int TrialPremium;
        public int Crystal;
        public int CrystalOld;
        public int PremiumPrice;
        public int ShopFeaturePrice;
        public float CrystalPremiumBouns;
        //public List<PandoraPlayer> Players;
    }

    [System.Serializable]
    public class PandoraPlayer
    {
        public string Address;
        public int PremiumEndBlock;

        public bool IsPremium()
        {
            try
            {
                int currentBlock = (int)Game.Game.instance.Agent.BlockIndex;
                bool result = false;
                if (PremiumEndBlock >= currentBlock && PremiumEndBlock > 5263000)
                    result = true;
                if (PandoraMaster.PanDatabase.TrialPremium > currentBlock && PandoraMaster.PanDatabase.TrialPremium > 5263000)
                    result = true;
                return result;
            }
            catch
            {
                return false;
            }
        }
    }

    [System.Serializable]
    public class Guild
    {
        public string Tag;
        public string Name;
        public string Desc;
        public string MinLevel;
        public string Link;
        public int Type;
        public string Language;
    }

    [System.Serializable]
    public class GuildPlayer
    {
        public string Address;
        public string Guild;
        public string AvatarAddress;
        public int Rank;

        public bool IsEqual(string otherAddress)
        {
            return AvatarAddress.ToLower() == otherAddress.ToLower();
        }
    }

    [System.Serializable]
    public class FeatureItem
    {
        public string ItemID;
        public int EndBlock;

        public bool IsEqual(string otherID)
        {
            return ItemID.ToLower() == otherID.ToLower();
        }

        public bool IsValid()
        {
            int currentBlock = (int)Game.Game.instance.Agent.BlockIndex;
            bool result = false;
            if (EndBlock >= currentBlock)
                result = true;
            return result;
        }
    }

    [System.Serializable]
    public class NFTItem
    {
        public string ItemID;
        public string ItemName;
        public string ItemDesc;
        public string PrefabLocation;
        public string ContractAddress;
        public string TokenID;
        public int Quantity;
    }

    [System.Serializable]
    public class NFTOwner
    {
        public string Address;
        public string AvatarAddress;
        public List<string> OwnedItems;
        public string CurrentArenaBanner;
        public string CurrentFullCostume;
        public string CurrentPortrait;
    }

    [System.Serializable]
    public class PandoraMembership
    {
        public int ID;

        //Settings
        public int ActionCooldown;
        public int FavoriteItemsMaxCount;
    }

    [System.Serializable]
    public class RunnerSettings
    {
        public bool Available;
        public string Name;
        public List<int> RewardsNCG;
        public List<int> RewardsPG;
        public List<int> RewardsPC;
    }
}
