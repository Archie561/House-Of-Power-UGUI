using Game.General;
using System;
using TMPro;
using UnityEngine;

namespace Game.Features.Popup
{
    /// <summary>
    /// Popup that shows a countdown timer for the next trade refresh and an option to skip the wait.
    /// </summary>
    public class RefreshTradesPopup : BasePopup
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private PopupCostButton _skipButton;

        private RefreshTradesPopupData _popupData;
        private bool _isTimerRunning;

        private void Update()
        {
            if (!_isTimerRunning) return;
            UpdateTimerVisuals();
        }

        /// <summary>
        /// Initializes the popup with timer data and skip cost.
        /// </summary>
        public void Initialize(RefreshTradesPopupData data)
        {
            if (data == null)
            {
                Debug.LogError("[RefreshTradesPopup] Data is missing!");
                return;
            }

            _popupData = data;

            _isTimerRunning = true;
            _skipButton.Initialize(ResourceType.Gems, _popupData.SkipCost, _popupData.CanAfford, _popupData.OnSkipClick);
            UpdateTimerVisuals();
        }

        private void UpdateTimerVisuals()
        {
            var diff = _popupData.TargetTime - DateTime.UtcNow;

            if (diff.TotalSeconds <= 0)
            {
                _timerText.text = "00:00";
                _isTimerRunning = false;
                _popupData.OnTimerVisuallyFinished?.Invoke();
                return;
            }

            int totalSecondsLeft = Mathf.CeilToInt((float)diff.TotalSeconds);

            int m = totalSecondsLeft / 60;
            int s = totalSecondsLeft % 60;

            _timerText.text = $"{m:00}:{s:00}";
        }
    }
}