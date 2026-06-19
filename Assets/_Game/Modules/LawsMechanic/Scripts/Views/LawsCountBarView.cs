using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Features.Law
{
    /// <summary>
    /// Represents a UI panel-object with law count bar and a timer.
    /// </summary>
    public class LawsCountBarView : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _lawsCountText;
        [SerializeField] private TextMeshProUGUI _timeToNextLawReplenishText;
        [SerializeField] private Image _timerIcon;
        [SerializeField] private Button _replenishLawButton;
        [SerializeField] private Sprite _activeButtonSprite;
        [SerializeField] private Sprite _unactiveButtonSprite;
        [SerializeField] private Slider _lawsCountBar;

        [Header("Config")]
        [SerializeField] private float _countBarAnimationDuration = 0.5f;

        public event Action OnReplenishButtonClick;

        private void Awake()
        {
            _replenishLawButton.onClick.RemoveAllListeners();
            _replenishLawButton.onClick.AddListener(() => OnReplenishButtonClick?.Invoke());
        }

        public void UpdateBarVisuals(int newLawsCount, int maxLawsCount)
        {
            _lawsCountText.text = $"{newLawsCount}/{maxLawsCount}";
            float barValue = (float)newLawsCount / maxLawsCount;

            _lawsCountBar.DOKill();
            _lawsCountBar.DOValue(barValue, _countBarAnimationDuration);

            bool isFullyReplenished = newLawsCount >= maxLawsCount;
            _replenishLawButton.image.sprite = isFullyReplenished ? _unactiveButtonSprite : _activeButtonSprite;
            if (isFullyReplenished) UpdateTimerVisuals(0);
        }

        public void UpdateTimerVisuals(int seconds)
        {
            bool shouldBeVisible = seconds > 0;

            if (_timeToNextLawReplenishText.gameObject.activeSelf != shouldBeVisible)
            {
                _timeToNextLawReplenishText.gameObject.SetActive(shouldBeVisible);
                _timerIcon.gameObject.SetActive(shouldBeVisible);
            }

            if (!shouldBeVisible) return;

            int m = seconds / 60;
            int s = seconds % 60;
            _timeToNextLawReplenishText.text = $"{m:00}:{s:00}";
        }
    }
}