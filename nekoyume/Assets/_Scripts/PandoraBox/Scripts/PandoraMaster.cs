using UnityEngine;
using DG.Tweening;
using UnityEngine.Audio;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Networking;
using TMPro;
using Nekoyume.State;
using Libplanet.Action;
using PlayFab.ClientModels;
using Nekoyume.Model.BattleStatus;
using UniRx;
using Nekoyume.UI;
using Nekoyume.Model.State;
using Libplanet;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using System.Linq;

namespace Nekoyume.PandoraBox
{
    public class PandoraMaster : MonoBehaviour
    {
        public static PandoraMaster Instance;

        //Unsaved Reg Settings 
        public static string OriginalVersionId = "v100351";
        public static string VersionId = "010085";

        //Pandora Database
        public static PanDatabase PanDatabase;
        public static Guild CurrentGuild; //data for local player since we use it alot
        public static GuildPlayer CurrentGuildPlayer; //data for local player since we use it alot

        //Playfab
        public static PlayerProfileModel PlayFabCurrentPlayer = new PlayerProfileModel();
        public static GetUserInventoryResult PlayFabInventory = new GetUserInventoryResult();

        //General
        public static string CrystalTransferTx = "";
        public static PandoraUtil.ActionType CurrentAction = PandoraUtil.ActionType.Idle;
        public static int ActionCooldown = 4;
        public static bool MarketPriceHelper = false;
        public static string MarketPriceValue;
        public static int LoginIndex;
        public static int ArenaTicketsToUse = 1;
        public static List<string> ArenaFavTargets = new List<string>();
        public static int FavItemsMaxCount = 15;
        public static List<string> FavItems = new List<string>();
        public static bool IsRankingSimulate; //simulate ranking battle
        public static bool IsHackAndSlashSimulate; //simulate h&s
        public static BattleLog CurrentBattleLog; //current stage log
        public static int SelectedWorldID; // pve simulate
        public static int SelectedStageID; // pve simulate
        public static string CurrentArenaEnemyAddress;
        public static Model.State.AvatarState CurrentShopSellerAvatar; //selected item owner avatar
        public static bool IsMultiCombine;


        //Objects
        public PandoraSettings Settings;
        public AudioMixer Audiomixer;
        public GameObject CosmicSword;
        public Sprite CosmicIcon;

        //Inventory
        public static List<Model.Item.ItemBase> TestShopItems = new List<Model.Item.ItemBase>();

