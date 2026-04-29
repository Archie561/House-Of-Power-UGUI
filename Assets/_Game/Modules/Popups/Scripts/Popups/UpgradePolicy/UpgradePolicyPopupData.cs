using System;
using Game.General;

namespace Game.Features.Popup
{
    /// <summary>
    /// Data transfer object (Model) for the Upgrade Policy Popup.
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