#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
#define RUN_ON_MOBILE
#define ENABLE_FIREBASE
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Libplanet.Crypto;
using Nekoyume.L10n;
using Nekoyume.State;
using Nekoyume.UI;
using NineChronicles.ExternalServices.IAPService.Runtime.Models;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using ProductType = UnityEngine.Purchasing.ProductType;

namespace Nekoyume.IAPStore
{
    public class IAPStoreManager : MonoBehaviour, IDetailedStoreListener
    {
        private IStoreController _controller;
        private IExtensionProvider _extensions;

        public IEnumerable<Product> IAPProducts => _controller.products.all;
        public bool IsInitialized { get; private set; }

        private Dictionary<string, ProductSchema> _initailizedProductSchema = new Dictionary<string, ProductSchema>();

        public Dictionary<string, ProductSchema> SeasonPassProduct = new Dictionary<string, ProductSchema>();

        private async void Awake()
        {
            try
            {
                var initializationOptions = new InitializationOptions()
                    .SetEnvironmentName("dev");
                await UnityServices.InitializeAsync(initializationOptions);
            }
            catch (Exception exception)
            {
                // An error occurred during services initialization.
                Debug.LogException(exception);
            }

            var categorys = await Game.Game.instance.IAPServiceManager.GetProductsAsync(
                States.Instance.AgentState.address, Game.Game.instance.CurrentPlanetId.ToString());
            if (categorys is null)
            {
                // TODO: not initialized case handling
                Debug.LogError(
                    $"IAPServiceManager.GetProductsAsync({States.Instance.AgentState.address}): Product Catagorys is null.");
                return;
            }

            foreach (var category in categorys)
            {
                foreach (var product in category.ProductList)
                {
                    _initailizedProductSchema.TryAdd(product.Sku, product);
                }
                if(category.Name == "NoShow")
                {
                    foreach (var product in category.ProductList)
                    {
                        SeasonPassProduct.Add(product.Name, product);
                    }
                }
            }

#if UNITY_EDITOR || RUN_ON_MOBILE
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
            foreach (var schema in _initailizedProductSchema.Where(s => s.Value.Active))
            {
                builder.AddProduct(schema.Value.Sku, ProductType.Consumable);
            }

            UnityPurchasing.Initialize(this, builder);
#endif
        }

        public ProductSchema GetProductSchema(string sku)
        {
            _initailizedProductSchema.TryGetValue(sku, out var result);
            return result;
        }

        public void OnPurchaseClicked(string productId)
        {
            _controller.InitiatePurchase(productId);
        }

        /// <summary>
        /// Called when Unity IAP is ready to make purchases.
        /// </summary>
        void IStoreListener.OnInitialized(IStoreController controller,
            IExtensionProvider extensions)
        {
            _controller = controller;
            _extensions = extensions;
            Debug.Log("IAP Store Manager Initialized successfully!");
            foreach (var product in _controller.products.all)
            {
                Debug.Log(
                    $"{product.definition.id}: {product.metadata.localizedTitle}, {product.metadata.localizedDescription}, {product.metadata.localizedPriceString}");
            }

            IsInitialized = true;
        }

        /// <summary>
        /// Called when Unity IAP encounters an unrecoverable initialization error.
        ///
        /// Note that this will not be called if Internet is unavailable; Unity IAP
        /// will attempt initialization until it becomes available.
        /// </summary>
        void IStoreListener.OnInitializeFailed(InitializationFailureReason error)
        {
            ((IStoreListener)this).OnInitializeFailed(error, string.Empty);
        }

        void IStoreListener.OnInitializeFailed(InitializationFailureReason error, string message)
        {
            Debug.LogError($"Initializing failed for the specified reason: {error}\n{message}");
        }


        [Serializable]
        struct PurchaseReciept
        {
            public string Receipt;
            public string AgentAddressHex;
            public string AvatarAddressHex;
            public string PlanetId;
        }

        [System.Serializable]
        public class LocalTransactionsWrapper
        {
            public List<string> data;
        }

        private const string LOCAL_TRANSACTIONS = "LOCAL_TRANSACTIONS";
        List<string> GetLocalTransactions()
        {
            var listString = PlayerPrefs.GetString(LOCAL_TRANSACTIONS, string.Empty);
            if (string.IsNullOrEmpty(listString))
            {
                return new List<string>();
            }
            return JsonUtility.FromJson<LocalTransactionsWrapper>(listString).data;
        }

        void AddLocalTransactions(string transaction)
        {
            var transactionList = GetLocalTransactions();
            if (transactionList.Contains(transaction))
            {
                Debug.LogWarning($"[AddLocalTransactions] duplicate Transaction {transaction}");
            }
            else
            {
                transactionList.Add(transaction);
                PlayerPrefs.SetString(LOCAL_TRANSACTIONS, JsonUtility.ToJson(new LocalTransactionsWrapper { data = transactionList }));
            }
        }