        //Skins
        public static Color StickManOutlineColor;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                Settings = new PandoraSettings();
                Settings.Load();
                StartCoroutine(PandoraDB.GetDatabase());
            }
        }

        public void SpeedTimeScale(int speed = 1)
        {
            if (speed != 1)
            {
                Time.timeScale = Settings.FightSpeed;
                DOTween.timeScale = Settings.MenuSpeed / Settings.FightSpeed;
            }
            else
            {
                Time.timeScale = 1;
                DOTween.timeScale = 1;
            }
        }

        public static PandoraPlayer GetPandoraPlayer(string address)
        {
            foreach (PandoraPlayer player in Premium.Pandoraplayers)
            {
                if (player.Address.ToLower() == address.ToLower())
                    return player;
            }

            return new PandoraPlayer();
        }

        public void ShowError(int errorNumber)
        {
            Widget.Find<PandoraError>().Show($"Error <color=red>{errorNumber}</color>!", PandoraUtil.GetNotificationText(errorNumber));
        }
    }

    public class PandoraSettings
    {
        //General
        [HideInInspector] public bool IsStory { get; set; } = false;
        [HideInInspector] public bool IsMultipleLogin { get; set; } = false;

        [HideInInspector] public int BlockShowType { get; set; } = 0;
        [HideInInspector] public int MenuSpeed { get; set; } = 3;

        //PVE
        [HideInInspector] public int FightSpeed { get; set; } = 1;
        [HideInInspector] public int RaidCooldown { get; set; } = 30;

        //PVP

        [HideInInspector] public int ArenaListUpper { get; set; } = 0;

        [HideInInspector] public int ArenaListLower { get; set; } = 0;

        [HideInInspector] public int ArenaListStep { get; set; } = 90;
        [HideInInspector] public bool ArenaPush { get; set; } = true; //push means send every 'ArenaPushStep' whatever its confirm or not
        [HideInInspector] public int ArenaPushStep { get; set; } = 5;
        [HideInInspector] public bool ArenaValidator { get; set; } = true; //true = 9cscan, false = local node

        public void Save()
        {
            //General
            PlayerPrefs.SetString("_PandoraBox_Ver", PandoraMaster.VersionId);
            PlayerPrefs.SetInt("_PandoraBox_General_IsStory", System.Convert.ToInt32(IsStory));
            PlayerPrefs.SetInt("_PandoraBox_General_IsMultipleLogin", System.Convert.ToInt32(IsMultipleLogin));
            PlayerPrefs.SetInt("_PandoraBox_General_BlockShowType", BlockShowType);
            PlayerPrefs.SetInt("_PandoraBox_General_MenuSpeed", MenuSpeed);

            //PVE
            PlayerPrefs.SetInt("_PandoraBox_PVE_FightSpeed", FightSpeed);
            PlayerPrefs.SetInt("_PandoraBox_PVE_RaidCooldown", RaidCooldown);

            //PVP
            PlayerPrefs.SetInt("_PandoraBox_PVP_ListCountLower", ArenaListLower);
            PlayerPrefs.SetInt("_PandoraBox_PVP_ListCountUpper", ArenaListUpper);
            PlayerPrefs.SetInt("_PandoraBox_PVP_ListCountStep", ArenaListStep);
            PlayerPrefs.SetInt("_PandoraBox_PVP_ArenaPush", System.Convert.ToInt32(ArenaPush));
            PlayerPrefs.SetInt("_PandoraBox_PVP_ArenaPushStep", ArenaPushStep);
            PlayerPrefs.SetInt("_PandoraBox_PVP_ArenaValidator", System.Convert.ToInt32(ArenaValidator));

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
            if (int.Parse(PandoraMaster.VersionId.Substring(0, 5)) >
                int.Parse(PlayerPrefs.GetString("_PandoraBox_Ver").Substring(0, 5)))
            {
                PlayerPrefs.SetString("_PandoraBox_Ver", PandoraMaster.VersionId);
                //PlayerPrefs.SetInt("_PandoraBox_General_WhatsNewShown", 0); //false

                PlayerPrefs.SetInt("_PandoraBox_General_IsStory", System.Convert.ToInt32(true));
            }

            //General
            IsStory = System.Convert.ToBoolean(PlayerPrefs.GetInt("_PandoraBox_General_IsStory",
                System.Convert.ToInt32(IsStory)));
            IsMultipleLogin = System.Convert.ToBoolean(PlayerPrefs.GetInt("_PandoraBox_General_IsMultipleLogin",
                System.Convert.ToInt32(IsMultipleLogin)));
            BlockShowType = PlayerPrefs.GetInt("_PandoraBox_General_BlockShowType", BlockShowType);
            MenuSpeed = PlayerPrefs.GetInt("_PandoraBox_General_MenuSpeed", MenuSpeed);

            //PVE
            FightSpeed = PlayerPrefs.GetInt("_PandoraBox_PVE_FightSpeed", FightSpeed);
            RaidCooldown = PlayerPrefs.GetInt("_PandoraBox_PVE_RaidCooldown", RaidCooldown);

            //PVP
            ArenaListUpper = PlayerPrefs.GetInt("_PandoraBox_PVP_ListCountUpper", ArenaListUpper);
            ArenaListLower = PlayerPrefs.GetInt("_PandoraBox_PVP_ListCountLower", ArenaListLower);
            ArenaListStep = PlayerPrefs.GetInt("_PandoraBox_PVP_ListCountStep", ArenaListStep);
            ArenaPush = System.Convert.ToBoolean(PlayerPrefs.GetInt("_PandoraBox_PVP_ArenaPush",System.Convert.ToInt32(ArenaPush)));
            ArenaPushStep = PlayerPrefs.GetInt("_PandoraBox_PVP_ArenaPushStep", ArenaPushStep);
            ArenaValidator = System.Convert.ToBoolean(PlayerPrefs.GetInt("_PandoraBox_PVP_ArenaValidator", System.Convert.ToInt32(ArenaValidator)));

            //Load ingame changes
            DOTween.timeScale = MenuSpeed;
        }
    }
}
