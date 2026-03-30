
using System;

namespace Game.Features.Popup
{
    /// <summary>
    /// ViewModel for the Replenish Laws Popup, containing all necessary data and actions for the popup's functionality.
    /// </summary>
    public class ReplenishLawsPopupData
    {
        public DateTime TargetTime { get; }
        public Action OnConfirmClick { get; }
        public Action OnTimerVisuallyFinished { get; }

        public int TotalCost { get; private set; }
        public bool CanAfford { get; private set; }

        public Action OnDataUpdated { get; set; }

        public ReplenishLawsPopupData(DateTime targetTime, int totalCost, bool canAfford, Action onConfirmClick, Action onTimerVisuallyFinished)
        {
            TargetTime = targetTime;
            TotalCost = totalCost;
            CanAfford = canAfford;
            OnConfirmClick = onConfirmClick;
            OnTimerVisuallyFinished = onTimerVisuallyFinished;
        }

        public void UpdateCost(int newCost, bool canAfford)
        {
            TotalCost = newCost;
            CanAfford = canAfford;
            OnDataUpdated?.Invoke();
        }
    }
}
