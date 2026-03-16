using DG.Tweening;
using Game.Features.Trade;
using Game.General;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Features.Law
{
    /// <summary>
    /// Manages the user interface for displaying and interacting with laws.
    /// Acts as an Orchestrator: listens to Logic events and updates the View, 
    /// and maps View input events to Logic commands or Popups.
    /// </summary>
    public class LawViewController : MonoBehaviour
    {
        [Header("Policies Panel")]
        [SerializeField] private PolicyProgressView _policyProgressPrefab;
        [SerializeField] private Transform _policiesContainer;

        [Header("Refresh Info Panel")]
        [SerializeField] private TextMeshProUGUI _lawsCount;
        [SerializeField] private TextMeshProUGUI _timeToNextLawReplenish;
        [SerializeField] private Image _timerIcon;
        [SerializeField] private Button _replenishLawButton;
        [SerializeField] private Sprite _activeButtonSprite;
        [SerializeField] private Sprite _unactiveButtonSprite;
        [SerializeField] private Slider _lawsCountBar;
        [SerializeField] private float _countBarAnimationDuration = 0.5f;

        [Header("Law Panel")]
        [SerializeField] private LawView _lawView;
        [SerializeField] private RectTransform _lawViewRect;
        [SerializeField] private Button _confirmLawButton;
        [SerializeField] private Button _declineLawButton;
        [SerializeField] private float _lawAnimationDuration = 0.5f;

        private Dictionary<ResourceType, PolicyProgressView> _spawnedPolicies = new Dictionary<ResourceType, PolicyProgressView>();
        private int _maxLawsCount; // Cache max laws count for quick access

        #region Unity Lifecycle

        private void Start()
        {
            _confirmLawButton.onClick.RemoveAllListeners();
            _declineLawButton.onClick.RemoveAllListeners();

            _confirmLawButton.onClick.AddListener(() => OnLawExecuted(true));
            _declineLawButton.onClick.AddListener(() => OnLawExecuted(false));
        }

        private void OnEnable()
        {
            if (LawLogicController.Instance == null) return;

            // Subscribe to events
            LawLogicController.Instance.OnPolicyAmountChanged += RefreshPolicyProgress;
            LawLogicController.Instance.OnTimerTick += UpdateTimer;
            LawLogicController.Instance.OnLawsCountChanged += RefreshReplenishPanel;

            // Force Sync (Update UI to match current logic state immediately)
            InitializePoliciesProgress();
            RefreshReplenishPanel(LawLogicController.Instance.GetCurrentLawsCount());

            if (LawLogicController.Instance.TryGetActiveLaw(out var activeLaw))
            {
                ShowLaw(activeLaw, playAnimation: false);
            }
            else
            {
                // show no law text
                _lawView.gameObject.SetActive(false);
            }
        }

        private void OnDisable()
        {
            if (LawLogicController.Instance == null) return;

            // Unsubscribe from events
            LawLogicController.Instance.OnPolicyAmountChanged -= RefreshPolicyProgress;
            LawLogicController.Instance.OnTimerTick -= UpdateTimer;
            LawLogicController.Instance.OnLawsCountChanged -= RefreshReplenishPanel;
        }

        #endregion

        #region View Initialization & Updates

        // Initializes static policy progress values
        private void InitializePoliciesProgress()
        {
            var policies = LawLogicController.Instance.GetPlayerPolicies();

            foreach (var policy in policies)
            {
                if (!_spawnedPolicies.ContainsKey(policy.Type))
                {
                    var progressView = Instantiate(_policyProgressPrefab, _policiesContainer);
                    progressView.Initialize(policy.Type);
                    _spawnedPolicies.Add(policy.Type, progressView);
                }

                RefreshPolicyProgress(policy.Type);
            }
        }

        // Updates the progress of a specific policy on change
        private void RefreshPolicyProgress(ResourceType type)
        {
            if (_spawnedPolicies.TryGetValue(type, out var progressView))
            {
                var data = LawLogicController.Instance.GetPolicyLevelData(type);
                progressView.UpdateView(data.level, data.currentXp, data.requiredXp);
            }
        }

        // Updates the replenish panel with the current laws count and timer
        private void RefreshReplenishPanel(int newLawsCount)
        {
            if (_maxLawsCount == 0) _maxLawsCount = LawLogicController.Instance.GetMaxLawsCount();

            _lawsCount.text = $"{newLawsCount}/{_maxLawsCount}";
            float barValue = (float)newLawsCount / _maxLawsCount;
            _lawsCountBar.DOKill();
            _lawsCountBar.DOValue(barValue, _countBarAnimationDuration);

            _replenishLawButton.image.sprite = newLawsCount < _maxLawsCount ? _activeButtonSprite : _unactiveButtonSprite;

            if (newLawsCount == 1)
            {
                if (LawLogicController.Instance.TryGetActiveLaw(out var activeLaw))
                {
                    ShowLaw(activeLaw);
                }
            }
        }

        private void UpdateTimer(int time)
        {
            bool shouldBeVisible = time > 0;

            if (_timeToNextLawReplenish.gameObject.activeSelf != shouldBeVisible)
            {
                _timeToNextLawReplenish.gameObject.SetActive(shouldBeVisible);
                _timerIcon.gameObject.SetActive(shouldBeVisible);
            }

            if (!shouldBeVisible) return;

            int m = time / 60;
            int s = time % 60;
            _timeToNextLawReplenish.text = $"{m:00}:{s:00}";
        }

        private void ShowLaw(LawData data, bool playAnimation = true)
        {
            _lawView.Initialize(data.Type, data.Id);
            _lawView.gameObject.SetActive(true);

            _confirmLawButton.interactable = true;
            _declineLawButton.interactable = true;

            _lawViewRect.anchoredPosition = Vector2.zero;

            if (!playAnimation)
            {
                _lawViewRect.localScale = Vector3.one;
                return;
            }

            _lawView.transform.DOKill(complete: true);
            _lawView.transform.DOScale(Vector2.one, _lawAnimationDuration).From(Vector2.zero).SetEase(Ease.OutBack);
        }

        #endregion

        #region User Interaction Handlers

        private void OnLawExecuted(bool accepted)
        {
            _confirmLawButton.interactable = false;
            _declineLawButton.interactable = false;
            _lawView.transform.DOKill(complete: true);

            LawLogicController.Instance.ExecuteActiveLaw(accepted);

            float targetX = accepted ? Screen.width : -Screen.width;

            _lawViewRect.DOAnchorPosX(targetX, _lawAnimationDuration)
                .SetEase(Ease.OutQuart)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(() =>
                {
                    if (LawLogicController.Instance.TryGetActiveLaw(out var nextLaw))
                    {
                        ShowLaw(nextLaw);
                    }
                    else
                    {
                        // Show No Law Text
                        _lawView.gameObject.SetActive(false);
                    }
                });
        }

        #endregion
    }
}
