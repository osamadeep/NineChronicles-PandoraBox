using Nekoyume.Game.Controller;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume.PandoraBox
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
                RunnerLevelManager.instance.UpdateScore(50, 1);

                AudioController.instance.PlaySfx(AudioController.SfxCode.RewardItem);
                GameObject ob = PickupPooler.instance.GetpooledObject();
                ob.transform.SetParent(RunnerLevelManager.instance.transform);
                RectTransform playerRecttransform = Recttransform;
                ob.GetComponent<RectTransform>().anchoredPosition = playerRecttransform.anchoredPosition;
                ob.GetComponent<RectTransform>().anchorMax = playerRecttransform.anchorMax;
                ob.GetComponent<RectTransform>().anchorMin = playerRecttransform.anchorMin;
                ob.transform.localScale = new Vector3(1, 1, 1);
                ob.SetActive(true);
                gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (Recttransform.anchoredPosition.x <= -1500)
                gameObject.SetActive(false);
        }
    }
}