        void RemoveLocalTransactions(string transaction)
        {
            var transactionList = GetLocalTransactions();
            if (transactionList.Remove(transaction))
            {
                PlayerPrefs.SetString(LOCAL_TRANSACTIONS, JsonUtility.ToJson(new LocalTransactionsWrapper { data = transactionList }));
            }
        }

        async void RePurchaseTryAsync(Product product)
        {
            var purchaseData = PlayerPrefs.GetString("PURCHASE_TX_" + product.transactionID, string.Empty);
            PurchaseReciept pData;
            if (string.IsNullOrEmpty(purchaseData))
            {
                var states = States.Instance;
                pData = new PurchaseReciept
                {
                    Receipt = product.receipt,
                    AgentAddressHex = states?.AgentState?.address.ToHex(),
                    AvatarAddressHex = states?.CurrentAvatarState?.address.ToHex(),
                    PlanetId = Game.Game.instance?.CurrentPlanetId?.ToString(),
                };
            }
            else
            {
                pData = JsonUtility.FromJson<PurchaseReciept>(purchaseData);
            }

            var result = await Game.Game.instance.IAPServiceManager
                    .PurchaseRequestAsync(
                        product.receipt,
                        pData.AgentAddressHex != null ? pData.AgentAddressHex : string.Empty,
                        pData.AvatarAddressHex != null ? pData.AvatarAddressHex : string.Empty,
                        pData.PlanetId != null ? pData.PlanetId : string.Empty,
                        product.transactionID,
                        product.appleOriginalTransactionID);

            if (result is null)
            {
                Debug.LogError($"[RePurchaseTryAsync] Failed {pData.Receipt} AgentAddressHex: {pData.AgentAddressHex} AvatarAddressHex: {pData.AvatarAddressHex} PlanetId: {pData.PlanetId}");
            }
            else
            {
                _controller.ConfirmPendingPurchase(product);
                RemoveLocalTransactions(product.transactionID);
            }
        }

        /// <summary>
        /// Called when a purchase completes.
        /// May be called at any time after OnInitialized().
        /// </summary>
        PurchaseProcessingResult IStoreListener.ProcessPurchase(PurchaseEventArgs e)
        {
            if(e == null)
            {
                Debug.Log("[ProcessPurchase] PurchaseEventArgs is null");
                return PurchaseProcessingResult.Pending;
            }

            try
            {
                var states = States.Instance;
                PurchaseReciept purchaseReciepe = new PurchaseReciept
                {
                    Receipt = e.purchasedProduct.receipt,
                    AgentAddressHex = states.AgentState.address.ToHex(),
                    AvatarAddressHex = states.CurrentAvatarState.address.ToHex(),
                    PlanetId = Game.Game.instance.CurrentPlanetId.ToString(),
                };
                PlayerPrefs.SetString("PURCHASE_TX_"+e.purchasedProduct.transactionID, JsonUtility.ToJson(purchaseReciepe));
                AddLocalTransactions(e.purchasedProduct.transactionID);
            }
            catch (Exception error)
            {
                Debug.LogError("[ProcessPurchase] AddLocalTransactions Error " + error);
            }

            try
            {
                if (!Widget.Find<ShopListPopup>().isActiveAndEnabled && !Widget.Find<SeasonPassPremiumPopup>().isActiveAndEnabled)
                {
                    Debug.Log("[ProcessPurchase] Is not PurchasePage");
                    RePurchaseTryAsync(e.purchasedProduct);
                    return PurchaseProcessingResult.Pending;
                }
            }
            catch (Exception error)
            {
                Debug.LogError("[ProcessPurchase] RePurchaseTryAsync Error " + error);
            }

            try
            {
                if (e.purchasedProduct.availableToPurchase)
                {
                    OnPurchaseRequestAsync(e);
                }
                Widget.Find<ShopListPopup>().PurchaseButtonLoadingEnd();
                Widget.Find<SeasonPassPremiumPopup>().PurchaseButtonLoadingEnd();
                Debug.LogWarning($"not availableToPurchase. e.purchasedProduct.availableToPurchase: {e.purchasedProduct.availableToPurchase}");
                return PurchaseProcessingResult.Pending;
            }
            catch(Exception error)
            {
                Debug.LogError("[ProcessPurchase] " + error);
                return PurchaseProcessingResult.Pending;
            }
        }

        /// <summary>
        /// Called when a purchase fails.
        /// IStoreListener.OnPurchaseFailed is deprecated,
        /// use IDetailedStoreListener.OnPurchaseFailed instead.
        /// </summary>
        void IStoreListener.OnPurchaseFailed(Product i, PurchaseFailureReason p)
        {
            Debug.LogError($"PurchaseFail. reason: {p}, Product: {i}");
            if (p == PurchaseFailureReason.PurchasingUnavailable)
            {
                // IAP may be disabled in device settings.
            }
        }

