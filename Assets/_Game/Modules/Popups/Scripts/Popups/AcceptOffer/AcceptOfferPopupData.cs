using Game.Features.Trade;
using System;
using UnityEngine.UI;

namespace Game.Features.Popup
{
    /// <summary>
    /// Data transfer object (Model) for the Accept Offer Popup.
    /// </summary>
    public class AcceptOfferPopupData
    {
        public TradeOfferData OfferData { get; }
        public bool CanAfford { get; }
        public Action OnConfirmClick { get; }
        public Action OnCancelClick { get; }

        public AcceptOfferPopupData(TradeOfferData offerData, bool canAfford, Action onConfirmClick, Action onCancelClick)
        {
            OfferData = offerData;
            CanAfford = canAfford;
            OnConfirmClick = onConfirmClick;
            OnCancelClick = onCancelClick;
        }
    }
}
