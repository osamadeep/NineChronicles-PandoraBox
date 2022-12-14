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
using Nekoyume.State;
using Nekoyume.PandoraBox;
using Nekoyume.UI.Scroller;
using PlayFab.Json;
using Cysharp.Threading.Tasks;
using Nekoyume.UI;

namespace Nekoyume.PandoraBox
{
    public class PandoraRunner : MonoBehaviour
    {
        public enum RunnerState { NewRound, Playing, Dead, GotHit, Pause,Town }
        public RunnerController player;
        public Transform runnerBoss;
        public Transform runnerLevel;

        [SerializeField] Transform[] BgArray;
        [SerializeField] Transform[] enemiesArray;
        [SerializeField] RunnerMissile missileEnemy;
        [SerializeField] Transform[] CoinsArray;

        //core variables
        public float LevelSpeed = 1f;
        int scoreDistance;
        int scoreCoins;
        int life;


        //Utilities
        public UtilitieSlot SelectedUtilitie = null;


        //local settings
        float dieSpeed = 1;

        //for statistics
        float playTimer=0;
        float idleTimer=0;
        int sectionsPassed;
        int gameSeed = 0;
        int avoidMissiles = 0;
        int livesBought = 0;
        int speedBooster = 0;

        public RunnerState currentRunnerState = RunnerState.Town;
        public Runner RunnerUI; //DELETE

        public void Initialize()
        {
            //something to Initialize
            currentRunnerState = RunnerState.Town;
            player.runner = currentRunnerState;
        }

        private void Update()
        {
            if (currentRunnerState == RunnerState.Playing)
                playTimer += Time.deltaTime;
            else
                idleTimer += Time.unscaledDeltaTime;
        }

        public void OnRunnerStart()
        {
            runnerLevel.gameObject.SetActive(true);
            player.gameObject.SetActive(true);
            RunnerUI = Widget.Find<Runner>();
            Time.timeScale = 1;

            LevelSpeed = 1;
            SpeedChange();
            StopAllCoroutines();
            InitilizeGame();
        }


        void InitilizeGame()
        {
            //clear all pooled objects
            foreach (Transform item in enemiesArray)
                item.gameObject.SetActive(false);
            foreach (Transform item in CoinsArray)
                item.gameObject.SetActive(false);
            missileEnemy.EliminateMissile();

            currentRunnerState = RunnerState.NewRound;
            player.runner = currentRunnerState;

            AudioController.instance.PlayMusic(AudioController.MusicCode.Runner1, 0.5f);
            missileEnemy.WarningSprite.gameObject.SetActive(false);
            RunnerUI.FinalResult.SetActive(false);
            RunnerUI.UIElement.SetActive(false);

            //reset boss position
            player.transform.position = new Vector2(-4.185f, -1.11f);
            runnerBoss.position = new Vector2(-6.463f, -1.11f);
            runnerBoss.gameObject.SetActive(true);

            SelectedUtilitie = null;

            StartCoroutine(PrepareGame());
        }

