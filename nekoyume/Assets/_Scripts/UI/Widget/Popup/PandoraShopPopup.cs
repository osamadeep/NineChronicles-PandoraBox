using System.Collections.Generic;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using TMPro;
using UnityEngine;

using Nekoyume.PandoraBox;
using Nekoyume.Game.Controller;
using UnityEngine.UIElements;
using Nekoyume.UI.Scroller;

namespace Nekoyume.UI
{
    using UnityEngine.UI;

    public class PandoraShopPopup : PopupWidget
    {
        public Transform TabHolder;
        public GameObject WaitingImage;
        [SerializeField] Transform tabContentHolder;


        [Header("Crystal Tab")]
        [SerializeField] TextMeshProUGUI EstimatedValueText;
        [SerializeField] TMP_InputField NcgInput;
        [SerializeField] TextMeshProUGUI PremiumBounsText;
        [SerializeField] TMP_InputField CrystalInput;
        [SerializeField] TextMeshProUGUI BonusGemsText;
        [SerializeField] UnityEngine.UI.Slider crystalPriceSlider;
        [SerializeField] UnityEngine.UI.Slider BounsSlider;
        [SerializeField] List<TextMeshProUGUI> BounsSliderNCGTexts;
        [SerializeField] List<TextMeshProUGUI> BounsSliderRateTexts;
        public UnityEngine.UI.Button BuyCrystalBtn;
        int CrystalPerNCG = 0;
        float totalCrystal = 0;
        int ncgToSpend = 10;
        [Space(50)]


        [SerializeField] TextMeshProUGUI GemsText;
        [SerializeField] TextMeshProUGUI CoinsText;



        protected override void Awake()
        {
            base.Awake();
        }

        public void ContactSupport()
        {
            Application.OpenURL("https://discordapp.com/users/1015187437888225310");
        }

        public void SwitchTab(int currentTab)
        {
            foreach (Transform item in TabHolder)
            {
                item.GetComponent<Image>().color = new Color(1, 1, 1, 0.5f);
                item.GetChild(0).GetComponent<Image>().color = new Color(1, 1, 1, 0.5f);
            }
            foreach (Transform item in tabContentHolder)
                item.gameObject.SetActive(false);

            TabHolder.GetChild(currentTab).GetComponent<Image>().color = new Color(1, 1, 1, 1);
            TabHolder.GetChild(currentTab).GetChild(0).GetComponent<Image>().color = new Color(1, 1, 1, 1);
            tabContentHolder.GetChild(currentTab).gameObject.SetActive(true);
            AudioController.PlayClick();
        }

        public void Show()
        {
            base.Show();
            UpdateCurrency();

            WaitingImage.gameObject.SetActive(false);

            //Crystal tab
            BuyCrystalBtn.interactable = true;
            BuyCrystalBtn.GetComponentInChildren<TextMeshProUGUI>().text = "BUY";

            StartCoroutine(ChangeCrystalValue());
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);
        }

        public void UpdateCurrency()
        {
            GemsText.text = PandoraMaster.PlayFabInventory.VirtualCurrency["PG"].ToString();
            CoinsText.text = PandoraMaster.PlayFabInventory.VirtualCurrency["PC"].ToString();
        }

        public void BuyCrystal()
        {
            AudioController.PlayClick();
            if (States.Instance.GoldBalanceState.Gold.MajorUnit < ncgToSpend)
            {
                OneLineSystem.Push(MailType.System,
                "<color=green>Pandora Box</color>: no enough gold!"
                , NotificationCell.NotificationType.Alert);
                return;
            }

            float totalchoosenCrystal = ncgToSpend * bounsFactor * CrystalPerNCG;
            string content = $"Are you sure to spend <b>{ncgToSpend}</b> <color=#FFCF2A>NCG</color> to get <b>{(int)totalchoosenCrystal}</b> <color=#EF3DFF>CRYSTALS</color> ?";
            Find<TwoButtonSystem>().Show(content, "Yes","No",
            (() => {
                Premium.ACCOUNT_BuyCrystals(ncgToSpend, CrystalPerNCG, bounsFactor);
                StopCoroutine(ChangeCrystalValue());
                }));
        }

