using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Game.Character;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.UI.Scroller;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using Nekoyume.PandoraBox;

namespace Nekoyume.UI
{
    public class NineRunnerPopup : PopupWidget
    {
        [SerializeField] TextMeshProUGUI GemsText;
        [SerializeField] TextMeshProUGUI CoinsText;
        [SerializeField] TextMeshProUGUI ResetDateText;
        [SerializeField] TextMeshProUGUI BestScoreText;
        [SerializeField] GameObject LeaderboardLoading;
        public RectTransform ScrollContent;
        [SerializeField] RunnerCell CurrentPlayerCell;

        RunnerCellContent currentPlayer;

        int TotalLeaderboarCount = 20;

        protected override void Awake()
        {
            base.Awake();
            for (int i = 1; i < TotalLeaderboarCount; i++)
            {
                Transform ob =  Instantiate(ScrollContent.GetChild(0), ScrollContent);
                ob.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, (-i * 68) - 34);
            }
            //ScrollContent.anchoredPosition = new Vector2(0, 68 * TotalLeaderboarCount);
        }

        public void Show()
        {
            //show player inventory
            UpdateCurrency();
            GetLeaderboard();
            base.Show();
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);
        }

        public void ComingSoon()
        {
            OneLineSystem.Push(MailType.System, "<color=green>Pandora Box</color>: coming soon!",
                NotificationCell.NotificationType.Information);
            return;
        }

        public void UpdateCurrency()
        {
            GemsText.text = PandoraMaster.PlayFabInventory.VirtualCurrency["PG"].ToString();
            CoinsText.text = PandoraMaster.PlayFabInventory.VirtualCurrency["PC"].ToString();
        }

        public void StartRunner()
        {
            Game.Game.instance.Runner.OnRunnerStart();
            Close();
        }

        public void ShowPandoraShop()
        {
            Widget.Find<PandoraShopPopup>().Show();
        }

        void GetLeaderboard()
        {
            LeaderboardLoading.SetActive(true);
            var request = new GetLeaderboardRequest
            {
                StatisticName = PandoraMaster.PlayFabRunnerLeaderboard,
                StartPosition = 0,
                MaxResultsCount = TotalLeaderboarCount,
            };
            PlayFabClientAPI.GetLeaderboard(request, OnLeaderboardSuccess, OnLeaderboardError);
        }

        void OnLeaderboardSuccess(GetLeaderboardResult result)
        {
            foreach (Transform item in ScrollContent)
            {
                item.gameObject.SetActive(false);
            }

            for (int i = 0; i < result.Leaderboard.Count; i++)
            {
                RunnerCellContent pContent = new RunnerCellContent()
                {
                    PlayFabID = result.Leaderboard[i].PlayFabId,
                    Position = result.Leaderboard[i].Position ,
                    PlayerName = result.Leaderboard[i].DisplayName,
                    Score = result.Leaderboard[i].StatValue
                };
                ScrollContent.GetChild(i).GetComponent<RunnerCell>().UpdateContent(pContent);
                ScrollContent.GetChild(i).gameObject.SetActive(true);
            }

            System.TimeSpan ts = System.DateTime.Parse(result.NextReset.Value.ToString()) - System.DateTime.Now;
            string remains = $"{ts.Days}d {ts.Hours}h {ts.Minutes}m {ts.Seconds}s";
            ResetDateText.text = "Reset Date: <color=red>" + remains;
            LeaderboardLoading.SetActive(false);

            var request = new GetLeaderboardAroundPlayerRequest { StatisticName = PandoraMaster.PlayFabRunnerLeaderboard, MaxResultsCount = 1 };
            PlayFabClientAPI.GetLeaderboardAroundPlayer(request, OnLeaderboardAroundPlayerSuccess, OnLeaderboardError);
        }

        void OnLeaderboardAroundPlayerSuccess(GetLeaderboardAroundPlayerResult result)
        {
            foreach (var item in result.Leaderboard)
            {
                currentPlayer = new RunnerCellContent()
                {
                    PlayFabID = item.PlayFabId,
                    Position = item.Position,
                    PlayerName = item.DisplayName,
                    Score = item.StatValue
                };
                BestScoreText.text = $"Best Score\n<size=200%><color=green>{item.StatValue} m</color></size>";
            }
            if (result.Leaderboard.Count != 0)
                CurrentPlayerCell.UpdateContent(currentPlayer);
            //else
            //    CurrentPlayerCell.UpdateContent
        }

        void OnLeaderboardError(PlayFabError error)
        {
            Debug.LogError("Playfab Error: " + error.GenerateErrorReport());
        }
    }
}
