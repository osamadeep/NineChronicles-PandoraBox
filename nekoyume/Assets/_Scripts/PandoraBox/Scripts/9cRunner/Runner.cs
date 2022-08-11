using Nekoyume.Game.Controller;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Nekoyume.UI.Module;
using Nekoyume.Game;
using Spine.Unity;
using PlayFab;
using PlayFab.ClientModels;

namespace Nekoyume.UI
{
    public class Runner : Widget
    {
        public enum RunnerState { Start, Play, Die, Hit, Info }
        [SerializeField] Transform[] enemies;
        [SerializeField] TextMeshProUGUI centerText = null;
        [SerializeField] TextMeshProUGUI scoreText;
        [SerializeField] TextMeshProUGUI LevelText;
        [SerializeField] Transform healthHolder = null;
        [SerializeField] Transform coinSpawner;
        [SerializeField] Transform crystalSpawner;
        [SerializeField] RunnerController runcontrol;
        [SerializeField] GameObject warningObj;
        [SerializeField] GameObject FinalResult;


        int scoreDistance;
        int scoreCoin;
        public float LevelSpeed = 1f;
        int currentLife;


        RunnerState currentRunnerState = RunnerState.Start;


        protected override void Awake()
        {
            base.Awake();
            //ActionCamera.instance.Shake();
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            Find<HeaderMenuStatic>().Close();
            Find<Menu>().Close();
            Find<VersionSystem>().Close();
            base.Show(ignoreShowAnimation);

            InitilizeGame();
        }

        void InitilizeGame()
        {
            //clear all pooled objects
            foreach (GameObject item in GetComponent<CoinPooler>().pooledObjects)
                item.SetActive(false);
            foreach (GameObject item in GetComponent<PickupPooler>().pooledObjects)
                item.SetActive(false);
            foreach (GameObject item in GetComponent<CrystalPooler>().pooledObjects)
                item.SetActive(false);

            currentRunnerState = RunnerState.Start;
            runcontrol.runner = RunnerState.Start;
            scoreDistance = 0;
            scoreCoin = 0;
            UpdateScore(0, 0);

            AudioController.instance.PlayMusic(AudioController.MusicCode.Runner1, 0.5f);
            FinalResult.SetActive(false);

            StopAllCoroutines();

            StartCoroutine(PrepareGame());
            StartCoroutine(PrepareLife());

            StartCoroutine(CoinSpawner());
            StartCoroutine(ScorePerMinute());
            StartCoroutine(EnemySpawn());
            StartCoroutine(IncreaseSpeed());
        }

        IEnumerator PrepareGame()
        {
            centerText.gameObject.SetActive(true);
            centerText.text = "Prepare";
            for (int i = 3; i > 0; i--)
            {
                centerText.text = i.ToString();
                AudioController.instance.PlaySfx(AudioController.SfxCode.OptionNormal);
                yield return new WaitForSeconds(1);
            }
            centerText.text = "Start!";
            AudioController.instance.PlaySfx(AudioController.SfxCode.OptionSpecial);
            yield return new WaitForSeconds(1);

            centerText.gameObject.SetActive(false);
            currentRunnerState = RunnerState.Play;
            runcontrol.runner = RunnerState.Play;
        }

