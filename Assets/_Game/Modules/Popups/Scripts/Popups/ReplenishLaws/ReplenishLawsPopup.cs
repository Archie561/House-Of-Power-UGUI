
using System;
using TMPro;
using UnityEngine;

namespace Game.Features.Popup
{
    /// <summary>
    /// View for the Replenish Laws Popup. Displays a timer until the next free replenish and a button to buy a full replenish.
    /// Forwards results of user interactions via callbacks. Contains a method for updating the cost and affordability state of the confirm button.
    /// It is recommended to subscribe to the relevant events in the popup's setup method (via PopupController)
    /// to keep the button's state updated without reinitializing the entire popup.
    /// </summary>
    public class ReplenishLawsPopup : BasePopup
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private PopupCostButton _confirmButton;

        private ReplenishLawsPopupData _popupData;
        private bool _isTimerRunning;
        private int _lastDisplayedSecond = -1;

        /// <summary>
        /// Initializes the popup with timer data and skip cost.
        /// </summary>
        public void Initialize(ReplenishLawsPopupData data)
        {
            _popupData = data;

            _isTimerRunning = true;
            _confirmButton.Initialize(_popupData.CostType, _popupData.CostAmount, _popupData.CanAfford, _popupData.OnConfirmClick);
        }

        /// <summary>
        /// Updates the cost button's visuals, allowing to change the cost and affordability state without reinitializing the entire popup.
        /// </summary>
        public void UpdateCostVisuals(int costAmount, bool canAfford)
        {
            _confirmButton.Initialize(_popupData.CostType, costAmount, canAfford, _popupData.OnConfirmClick);
        }

        private void UpdateTimer()
        {
            if (!_isTimerRunning) return;
            var diff = _popupData.TargetTime - DateTime.UtcNow;

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

        private void Update() => UpdateTimer();
    }
}