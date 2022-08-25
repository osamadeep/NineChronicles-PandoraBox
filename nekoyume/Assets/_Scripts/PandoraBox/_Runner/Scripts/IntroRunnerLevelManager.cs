using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Nekoyume.PandoraBox
{
    public class IntroRunnerLevelManager : MonoBehaviour
    {
        public static IntroRunnerLevelManager instance;

        [SerializeField] TextMeshProUGUI scoreText;
        [SerializeField] Transform coinSpawner;

        int scoreDistance;
        int scoreCoin;


        private void Awake()
        {
            instance = this;
        }

        // Start is called before the first frame update
        void Start()
        {
            //StartCoroutine(CoinSpawner());
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
