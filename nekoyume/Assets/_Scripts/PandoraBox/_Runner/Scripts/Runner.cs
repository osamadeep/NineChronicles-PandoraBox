using Nekoyume.Game.Controller;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    public class Runner : Widget
    {
        public TextMeshProUGUI centerText = null;
        public TextMeshProUGUI scoreText;
        public TextMeshProUGUI coinText;
        public TextMeshProUGUI startCounterText;
        public TextMeshProUGUI DieCounterText;
        public GameObject FinalResult;
        public GameObject UIElement;
        public GameObject UIBoosters;
        public GameObject UIBoostersDie;

        public override void Show(bool ignoreShowAnimation = false)
        {
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
            }
            else
            {
                Time.timeScale = 1;
            }
        }

        public void ExitToMenu()
        {
            Time.timeScale = 1;
            Game.Event.OnRoomEnter.Invoke(false);
            Find<VersionSystem>().Show();

            Game.Game.instance.Runner.runnerLevel.gameObject.SetActive(false);
            Game.Game.instance.Runner.player.gameObject.SetActive(false);
            Close();
            //gameObject.SetActive(false);
        }

        public void RestartGame()
        {
            Game.Game.instance.Runner.OnRunnerStart();
        }

        public IEnumerator ShowBoosterSelection()
        {
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
            UIBoosters.SetActive(false);
        }

    }
}
