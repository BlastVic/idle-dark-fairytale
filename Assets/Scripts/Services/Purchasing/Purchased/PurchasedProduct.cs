using System;
using UnityEngine.Purchasing;

namespace Assets.Scripts.Services.Purchasing.Purchased
{
    public class PurchasedProduct
    {
        public String Id { get; }
        public Product IAP { get; }
        public ProductType Type { get; }

        public PurchasedProduct(String id, ProductType type)
        {
            Id = id;
            Type = type;
        }

        public PurchasedProduct(Product product)
        {
            IAP = product;
            Id = product.definition.id;
            Type = product.definition.type;
        }
    }
}