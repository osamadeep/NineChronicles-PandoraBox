using Nekoyume.Game.Controller;
using Nekoyume.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Nekoyume.PandoraBox
{
    public class Runner : Widget
    {
        public TextMeshProUGUI playerName;
        public TextMeshProUGUI pcBalance;
        public TextMeshProUGUI pgBalance;
        public TextMeshProUGUI centerText = null;
        public TextMeshProUGUI scoreText;
        public TextMeshProUGUI coinText;
        public TextMeshProUGUI startCounterText;
        public TextMeshProUGUI DieCounterText;
        public GameObject FinalResult;
        public GameObject UIBalance;
        public GameObject UIElement;
        public GameObject StartFeaturesHolder;
        public GameObject EndFeaturesHolder;

        public int FeaturesUICooldown = 0;

        public override void Show(bool ignoreShowAnimation = false)
        {
            UIBalance.SetActive(false);
            UIElement.SetActive(false);
            StartFeaturesHolder.SetActive(false);
            EndFeaturesHolder.SetActive(false);
            base.Show(ignoreShowAnimation);
        }

        protected override void OnCompleteOfShowAnimationInternal()
        {
            base.OnCompleteOfShowAnimationInternal();
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);
        }

        public void PauseGame(bool isPause)
        {
            if (isPause)
            {
                Time.timeScale = 0;
                Game.Game.instance.Runner.currentRunnerState = PandoraRunner.RunnerState.Pause;
                Game.Game.instance.Runner.player.runner = Game.Game.instance.Runner.currentRunnerState;
            }
            else
            {
                Time.timeScale = 1;
                Game.Game.instance.Runner.currentRunnerState = PandoraRunner.RunnerState.Playing;
                Game.Game.instance.Runner.player.runner = Game.Game.instance.Runner.currentRunnerState;
            }
        }

        public void ExitToTown()
        {
            Time.timeScale = 1;
            Find<VersionSystem>().Show();
            Game.Game.instance.Runner.runnerLevel.gameObject.SetActive(false);
            //Game.Game.instance.Runner.player.gameObject.SetActive(false);

            //prepare char for town
            //Game.Game.instance.Runner.GetComponent<AudioSource>().enabled = false;
            Game.Game.instance.Runner.currentRunnerState = PandoraRunner.RunnerState.Town;
            Game.Game.instance.Runner.player.transform.position = new Vector3(-5000, -5000);
            Game.Game.instance.Runner.player.ChangeState("Idle");
            //Game.Game.instance.Runner.player.CheckIfOnGround();
            Find<RunnerTown>().Show(true);
            Close(true);
            //gameObject.SetActive(false);
        }

        public void RestartGame()
        {
            StopAllCoroutines();
            Game.Game.instance.Runner.OnRunnerStart();
        }

        public IEnumerator ShowStartBooster()
        {
            playerName.text = Premium.PandoraProfile.Profile.DisplayName;
            pcBalance.text = Premium.PandoraProfile.Currencies["PC"].ToString();
            pgBalance.text = Premium.PandoraProfile.Currencies["PG"].ToString();
            startCounterText.text = "";
            StartFeaturesHolder.SetActive(true);
            UIBalance.SetActive(true);
            Transform boosters = StartFeaturesHolder.transform.Find("Boosters");
            //for (int i = 0; i < 2; i++) // it should be UIBoosters.childCount when all boost is available
            //{
            //    boosters.GetChild(i).GetComponent<UtilitieSlot>().SetItemData();
            //}
            foreach (Transform Utilitie in boosters)
                Utilitie.GetComponent<UtilitieSlot>().SetItemData();

            FeaturesUICooldown = 50;
            while (FeaturesUICooldown > 0)
            {
                startCounterText.text = (Mathf.Round((float)(FeaturesUICooldown--) / 10f)).ToString();
                yield return new WaitForSeconds(0.1f);
            }

            StartFeaturesHolder.SetActive(false);
            UIBalance.SetActive(false);
        }

        public void SkipCounter()
        {
            FeaturesUICooldown = 0;
        }

        public IEnumerator ShowEndBooster(int livesBought, int gameSeed)
        {
            pcBalance.text = Premium.PandoraProfile.Currencies["PC"].ToString();
            pgBalance.text = Premium.PandoraProfile.Currencies["PG"].ToString();
            EndFeaturesHolder.SetActive(true);
            UIBalance.SetActive(true);
            Transform boosters = EndFeaturesHolder.transform.Find("Boosters");
            for (int i = 0; i < 1; i++) // it should be UIBoosters.childCount when all boost is available
            {
                boosters.GetChild(i).GetComponent<UtilitieSlot>().SetItemData();
            }

            int newPrice = boosters.GetChild(0).GetComponent<UtilitieSlot>().ItemPrice
                           + (boosters.GetChild(0).GetComponent<UtilitieSlot>().ItemPrice * (livesBought - gameSeed));
            boosters.GetChild(0).GetComponent<UtilitieSlot>().itemPrice.text = "x " + (newPrice);
            ;

            FeaturesUICooldown = 50;
            while (FeaturesUICooldown > 0)
            {
                DieCounterText.text = (Mathf.Round((float)(FeaturesUICooldown--) / 10f)).ToString();
                if (FeaturesUICooldown % 10 == 0)
                    AudioController.instance.PlaySfx(AudioController.SfxCode.OptionNormal);
                yield return new WaitForSeconds(0.1f);
            }

            EndFeaturesHolder.SetActive(false);
            UIBalance.SetActive(false);
        }
    }
}