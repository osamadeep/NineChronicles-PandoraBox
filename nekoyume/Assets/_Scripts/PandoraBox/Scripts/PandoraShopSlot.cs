using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Nekoyume.PandoraBox;
using Nekoyume.UI.Scroller;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.Json;
using Nekoyume.State;

namespace Nekoyume.UI
{
    public class PandoraShopSlot : MonoBehaviour
    {
        public string ItemID;
        public Sprite ItemImg;
        public string ItemTitle;
        public string ItemNameDesc;
        public int ItemPrice;
        public string CurrencySTR;

        [SerializeField] Image itemImg;
        [SerializeField] TextMeshProUGUI itemTitle;
        [SerializeField] TextMeshProUGUI itemNameDesc;
        [SerializeField] TextMeshProUGUI itemPrice;
        [SerializeField] Button buyBtn;

        private void Awake()
        {
            SetItemData();
        }

        public void SetItemData()
        {
            itemImg.sprite = ItemImg;
            itemTitle.text = ItemTitle;
            itemNameDesc.text = ItemNameDesc;
            itemPrice.text = "x " + ItemPrice + " BUY";
            buyBtn.onClick.AddListener(() => BuyItem(ItemID));
        }

        public void BuyItem(string buyID)
        {

            //check client-side if cost is enough
            if (PandoraMaster.PlayFabInventory.VirtualCurrency[CurrencySTR] < ItemPrice)
            {
                NotificationSystem.Push(Nekoyume.Model.Mail.MailType.System, "PandoraBox: No Enough Currency!" , NotificationCell.NotificationType.Alert);
                return;
            }

            foreach (Transform item in transform.parent)
            {
                item.GetComponent<PandoraShopSlot>().buyBtn.interactable = false;
            }

            switch (buyID)
            {
                case "PandoraMembership7":
                    BuyMembership(7);
                    break;
                case "PandoraMembership30":
                    BuyMembership(30);
                    break;
                case "PandoraMembership90":
                    BuyMembership(90);
                    break;
            }
        }

        void BuyMembership(int days)
        {
            PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest
            {
                FunctionName = "buyMembership",
                FunctionParameter = new
                {
                    blocks = days * 7300,
                    currentBlock = (int)Game.Game.instance.Agent.BlockIndex,
                    currency = CurrencySTR,
                    cost = ItemPrice
                }
            },
            success =>
            {
                if (success.FunctionResult.ToString() == "Success")
                {
                    //adding score success
                    PandoraMaster.PlayFabInventory.VirtualCurrency[CurrencySTR] -= ItemPrice; //just UI update instead of request new call
                    NotificationSystem.Push(Nekoyume.Model.Mail.MailType.System, "PandoraBox: Buy <color=green>Success!</color>", NotificationCell.NotificationType.Information);
                    Widget.Find<PandoraShopPopup>().UpdateCurrency();
                    Widget.Find<NineRunnerPopup>().UpdateCurrency();

                    //update database
                    Premium.GetDatabase(States.Instance.CurrentAvatarState.agentAddress);
                }
                else
                {
                    NotificationSystem.Push(Nekoyume.Model.Mail.MailType.System, "PandoraBox: Buy <color=red>Failed!</color>", NotificationCell.NotificationType.Alert);
                }

                foreach (Transform item in transform.parent)
                    item.GetComponent<PandoraShopSlot>().buyBtn.interactable = true;

            },
            failed =>
            {
                foreach (Transform item in transform.parent)
                    item.GetComponent<PandoraShopSlot>().buyBtn.interactable = true;
                Debug.LogError("Score Not Sent!, " + failed.GenerateErrorReport());
            });
        }
    }
}
