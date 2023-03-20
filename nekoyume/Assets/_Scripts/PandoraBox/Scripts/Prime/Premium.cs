using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.UI;
using Nekoyume.UI.Scroller;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Nekoyume.Model.State;
using Nekoyume.BlockChain;
using Newtonsoft.Json;
using Libplanet;
using System;
using Newtonsoft.Json.Linq;


namespace Nekoyume.PandoraBox
{
    using Bencodex.Types;
    using Cysharp.Threading.Tasks;
    using Nekoyume.Action;
    using Nekoyume.Arena;
    using Nekoyume.Battle;
    using Nekoyume.EnumType;
    using Nekoyume.Game.Controller;
    using Nekoyume.Helper;
    using Nekoyume.L10n;
    using Nekoyume.Model;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.Stat;
    using Nekoyume.TableData;
    using Nekoyume.TableData.Event;
    using Nekoyume.UI.Model;
    using Nekoyume.UI.Module;
    using Org.BouncyCastle.Asn1.Mozilla;
    using PlayFab;
    using PlayFab.ClientModels;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;
    using UniRx;
    using UnityEditor;
    using UnityEngine.Networking;
    using static Nekoyume.UI.SubRecipeView;

    public class Premium
    {
        public static PandoraAccount PandoraProfile = new PandoraAccount();
        public static List<string> Pandoraplayers { get; private set; } = new List<string>();
        public static ConcurrentQueue<WebhookNotification> QueueWebHook = new ConcurrentQueue<WebhookNotification>();

        public static int ArenaMaxBattleCount { get; private set; }
        public static int ArenaRemainsBattle { get; private set; }
        public static bool ArenaBattleInProgress { get; set; }

        static Address enemyAvatarAddress;
        static List<Guid> costumes;
        static List<Guid> equipments;
        static List<RuneSlotInfo> runeInfos;
        static int championshipId;
        static int round;
        static int ticket;

        #region PANDORA ACCOUNT METHODS

        public static bool PANDORA_CheckPremium()
        {
            if (PandoraProfile.IsPremium())
                return true;
            else
            {
                OneLineSystem.Push(MailType.System, "<color=green>Pandora Box</color>: This is Premium Feature!",
                    NotificationCell.NotificationType.Alert);
                return false;
            }
        }

        public static void GetPremiumPlayers()
        {
            var request = new ExecuteCloudScriptRequest
            {
                FunctionName = "getPremiums",
                GeneratePlayStreamEvent = true,
                RevisionSelection = CloudScriptRevisionOption.Live,
                FunctionParameter = new { }
            };

            PlayFabClientAPI.ExecuteCloudScript(request, OnGetPremiumsSuccess
                    =>
                {
                    if (OnGetPremiumsSuccess.FunctionResult != null)
                    {
                        string json = (string)OnGetPremiumsSuccess.FunctionResult;
                        JObject jsonObject = JObject.Parse(json);
                        JArray premiumAddresses = (JArray)jsonObject["PremiumAddresses"];
                        Pandoraplayers = premiumAddresses.Select(a => (string)a).ToList();
                    }
                },
                OnGetPremiumsFailure =>
                {
                    PandoraUtil.ShowSystemNotification(OnGetPremiumsFailure.ErrorMessage,
                        NotificationCell.NotificationType.Alert);
                });
        }


        public static void PANDORA_GetDatabase()
        {
            ////get players data
            //PlayFabClientAPI.GetLeaderboard(new GetLeaderboardRequest
            //{
            //    StatisticName = "PremiumEndBlock",
            //    ProfileConstraints = new PlayerProfileViewConstraints() { ShowDisplayName = true, ShowLinkedAccounts = true, },
            //    MaxResultsCount = 100,
            //}, success =>
            //{
            //    Pandoraplayers = new List<PandoraPlayer>();
            //    foreach (var player in success.Leaderboard)
            //    {
            //        if (player.StatValue > Game.Game.instance.Agent.BlockIndex)
            //        {
            //            PandoraPlayer newPlayer = new PandoraPlayer()
            //            {
            //                Address = player.Profile.LinkedAccounts[0].PlatformUserId,
            //                PremiumEndBlock = player.StatValue
            //            };
            //            Pandoraplayers.Add(newPlayer);
            //        }
            //    }

            //    //check current player stats
            //    var panplayer = Pandoraplayers.Find(x => x.Address.ToLower() == agentAddress.ToString().ToLower());
            //    if (panplayer is null)
            //        PANDORA_Initialize(new PandoraPlayer());
            //    else
            //        PANDORA_Initialize(panplayer);

            //    //get player inventory
            //    PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(),
            //    succuss =>
            //    {
            //        PandoraMaster.PlayFabInventory = succuss;
            //        try
            //        {
            //            //UI update, not optimal
            //            Widget.Find<PandoraShopPopup>().UpdateCurrency();
            //            Widget.Find<RunnerRankPopup>().UpdateCurrency();
            //        }
            //        catch (Exception e) { Debug.LogError(e); }
            //    }, fail => { PandoraUtil.ShowSystemNotification(322, NotificationCell.NotificationType.Alert); });
            //},
            //fail => { PandoraUtil.ShowSystemNotification(362, NotificationCell.NotificationType.Alert); });
        }

        public static async UniTask PANDORA_SendWebhookT(string hook, string message)
        {
            WWWForm form = new WWWForm();
            form.AddField("content", message);
            using (UnityWebRequest www = UnityWebRequest.Post(hook, form))
            {
                await www.SendWebRequest();
                www.Dispose();
            }
        }

        public static void PANDORA_TransferAsset(string currency, long majorUnit, long minorUnit, string recipent,
            string memo)
        {
            var currencyUsed = new Libplanet.Assets.Currency();
            if (currency == "NCG")
                currencyUsed = Libplanet.Assets.Currency.Legacy("NCG", 2,
                    new Libplanet.Address("47d082a115c63e7b58b1532d20e631538eafadde"));
            else if (currency == "CRYSTAL")
                currencyUsed = Libplanet.Assets.Currency.Legacy("CRYSTAL", 18, minters: null);
            else if (currency == "RUNE_FENRIR1")
                currencyUsed = Libplanet.Assets.Currency.Legacy("RUNE_FENRIR1", 0, minters: null);

            var blockIndex = Game.Game.instance.Agent?.BlockIndex ?? -1;
            ActionManager.Instance
                .TransferAsset(
                    States.Instance.CurrentAvatarState.agentAddress,
                    new Libplanet.Address(recipent),
                    new Libplanet.Assets.FungibleAssetValue(currencyUsed, majorUnit, minorUnit),
                    memo)
                .Subscribe();
        }

