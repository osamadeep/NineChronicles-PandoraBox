using Nekoyume.PandoraBox;
using Nekoyume.UI;
using Nekoyume.UI.Scroller;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume
{
    public class BoosterSlot : MonoBehaviour
    {
        public string ItemID;
        public Sprite ItemImg;
        public string ItemTitle;
        public int ItemPrice;
        public string CurrencySTR;

        public GameObject CheckObj;
        [SerializeField] Image itemImg;
        [SerializeField] TextMeshProUGUI itemTitle;
        public TextMeshProUGUI itemPrice;
        [SerializeField] Button selectBtn;

        private void Awake()
        {
            SetItemData();
            selectBtn.onClick.AddListener(() => SelectBoosterItem(ItemID));
        }

        public void SetItemData()
        {
            itemImg.sprite = ItemImg;
            itemTitle.text = ItemTitle;
            itemPrice.text = "x " + ItemPrice;
            CheckObj.SetActive(false);
            selectBtn.interactable = PandoraMaster.PlayFabInventory.VirtualCurrency[CurrencySTR] >= ItemPrice;

            //disable PG boosters
            if (CurrencySTR == "PG")
                selectBtn.interactable = false;
        }

        public void SelectBoosterItem(string buyID)
        {
            //check client-side if cost is enough
            if (PandoraMaster.PlayFabInventory.VirtualCurrency[CurrencySTR] < ItemPrice)
            {
                NotificationSystem.Push(Nekoyume.Model.Mail.MailType.System, "PandoraBox: No Enough Currency!", NotificationCell.NotificationType.Alert);
                return;
            }

            if (CheckObj.activeInHierarchy)
            {
                CheckObj.SetActive(false);
                //Game.Game.instance.Runner.SelectedBooster = null; // <-- use this for production
                PandoraRunner.instance.SelectedBooster = null;
            }
            else
            {
                foreach (Transform item in transform.parent)
                {
                    item.GetComponent<BoosterSlot>().CheckObj.SetActive(false);
                }
                CheckObj.SetActive(true);

                //Game.Game.instance.Runner.SelectedBooster = this; // <-- use this for production
                PandoraRunner.instance.SelectedBooster = this;
            }
        }
    }
}
