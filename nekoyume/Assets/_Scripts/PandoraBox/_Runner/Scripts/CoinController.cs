using Nekoyume.Game.Controller;
using Nekoyume.PandoraBox;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume.UI
{
    public class CoinController : MonoBehaviour
    {
        RectTransform Recttransform;

        private void Start()
        {
            Recttransform = GetComponent<RectTransform>();
        }
        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                if (Widget.Find<IntroScreen>().IsActive())
                    IntroRunnerLevelManager.instance.UpdateScore(50, 1);
                else
                    Game.Game.instance.Runner.UpdateScore(50, 1);

                AudioController.instance.PlaySfx(AudioController.SfxCode.RewardItem);
                GameObject ob = PickupPooler.instance.GetpooledObject();

                if (Widget.Find<IntroScreen>().IsActive())
                    ob.transform.SetParent(IntroRunnerLevelManager.instance.transform);
                else
                    ob.transform.SetParent(Game.Game.instance.Runner.transform.Find("PooledObj"));
                RectTransform playerRecttransform = Recttransform;
                ob.GetComponent<RectTransform>().anchoredPosition = playerRecttransform.anchoredPosition;
                ob.GetComponent<RectTransform>().anchorMax = playerRecttransform.anchorMax;
                ob.GetComponent<RectTransform>().anchorMin = playerRecttransform.anchorMin;
                ob.transform.localScale = new Vector3(1, 1, 1);
                ob.GetComponent<RunnerUnitMovements>().TimeScale = GetComponent<RunnerUnitMovements>().TimeScale;
                ob.SetActive(true);
                gameObject.SetActive(false);
            }
        }
    }
}
