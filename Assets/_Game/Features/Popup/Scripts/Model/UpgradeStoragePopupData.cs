using Game.Features.Trade;
using System;

namespace Game.Features.Popup
{
    /// <summary>
    /// Data container for the Upgrade Storage Popup configuration.
    /// </summary>
    public class UpgradeStoragePopupData
    {
        public UpgradeStorageData UpgradeData { get; }
        public bool CanAffordDefault { get; }
        public bool CanAffordPremium { get; }
        public Action OnDefaultClick { get; }
        public Action OnPremiumClick { get; }

        public UpgradeStoragePopupData(UpgradeStorageData upgradeData, bool canAffordDefault, bool canAffordPremium, Action onDefaultClick, Action onPremiumClick)
        {
            UpgradeData = upgradeData;
            CanAffordDefault = canAffordDefault;
            CanAffordPremium = canAffordPremium;
            OnDefaultClick = onDefaultClick;
            OnPremiumClick = onPremiumClick;
        }
    }
}
