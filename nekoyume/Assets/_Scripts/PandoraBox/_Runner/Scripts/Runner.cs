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

namespace Nekoyume.UI
{
    public class Runner : Widget
    {
        public enum RunnerState { Start, Play, Die, Hit, Info, Pause }
        [SerializeField] Transform[] BgArray;
        [SerializeField] Transform[] enemiesArray;
        [SerializeField] Transform[] CoinsArray;
        [SerializeField] TextMeshProUGUI centerText = null;
        [SerializeField] TextMeshProUGUI scoreText;
        [SerializeField] TextMeshProUGUI coinText;
        [SerializeField] TextMeshProUGUI startCounterText;
        [SerializeField] TextMeshProUGUI DieCounterText;
        //[SerializeField] Transform healthHolder = null;
        [SerializeField] Transform coinSpawner;
        [SerializeField] Transform crystalSpawner;
        [SerializeField] RectTransform runnerPlayer;
        [SerializeField] RectTransform runnerBoss;
        [SerializeField] RectTransform warningObj;
        [SerializeField] GameObject FinalResult;
        [SerializeField] GameObject UIElement;
        [SerializeField] GameObject UIBoosters;
        [SerializeField] GameObject UIBoostersDie;
        public Sprite[] Sprites;

        //booster
        public BoosterSlot SelectedBooster = null;

        int scoreDistance;
        int scoreCoins;
        int life;
        int sectionsPassed;
        public float LevelSpeed = 1f;
        float dieSpeed = 1;

        //for statistics
        int gameSeed = 0;
        int avoidMissiles = 0;
        int livesTaken = 0;
        int livesBought = 0;
        int speedBooster = 0;

        RunnerState currentRunnerState = RunnerState.Start;

        public static Runner instance;
        protected override void Awake()
        {
            base.Awake();
            //ActionCamera.instance.Shake();
            instance = this;
        }

        protected override void Update()
        {
            warningObj.anchoredPosition = new Vector2(warningObj.anchoredPosition.x, runnerPlayer.anchoredPosition.y - 360);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            Time.timeScale = 1;

            Find<HeaderMenuStatic>().Close();
            Find<Menu>().Close();
            Find<VersionSystem>().Close();
            base.Show(ignoreShowAnimation);
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

            currentRunnerState = RunnerState.Start;
            runnerPlayer.GetComponent<RunnerController>().runner = RunnerState.Start;

            AudioController.instance.PlayMusic(AudioController.MusicCode.Runner1, 0.5f);
            FinalResult.SetActive(false);
            UIElement.SetActive(false);

            //reset boss position
            runnerPlayer.anchoredPosition = new Vector2(-90, 150);
            runnerBoss.anchoredPosition = new Vector2(-450, 25);

            SelectedBooster = null;

            StartCoroutine(PrepareGame());
        }

        IEnumerator PrepareGame()
        {
            //intro animation
            float learp = 0;
            while (learp < 1)
            {
                learp += Time.deltaTime / 2;
                yield return new WaitForSeconds(0);
                runnerPlayer.anchoredPosition += new Vector2(4.5f, 0);
                runnerBoss.anchoredPosition += new Vector2(4.5f, 0);
            }
            runnerBoss.GetComponent<AudioSource>().PlayOneShot(runnerBoss.GetComponent<AudioSource>().clip);

            startCounterText.text = "";
            UIBoosters.SetActive(true);
            Transform boosters = UIBoosters.transform.Find("Boosters");
            for (int i = 0; i < 2; i++) // it should be UIBoosters.childCount when all boost is available
            {
                boosters.GetChild(i).GetComponent<BoosterSlot>().SetItemData();
            }

            for (int i = 5; i > 0; i--)
            {
                startCounterText.text = i.ToString();
                AudioController.instance.PlaySfx(AudioController.SfxCode.OptionNormal);
                yield return new WaitForSeconds(1);
            }
            runnerBoss.GetComponent<AudioSource>().PlayOneShot(runnerBoss.GetComponent<AudioSource>().clip);
            UIBoosters.SetActive(false);

            //prepare start could function
            object FuncParam = new { booster = "None" };
            if (SelectedBooster != null)
                FuncParam = new { booster = SelectedBooster.ItemID };

            //Get Greenlight from Server
            var request = new ExecuteCloudScriptRequest
            {
                FunctionName = "startGame",
                FunctionParameter = FuncParam
            };
            PlayFabClientAPI.ExecuteCloudScript(request, OnStartSuccess, OnPlayFabError);
        }

