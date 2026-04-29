using System;

namespace Game.Features.Popup
{
    /// <summary>
    /// Data transfer object (Model) for the Refresh Trades Popup.
    /// </summary>
    public class RefreshTradesPopupData
    {
        public DateTime TargetTime { get; }
        public int SkipCost { get; }
        public bool CanAfford { get; }
        public Action OnSkipClick { get; }
        public Action OnTimerVisuallyFinished { get; }

        public RefreshTradesPopupData(DateTime targetTime, int skipCost, bool canAfford, Action onSkipClick, Action onTimerVisuallyFinished)
        {
            TargetTime = targetTime;
            SkipCost = skipCost;
            CanAfford = canAfford;
            OnSkipClick = onSkipClick;
            OnTimerVisuallyFinished = onTimerVisuallyFinished;
        }
    }
}