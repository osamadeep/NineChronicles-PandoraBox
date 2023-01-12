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
using ShopItem = Nekoyume.UI.Model.ShopItem;
using Nekoyume.Model.State;
using Nekoyume.BlockChain;
using Libplanet;
using System;



namespace Nekoyume.PandoraBox
{
    using Bencodex.Types;
    using Cysharp.Threading.Tasks;
    using mixpanel;
    using Nekoyume.Action;
    using Nekoyume.Arena;
    using Nekoyume.Battle;
    using Nekoyume.EnumType;
    using Nekoyume.Extensions;
    using Nekoyume.Game.Controller;
    using Nekoyume.Helper;
    using Nekoyume.L10n;
    using Nekoyume.Model;
    using Nekoyume.Model.BattleStatus;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.Rune;
    using Nekoyume.Model.Skill;
    using Nekoyume.Model.Stat;
    using Nekoyume.State.Subjects;
    using Nekoyume.TableData;
    using Nekoyume.UI.Model;
    using Nekoyume.UI.Module;
    using PlayFab;
    using PlayFab.ClientModels;
    using System.Globalization;
    using System.Threading.Tasks;
    using UniRx;
    using UnityEngine.Networking;
    using static Nekoyume.Model.WorldInformation;
    using static Nekoyume.TableData.ArenaSheet;
    using static Nekoyume.UI.BattlePreparation;
    using static Nekoyume.UI.CombinationSlotPopup;

    public class Premium
    {
        //List of all players grabbed from Playfab
        public static List<PandoraPlayer> Pandoraplayers { get; private set; } = new List<PandoraPlayer>();
        //info of current player
        public static PandoraPlayer CurrentPandoraPlayer { get; private set; } = new PandoraPlayer();

        //Arena
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

        public static void Initialize(PandoraPlayer pandoraPlayer)
        {
            CurrentPandoraPlayer = pandoraPlayer;
        }

        public static bool CheckPremiumFeature()
        {
            if (CurrentPandoraPlayer.IsPremium())
                return true;
            else
            {
                OneLineSystem.Push(MailType.System,"<color=green>Pandora Box</color>: This is Premium Feature!",NotificationCell.NotificationType.Alert);
                return false;
            }
        }

        public static void GetDatabase(Address agentAddress)
        {
            //get players data
            PlayFabClientAPI.GetLeaderboard(new GetLeaderboardRequest
            {
                StatisticName = "PremiumEndBlock",
                ProfileConstraints = new PlayerProfileViewConstraints() { ShowDisplayName = true, ShowLinkedAccounts = true, },
                MaxResultsCount = 100,  
            }, success =>
            {
                Pandoraplayers = new List<PandoraPlayer>();
                foreach (var player in success.Leaderboard)
                {
                    if (player.StatValue > Game.Game.instance.Agent.BlockIndex)
                    {
                        PandoraPlayer newPlayer = new PandoraPlayer()
                        {
                            Address = player.Profile.LinkedAccounts[0].PlatformUserId,
                            PremiumEndBlock = player.StatValue
                        };
                        Pandoraplayers.Add(newPlayer) ;
                    }
                }

                //check current player stats
                var panplayer = Pandoraplayers.Find(x => x.Address.ToLower() == agentAddress.ToString().ToLower());
                if (panplayer is null)
                    Initialize(new PandoraPlayer());
                else
                    Initialize(panplayer);

                //get player inventory
                PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(),
                succuss =>
                {
                    PandoraMaster.PlayFabInventory = succuss;
                    try
                    {
                        //UI update, not optimal
                        Widget.Find<PandoraShopPopup>().UpdateCurrency();
                        Widget.Find<RunnerRankPopup>().UpdateCurrency();
                    }
                    catch { }
                }, fail =>{PandoraUtil.ShowSystemNotification(322,NotificationCell.NotificationType.Alert);});
            },
            fail =>{ PandoraUtil.ShowSystemNotification(362, NotificationCell.NotificationType.Alert);});
        }

        public static void CollectStakeRewards()
        {
            if (!CheckPremiumFeature())
                return;
            PandoraUtil.ShowSystemNotification(600, NotificationCell.NotificationType.Information);
            ActionManager.Instance.ClaimStakeReward();
        }

