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
        public enum RunnerState { Start, Play, Die, Hit, Info, Pause }
        public RunnerController player;
        [SerializeField] Transform[] BgArray;
        [SerializeField] Transform[] enemiesArray;
        [SerializeField] Transform[] CoinsArray;

        [SerializeField] Transform runnerBoss;

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

        public Runner RunnerUI; //DELETE

        public static PandoraRunner instance;
        void Awake()
        {
            AudioController.instance.Initialize(); //delete
            //ActionCamera.instance.Shake();
            instance = this; //delete
        }

        private void Start()
        {
            //Login into playfab
            PlayFabClientAPI.LoginWithCustomID(new LoginWithCustomIDRequest
            {
                CustomId = "0x46528E7DEdaC16951bDccb55B20303AB0c729679",
                CreateAccount = true,
                InfoRequestParameters = new GetPlayerCombinedInfoRequestParams { GetPlayerProfile = true }
            },
            success =>
            {
                if (success.InfoResultPayload.PlayerProfile != null)
                {
                    PandoraMaster.PlayFabCurrentPlayer = success.InfoResultPayload.PlayerProfile;

                    //get inv
                    PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(),
                    succuss =>
                    {
                        PandoraMaster.PlayFabInventory = succuss;
                        try
                        {
                            OnRunnerStart();
                        }
                        catch { }
                    }, fail =>
                    {
                        PandoraMaster.Instance.ShowError(322, "Pandora cannot read Player Inventory!");
                    });
                }
            },
            failed =>
            {
                if (failed.Error == PlayFabErrorCode.AccountBanned)
                    PandoraMaster.Instance.ShowError(101, "This address is Banned, please visit us for more information!");
                else
                    Debug.LogError(failed.GenerateErrorReport());
            });


            
        }

        public void Initialize()
        {
            //something to Initialize
        }

        void Update()
        {
            //warningObj.anchoredPosition = new Vector2(warningObj.anchoredPosition.x, runnerPlayer.position.y - 360);
        }

        public void OnRunnerStart()
        {
            Time.timeScale = 1;

            //Widget.Find<HeaderMenuStatic>().Close();
            //Widget.Find<Menu>().Close();
            //Widget.Find<VersionSystem>().Close();
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
            player.runner = currentRunnerState;

            AudioController.instance.PlayMusic(AudioController.MusicCode.Runner1, 0.5f);
            RunnerUI.FinalResult.SetActive(false);
            RunnerUI.UIElement.SetActive(false);

            //reset boss position
            player.transform.position = new Vector2(-4.185f, -1.11f);
            runnerBoss.position = new Vector2(-6.463f, -1.11f);
            player.gameObject.SetActive(true);
            runnerBoss.gameObject.SetActive(true);

            SelectedBooster = null;

            StartCoroutine(PrepareGame());
        }

        IEnumerator PrepareGame()
        {
            //intro animation
            player.WalkingSound.enabled = true;
            //float learp = 0;
            while (player.transform.position.x < 1)
            {
                //learp += Time.deltaTime / 2;
                yield return new WaitForSeconds(0.02f);
                player.transform.position += new Vector3(0.04f, 0);
                runnerBoss.position += new Vector3(0.04f, 0);
            }
            runnerBoss.GetComponent<AudioSource>().PlayOneShot(runnerBoss.GetComponent<AudioSource>().clip);

            RunnerUI.Show();
            yield return StartCoroutine(RunnerUI.ShowBoosterSelection()); //Widget.Find<Runner>()

            //runnerBoss.GetComponent<AudioSource>().PlayOneShot(runnerBoss.GetComponent<AudioSource>().clip);

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

            RunnerUI.UIElement.SetActive(true);
            RunnerUI.centerText.gameObject.SetActive(false);
            currentRunnerState = RunnerState.Play;
            player.GetComponent<RunnerController>().runner = RunnerState.Play;
            //


            if (isSuccess.ToString() == "Failed")
            {
                //NotificationSystem.Push(Nekoyume.Model.Mail.MailType.System, "PandoraBox: No Booster Failed!", NotificationCell.NotificationType.Alert);
                StartCoroutine(FinishStartAnimation(false));
            }
            else
            {
                PandoraMaster.PlayFabInventory.VirtualCurrency["PC"] -= SelectedBooster.ItemPrice; //just UI update instead of request new call
                StartCoroutine(FinishStartAnimation(true));
            }

            
        }

        IEnumerator FinishStartAnimation(bool isBoosted)
        {
            runnerBoss.GetComponent<AudioSource>().PlayOneShot(runnerBoss.GetComponent<AudioSource>().clip);
            while (player.transform.position.x > -2)
            {
                yield return new WaitForSeconds(0.02f);
                player.transform.position -= new Vector3(0.025f, 0);
                runnerBoss.position -= new Vector3(0.04f, 0);
            }

            if (isBoosted)
            {
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

            //StartCoroutine(PrepareLife());
            StartCoroutine(ScorePerMinute());
            StartCoroutine(EnemySpawn());
            StartCoroutine(IncreaseSpeed());
            StartCoroutine(RocketSpawn());
        }

        IEnumerator SpeedUp(int targetDistance)
        {
            player.GetComponent<RunnerController>().EnableSpeed(true);
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

            player.GetComponent<RunnerController>().EnableSpeed(false);
        }

        public void CollectCoins(Transform currentCoin)
        {
            UpdateScore(1, 1);
            AudioController.instance.PlaySfx(AudioController.SfxCode.RewardItem);

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
                    RunnerMissile currentEnemy = enemiesArray[0].GetComponent<RunnerMissile>(); //rocket enemy

                    //show warning
                    bool isVisible = false;
                    AudioController.instance.PlaySfx(AudioController.SfxCode.Alert);

                    currentEnemy.WarningSprite.gameObject.SetActive(false);
                    for (int i = 0; i < 10; i++)
                    {
                        isVisible = !isVisible;
                        currentEnemy.WarningSprite.gameObject.SetActive(isVisible);
                        yield return new WaitForSeconds(0.2f / LevelSpeed);
                    }
                    currentEnemy.WarningSprite.gameObject.SetActive(false);

                    if (currentRunnerState == RunnerState.Play)
                    {

                        currentEnemy.Missile.transform.position = new Vector3(10, currentEnemy.WarningSprite.transform.position.y);
                        currentEnemy.GetComponent<RunnerUnitMovements>().TimeScale = LevelSpeed;
                        AudioController.instance.PlaySfx(AudioController.SfxCode.DamageFire);
                        currentEnemy.gameObject.SetActive(true);
                    }

                }
            }

            yield return new WaitForSeconds(0);
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
            currentRunnerState = RunnerState.Hit;
            player.GetComponent<RunnerController>().runner = RunnerState.Hit;
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
                currentRunnerState = RunnerState.Die;
                player.runner = RunnerState.Die;

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

                //player animation
                player.RunnerSkeletonAnimation.state.TimeScale = 1;
                player.LoseAnimation();

                yield return new WaitForSeconds(1f);
                SelectedBooster = null;

                RunnerUI.UIBoostersDie.SetActive(true);
                Transform boosters = RunnerUI.UIBoostersDie.transform.Find("Boosters");
                for (int i = 0; i < 1; i++) // it should be UIBoosters.childCount when all boost is available
                {
                    boosters.GetChild(i).GetComponent<BoosterSlot>().SetItemData();
                }
                int newPrice = boosters.GetChild(0).GetComponent<BoosterSlot>().ItemPrice
                    + (boosters.GetChild(0).GetComponent<BoosterSlot>().ItemPrice * (livesBought-gameSeed));
                boosters.GetChild(0).GetComponent<BoosterSlot>().itemPrice.text = "x " + (newPrice); ;

                for (int i = 5; i > 0; i--)
                {
                    RunnerUI.DieCounterText.text = i.ToString();
                    AudioController.instance.PlaySfx(AudioController.SfxCode.OptionNormal);
                    yield return new WaitForSeconds(1);
                }
                //runnerBoss.GetComponent<AudioSource>().PlayOneShot(runnerBoss.GetComponent<AudioSource>().clip);
                RunnerUI.UIBoostersDie.SetActive(false);

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
                //warningObj.gameObject.SetActive(false);
                //reduce speed
                //LevelSpeed = Mathf.Clamp(LevelSpeed - 1f, 1, 4);
                //SpeedChange();

                var playerGFX = player.transform.Find("GFX").GetComponent<SkeletonAnimation>();
                bool isVisible = true;
                for (int i = 0; i < 12; i++)
                {
                    isVisible = !isVisible;
                    if (isVisible)
                        playerGFX.skeleton.SetColor(new Color(1, 1, 1, 1));
                    else
                        playerGFX.skeleton.SetColor(new Color(1, 1, 1, 0.1f));
                    yield return new WaitForSeconds(0.1f);
                }
                playerGFX.skeleton.SetColor(new Color(1, 1, 1, 1));

                currentRunnerState = RunnerState.Play;
                player.GetComponent<RunnerController>().runner = RunnerState.Play;

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
            RunnerUI.centerText.gameObject.SetActive(true);
            RunnerUI.centerText.text = "Game Over!";

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
                    address = "0x1012041FF2254f43d0a938aDF89c3f11867A2A58", //address = States.Instance.CurrentAvatarState.agentAddress.ToString(),

                    //statistics
                    lives = livesTaken,
                    speedBoosterUsed = speedBooster
                }
            };
            //Debug.LogError($"{gameSeed},{scoreDistance},{sectionsPassed},{scoreCoins},{livesTaken},{speedBooster}");
            PlayFabClientAPI.ExecuteCloudScript(request, OnValidateSuccess, OnPlayFabError);


            yield return new WaitForSeconds(3f);
            RunnerUI.centerText.gameObject.SetActive(false);
            RunnerUI.UIElement.SetActive(false);
            RunnerUI.FinalResult.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = (scoreDistance - gameSeed) + "<size=60%>M</size>";
            RunnerUI.FinalResult.transform.GetChild(3).GetComponent<TextMeshProUGUI>().text = (scoreCoins - gameSeed).ToString();
            RunnerUI.FinalResult.SetActive(true);
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

                //if (scoreDistance - gameSeed > Widget.Find<NineRunnerPopup>().ScrollContent.GetChild(0).GetComponent<RunnerCell>().CurrentCellContent.Score)
                //{
                //    //send highscore notify
                //    Premium.SendWebhookT(DatabasePath.PandoraDiscordBot, $"<:highScore:1009757079042539520>**[High Score]** {PandoraMaster.PlayFabCurrentPlayer.DisplayName} " +
                //            $" broke {Widget.Find<NineRunnerPopup>().ScrollContent.GetChild(0).GetComponent<RunnerCell>().CurrentCellContent.PlayerName}" +
                //            $" by scoring **7897m**").Forget();
                //}
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

        public void ExitToMenu()
        {
            Time.timeScale = 1;
            Game.Event.OnRoomEnter.Invoke(false);
            Widget.Find<VersionSystem>().Show();
            gameObject.SetActive(false);
        }
    }
}
