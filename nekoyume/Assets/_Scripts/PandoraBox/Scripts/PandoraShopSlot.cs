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
using Cysharp.Threading.Tasks;
using Nekoyume.Game.Controller;

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
            {
                //exception for item price decided by database
                if (ItemID == "PandoraMembership30")
                    ItemPrice = PandoraMaster.PanDatabase.PremiumPrice;
                else if (ItemID == "PandoraMembership90")
                    ItemPrice = PandoraMaster.PanDatabase.PremiumPrice * 3;
                else if (ItemID == "PandoraMembership180")
                    ItemPrice = PandoraMaster.PanDatabase.PremiumPrice * 6;
            }
            itemPrice.text = "x " + ItemPrice + " BUY";
            buyBtn.onClick.AddListener(() => BuyItem(ItemID));
        }

        public void BuyItem(string buyID)
        {
            AudioController.PlayClick();
            //check client-side if cost is enough
            if (CurrencySTR == "NCG" && States.Instance.GoldBalanceState.Gold.MajorUnit < ItemPrice)
            {
                OneLineSystem.Push(Nekoyume.Model.Mail.MailType.System,"<color=green>Pandora Box</color>: no enough gold!", NotificationCell.NotificationType.Alert);
                return;
            }
            else if (CurrencySTR != "NCG" && PandoraMaster.PlayFabInventory.VirtualCurrency[CurrencySTR] < ItemPrice)
            {
                NotificationSystem.Push(Nekoyume.Model.Mail.MailType.System, "PandoraBox: No Enough Currency!" , NotificationCell.NotificationType.Alert);
                return;
            }

            AudioController.PlayClick();
            switch (buyID)
            {
                case "PandoraMembership30":
                    BuyMembership(30);
                    break;
                case "PandoraMembership90":
                    BuyMembership(99);
                    break;
                case "PandoraMembership180":
                    BuyMembership(216);
                    break;
                case "PG500":
                    BuyPandoraGems(500);
                    break;
                case "PG1050":
                    BuyPandoraGems(1050);
                    break;
                case "PG3300":
                    BuyPandoraGems(3300);
                    break;
            }
        }

        void BuyPandoraGems(int gems)
        {
            string content = $"Are you sure to spend <color=#FFCF2A><b>{ItemPrice}</b> {CurrencySTR}</color> to get <color=#76F3FE><b>{gems}</b></color> Pandora Gems?";
            Widget.Find<TwoButtonSystem>().Show(content, "Yes", "No",
            (() =>
            {
                Widget.Find<PandoraShopPopup>().WaitingImage.SetActive(true);
                Premium.BuyGems(ItemPrice, gems);
            }),
            () => { Widget.Find<PandoraShopPopup>().WaitingImage.SetActive(false); }
            );
        }

        void BuyMembership(int days)
        {
            string content = $"Are you sure to spend <color=#00C3FF><b>{ItemPrice}</b> {CurrencySTR}</color> to get <color=#EF3DFF><b>{(int)(days * 7300)}</b></color> Blocks (~{days} days)?";
            Widget.Find<TwoButtonSystem>().Show(content, "Yes", "No",
            (() =>
            {
                Widget.Find<PandoraShopPopup>().WaitingImage.SetActive(true);

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
                        AudioController.instance.PlaySfx(AudioController.SfxCode.BuyItem);
                        PandoraMaster.PlayFabInventory.VirtualCurrency[CurrencySTR] -= ItemPrice; //just UI update instead of request new call
                        NotificationSystem.Push(Nekoyume.Model.Mail.MailType.System, "PandoraBox: Buy <color=green>Success!</color>", NotificationCell.NotificationType.Information);
                        Widget.Find<PandoraShopPopup>().WaitingImage.SetActive(false);
                        Widget.Find<PandoraShopPopup>().Close();

                        //update database
                        Premium.UpdateDatabase(10).Forget();
                    }
                    else
                    {
                        NotificationSystem.Push(Nekoyume.Model.Mail.MailType.System, "PandoraBox: Buy <color=red>Failed!</color>", NotificationCell.NotificationType.Alert);
                    }
                },
                failed =>
                {
                    foreach (Transform item in transform.parent)
                        item.GetComponent<PandoraShopSlot>().buyBtn.interactable = true;
                    Debug.LogError("Score Not Sent!, " + failed.GenerateErrorReport());
                });
            })
            ,() => { Widget.Find<PandoraShopPopup>().WaitingImage.SetActive(false); }
            );
        }
    }
}
