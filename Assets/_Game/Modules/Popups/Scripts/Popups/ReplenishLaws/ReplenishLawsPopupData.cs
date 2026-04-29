
using System;

namespace Game.Features.Popup
{
    /// <summary>
    /// Observable Model for the Replenish Laws Popup.
    /// Holds the data and uses the Observer pattern (OnDataUpdated event) 
    /// to notify subscribers (Presenters/Containers) when data changes, 
    /// keeping the data logic decoupled from the UI.
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
