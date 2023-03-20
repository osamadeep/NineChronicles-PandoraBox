using TMPro;
using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Libplanet;
using Libplanet.Crypto;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using UniRx;
using TimeSpan = System.TimeSpan;
using Nekoyume.UI.Scroller;
using Nekoyume.PandoraBox;
using Nekoyume.Model.Item;
using Nekoyume.State;
using PlayFab;
using PlayFab.ClientModels;
using Cysharp.Threading.Tasks;
using Libplanet.Assets;

namespace Nekoyume.UI
{
    public class PandoraTransfer : PopupWidget
    {
        [SerializeField] private TMP_InputField amount;
        [SerializeField] private TMP_InputField address;
        [SerializeField] private TMP_InputField memo;
        [SerializeField] private Button submitButton;
        [SerializeField] private Toggle pandoraTip;


        protected override void Awake()
        {
            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            submitButton.onClick.AddListener(() => { Transfer(); });
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
            base.Awake();
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            amount.text = memo.text = address.text = "";
            base.Show(ignoreShowAnimation);
        }

        async void Transfer()
        {
            // Check if the 9c address is valid
            if (!IsValidAddress(address.text))
            {
                // Display a system notification
                PandoraUtil.ShowSystemNotification("Please enter a valid 9c address.",
                    NotificationCell.NotificationType.Alert);
                return;
            }

            float value = float.Parse(amount.text);
            if (value <= 0 || string.IsNullOrEmpty(amount.text))
            {
                // Display a system notification
                PandoraUtil.ShowSystemNotification("Please enter more than 0 amount.",
                    NotificationCell.NotificationType.Alert);
                return;
            }

            int majorPart = (int)(value);
            int minerPart = Mathf.FloorToInt((value * 100f) % 100f);
            ;

            var balance = await GetAgentBalance(States.Instance.CurrentAvatarState.agentAddress);

            if ((balance.MajorUnit * 100) + balance.MinorUnit < ((majorPart * 100) + minerPart))
            {
                // Display a system notification
                PandoraUtil.ShowSystemNotification("No enough balance!",
                    NotificationCell.NotificationType.Alert);
                return;
            }

            Premium.PANDORA_TransferAsset("NCG", majorPart, minerPart, address.text, memo.text);
            if (pandoraTip.isOn)
                Premium.PANDORA_TransferAsset("NCG", 0, 50, PandoraMaster.PandoraAddress, "Tip from Transfer tool");
            amount.text = memo.text = address.text = "";
            PandoraUtil.ShowSystemNotification("Transfer sent Successfully!",
                NotificationCell.NotificationType.Information);
        }

        // Helper method to validate 9c address
        bool IsValidAddress(string address)
        {
            try
            {
                Address nineAddress = new Address(address);
                return true;
            }
            catch
            {
                return false;
            }
        }

        async UniTask<FungibleAssetValue> GetAgentBalance(Address agentAddress)
        {
            var ncgCurrency =
                Libplanet.Assets.Currency.Legacy("NCG", 2, new Address("47d082a115c63e7b58b1532d20e631538eafadde"));
            var crystalCurrency = Libplanet.Assets.Currency.Legacy("CRYSTAL", 18, minters: null);
            var goldbalance = await Game.Game.instance.Agent.GetBalanceAsync(agentAddress, ncgCurrency);
            return goldbalance;
        }
    }
}