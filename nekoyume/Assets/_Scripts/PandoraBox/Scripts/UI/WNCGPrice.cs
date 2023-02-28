using Cysharp.Threading.Tasks;
using Nekoyume.PandoraBox;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

namespace Nekoyume
{
    public class WNCGPrice : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI price;
        [SerializeField] TextMeshProUGUI percentage;

        float percent;

        public void UpdateWncgPrice()
        {
            GrabPrice().Forget();
        }

        async UniTask GrabPrice()
        {
            price.text = "...";
            percentage.text = "...";
            string url = "https://api.9cscan.com/price/";
            UnityWebRequest www = UnityWebRequest.Get(url);
            await www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
            }
            else
            {
                CoinData data = JsonUtility.FromJson<CoinData>(www.downloadHandler.text);

                // Access the price and percent_change_24h values
                float priceF = data.quote.USD.price;
                float percent_change_24h = data.quote.USD.percent_change_24h;
                PandoraMaster.WncgPrice = priceF;

                bool isPercentagePositive = percent_change_24h > 0;
                price.text = "$" + PandoraMaster.WncgPrice.ToString("F3");
                percentage.text = isPercentagePositive
                    ? "+" + percent_change_24h.ToString("F2") + "%"
                    : percent_change_24h.ToString("F2") + "%";
                percentage.color = isPercentagePositive ? Color.green : Color.red;
            }
        }
    }

    [System.Serializable]
    public class CoinData
    {
        public Quote quote;
    }

    [System.Serializable]
    public class Quote
    {
        public USD USD;
    }

    [System.Serializable]
    public class USD
    {
        public float price;
        public float percent_change_24h;
    }
}