        void OnEndSuccess(ExecuteCloudScriptResult result)
        {
            if (result.FunctionResult.ToString() == "Success")
            {
                life= gameSeed + 1;
                livesBought++;


                int newPrice = SelectedBooster.ItemPrice + (SelectedBooster.ItemPrice * (livesBought - gameSeed));
                PandoraMaster.PlayFabInventory.VirtualCurrency["PC"] -= newPrice; //just UI update instead of request new call
                StartCoroutine(GotRecover(false));
            }
            else
            {
                StartCoroutine(EndGame());
            }
        }

        void OnStartSuccess(ExecuteCloudScriptResult result)
        {
            JsonObject jsonResult = (JsonObject)result.FunctionResult;
            object isSuccess;
            object seedF;
            //Debug.LogError(result.FunctionResult);
            jsonResult.TryGetValue("success", out isSuccess);
            jsonResult.TryGetValue("seed", out seedF);

            gameSeed = int.Parse(seedF.ToString());
            //Debug.LogError(gameSeed);

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
            livesTaken = 0 + gameSeed;
            speedBooster = 0 + gameSeed;

            UIElement.SetActive(true);
            centerText.gameObject.SetActive(false);
            currentRunnerState = RunnerState.Play;
            runnerPlayer.GetComponent<RunnerController>().runner = RunnerState.Play;
            //


            if (isSuccess.ToString() == "Failed")
            {
                //NotificationSystem.Push(Nekoyume.Model.Mail.MailType.System, "PandoraBox: No Booster Failed!", NotificationCell.NotificationType.Alert);
            }
            else
            {
                PandoraMaster.PlayFabInventory.VirtualCurrency["PC"] -= SelectedBooster.ItemPrice; //just UI update instead of request new call

                switch (SelectedBooster.ItemID)
                {
                    case "Boost750":
                        StartCoroutine(SpeedUp(750));
                        break;
                    case "Boost1500":
                        StartCoroutine(SpeedUp(1500));
                        break;
                }
            }

            StartCoroutine(FinishStartAnimation());
        }

        IEnumerator FinishStartAnimation()
        {
            float learp = 0;
            runnerBoss.GetComponent<AudioSource>().PlayOneShot(runnerBoss.GetComponent<AudioSource>().clip);
            while (learp < 1)
            {
                learp += Time.deltaTime / 2;
                yield return new WaitForSeconds(0);
                runnerPlayer.anchoredPosition -= new Vector2(2f, 0);
                runnerBoss.anchoredPosition -= new Vector2(4, 0);
            }

            //StartCoroutine(PrepareLife());
            StartCoroutine(ScorePerMinute());
            StartCoroutine(EnemySpawn());
            StartCoroutine(IncreaseSpeed());
            StartCoroutine(RocketSpawn());
        }

        IEnumerator SpeedUp(int targetDistance)
        {
            runnerPlayer.GetComponent<RunnerController>().EnableSpeed(true);
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
                yield return new WaitForSeconds(0.1f);
                scoreDistance += 7;
            }

            learp = 0;
            while (learp < 1)
            {
                learp += Time.deltaTime / 1;
                yield return new WaitForSeconds(0);
                LevelSpeed = Mathf.Lerp(boostSpeed,1 , learp);// Mathf.Clamp(LevelSpeed + boostSpeed, 1, boostSpeed);
                SpeedChange();
            }

            foreach (Transform item in enemiesArray)
                item.gameObject.SetActive(false);

            runnerPlayer.GetComponent<RunnerController>().EnableSpeed(false);
        }

        public void CollectCoins(RectTransform currentCoin)
        {
            UpdateScore(1, 1);
            AudioController.instance.PlaySfx(AudioController.SfxCode.RewardItem);

            GameObject ob = PickupPooler.instance.GetpooledObject();
            ob.transform.SetParent(transform.Find("PooledObj"));
            ob.GetComponent<RectTransform>().position = currentCoin.GetComponent<RectTransform>().position;
            ob.transform.localScale = new Vector3(1, 1, 1);
            ob.GetComponent<RunnerUnitMovements>().TimeScale = LevelSpeed;
            ob.SetActive(true);
            currentCoin.gameObject.SetActive(false);
        }
        IEnumerator IncreaseSpeed()
        {
            while (true)
            {
                yield return new WaitForSeconds(15f/ LevelSpeed);
                if (currentRunnerState == RunnerState.Play)
                {
                    LevelSpeed += 0.1f;
                    SpeedChange();
                }
            }
        }

