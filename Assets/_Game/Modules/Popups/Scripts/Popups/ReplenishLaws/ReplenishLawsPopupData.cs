using System;
using Game.General;

namespace Game.Features.Popup
{
    /// <summary>
    /// Data container for the Replenish Laws Popup.
    /// </summary>
    public class ReplenishLawsPopupData
    {
        public DateTime TargetTime { get; private set; }
        public ResourceType CostType {get; private set; }
        public int CostAmount { get; private set; }
        public bool CanAfford { get; private set; }
        public Action OnConfirmClick { get; private set;}

        public ReplenishLawsPopupData(DateTime targetTime, ResourceType costType, int costAmount, bool canAfford, Action onConfirmClick)
        {
            TargetTime = targetTime;
            CostType = costType;
            CostAmount = costAmount;
            CanAfford = canAfford;
            OnConfirmClick = onConfirmClick;
        }
    }
}
