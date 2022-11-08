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
