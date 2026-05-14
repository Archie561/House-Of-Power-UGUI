using Game.General;
using System;

namespace Game.Features.Popup
{
    /// <summary>
    /// Data container for the Upgrade Policy Popup. Contains all the necessary initial data.
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