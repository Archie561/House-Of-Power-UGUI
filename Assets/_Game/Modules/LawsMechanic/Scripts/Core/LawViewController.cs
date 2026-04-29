using DG.Tweening;
using Game.Features.Popup;
using Game.General;
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
        [SerializeField] private Button _policiesPanelButton;

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
        [SerializeField] private TextMeshProUGUI _noLawsText;
        [SerializeField] private Button _confirmLawButton;
        [SerializeField] private Button _declineLawButton;
        [SerializeField] private float _lawAnimationDuration = 0.5f;

        private Dictionary<ResourceType, PolicyProgressView> _spawnedPolicies = new Dictionary<ResourceType, PolicyProgressView>();
        private ReplenishLawsPopupData _currentReplenishPopupData; // Cache current popup data to update it when laws count changes
        private LawPoliciesPopupData _currentPoliciesPopupData;
        private int _maxLawsCount; // Cache max laws count for quick access

        #region Unity Lifecycle

        private void Start()
        {
            _confirmLawButton.onClick.RemoveAllListeners();
            _declineLawButton.onClick.RemoveAllListeners();
            _replenishLawButton.onClick.RemoveAllListeners();
            _policiesPanelButton.onClick.RemoveAllListeners();

            _confirmLawButton.onClick.AddListener(() => OnLawExecuted(true));
            _declineLawButton.onClick.AddListener(() => OnLawExecuted(false));
            _replenishLawButton.onClick.AddListener(OnReplenishClicked);
            _policiesPanelButton.onClick.AddListener(OnPoliciesPanelClicked);
        }

        private void OnEnable()
        {
            if (LawLogicController.Instance == null) return;

            // Subscribe to events
            LawLogicController.Instance.OnPolicyAmountChanged += RefreshPolicyProgress;
            LawLogicController.Instance.OnTimerTick += UpdateTimer;
            LawLogicController.Instance.OnLawsCountChanged += RefreshLawsCount;

            RestoreViewState();
            
        }

        private void OnDisable()
        {
            if (LawLogicController.Instance == null) return;

            // Unsubscribe from events
            LawLogicController.Instance.OnPolicyAmountChanged -= RefreshPolicyProgress;
            LawLogicController.Instance.OnTimerTick -= UpdateTimer;
            LawLogicController.Instance.OnLawsCountChanged -= RefreshLawsCount;
        }

        #endregion

        #region View Initialization & Updates

        // Restores the view to match the current state of the logic when enabled
        private void RestoreViewState()
        {
            InitializePoliciesProgress();
            RefreshLawsCount(LawLogicController.Instance.GetCurrentLawsCount());

            // If there is law to show, display it
            if (LawLogicController.Instance.TryGetActiveLaw(out var activeLaw))
            {
                ShowLaw(activeLaw, playAnimation: false);
            }
            // Otherwise, ensure the view is in the correct state with no law displayed
            else
            {
                _lawView.gameObject.SetActive(false);
                _noLawsText.gameObject.SetActive(true);
            }
        }

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

        // Updates the replenish panel on laws count change
        private void RefreshLawsCount(int newLawsCount)
        {
            // if _maxLawsCount is not initialized yet, get it from the logic controller and cache it
            if (_maxLawsCount == 0) _maxLawsCount = LawLogicController.Instance.GetMaxLawsCount();

            _lawsCount.text = $"{newLawsCount}/{_maxLawsCount}";
            float barValue = (float)newLawsCount / _maxLawsCount;

            _lawsCountBar.DOKill();
            _lawsCountBar.DOValue(barValue, _countBarAnimationDuration);

            _replenishLawButton.image.sprite = newLawsCount < _maxLawsCount ? _activeButtonSprite : _unactiveButtonSprite;

            // Update the popup data if it's open and the laws count has changed (e.g., replenished while popup was open)
            if (_currentReplenishPopupData != null && newLawsCount < _maxLawsCount)
            {
                var totalCost = LawLogicController.Instance.GetTotalLawsReplenishCost();
                var canAfford = LawLogicController.Instance.CanAfford(ResourceType.Gems, totalCost);

                _currentReplenishPopupData.UpdateCost(totalCost, canAfford);
            }

            // If law replenished while the _lawView is not active, show the new law immediately
            if (!_lawView.gameObject.activeSelf && LawLogicController.Instance.TryGetActiveLaw(out var activeLaw))
                ShowLaw(activeLaw);

            // If we have reached max laws, ensure the timer is hidden
            if (newLawsCount >= _maxLawsCount)
            {
                UpdateTimer(0);
            }
        }

        // Updates the timer display for the next law replenishment
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

        // Displays a new law with an optional animation
        private void ShowLaw(LawData data, bool playAnimation = true)
        {
            _lawView.Initialize(data.Type, data.Id);

            if (!_lawView.gameObject.activeSelf) _lawView.gameObject.SetActive(true);
            if (_noLawsText.gameObject.activeSelf) _noLawsText.gameObject.SetActive(false);

            _lawViewRect.DOKill(complete: true);
            _lawViewRect.anchoredPosition = Vector2.zero;

            if (!playAnimation)
            {
                _lawViewRect.localScale = Vector3.one;
                _confirmLawButton.interactable = true;
                _declineLawButton.interactable = true;

                return;
            }

            _lawViewRect.DOScale(Vector2.one, _lawAnimationDuration)
                .From(Vector2.zero)
                .SetEase(Ease.OutBack)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(() =>
                {
                    _confirmLawButton.interactable = true;
                    _declineLawButton.interactable = true;
                });
        }

        #endregion

        #region User Interaction Handlers

        // Notifies the logic module whether a law has been accepted or rejected and handles the UI behavior
        private void OnLawExecuted(bool accepted)
        {
            _confirmLawButton.interactable = false;
            _declineLawButton.interactable = false;

            LawLogicController.Instance.ExecuteActiveLaw(accepted);

            RectTransform parentRect = (RectTransform)_lawViewRect.parent;
            // Taking half the width of the parent + half the width of the form itself,
            // so that it is guaranteed to hide behind the edge (adding a multiplier of 1.2f for a small margin)
            float offScreenOffset = (parentRect.rect.width / 2f + _lawViewRect.rect.width / 2f) * 1.2f;

            float targetX = accepted ? offScreenOffset : -offScreenOffset;

            _lawViewRect.DOKill(complete: true);
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
                        _lawView.gameObject.SetActive(false);
                        _noLawsText.gameObject.SetActive(true);
                    }
                });
        }

        // Handles the logic for when the replenish button is clicked, including showing the confirmation popup
        // and updating it if the laws count changes while it's open
        private void OnReplenishClicked()
        {
            if (LawLogicController.Instance.GetCurrentLawsCount() >= _maxLawsCount) return;

            var targetTime = LawLogicController.Instance.GetTotalLawsReplenishTime();
            var totalCost = LawLogicController.Instance.GetTotalLawsReplenishCost();
            var canAfford = LawLogicController.Instance.CanAfford(ResourceType.Gems, totalCost);

            _currentReplenishPopupData = new ReplenishLawsPopupData(
                targetTime,
                totalCost,
                canAfford,
                onConfirmClick: () =>
                {
                    LawLogicController.Instance.TryReplenishLaws();
                    PopupController.Instance.CloseCurrentPopup();
                    _currentReplenishPopupData = null;
                },
                onTimerVisuallyFinished: () =>
                {
                    PopupController.Instance.CloseCurrentPopup();
                    _currentReplenishPopupData = null;
                });

            PopupController.Instance.Show<ReplenishLawsPopup>(popup => popup.Initialize(_currentReplenishPopupData));
        }

        // Handles the logic for when the policies panel is clicked, showing the policies popup with the current policies data
        private void OnPoliciesPanelClicked()
        {
            _currentPoliciesPopupData = new LawPoliciesPopupData(GeneratePoliciesList());
            PopupController.Instance.Show<LawPoliciesPopup>(popup => popup.Initialize(_currentPoliciesPopupData));
        }

        // Generates a list of policy progress data for all player policies, used to populate the policies popup
        private List<DetailedPolicyProgressData> GeneratePoliciesList()
        {
            var policyDatas = new List<DetailedPolicyProgressData>();
            var policies = LawLogicController.Instance.GetPlayerPolicies();

            foreach (var policy in policies)
            {
                var levelData = LawLogicController.Instance.GetPolicyLevelData(policy.Type);
                var itemData = new DetailedPolicyProgressData(
                    policy.Type,
                    levelData.level,
                    levelData.currentXp,
                    levelData.requiredXp,
                    onBuyClick: () => OpenUpgradePolicyPopup(policy.Type)
                );

                policyDatas.Add(itemData);
            }

            return policyDatas;
        }

        // Opens the upgrade policy popup for a specific policy type, allowing the player to spend resources to upgrade it
        private void OpenUpgradePolicyPopup(ResourceType type)
        {
            var gemPrice = LawLogicController.Instance.GetPolicyXpUpgradeCost(type);
            var data = new UpgradePolicyPopupData(
                type,
                gemPrice,
                LawLogicController.Instance.CanAfford(ResourceType.Gems, gemPrice),
                onBuyClick: () =>
                {
                    LawLogicController.Instance.TryUpgradePolicy(type);
                    PopupController.Instance.CloseCurrentPopup();

                    if (_currentPoliciesPopupData != null)
                    {
                        _currentPoliciesPopupData.UpdatePolicies(GeneratePoliciesList());
                    }
                });

            PopupController.Instance.Show<UpgradePolicyPopup>(popup => popup.Initialize(data));
        }

        #endregion
    }
}
