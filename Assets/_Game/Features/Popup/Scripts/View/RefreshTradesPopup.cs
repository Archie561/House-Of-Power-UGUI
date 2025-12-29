using System;
using TMPro;
using UnityEngine;

public class RefreshTradesPopup : BasePopup
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _timerText;
    [SerializeField] private PopupCostButton _skipButton;

    private DateTime _targetTime;
    private bool _isTimerRunning;
    private Action _onTimerFinishedCallback;

    public void Initialize(RefreshTradesPopupData data)
    {
        _targetTime = data.TargetTime;
        _onTimerFinishedCallback = data.OnTimerVisuallyFinished;
        _isTimerRunning = true;

        _skipButton.Initialize(ResourceType.Gems, data.SkipCost, data.CanAfford, data.OnSkipClick);

        UpdateTimerVisuals();
    }

    private void Update()
    {
        if (!_isTimerRunning) return;
        UpdateTimerVisuals();
    }

    private void UpdateTimerVisuals()
    {
        var timeLeft = _targetTime - DateTime.Now;

        if (timeLeft.TotalSeconds <= 0)
        {
            _timerText.text = "00:00";
            _isTimerRunning = false;
            _onTimerFinishedCallback?.Invoke();
            return;
        }

        _timerText.text = $"{timeLeft.Minutes:D2}:{timeLeft.Seconds:D2}";
    }
}
