
using System;
using Game.General;
using TMPro;
using UnityEngine;

namespace Game.Features.Popup
{
    /// <summary>
    /// Container View / Presenter for the Replenish Laws Popup.
    /// Acts as a "Smart Component" that listens to the Observable Model.
    /// </summary>
    public class ReplenishLawsPopup : BasePopup
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private PopupCostButton _confirmButton;

        private ReplenishLawsPopupData _data;
        private bool _isTimerRunning;
        private int _lastDisplayedSecond = -1;

        /// <summary>
        /// Initializes the popup with cost and timer data.
        /// </summary>
        /// <param name="data"></param>
        public void Initialize(ReplenishLawsPopupData data)
        {
            _data = data;

            _isTimerRunning = true;
            _confirmButton.Initialize(_data.CostType, _data.CostAmount, _data.CanAfford, _data.OnConfirmClick);
        }

        private void Update() => UpdateTimer();

        private void UpdateTimer()
        {
            if (!_isTimerRunning) return;
            var diff = _data.TargetTime - DateTime.UtcNow;

            if (diff.TotalSeconds <= 0)
            {
                _timerText.text = "00:00";
                _isTimerRunning = false;
                return;
            }

            int totalSecondsLeft = Mathf.CeilToInt((float)diff.TotalSeconds);

            // Update the timer text only if the displayed second has changed to minimize UI updates
            if (totalSecondsLeft != _lastDisplayedSecond)
            {
                _lastDisplayedSecond = totalSecondsLeft;
                int m = totalSecondsLeft / 60;
                int s = totalSecondsLeft % 60;
                _timerText.text = $"{m:00}:{s:00}";
            }
        }

        public void UpdateCostVisuals(int costAmount, bool canAfford)
        {
            _confirmButton.Initialize(_data.CostType, costAmount, canAfford, _data.OnConfirmClick);
        }
    }
}