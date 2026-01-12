using System;

namespace Game.Features.Popup
{
    /// <summary>
    /// Data required for the Refresh Trades Popup, including timer logic and skip costs.
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