        IEnumerator RocketSpawn()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(5f, 10f) / LevelSpeed);
                if (currentRunnerState == RunnerState.Play)
                {
                    //show warning
                    bool isVisible = false;
                    for (int i = 0; i < 10; i++)
                    {
                        isVisible = !isVisible;
                        warningObj.gameObject.SetActive(isVisible);
                        if (isVisible)
                            AudioController.instance.PlaySfx(AudioController.SfxCode.Alert);
                        yield return new WaitForSeconds(0.2f / LevelSpeed);
                    }
                    warningObj.gameObject.SetActive(false);

                    if (currentRunnerState == RunnerState.Play)
                    {
                        Transform currentEnemy = enemiesArray[0]; //rocket enemy
                        currentEnemy.GetComponent<RectTransform>().anchoredPosition = new Vector2(850, warningObj.anchoredPosition.y);
                        currentEnemy.GetComponent<RunnerUnitMovements>().TimeScale = LevelSpeed;
                        AudioController.instance.PlaySfx(AudioController.SfxCode.DamageFire);
                        currentEnemy.gameObject.SetActive(true);
                    }

                }
            }
        }
        IEnumerator EnemySpawn()
        {
            bool isCoin = true;
            yield return new WaitForSeconds(3);
            while (true)
            {
                isCoin = !isCoin;
                if (currentRunnerState == RunnerState.Play)
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
                        currentCoin.GetComponent<RectTransform>().anchoredPosition = new Vector2(850, currentCoin.GetComponent<RectTransform>().anchoredPosition.y);
                        currentCoin.GetComponent<RunnerUnitMovements>().TimeScale = LevelSpeed;
                        currentCoin.gameObject.SetActive(true);
                    }
                    else
                    {
                        //select enemy
                        int rndEnemy = Random.Range(1, enemiesArray.Length);
                        Transform currentEnemy = enemiesArray[rndEnemy];
                        currentEnemy.GetComponent<RectTransform>().anchoredPosition = new Vector2(850, currentEnemy.GetComponent<RectTransform>().anchoredPosition.y);
                        currentEnemy.GetComponent<RunnerUnitMovements>().TimeScale = LevelSpeed;
                        currentEnemy.gameObject.SetActive(true);
                    }

                }
                yield return new WaitForSeconds(Random.Range(7f, 10f) / LevelSpeed);
            }
        }

        public void PlayerGotHit()
        {
            currentRunnerState = RunnerState.Hit;
            runnerPlayer.GetComponent<RunnerController>().runner = RunnerState.Hit;
            StartCoroutine(GotRecover(true));
        }

        IEnumerator GotRecover(bool DoReduce)
        {
            if (DoReduce)
                life--;

            //livesTaken++;
            //SetLifeUI(life - gameSeed);
            if (life <= gameSeed)
            {
                currentRunnerState = RunnerState.Die;
                runnerPlayer.GetComponent<RunnerController>().runner = RunnerState.Die;

                dieSpeed = LevelSpeed;
                LevelSpeed = 0;
                SpeedChange();

                StopCoroutine(ScorePerMinute());
                StopCoroutine(EnemySpawn());
                StopCoroutine(IncreaseSpeed());
                StopCoroutine(RocketSpawn());
                
                //clear all pooled objects
                foreach (Transform item in enemiesArray)
                    item.gameObject.SetActive(false);
                foreach (Transform item in CoinsArray)
                    item.gameObject.SetActive(false);


                yield return new WaitForSeconds(1f);
                SelectedBooster = null;

                UIBoostersDie.SetActive(true);
                Transform boosters = UIBoostersDie.transform.Find("Boosters");
                for (int i = 0; i < 1; i++) // it should be UIBoosters.childCount when all boost is available
                {
                    boosters.GetChild(i).GetComponent<BoosterSlot>().SetItemData();
                }
                int newPrice = boosters.GetChild(0).GetComponent<BoosterSlot>().ItemPrice
                    + (boosters.GetChild(0).GetComponent<BoosterSlot>().ItemPrice * (livesBought-gameSeed));
                boosters.GetChild(0).GetComponent<BoosterSlot>().itemPrice.text = "x " + (newPrice); ;

                for (int i = 5; i > 0; i--)
                {
                    DieCounterText.text = i.ToString();
                    AudioController.instance.PlaySfx(AudioController.SfxCode.OptionNormal);
                    yield return new WaitForSeconds(1);
                }
                //runnerBoss.GetComponent<AudioSource>().PlayOneShot(runnerBoss.GetComponent<AudioSource>().clip);
                UIBoostersDie.SetActive(false);

                //prepare start could function
                object FuncParam = new { booster = "None" };
                if (SelectedBooster != null)
                {
                    //player want to continue
                    FuncParam = new { booster = SelectedBooster.ItemID, count = livesBought - gameSeed };

                    //check if player has enough cost for end boosters
                    var request = new ExecuteCloudScriptRequest
                    {
                        FunctionName = "EndBoosters",
                        FunctionParameter = FuncParam
                    };
                    PlayFabClientAPI.ExecuteCloudScript(request, OnEndSuccess, OnPlayFabError);
                }
                else
                {
                    StartCoroutine(EndGame());
                }
            }
            else
            {
                foreach (Transform item in enemiesArray)
                    item.gameObject.SetActive(false);
                warningObj.gameObject.SetActive(false);
                //reduce speed
                //LevelSpeed = Mathf.Clamp(LevelSpeed - 1f, 1, 4);
                //SpeedChange();

                SkeletonGraphic playerGFX = runnerPlayer.transform.Find("GFX").GetComponent<SkeletonGraphic>();
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
                runnerPlayer.GetComponent<RunnerController>().runner = RunnerState.Play;

                LevelSpeed = dieSpeed;
                SpeedChange();

                StartCoroutine(ScorePerMinute());
                StartCoroutine(EnemySpawn());
                StartCoroutine(IncreaseSpeed());
                StartCoroutine(RocketSpawn());
            }
        }

        IEnumerator EndGame()
        {
            centerText.gameObject.SetActive(true);
            centerText.text = "Game Over!";

            //Check for cheating
            var request = new ExecuteCloudScriptRequest
            {
                FunctionName = "validateScore",
                FunctionParameter = new
                {
                    iac = gameSeed,
                    distance = scoreDistance,
                    sections = sectionsPassed,
                    coins = scoreCoins,
                    address = States.Instance.CurrentAvatarState.agentAddress.ToString(),

                    //statistics
                    lives = livesTaken,
                    speedBoosterUsed = speedBooster
                }
            };
            //Debug.LogError($"{gameSeed},{scoreDistance},{sectionsPassed},{scoreCoins},{livesTaken},{speedBooster}");
            PlayFabClientAPI.ExecuteCloudScript(request, OnValidateSuccess, OnPlayFabError);


            yield return new WaitForSeconds(3f);
            centerText.gameObject.SetActive(false);
            UIElement.SetActive(false);
            FinalResult.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = (scoreDistance - gameSeed) + "<size=60%>M</size>";
            FinalResult.transform.GetChild(3).GetComponent<TextMeshProUGUI>().text = (scoreCoins - gameSeed).ToString();
            FinalResult.SetActive(true);
        }

        void OnValidateSuccess(ExecuteCloudScriptResult result)
        {
            if (result.FunctionResult.ToString() == "Failed")
            {
                NotificationSystem.Push(Nekoyume.Model.Mail.MailType.System, "PandoraBox: sending score <color=red>Failed!</color>", NotificationCell.NotificationType.Alert);
            }
            else
            {
                PandoraMaster.PlayFabInventory.VirtualCurrency["PC"] += scoreCoins; //just UI update instead of request new call

                if (scoreDistance - gameSeed > Find<NineRunnerPopup>().ScrollContent.GetChild(0).GetComponent<RunnerCell>().CurrentCellContent.Score)
                {
                    //send highscore notify
                    Premium.SendWebhookT(DatabasePath.PandoraDiscordBot, $"<:highScore:1009757079042539520>**[High Score]** {PandoraMaster.PlayFabCurrentPlayer.DisplayName} " +
                            $" broke {Find<NineRunnerPopup>().ScrollContent.GetChild(0).GetComponent<RunnerCell>().CurrentCellContent.PlayerName}" +
                            $" by scoring **7897m**").Forget();
                }
            }
        }

        void OnPlayFabError(PlayFabError result)
        {
            Debug.LogError("Score Not Sent!, " + result.GenerateErrorReport());
        }

        public void PauseGame(bool isPause)
        {
            if (isPause)
            {
                Time.timeScale = 0;
            }
            else
            {
                Time.timeScale = 1;
            }
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
            runnerPlayer.GetComponent<RunnerController>().TimeScale = LevelSpeed; // MOVE IT!!
            if (typeScore == 0)
            {
                scoreDistance += value;
                scoreText.text = (scoreDistance - gameSeed) + "<size=25> M</size>";
            }
            else if (typeScore == 1)
            {
                scoreCoins += value;
                coinText.text = "x " + (scoreCoins - gameSeed);
            }
        }

        IEnumerator ScorePerMinute()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.1f/LevelSpeed);
                if (currentRunnerState == RunnerState.Play)
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

        protected override void OnCompleteOfShowAnimationInternal()
        {
            base.OnCompleteOfShowAnimationInternal();
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);
        }

        //public void SetLifeUI(int lives)
        //{
        //    lives = Mathf.Clamp(lives, 0, 3);
        //    foreach (Transform item in healthHolder)
        //        item.gameObject.SetActive(false);

        //    for (int i = 0; i < lives; i++)
        //        healthHolder.GetChild(i).gameObject.SetActive(true);
        //}

        public void ExitToMenu()
        {
            Time.timeScale = 1;
            Game.Event.OnRoomEnter.Invoke(false);
            Find<VersionSystem>().Show();
            Close();
        }
    }
}
