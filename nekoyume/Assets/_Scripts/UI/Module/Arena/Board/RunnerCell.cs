using System;
using System.Globalization;
using Nekoyume.Helper;
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
                _gemText.text = "x 20";
                _coinText.text = "x 12K";
            }
            else if (playerData.Position > 0 && playerData.Position < 5)
            {
                _gemText.text = "x 12";
                _coinText.text = "x 7K";
            }
            else if (playerData.Position >= 5 && playerData.Position < 10)
            {
                _gemText.text = "x 6";
                _coinText.text = "x 5K";
            }
            else if (playerData.Position >= 10 && playerData.Position < 15)
            {
                _gemText.text = "x 2";
                _coinText.text = "x 3k";
            }
            else if (playerData.Position >= 15 && playerData.Position < 20)
            {
                _gemText.text = "x 0";
                _gemText.gameObject.SetActive(false);
                _coinText.text = "x 1k";
            }
            else if (playerData.Position >= 20 && playerData.Position < 50)
            {
                _gemText.text = "x 0";
                _gemText.gameObject.SetActive(false);
                _coinText.text = "x 500";
            }
            else
            {
                _gemText.text = "x 0";
                _gemText.gameObject.SetActive(false);
                _coinText.text = "x 0";
                _coinText.gameObject.SetActive(false);
            }
        }
    }
}
