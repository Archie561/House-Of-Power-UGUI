using System;
using Game.General;

namespace Game.Features.Popup
{
    /// <summary>
    /// Data class used to initialize the Upgrade Policy Popup. Contains all necessary information about the policy and its upgrade status.
    /// </summary>
    public class UpgradePolicyPopupData
    {
        public ResourceType Type { get; }
        public int GemPrice { get; }
        public bool CanAfford { get; }
        public Action OnBuyClick { get; }

        public UpgradePolicyPopupData(ResourceType type, int gemPrice, bool canAfford, Action onBuyClick)
        {
            Type = type;
            GemPrice = gemPrice;
            CanAfford = canAfford;
            OnBuyClick = onBuyClick;
        }
    }
}