        public static IEnumerator PANDORA_ProcessQueueWebHook()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);
                //if (QueueWebHook.Count > 0)
                //{
                //    var currentNotification = QueueWebHook.First();
                //    yield return PANDORA_SendWebhookT(currentNotification.Uri, currentNotification.Message);
                //    QueueWebHook.RemoveAt(0);
                //}
                if (QueueWebHook.TryDequeue(out var currentNotification))
                    yield return PANDORA_SendWebhookT(currentNotification.Uri, currentNotification.Message);
            }
        }

        public static void PANDORA_BuyGems(int ncg, int gems)
        {
            var currency =
                Libplanet.Assets.Currency.Legacy("NCG", 2, new Address("47d082a115c63e7b58b1532d20e631538eafadde"));

            ActionManager.Instance
                .TransferAsset(
                    States.Instance.CurrentAvatarState.agentAddress,
                    new Address("0x1012041FF2254f43d0a938aDF89c3f11867A2A58"),
                    new Libplanet.Assets.FungibleAssetValue(currency, ncg, 0),
                    $"Pandora Gems: {gems}")
                .Subscribe();
        }

        public static void PANDORA_ConfirmGemsRequest(long blockIndex, int gems, int ncg)
        {
            if (string.IsNullOrEmpty(PandoraMaster.CrystalTransferTx)) //multi instanses is running
                return;

            //auto fill
            PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest
                {
                    FunctionName = "buyGems2",
                    FunctionParameter = new
                    {
                        premium = PandoraProfile.IsPremium(),
                        address = States.Instance.CurrentAvatarState.agentAddress.ToString(),
                        gems = gems,
                        currentBlock = blockIndex,
                        cost = ncg,
                        transaction = PandoraMaster.CrystalTransferTx
                    }
                },
                success =>
                {
                    if (success.FunctionResult.ToString() == "Success")
                    {
                        //adding score success
                        AudioController.instance.PlaySfx(AudioController.SfxCode.BuyItem);
                        Premium.PandoraProfile.Currencies["PG"] += gems; //just UI update instead of request new call
                        NotificationSystem.Push(Nekoyume.Model.Mail.MailType.System,
                            $"PandoraBox: <color=#76F3FE><b>{gems}Pandora Gems</b></color> added <color=green>Successfully!</color>",
                            NotificationCell.NotificationType.Information);
                    }
                    else
                    {
                        NotificationSystem.Push(Nekoyume.Model.Mail.MailType.System,
                            "PandoraBox: Buy <color=red>Failed!</color>", NotificationCell.NotificationType.Alert);
                    }
                },
                failed => { Debug.LogError("Process Failed!, " + failed.GenerateErrorReport()); });
            PandoraMaster.CrystalTransferTx = "";
        }

        #endregion

        #region ACCOUNT METHODS

        public static void ACCOUNT_CollectStakeRewards()
        {
            if (!PANDORA_CheckPremium())
                return;
            PandoraUtil.ShowSystemNotification("Staking Rewards <color=green>Collected</color>!",
                NotificationCell.NotificationType.Information);
            ActionManager.Instance.ClaimStakeReward();
        }

        public static void ACCOUNT_BuyCrystals(int ncgToSpend, int CrystalPerNCG, float bounsMultiplier)
        {
            Widget.Find<PandoraShopPopup>().WaitingImage.SetActive(true);
            float totalCrystal = bounsMultiplier * ncgToSpend * CrystalPerNCG;
            var currency =
                Libplanet.Assets.Currency.Legacy("NCG", 2, new Address("47d082a115c63e7b58b1532d20e631538eafadde"));

            ActionManager.Instance.TransferAsset(
                    States.Instance.CurrentAvatarState.agentAddress,
                    new Address("0x1012041FF2254f43d0a938aDF89c3f11867A2A58"),
                    new Libplanet.Assets.FungibleAssetValue(currency, ncgToSpend, 0),
                    $"Pandora Crystal: OR={CrystalPerNCG},TL={(int)totalCrystal},Bouns={bounsMultiplier})")
                .Subscribe();
        }

        public static void ACCOUNT_ConfirmCrystalRequest(long blockIndex, string memo, int ncg)
        {
            if (string.IsNullOrEmpty(PandoraMaster.CrystalTransferTx)) //multi instanses is running?
                return;

            //auto fill
            PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest
                {
                    FunctionName = "buyCrystals",
                    FunctionParameter = new
                    {
                        memo = memo,
                        currentBlock = blockIndex,
                        cost = ncg,
                        transaction = PandoraMaster.CrystalTransferTx,
                        address = States.Instance.CurrentAvatarState.agentAddress.ToString(),
                        bonus = (int)(ncg / 10)
                    }
                },
                success =>
                {
                    if (success.FunctionResult.ToString() == "Success")
                    {
                        //adding score success
                        AudioController.instance.PlaySfx(AudioController.SfxCode.BuyItem);
                        Premium.PandoraProfile.Currencies["PG"] +=
                            (int)(ncg / 10); //just UI update instead of request new call
                        NotificationSystem.Push(Nekoyume.Model.Mail.MailType.System,
                            $"PandoraBox: Crystel Request <color=green>Success!</color>, " +
                            $"{(int)(ncg / 10)} PG added to your Account!",
                            NotificationCell.NotificationType.Information);
                    }
                    else
                    {
                        NotificationSystem.Push(Nekoyume.Model.Mail.MailType.System,
                            "PandoraBox: Buy <color=red>Failed!</color>", NotificationCell.NotificationType.Alert);
                    }
                },
                failed => { Debug.LogError("Process Failed!, " + failed.GenerateErrorReport()); });


            PandoraMaster.CrystalTransferTx = "";
        }

        #endregion

        #region CRAFT METHODS

        public static void CRAFT_AutoCraftConsumable(AvatarState currentAvatarState, int slotIndex,
            ConsumableItemRecipeSheet.Row consRow)
        {
            if (!PandoraProfile.IsPremium())
                return;

            Dictionary<int, int> materialMap = new Dictionary<int, int>();
            foreach (var material in consRow.Materials)
                materialMap.Add(material.Id, material.Count);

            //check if he has material
            int missingParts = 999;
            try //in case States.Instance.CurrentAvatarState.inventory not ready
            {
                missingParts = CRAFT_GetReplacedMaterials(currentAvatarState, materialMap).Count;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return;
            }

            if (missingParts > 0)
            {
                OneLineSystem.Push(MailType.System,
                    $"<color=green>Pandora Box</color>: <color=green>{currentAvatarState.name}</color> " +
                    $"missing <color=green>{missingParts + 1}</color> Material parts!"
                    , NotificationCell.NotificationType.Alert);
                return;
            }

            try //in case actionManager is not ready yet
            {
                var itemName = L10nManager.Localize($"ITEM_NAME_{consRow.ResultConsumableItemId}");
                string analyzeText = $"CombinationConsumable > {itemName} > {slotIndex} ";

                if (currentAvatarState.address == States.Instance.CurrentAvatarState.address)
                {
                    var recipeInfo = new RecipeInfo
                    {
                        RecipeId = consRow.Id,
                        Materials = materialMap,
                        ReplacedMaterials = CRAFT_GetReplacedMaterials(currentAvatarState, materialMap),
                    };

                    ActionManager.Instance.CombinationConsumable(recipeInfo, slotIndex).Subscribe();
                    //analyze actions
                    string message =
                        $"[v{PandoraMaster.VersionId.Substring(3)}][{Game.Game.instance.Agent.BlockIndex}] **{currentAvatarState.name}** Lv.**{currentAvatarState.level}** " +
                        $"<:NCG:1009757564256407592>**{States.Instance.GoldBalanceState.Gold.MajorUnit}** > {currentAvatarState.agentAddress}, " +
                        analyzeText;
                    ActionManager.Instance.AnalyzeActions(message).Forget();
                }
                else
                {
                    var action = new CombinationConsumable
                    {
                        recipeId = consRow.Id,
                        avatarAddress = currentAvatarState.address,
                        slotIndex = slotIndex,
                    };

                    ActionManager.Instance.PreProcessAction(action, currentAvatarState, analyzeText);
                }

                Widget.Find<ChronoSlotsPopup>().slots[slotIndex].stageCooldown = 0; //force reset avatar?
                OneLineSystem.Push(MailType.System,
                    $"<color=green>Pandora Box</color>: {currentAvatarState.NameWithHash} " +
                    $"start Auto Craft <color=green>{itemName}</color> on Slot <color=green>{slotIndex + 1}</color>!",
                    NotificationCell.NotificationType.Information);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        public static void CRAFT_AutoCraftEventConsumable(AvatarState currentAvatarState, int slotIndex,
            EventConsumableItemRecipeSheet.Row consRow)
        {
            if (!PandoraProfile.IsPremium())
                return;

            Dictionary<int, int> materialMap = new Dictionary<int, int>();
            foreach (var material in consRow.Materials)
                materialMap.Add(material.Id, material.Count);

            //check if he has material
            int missingParts = 999;
            try //in case States.Instance.CurrentAvatarState.inventory not ready
            {
                missingParts = CRAFT_GetReplacedMaterials(currentAvatarState, materialMap).Count;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return;
            }

            if (missingParts > 0)
            {
                OneLineSystem.Push(MailType.System,
                    $"<color=green>Pandora Box</color>: <color=green>{currentAvatarState.name}</color> " +
                    $"missing <color=green>{missingParts + 1}</color> Material parts!"
                    , NotificationCell.NotificationType.Alert);
                return;
            }

            try //in case actionManager is not ready yet
            {
                var itemName = L10nManager.Localize($"ITEM_NAME_{consRow.ResultConsumableItemId}");
                string analyzeText = $"CombinationConsumable > {itemName} > {slotIndex} ";

                if (currentAvatarState.address == States.Instance.CurrentAvatarState.address)
                {
                    var recipeInfo = new RecipeInfo
                    {
                        RecipeId = consRow.Id,
                        Materials = materialMap,
                        ReplacedMaterials = CRAFT_GetReplacedMaterials(currentAvatarState, materialMap),
                    };
                    ActionManager.Instance
                        .EventConsumableItemCrafts(RxProps.EventScheduleRowForRecipe.Value.Id, recipeInfo, slotIndex)
                        .Subscribe();
                    //analyze actions
                    string message =
                        $"[v{PandoraMaster.VersionId.Substring(3)}][{Game.Game.instance.Agent.BlockIndex}] **{currentAvatarState.name}** Lv.**{currentAvatarState.level}** " +
                        $"<:NCG:1009757564256407592>**{States.Instance.GoldBalanceState.Gold.MajorUnit}** > {currentAvatarState.agentAddress}, " +
                        analyzeText;
                    ActionManager.Instance.AnalyzeActions(message).Forget();
                }
                else
                {
                    var action = new EventConsumableItemCrafts
                    {
                        AvatarAddress = currentAvatarState.address,
                        EventScheduleId = RxProps.EventScheduleRowForRecipe.Value.Id,
                        EventConsumableItemRecipeId = consRow.Id,
                        SlotIndex = slotIndex,
                    };
                    ActionManager.Instance.PreProcessAction(action, currentAvatarState, analyzeText);
                }

                Widget.Find<ChronoSlotsPopup>().slots[slotIndex].stageCooldown = 0; //force reset avatar?
                OneLineSystem.Push(MailType.System,
                    $"<color=green>Pandora Box</color>: {currentAvatarState.NameWithHash} " +
                    $"start Auto Craft <color=green>{itemName}</color> on Slot <color=green>{slotIndex + 1}</color>!",
                    NotificationCell.NotificationType.Information);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        public static async UniTask CRAFT_AutoCraftEquipment(AvatarState currentAvatarState, int slotIndex,
            EquipmentItemRecipeSheet.Row recipeRow, bool payByCrystal, bool isPremium, int avatarSlotIndex)
        {
            if (!PandoraProfile.IsPremium())
                return;

            //get sub id
            var tableSheets = Game.TableSheets.Instance;
            var itemSubSheet = tableSheets.EquipmentItemSubRecipeSheetV2;
            int indexSub = isPremium ? 1 : 0;
            var itemSub = itemSubSheet.First(x => x.Value.Id == recipeRow.SubRecipeIds[indexSub]).Value;

            //check if there is supercraft
            bool isHammerCraft = false;
            var hammerAddress = Addresses.GetHammerPointStateAddress(currentAvatarState.address, recipeRow.Id);
            var hammerState = await Game.Game.instance.Agent.GetStateAsync(hammerAddress);
            var hammerData = hammerState is List list
                ? new HammerPointState(hammerAddress, list)
                : new HammerPointState(hammerAddress, recipeRow.Id);

            var max = Game.TableSheets.Instance.CrystalHammerPointSheet[recipeRow.Id].MaxPoint;
            isHammerCraft = hammerData.HammerPoint == max;
            if (isHammerCraft)
            {
                indexSub = 1;
                itemSub = itemSubSheet.First(x => x.Value.Id == recipeRow.SubRecipeIds[indexSub]).Value;
            }

            //check if supercraft has crystal cost
            if (isHammerCraft && States.Instance.CrystalBalance.MajorUnit <
                Game.TableSheets.Instance.CrystalHammerPointSheet[recipeRow.Id].CRYSTAL)
            {
                OneLineSystem.Push(MailType.System,
                    $"<color=green>Pandora Box</color>: <color=green>{currentAvatarState.name}</color> don't have enough Crystals for SuperCraft!"
                    , NotificationCell.NotificationType.Alert);
                return;
            }


            //check if he has material
            if (!payByCrystal && !isHammerCraft)
            {
                int missingParts = 999;
                try //in case States.Instance.CurrentAvatarState.inventory not ready
                {
                    missingParts = CRAFT_IsEnoughMaterials(currentAvatarState, recipeRow.Id, itemSub.Id);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    return;
                }

                if (missingParts > 0)
                {
                    OneLineSystem.Push(MailType.System,
                        $"<color=green>Pandora Box</color>: <color=green>{currentAvatarState.name}</color> " +
                        $"missing <color=green>{missingParts + 1}</color> Material parts!"
                        , NotificationCell.NotificationType.Alert);
                    return;
                }
            }

            //check if he has crystal


            try //in case actionManager is not ready yet
            {
                var itemName = L10nManager.Localize($"ITEM_NAME_{recipeRow.ResultEquipmentId}");
                string premiumCraft = indexSub == 0 ? "Basic" : "Premium";
                string hammerCraft = isHammerCraft ? "Super Craft" : "";
                string crystal = payByCrystal ? ", With Crystal!" : "";
                string analyzeText =
                    $"CombinationEquipment > **{hammerCraft} {premiumCraft}** {itemName} > {slotIndex}{crystal} " +
                    $"> hammer:{hammerData.HammerPoint + 1}/{max}";

                if (currentAvatarState.address == States.Instance.CurrentAvatarState.address)
                {
                    ActionManager.Instance
                        .CombinationEquipment(
                            new RecipeInfo
                            {
                                RecipeId = recipeRow.Id,
                                SubRecipeId = itemSub.Id,
                                CostNCG = default,
                                CostCrystal = default,
                                CostAP = 0,
                                Materials = default,
                                ReplacedMaterials = null,
                            },
                            slotIndex,
                            payByCrystal,
                            isHammerCraft)
                        .Subscribe();
                    //analyze actions
                    string message =
                        $"[v{PandoraMaster.VersionId.Substring(3)}][{Game.Game.instance.Agent.BlockIndex}] **{currentAvatarState.name}** Lv.**{currentAvatarState.level}** " +
                        $"<:NCG:1009757564256407592>**{States.Instance.GoldBalanceState.Gold.MajorUnit}** > {currentAvatarState.agentAddress}, " +
                        analyzeText;
                    ActionManager.Instance.AnalyzeActions(message).Forget();
                }
                else
                {
                    var action = new CombinationEquipment
                    {
                        avatarAddress = currentAvatarState.address,
                        slotIndex = slotIndex,
                        recipeId = recipeRow.Id,
                        subRecipeId = itemSub.Id,
                        payByCrystal = payByCrystal,
                        useHammerPoint = isHammerCraft,
                    };
                    ActionManager.Instance.PreProcessAction(action, currentAvatarState, analyzeText);
                }

                Widget.Find<ChronoSlotsPopup>().slots[avatarSlotIndex].stageCooldown = 0; //force reset avatar?
                OneLineSystem.Push(MailType.System,
                    $"<color=green>Pandora Box</color>: {currentAvatarState.NameWithHash} " +
                    $"start Auto Craft <color=green>{itemName}</color> on Slot <color=green>{slotIndex + 1}</color> hammer points: <color=green>{hammerData.HammerPoint + 1}</color>/{max}!",
                    NotificationCell.NotificationType.Information);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        static int CRAFT_IsEnoughMaterials(AvatarState currentAvatarState, int itemID, int itemSubID)
        {
            var tableSheets = Game.TableSheets.Instance;
            var itemSheet = tableSheets.EquipmentItemRecipeSheet;
            var itemSubSheet = tableSheets.EquipmentItemSubRecipeSheetV2;
            var itemRow = itemSheet.First(x => x.Value.Id == itemID).Value;
            var itemSub = itemSubSheet.First(x => x.Value.Id == itemSubID).Value;

            var baseMaterialInfo =
                new EquipmentItemSubRecipeSheet.MaterialInfo(itemRow.MaterialId, itemRow.MaterialCount);

            Dictionary<int, int> materialMap = new Dictionary<int, int>();
            materialMap.Add(itemRow.MaterialId, itemRow.MaterialCount); // Add base material
            foreach (var material in itemSub.Materials) // Add other materials
                materialMap.Add(material.Id, material.Count);

            return CRAFT_GetReplacedMaterials(currentAvatarState, materialMap).Count;
        }

        public static int CRAFT_GetCraftingFees(Equipment equipment)
        {
            int grade = equipment
                .Grade; // The grade of the item, 1 for normal, 2 for epic, 3 for legendary, 4 for mythic
            int level = equipment.level; // The level of the item, can be any integer from 0 to 7

            int craftingFees = 0; // The crafting fees for the item, initially set to 0
            int totalFees = 0; // The total crafting fees for all levels up to the given level, initially set to 0

            switch (grade) // Calculate the crafting fees based on the grade
            {
                case 1:
                    craftingFees = 10;
                    break;
                case 2:
                    craftingFees = 20;
                    break;
                case 3:
                    craftingFees = 30;
                    break;
                case 4:
                    craftingFees = 40;
                    break;
                default:
                    // Handle invalid grade values here
                    break;
            }

            if (level >= 4) // Levels 4, 5, 6, 7, 8, and 9 have crafting fees
            {
                if (level == 4)
                {
                    totalFees = craftingFees;
                }
                else if (level == 5)
                {
                    totalFees = craftingFees * 2;
                }
                else if (level == 6)
                {
                    totalFees = craftingFees * 4;
                }
                else if (level == 7)
                {
                    totalFees = craftingFees * 8 + (craftingFees * 2);
                }
                else if (level == 8)
                {
                    totalFees = craftingFees * 16 + (craftingFees * 4);
                }
                else if (level == 9)
                {
                    totalFees = craftingFees * 32 + (craftingFees * 8);
                }
                else if (level == 10)
                {
                    totalFees = craftingFees * 64 + (craftingFees * 16);
                }
                else if (level == 11)
                {
                    totalFees = craftingFees * 128 + (craftingFees * 32);
                }
                else if (level == 12)
                {
                    totalFees = craftingFees * 256 + (craftingFees * 64);
                }
            }

            return totalFees;
        }

        public static int CRAFT_GetCraftingFodderCount(Equipment equipment)
        {
            return (equipment.level >= 1) ? (int)Math.Pow(2, equipment.level - 2) : 0;
        }

        static Dictionary<int, int> CRAFT_GetReplacedMaterials(AvatarState currentAvatarState,
            Dictionary<int, int> required)
        {
            var replacedMaterialMap = new Dictionary<int, int>();
            var inventory = currentAvatarState.inventory;

            foreach (var (id, count) in required)
            {
                if (!Game.TableSheets.Instance.MaterialItemSheet.TryGetValue(id, out var row))
                {
                    continue;
                }

                var itemCount = inventory.TryGetFungibleItems(
                    row.ItemId,
                    out var outFungibleItems)
                    ? outFungibleItems.Sum(e => e.count)
                    : 0;

                if (count > itemCount)
                {
                    replacedMaterialMap.Add(row.Id, count - itemCount);
                }
            }

            return replacedMaterialMap;
        }

        #endregion

        #region PVE METHODS

        public static async UniTask PVE_AutoWorldBoss(AvatarState currentAvatarState, RaiderState raider)
        {
            if (!PandoraProfile.IsPremium())
                return;

            var currentBlockIndex = Game.Game.instance.Agent.BlockIndex;
            var curStatus = WorldBossFrontHelper.GetStatus(currentBlockIndex);
            if (curStatus != UI.Module.WorldBoss.WorldBossStatus.Season)
            {
                OneLineSystem.Push(MailType.System, $"<color=green>Pandora Box</color>: No Boss Season Found!",
                    NotificationCell.NotificationType.Alert);
                return;
            }

            try //in case actionManager is not ready yet
            {
                var (itemSlotStates, runeSlotStates) = await currentAvatarState.GetSlotStatesAsync();

                string analyzeText =
                    $"**Raid** > {3 - (WorldBossFrontHelper.GetRemainTicket(raider, currentBlockIndex) - 1)}/3";
                if (currentAvatarState.address == States.Instance.CurrentAvatarState.address)
                {
                    ActionManager.Instance.Raid(itemSlotStates[2].Costumes,
                        itemSlotStates[2].Equipments,
                        new List<Guid>(),
                        runeSlotStates[2].GetEquippedRuneSlotInfos(),
                        false).Subscribe();

                    //analyze actions
                    string message =
                        $"[v{PandoraMaster.VersionId.Substring(3)}][{Game.Game.instance.Agent.BlockIndex}] **{currentAvatarState.name}** Lv.**{currentAvatarState.level}** " +
                        $"<:NCG:1009757564256407592>**{States.Instance.GoldBalanceState.Gold.MajorUnit}** > {currentAvatarState.agentAddress}, " +
                        analyzeText;
                    ActionManager.Instance.AnalyzeActions(message).Forget();
                }
                else
                {
                    var action = new Raid
                    {
                        AvatarAddress = currentAvatarState.address,
                        CostumeIds = itemSlotStates[2].Costumes,
                        EquipmentIds = itemSlotStates[2].Equipments,
                        FoodIds = new List<Guid>(),
                        RuneInfos = runeSlotStates[2].GetEquippedRuneSlotInfos(),
                        PayNcg = false,
                    };

                    ActionManager.Instance.PreProcessAction(action, currentAvatarState, analyzeText);
                }


                OneLineSystem.Push(MailType.System,
                    $"<color=green>Pandora Box</color>: <color=red>{currentAvatarState.name}</color> sent World Boss Fight!",
                    NotificationCell.NotificationType.Information);
            }
            catch (Exception e)
            {
                PandoraUtil.ShowSystemNotification(
                    currentAvatarState.name + " Fight Sets is not register all, you should have 3 sets!",
                    NotificationCell.NotificationType.Alert);
            }
        }

        public static StageSimulator PVE_StageSimulator(int _worldId, int _stageId, List<Guid> consumables,
            int skillId = -1)
        {
            var avatarSlotIndex = States.Instance.AvatarStates
                .FirstOrDefault(x => x.Value.address == States.Instance.CurrentAvatarState.address).Key;
            var itemSlotState = States.Instance.ItemSlotStates[avatarSlotIndex][BattleType.Adventure];
            var equipments = itemSlotState.Equipments;
            var costumes = itemSlotState.Costumes;
            var runeStates = States.Instance.GetEquippedRuneStates(BattleType.Adventure);
            var tableSheets = Game.Game.instance.TableSheets;
            var avatarState = States.Instance.CurrentAvatarState;
            var items = new List<Guid>();
            items.AddRange(equipments);
            items.AddRange(costumes);
            avatarState.EquipItems(items);
            List<Model.Skill.Skill> buffSkills = new List<Model.Skill.Skill>();
            if (skillId != -1 && PandoraProfile.IsPremium())
            {
                var skill = CrystalRandomSkillState.GetSkill(
                    skillId,
                    tableSheets.CrystalRandomBuffSheet,
                    tableSheets.SkillSheet);
                buffSkills.Add(skill);
            }

            return new StageSimulator(
                new Cheat.DebugRandom(),
                avatarState,
                consumables,
                runeStates,
                buffSkills,
                _worldId,
                _stageId,
                tableSheets.StageSheet[_stageId],
                tableSheets.StageWaveSheet[_stageId],
                avatarState.worldInformation.IsStageCleared(_stageId),
                StageRewardExpHelper.GetExp(avatarState.level, _stageId),
                tableSheets.GetStageSimulatorSheets(),
                tableSheets.EnemySkillSheet,
                tableSheets.CostumeStatSheet,
                StageSimulator.GetWaveRewards(new Cheat.DebugRandom(), tableSheets.StageSheet[_stageId],
                    tableSheets.MaterialItemSheet),
                PandoraMaster.IsHackAndSlashSimulate
            );
        }

        public static async UniTask PVE_AutoEventDungeon(AvatarState currentAvatarState, int eventDungeonId, int count)
        {
            if (!PandoraProfile.IsPremium())
                return;

            if (RxProps.EventScheduleRowForDungeon.Value is null)
            {
                OneLineSystem.Push(MailType.System, $"<color=green>Pandora Box</color>: No Event Found!",
                    NotificationCell.NotificationType.Alert);
                return;
            }

            try //in case actionManager is not ready yet
            {
                var (itemSlotStates, runeSlotStates) = await currentAvatarState.GetSlotStatesAsync();

                for (int i = 0; i < count; i++)
                {
                    string analyzeText = $"**EventDungeonBattle** > {eventDungeonId} > {i + 1}/{count}";
                    if (currentAvatarState.address == States.Instance.CurrentAvatarState.address)
                    {
                        ActionManager.Instance.EventDungeonBattle(
                            RxProps.EventScheduleRowForDungeon.Value.Id,
                            10030001,
                            eventDungeonId,
                            itemSlotStates[0].Equipments,
                            itemSlotStates[0].Costumes,
                            null,
                            runeSlotStates[0].GetEquippedRuneSlotInfos(),
                            false).Subscribe();

                        //analyze actions
                        string message =
                            $"[v{PandoraMaster.VersionId.Substring(3)}][{Game.Game.instance.Agent.BlockIndex}] **{currentAvatarState.name}** Lv.**{currentAvatarState.level}** " +
                            $"<:NCG:1009757564256407592>**{States.Instance.GoldBalanceState.Gold.MajorUnit}** > {currentAvatarState.agentAddress}, " +
                            analyzeText;
                        ActionManager.Instance.AnalyzeActions(message).Forget();
                    }
                    else
                    {
                        var action = new EventDungeonBattle
                        {
                            AvatarAddress = currentAvatarState.address,
                            EventScheduleId = RxProps.EventScheduleRowForDungeon.Value.Id,
                            EventDungeonId = 10030001,
                            EventDungeonStageId = eventDungeonId,
                            Equipments = itemSlotStates[0].Equipments,
                            Costumes = itemSlotStates[0].Costumes,
                            Foods = new List<Guid>(),
                            BuyTicketIfNeeded = false,
                            RuneInfos = runeSlotStates[0].GetEquippedRuneSlotInfos(),
                        };
                        ActionManager.Instance.PreProcessAction(action, currentAvatarState, analyzeText);
                    }
                }

                OneLineSystem.Push(MailType.System,
                    $"<color=green>Pandora Box</color>: <color=red>{currentAvatarState.name}</color> sent {count} Event tickets!",
                    NotificationCell.NotificationType.Information);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        public static async UniTask<string> PVE_AutoStageRepeat(AvatarState currentAvatarState)
        {
            if (!PandoraProfile.IsPremium())
                return "This is not <b>Premium</b> account!";


            int _stageId = 1;
            currentAvatarState.worldInformation.TryGetLastClearedStageId(out _stageId);
            _stageId = Math.Clamp(_stageId + 1, 1, 300);

            int worldID = 0;
            if (_stageId < 51)
                worldID = 1;
            else if (_stageId > 50 && _stageId < 101)
                worldID = 2;
            else if (_stageId > 100 && _stageId < 151)
                worldID = 3;
            else if (_stageId > 150 && _stageId < 201)
                worldID = 4;
            else if (_stageId > 200 && _stageId < 251)
                worldID = 5;
            else if (_stageId > 250 && _stageId < 301)
                worldID = 6;

            var playCount = 0;
            var stage = Game.TableSheets.Instance.StageSheet.OrderedList.FirstOrDefault(i => i.Id == _stageId);
            if (!(stage is null))
                playCount = currentAvatarState.actionPoint / stage.CostAP;

            try //in case actionManager is not ready yet
            {
                if (!(States.Instance is null) && !(ActionManager.Instance is null))
                {
                    var (itemSlotStates, runeSlotStates) = await currentAvatarState.GetSlotStatesAsync();
                    if (itemSlotStates.Count < 1)
                        return "there is no SlotStates";
                    string analyzeText = $"HackAndSlash> {currentAvatarState.actionPoint}> **{_stageId}**";

                    if (currentAvatarState.address == States.Instance.CurrentAvatarState.address)
                    {
                        ActionManager.Instance.HackAndSlash(
                            itemSlotStates[0].Costumes,
                            itemSlotStates[0].Equipments,
                            new List<Consumable>(),
                            runeSlotStates[0].GetEquippedRuneSlotInfos(),
                            worldID,
                            _stageId,
                            playCount).Subscribe();

                        //analyze actions
                        string message =
                            $"[v{PandoraMaster.VersionId.Substring(3)}][{Game.Game.instance.Agent.BlockIndex}] **{currentAvatarState.name}** Lv.**{currentAvatarState.level}** " +
                            $"<:NCG:1009757564256407592>**{States.Instance.GoldBalanceState.Gold.MajorUnit}** > {currentAvatarState.agentAddress}, " +
                            analyzeText;
                        ActionManager.Instance.AnalyzeActions(message).Forget();
                        return "";
                    }
                    else
                    {
                        var action = new HackAndSlash
                        {
                            Costumes = itemSlotStates[0].Costumes,
                            Equipments = itemSlotStates[0].Equipments,
                            Foods = new List<Guid>(),
                            RuneInfos = runeSlotStates[0].GetEquippedRuneSlotInfos(),
                            WorldId = worldID,
                            StageId = _stageId,
                            AvatarAddress = currentAvatarState.address,
                            TotalPlayCount = playCount,
                        };
                        ActionManager.Instance.PreProcessAction(action, currentAvatarState, analyzeText);
                        return "";
                    }
                }
                else
                {
                    return "ActionManager or States not <b>Ready</b>!";
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return e.Message;
            }
        }

        public static async UniTask<string> PVE_AutoStageSweep(AvatarState currentAvatarState, int sweepStage)
        {
            if (!PandoraProfile.IsPremium())
                return "This is not <b>Premium</b> account!";

            int worldID = 0;
            if (sweepStage < 51)
                worldID = 1;
            else if (sweepStage > 50 && sweepStage < 101)
                worldID = 2;
            else if (sweepStage > 100 && sweepStage < 151)
                worldID = 3;
            else if (sweepStage > 150 && sweepStage < 201)
                worldID = 4;
            else if (sweepStage > 200 && sweepStage < 251)
                worldID = 5;
            else if (sweepStage > 250 && sweepStage < 301)
                worldID = 6;

            try //in case actionManager is not ready yet
            {
                if (!(States.Instance is null) && !(ActionManager.Instance is null))
                {
                    var (itemSlotStates, runeSlotStates) = await currentAvatarState.GetSlotStatesAsync();
                    if (itemSlotStates.Count < 1)
                        return "there is no SlotStates";
                    string analyzeText = $"HackAndSlashSweep> {currentAvatarState.actionPoint}> **{sweepStage}**";
                    if (currentAvatarState.address == States.Instance.CurrentAvatarState.address)
                    {
                        Game.Game.instance.ActionManager.HackAndSlashSweep(
                            itemSlotStates[0].Costumes,
                            itemSlotStates[0].Equipments,
                            runeSlotStates[0].GetEquippedRuneSlotInfos(),
                            0,
                            currentAvatarState.actionPoint,
                            worldID,
                            sweepStage,
                            currentAvatarState.actionPoint).Subscribe();
                        //analyze actions
                        string message =
                            $"[v{PandoraMaster.VersionId.Substring(3)}][{Game.Game.instance.Agent.BlockIndex}] **{currentAvatarState.name}** Lv.**{currentAvatarState.level}** " +
                            $"<:NCG:1009757564256407592>**{States.Instance.GoldBalanceState.Gold.MajorUnit}** > {currentAvatarState.agentAddress}, " +
                            analyzeText;
                        ActionManager.Instance.AnalyzeActions(message).Forget();
                        return "";
                    }
                    else
                    {
                        var action = new HackAndSlashSweep
                        {
                            costumes = itemSlotStates[0].Costumes,
                            equipments = itemSlotStates[0].Equipments,
                            runeInfos = runeSlotStates[0].GetEquippedRuneSlotInfos(),
                            avatarAddress = currentAvatarState.address,
                            apStoneCount = 0,
                            actionPoint = currentAvatarState.actionPoint,
                            worldId = worldID,
                            stageId = sweepStage,
                        };
                        ActionManager.Instance.PreProcessAction(action, currentAvatarState, analyzeText);
                        return "";
                    }
                }
                else
                {
                    return "ActionManager or States not <b>Ready</b>!";
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return e.Message;
            }
        }

        public static bool PVE_SweepMoreStones(int apStoneCount, List<Guid> _costumes, List<Guid> _equipments,
            List<RuneSlotInfo> _runes
            , int worldId, StageSheet.Row stageRow)
        {
            if (!PANDORA_CheckPremium())
                return false;

            int extraApStoneCount = Mathf.FloorToInt(apStoneCount / 10f);
            apStoneCount -= extraApStoneCount * 10;

            var (apPlayCount, apStonePlayCount)
                = PandoraUtil.GetPlayCount(stageRow, apStoneCount, 0, States.Instance.StakingLevel);
            var totalPlayCount = apPlayCount + apStonePlayCount;

            for (int i = 0; i < extraApStoneCount; i++)
            {
                Game.Game.instance.ActionManager.HackAndSlashSweep(
                    _costumes,
                    _equipments,
                    _runes,
                    10,
                    0,
                    worldId,
                    stageRow.Id, totalPlayCount);
            }

            return true;
        }

        public static async void PVE_MultiRepeat(Action<StageType, int, int, bool> _repeatBattleAction
            , ReactiveProperty<int> _ap, StageSheet.Row _stageRow, int _costAp, int iteration,
            Model.Item.Material apStone)
        {
            if (!PANDORA_CheckPremium())
                return;

            if (PandoraMaster.CurrentAction != PandoraUtil.ActionType.Idle)
            {
                OneLineSystem.Push(MailType.System,
                    "<color=green>Pandora Box</color>: Other actions in progress, please wait!",
                    NotificationCell.NotificationType.Alert);
                return;
            }

            PandoraMaster.CurrentAction = PandoraUtil.ActionType.HackAndSlash; //repeat

            //do the ap bar first
            if (_ap.Value >= _stageRow.CostAP)
            {
                _repeatBattleAction(
                    StageType.HackAndSlash,
                    _ap.Value / _costAp,
                    0,
                    false);

                if (iteration > 0)
                    OneLineSystem.Push(MailType.System,
                        "<color=green>Pandora Box</color>: Sending repeat by AP bar!",
                        NotificationCell.NotificationType.Information);
            }

            //repeat the stones
            for (int i = 0; i < iteration; i++)
            {
                ActionManager.Instance.ChargeActionPoint(apStone)
                    .Subscribe();
                await Task.Delay(System.TimeSpan.FromSeconds(2));
                OneLineSystem.Push(MailType.System,
                    $"<color=green>Pandora Box</color>: Sending Repeat using AP Stone {i + 1}/{iteration}",
                    NotificationCell.NotificationType.Information);
                _repeatBattleAction(
                    StageType.HackAndSlash,
                    120 / _costAp,
                    0,
                    false);
                await Task.Delay(System.TimeSpan.FromSeconds(2));
            }

            Widget.Find<SweepPopup>().Close();
        }

        public static StageSimulator PVE_SoloSimulate(int _worldId, int _stageId, List<Guid> consumables,
            int skillId = -1)
        {
            return PVE_StageSimulator(_worldId, _stageId, consumables, skillId);
        }

        public static async void PVE_MultiSimulate(int _worldId, int _stageId, List<Guid> consumables, int skillId = -1)
        {
            if (!PANDORA_CheckPremium())
                return;

            var preparePVE = Widget.Find<BattlePreparation>();
            preparePVE.MultipleSimulateButton.interactable = false;
            preparePVE.MultipleSimulateButton.GetComponentInChildren<TextMeshProUGUI>().text = "Simulating...";
            foreach (var item in preparePVE.winStarTexts)
                item.text = "?";

            int totalSimulations = 200;
            int[] winStars = { 0, 0, 0 };

            for (int i = 0; i < totalSimulations; i++)
            {
                PandoraMaster.IsHackAndSlashSimulate = true;
                var simulator = PVE_StageSimulator(_worldId, _stageId, consumables, skillId);
                simulator.Simulate();
                await Task.Delay(1);

                var log = simulator.Log;
                PandoraMaster.IsHackAndSlashSimulate = false;

                if (log.clearedWaveNumber == 3)
                {
                    winStars[2]++;
                    winStars[1]++;
                    winStars[0]++;
                }
                else if (log.clearedWaveNumber == 2)
                {
                    winStars[1]++;
                    winStars[0]++;
                }
                else if (log.clearedWaveNumber == 1)
                {
                    winStars[0]++;
                }
            }

            for (int i = 0; i < 3; i++)
            {
                float finalRatio = (float)winStars[i] / (float)totalSimulations;
                float FinalValue = (int)(finalRatio * 100f);

                if (finalRatio <= 0.5f)
                    preparePVE.winStarTexts[i].text = $"<color=#59514B>{FinalValue}</color>%";
                else if (finalRatio > 0.5f && finalRatio <= 0.75f)
                    preparePVE.winStarTexts[i].text = $"<color=#CD8756>{FinalValue}</color>%";
                else
                    preparePVE.winStarTexts[i].text = $"<color=#50A931>{FinalValue}</color>%";
            }

            preparePVE.MultipleSimulateButton.interactable = true;
            preparePVE.MultipleSimulateButton.GetComponentInChildren<TextMeshProUGUI>().text = "100 X Simulate";
        }

        #endregion

        #region PVP METHODS

        static int myLastPushBattle = 0;

        static string PVP_MultiWinRate(ArenaPlayerDigest mD, ArenaPlayerDigest eD, int iterations)
        {
            string result = "";
            int totalSimulations = iterations;
            int win = 0;

            var tableSheets = Game.Game.instance.TableSheets;

            for (int i = 0; i < totalSimulations; i++)
            {
                var simulator = new ArenaSimulator(new Cheat.DebugRandom());
                var log = simulator.Simulate(
                    mD,
                    eD,
                    tableSheets.GetArenaSimulatorSheets());

                if (log.Result == Nekoyume.Model.BattleStatus.Arena.ArenaLog.ArenaResult.Win)
                    win++;
            }

            float finalRatio = (float)win / (float)totalSimulations;
            float FinalValue = finalRatio * 100f;

            if (finalRatio <= 0.5f)
                result = $"<color=#59514B>{String.Format("{0:0.0}", FinalValue)}</color>%";
            else if (finalRatio > 0.5f && finalRatio <= 0.75f)
                result = $"<color=#CD8756>{String.Format("{0:0.0}", FinalValue)}</color>%";
            else
                result = $"<color=#50A931>{String.Format("{0:0.0}", FinalValue)}</color>%";

            return result;
        }

        public static void PVP_CancelPendingFights()
        {
            int totalSpent = ArenaMaxBattleCount - ArenaRemainsBattle + 1;
            if (PandoraMaster.Instance.Settings.ArenaPush)
                totalSpent = ArenaMaxBattleCount - ArenaRemainsBattle;

            Widget.Find<ArenaBoard>().MultipleSlider.gameObject.SetActive(false);
            OneLineSystem.Push(MailType.System,
                $"<color=green>Pandora Box</color>: Multi Arena Canceled, <color=green>" +
                $"{totalSpent}</color>/{ArenaMaxBattleCount} Used!",
                NotificationCell.NotificationType.Information);
            ArenaBattleInProgress = false;
            ArenaRemainsBattle = 0;
            ArenaMaxBattleCount = 0;
        }

        public static void PVP_ChangeTicketsCount(float SliderValue, ArenaSheet.RoundData _roundData)
        {
            var arena = Widget.Find<ArenaBattlePreparation>();
            arena.CurrentTicketsText.text = SliderValue.ToString();

            var hasEnoughTickets =
                RxProps.ArenaTicketsProgress.HasValue &&
                RxProps.ArenaTicketsProgress.Value.currentTickets >= SliderValue;

            if (hasEnoughTickets)
                arena.ExpectedCostText.text = "0 NCG";
            else
            {
                var gold = States.Instance.GoldBalanceState.Gold;
                var startPrice = ArenaHelper.GetTicketPrice(
                    _roundData,
                    RxProps.PlayersArenaParticipant.Value.CurrentArenaInfo,
                    gold.Currency);

                var finalNeeded = SliderValue - RxProps.ArenaTicketsProgress.Value.currentTickets;
                var finalCost = new Libplanet.Assets.FungibleAssetValue(gold.Currency, 0, 0);
                for (int i = 0; i < finalNeeded; i++)
                {
                    System.Numerics.BigInteger increment = (System.Numerics.BigInteger)(40f * i);
                    if (increment < 100)
                        finalCost += startPrice + new Libplanet.Assets.FungibleAssetValue(gold.Currency, 0, increment);
                    else
                    {
                        var toInt = (int)(increment / 100);
                        finalCost += startPrice +
                                     new Libplanet.Assets.FungibleAssetValue(gold.Currency, toInt, increment % 100);
                    }
                }

                arena.ExpectedCostText.text = finalCost.MajorUnit + "." + finalCost.MinorUnit + " NCG";
            }

            arena.ExpectedBlockText.text = "#" + (Game.Game.instance.Agent.BlockIndex + (SliderValue * 4));
        }

        public static void PVP_ExpectedTicketsToReach(TextMeshProUGUI tickets, TextMeshProUGUI ranks)
        {
            if (!PANDORA_CheckPremium() || Widget.Find<ArenaBoard>()._useGrandFinale)
            {
                tickets.text = "...\n...\n...\n...\n...\n";
                ranks.text = "Top 10 (-)\nTop 35 (-)\nTop 60 (-)\nTop 100 (-)\nTop 200 (-)\n";
                return;
            }

            int myscore = RxProps.PlayersArenaParticipant.Value.Score;
            string ticketsTxt = "";
            string ranksTxt = "";
            var players = Widget.Find<ArenaBoard>()._boundedData;

            int difference = (players[10].Score - myscore) / 20;
            ticketsTxt += difference <= 0 ? "-\n" : $"<color=green>+{difference}</color>\n";
            ranksTxt += "Top 10 (" + players[10].Score + "):\n";

            difference = (players[35].Score - myscore) / 20;
            ticketsTxt += difference <= 0 ? "-\n" : $"<color=green>+{difference}</color>\n";
            ranksTxt += "Top 35 (" + players[35].Score + "):\n";

            difference = (players[60].Score - myscore) / 20;
            ticketsTxt += difference <= 0 ? "-\n" : $"<color=green>+{difference}</color>\n";
            ranksTxt += "Top 60 (" + players[60].Score + "):\n";

            difference = (players[100].Score - myscore) / 20;
            ticketsTxt += difference <= 0 ? "-\n" : $"<color=green>+{difference}</color>\n";
            ranksTxt += "Top 100 (" + players[100].Score + "):\n";

            difference = (players[200].Score - myscore) / 20;
            ticketsTxt += difference <= 0 ? "-\n" : $"<color=green>+{difference}</color>\n";
            ranksTxt += "Top 200 (" + players[200].Score + "):\n";

            tickets.text = ticketsTxt;
            ranks.text = ranksTxt;
        }

        public static void PVP_OnConfirmBattleEnd(string enemyName, string result, string score, long blockIndex)
        {
            Game.Game.instance.Arena.IsAvatarStateUpdatedAfterBattle = true;
            ArenaBattleInProgress = false;
            ActionRenderHandler.Instance.Pending = false;
            ArenaRemainsBattle--;
            string message = $"You <color=green>{result}</color> against " +
                             $"({enemyName}) and got <color=green>{score}</color> points!";
            OneLineSystem.Push(MailType.System, "<color=green>Pandora Box</color>: " + message,
                NotificationCell.NotificationType.Information);
            Widget.Find<ArenaBoard>().LastFightText.text = $"[<color=green>{blockIndex}</color>] {message}";
            Widget.Find<UI.Module.EventBanner>().Close(true);
            Widget.Find<ArenaBoard>().ShowPandora();
        }

        public static bool PVP_MultiConfirmBattles(float count, Address _chooseAvatarStateAddress,
            int roundDataChampionshipId, int _roundDataRound, int _ticketCountToUse)
        {
            if (!PANDORA_CheckPremium())
                return false;

            var avatarSlotIndex = States.Instance.AvatarStates
                .FirstOrDefault(x => x.Value.address == States.Instance.CurrentAvatarState.address).Key;

            ArenaMaxBattleCount = (int)count;
            ArenaRemainsBattle = ArenaMaxBattleCount;
            enemyAvatarAddress = _chooseAvatarStateAddress;
            costumes = States.Instance.ItemSlotStates[avatarSlotIndex][BattleType.Arena].Costumes;
            equipments = States.Instance.ItemSlotStates[avatarSlotIndex][BattleType.Arena].Equipments;
            runeInfos = States.Instance.RuneSlotStates[avatarSlotIndex][BattleType.Arena].GetEquippedRuneSlotInfos();
            championshipId = roundDataChampionshipId;
            round = _roundDataRound;
            ticket = _ticketCountToUse;
            return true;
        }

        public static async void PVP_CheckPendingConfirmFights()
        {
            if (!PandoraProfile.IsPremium() || ArenaBattleInProgress)
                return;

            if (ArenaRemainsBattle > 0)
            {
                Slider multiSlider = Widget.Find<ArenaBoard>().MultipleSlider;
                multiSlider.maxValue = ArenaMaxBattleCount;
                multiSlider.value = (ArenaMaxBattleCount - ArenaRemainsBattle) + 1;
                multiSlider.transform.Find("TxtMax").GetComponent<TextMeshProUGUI>().text =
                    (ArenaMaxBattleCount - ArenaRemainsBattle + 1) + "/" + ArenaMaxBattleCount;

                var blocksBetweenBattles = 4; // Fixed Planet Policy
                var versionWidget = Widget.Find<VersionSystem>();
                var nodeBlockValidator = PandoraMaster.Instance.Settings.ArenaValidator
                    ? versionWidget.NodeBlockIndex
                    : Game.Game.instance.Agent.BlockIndex;
                var lastArenaBattle = Widget.Find<ArenaBoard>().myLastBattle; //last commited arena fight
                while (nodeBlockValidator < lastArenaBattle + blocksBetweenBattles)
                {
                    await Task.Delay(System.TimeSpan.FromSeconds(3));
                    if (ArenaRemainsBattle <= 0)
                        return;

                    //update values
                    lastArenaBattle = Widget.Find<ArenaBoard>().myLastBattle;
                    nodeBlockValidator = PandoraMaster.Instance.Settings.ArenaValidator
                        ? versionWidget.NodeBlockIndex
                        : Game.Game.instance.Agent.BlockIndex;
                }


                ActionRenderHandler.Instance.Pending = true;
                ArenaBattleInProgress = true;
                OneLineSystem.Push(MailType.System, $"<color=green>Pandora Box</color>: Fight " +
                                                    $"{(ArenaMaxBattleCount - ArenaRemainsBattle + 1)}/{ArenaMaxBattleCount} Sent!",
                    NotificationCell.NotificationType.Information);
                ActionRenderHandler.Instance.Pending = true;
                ActionManager.Instance.BattleArena(enemyAvatarAddress, costumes, equipments, runeInfos, championshipId,
                    round, ticket).Subscribe();
            }
        }

        public static async void PVP_SoloSimulate(AvatarState myAvatarState, AvatarState enemyAvatarState)
        {
            if (!PANDORA_CheckPremium())
                return;

            PandoraMaster.CurrentArenaEnemyAddress = enemyAvatarState.address.ToString().ToLower();
            PandoraMaster.IsRankingSimulate = true;

            //my data
            var avatarSlotIndex = States.Instance.AvatarStates
                .FirstOrDefault(x => x.Value.address == myAvatarState.address).Key;
            var myItemSlotState = States.Instance.ItemSlotStates[avatarSlotIndex][BattleType.Arena];
            var myRuneStates = States.Instance.GetEquippedRuneStates(BattleType.Arena);
            var myDigest = new ArenaPlayerDigest(myAvatarState, myItemSlotState.Equipments, myItemSlotState.Costumes,
                myRuneStates);

            //enemy data
            var (enemyItemSlotStates, enemyRuneSlotStates) = await enemyAvatarState.GetSlotStatesAsync();
            var enemyItemSlotState = enemyItemSlotStates.FirstOrDefault(x => x.BattleType == BattleType.Arena);
            var enemyRuneSlotState = enemyRuneSlotStates.FirstOrDefault(x => x.BattleType == BattleType.Arena);

            if (enemyItemSlotState == null)
                enemyItemSlotState = new ItemSlotState(BattleType.Arena);

            if (enemyRuneSlotState == null)
                enemyRuneSlotState = new RuneSlotState(BattleType.Arena);

            var enemyRuneStates = await enemyAvatarState.GetRuneStatesAsync();
            var enemyDigest = new ArenaPlayerDigest(enemyAvatarState, enemyItemSlotState.Equipments,
                enemyItemSlotState.Costumes, enemyRuneStates);

            var tableSheets = Game.Game.instance.TableSheets;
            var simulator = new ArenaSimulator(new Cheat.DebugRandom());
            var log = simulator.Simulate(
                myDigest,
                enemyDigest,
                tableSheets.GetArenaSimulatorSheets());

            Widget.Find<FriendInfoPopupPandora>().Close(true);
            PandoraMaster.IsRankingSimulate = true;


            var rewards = RewardSelector.Select(
                new Cheat.DebugRandom(),
                tableSheets.WeeklyArenaRewardSheet,
                tableSheets.MaterialItemSheet,
                myDigest.Level,
                maxCount: ArenaHelper.GetRewardCount(3));

            Game.Game.instance.Arena.Enter(
                log,
                rewards,
                myDigest,
                enemyDigest);
            Widget.Find<ArenaBoard>().Close();
        }

        public static async Task<string> PVP_WinRate(AvatarState myAvatarState, AvatarState enemyAvatarState,
            int iterations)
        {
            if (!PandoraProfile.IsPremium())
                return "";

            //my data
            var avatarSlotIndex = States.Instance.AvatarStates
                .FirstOrDefault(x => x.Value.address == myAvatarState.address).Key;
            var myItemSlotState = States.Instance.ItemSlotStates[avatarSlotIndex][BattleType.Arena];
            var myRuneStates = States.Instance.GetEquippedRuneStates(BattleType.Arena);
            var myDigest = new ArenaPlayerDigest(myAvatarState, myItemSlotState.Equipments, myItemSlotState.Costumes,
                myRuneStates);

            //enemy data
            var (enemyItemSlotStates, enemyRuneSlotStates) = await enemyAvatarState.GetSlotStatesAsync();
            var enemyItemSlotState = enemyItemSlotStates.FirstOrDefault(x => x.BattleType == BattleType.Arena);
            var enemyRuneSlotState = enemyRuneSlotStates.FirstOrDefault(x => x.BattleType == BattleType.Arena);

            if (enemyItemSlotState == null)
                enemyItemSlotState = new ItemSlotState(BattleType.Arena);

            if (enemyRuneSlotState == null)
                enemyRuneSlotState = new RuneSlotState(BattleType.Arena);

            var enemyRuneStates = await enemyAvatarState.GetRuneStatesAsync();
            var enemyDigest = new ArenaPlayerDigest(enemyAvatarState, enemyItemSlotState.Equipments,
                enemyItemSlotState.Costumes, enemyRuneStates);

            return PVP_MultiWinRate(myDigest, enemyDigest, iterations);
        }

        public static (Address avatarAddr, int score, int rank)[] PVP_GetListRange(
            (Address avatarAddr, int score, int rank)[] tuples, ArenaType arenaType, int playerScore,
            int currentPlayerRank)
        {
            int upper = 10 + (PandoraMaster.Instance.Settings.ArenaListUpper *
                              PandoraMaster.Instance.Settings.ArenaListStep);
            int lower = 10 + (PandoraMaster.Instance.Settings.ArenaListLower *
                              PandoraMaster.Instance.Settings.ArenaListStep);

            if (PandoraProfile.IsPremium())
                return tuples.Where(tuple =>
                    tuple.rank <= 300 || (tuple.rank >= currentPlayerRank - upper &&
                                          tuple.rank <= currentPlayerRank + lower)).ToArray();
            else
                return tuples.Where(tuple =>
                    tuple.rank <= 200 || (tuple.rank >= currentPlayerRank - upper &&
                                          tuple.rank <= currentPlayerRank + lower)).ToArray();
        }

        public static async void PVP_PushFight()
        {
            if (!PandoraProfile.IsPremium())
                return;

            if (ArenaRemainsBattle > 0)
            {
                Slider multiSlider = Widget.Find<ArenaBoard>().MultipleSlider;
                multiSlider.maxValue = ArenaMaxBattleCount;
                multiSlider.value = (ArenaMaxBattleCount - ArenaRemainsBattle);
                multiSlider.transform.Find("TxtMax").GetComponent<TextMeshProUGUI>().text =
                    (ArenaMaxBattleCount - ArenaRemainsBattle) + "/" + ArenaMaxBattleCount;

                var blocksBetweenBattles = PandoraMaster.Instance.Settings.ArenaPushStep;
                var versionWidget = Widget.Find<VersionSystem>();
                var nodeBlockValidator = PandoraMaster.Instance.Settings.ArenaValidator
                    ? versionWidget.NodeBlockIndex
                    : Game.Game.instance.Agent.BlockIndex;

                while (nodeBlockValidator < myLastPushBattle + blocksBetweenBattles)
                {
                    await Task.Delay(System.TimeSpan.FromSeconds(1));
                    if (ArenaRemainsBattle <= 0)
                        return;

                    //update values
                    nodeBlockValidator = PandoraMaster.Instance.Settings.ArenaValidator
                        ? versionWidget.NodeBlockIndex
                        : Game.Game.instance.Agent.BlockIndex;
                }

                myLastPushBattle = (int)nodeBlockValidator; // 2 = this is the estimation block needed to send the tx 
                OneLineSystem.Push(MailType.System, $"<color=green>Pandora Box</color>: Fight " +
                                                    $"{(ArenaMaxBattleCount - ArenaRemainsBattle + 1)}/{ArenaMaxBattleCount} Sent!",
                    NotificationCell.NotificationType.Information);

                ActionManager.Instance.BattleArena(enemyAvatarAddress, costumes, equipments, runeInfos, championshipId,
                    round, ticket).Subscribe();

                Game.Game.instance.Arena.IsAvatarStateUpdatedAfterBattle = true;
                ArenaBattleInProgress = true;
                ActionRenderHandler.Instance.Pending = false;
                ArenaRemainsBattle--;
                Widget.Find<UI.Module.EventBanner>().Close(true);
                Widget.Find<ArenaBoard>().ShowPandora();
            }
        }

        #endregion

        #region SHOP METHODS

        public static void SHOP_FeatureItem(string itemID)
        {
            //request buyMarketFeature cloud
            PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest
                {
                    FunctionName = "buyMarketFeature",
                    FunctionParameter = new
                    {
                        itemID = itemID,
                        currentBlock = Game.Game.instance.Agent.BlockIndex,
                        address = States.Instance.CurrentAvatarState.agentAddress.ToString()
                    }
                },
                success =>
                {
                    if (success.FunctionResult.ToString() == "Success")
                    {
                        //adding score success
                        AudioController.instance.PlaySfx(AudioController.SfxCode.BuyItem);
                        Premium.PandoraProfile.Currencies["PG"] -=
                            PandoraMaster.PanDatabase.ShopFeaturePrice; //just UI update instead of request new call
                        NotificationSystem.Push(Nekoyume.Model.Mail.MailType.System,
                            $"PandoraBox: Feature Item Request <color=green>Sent!</color>",
                            NotificationCell.NotificationType.Information);
                    }
                    else
                    {
                        NotificationSystem.Push(Nekoyume.Model.Mail.MailType.System,
                            "PandoraBox: Feature Item Request <color=red>Failed!</color>",
                            NotificationCell.NotificationType.Alert);
                    }
                },
                failed => { Debug.LogError("Process Failed!, " + failed.GenerateErrorReport()); });
        }

        public static void SHOP_CancelAll(SellView view)
        {
            if (!PANDORA_CheckPremium())
                return;

            OneLineSystem.Push(MailType.System,
                $"<color=green>Pandora Box</color>: Cancel All items Process Started...",
                NotificationCell.NotificationType.Information);

            var digests = ReactiveShopState.SellDigest.Value;
            var orderDigests = digests.ToList();

            view.SetLoading(orderDigests);

            foreach (var orderDigest in orderDigests)
            {
                if (!ReactiveShopState.TryGetShopItem(orderDigest, out var itemBase))
                {
                    return;
                }

                if (!(itemBase is ITradableItem tradableItem))
                {
                    continue;
                }

                var avatarAddress = States.Instance.CurrentAvatarState.address;
                var tradableId = tradableItem.TradableId;
                var requiredBlockIndex = tradableItem.RequiredBlockIndex;
                var subType = tradableItem.ItemSubType;
                var price = orderDigest.Price;
                var count = orderDigest.ItemCount;

                var digest = ReactiveShopState.GetSellDigest(tradableId, requiredBlockIndex, price, count);

                Game.Game.instance.ActionManager.SellCancellation(
                    avatarAddress,
                    digest.OrderId,
                    digest.TradableId,
                    subType).Subscribe();
            }
        }

        public static void SHOP_CancelLast()
        {
            if (!PANDORA_CheckPremium())
                return;

            var item = Widget.Find<ShopSell>().LastItemSold;
            if (item is null)
                return;

            try
            {
                Game.Game.instance.ActionManager.SellCancellation(
                    States.Instance.CurrentAvatarState.address,
                    Widget.Find<ShopSell>().LastItemSoldOrderID,
                    (item as ITradableItem).TradableId,
                    item.ItemSubType).Subscribe();
                OneLineSystem.Push(MailType.System,
                    $"<color=green>Pandora Box</color>: {item.GetLocalizedName()} Order Canceled!",
                    NotificationCell.NotificationType.Alert);

                Widget.Find<ShopSell>().LastItemSold = null;
                Widget.Find<ShopSell>().LastSoldTxt.text = "";
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        public static void SHOP_FirstFilter(ReactiveProperty<Nekoyume.EnumType.ShopSortFilter> _selectedSortFilter)
        {
            if (PandoraProfile.IsPremium())
                _selectedSortFilter.SetValueAndForceNotify(Nekoyume.EnumType.ShopSortFilter.Time);
            else
                _selectedSortFilter.SetValueAndForceNotify(Nekoyume.EnumType.ShopSortFilter.Class);
        }

        public static IEnumerable<ShopItem> SHOP_TimeFilter(ReactiveProperty<bool> _isAscending, List<ShopItem> models)
        {
            if (!PANDORA_CheckPremium())
                return new List<ShopItem>();

            return _isAscending.Value
                ? models.OrderBy(x => x.OrderDigest.StartedBlockIndex).ToList()
                : models.OrderByDescending(x => x.OrderDigest.StartedBlockIndex).ToList();
        }

        public static IEnumerable<ShopItem> SHOP_PandoraScoreFilter(ReactiveProperty<bool> _isAscending,
            List<ShopItem> models)
        {
            if (!PANDORA_CheckPremium())
                return new List<ShopItem>();

            return _isAscending.Value
                ? models.OrderBy(x => SHOP_GetPandoraScore(x.ItemBase)).ToList()
                : models.OrderByDescending(x => SHOP_GetPandoraScore(x.ItemBase)).ToList();
        }

        public static async Task<string> SHOP_GetItemOwnerName(Guid guid)
        {
            if (!PandoraProfile.IsPremium())
                return "<size=120%><color=green>PREMIUM FEATURE!</color>";


            var order = await Util.GetOrder(guid);
            //var ItemTooltip = Widget.Find<ItemTooltip>();
            var (exist, avatarState) = await States.TryGetAvatarStateAsync(order.SellerAvatarAddress);
            if (!exist)
                return "NOT EXIST!";
            else
            {
#if UNITY_EDITOR
                Debug.LogError(avatarState.agentAddress + "  |  " + order.OrderId + "  |  " + $"{avatarState.name} <color=#A68F7E>#{avatarState.address.ToHex().Substring(0, 4)}</color>");
#endif
                PandoraMaster.CurrentShopSellerAvatar = avatarState;
                //Widget.Find<EquipmentTooltip>().currentSellerAvatar = avatarState;

                if (PandoraProfile.IsPremium())
                    if (!CheckPremium(avatarState.agentAddress))
                        return $"<size=120%>{avatarState.NameWithHash}";
                    else
                        return "<size=120%>" + avatarState.NameWithHash + "  <sprite name=UI_main_icon_star>";
                else
                    return "<size=120%><color=green>PREMIUM FEATURE!</color>";
            }
        }

        public static bool CheckPremium(Address agentAddress)
        {
            return Pandoraplayers.Contains(agentAddress.ToString().Substring(2, 20).ToLower());
        }

        public static async void SHOP_Refresh(Action<ShopItem> clickItem)
        {
            var shopBuy = Widget.Find<ShopBuy>();
            int cooldown = 50;

            if (!PandoraProfile.IsPremium())
            {
                foreach (var item in shopBuy.EquipmentToggles)
                    item.isOn = true;
                foreach (var item in shopBuy.MainToggles)
                    item.isOn = true;
            }
            else
            {
                ReactiveShopState._buyDigest.Clear();
            }

            var initWeaponTask = Task.Run(async () =>
            {
                var list = new List<ItemSubType>();
                if (shopBuy.EquipmentToggles[0].isOn)
                    list.Add(ItemSubType.Weapon);
                //var list = new List<ItemSubType> { ItemSubType.Weapon, };
                await ReactiveShopState.SetBuyDigestsAsync(list);
                return true;
            });

            var initWeaponResult = await initWeaponTask;
            if (initWeaponResult)
            {
                //base.Show(ignoreShowAnimation);
                shopBuy.view.Show(ReactiveShopState.BuyDigest, clickItem);
            }


            CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
            var initOthersTask = Task.Run(async () =>
            {
                var list = new List<ItemSubType>();
                if (shopBuy.EquipmentToggles[1].isOn)
                    list.Add(ItemSubType.Armor);
                if (shopBuy.EquipmentToggles[2].isOn)
                    list.Add(ItemSubType.Belt);
                if (shopBuy.EquipmentToggles[3].isOn)
                    list.Add(ItemSubType.Necklace);
                if (shopBuy.EquipmentToggles[4].isOn)
                    list.Add(ItemSubType.Ring);

                if (shopBuy.MainToggles[0].isOn)
                    list.Add(ItemSubType.Food);

                if (shopBuy.MainToggles[1].isOn)
                    list.AddRange(new List<ItemSubType>
                    {
                        ItemSubType.Hourglass,
                        ItemSubType.ApStone,
                    });

                if (shopBuy.MainToggles[2].isOn)
                    list.AddRange(new List<ItemSubType>
                    {
                        ItemSubType.FullCostume,
                        ItemSubType.HairCostume,
                        ItemSubType.EarCostume,
                        ItemSubType.EyeCostume,
                        ItemSubType.TailCostume,
                        ItemSubType.Title
                    });


                await ReactiveShopState.SetBuyDigestsAsync(list);
                return true;
            }, _cancellationTokenSource.Token);

            if (initOthersTask.IsCanceled)
            {
                return;
            }

            var initOthersResult = await initOthersTask;
            if (!initOthersResult)
            {
                return;
            }

            shopBuy.view.IsDoneLoadItem = true;

            cooldown = 5;
            if (PandoraProfile.IsPremium())
                cooldown = 0;

            Button refreshButton = Widget.Find<ShopBuy>().RefreshButton;

            for (int i = cooldown; i > 0; i--)
            {
                refreshButton.GetComponentInChildren<TextMeshProUGUI>().text = i.ToString();
                await Task.Delay(1000);
            }

            refreshButton.GetComponentInChildren<TextMeshProUGUI>().text = "Refresh";
            refreshButton.interactable = true;
        }

        public static string SHOP_GetStatePercentage(int itemID, StatView statView, int starCount, int level,
            out float percent100, UI.Module.SkillView skillView = null)
        {
            percent100 = 0;
            var tableSheets = Game.TableSheets.Instance;
            var itemSheet = tableSheets.EquipmentItemRecipeSheet;
            var itemRow = itemSheet.First(x => x.Value.ResultEquipmentId == itemID).Value;

            float levelStatsMultiply = 1;
            for (int i = 0; i <= level; i++)
            {
                if (i == 4 || i == 7)
                    levelStatsMultiply *= 1.3f;
                else if (i > 7)
                    levelStatsMultiply *= 1.1f;
            }

            if (itemRow != null)
            {
                var itemSubSheet = tableSheets.EquipmentItemSubRecipeSheetV2;
                var optionSheet = tableSheets.EquipmentItemOptionSheet;
                var skillSheet = tableSheets.SkillSheet;

                var itemSub = itemSubSheet.First(x => x.Value.Id == itemRow.SubRecipeIds[1]).Value;
                var optionInfos = itemSub.Options;

                var options = optionInfos
                    .Select(x => (ratio: x.Ratio, option: optionSheet[x.Id]))
                    .ToList();

                var statOptions = optionInfos
                    .Select(x => (ratio: x.Ratio, option: optionSheet[x.Id]))
                    .Where(x => x.option.StatType != StatType.NONE)
                    .ToList();

                var skillOptions = optionInfos
                    .Select(x => (ratio: x.Ratio, option: optionSheet[x.Id]))
                    .Except(statOptions)
                    .ToList();

                float stateValue = 0;
                float statMin = 0;
                float statMax = 0;

                if (skillView is null)
                {
                    stateValue = float.Parse(statView.valueText.text);
                    var xx = Enum.TryParse(statView.statTypeText.text, out StatType statType);
                    foreach (var (ratio, option) in statOptions)
                    {
                        if (option.StatType == statType)
                        {
                            float newMin = statType == StatType.SPD || statType == StatType.DRR
                                ? (option.StatMin / 100f)
                                : option.StatMin;
                            float newMax = statType == StatType.SPD || statType == StatType.DRR
                                ? (option.StatMax / 100f)
                                : option.StatMax;
                            if (statMax == 0)
                            {
                                statMax = newMax;
                                statMin = newMin;
                            }
                            else
                            {
                                statMax += newMax;
                            }

                            if (newMin > statMin)
                                statMin = newMin;
                        }
                    }
                }
                else
                {
                    foreach (var (ratio, option) in skillOptions)
                    {
                        //if (option.SkillChanceMax > 0 )
                        //{
                        //    percent100 = 100;
                        //    return "";
                        //}
                        //else
                        {
                            try
                            {
                                stateValue = float.Parse(skillView.powerText.text.Substring(7));
                                if (skillSheet.TryGetValue(option.SkillId, out var skillRow))
                                {
                                    statMin = option.SkillDamageMin;
                                    statMax = option.SkillDamageMax;
                                }
                            }
                            catch (Exception e)
                            {
                                Debug.LogError(e);
                                //percent100 = 100;
                                return "";
                            }
                        }
                    }
                }

                //apply item level
                statMin *= levelStatsMultiply;
                statMax *= levelStatsMultiply;

                float percent = ((stateValue - statMin) / (statMax - statMin) * 100f);
                percent = Mathf.Clamp(percent, 0, 130);
                percent100 = percent;
                string precentStr = "";
                if (percent >= 75)
                    precentStr = $"<color=green>{(int)percent}</color>";
                else if (percent > 50 && percent < 75)
                    precentStr = $"<color=#FF4900>{(int)percent}</color>";
                else
                    precentStr = $"<color=red>{(int)percent}</color>";
                return $" <size=50%><color=#A68F7E>[{(int)statMin}-{(int)statMax}]</color></size> {precentStr}%";
            }
            else
                return "";
        }

        public static async UniTaskVoid SHOP_RelistAll(SellView view)
        {
            if (!PANDORA_CheckPremium())
                return;

            OneLineSystem.Push(MailType.System, $"<color=green>Pandora Box</color>: Relisting items Process Started...",
                NotificationCell.NotificationType.Information);

            var digests = ReactiveShopState.SellDigest.Value;
            var orderDigests = digests.ToList();

            if (!orderDigests.Any())
            {
                OneLineSystem.Push(MailType.System, $"<color=green>Pandora Box</color>: No Items Found!",
                    NotificationCell.NotificationType.Alert);
                return;
            }

            view.SetLoading(orderDigests);

            var oneLineSystemInfos = new List<(string name, int count)>();
            int chunkSize = 10;
            for (int i = 0; i < orderDigests.Count; i += chunkSize)
            {
                var chunk = orderDigests.Skip(i).Take(chunkSize);
                var updateSellInfos = new List<UpdateSellInfo>();
                foreach (var orderDigest in chunk)
                {
                    if (!ReactiveShopState.TryGetShopItem(orderDigest, out var itemBase))
                        continue;

                    var currentprice = orderDigest.Price;
                    var updateSellInfo = new UpdateSellInfo(
                        orderDigest.OrderId,
                        Guid.NewGuid(),
                        orderDigest.TradableId,
                        itemBase.ItemSubType,
                        currentprice,
                        orderDigest.ItemCount
                    );
                    updateSellInfos.Add(updateSellInfo);
                    oneLineSystemInfos.Add((itemBase.GetLocalizedName(), orderDigest.ItemCount));
                }

                Game.Game.instance.ActionManager.UpdateSell(updateSellInfos).Subscribe();
                PandoraUtil.ShowSystemNotification(
                    $"<color=red>{i + updateSellInfos.Count}</color>/{orderDigests.Count} Orders relisted!",
                    NotificationCell.NotificationType.Information);
                await Task.Delay(2000);
            }

            PandoraUtil.ShowSystemNotification(
                $"All <color=red>{orderDigests.Count}</color> Orders relisted successfully!",
                NotificationCell.NotificationType.Information);

            //foreach (var orderDigest in orderDigests)
            //{
            //    if (!ReactiveShopState.TryGetShopItem(orderDigest, out var itemBase))
            //        return;

            //    var currentprice = orderDigest.Price;
            //    var updateSellInfo = new UpdateSellInfo(
            //        orderDigest.OrderId,
            //        Guid.NewGuid(),
            //        orderDigest.TradableId,
            //        itemBase.ItemSubType,
            //        //orderDigest.Price,
            //        currentprice,
            //        orderDigest.ItemCount
            //    );
            //    updateSellInfos.Add(updateSellInfo);
            //    oneLineSystemInfos.Add((itemBase.GetLocalizedName(), orderDigest.ItemCount));

            //    await Task.Delay(1000);
            //}
        }

        public static int SHOP_GetPandoraScore(ItemBase itemBase)
        {
            if (!PandoraProfile.IsPremium())
                return -1;
            if (!(itemBase is Equipment equipment))
                return -1;

            if (equipment.GetOptionCountFromCombination() <= 0)
                return -1;

            var tableSheets = Game.TableSheets.Instance;
            var itemSheet = tableSheets.EquipmentItemRecipeSheet;
            var itemRow = itemSheet.First(x => x.Value.ResultEquipmentId == itemBase.Id).Value;

            float levelStatsMultiply = 1;
            for (int i = 0; i <= equipment.level; i++)
            {
                if (i == 4 || i == 7)
                    levelStatsMultiply *= 1.3f;
                else if (i > 7)
                    levelStatsMultiply *= 1.1f;
            }

            if (itemRow is null)
                return -1;

            var itemSubSheet = tableSheets.EquipmentItemSubRecipeSheetV2;
            var optionSheet = tableSheets.EquipmentItemOptionSheet;
            var skillSheet = tableSheets.SkillSheet;

            var itemSub = itemSubSheet.First(x => x.Value.Id == itemRow.SubRecipeIds[1]).Value;
            var optionInfos = itemSub.Options;

            var options = optionInfos
                .Select(x => (ratio: x.Ratio, option: optionSheet[x.Id]))
                .ToList();

            var statOptions = optionInfos
                .Select(x => (ratio: x.Ratio, option: optionSheet[x.Id]))
                .Where(x => x.option.StatType != StatType.NONE)
                .ToList();

            var skillOptions = optionInfos
                .Select(x => (ratio: x.Ratio, option: optionSheet[x.Id]))
                .Except(statOptions)
                .ToList();

            var myItemoptionInfo = new ItemOptionInfo(equipment);
            float finalPercentage = 0;
            foreach (var itemOptions in myItemoptionInfo.StatOptions)
            {
                float stateValue = itemOptions.value;
                float statMin = 0;
                float statMax = 0;
                foreach (var rawOption in statOptions)
                {
                    if (rawOption.option.StatType == itemOptions.type)
                    {
                        if (statMax == 0)
                        {
                            statMin = rawOption.option.StatMin;
                            statMax = rawOption.option.StatMax;
                        }
                        else
                        {
                            statMax += (rawOption.option.StatMax);
                        }
                    }
                }

                statMin *= levelStatsMultiply;
                statMax *= levelStatsMultiply;
                float percent = (stateValue - statMin) / (statMax - statMin) * 100f;
                percent = Mathf.Clamp(percent, 0, 130);
                finalPercentage += percent * 0.25f * itemOptions.count;
            }

            foreach (var itemOptions in myItemoptionInfo.SkillOptions)
            {
                try
                {
                    float stateValue = itemOptions.power;
                    if (stateValue != 0)
                    {
                        float statMin = 0;
                        float statMax = 0;
                        foreach (var skill in skillOptions)
                            if (skill.option.SkillId == itemOptions.skillRow.Id)
                            {
                                statMin = skill.option.SkillDamageMin;
                                statMax = skill.option.SkillDamageMax;
                            }

                        statMin *= levelStatsMultiply;
                        statMax *= levelStatsMultiply;
                        float percent = (stateValue - statMin) / (statMax - statMin) * 100f;
                        percent = Mathf.Clamp(percent, 0, 130);
                        finalPercentage += percent * 0.25f;
                    }
                    else
                    {
                        finalPercentage += 25;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }

            return (int)(Mathf.Clamp(finalPercentage, 0, 130));
        }

        #endregion
    }

    public class WebhookNotification
    {
        public string Uri;
        public string Message;
    }

    [System.Serializable]
    public class UsernamesData
    {
        public string[] usernames;
    }
}