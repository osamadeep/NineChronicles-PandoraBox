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
    public class UtilitieSlot : MonoBehaviour
    {
        //core info
        public string ItemID;
        public int ItemPrice;
        public string CurrencySTR;
        public bool IsLocked;
        [SerializeField] Sprite itemIcon;

        //visual info
        [SerializeField] Sprite[] itemCurrencies;
        [SerializeField] Image itemImg;
        [SerializeField] Image currencyImg;
        [SerializeField] Button selectBtn;

        //out visual changes
        public TextMeshProUGUI itemPrice;

        private void Awake()
        {
            SetItemData();
            selectBtn.onClick.AddListener(() => SelectBoosterItem(ItemID));
        }

        public void SetItemData()
        {
            itemImg.sprite = itemIcon;
            itemPrice.text = ItemPrice.ToString();
            currencyImg.sprite = CurrencySTR == "PC"? itemCurrencies[0]: currencyImg.sprite = itemCurrencies[1];
            CheckAvailability();
        }

        public void SelectBoosterItem(string buyID)
        {
            //check client-side if cost is enough
            if (PandoraMaster.PlayFabInventory.VirtualCurrency[CurrencySTR] < ItemPrice)
                return;

            Game.Game.instance.Runner.SelectedUtilitie = this;
            Game.Game.instance.Runner.RunnerUI.FeaturesUICooldown = 0;
        }

        public void CheckAvailability()
        {
            if (PandoraMaster.PlayFabInventory.VirtualCurrency[CurrencySTR] < ItemPrice || IsLocked)
            {
                selectBtn.interactable = false;
                itemImg.color = new Color(1, 1, 1, 0.5f);
            }
            else
            {
                selectBtn.interactable = true;
                itemImg.color = new Color(1, 1, 1, 1f);
            }
        }
    }
}
