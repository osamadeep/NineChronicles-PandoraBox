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

namespace Nekoyume.UI
{
    public class Runner : Widget
    {
        public enum RunnerState { Start, Play, Die, Hit, Info, Pause }
        [SerializeField] Transform[] enemiesArray;
        [SerializeField] Transform[] CoinsArray;
        [SerializeField] TextMeshProUGUI centerText = null;
        [SerializeField] TextMeshProUGUI scoreText;
        [SerializeField] TextMeshProUGUI coinText;
        [SerializeField] Transform healthHolder = null;
        [SerializeField] Transform coinSpawner;
        [SerializeField] Transform crystalSpawner;
        [SerializeField] RectTransform runnerPlayer;
        [SerializeField] RectTransform warningObj;
        [SerializeField] GameObject FinalResult;
        [SerializeField] GameObject UIElement;

        int scoreDistance;
        int scoreCoins;
        int life;
        int sectionsPassed;
        public float LevelSpeed = 1f;

        //for statistics
        int incrementAntiCheat = 0;
        int avoidMissiles = 0;
        int livesTaken = 0;
        int speedBooster = 0;


        RunnerState currentRunnerState = RunnerState.Start;


        protected override void Awake()
        {
            base.Awake();
            //ActionCamera.instance.Shake();
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

            //Get Greenlight from Server
            var request = new ExecuteCloudScriptRequest
            {
                FunctionName = "startGame"
            };
            PlayFabClientAPI.ExecuteCloudScript(request, OnStartSuccess, OnPlayFabError);
        }

        void OnStartSuccess(ExecuteCloudScriptResult result)
        {
            incrementAntiCheat = int.Parse(result.FunctionResult.ToString());
            Debug.LogError(incrementAntiCheat);

            //core variables
            scoreDistance = 0 + incrementAntiCheat;
            scoreCoins = 0 + incrementAntiCheat;
            sectionsPassed = 1 + incrementAntiCheat;
            UpdateScore(0, 0);
            UpdateScore(0, 1);

            //for statistics
            avoidMissiles = 0 + incrementAntiCheat;
            livesTaken = 0 + incrementAntiCheat;
            speedBooster = 0 + incrementAntiCheat;

            UIElement.SetActive(true);

            StartCoroutine(PrepareGame());
            StartCoroutine(PrepareLife());

            StartCoroutine(ScorePerMinute());
            StartCoroutine(EnemySpawn());
            StartCoroutine(IncreaseSpeed());
            StartCoroutine(RocketSpawn());
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
            runnerPlayer.GetComponent<RunnerController>().runner = RunnerState.Play;
            StartCoroutine(SpeedUp(750));
        }

        IEnumerator SpeedUp(int targetDistance)
        {
            speedBooster += targetDistance;
            int boostSpeed = 5;
            LevelSpeed = Mathf.Clamp(LevelSpeed + boostSpeed, 1, boostSpeed);
            SpeedChange();
            runnerPlayer.GetComponent<RunnerController>().EnableSpeed(true);

            while (scoreDistance < targetDistance)
            {
                yield return new WaitForSeconds(0.1f);
                scoreDistance += 5;
            }

            foreach (Transform item in enemiesArray)
                item.gameObject.SetActive(false);

            LevelSpeed = Mathf.Clamp(LevelSpeed - boostSpeed, 1, boostSpeed);
            SpeedChange();
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
            LevelSpeed = 1;
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
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(7f, 10f) / LevelSpeed);
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
            }
        }

        public void PlayerGotHit()
        {
            currentRunnerState = RunnerState.Hit;
            runnerPlayer.GetComponent<RunnerController>().runner = RunnerState.Hit;
            StartCoroutine(GotRecover());
        }

        IEnumerator GotRecover()
        {
            life--;
            livesTaken++;
            SetLifeUI(life - incrementAntiCheat);
            if (life <= incrementAntiCheat)
            {
                currentRunnerState = RunnerState.Die;
                centerText.gameObject.SetActive(true);
                centerText.text = "Game Over!";

                //Check for cheating
                var request = new ExecuteCloudScriptRequest
                {
                    FunctionName = "validateScore",
                    FunctionParameter = new
                    {
                        iac = incrementAntiCheat,
                        distance = scoreDistance,
                        sections = sectionsPassed,
                        coins = scoreCoins,
                        address = States.Instance.CurrentAvatarState.agentAddress.ToString(),

                        //statistics
                        lives = livesTaken,
                        speedBoosterUsed = speedBooster
                    }
                };
                Debug.LogError($"{incrementAntiCheat},{scoreDistance},{sectionsPassed},{scoreCoins},{livesTaken},{speedBooster}");
                PlayFabClientAPI.ExecuteCloudScript(request, OnValidateSuccess, OnPlayFabError);


                yield return new WaitForSeconds(3f);
                centerText.gameObject.SetActive(false);
                UIElement.SetActive(false);
                FinalResult.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = scoreDistance + "<size=60%>M</size>";
                FinalResult.transform.GetChild(3).GetComponent<TextMeshProUGUI>().text = (scoreCoins).ToString();
                FinalResult.SetActive(true);
            }
            else
            {
                foreach (Transform item in enemiesArray)
                    item.gameObject.SetActive(false);

                //reduce speed
                LevelSpeed = Mathf.Clamp(LevelSpeed - 1f, 1, 4);
                SpeedChange();

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
            }
        }

        void OnValidateSuccess(ExecuteCloudScriptResult result)
        {
            if (result.FunctionResult.ToString() == "Failed")
            {
                NotificationSystem.Push(Nekoyume.Model.Mail.MailType.System, "PandoraBox: sending score <color=red>Failed!</color>", NotificationCell.NotificationType.Alert);
            }
            else
            {
                PandoraBoxMaster.PlayFabInventory.VirtualCurrency["PC"] += scoreCoins; //just UI update instead of request new call
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
                scoreText.text = (scoreDistance - incrementAntiCheat) + "<size=25> M</size>";
            }
            else if (typeScore == 1)
            {
                scoreCoins += value;
                coinText.text = "x " + (scoreCoins - incrementAntiCheat);
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
            //speed current pooled objects
            foreach (GameObject item in GetComponent<PickupPooler>().pooledObjects)
                item.GetComponent<RunnerUnitMovements>().TimeScale = LevelSpeed;
            foreach (Transform item in enemiesArray)
                item.GetComponent<RunnerUnitMovements>().TimeScale = LevelSpeed;
            foreach (Transform item in CoinsArray)
                item.GetComponent<RunnerUnitMovements>().TimeScale = LevelSpeed;
        }

        IEnumerator PrepareLife()
        {
            yield return new WaitForSeconds(1.5f);
            int lifeAnimation = 0;
            for (int i = 0; i < 3; i++)
            {
                lifeAnimation++;
                SetLifeUI(lifeAnimation);
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

        public void SetLifeUI(int lives)
        {
            lives = Mathf.Clamp(lives, 0, 3);
            foreach (Transform item in healthHolder)
                item.gameObject.SetActive(false);

            for (int i = 0; i < lives; i++)
                healthHolder.GetChild(i).gameObject.SetActive(true);
        }

        public void ExitToMenu()
        {
            Time.timeScale = 1;
            Game.Event.OnRoomEnter.Invoke(false);
            Find<VersionSystem>().Show();
            Close();
        }
    }
}
