using Game.General;
using System;

namespace Game.Features.Popup
{
    /// <summary>
    /// Data container for the Refresh Trades Popup. Contains all the necessary initial data.
    /// </summary>
    public class RefreshTradesPopupData
    {
        public DateTime TargetTime { get; }
        public ResourceType CostType {get; }
        public int CostAmount { get; }
        public bool CanAfford { get; }
        public Action OnConfirmClick { get; }

        public RefreshTradesPopupData(DateTime targetTime, ResourceType costType, int costAmount, bool canAfford, Action onConfirmClick)
        {
            TargetTime = targetTime;
            CostType = costType;
            CostAmount = costAmount;
            CanAfford = canAfford;
            OnConfirmClick = onConfirmClick;
        }
    }
}