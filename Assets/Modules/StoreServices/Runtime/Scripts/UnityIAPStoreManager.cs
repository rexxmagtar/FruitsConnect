using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using System.Collections.Generic;
using System;
using Unity.Services.Core;
using System.Threading.Tasks;
using System.Linq;

namespace StoreServices
{
    public class UnityIAPStoreManager : IStoreServiceController, IDetailedStoreListener
    {
        private UnityEngine.Purchasing.IStoreController storeController;
        private IExtensionProvider storeExtensionProvider;
        private bool isInitialized = false;

        // Define your product IDs here
        private const string PRODUCT_NO_ADS = "no_ads";

        public event Action<ProductInfo> OnPurchaseCompleteEvent;
        public event Action<ProductInfo, string> OnPurchaseFailedEvent;
        
        // Internal initialization event for StoreManager
        public event Action<bool> OnInitializedEvent;
        
        // Internal property for StoreManager
        public bool IsInitialized => isInitialized;

        public async Task<bool> InitializeAsync()
        {
            try
            {
                Debug.Log("Starting Unity Services initialization...");
                
                // Check if Unity Services is already initialized
                if (UnityServices.State == ServicesInitializationState.Initialized)
                {
                    Debug.Log("Unity Services already initialized, proceeding with IAP initialization");
                    InitializePurchasing();
                    return true;
                }

                var options = new InitializationOptions();
                await UnityServices.InitializeAsync(options);
                Debug.Log("Unity Services initialized successfully");
                InitializePurchasing();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Unity Services initialization failed: {e.Message}");
                Debug.LogError($"Stack trace: {e.StackTrace}");
                OnInitializedEvent?.Invoke(false);
                return false;
            }
        }

        private void InitializePurchasing()
        {
            try
            {
                Debug.Log("Initializing Unity IAP Store");
                if (CheckStoreInitialized())
                {
                    isInitialized = true;
                    OnInitializedEvent?.Invoke(true);
                    return;
                }

                Debug.Log("Creating Unity IAP configuration builder");
                var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
                
                if (builder == null)
                {
                    Debug.LogError("Failed to create ConfigurationBuilder instance");
                    OnInitializedEvent?.Invoke(false);
                    return;
                }
                
                builder.AddProduct(PRODUCT_NO_ADS, ProductType.NonConsumable);

                Debug.Log("Initializing Unity Purchasing with builder");
                UnityPurchasing.Initialize(this, builder);
                Debug.Log("Unity Purchasing initialization call completed");
            }
            catch (Exception e)
            {
                Debug.LogError($"Unity IAP initialization failed: {e.Message}");
                Debug.LogError($"Stack trace: {e.StackTrace}");
                OnInitializedEvent?.Invoke(false);
            }
        }

        private bool CheckStoreInitialized()
        {
            return storeController != null && storeExtensionProvider != null;
        }

        public void OnInitialized(UnityEngine.Purchasing.IStoreController controller, IExtensionProvider extensions)
        {
            try
            {
                Debug.Log("OnInitialized: UnityIAPStoreManager");
                if (controller == null)
                {
                    Debug.LogError("Store controller is null in OnInitialized callback");
                    OnInitializedEvent?.Invoke(false);
                    return;
                }

                Debug.Log("OnInitialized: UnityIAPStoreManager_2");

                if (extensions == null)
                {
                    Debug.LogError("Store extension provider is null in OnInitialized callback");
                    OnInitializedEvent?.Invoke(false);
                    return;
                }

                Debug.Log("OnInitialized: UnityIAPStoreManager_3");

                storeController = controller;
                storeExtensionProvider = extensions;
                isInitialized = true;
                Debug.Log("OnInitialized: UnityIAPStoreManager_4");
                OnInitializedEvent?.Invoke(true);
                Debug.Log("Unity IAP Store initialized successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error in OnInitialized callback: {e.Message}");
                OnInitializedEvent?.Invoke(false);
            }
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            isInitialized = false;
            Debug.LogError($"Unity IAP Store initialization failed: {error}");
            OnInitializedEvent?.Invoke(false);
        }

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            isInitialized = false;
            Debug.LogError($"Unity IAP Store initialization failed: {error}, Message: {message}");
            OnInitializeFailed(error);
        }

