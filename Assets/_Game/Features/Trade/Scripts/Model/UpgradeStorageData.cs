public class UpgradeStorageData
{
    public ResourceType StorageType;
    public int DefaultCostAmount;
    public int PremiumCostAmount;
    public int CurrentCapacity;
    public int NextCapacity;

    public UpgradeStorageData(ResourceType storageType, int defaultCostAmount, int premiumCostAmount, int currentCapacity, int nextCapacity)
    {
        StorageType = storageType;
        DefaultCostAmount = defaultCostAmount;
        PremiumCostAmount = premiumCostAmount;
        CurrentCapacity = currentCapacity;
        NextCapacity = nextCapacity;
    }
}