
using System;
using Game.General;
using TMPro;
using UnityEngine;

namespace Game.Features.Popup
{
    public class ReplenishLawsPopup : BasePopup
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private PopupCostButton _confirmButton;

        private ReplenishLawsPopupData _popupData;
        private bool _isTimerRunning;

        private void Update()
        {
            if (!_isTimerRunning) return;
            UpdateTimerVisuals();
        }

        public void Initialize(ReplenishLawsPopupData data)
        {
            if (data == null)
            {
                Debug.LogError("[ReplenishLawsPopup] Data is missing!");
                return;
            }

            if (_popupData != null) _popupData.OnDataUpdated -= RefreshVisuals;

            _popupData = data;
            _popupData.OnDataUpdated += RefreshVisuals;

            _isTimerRunning = true;
            RefreshVisuals();
        }

        private void RefreshVisuals()
        {
            _confirmButton.Initialize(ResourceType.Gems, _popupData.TotalCost, _popupData.CanAfford, _popupData.OnConfirmClick);
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