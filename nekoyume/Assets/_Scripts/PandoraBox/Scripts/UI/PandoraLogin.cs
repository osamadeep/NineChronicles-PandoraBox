using Libplanet;
using Libplanet.Crypto;
using Libplanet.KeyStore;
using Nekoyume.Game.Controller;
using Nekoyume.PandoraBox;
using Nekoyume.UI;
using Nekoyume.UI.Scroller;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.CloudScriptModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume
{
    public class PandoraLogin : MonoBehaviour
    {
        [Header("PANDORA CUSTOM FIELDS")] public List<PandoraAccountSlot> PandoraAccounts;
        [SerializeField] private GameObject pandoraSignupGroup;
        [SerializeField] private GameObject pandoraLoginGroup;

        [SerializeField] private GameObject nineLoginGroup;
        [SerializeField] private Toggle nineLoginRememberToggle;
        [Space(10)] [SerializeField] private InputField pandoraSignupEmailField;
        [SerializeField] private InputField pandoraSignup9cAddressField;
        [SerializeField] private InputField pandoraSignupPasswordField;
        [SerializeField] private Toggle pandoraSignupShowToggle;
        [SerializeField] private Button pandoraSignupButton;
        [Space(10)] [SerializeField] private InputField pandoraLoginEmailField;
        [SerializeField] private InputField pandoraLoginPasswordField;
        [SerializeField] private Toggle pandoraLoginRememberToggle;
        [SerializeField] private Button pandoraLoginButton;
        [Space(10)] bool isAuth;
        int localKeystoreIndex = -1;
        int cloudCheckCounter;
        IKeyStore KeyStore = Web3KeyStore.DefaultKeyStore;

        void Awake()
        {
            pandoraSignupButton.onClick.AddListener(PandoraSignupClick);
            pandoraLoginButton.onClick.AddListener(PandoraLoginClick);
            pandoraSignupShowToggle.onValueChanged.AddListener(_ => ShowPassword());
        }

        public void Initilize(string path)
        {
            //Prime.CheckAccounts(this);
            for (int i = 0; i < PandoraAccounts.Count; i++)
            {
                PandoraAccounts[i].LoadData(i);
            }

            // Initialize the key store with the provided path, or use the default key store if path is null
            KeyStore = path is null ? Web3KeyStore.DefaultKeyStore : new Web3KeyStore(path);

            // Load the index of the last logged-in Pandora account from PlayerPrefs, default to 0 if not set
            PandoraMaster.SelectedLoginAccountIndex = PlayerPrefs.GetInt("_PandoraBox_Account_lastLoginIndex", 0);

            // Select the appropriate account slot and load its data
            foreach (var accountSlot in PandoraAccounts)
                accountSlot.CheckSelect();

            // Show the appropriate UI group based on whether the selected account has an email address or not
            if (string.IsNullOrEmpty(PandoraAccounts[PandoraMaster.SelectedLoginAccountIndex].Email))
            {
                pandoraSignupGroup.SetActive(true);
                pandoraLoginGroup.SetActive(false);
            }
            else
            {
                // Populate the email and remember toggle fields with the values from the selected account
                pandoraLoginEmailField.text = PandoraAccounts[PandoraMaster.SelectedLoginAccountIndex].Email;
                pandoraLoginRememberToggle.isOn = PandoraAccounts[PandoraMaster.SelectedLoginAccountIndex].IsRemember;

                // Set the password field to the saved password if the remember toggle is on, or an empty string otherwise
                pandoraLoginPasswordField.text = pandoraLoginRememberToggle.isOn
                    ? PandoraAccounts[PandoraMaster.SelectedLoginAccountIndex].Password
                    : "";

                // Show the login group and hide the sign-up group
                pandoraSignupGroup.SetActive(false);
                pandoraLoginGroup.SetActive(true);
            }
        }


        void PandoraLoginClick()
        {
            // Prevent cloud spam
            if (cloudCheckCounter > 3)
            {
                // Show a system notification alert and return if the counter reaches maximum limit
                PandoraUtil.ShowSystemNotification("You have reached the maximum tries limit. Please restart Pandora.",
                    NotificationCell.NotificationType.Alert);
                return;
            }

            // Check if the email is valid
            if (!IsValidEmail(pandoraLoginEmailField.text))
            {
                // Show a system notification alert and return if the email is invalid
                PandoraUtil.ShowSystemNotification("The email is invalid.", NotificationCell.NotificationType.Alert);
                return;
            }

            // Check if the password is at least 6 characters long
            if (pandoraLoginPasswordField.text.Length < 6)
            {
                // Show a system notification alert and return if the password is less than 6 characters
                PandoraUtil.ShowSystemNotification("The password should be at least 6 characters long.",
                    NotificationCell.NotificationType.Alert);
                return;
            }

            // Increment cloud check counter to prevent spam
            cloudCheckCounter++;

            // hide the login group
            pandoraLoginGroup.SetActive(false);

            // create a request object with user email, password and account info
            var request = new LoginWithEmailAddressRequest
            {
                Email = pandoraLoginEmailField.text,
                Password = pandoraLoginPasswordField.text,
                InfoRequestParameters = new GetPlayerCombinedInfoRequestParams { GetUserAccountInfo = true }
            };

            // send a login request to the PlayFab server
            PlayFabClientAPI.LoginWithEmailAddress(request,
                OnLoginSuccess =>
                {
                    // if the user is using an premium account slot
                    if (PandoraMaster.SelectedLoginAccountIndex > 0)
                    {
                        // check if the user is a premium user
                        PlayFabClientAPI.ExecuteCloudScript(
                            new ExecuteCloudScriptRequest { FunctionName = "checkPremium" },
                            success =>
                            {
                                //PlayFabCloudScriptAPI.ExecuteFunction(new ExecuteFunctionRequest()
                                //{
                                //    Entity = new PlayFab.CloudScriptModels.EntityKey()
                                //    {
                                //        Id = PlayFabSettings.staticPlayer.EntityId, //Get this from when you logged in,
                                //        Type = PlayFabSettings.staticPlayer.EntityType, //Get this from when you logged in
                                //    },
                                //    FunctionName = "GetPandoraTitleData", //This should be the name of your Azure Function that you created.
                                //    FunctionParameter = new Dictionary<string, object>() { { "inputValue", "OsOs" } }, //This is the data that you would want to pass into your function.
                                //    GeneratePlayStreamEvent = true //Set this to true if you would like this call to show up in PlayStream
                                //}, (ExecuteFunctionResult result) =>
                                //{
                                //    if (result.FunctionResultTooLarge ?? false)
                                //    {
                                //        Debug.Log("This can happen if you exceed the limit that can be returned from an Azure Function, See PlayFab Limits Page for details.");
                                //        return;
                                //    }
                                //    Debug.LogError($"The {result.FunctionName} function took {result.ExecutionTimeMilliseconds} to complete");
                                //    Debug.LogError($"Result: {result.FunctionResult.ToString()}");
                                //}, (PlayFabError error) =>
                                //{
                                //    Debug.LogError($"Opps Something went wrong: {error.GenerateErrorReport()}");
                                //});


                                // if the user is not a premium user
                                if (!Convert.ToBoolean(success.FunctionResult))
                                {
                                    PandoraUtil.ShowSystemNotification("This Slot for only Premium Accounts!",
                                        NotificationCell.NotificationType.Information);
                                    pandoraLoginGroup.SetActive(true);
                                    nineLoginGroup.SetActive(false);
                                    return;
                                }
                                // if the user is a premium user
                                else
                                {
                                    PandoraUtil.ShowSystemNotification("Account Logged In Successfully!",
                                        NotificationCell.NotificationType.Information);
                                    GetPandoraUserData(OnLoginSuccess.InfoResultPayload.AccountInfo.Username.ToLower(),
                                        pandoraLoginEmailField.text, pandoraLoginPasswordField.text);
                                }
                            },
                            failed =>
                            {
                                PandoraUtil.ShowSystemNotification(failed.ErrorMessage,
                                    NotificationCell.NotificationType.Alert);
                            });
                    }
                    // if the user is using the free first account slot
                    else
                    {
                        PandoraUtil.ShowSystemNotification("Account Logged In Successfully!",
                            NotificationCell.NotificationType.Information);
                        GetPandoraUserData(OnLoginSuccess.InfoResultPayload.AccountInfo.Username.ToLower(),
                            pandoraLoginEmailField.text, pandoraLoginPasswordField.text);
                    }
                },
                OnLoginFailure =>
                {
                    pandoraLoginGroup.SetActive(true);
                    PandoraUtil.ShowSystemNotification(OnLoginFailure.ErrorMessage,
                        NotificationCell.NotificationType.Alert);
                });
        }


        void PandoraSignupClick()
        {
            // Prevent cloud spam
            if (cloudCheckCounter > 3)
            {
                // Display a system notification
                PandoraUtil.ShowSystemNotification(
                    "You have reached the maximum number of login attempts. Please restart Pandora.",
                    NotificationCell.NotificationType.Alert);
                return;
            }

            // Check if the email is a valid email address
            if (!IsValidEmail(pandoraSignupEmailField.text))
            {
                // Display a system notification
                PandoraUtil.ShowSystemNotification("Please enter a valid email address.",
                    NotificationCell.NotificationType.Alert);
                return;
            }

            // Check if the 9c address is valid
            if (!IsValidAddress(pandoraSignup9cAddressField.text))
            {
                // Display a system notification
                PandoraUtil.ShowSystemNotification("Please enter a valid 9c address.",
                    NotificationCell.NotificationType.Alert);
                return;
            }

            // Check if the password is valid
            if (pandoraSignupPasswordField.text.Length < 6)
            {
                // Display a system notification
                PandoraUtil.ShowSystemNotification("Please enter a password that is at least 6 characters long.",
                    NotificationCell.NotificationType.Alert);
                return;
            }

            // Prevent cloud spam by incrementing the counter
            cloudCheckCounter++;

            // Hide the sign-up UI
            pandoraSignupGroup.SetActive(false);

            // Create a new registration request using the input fields
            var signupRequest = new RegisterPlayFabUserRequest
            {
                Email = pandoraSignupEmailField.text,
                Password = pandoraSignupPasswordField.text,
                Username = pandoraSignup9cAddressField.text.Substring(2, 20).ToLower(),
            };

            // Register the user with PlayFab using the request and handle the response
            PlayFabClientAPI.RegisterPlayFabUser(signupRequest,
                OnRegisterSuccess =>
                {
                    // if the user is using an premium account slot
                    if (PandoraMaster.SelectedLoginAccountIndex > 0)
                    {
                        PandoraUtil.ShowSystemNotification("Account Created Successfully!",
                            NotificationCell.NotificationType.Information);
                        PandoraUtil.ShowSystemNotification("This Slot for only Premium Accounts!",
                            NotificationCell.NotificationType.Information);
                        pandoraLoginGroup.SetActive(true);
                        nineLoginGroup.SetActive(false);
                        return;
                    }
                    else
                    {
                        // If registration was successful, show a success notification and get user data
                        PandoraUtil.ShowSystemNotification("Account Created Successfully!",
                            NotificationCell.NotificationType.Information);
                        GetPandoraUserData(pandoraSignup9cAddressField.text.Substring(2, 20).ToLower(),
                            pandoraSignupEmailField.text, pandoraSignupPasswordField.text);
                    }
                },
                OnRegisterFailure =>
                {
                    // If registration failed, show an appropriate notification and enable the sign-up UI
                    if (OnRegisterFailure.Error == PlayFabErrorCode.UsernameNotAvailable)
                    {
                        PandoraUtil.ShowSystemNotification("This 9c Address is Already Linked to Another Account!",
                            NotificationCell.NotificationType.Alert);
                    }
                    else
                    {
                        PandoraUtil.ShowSystemNotification(OnRegisterFailure.ErrorMessage,
                            NotificationCell.NotificationType.Alert);
                    }

                    pandoraSignupGroup.SetActive(true);
                });
        }

        void GetPandoraUserData(string username, string email, string password)
        {
            // Save the data to the selected Pandora account
            PandoraAccounts[PandoraMaster.SelectedLoginAccountIndex]
                .SaveData(email, password, username, pandoraLoginRememberToggle.isOn);

            // Set the email and password fields in the login screen with the values entered in the signup screen
            pandoraLoginEmailField.text = email;
            pandoraLoginPasswordField.text = password;

            // Set the signup and login groups to inactive
            pandoraSignupGroup.SetActive(false);
            pandoraLoginGroup.SetActive(false);

            // Look up the address and password for the newly registered user from the key store
            localKeystoreIndex = -1;
            string tempAddress = "";
            string tempPassword = "";
            for (int i = 0; i < KeyStore.List().Count(); i++)
            {
                if (KeyStore.List().ElementAt(i).Item2.Address.ToString().Substring(2, 20).ToLower() == username)
                {
                    localKeystoreIndex = i;
                    tempAddress = KeyStore.List().ElementAt(i).Item2.Address.ToString();
                    isAuth = true;
                    break;
                }
            }

            // Set the remember toggle state for the 9c login info widget
            nineLoginRememberToggle.isOn = PandoraAccounts[PandoraMaster.SelectedLoginAccountIndex].IsAutoLogin;

            // If a key store entry is found, get the password and update the widget; otherwise, show an error message
            if (localKeystoreIndex != -1)
            {
                if (nineLoginRememberToggle.isOn)
                    tempPassword = PandoraAccounts[PandoraMaster.SelectedLoginAccountIndex].AddressPassword;
                else
                    nineLoginGroup.SetActive(true);
            }
            else
            {
                pandoraLoginGroup.SetActive(true);
                PandoraUtil.ShowSystemNotification("There is no KeyStore for your Registered Address!",
                    NotificationCell.NotificationType.Information);
            }

            // Update the 9c login info widget with the address and password
            Widget.Find<LoginSystem>().Update9cLoginInfo(tempAddress, tempPassword, nineLoginRememberToggle.isOn);
        }

        public PrivateKey GetKey(string accountPassword)
        {
            // Check if there is valid playfab authentication before dealing with the key store
            if (!isAuth)
            {
                return null;
            }

            // Get the protected private key from the key store object
            KeyStore.List().ElementAt(localKeystoreIndex).Deconstruct(out Guid keyId, out ProtectedPrivateKey ppk);

            try
            {
                // Attempt to unprotect the private key using the provided account password
                PrivateKey privateKey = ppk.Unprotect(accountPassword);

                // Save auto login preference to local player preferences
                int autoLoginInt = Convert.ToInt32(nineLoginRememberToggle);
                PlayerPrefs.SetInt(
                    "_PandoraBox_Account_LoginAccount_" + PandoraMaster.SelectedLoginAccountIndex + "_IsAutoLogin",
                    autoLoginInt);

                return privateKey;
            }
            catch (Exception e)
            {
                // If unprotection is unsuccessful, show an error message and return null
                PandoraUtil.ShowSystemNotification(e.Message, NotificationCell.NotificationType.Information);
                return null;
            }
        }

        public void SetAccountIndex(int value)
        {
            // Play a click sound effect
            AudioController.instance.PlaySfx(AudioController.SfxCode.Click);

            // Set the selected login account index
            PandoraMaster.SelectedLoginAccountIndex = value;

            // Check and update the account selection for each account slot
            foreach (var accountSlot in PandoraAccounts)
                accountSlot.CheckSelect();

            // Set the email field, remember toggle and password field for the selected account slot
            pandoraLoginEmailField.text = PandoraAccounts[value].Email;
            pandoraLoginRememberToggle.isOn = PandoraAccounts[value].IsRemember;
            pandoraLoginPasswordField.text = PandoraAccounts[value].IsRemember ? PandoraAccounts[value].Password : "";
        }

        void ShowPassword()
        {
            if (pandoraSignupShowToggle.isOn)
                pandoraSignupPasswordField.contentType = InputField.ContentType.Standard;
            else
                pandoraSignupPasswordField.contentType = InputField.ContentType.Password;
            pandoraSignupPasswordField.ForceLabelUpdate();
        }

        // Helper method to validate email address
        bool IsValidEmail(string email)
        {
            try
            {
                MailAddress mailAddress = new MailAddress(email);
                return true;
            }
            catch
            {
                return false;
            }
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
    }
}