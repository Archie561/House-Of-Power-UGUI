using Game.General;
using System;

namespace Game.Features.Popup
{
    /// <summary>
    /// Data transfer object (Model) for the Upgrade Storage Popup.
    /// </summary>
    public class UpgradeStoragePopupData
    {
        public ResourceType StorageType { get; }
        public int CurrentCapacity { get; }
        public int UpgradedCapacity { get; }
        public int DefaultCost { get; }
        public int PremiumCost { get; }
        public bool CanAffordDefault { get; }
        public bool CanAffordPremium { get; }
        public Action OnDefaultClick { get; }
        public Action OnPremiumClick { get; }

        public UpgradeStoragePopupData(ResourceType storageType, int currentCapacity, int upgradedCapacity, int defaultCost, int premiumCost,
            bool canAffordDefault, bool canAffordPremium, Action onDefaultClick, Action onPremiumClick)
        {
            StorageType = storageType;
            CurrentCapacity = currentCapacity;
            UpgradedCapacity = upgradedCapacity;
            DefaultCost = defaultCost;
            PremiumCost = premiumCost;
            CanAffordDefault = canAffordDefault;
            CanAffordPremium = canAffordPremium;
            OnDefaultClick = onDefaultClick;
            OnPremiumClick = onPremiumClick;
        }
    }
}
