using System;

public class RefreshTradesPopupData
{
    public DateTime TargetTime;
    public int SkipCost;
    public bool CanAfford;
    public Action OnSkipClick;
    public Action OnTimerVisuallyFinished;

    public RefreshTradesPopupData(DateTime targetTime, int skipCost, bool canAfford, Action onSkipClick, Action onTimerVisuallyFinished)
    {
        TargetTime = targetTime;
        SkipCost = skipCost;
        CanAfford = canAfford;
        OnSkipClick = onSkipClick;
        OnTimerVisuallyFinished = onTimerVisuallyFinished;
    }
}
