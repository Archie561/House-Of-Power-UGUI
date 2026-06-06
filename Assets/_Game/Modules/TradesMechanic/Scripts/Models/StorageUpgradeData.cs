namespace Game.Features.Trade
{
    /// <summary>
    /// Data transfer object representing the storage upgrade information for a trade mechanic.
    /// </summary>
    public class StorageUpgradeData
    {
        public int CurrentCapacity { get; }
        public int UpgradedCapacity { get; }
        public int DefaultCost { get; }
        public int PremiumCost { get; }

        public StorageUpgradeData(int currentCapacity, int upgradedCapacity, int defaultCost, int premiumCost)
        {
            CurrentCapacity = currentCapacity;
            UpgradedCapacity = upgradedCapacity;
            DefaultCost = defaultCost;
            PremiumCost = premiumCost;
        }
    }
}