using Nekoyume.PandoraBox;
using Nekoyume.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.PandoraBox
{
    public class PandoraAccountSlot : MonoBehaviour
    {
        public int _slotIndex;
        [SerializeField] GameObject selectionVFX;
        [SerializeField] GameObject particleVFX;
        [SerializeField] GameObject premiumButton;
        [SerializeField] Image slotPicture;
        [SerializeField] Button slotButton;
        [SerializeField] TextMeshProUGUI titleText;

        public string Email { private set; get; }
        public string Password { private set; get; }
        public string Username { private set; get; }
        public string DisplayText { private set; get; }
        public bool IsRemember { private set; get; }
        public bool IsAutoLogin { private set; get; }
        public string AddressPassword { private set; get; }

        public void LoadData(int index)
        {
            _slotIndex = index;
            Email = PlayerPrefs.GetString("_PandoraBox_Account_LoginAccount_" + _slotIndex + "_Email", "").ToLower();
            Password = PandoraUtil.SimpleDecrypt(
                PlayerPrefs.GetString("_PandoraBox_Account_LoginAccount_" + _slotIndex + "_Password", ""));
            Username = PlayerPrefs.GetString("_PandoraBox_Account_LoginAccount_" + _slotIndex + "_Username", "");
            DisplayText = PlayerPrefs.GetString("_PandoraBox_Account_LoginAccount_" + _slotIndex + "_DisplayText",
                "Account");
            IsRemember =
                System.Convert.ToBoolean(
                    PlayerPrefs.GetInt("_PandoraBox_Account_LoginAccount_" + _slotIndex + "_IsRemember", 0));
            IsAutoLogin =
                System.Convert.ToBoolean(
                    PlayerPrefs.GetInt("_PandoraBox_Account_LoginAccount_" + _slotIndex + "_IsAutoLogin", 0));
            AddressPassword = PandoraUtil.SimpleDecrypt(
                PlayerPrefs.GetString("_PandoraBox_Account_LoginAccount_" + _slotIndex + "_AddressPassword", ""));
            premiumButton.SetActive(_slotIndex > 0);
            titleText.text = DisplayText;

            slotButton.onClick.RemoveAllListeners();
            slotButton.onClick.AddListener(() => Widget.Find<LoginSystem>().pandoraLogin.SetAccountIndex(_slotIndex));
        }

        public void SaveData(string email, string password, string username, bool isRemember)
        {
            PlayerPrefs.SetString("_PandoraBox_Account_LoginAccount_" + _slotIndex + "_Email", email);
            PlayerPrefs.SetString("_PandoraBox_Account_LoginAccount_" + _slotIndex + "_Username", username.ToLower());
            //PlayerPrefs.SetString("_PandoraBox_Account_LoginAccount_" + _slotIndex + "_DisplayText", username.ToLower().Substring(0,6));
            PlayerPrefs.SetInt("_PandoraBox_Account_LoginAccount_" + _slotIndex + "_IsRemember",
                System.Convert.ToInt32(isRemember));
            string newPassword = isRemember ? password : "";
            PlayerPrefs.SetString("_PandoraBox_Account_LoginAccount_" + _slotIndex + "_Password",
                PandoraUtil.SimpleEncrypt(newPassword));
            PlayerPrefs.SetInt("_PandoraBox_Account_lastLoginIndex", _slotIndex);

            //update local data
            Email = email;
            Password = password;
            Username = username;
            IsRemember = isRemember;
        }

        public void CheckSelect()
        {
            Color pictureColor = _slotIndex == PandoraMaster.SelectedLoginAccountIndex
                ? Color.white
                : new Color(100f / 255f, 100f / 255f, 100f / 255f);
            slotPicture.color = pictureColor;
            selectionVFX.SetActive(_slotIndex == PandoraMaster.SelectedLoginAccountIndex);
            particleVFX.SetActive(_slotIndex == PandoraMaster.SelectedLoginAccountIndex);
        }
    }
}