        float bounsFactor = 1;
        public void ChangeCrystal()
        {
            if (!string.IsNullOrEmpty(NcgInput.text))
            {
                ncgToSpend = Mathf.Clamp(int.Parse(NcgInput.text),10,1000);

                //slider
                if (ncgToSpend >= 0 && ncgToSpend <= 50)
                    BounsSlider.value = (ncgToSpend - 0) * 0.25f / 50;
                else if (ncgToSpend > 50 && ncgToSpend <= 100)
                    BounsSlider.value = 0.25f + (ncgToSpend - 50) * 0.25f / 50;
                else if (ncgToSpend > 100 && ncgToSpend <= 300)
                    BounsSlider.value = 0.5f + (ncgToSpend - 100) * 0.25f / 200;
                else if (ncgToSpend > 300 && ncgToSpend <= 500)
                    BounsSlider.value = 0.75f + (ncgToSpend - 300) * 0.25f / 200;
                else if (ncgToSpend > 500)
                    BounsSlider.value = 1;

                //visual slider
                foreach (var item in BounsSliderNCGTexts)
                    item.color = new Color(item.color.r, item.color.g, item.color.b, 0.3f);
                foreach (var item in BounsSliderRateTexts)
                    item.color = new Color(item.color.r, item.color.g, item.color.b, 0.3f);


                if (ncgToSpend >= 10 && ncgToSpend <= 49)
                {
                    BounsSliderNCGTexts[0].color = new Color(BounsSliderNCGTexts[0].color.r, BounsSliderNCGTexts[0].color.g, BounsSliderNCGTexts[0].color.b, 1);
                    BounsSliderRateTexts[0].color = new Color(BounsSliderNCGTexts[0].color.r, BounsSliderNCGTexts[0].color.g, BounsSliderNCGTexts[0].color.b, 1);
                }
                else if (ncgToSpend >= 50 && ncgToSpend <= 99)
                {
                    BounsSliderNCGTexts[1].color = new Color(BounsSliderNCGTexts[1].color.r, BounsSliderNCGTexts[1].color.g, BounsSliderNCGTexts[1].color.b, 1);
                    BounsSliderRateTexts[1].color = new Color(BounsSliderNCGTexts[1].color.r, BounsSliderNCGTexts[1].color.g, BounsSliderNCGTexts[1].color.b, 1);
                    bounsFactor = 1.05f;
                }
                else if (ncgToSpend >= 100 && ncgToSpend <= 299)
                {
                    BounsSliderNCGTexts[2].color = new Color(BounsSliderNCGTexts[2].color.r, BounsSliderNCGTexts[2].color.g, BounsSliderNCGTexts[2].color.b, 1);
                    BounsSliderRateTexts[2].color = new Color(BounsSliderNCGTexts[2].color.r, BounsSliderNCGTexts[2].color.g, BounsSliderNCGTexts[2].color.b, 1);
                    bounsFactor = 1.1f;
                }
                else if (ncgToSpend >= 300 && ncgToSpend <= 499)
                {
                    BounsSliderNCGTexts[3].color = new Color(BounsSliderNCGTexts[3].color.r, BounsSliderNCGTexts[3].color.g, BounsSliderNCGTexts[3].color.b, 1);
                    BounsSliderRateTexts[3].color = new Color(BounsSliderNCGTexts[3].color.r, BounsSliderNCGTexts[3].color.g, BounsSliderNCGTexts[3].color.b, 1);
                    bounsFactor = 1.15f;
                }
                else if (ncgToSpend >= 500)
                {
                    BounsSliderNCGTexts[4].color = new Color(BounsSliderNCGTexts[4].color.r, BounsSliderNCGTexts[4].color.g, BounsSliderNCGTexts[4].color.b, 1);
                    BounsSliderRateTexts[4].color = new Color(BounsSliderNCGTexts[4].color.r, BounsSliderNCGTexts[4].color.g, BounsSliderNCGTexts[4].color.b, 1);
                    bounsFactor = 1.2f;
                }

                totalCrystal = (CrystalPerNCG * bounsFactor) * ncgToSpend;
                CrystalInput.text = PandoraUtil.ToLongNumberNotation((System.Numerics.BigInteger)totalCrystal);
                BonusGemsText.text = (ncgToSpend / 10).ToString();
                PremiumBounsText.text = $"Your bouns : <color=#EF3DFF>+" +
                    $"{PandoraUtil.ToLongNumberNotation((System.Numerics.BigInteger)(totalCrystal - (CrystalPerNCG* ncgToSpend)))}</color>";
            }
        }

        System.Collections.IEnumerator ChangeCrystalValue()
        {
            CrystalPerNCG = 0;
            totalCrystal = 0;
            ncgToSpend = 10;
            NcgInput.text = ncgToSpend.ToString();

            while (true)
            {
                int sliderValue = 100;
                CrystalPerNCG = Random.Range(PandoraMaster.PanDatabase.Crystal - 500, PandoraMaster.PanDatabase.Crystal);
                EstimatedValueText.text = $"Estimated Price:   <b>1</b> <color=#FFCF2A>NCG</color> = <b>{CrystalPerNCG}</b> <color=#EF3DFF>CRYSTALS</color>";
                totalCrystal = ncgToSpend * bounsFactor;
                CrystalInput.text = PandoraUtil.ToLongNumberNotation((System.Numerics.BigInteger)totalCrystal);
                while (--sliderValue > 0)
                {
                    yield return new WaitForSeconds(0.5f);
                    crystalPriceSlider.value = sliderValue;
                } 
            }

           
        }
    }
}
