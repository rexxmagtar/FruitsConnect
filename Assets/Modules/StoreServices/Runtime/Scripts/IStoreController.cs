using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StoreServices
{
    public interface IStoreServiceController
    {
        event Action<ProductInfo> OnPurchaseCompleteEvent;
        event Action<ProductInfo, string> OnPurchaseFailedEvent;
        
        Task<bool> PurchaseAsync(string productId);
        Task<ProductInfo> GetProductInfoAsync(string productId);
        Task<List<ProductInfo>> GetAllProductsAsync();
        Task<bool> IsProductPurchasedAsync(string productId);
    }

    [Serializable]
    public class ProductInfo
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Price { get; set; }
        public string PriceCurrencyCode { get; set; }
        public bool IsOwned { get; set; }
        public bool IsAvailable { get; set; }
    }
}
