using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StoreServices;

namespace StoreServices
{
    public class StoreManager : MonoBehaviour, IStoreServiceController
    {
        public static StoreManager Instance { get; private set; }

        private IStoreServiceController storeController;
        private bool isInitialized = false;

        // Platform defines
        private const string GOOGLE_PLAY_PLATFORM = "GOOGLE_PLAY_PLATFORM";
        private const string RUSTORE_PLATFORM = "RUSTORE_PLATFORM";

        public bool IsInitialized => isInitialized;

        public event Action<bool> OnInitializedEvent;
        public event Action<ProductInfo> OnPurchaseCompleteEvent;
        public event Action<ProductInfo, string> OnPurchaseFailedEvent;

        private async void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                await InitializeStore();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private async Task InitializeStore()
        {
            try
            {
                // Determine which store implementation to use based on platform defines
                await CreateStoreController();
            }
            catch (Exception e)
            {
                Debug.LogError($"Store initialization failed: {e.Message}");
                isInitialized = false;
            }
            
        }

        private async Task CreateStoreController()
        {
            // Check platform defines to determine which store to use
            #if GOOGLE_PLAY_PLATFORM
                Debug.Log("Creating Unity IAP Store Manager for Google Play");
                 var storeControllerImpl = new UnityIAPStoreManager();
                 storeController = storeControllerImpl;
                 storeControllerImpl.OnInitializedEvent += OnStoreInitialized;
                 await storeControllerImpl.InitializeAsync();
            #elif RUSTORE_PLATFORM
                Debug.Log("Creating Rustore Store Manager");
                var storeControllerImpl = new RustoreStoreManager();
                storeController = storeControllerImpl;
                storeControllerImpl.OnInitializedEvent += OnStoreInitialized;
                await storeControllerImpl.InitializeAsync();
            #else
                Debug.Log("No platform define found, defaulting to Unity IAP Store Manager");
                var storeControllerImpl = new UnityIAPStoreManager();
                storeController = storeControllerImpl;
                 storeControllerImpl.OnInitializedEvent += OnStoreInitialized;
                 await storeControllerImpl.InitializeAsync();
            #endif
        }

        private void OnStoreInitialized(bool success)
        {
            isInitialized = success;
            storeController.OnPurchaseCompleteEvent += OnStorePurchaseComplete;
            storeController.OnPurchaseFailedEvent += OnStorePurchaseFailed;
            OnInitializedEvent?.Invoke(success);
        }

        private void OnStorePurchaseComplete(ProductInfo productInfo)
        {
            OnPurchaseCompleteEvent?.Invoke(productInfo);
        }

        private void OnStorePurchaseFailed(ProductInfo productInfo, string error)
        {
            OnPurchaseFailedEvent?.Invoke(productInfo, error);
        }



        public async Task<bool> PurchaseAsync(string productId) => await storeController.PurchaseAsync(productId);

        public async Task<ProductInfo> GetProductInfoAsync(string productId) => await storeController.GetProductInfoAsync(productId);
        
           

        public async Task<List<ProductInfo>> GetAllProductsAsync() => await storeController.GetAllProductsAsync();


        public async Task<bool> IsProductPurchasedAsync(string productId) => await storeController.IsProductPurchasedAsync(productId);


        public void ReinitializeStore()
        {
            if (!isInitialized)
            {
                InitializeStore();
            }
        }
    }
}