        public void CollectCoins(Transform currentCoin)
        {
            UpdateScore(5, 1);
            AudioController.instance.PlaySfx(AudioController.SfxCode.RewardItem);
            RectTransform playerRecttransform = currentCoin.GetComponent<RectTransform>();

            GameObject ob = PickupPooler.instance.GetpooledObject();
            ob.transform.SetParent(transform.Find("PooledObj"));
            ob.GetComponent<RectTransform>().anchoredPosition = playerRecttransform.anchoredPosition;
            ob.GetComponent<RectTransform>().anchorMax = playerRecttransform.anchorMax;
            ob.GetComponent<RectTransform>().anchorMin = playerRecttransform.anchorMin;
            ob.transform.localScale = new Vector3(1, 1, 1);
            ob.GetComponent<RunnerUnitMovements>().TimeScale = LevelSpeed;
            ob.SetActive(true);
            currentCoin.gameObject.SetActive(false);
        }
        IEnumerator IncreaseSpeed()
        {
            LevelSpeed = 1;
            LevelText.text = "Level: 1";
            while (true)
            {
                yield return new WaitForSeconds(15);
                if (currentRunnerState == RunnerState.Play)
                {
                    LevelSpeed += 0.1f;
                    LevelText.text = "Level: " + (int)(((LevelSpeed - 1) / 0.1f) + 1);
                    //Debug.LogError("Level: " + (((LevelSpeed - 1) / 0.1f) + 1));
                    //speed current pooled objects
                    foreach (GameObject item in GetComponent<PickupPooler>().pooledObjects)
                        item.GetComponent<RunnerUnitMovements>().TimeScale = LevelSpeed;
                    foreach (GameObject item in GetComponent<CoinPooler>().pooledObjects)
                        item.GetComponent<RunnerUnitMovements>().TimeScale = LevelSpeed;
                    foreach (GameObject item in GetComponent<CrystalPooler>().pooledObjects)
                        item.GetComponent<RunnerUnitMovements>().TimeScale = LevelSpeed;
                    foreach (Transform item in enemies)
                        item.GetComponent<RunnerUnitMovements>().TimeScale = LevelSpeed;

                    if ((int)(((LevelSpeed - 1) / 0.1f) + 1) == 10)
                    {
                        //speed music only once
                        currentRunnerState = RunnerState.Info;
                        AudioController.instance.StopMusicAll();
                        AudioController.instance.PlayMusic(AudioController.MusicCode.Runner2);
                        centerText.text = "ENRAGE!!!";
                        centerText.gameObject.SetActive(true);
                        yield return new WaitForSeconds(2);
                        centerText.gameObject.SetActive(false);
                        currentRunnerState = RunnerState.Play;
                        StartCoroutine(CrystalSpawner());
                    }
                }
            }
        }

        public void ExitToMenu()
        {
            Game.Event.OnRoomEnter.Invoke(false);
            Find<VersionSystem>().Show();
            Close();
        }