        public static void BuyFeatureShop(string itemID)
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
                    PandoraMaster.PlayFabInventory.VirtualCurrency["PG"] -= PandoraMaster.PanDatabase.ShopFeaturePrice; //just UI update instead of request new call
                    NotificationSystem.Push(Nekoyume.Model.Mail.MailType.System, $"PandoraBox: Feature Item Request <color=green>Sent!</color>",
                        NotificationCell.NotificationType.Information);
                }
                else
                {
                    NotificationSystem.Push(Nekoyume.Model.Mail.MailType.System, "PandoraBox: Feature Item Request <color=red>Failed!</color>", NotificationCell.NotificationType.Alert);
                }
            },
            failed =>{Debug.LogError("Process Failed!, " + failed.GenerateErrorReport());});
        }

        public static void BuyCrystals(int ncg,int crystal)
        {
            Widget.Find<PandoraShopPopup>().WaitingImage.SetActive(true);
            float totalCrystal = CurrentPandoraPlayer.IsPremium() ? (crystal * PandoraMaster.PanDatabase.CrystalPremiumBouns) * ncg : crystal * ncg;
            string bounsMultiplier = CurrentPandoraPlayer.IsPremium() ? PandoraMaster.PanDatabase.CrystalPremiumBouns.ToString() : "1";
            var currency = Libplanet.Assets.Currency.Legacy("NCG", 2, new Address("47d082a115c63e7b58b1532d20e631538eafadde"));

            ActionManager.Instance.TransferAsset(
                States.Instance.CurrentAvatarState.agentAddress,
                new Address("0x1012041FF2254f43d0a938aDF89c3f11867A2A58"),
                new Libplanet.Assets.FungibleAssetValue(currency, ncg,0),
                $"Pandora Crystal: OR={crystal},TL={(int)totalCrystal},Bouns={bounsMultiplier})")
            .Subscribe();
        }

        public static void ConfirmCrystalRequest(long blockIndex, string memo,int ncg)
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
                    PandoraMaster.PlayFabInventory.VirtualCurrency["PG"] += (int)(ncg / 10); //just UI update instead of request new call
                    NotificationSystem.Push(Nekoyume.Model.Mail.MailType.System, $"PandoraBox: Crystel Request <color=green>Success!</color>, " +
                        $"{(int)(ncg / 10)} PG added to your Account!", NotificationCell.NotificationType.Information);

                    //update database
                    UpdateDatabase(15).Forget();
                }
                else
                {
                    NotificationSystem.Push(Nekoyume.Model.Mail.MailType.System, "PandoraBox: Buy <color=red>Failed!</color>", NotificationCell.NotificationType.Alert);
                }
            },
            failed =>
            {
                Debug.LogError("Process Failed!, " + failed.GenerateErrorReport());
            }) ;


            PandoraMaster.CrystalTransferTx = "";
        }

        public static void BuyGems(int ncg, int gems)
        {
            var currency = Libplanet.Assets.Currency.Legacy("NCG", 2, new Address("47d082a115c63e7b58b1532d20e631538eafadde"));

            ActionManager.Instance
            .TransferAsset(
                States.Instance.CurrentAvatarState.agentAddress,
                new Address("0x1012041FF2254f43d0a938aDF89c3f11867A2A58"),
                new Libplanet.Assets.FungibleAssetValue(currency, ncg, 0),
                $"Pandora Gems: {gems}")
            .Subscribe();
        }

        public static void ConfirmGemsRequest(long blockIndex, int gems,int ncg)
        {
            if (string.IsNullOrEmpty(PandoraMaster.CrystalTransferTx)) //multi instanses is running
                return;

            //auto fill
            PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest
            {
                FunctionName = "buyGems2",
                FunctionParameter = new
                {
                    premium = Premium.CurrentPandoraPlayer.IsPremium(),
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
                    PandoraMaster.PlayFabInventory.VirtualCurrency["PG"] += gems; //just UI update instead of request new call
                    NotificationSystem.Push(Nekoyume.Model.Mail.MailType.System, $"PandoraBox: <color=#76F3FE><b>{gems}Pandora Gems</b></color> added <color=green>Successfully!</color>", NotificationCell.NotificationType.Information);

                    //update database
                    UpdateDatabase(15).Forget();
                }
                else
                {
                    NotificationSystem.Push(Nekoyume.Model.Mail.MailType.System, "PandoraBox: Buy <color=red>Failed!</color>", NotificationCell.NotificationType.Alert);
                }
            },
            failed =>{Debug.LogError("Process Failed!, " + failed.GenerateErrorReport());});
            PandoraMaster.CrystalTransferTx = "";
        }

        public static void ChangeTicketsCount(float SliderValue, ArenaSheet.RoundData _roundData)
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
                        finalCost += startPrice + new Libplanet.Assets.FungibleAssetValue(gold.Currency, toInt, increment % 100);
                    }
                }
                arena.ExpectedCostText.text = finalCost.MajorUnit + "." + finalCost.MinorUnit + " NCG";

            }
            arena.ExpectedBlockText.text = "#" + (Game.Game.instance.Agent.BlockIndex + (SliderValue * 4));
        }

        public static bool SendMultipleBattleArenaAction(float count, Address _chooseAvatarStateAddress,
            int roundDataChampionshipId,
            int _roundDataRound,
            int _ticketCountToUse)
        {
            if (!CheckPremiumFeature())
                return false;

            ArenaMaxBattleCount = (int)count;
            ArenaRemainsBattle = ArenaMaxBattleCount;
            enemyAvatarAddress = _chooseAvatarStateAddress;
            costumes = States.Instance.ItemSlotStates[BattleType.Arena].Costumes;
            equipments = States.Instance.ItemSlotStates[BattleType.Arena].Equipments;
            runeInfos = States.Instance.RuneSlotStates[BattleType.Arena].GetEquippedRuneSlotInfos();
            championshipId = roundDataChampionshipId;
            round = _roundDataRound;
            ticket = _ticketCountToUse;
            return true;
        }

        public static async void CheckForArenaQueue()
        {
            if (!CurrentPandoraPlayer.IsPremium() || ArenaBattleInProgress)
                return;

            if (ArenaRemainsBattle > 0)
            {
                Slider multiSlider = Widget.Find<ArenaBoard>().MultipleSlider;
                multiSlider.maxValue = ArenaMaxBattleCount;
                multiSlider.value = (ArenaMaxBattleCount - ArenaRemainsBattle) +1;
                multiSlider.transform.Find("TxtMax").GetComponent<TextMeshProUGUI>().text =
                    (ArenaMaxBattleCount - ArenaRemainsBattle + 1) + "/" + ArenaMaxBattleCount;

                var myLastBattle = Widget.Find<ArenaBoard>().myLastBattle;

                while (Game.Game.instance.Agent.BlockIndex < myLastBattle + 4
                    && Game.Game.instance.Agent.BlockIndex < Widget.Find<VersionSystem>().NodeBlockIndex + 4)
                {
                    await Task.Delay(System.TimeSpan.FromSeconds(0.5f));
                    if (ArenaRemainsBattle <= 0)
                        return;
                }
                    

                ActionRenderHandler.Instance.Pending = true;
                ArenaBattleInProgress = true;
                OneLineSystem.Push(MailType.System, $"<color=green>Pandora Box</color>: Fight " +
                    $"{(ArenaMaxBattleCount - ArenaRemainsBattle + 1)}/{ArenaMaxBattleCount} Sent!",
                NotificationCell.NotificationType.Information);
                //var costumes = States.Instance.ItemSlotStates[BattleType.Arena].Costumes;
                //var equipments = States.Instance.ItemSlotStates[BattleType.Arena].Equipments;
                //var runeInfos = States.Instance.RuneSlotStates[BattleType.Arena]
                //    .GetEquippedRuneSlotInfos();
                ActionRenderHandler.Instance.Pending = true;
                ActionManager.Instance.BattleArena(enemyAvatarAddress,costumes,equipments,runeInfos,championshipId,round,ticket).Subscribe();
            }
        }

        static int myLastBattle=0;
        public static async void PushArenaFight()
        {
            if (!CurrentPandoraPlayer.IsPremium())
                return;

            if (ArenaRemainsBattle > 0)
            {
                Slider multiSlider = Widget.Find<ArenaBoard>().MultipleSlider;
                multiSlider.maxValue = ArenaMaxBattleCount;
                multiSlider.value = (ArenaMaxBattleCount - ArenaRemainsBattle);
                multiSlider.transform.Find("TxtMax").GetComponent<TextMeshProUGUI>().text =
                    (ArenaMaxBattleCount - ArenaRemainsBattle) + "/" + ArenaMaxBattleCount;

                //var myLastBattle = myArenaAvatarState.LastBattleBlockIndex;

                while (Game.Game.instance.Agent.BlockIndex < myLastBattle + PandoraMaster.Instance.Settings.ArenaPushStep)
                {
                    if (ArenaRemainsBattle <= 0)
                        return;
                    await Task.Delay(System.TimeSpan.FromSeconds(0.5f));
                }

                await Task.Delay(System.TimeSpan.FromSeconds(2));
                myLastBattle = (int)Game.Game.instance.Agent.BlockIndex;
                OneLineSystem.Push(MailType.System, $"<color=green>Pandora Box</color>: Fight " +
                    $"{(ArenaMaxBattleCount - ArenaRemainsBattle + 1)}/{ArenaMaxBattleCount} Sent!",
                NotificationCell.NotificationType.Information);

                ActionManager.Instance.BattleArena(enemyAvatarAddress, costumes, equipments, runeInfos, championshipId, round, ticket).Subscribe();

                Game.Game.instance.Arena.IsAvatarStateUpdatedAfterBattle = true;
                ArenaBattleInProgress = true;
                ActionRenderHandler.Instance.Pending = false;
                ArenaRemainsBattle--;
                Widget.Find<UI.Module.EventBanner>().Close(true);
                Widget.Find<ArenaBoard>().ShowPandora();
            }
        }

        public static void OnArenaEnd(string enemyName,string result,string score,long blockIndex)
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

        public static void CancelMultiArena()
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


        public static (Address avatarAddr, int score, int rank)[] GetBoundsWithPlayerScore(
            (Address avatarAddr, int score, int rank)[] tuples,
            ArenaType arenaType,
            int playerScore, int currentPlayerRank)
        {
            int upper = 10 + (PandoraMaster.Instance.Settings.ArenaListUpper *
                        PandoraMaster.Instance.Settings.ArenaListStep);
            int lower = 10 + (PandoraMaster.Instance.Settings.ArenaListLower *
                        PandoraMaster.Instance.Settings.ArenaListStep);

                if (CurrentPandoraPlayer.IsPremium())
                    return tuples.Where(tuple => tuple.rank <= 300 || (tuple.rank >= currentPlayerRank - upper && tuple.rank <= currentPlayerRank + lower)).ToArray();
                else
                    return tuples.Where(tuple => tuple.rank <= 200 || (tuple.rank >= currentPlayerRank - upper && tuple.rank <= currentPlayerRank + lower)).ToArray();
        }

        public static void ExpectedTicketsToReach(TextMeshProUGUI tickets, TextMeshProUGUI ranks)
        {
            if (!CheckPremiumFeature() || Widget.Find<ArenaBoard>()._useGrandFinale)
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

        public static async void SoloSimulate(AvatarState myAvatarState, AvatarState enemyAvatarState)
        {
            if (!CheckPremiumFeature())
                return;

            PandoraMaster.CurrentArenaEnemyAddress = enemyAvatarState.address.ToString().ToLower();
            PandoraMaster.IsRankingSimulate = true;

            //my data
            var myItemSlotState = States.Instance.ItemSlotStates[BattleType.Arena];
            var myRuneStates = States.Instance.GetEquippedRuneStates(BattleType.Arena);
            var myDigest = new ArenaPlayerDigest(myAvatarState, myItemSlotState.Equipments, myItemSlotState.Costumes, myRuneStates);

            //enemy data
            var (enemyItemSlotStates, enemyRuneSlotStates) = await enemyAvatarState.GetSlotStatesAsync();
            var enemyItemSlotState = enemyItemSlotStates.FirstOrDefault(x => x.BattleType == BattleType.Arena);
            var enemyRuneSlotState = enemyRuneSlotStates.FirstOrDefault(x => x.BattleType == BattleType.Arena);

            if (enemyItemSlotState == null)
                enemyItemSlotState = new ItemSlotState(BattleType.Arena);

            if (enemyRuneSlotState == null)
                enemyRuneSlotState = new RuneSlotState(BattleType.Arena);

            var enemyRuneStates = await enemyAvatarState.GetRuneStatesAsync();
            var enemyDigest = new ArenaPlayerDigest(enemyAvatarState, enemyItemSlotState.Equipments, enemyItemSlotState.Costumes, enemyRuneStates);

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

        public static async Task<string> WinRatePVP(AvatarState myAvatarState, AvatarState enemyAvatarState,int iterations)
        {
            if (!CurrentPandoraPlayer.IsPremium())
                return "";

            //my data
            var myItemSlotState = States.Instance.ItemSlotStates[BattleType.Arena];
            var myRuneStates = States.Instance.GetEquippedRuneStates(BattleType.Arena);
            var myDigest = new ArenaPlayerDigest(myAvatarState,myItemSlotState.Equipments,myItemSlotState.Costumes,myRuneStates);

            //enemy data
            var (enemyItemSlotStates, enemyRuneSlotStates) = await enemyAvatarState.GetSlotStatesAsync();
            var enemyItemSlotState = enemyItemSlotStates.FirstOrDefault(x => x.BattleType == BattleType.Arena);
            var enemyRuneSlotState = enemyRuneSlotStates.FirstOrDefault(x => x.BattleType == BattleType.Arena);

            if (enemyItemSlotState == null)
                enemyItemSlotState = new ItemSlotState(BattleType.Arena);

            if (enemyRuneSlotState == null)
                enemyRuneSlotState = new RuneSlotState(BattleType.Arena);

            var enemyRuneStates = await enemyAvatarState.GetRuneStatesAsync();
            var enemyDigest = new ArenaPlayerDigest(enemyAvatarState,enemyItemSlotState.Equipments, enemyItemSlotState.Costumes, enemyRuneStates);

            return  IEMultipleSimulate(myDigest, enemyDigest, iterations);
        }

        public static StageSimulator SoloPVESimulate(int _worldId, int _stageId, List<Guid> consumables, int skillId = -1)
        {
            return GetPVESimulator(_worldId, _stageId, consumables, skillId);
        }

        public static async void MultiPVESimulate(int _worldId,int _stageId,List<Guid> consumables, int skillId = -1)
        {
            if (!CheckPremiumFeature())
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
                var simulator = GetPVESimulator(_worldId, _stageId, consumables, skillId);
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

        static string IEMultipleSimulate(ArenaPlayerDigest mD, ArenaPlayerDigest eD, int iterations)
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
            float FinalValue = (int)(finalRatio * 100f);

            if (finalRatio <= 0.5f)
                result = $"<color=#59514B>{FinalValue}</color>%";
            else if (finalRatio > 0.5f && finalRatio <= 0.75f)
                result = $"<color=#CD8756>{FinalValue}</color>%";
            else
                result = $"<color=#50A931>{FinalValue}</color>%";

            return result;
        }

        public static StageSimulator GetPVESimulator(int _worldId, int _stageId, List<Guid> consumables, int skillId = -1)
        {
            var itemSlotState = States.Instance.ItemSlotStates[BattleType.Adventure];
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
            if (skillId != -1 && CurrentPandoraPlayer.IsPremium())
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
                    StageSimulator.GetWaveRewards(new Cheat.DebugRandom(), tableSheets.StageSheet[_stageId], tableSheets.MaterialItemSheet),
                    PandoraMaster.IsHackAndSlashSimulate
                );
        }

        public static async void RepeatMultiple(Action<StageType, int, bool> _repeatBattleAction
            , ReactiveProperty<int> _ap, StageSheet.Row _stageRow, int _costAp, int iteration,
            Model.Item.Material apStone)
        {
            if (!CheckPremiumFeature())
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
                false);

                if (iteration >0)
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
                false);
                await Task.Delay(System.TimeSpan.FromSeconds(2));
            }

            Widget.Find<SweepPopup>().Close();
        }

        public static bool SweepMoreStone(int apStoneCount, List<Guid> _costumes, List<Guid> _equipments, List<RuneSlotInfo> _runes
            ,int worldId, StageSheet.Row stageRow)
        {
            if (!CheckPremiumFeature())
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

        public static void RelistAllShopItems(SellView view)
        {
            if (!CheckPremiumFeature())
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

            var updateSellInfos = new List<UpdateSellInfo>();
            var oneLineSystemInfos = new List<(string name, int count)>();
            foreach (var orderDigest in orderDigests)
            {
                if (!ReactiveShopState.TryGetShopItem(orderDigest, out var itemBase))
                {
                    return;
                }
                var currentprice = orderDigest.Price;
                //if (itemBase.Id == 201020)
                //{
                //    var currency = new Libplanet.Assets.Currency("NCG", 2, new Libplanet.Address("0x47d082a115c63e7b58b1532d20e631538eafadde"));
                //    currentprice = new Libplanet.Assets.FungibleAssetValue(currency, 1, 79);
                //}

                var updateSellInfo = new UpdateSellInfo(
                  orderDigest.OrderId,
                  Guid.NewGuid(),
                  orderDigest.TradableId,
                  itemBase.ItemSubType,
                  //orderDigest.Price,
                  currentprice,
                  orderDigest.ItemCount
                );
                updateSellInfos.Add(updateSellInfo);
                oneLineSystemInfos.Add((itemBase.GetLocalizedName(), orderDigest.ItemCount));
            }

            Game.Game.instance.ActionManager.UpdateSell(updateSellInfos).Subscribe();
        }

        public static void CancellLastShopItem()
        {
            if (!CheckPremiumFeature())
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
                OneLineSystem.Push(MailType.System, $"<color=green>Pandora Box</color>: {item.GetLocalizedName()} Order Canceled!",
                NotificationCell.NotificationType.Alert);

                Widget.Find<ShopSell>().LastItemSold = null;
                Widget.Find<ShopSell>().LastSoldTxt.text = "";
            }
            catch
            { Debug.LogError("Cancel Failed!"); }
        }

        public static void CancellAllShopItems(SellView view)
        {
            if (!CheckPremiumFeature())
                return;

            OneLineSystem.Push(MailType.System, $"<color=green>Pandora Box</color>: Cancel All items Process Started...",
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

        public static void FirstSortShop(ReactiveProperty<Nekoyume.EnumType.ShopSortFilter> _selectedSortFilter)
        {
            if (CurrentPandoraPlayer.IsPremium())
                _selectedSortFilter.SetValueAndForceNotify(Nekoyume.EnumType.ShopSortFilter.Time);
            else
                _selectedSortFilter.SetValueAndForceNotify(Nekoyume.EnumType.ShopSortFilter.Class);
        }

        public static IEnumerable<ShopItem> SortShopbyTime(
            ReactiveProperty<bool> _isAscending, List<ShopItem> models)
        {
            if (!CheckPremiumFeature())
                return new List<ShopItem>();

            return _isAscending.Value
                    ? models.OrderBy(x => x.OrderDigest.StartedBlockIndex).ToList()
                    : models.OrderByDescending(x => x.OrderDigest.StartedBlockIndex).ToList();
        }

        public static async Task<string> GetItemOwnerName(Guid guid)
        {
            if (!CurrentPandoraPlayer.IsPremium())
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
                PandoraPlayer currentSeller = PandoraMaster.GetPandoraPlayer(avatarState.agentAddress.ToString());

                if (CurrentPandoraPlayer.IsPremium())
                    if (!currentSeller.IsPremium())
                        return "<size=120%>" + avatarState.NameWithHash;
                    else
                        return "<size=120%><color=green>[P]</color>" + avatarState.NameWithHash;
                else
                    return "<size=120%><color=green>PREMIUM FEATURE!</color>";
            }
        }

        public static async void ShopRefresh(BuyView view, Action<ShopItem> clickItem, CancellationTokenSource _cancellationTokenSource)
        {
            int cooldown = 50;

            var initWeaponTask = Task.Run(async () =>
            {
                var list = new List<ItemSubType> { ItemSubType.Weapon, };
                await ReactiveShopState.SetBuyDigestsAsync(list);
                return true;
            });

            var initWeaponResult = await initWeaponTask;
            if (initWeaponResult)
            {
                //base.Show(ignoreShowAnimation);
                view.Show(ReactiveShopState.BuyDigest, clickItem);
            }

            _cancellationTokenSource = new CancellationTokenSource();
            var initOthersTask = Task.Run(async () =>
            {
                var list = new List<ItemSubType>
                {
                    ItemSubType.Armor,
                    ItemSubType.Belt,
                    ItemSubType.Necklace,
                    ItemSubType.Ring,
                    ItemSubType.Food,
                    ItemSubType.FullCostume,
                    ItemSubType.HairCostume,
                    ItemSubType.EarCostume,
                    ItemSubType.EyeCostume,
                    ItemSubType.TailCostume,
                    ItemSubType.Title,
                    ItemSubType.Hourglass,
                    ItemSubType.ApStone,
                };
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

            view.IsDoneLoadItem = true;

            cooldown = 5;
            if (CurrentPandoraPlayer.IsPremium())
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

        public static async UniTask SendWebhookT(string hook, string message)
        {
            WWWForm form = new WWWForm();
            form.AddField("content", message);
            using (UnityWebRequest www = UnityWebRequest.Post(hook, form))
            {
                await www.SendWebRequest();
            }
        }

        public static async UniTask UpdateDatabasePeriodly()
        {
            while (true)
            {
                await Task.Delay(System.TimeSpan.FromSeconds(1800)); //30m
                GetDatabase(States.Instance.CurrentAvatarState.agentAddress);
            }
        }

        public static async UniTask UpdateDatabase(float time)
        {
            await Task.Delay(System.TimeSpan.FromSeconds(time));
            GetDatabase(States.Instance.CurrentAvatarState.agentAddress);
        }

        public static string GetStatePercentage(int itemID, StatView statView,int starCount,int level, out int percent100,UI.Module.SkillView skillView = null)
        {
            percent100 = 0;
            var tableSheets = Game.TableSheets.Instance;
            var itemSheet = tableSheets.EquipmentItemRecipeSheet;
            var itemRow = itemSheet.First(x=> x.Value.ResultEquipmentId == itemID).Value;

            float levelStatsMultiply = 1;
            for (int i = 0; i <= level; i++)
            {
                if (i == 4 || i==7)
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


                foreach (var (ratio, option) in options)
                {
                    if (skillView is null)
                    {
                        stateValue = float.Parse(statView.valueText.text);
                        var xx = Enum.TryParse(statView.statTypeText.text, out StatType statType);
                        if (option.StatType == statType)
                        {
                            float newMin = statType == StatType.SPD || statType == StatType.DRR ? (option.StatMin / 100f) : option.StatMin;
                            if (newMin > statMin)
                                statMin = newMin;

                            float newMax = statType == StatType.SPD || statType == StatType.DRR ? (option.StatMax / 100f) : option.StatMax;
                            if (starCount == 1)
                            {
                                if (statMax == 0)
                                    statMax = newMax;
                            }
                            else
                            {
                                statMax += statType == StatType.SPD || statType == StatType.DRR ? (option.StatMax / 100f) : option.StatMax;
                            }
                        }
                    }
                    else
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
                        catch
                        {
                            return "";
                        }

                    }
                }

                //apply item level
                statMin *= levelStatsMultiply;
                statMax *= levelStatsMultiply;

                int percent = (int)((stateValue - statMin) / (statMax - statMin) * 100f);
                percent100 = percent;
                string precentStr = "";
                if (percent >= 75)
                    precentStr = $"<color=green>{percent}</color>";
                else if (percent > 50 && percent < 75)
                    precentStr = $"<color=#FF4900>{percent}</color>";
                else
                    precentStr = $"<color=red>{percent}</color>";
                return $" <size=50%><color=#A68F7E>[{(int)statMin}-{(int)statMax}]</color></size> {precentStr}%";
            }
            else
                return "";

        }



        // CUSTOME ACTIONS
        public static async UniTaskVoid AutoEventDungeon(AvatarState currentAvatarState, int eventDungeonId, int count)
        {
            if (!CurrentPandoraPlayer.IsPremium())
                return;

            if (RxProps.EventScheduleRowForDungeon.Value is null)
            {
                OneLineSystem.Push(MailType.System, $"<color=green>Pandora Box</color>: No Event Found!", NotificationCell.NotificationType.Alert);
                return;
            }

            try //in case actionManager is not ready yet
            {
                var (itemSlotStates, runeSlotStates) = await currentAvatarState.GetSlotStatesAsync();

                for (int i = 0; i < count; i++)
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
                    string analyzeText = $"**EventDungeonBattle** > {eventDungeonId} > {i+1}/{count}";
                    ActionManager.Instance.PreProcessAction(action, currentAvatarState, analyzeText).Forget();
                }
                OneLineSystem.Push(MailType.System, $"<color=green>Pandora Box</color>: <color=red>{currentAvatarState.name}</color> sent {count} Event tickets!",
                    NotificationCell.NotificationType.Information);
            }
            catch { }
        }

        public static async UniTask AutoCraft(AvatarState currentAvatarState, int slotIndex, int recipeId, int subRecipeId, bool payByCrystal, bool isBasic, int craftID)
        {
            if (!CurrentPandoraPlayer.IsPremium())
                return;

            //check if there is supercraft
            bool isHammerCraft = false;
            var hammerAddress = Addresses.GetHammerPointStateAddress(currentAvatarState.address, recipeId);
            var hammerState = await Game.Game.instance.Agent.GetStateAsync(hammerAddress);
            var hammerData = hammerState is List list? new HammerPointState(hammerAddress, list): new HammerPointState(hammerAddress, recipeId);;

            var max = Game.TableSheets.Instance.CrystalHammerPointSheet[recipeId].MaxPoint;
            isHammerCraft = hammerData.HammerPoint == max;
            //check if supercraft has crystal cost
            if (isHammerCraft && States.Instance.CrystalBalance.MajorUnit < Game.TableSheets.Instance.CrystalHammerPointSheet[recipeId].CRYSTAL)
            {
                OneLineSystem.Push(MailType.System, $"<color=green>Pandora Box</color>: <color=green>{currentAvatarState.name}</color> don't have enough Crystals for SuperCraft!"
                    ,NotificationCell.NotificationType.Alert);
                return;
            }


            //check if he has material
            if (!payByCrystal && !isHammerCraft)
            {

            }

            try //in case actionManager is not ready yet
            {
                var action = new CombinationEquipment
                {
                    avatarAddress = currentAvatarState.address,
                    slotIndex = slotIndex,
                    recipeId = recipeId,
                    subRecipeId = subRecipeId,
                    payByCrystal = payByCrystal,
                    useHammerPoint = isHammerCraft,
                };
                var itemName = L10nManager.Localize($"ITEM_NAME_{craftID}");
                string basicCraft = isBasic ? "Basic" : "Premium";
                string hammerCraft = isHammerCraft ? "Super Craft" : "";
                string crystal = payByCrystal ? ", With Crystal!" : "";
                string analyzeText = $"CombinationEquipment > **{hammerCraft}** **{basicCraft}** {itemName} > {slotIndex}{crystal} " +
                    $"> hammer:{hammerData.HammerPoint}/{max}";
                ActionManager.Instance.PreProcessAction(action, currentAvatarState,analyzeText).Forget();

                OneLineSystem.Push(MailType.System, $"<color=green>Pandora Box</color>: {currentAvatarState.NameWithHash} " +
                $"start Auto Craft <color=green>{itemName}</color> on Slot <color=green>{slotIndex + 1}</color> hammer points: <color=green>{hammerData.HammerPoint+1}</color>/{max}!",
                NotificationCell.NotificationType.Information);
            }
            catch { }

        }

        public static async UniTaskVoid AutoStageSweep(AvatarState currentAvatarState,int sweepStage)
        {
            if (!CurrentPandoraPlayer.IsPremium())
                return;

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
                var (itemSlotStates, runeSlotStates) = await currentAvatarState.GetSlotStatesAsync();

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
                string analyzeText = $"HackAndSlashSweep> {currentAvatarState.actionPoint}> **{sweepStage}**";
                ActionManager.Instance.PreProcessAction(action, currentAvatarState, analyzeText).Forget();

                OneLineSystem.Push(MailType.System, $"<color=green>Pandora Box</color>: {currentAvatarState.NameWithHash} Sweep <color=green>{currentAvatarState.actionPoint}</color> AP for stage {sweepStage}!", NotificationCell.NotificationType.Information);
            }
            catch { }
        }
    }
}