        IEnumerator PrepareGame()
        {
            //intro animation
            player.ChangeState("Run");

            while (player.transform.position.x < 1)
            {
                //learp += Time.deltaTime / 2;
                yield return new WaitForSeconds(0.02f);
                player.transform.position += new Vector3(0.04f, 0);
                runnerBoss.position += new Vector3(0.04f, 0);
            }
            AudioController.instance.PlaySfx(AudioController.SfxCode.RunnerBoss);

            RunnerUI.Show();
            yield return StartCoroutine(RunnerUI.ShowStartBooster()); 

            if(SelectedUtilitie != null)
                AudioController.instance.PlaySfx(AudioController.SfxCode.BgmGreatSuccess);
            //prepare StartFeatures Function
            object FuncParam = new {
                premium = Premium.CurrentPandoraPlayer.IsPremium(),
                address = States.Instance.CurrentAvatarState.agentAddress.ToString(),
                block = Game.Game.instance.Agent.BlockIndex,
                utilitie = SelectedUtilitie != null? SelectedUtilitie.ItemID: "None",
                currency = SelectedUtilitie != null? SelectedUtilitie.CurrencySTR:"XX",
            };

            //Get Greenlight from Server
            PlayFabClientAPI.ExecuteCloudScript(
                new ExecuteCloudScriptRequest {FunctionName = "StartFeatures",FunctionParameter = FuncParam},
                success =>
                {
                    JsonObject jsonResult = (JsonObject)success.FunctionResult;
                    object isUtilitie;
                    object seedF;
                    jsonResult.TryGetValue("feature", out isUtilitie);
                    jsonResult.TryGetValue("seed", out seedF);
                    gameSeed = int.Parse(seedF.ToString());

                    //core variables
                    scoreDistance = 0 + gameSeed;
                    scoreCoins = 0 + gameSeed;
                    sectionsPassed = 1 + gameSeed;
                    life = 1 + gameSeed;
                    UpdateScore(0, 0);
                    UpdateScore(0, 1);

                    //for statistics
                    avoidMissiles = 0 + gameSeed;
                    livesBought = 0 + gameSeed;
                    speedBooster = 0 + gameSeed;

                    RunnerUI.UIElement.SetActive(true);
                    RunnerUI.centerText.gameObject.SetActive(false);
                    currentRunnerState = RunnerState.Playing;
                    player.runner = currentRunnerState;
                    //


                    if (!(bool)isUtilitie)
                    {
                        //NotificationSystem.Push(Nekoyume.Model.Mail.MailType.System, "PandoraBox: No Booster Failed!", NotificationCell.NotificationType.Alert);
                        StartCoroutine(FinishStartAnimation(false));
                    }
                    else
                    {
                        PandoraMaster.PlayFabInventory.VirtualCurrency["PC"] -= SelectedUtilitie.ItemPrice; //just UI update instead of request new call
                        StartCoroutine(FinishStartAnimation(true));
                    }
                },
                failed =>
                {
                    Debug.LogError("Failed to StartFeatures!, " + failed.GenerateErrorReport());
                });
        }


        IEnumerator FinishStartAnimation(bool isUtilitie)
        {
            AudioController.instance.PlaySfx(AudioController.SfxCode.RunnerBoss);
            while (player.transform.position.x > -2)
            {
                yield return new WaitForSeconds(0.02f);
                player.transform.position -= new Vector3(0.025f, 0);
                runnerBoss.position -= new Vector3(0.04f, 0);
            }

            if (isUtilitie)
            {
                switch (SelectedUtilitie.ItemID)
                {
                    case "HeadStart750":
                        StartCoroutine(HeadStart(750));
                        break;
                    case "HeadStart1500":
                        StartCoroutine(HeadStart(1500));
                        break;
                }
            }

            //start run
            playTimer = 0;
            idleTimer = 0;

            //StartCoroutine(PrepareLife());
            StartCoroutine(ScorePerMinute());
            StartCoroutine(EnemySpawn());
            StartCoroutine(IncreaseSpeed());
            StartCoroutine(MissileSpawn());
        }

        IEnumerator HeadStart(int targetDistance)
        {
            player.EnableSpeed(true);
            yield return new WaitForSeconds(0.5f);
            speedBooster += targetDistance;

            float learp = 0;
            int boostSpeed = 5;
            while (learp < 1)
            {
                learp += Time.deltaTime / 1;
                yield return new WaitForSeconds(0);
                LevelSpeed = Mathf.Lerp(1, boostSpeed, learp);// Mathf.Clamp(LevelSpeed + boostSpeed, 1, boostSpeed);
                SpeedChange();
            }


            while (scoreDistance - gameSeed < targetDistance )
            {
                yield return new WaitForSeconds(0.05f);
                scoreDistance += 9;
            }

            learp = 0;
            while (learp < 1)
            {
                learp += Time.deltaTime / 1;
                yield return new WaitForSeconds(0);
                LevelSpeed = Mathf.Lerp(boostSpeed,1 , learp);// Mathf.Clamp(LevelSpeed + boostSpeed, 1, boostSpeed);
                SpeedChange();
            }

            //kill all enemies
            foreach (Transform item in enemiesArray)
                item.gameObject.SetActive(false);
            //foreach (Transform item in CoinsArray)
            //    item.gameObject.SetActive(false);
            missileEnemy.EliminateMissile();

            player.EnableSpeed(false);
        }

        public void CollectCoins(Transform currentCoin)
        {
            UpdateScore(1, 1);
            AudioController.instance.PlaySfx(AudioController.SfxCode.RewardItem,0.3f);
            GameObject ob = PickupPooler.instance.GetpooledObject();
            ob.transform.SetParent(transform.Find("PooledObj"));
            ob.transform.position = currentCoin.transform.position;
            ob.transform.localScale = new Vector3(1, 1, 1);
            ob.GetComponent<RunnerUnitMovements>().TimeScale = LevelSpeed;
            ob.SetActive(true);
            currentCoin.gameObject.SetActive(false);
        }
        IEnumerator IncreaseSpeed()
        {
            while (true)
            {
                yield return new WaitForSeconds(20f/ LevelSpeed);
                if (currentRunnerState == RunnerState.Playing)
                {
                    LevelSpeed += 0.05f;
                    SpeedChange();
                }
            }
        }

