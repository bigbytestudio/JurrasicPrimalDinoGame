using System;
using UnityEngine;

namespace DinoGame.UI.Menu
{
    [Serializable]
    public struct StoreIapOffer
    {
        [Tooltip("Optional. Matches the card GameObject name in the scroll list (e.g. starter_1).")]
        public string cardId;

        public string heading;
        public string priceDisplay;
        [Min(0)] public int amount;

        [Tooltip("Optional store SKU for real IAP integration.")]
        public string storeProductId;
    }

    [CreateAssetMenu(menuName = "Dino Game/UI/Store IAP Catalog", fileName = "StoreIapCatalog")]
    public sealed class StoreIapCatalog : ScriptableObject
    {
        [SerializeField] private StoreIapOffer[] bonesOffers = Array.Empty<StoreIapOffer>();
        [SerializeField] private StoreIapOffer[] dnaOffers = Array.Empty<StoreIapOffer>();

        public StoreIapOffer[] BonesOffers => bonesOffers ?? Array.Empty<StoreIapOffer>();
        public StoreIapOffer[] DnaOffers => dnaOffers ?? Array.Empty<StoreIapOffer>();

        public StoreIapOffer[] GetOffers(StoreTab tab)
        {
            return tab switch
            {
                StoreTab.Bones => BonesOffers,
                StoreTab.Dna => DnaOffers,
                _ => Array.Empty<StoreIapOffer>()
            };
        }
    }
}
