using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Nekoyume.PandoraBox
{
    public class RunnerLevelManager : MonoBehaviour
    {
        public static RunnerLevelManager instance;

        [SerializeField] TextMeshProUGUI scoreText;
        [SerializeField] Transform coinSpawner;
        [SerializeField] Transform crystalSpawner;

        int scoreDistance;
        int scoreCoin;


        private void Awake()
        {
            instance = this;
        }

        // Start is called before the first frame update
        void Start()
        {
            StartCoroutine(CoinSpawner());
            StartCoroutine(CrystalSpawner());
            StartCoroutine(ScorePerMinute());
        }

        // Update is called once per frame
        void Update()
        {
        
        }


        IEnumerator CoinSpawner()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(1, 4));

                int count = Random.Range(4, 8);
                for (int i = 0; i < count; i++)
                {
                    yield return new WaitForSeconds(0.4f);
                    GameObject ob = CoinPooler.instance.GetpooledObject();
                    ob.transform.SetParent(transform);
                    ob.GetComponent<RectTransform>().anchoredPosition = coinSpawner.GetComponent<RectTransform>().anchoredPosition;
                    ob.GetComponent<RectTransform>().anchorMax = coinSpawner.GetComponent<RectTransform>().anchorMax;
                    ob.GetComponent<RectTransform>().anchorMin = coinSpawner.GetComponent<RectTransform>().anchorMin;
                    ob.transform.localScale = new Vector3(1, 1,1);
                    ob.SetActive(true);
                }
            }
        }

        IEnumerator CrystalSpawner()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(5, 15));
                    GameObject ob = CrystalPooler.instance.GetpooledObject();
                    ob.transform.SetParent(transform);
                    ob.GetComponent<RectTransform>().anchoredPosition = crystalSpawner.GetComponent<RectTransform>().anchoredPosition;
                    ob.GetComponent<RectTransform>().anchorMax = crystalSpawner.GetComponent<RectTransform>().anchorMax;
                    ob.GetComponent<RectTransform>().anchorMin = crystalSpawner.GetComponent<RectTransform>().anchorMin;
                    ob.transform.localScale = new Vector3(1, 1, 1);
                    ob.SetActive(true);
            }
        }

        public void UpdateScore(int value, int typeScore) //0 = msafa , 1 = coin
        {
            if (typeScore == 0)
                scoreDistance += value;
            else if (typeScore == 1)
                scoreCoin += value;

            scoreText.text = "SCORE: " + (scoreDistance + scoreCoin ).ToString();
        }

        IEnumerator ScorePerMinute()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.1f);
                UpdateScore(1, 0);
            }
        }
    }
}