        IEnumerator MissileSpawn()
        {
            //RunnerMissile currentEnemy = missileEnemy.GetComponent<RunnerMissile>(); //rocket enemy
            var sprite = missileEnemy.WarningSprite.GetComponent<SpriteRenderer>();
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(8f, 15f));
                if (currentRunnerState == RunnerState.Playing)
                {
                    //fade sign
                    sprite.color = new Color(1, 1, 1, 0);
                    missileEnemy.WarningSprite.gameObject.SetActive(true);

                    float learp = 0;
                    while (learp < 1)
                    {
                        learp += 0.0075f * LevelSpeed;
                        yield return new WaitForSeconds(0);
                        sprite.color = new Color(1f, 1f, 1f, learp);
                    }

                    //show warning
                    bool isVisible = false;
                    AudioController.instance.PlaySfx(AudioController.SfxCode.Alert);

                    missileEnemy.WarningSprite.gameObject.SetActive(false);
                    for (int i = 0; i < 8; i++)
                    {
                        isVisible = !isVisible;
                        missileEnemy.WarningSprite.gameObject.SetActive(isVisible);
                        yield return new WaitForSeconds(0.1f);
                    }
                    missileEnemy.WarningSprite.gameObject.SetActive(false);

                    if (currentRunnerState == RunnerState.Playing)
                    {
                        missileEnemy.Missile.transform.position = new Vector3(10, missileEnemy.WarningSprite.transform.position.y);
                        missileEnemy.Missile.GetComponent<RunnerUnitMovements>().TimeScale = LevelSpeed;
                        AudioController.instance.PlaySfx(AudioController.SfxCode.FailedEffect);
                        missileEnemy.Missile.gameObject.SetActive(true);
                    }

                }
            }