        /// <summary>
        /// Called when a purchase fails.
        /// </summary>
        void IDetailedStoreListener.OnPurchaseFailed(Product i, PurchaseFailureDescription p)
        {
            Debug.LogError($"PurchaseFail. reason: {p.reason}, Product: {i}");
            Analyzer.Instance.Track(
                "Unity/Shop/IAP/PurchaseResult",
                ("product-id", p.productId),
                ("result", p.reason.ToString()));

            var evt = new AirbridgeEvent("IAP_Failed");
            evt.SetAction(p.productId);
            evt.SetLabel(p.reason.ToString());
            evt.AddCustomAttribute("product-id", p.productId);
            AirbridgeUnity.TrackEvent(evt);

            Widget.Find<SeasonPassPremiumPopup>().PurchaseButtonLoadingEnd();
            Widget.Find<ShopListPopup>().PurchaseButtonLoadingEnd();

            switch (p.reason)
            {
                case PurchaseFailureReason.PurchasingUnavailable:
                    break;
                case PurchaseFailureReason.ExistingPurchasePending:
                    break;
                case PurchaseFailureReason.ProductUnavailable:
                    break;
                case PurchaseFailureReason.SignatureInvalid:
                    break;
                case PurchaseFailureReason.UserCancelled:
                    break;
                case PurchaseFailureReason.PaymentDeclined:
                    break;
                case PurchaseFailureReason.DuplicateTransaction:
                    break;
                case PurchaseFailureReason.Unknown:
                    break;
                default:
                    break;
            }
        }

        private async void OnPurchaseRequestAsync(PurchaseEventArgs e)
        {
            var popup = Widget.Find<IconAndButtonSystem>();
            var states = States.Instance;

            try
            {
                var result = await Game.Game.instance.IAPServiceManager
                    .PurchaseRequestAsync(
                        e.purchasedProduct.receipt,
                        states.AgentState.address.ToHex(),
                        states.CurrentAvatarState.address.ToHex(),
                        Game.Game.instance.CurrentPlanetId.ToString(),
                        e.purchasedProduct.transactionID,
                        e.purchasedProduct.appleOriginalTransactionID);

                Widget.Find<ShopListPopup>().PurchaseButtonLoadingEnd();
                Widget.Find<SeasonPassPremiumPopup>().PurchaseButtonLoadingEnd();

                if (result is null)
                {
                    popup.Show(
                        "UI_ERROR",
                        "UI_IAP_PURCHASE_FAILED",
                        "UI_OK",
                        true);
                }
                else
                {
                    Analyzer.Instance.Track(
                        "Unity/Shop/IAP/PurchaseResult",
                        ("product-id", e.purchasedProduct.definition.id),
                        ("result", "Complete"),
                        ("transaction-id", e.purchasedProduct.transactionID));

                    var evt = new AirbridgeEvent("IAP");
                    evt.SetAction(e.purchasedProduct.definition.id);
                    evt.SetLabel("iap");
                    evt.SetCurrency(e.purchasedProduct.metadata.isoCurrencyCode);
                    evt.SetValue((double)e.purchasedProduct.metadata.localizedPrice);
                    evt.AddCustomAttribute("product-id", e.purchasedProduct.definition.id);
                    evt.SetTransactionId(e.purchasedProduct.transactionID);
                    AirbridgeUnity.TrackEvent(evt);

                    popup.Show(
                        "UI_COMPLETED",
                        "UI_IAP_PURCHASE_COMPLETE",
                        "UI_OK",
                        true,
                        IconAndButtonSystem.SystemType.Information);

                    popup.ConfirmCallback = () =>
                    {
                        if (LoginSystem.GetPassPhrase(states.AgentState.address.ToString()).Equals(string.Empty))
                        {
                            Widget.Find<LoginSystem>().ShowResetPassword();
                        }
                    };

                    Widget.Find<MobileShop>().PurchaseComplete(e.purchasedProduct.definition.id);
                    Widget.Find<MobileShop>().RefreshGrid();
                    Widget.Find<ShopListPopup>().Close();
                    _controller.ConfirmPendingPurchase(e.purchasedProduct);
                    RemoveLocalTransactions(e.purchasedProduct.transactionID);
                }
            }
            catch (Exception exc)
            {
                Widget.Find<SeasonPassPremiumPopup>().PurchaseButtonLoadingEnd();
                Widget.Find<ShopListPopup>().PurchaseButtonLoadingEnd();
                Widget.Find<IconAndButtonSystem>().Show("UI_ERROR", exc.Message, localize: false);
            }
        }
    }
}
