using System;
using System.Globalization;
using Nekoyume.Helper;
using Nekoyume.PandoraBox;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace Nekoyume.UI
{
    public class RunnerCellContent
    {
        public string PlayFabID { set; get; }
        public int Position { set; get; }
        public string PlayerName { set; get; }
        public int Score { set; get; }
    }

    public class RunnerCell : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI _rankText;
        [SerializeField]
        private TextMeshProUGUI _nameText;
        [SerializeField]
        private TextMeshProUGUI _scoreText;
        [SerializeField]
        private TextMeshProUGUI _ncgText;
        [SerializeField]
        private TextMeshProUGUI _gemText;
        [SerializeField]
        private TextMeshProUGUI _coinText;

        public RunnerCellContent CurrentCellContent;

        private void Awake()
        {
            //_choiceButton.OnClickSubject
            //    .Subscribe(_ => Context.onClickChoice?.Invoke(Index))
            //    .AddTo(gameObject);
        }

        public void UpdateContent(RunnerCellContent playerData)
        {
            CurrentCellContent = playerData;
            _rankText.text = (playerData.Position + 1).ToString();
            _nameText.text = playerData.PlayerName;
            _scoreText.text = playerData.Score.ToString();

            _gemText.gameObject.SetActive(true);
            _coinText.gameObject.SetActive(true);

            
            if (playerData.Position == 0)
            {
                SetRewards(0);
            }
            else if (playerData.Position > 0 && playerData.Position < 5)
            {
                SetRewards(1);
            }
            else if (playerData.Position >= 5 && playerData.Position < 10)
            {
                SetRewards(2);
            }
            else if (playerData.Position >= 10 && playerData.Position < 15)
            {
                SetRewards(3);
            }
            else if (playerData.Position >= 15 && playerData.Position < 20)
            {
                SetRewards(4);
            }
            else if (playerData.Position >= 20 && playerData.Position < 50)
            {
                SetRewards(5);
            }
            else
            {
                _ncgText.gameObject.SetActive(false);
                _gemText.gameObject.SetActive(false);
                _coinText.gameObject.SetActive(false);
            }
        }

        void SetRewards(int index)
        {
            var settings = PandoraMaster.PanDatabase.RunnerSettings;
            int ncg = settings.RewardsNCG[index];
            int pg = settings.RewardsPG[index];
            int pc = settings.RewardsPC[index];

            _ncgText.text = PandoraUtil.ToLongNumberNotation(ncg);
            _gemText.text = PandoraUtil.ToLongNumberNotation(pg);
            _coinText.text = PandoraUtil.ToLongNumberNotation(pc);

            _ncgText.gameObject.SetActive(ncg > 0);
            _gemText.gameObject.SetActive(pg > 0);
            _coinText.gameObject.SetActive(pc > 0);
        }
    }
}
