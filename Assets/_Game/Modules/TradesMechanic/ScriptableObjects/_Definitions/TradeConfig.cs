using System;
using UnityEngine;

namespace Game.Features.Trade
{
    [CreateAssetMenu(fileName = "TradeConfig", menuName = "Game/Trade/Trade Config")]
    public class TradeConfig : ScriptableObject
    {
        [Header("General")]
        [SerializeField] private float _refreshTradesCooldown = 600f;
        [SerializeField] private int _skipRefreshGemCostPerMinute = 2;
        [SerializeField] private int _offersToGenerate = 8;

        [Header("Modules")]
        [SerializeField] private StorageUpgradeSettings _storageSettings;
        [SerializeField] private OfferGenerationSettings _generationSettings;

        public float RefreshTradesCooldown => _refreshTradesCooldown;
        public int SkipRefreshGemCostPerMinute => _skipRefreshGemCostPerMinute;
        public int OffersToGenerate => _offersToGenerate;
        public StorageUpgradeSettings StorageSettings => _storageSettings;
        public OfferGenerationSettings GenerationSettings => _generationSettings;
    }

    [Serializable]
    public class StorageUpgradeSettings
    {
        [Header("Capacity Growth")]
        public int BaseCapacity = 100;
        public float CapacityMultiplier = 1.5f;

        [Header("Upgrade Costs")]
        [Range(0f, 1f)] public float UpgradeCostFillRate = 0.8f;

        [Space]
        public int BasePremiumCost = 5;
        public float PremiumCostMultiplier = 1.3f;
    }

    [Serializable]
    public class OfferGenerationSettings
    {
        [Header("Items Per Offer")]
        public int MinItemsPerSide = 2;
        public int MaxItemsPerSide = 3;
        public int MinTransactionAmount = 10;

        [Header("Volume (Load Factors)")]
        [Range(0.01f, 1f)] public float MinLoadFactor = 0.1f;
        [Range(0.01f, 1f)] public float MaxLoadFactor = 0.3f;

        [Header("Profitability Rates (Import/Export)")]
        public float RateBadMin = 0.6f;
        public float RateBadMax = 0.8f;

        public float RateNormalMin = 0.9f;
        public float RateNormalMax = 1.1f;

        public float RateGoodMin = 1.3f;
        public float RateGoodMax = 1.8f;

        [Header("Probabilities (Must sum to 1.0)")]
        [Range(0f, 1f)] public float ChanceBad = 0.4f;
        [Range(0f, 1f)] public float ChanceNormal = 0.3f;
        // ChanceGood is implicitly 1 - (ChanceBad + ChanceNormal)
    }
}
