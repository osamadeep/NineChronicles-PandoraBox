using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Game.Character;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.UI.Scroller;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using Nekoyume.PandoraBox;

namespace Nekoyume.UI
{
    public class PandoraShopPopup : PopupWidget
    {
        [SerializeField] TextMeshProUGUI GemsText;
        [SerializeField] TextMeshProUGUI CoinsText;

        protected override void Awake()
        {
            base.Awake();
        }

        public void Show()
        {
            UpdateCurrency();
            base.Show();
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);
        }

        public void UpdateCurrency()
        {
            GemsText.text = "x" + PandoraBoxMaster.PlayFabInventory.VirtualCurrency["PG"];
            CoinsText.text = "x" + PandoraBoxMaster.PlayFabInventory.VirtualCurrency["PC"];
        }
    }
}