        IEnumerator EnemySpawn()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(3f, 6f) / LevelSpeed);
                if (currentRunnerState == RunnerState.Play)
                {
                    //show warning
                    bool isVisible = false;
                    for (int i = 0; i < 4; i++)
                    {
                        isVisible = !isVisible;
                        warningObj.SetActive(isVisible);
                        if (isVisible)
                            AudioController.instance.PlaySfx(AudioController.SfxCode.Alert);
                        yield return new WaitForSeconds(0.4f / LevelSpeed);
                    }
                    warningObj.SetActive(false);

                    if (currentRunnerState == RunnerState.Play)
                    {
                        //select enemy
                        int rndEnemy = Random.Range(0, 3);
                        if (LevelSpeed >= 2)
                            rndEnemy = Random.Range(0, enemies.Length);
                        Transform currentEnemy = enemies[rndEnemy];
                        currentEnemy.GetComponent<RectTransform>().anchoredPosition = new Vector2(850, currentEnemy.GetComponent<RectTransform>().anchoredPosition.y);
                        currentEnemy.GetComponent<RunnerUnitMovements>().TimeScale = LevelSpeed;
                        if (rndEnemy == 1 || rndEnemy == 2 || rndEnemy == 3)
                            AudioController.instance.PlaySfx(AudioController.SfxCode.DamageFire);
                        currentEnemy.gameObject.SetActive(true);
                    }
                }
            }
        }

        IEnumerator CoinSpawner()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(1, 4) / LevelSpeed);

                int count = Random.Range(4, 8);
                if (currentRunnerState == RunnerState.Play)
                    for (int i = 0; i < count; i++)
                    {
                        yield return new WaitForSeconds(0.4f / LevelSpeed);
                        GameObject ob = CoinPooler.instance.GetpooledObject();
                        ob.transform.SetParent(transform);
                        ob.GetComponent<RectTransform>().anchoredPosition = coinSpawner.GetComponent<RectTransform>().anchoredPosition;
                        ob.GetComponent<RectTransform>().anchorMax = coinSpawner.GetComponent<RectTransform>().anchorMax;
                        ob.GetComponent<RectTransform>().anchorMin = coinSpawner.GetComponent<RectTransform>().anchorMin;
                        ob.transform.localScale = new Vector3(1, 1, 1);
                        ob.GetComponent<RunnerUnitMovements>().TimeScale = LevelSpeed;
                        ob.SetActive(true);
                    }
            }
        }

        public void PlayerGotHit()
        {
            currentRunnerState = RunnerState.Hit;
            runcontrol.runner = RunnerState.Hit;
            StartCoroutine(GotRecover());
        }

        IEnumerator GotRecover()
        {
            currentLife--;
            SetCurrentLife(currentLife);
            if (currentLife <= 0)
            {
                currentRunnerState = RunnerState.Die;
                centerText.gameObject.SetActive(true);
                centerText.text = "Game Over!";
                yield return new WaitForSeconds(3f);
                centerText.gameObject.SetActive(false);
                FinalResult.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = (scoreDistance + scoreCoin).ToString();
                FinalResult.SetActive(true);
                //Send score to PlayFab
                var request = new UpdatePlayerStatisticsRequest
                {
                    Statistics = new List<StatisticUpdate> { new StatisticUpdate { StatisticName = "Runner", Value= (scoreDistance + scoreCoin) } }
                };
                PlayFabClientAPI.UpdatePlayerStatistics(request, OnLeaderboardSuccess, OnLeaderboardError);
            }
            else
            {
                SkeletonGraphic playerGFX = runcontrol.transform.GetChild(0).GetComponent<SkeletonGraphic>();
                bool isVisible = true;
                for (int i = 0; i < 12; i++)
                {
                    isVisible = !isVisible;
                    if (isVisible)
                        playerGFX.color = new Color(1,1,1,1);
                    else
                        playerGFX.color = new Color(1, 1, 1, 0.1f);
                    yield return new WaitForSeconds(0.1f);
                }
                playerGFX.color = new Color(1, 1, 1, 1);
                currentRunnerState = RunnerState.Play;
                runcontrol.runner = RunnerState.Play;
            }
        }

        void OnLeaderboardSuccess(UpdatePlayerStatisticsResult result)
        {
            //Debug.LogError("Score Sent!");
        }

        void OnLeaderboardError(PlayFabError result)
        {
            Debug.LogError("Score Not Sent!, " + result.GenerateErrorReport());
        }

        IEnumerator CrystalSpawner()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(10, 20));
                if (currentRunnerState == RunnerState.Play)
                {
                    GameObject ob = CrystalPooler.instance.GetpooledObject();
                    ob.transform.SetParent(transform);
                    ob.GetComponent<RectTransform>().anchoredPosition = crystalSpawner.GetComponent<RectTransform>().anchoredPosition;
                    ob.GetComponent<RectTransform>().anchorMax = crystalSpawner.GetComponent<RectTransform>().anchorMax;
                    ob.GetComponent<RectTransform>().anchorMin = crystalSpawner.GetComponent<RectTransform>().anchorMin;
                    ob.transform.localScale = new Vector3(1, 1, 1);
                    ob.GetComponent<RunnerUnitMovements>().TimeScale = LevelSpeed;
                    ob.SetActive(true);
                }
            }
        }

        public void UpdateScore(int value, int typeScore) //0 = msafa , 1 = coin
        {
            runcontrol.TimeScale = LevelSpeed; // MOVE IT!!
            if (typeScore == 0)
                scoreDistance += value;
            else if (typeScore == 1)
                scoreCoin += value;

            scoreText.text = "SCORE: " + (scoreDistance + scoreCoin).ToString();
        }

        IEnumerator ScorePerMinute()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f/LevelSpeed);
                if (currentRunnerState == RunnerState.Play)
                    UpdateScore(1, 0);
            }
        }


        IEnumerator PrepareLife()
        {
            yield return new WaitForSeconds(1.5f);
            currentLife = 0;
            for (int i = 0; i < 3; i++)
            {
                currentLife++;
                SetCurrentLife(currentLife);
                yield return new WaitForSeconds(0.5f);
            }
        }

        protected override void OnCompleteOfShowAnimationInternal()
        {
            base.OnCompleteOfShowAnimationInternal();
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);
        }

        public void SetCurrentLife(int lives)
        {
            lives = Mathf.Clamp(lives, 0, 3);
            foreach (Transform item in healthHolder)
            {
                item.gameObject.SetActive(false);
            }

            for (int i = 0; i < lives; i++)
            {
                healthHolder.GetChild(i).gameObject.SetActive(true);
            }
        }
    }
}
