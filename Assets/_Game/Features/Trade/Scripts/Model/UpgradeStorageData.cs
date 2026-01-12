using Game.General;

namespace Game.Features.Trade
{
    /// <summary>
    /// Contains all necessary data to display and process a storage upgrade.
    /// Includes current state, target state, and costs.
    /// </summary>
    public class UpgradeStorageData
    {
        public ResourceType StorageType { get; }
        public int DefaultCostAmount { get; }
        public int PremiumCostAmount { get; }
        public int CurrentCapacity { get; }
        public int NextCapacity { get; }

        public UpgradeStorageData(
            ResourceType storageType,
            int defaultCostAmount,
            int premiumCostAmount,
            int currentCapacity,
            int nextCapacity)
        {
            StorageType = storageType;
            DefaultCostAmount = defaultCostAmount;
            PremiumCostAmount = premiumCostAmount;
            CurrentCapacity = currentCapacity;
            NextCapacity = nextCapacity;
        }
    }
}