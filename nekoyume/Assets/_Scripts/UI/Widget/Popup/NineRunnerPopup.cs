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

namespace Nekoyume.UI
{
    public class NineRunnerPopup : PopupWidget
    {
        [SerializeField] TextMeshProUGUI LeaderboardText;
        [SerializeField] GameObject LeaderboardLoading;

        protected override void Awake()
        {
            base.Awake();

        }

        public void Show()
        {
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

        public void StartRunner()
        {
            Find<Runner>().Show();
            Close();
        }

        void GetLeaderboard()
        {
            LeaderboardLoading.SetActive(true);
            var request = new GetLeaderboardRequest
            {
                StatisticName = "Runner",
                StartPosition = 0,
                MaxResultsCount = 10
            };
            PlayFabClientAPI.GetLeaderboard(request, OnLeaderboardSuccess, OnLeaderboardError);
        }

        void OnLeaderboardSuccess(GetLeaderboardResult result)
        {
            string playersList = $"\n<color=green>#</color>  Score            Name";
            foreach (var item in result.Leaderboard)
            {
                playersList += $"\n{(item.Position+1)}  <color=green>{item.StatValue}</color>        {item.Profile.DisplayName}\n";
            }
            LeaderboardText.text = playersList;
            LeaderboardLoading.SetActive(false);
        }

        void OnLeaderboardError(PlayFabError error)
        {
            Debug.LogError("Playfab Error: " + error.GenerateErrorReport());
        }

    }
}