            //yield return new WaitForSeconds(0);
        }
        IEnumerator EnemySpawn()
        {
            bool isCoin = true;
            yield return new WaitForSeconds(3);
            while (true)
            {
                isCoin = !isCoin;
                if (currentRunnerState == RunnerState.Playing)
                {
                    sectionsPassed++;
                    if (isCoin)
                    {
                        //select coin
                        int rndCoin = Random.Range(0, CoinsArray.Length);
                        Transform currentCoin = CoinsArray[rndCoin];
                        foreach (Transform item in currentCoin)
                        {
                            item.gameObject.SetActive(true);
                        }
                        currentCoin.position = new Vector3(4, currentCoin.position.y);
                        currentCoin.GetComponent<RunnerUnitMovements>().TimeScale = LevelSpeed;
                        currentCoin.gameObject.SetActive(true);
                    }
                    else
                    {
                        //select enemy
                        int rndEnemy = Random.Range(1, enemiesArray.Length);
                        Transform currentEnemy = enemiesArray[rndEnemy];
                        currentEnemy.position = new Vector3(4, currentEnemy.position.y);
                        currentEnemy.GetComponent<RunnerUnitMovements>().TimeScale = LevelSpeed;
                        currentEnemy.gameObject.SetActive(true);
                    }

                }
                yield return new WaitForSeconds(Random.Range(7f, 10f) / LevelSpeed);
            }
        }

        public void PlayerGotHit()
        {
            currentRunnerState = RunnerState.GotHit;
            player.runner = currentRunnerState;
            StartCoroutine(GotRecover(true));
        }

        IEnumerator GotRecover(bool DoReduce)
        {

            if (DoReduce)
                life--;
            else
            {
                yield return StartCoroutine(player.RecoverAnimation());
            }

            //livesTaken++;
            //SetLifeUI(life - gameSeed);
            if (life <= gameSeed)
            {
                ActionCamera.instance.Shake();

                currentRunnerState = RunnerState.Dead;
                player.runner = currentRunnerState;

                dieSpeed = LevelSpeed;
                LevelSpeed = 0;
                SpeedChange();

                StopCoroutine(ScorePerMinute());
                StopCoroutine(EnemySpawn());
                StopCoroutine(IncreaseSpeed());
                StopCoroutine(MissileSpawn());

                //clear all pooled objects
                foreach (Transform item in enemiesArray)
                    item.gameObject.SetActive(false);
                foreach (Transform item in CoinsArray)
                    item.gameObject.SetActive(false);
                missileEnemy.EliminateMissile();

                //player animation
                player.RunnerSkeletonAnimation.state.TimeScale = 1;
                player.ChangeState("Lose");

                yield return new WaitForSeconds(3f);
                SelectedUtilitie = null;

                //show end booster
                yield return StartCoroutine(RunnerUI.ShowEndBooster(livesBought, gameSeed));

                if (SelectedUtilitie != null)
                {
                    AudioController.instance.PlaySfx(AudioController.SfxCode.BgmGreatSuccess);

                    //prepare End Features
                    object FuncParam = new
                    {
                        //core
                        block = Game.Game.instance.Agent.BlockIndex,
                        premium = Premium.CurrentPandoraPlayer.IsPremium(),
                        address = States.Instance.CurrentAvatarState.agentAddress.ToString(),
                        //Special
                        utilitie = SelectedUtilitie != null ? SelectedUtilitie.ItemID : "None",
                        currency = SelectedUtilitie != null ? SelectedUtilitie.CurrencySTR : "XX",
                        count = livesBought,
                        currentDistance = scoreDistance,
                        currentCoins = scoreCoins,
                        //Statistics
                        seed = gameSeed,
                        playTime = (int)playTimer,
                        pauseTime = (int)idleTimer
                    };

                    PlayFabClientAPI.ExecuteCloudScript(
                    new ExecuteCloudScriptRequest { FunctionName = "EndFeatures", FunctionParameter = FuncParam },
                    success =>
                    {
                        if (success.FunctionResult.ToString() == "Success")
                        {
                            life = gameSeed + 1;
                            livesBought++;

                            int newPrice = (SelectedUtilitie.ItemPrice * (livesBought - gameSeed));
                            PandoraMaster.PlayFabInventory.VirtualCurrency["PC"] -= newPrice; //just UI update instead of request new call
                            StartCoroutine(GotRecover(false));
                        }
                        else
                            EndGame();
                    },
                    failed =>
                    {
                        Debug.LogError("Score Not Sent!, " + failed.GenerateErrorReport());
                    });
                }
                else
                    EndGame();
            }
            else
            {
                foreach (Transform item in enemiesArray)
                    item.gameObject.SetActive(false);
                foreach (Transform item in CoinsArray)
                    item.gameObject.SetActive(false);
                missileEnemy.EliminateMissile();

                //warningObj.gameObject.SetActive(false);
                //reduce speed
                //LevelSpeed = Mathf.Clamp(LevelSpeed - 1f, 1, 4);
                //SpeedChange();

                //var playerGFX = player.transform.Find("GFX").GetComponent<SkeletonAnimation>();
                //bool isVisible = true;
                //for (int i = 0; i < 12; i++)
                //{
                //    isVisible = !isVisible;
                //    if (isVisible)
                //        playerGFX.skeleton.SetColor(new Color(1, 1, 1, 1));
                //    else
                //        playerGFX.skeleton.SetColor(new Color(1, 1, 1, 0.1f));
                //    yield return new WaitForSeconds(0.1f);
                //}
                //playerGFX.skeleton.SetColor(new Color(1, 1, 1, 1));

                currentRunnerState = RunnerState.Playing;
                player.runner = currentRunnerState;

                LevelSpeed = dieSpeed;
                SpeedChange();

                StartCoroutine(ScorePerMinute());
                StartCoroutine(EnemySpawn());
                StartCoroutine(IncreaseSpeed());
                StartCoroutine(MissileSpawn());
            }
        }

        void EndGame()
        {
            RunnerUI.centerText.gameObject.SetActive(true);
            RunnerUI.centerText.text = "Game Over!";

            //posting score to leaderboard
            var request = new ExecuteCloudScriptRequest
            {
                FunctionName = "postStats",
                FunctionParameter = new
                {
                    //core
                    block = Game.Game.instance.Agent.BlockIndex,
                    premium = Premium.CurrentPandoraPlayer.IsPremium(),
                    address = States.Instance.CurrentAvatarState.agentAddress.ToString(),

                    //Special
                    distance = scoreDistance,
                    sections = sectionsPassed,
                    coins = scoreCoins,

                    //Statistics
                    seed = gameSeed,
                    playTime = (int)playTimer,
                    pauseTime = (int)idleTimer,

                    //statistics
                    quickRevive = livesBought,
                    speedBoosterUsed = speedBooster
                }
            };
            //Debug.LogError($"{gameSeed},{scoreDistance},{sectionsPassed},{scoreCoins},{livesTaken},{speedBooster}");
            PlayFabClientAPI.ExecuteCloudScript(request,
                success =>
                {
                    if (success.FunctionResult.ToString() == "Success")
                        PandoraMaster.PlayFabInventory.VirtualCurrency["PC"] += scoreCoins - gameSeed; //just UI update instead of request new call
                    else
                        NotificationSystem.Push(Nekoyume.Model.Mail.MailType.System, "PandoraBox: sending score <color=red>Failed!</color>",
                            NotificationCell.NotificationType.Alert);

                    RunnerUI.centerText.gameObject.SetActive(false);
                    RunnerUI.UIElement.SetActive(false);
                    RunnerUI.FinalResult.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = (scoreDistance - gameSeed) + "<size=60%>M</size>";
                    RunnerUI.FinalResult.transform.GetChild(3).GetComponent<TextMeshProUGUI>().text = (scoreCoins - gameSeed).ToString();
                    RunnerUI.FinalResult.SetActive(true);
                },
                failed =>
                {
                    Debug.LogError("Failed to StartFeatures!, " + failed.GenerateErrorReport());
                });
        }

        //IEnumerator CrystalSpawner()
        //{
        //    while (true)
        //    {
        //        yield return new WaitForSeconds(Random.Range(10, 20));
        //        if (currentRunnerState == RunnerState.Play)
        //        {
        //            GameObject ob = CrystalPooler.instance.GetpooledObject();
        //            ob.transform.SetParent(transform);
        //            ob.GetComponent<RectTransform>().anchoredPosition = crystalSpawner.GetComponent<RectTransform>().anchoredPosition;
        //            ob.GetComponent<RectTransform>().anchorMax = crystalSpawner.GetComponent<RectTransform>().anchorMax;
        //            ob.GetComponent<RectTransform>().anchorMin = crystalSpawner.GetComponent<RectTransform>().anchorMin;
        //            ob.transform.localScale = new Vector3(1, 1, 1);
        //            ob.GetComponent<RunnerUnitMovements>().TimeScale = LevelSpeed;
        //            ob.SetActive(true);
        //        }
        //    }
        //}

        public void UpdateScore(int value, int typeScore) //0 = msafa , 1 = coin
        {
            //runnerPlayer.GetComponent<RunnerController>().TimeScale = LevelSpeed; // MOVE IT!!
            if (typeScore == 0)
            {
                scoreDistance += value;
                RunnerUI.scoreText.text = (scoreDistance - gameSeed) + "<size=25> M</size>";
            }
            else if (typeScore == 1)
            {
                scoreCoins += value;
                RunnerUI.coinText.text = "x " + (scoreCoins - gameSeed);
            }
        }

        IEnumerator ScorePerMinute()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.1f/LevelSpeed);
                if (currentRunnerState == RunnerState.Playing)
                    UpdateScore(1, 0);
            }
        }

        void SpeedChange()
        {
            //speed all level objects
            foreach (Transform item in BgArray)
                item.GetComponent<RunnerUnitMovements>().TimeScale = LevelSpeed;
            foreach (GameObject item in GetComponent<PickupPooler>().pooledObjects)
                item.GetComponent<RunnerUnitMovements>().TimeScale = LevelSpeed;
            foreach (Transform item in enemiesArray)
                item.GetComponent<RunnerUnitMovements>().TimeScale = LevelSpeed;
            foreach (Transform item in CoinsArray)
                item.GetComponent<RunnerUnitMovements>().TimeScale = LevelSpeed;

            //player
            player.RunnerSkeletonAnimation.state.TimeScale = LevelSpeed;
            player.WalkingSound.pitch = Mathf.Clamp(LevelSpeed, 1, 2);
        }

        //IEnumerator PrepareLife()
        //{
        //    yield return new WaitForSeconds(1.5f);
        //    int lifeAnimation = 0;
        //    for (int i = 0; i < 3; i++)
        //    {
        //        lifeAnimation++;
        //        SetLifeUI(lifeAnimation);
        //        yield return new WaitForSeconds(0.5f);
        //    }
        //}

        //public void SetLifeUI(int lives)
        //{
        //    lives = Mathf.Clamp(lives, 0, 3);
        //    foreach (Transform item in healthHolder)
        //        item.gameObject.SetActive(false);

        //    for (int i = 0; i < lives; i++)
        //        healthHolder.GetChild(i).gameObject.SetActive(true);
        //}
    }
}
