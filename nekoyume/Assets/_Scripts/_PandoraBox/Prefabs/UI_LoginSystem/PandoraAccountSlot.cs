using Nekoyume.PandoraBox;
using Nekoyume.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.PandoraBox
{
    [Serializable]
    public class PandoraAccountSlot : MonoBehaviour
    {
        //public int _slotIndex;
        [SerializeField] GameObject selectionVFX;
        [SerializeField] GameObject particleVFX;
        [SerializeField] Image slotPicture;
        [SerializeField] Button slotButton;
        [SerializeField] TextMeshProUGUI titleText;
        [SerializeField] TextMeshProUGUI numberText;

        public LoginSlotSettings SlotSettings;

        //public string PandoraPassword { private set; get; }
        //public string AddressPassword { private set; get; }


        //public string Email { private set; get; }
        //public string Username { private set; get; }
        //public string DisplayText { private set; get; }
        //public bool IsRemember { private set; get; }
        //public bool IsAutoLogin { private set; get; }

        public void LoadData(int index)
        {
            var settings = new LoginSlotsSettings();
            settings.LoadSettings();
            SlotSettings = settings.GetSettings(index);
            SlotSettings.Password = !string.IsNullOrEmpty(SlotSettings.Password)
                ? PandoraUtil.SimpleDecrypt(SlotSettings.Password)
                : "";
            SlotSettings.AddressPassword = !string.IsNullOrEmpty(SlotSettings.Password)
                ? PandoraUtil.SimpleDecrypt(SlotSettings.AddressPassword)
                : "";

            titleText.text = SlotSettings.DisplayText;
            numberText.text = "#" + (SlotSettings.Index + 1);

            slotButton.onClick.RemoveAllListeners();
            slotButton.onClick.AddListener(() =>
                Widget.Find<LoginSystem>().pandoraLogin.SetAccountIndex(SlotSettings.Index));
        }

        public void SaveData()
        {
            var settings = new LoginSlotsSettings();
            settings.LoadSettings();
            var temp = new LoginSlotSettings
            {
                Index = SlotSettings.Index,
                Email = SlotSettings.Email,
                Username = SlotSettings.Username,
                DisplayText = SlotSettings.DisplayText,
                IsRemember = SlotSettings.IsRemember,
                IsAutoLogin = SlotSettings.IsAutoLogin,
                Password = SlotSettings.IsRemember ? PandoraUtil.SimpleEncrypt(SlotSettings.Password) : "",
                AddressPassword = SlotSettings.IsAutoLogin
                    ? PandoraUtil.SimpleEncrypt(SlotSettings.AddressPassword)
                    : "",
            };
            settings.SaveSettings(temp);
            PlayerPrefs.SetInt("_PandoraBox_Account_lastLoginIndex", SlotSettings.Index);
        }

        public void CheckSelect()
        {
            Color pictureColor = SlotSettings.Index == Pandora.SelectedLoginAccountIndex
                ? Color.white
                : new Color(100f / 255f, 100f / 255f, 100f / 255f);
            slotPicture.color = pictureColor;
            selectionVFX.SetActive(SlotSettings.Index == Pandora.SelectedLoginAccountIndex);
            particleVFX.SetActive(SlotSettings.Index == Pandora.SelectedLoginAccountIndex);
        }
    }
}