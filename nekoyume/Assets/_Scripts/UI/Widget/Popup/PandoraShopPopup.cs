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
        public Transform TabHolder;
        [SerializeField] Transform tabContentHolder;

        [Header("Crystal Tab")]
        [SerializeField] TextMeshProUGUI EstimatedValueText;
        [SerializeField] TMP_InputField NcgInput;
        [SerializeField] TextMeshProUGUI PremiumBounsText;
        [SerializeField] TMP_InputField CrystalInput;
        [SerializeField] Slider crystalPriceSlider;
        public Button BuyCrystalBtn;
        int CrystalPerNCG = 0;
        float totalCrystal = 0;
        int currentNcg = 10;
        [Space(50)]


        [SerializeField] TextMeshProUGUI GemsText;
        [SerializeField] TextMeshProUGUI CoinsText;



        protected override void Awake()
        {
            base.Awake();
        }

        public void SwitchTab(int currentTab)
        {
            foreach (Transform item in TabHolder)
                item.GetComponentInChildren<Image>().color = new Color(1, 1, 1, 0.5f);
            foreach (Transform item in tabContentHolder)
                item.gameObject.SetActive(false);

            TabHolder.GetChild(currentTab).GetComponentInChildren<Image>().color = new Color(1, 1, 1, 1);
            tabContentHolder.GetChild(currentTab).gameObject.SetActive(true);
        }

        public void Show()
        {
            UpdateCurrency();
            base.Show();

            //Crystal tab
            BuyCrystalBtn.interactable = true;
            BuyCrystalBtn.GetComponentInChildren<TextMeshProUGUI>().text = "BUY";
            PremiumBounsText.text = $"PREMIUM Get {PandoraMaster.PanDatabase.CrystalPremiumBouns * 100}%";

            StartCoroutine(ChangeCrystalValue());
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);
        }

        public void UpdateCurrency()
        {
            GemsText.text = "x" + PandoraMaster.PlayFabInventory.VirtualCurrency["PG"];
            CoinsText.text = "x" + PandoraMaster.PlayFabInventory.VirtualCurrency["PC"];
        }

        public void BuyCrystal()
        {
            if (States.Instance.GoldBalanceState.Gold.MajorUnit < currentNcg)
            {
                OneLineSystem.Push(MailType.System,
                "<color=green>Pandora Box</color>: no enough gold!"
                , NotificationCell.NotificationType.Alert);
                return;
            }

            int choosenCrystalPrice = CrystalPerNCG;
            float totalchoosenCrystal = Premium.IsPremium ? (choosenCrystalPrice * PandoraMaster.PanDatabase.CrystalPremiumBouns) * currentNcg : choosenCrystalPrice * currentNcg;
            string content = $"Are you sure to spend <b>{currentNcg}</b> <color=#FFCF2A>NCG</color> to get <b>{(int)totalchoosenCrystal}</b> <color=#EF3DFF>CRYSTALS</color> ?";
            Find<TwoButtonSystem>().Show(content, "Yes","No",
            (() => {
                Premium.BuyCrystals(currentNcg, choosenCrystalPrice);
                StopCoroutine(ChangeCrystalValue());
                }));
        }


        public void ChangeCrystal()
        {
            if (!string.IsNullOrEmpty(NcgInput.text))
            {
                currentNcg = Mathf.Clamp(int.Parse(NcgInput.text),10,1000);
                totalCrystal = Premium.IsPremium ? (CrystalPerNCG * PandoraMaster.PanDatabase.CrystalPremiumBouns) * currentNcg : CrystalPerNCG * currentNcg;
                CrystalInput.text = ((int)totalCrystal).ToString();
            }
        }

        System.Collections.IEnumerator ChangeCrystalValue()
        {
            CrystalPerNCG = 0;
            totalCrystal = 0;
            currentNcg = 10;
            NcgInput.text = currentNcg.ToString();

            while (true)
            {
                int sliderValue = 100;
                CrystalPerNCG = Random.Range(PandoraMaster.PanDatabase.Crystal - 500, PandoraMaster.PanDatabase.Crystal);
                EstimatedValueText.text = $"Estimated Price:   <b>1</b> <color=#FFCF2A>NCG</color> = <b>{CrystalPerNCG}</b> <color=#EF3DFF>CRYSTALS</color>";
                totalCrystal = Premium.IsPremium ? (CrystalPerNCG * PandoraMaster.PanDatabase.CrystalPremiumBouns) * currentNcg : CrystalPerNCG * currentNcg;
                CrystalInput.text = ((int)totalCrystal).ToString();
                while (--sliderValue > 0)
                {
                    yield return new WaitForSeconds(0.5f);
                    crystalPriceSlider.value = sliderValue;
                } 
            }

           
        }
    }
}