        public async Task<bool> PurchaseAsync(string productId)
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("Unity IAP Store not initialized. Cannot purchase.");
                return false;
            }

            Product product = storeController.products.WithID(productId);
            if (product != null && product.availableToPurchase)
            {
                storeController.InitiatePurchase(product);
                return true;
            }
            else
            {
                Debug.LogWarning($"Product {productId} is not available for purchase");
                return false;
            }
        }

        public async Task<ProductInfo> GetProductInfoAsync(string productId)
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("Unity IAP Store not initialized. Cannot get product info.");
                return null;
            }

            Product product = storeController.products.WithID(productId);
            if (product == null)
            {
                Debug.LogWarning($"Product {productId} not found.");
                return null;
            }

            return new ProductInfo
            {
                Id = product.definition.id,
                Title = product.metadata.localizedTitle,
                Description = product.metadata.localizedDescription,
                Price = product.metadata.localizedPriceString,
                PriceCurrencyCode = product.metadata.isoCurrencyCode,
                IsOwned = product.hasReceipt,
                IsAvailable = product.availableToPurchase
            };
        }

        public async Task<List<ProductInfo>> GetAllProductsAsync()
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("Unity IAP Store not initialized. Cannot get products.");
                return new List<ProductInfo>();
            }

            return storeController.products.all.Select(product => new ProductInfo
            {
                Id = product.definition.id,
                Title = product.metadata.localizedTitle,
                Description = product.metadata.localizedDescription,
                Price = product.metadata.localizedPriceString,
                PriceCurrencyCode = product.metadata.isoCurrencyCode,
                IsOwned = product.hasReceipt,
                IsAvailable = product.availableToPurchase
            }).ToList();
        }

        public async Task<bool> IsProductPurchasedAsync(string productId)
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("Unity IAP Store not initialized. Cannot check product ownership.");
                return false;
            }

            Product product = storeController.products.WithID(productId);
            if (product == null)
            {
                Debug.LogWarning($"Product {productId} not found.");
                return false;
            }

            return product.hasReceipt;
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            Debug.Log($"Unity IAP Purchase successful: {args.purchasedProduct.definition.id}");
            
            var productInfo = new ProductInfo
            {
                Id = args.purchasedProduct.definition.id,
                Title = args.purchasedProduct.metadata.localizedTitle,
                Description = args.purchasedProduct.metadata.localizedDescription,
                Price = args.purchasedProduct.metadata.localizedPriceString,
                PriceCurrencyCode = args.purchasedProduct.metadata.isoCurrencyCode,
                IsOwned = true,
                IsAvailable = args.purchasedProduct.availableToPurchase
            };
            
            OnPurchaseCompleteEvent?.Invoke(productInfo);
            return PurchaseProcessingResult.Complete;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            Debug.LogWarning($"Unity IAP Purchase failed: {product.definition.id}, Reason: {failureReason}");
            
            var productInfo = new ProductInfo
            {
                Id = product.definition.id,
                Title = product.metadata.localizedTitle,
                Description = product.metadata.localizedDescription,
                Price = product.metadata.localizedPriceString,
                PriceCurrencyCode = product.metadata.isoCurrencyCode,
                IsOwned = product.hasReceipt,
                IsAvailable = product.availableToPurchase
            };
            
            OnPurchaseFailedEvent?.Invoke(productInfo, failureReason.ToString());
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
        {
            Debug.LogWarning($"Unity IAP Purchase failed: {product.definition.id}, Reason: {failureDescription.reason}, Message: {failureDescription.message}");
            
            var productInfo = new ProductInfo
            {
                Id = product.definition.id,
                Title = product.metadata.localizedTitle,
                Description = product.metadata.localizedDescription,
                Price = product.metadata.localizedPriceString,
                PriceCurrencyCode = product.metadata.isoCurrencyCode,
                IsOwned = product.hasReceipt,
                IsAvailable = product.availableToPurchase
            };
            
            OnPurchaseFailedEvent?.Invoke(productInfo, failureDescription.reason.ToString());
        }
    }
}
