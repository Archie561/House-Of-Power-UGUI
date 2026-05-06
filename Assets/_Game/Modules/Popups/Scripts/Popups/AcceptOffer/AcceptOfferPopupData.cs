using Game.Features.Trade;
using System;

namespace Game.Features.Popup
{
    /// <summary>
    /// Data container for the Accept Offer Popup.
    /// </summary>
    public class AcceptOfferPopupData
    {
        public TradeOfferData OfferData { get; private set; }
        public bool CanAfford { get; private set; }
        public Action OnConfirmClick { get; private set; }
        public Action OnCancelClick { get; private set; }

        public AcceptOfferPopupData(TradeOfferData offerData, bool canAfford, Action onConfirmClick, Action onCancelClick)
        {
            OfferData = offerData;
            CanAfford = canAfford;
            OnConfirmClick = onConfirmClick;
            OnCancelClick = onCancelClick;
        }
    }
}
