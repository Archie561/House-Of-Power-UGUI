using System;
using UnityEngine;

public class AcceptOfferPopupData
{
    public TradeOfferData OfferData;
    public bool CanAfford;
    public Action OnConfirmClick;
    public Action OnCancelClick;

    public AcceptOfferPopupData(TradeOfferData offerData, bool canAfford, Action onConfirmClick, Action onCancelClick)
    {
        OfferData = offerData;
        CanAfford = canAfford;
        OnConfirmClick = onConfirmClick;
        OnCancelClick = onCancelClick;
    }
}
