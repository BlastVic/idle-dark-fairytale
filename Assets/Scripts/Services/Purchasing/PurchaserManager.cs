using System;
using Assets.Scripts.Extentions;
using Assets.Scripts.Services.Purchasing.Purchased;
using Scripts.Level;
using UnityEngine;
using UnityEngine.Purchasing;

namespace Assets.Scripts.Services.Purchasing
{
    public class PurchaserManager : MonoSingleton<PurchaserManager>, IStoreListener
    {
        public const String cherries_consumable_80 = "80_CHERRIES";
        public const String cherries_consumable_500 = "500_CHERRIES";

        public Boolean IsInitialized => StoreController != null && StoreExtensionProvider != null;

        public static event Action<Boolean> Initialized;
        public static event Action<String> PurchaseStarted;
        public static event Action<PurchasedProduct, String> PurchaseSucceeded;
        public static event Action<PurchasedProduct> PurchaseFailed;

        public static IStoreController StoreController { get; private set; }          // The Unity Purchasing system.
        public static IExtensionProvider StoreExtensionProvider { get; private set; } // The store-specific Purchasing subsystems.

        private String _inAppSource;
        private LevelTypeEnum _lastStatPurchased = LevelTypeEnum.NONE;
        protected override void Awake()
        {
            base.Awake();
            // If we haven't set up the Unity Purchasing reference
            if (StoreController == null)
            {
                // Begin to configure our connection to Purchasing
                InitializePurchasing();
            }
        }

        public void InitializePurchasing()
        {
            // If we have already connected to Purchasing ...
            if (IsInitialized)
            {
                // ... we are done here.
                return;
            }

            // Create a builder, first passing in a suite of Unity provided stores.
            var module = StandardPurchasingModule.Instance();
            module.useFakeStoreUIMode = FakeStoreUIMode.StandardUser;
            var builder = ConfigurationBuilder.Instance(module);


            //Adding consumable items for shop
            builder.AddProduct(cherries_consumable_80, ProductType.Consumable);
            builder.AddProduct(cherries_consumable_500, ProductType.Consumable);

            // Kick off the remainder of the set-up with an asynchrounous call, passing the configuration 
            // and this class' instance. Expect a response either in OnInitialized or OnInitializeFailed.
            UnityPurchasing.Initialize(this, builder);
        }

        public Boolean LastStatPurchased(LevelTypeEnum levelType)
        {
            return _lastStatPurchased.Equals(levelType);
        }

        public void SetLastStatPurchased(LevelTypeEnum levelType)
        {
            _lastStatPurchased = levelType;
        }

        private void SetupPurchaseProcess(String productId, String source)
        {
            _inAppSource = source;
            PurchaseStarted.SafeInvoke(productId);
        }

        public void BuyProductID(string productId, String source = "Shop")
        {
            if (IsInitialized)
            {
                Product product = StoreController.products.WithID(productId);

                if (product != null && product.availableToPurchase)
                {
                    SetupPurchaseProcess(productId, source);
                    LoggerMethods.Log($"Purchasing product asychronously: '{product.definition.id}'");
                    StoreController.InitiatePurchase(product);
                }
                else
                {
                    LoggerMethods.LogError($"Unable to purchase product with id '{productId}' (product not found)");
                }
            }
            else
            {
                LoggerMethods.LogError("Unable to purchase product (system isn't initialized)");
            }
        }

#if UNITY_IOS
        // Restore purchases previously made by this customer. Some platforms automatically restore purchases, like Google. 
        // Apple currently requires explicit purchase restoration for IAP, conditionally displaying a password prompt.
        public void RestorePurchases()
        {
            // If Purchasing has not yet been set up ...
            if (!IsInitialized)
            {
                // ... report the situation and stop restoring. Consider either waiting longer, or retrying initialization.
                LoggerMethods.Log("RestorePurchases FAIL. Not initialized.");
                return;
            }

            // If we are running on an Apple device ... 
            if (Application.platform == RuntimePlatform.IPhonePlayer ||
                Application.platform == RuntimePlatform.OSXPlayer)
            {
                // ... begin restoring purchases
                LoggerMethods.Log("RestorePurchases started ...");

                // Fetch the Apple store-specific subsystem.
                var apple = StoreExtensionProvider.GetExtension<IAppleExtensions>();
                // Begin the asynchronous process of restoring purchases. Expect a confirmation response in 
                // the Action<bool> below, and ProcessPurchase if there are previously purchased products to restore.
                apple.RestoreTransactions((result) =>
                {
                    // The first phase of restoration. If no more responses are received on ProcessPurchase then 
                    // no purchases are available to be restored.
                    LoggerMethods.Log("RestorePurchases continuing: " + result + ". If no further messages, no purchases available to restore.");
                });
            }
            // Otherwise ...
            else
            {
                // We are not running on an Apple device. No work is necessary to restore purchases.
                LoggerMethods.Log("RestorePurchases FAIL. Not supported on this platform. Current = " + Application.platform);
            }
        }
#endif

        #region IStoreListener
        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            // Purchasing has succeeded initializing. Collect our Purchasing references.
            LoggerMethods.Log("OnInitialized: PASS");

            // Overall Purchasing system, configured with products for this application.
            StoreController = controller;
            // Store specific subsystem, for accessing device-specific store features.
            StoreExtensionProvider = extensions;
            Initialized.SafeInvoke(true);
        }


        public void OnInitializeFailed(InitializationFailureReason error)
        {
            // Purchasing set-up has not succeeded. Check error for reason. Consider sharing this reason with the user.
            LoggerMethods.Log("OnInitializeFailed InitializationFailureReason:" + error);
            Initialized.SafeInvoke(false);
        }


        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            FinalizePurchase(new PurchasedProduct(args.purchasedProduct), true);
            // Return a flag indicating whether this product has completely been received, or if the application needs 
            // to be reminded of this purchase at next app launch. Use PurchaseProcessingResult.Pending when still 
            // saving purchased products to the cloud, and when that save is delayed. 
            return PurchaseProcessingResult.Complete;
        }


        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            // A product purchase attempt did not succeed. Check failureReason for more detail. Consider sharing 
            // this reason with the user to guide their troubleshooting actions.
            LoggerMethods.Log(string.Format("OnPurchaseFailed: FAIL. Product: '{0}', PurchaseFailureReason: {1}", product.definition.storeSpecificId, failureReason));
            FinalizePurchase(new PurchasedProduct(product), false);
        }
        #endregion
        private void FinalizePurchase(PurchasedProduct product, Boolean success)
        {
            if (success)
            {
                PurchaseSucceeded.SafeInvoke(product, _inAppSource);
            }
            else
            {
                PurchaseFailed.SafeInvoke(product);
            }

            _inAppSource = null;
        }
    }
}