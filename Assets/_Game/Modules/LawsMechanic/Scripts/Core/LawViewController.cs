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

        [Header("Laws count bar")]
        [SerializeField] private LawsCountBarView _lawsCountBar;

        [Header("Law Panel")]
        [SerializeField] private LawCardView _lawCard;
        [SerializeField] private TextMeshProUGUI _noLawsText;

        private Dictionary<ResourceType, PolicyProgressView> _spawnedPolicies = new Dictionary<ResourceType, PolicyProgressView>();

        #region Unity Lifecycle

        private void Start()
        {
            _policiesPanelButton.onClick.RemoveAllListeners();
            _policiesPanelButton.onClick.AddListener(OnPoliciesPanelClicked);
        }
    
        private void OnEnable()
        {
            if (LawLogicController.Instance == null) return;

            // Subscribe to events
            LawLogicController.Instance.OnPolicyAmountChanged += RefreshPolicyProgress;
            LawLogicController.Instance.OnLawsCountChanged += HandleLawsCountChange;
            LawLogicController.Instance.OnTimerSecondsTick += _lawsCountBar.UpdateTimerVisuals;
            _lawCard.OnSwipeDecided += HandleSwipeDecision;
            _lawCard.OnHideAnimationFinished += HandleLawAnimationFinished;
            _lawsCountBar.OnReplenishButtonClick += HandleReplenishButtonClick;

            // Force Sync (Update UI to match current logic state immediately)
            if (LawLogicController.Instance.IsDataReady) RestoreViewState();
            else LawLogicController.Instance.OnDataReady += RestoreViewState;

        }

        private void OnDisable()
        {
            if (LawLogicController.Instance == null) return;

            // Unsubscribe from events
            LawLogicController.Instance.OnPolicyAmountChanged -= RefreshPolicyProgress;
            LawLogicController.Instance.OnLawsCountChanged -= HandleLawsCountChange;
            LawLogicController.Instance.OnDataReady -= RestoreViewState;
            LawLogicController.Instance.OnTimerSecondsTick -= _lawsCountBar.UpdateTimerVisuals;
            _lawCard.OnSwipeDecided -= HandleSwipeDecision;
            _lawCard.OnHideAnimationFinished -= HandleLawAnimationFinished;
            _lawsCountBar.OnReplenishButtonClick -= HandleReplenishButtonClick;
        }

        #endregion

        #region View Initialization & Updates

        // Restores the view to match the current state of the logic when enabled
        private void RestoreViewState()
        {
            InitializePoliciesProgress();
            ShowLawCard(playAnimation: false);
            _lawsCountBar.UpdateBarVisuals(LawLogicController.Instance.CurrentLawsCount, LawLogicController.Instance.MaxLawsCount);
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
                var data = LawLogicController.Instance.GetPolicyProgressData(type);
                progressView.UpdateVisuals(data.Level, data.CurrentXp, data.RequiredXp);
            }
        }

        // Shows law card if possible, otherwise, shows "No laws available" text
        private void ShowLawCard(bool playAnimation = true)
        {
            // If there is law to show, display it
            if (LawLogicController.Instance.TryGetActiveLaw(out var nextLaw))
            {
                _noLawsText.gameObject.SetActive(false);
                _lawCard.Show(nextLaw, playAnimation);
            }
            // Otherwise, ensure the view is in the correct state with no law displayed
            else
            {
                _lawCard.gameObject.SetActive(false);
                _noLawsText.gameObject.SetActive(true);
            }
        }

        // Updates the replenish panel on laws count change
        private void HandleLawsCountChange(int newLawsCount)
        {
            _lawsCountBar.UpdateBarVisuals(newLawsCount, LawLogicController.Instance.MaxLawsCount);

            // If law replenished while the _lawCard is not active, show the new law immediately
            if (newLawsCount > 0 && !_lawCard.gameObject.activeSelf) ShowLawCard();
        }

        private void HandleLawAnimationFinished() => ShowLawCard();

        #endregion

        #region User Interaction Handlers

        // Notifies the logic module whether a law has been accepted or rejected and handles the UI behavior
        private void HandleSwipeDecision(bool accepted)
        {
            LawLogicController.Instance.ExecuteActiveLaw(accepted);
        }

        // Handles the logic for when the replenish button is clicked, including showing the confirmation popup
        // and updating it if the laws count changes while it's open
        private void HandleReplenishButtonClick()
        {
            if (LawLogicController.Instance.CurrentLawsCount >= LawLogicController.Instance.MaxLawsCount)
                return;

            var replenishData = LawLogicController.Instance.GetLawsReplenishData();
            var canAfford = LawLogicController.Instance.CanAfford(ResourceType.Gems, replenishData.TotalCost);

            var data = new ReplenishLawsPopupData(
                targetTime: replenishData.TargetTime,
                costType: ResourceType.Gems,
                costAmount: replenishData.TotalCost,
                canAfford: canAfford,
                onConfirmClick: () =>
                {
                    LawLogicController.Instance.TryReplenishLaws();
                    PopupController.Instance.CloseCurrentPopup();
                }
            );

            PopupController.Instance.Show<ReplenishLawsPopup>(popup =>
            {
                popup.Initialize(data);
                
                // Subscribe to laws count changes to update the popup visuals if the player replenishes laws through other means while the popup is open
                void OnLawsCountChanged(int newCount)
                {
                    if (newCount >= LawLogicController.Instance.MaxLawsCount)
                    {
                        PopupController.Instance.CloseCurrentPopup();
                    }
                    else
                    {
                        var newCostAmount = LawLogicController.Instance.GetLawsReplenishData().TotalCost;
                        var newCanAfford = LawLogicController.Instance.CanAfford(ResourceType.Gems, newCostAmount);
                        popup.UpdateCostVisuals(newCostAmount, newCanAfford);
                    }
                }

                LawLogicController.Instance.OnLawsCountChanged += OnLawsCountChanged;

                // Unsubscribe when the popup is closed to prevent memory leaks
                popup.OnPopupClosed += () => LawLogicController.Instance.OnLawsCountChanged -= OnLawsCountChanged;
            });
        }

        // Handles the logic for when the policies panel is clicked, showing the policies popup with the current policies data
        private void OnPoliciesPanelClicked()
        {
            var policyDatas = new List<DetailedPolicyProgressData>();
            var policies = LawLogicController.Instance.GetPlayerPolicies();

            foreach (var policy in policies)
            {
                var progressData = LawLogicController.Instance.GetPolicyProgressData(policy.Type);
                var itemData = new DetailedPolicyProgressData(
                    policy.Type,
                    progressData.Level,
                    progressData.CurrentXp,
                    progressData.RequiredXp,
                    onBuyClick: () => OpenUpgradePolicyPopup(policy.Type)
                );

                policyDatas.Add(itemData);
            }

            var popupData = new LawPoliciesPopupData(policyDatas);

            PopupController.Instance.Show<LawPoliciesPopup>(popup =>
            {
                popup.Initialize(popupData);

                void OnPolicyAmountChanged(ResourceType type)
                {
                    var policyData = LawLogicController.Instance.GetPolicyProgressData(type);
                    popup.UpdatePolicyVisuals(type, policyData.Level, policyData.CurrentXp, policyData.RequiredXp);
                }

                LawLogicController.Instance.OnPolicyAmountChanged += OnPolicyAmountChanged;

                popup.OnPopupClosed += () => LawLogicController.Instance.OnPolicyAmountChanged -= OnPolicyAmountChanged;
            });
        }

        // Opens the upgrade policy popup for a specific policy type, allowing the player to spend resources to upgrade it
        private void OpenUpgradePolicyPopup(ResourceType type)
        {
            var gemPrice = LawLogicController.Instance.GetPolicyProgressData(type).UpgradeCostGems;

            var data = new UpgradePolicyPopupData(
                type,
                gemPrice,
                LawLogicController.Instance.CanAfford(ResourceType.Gems, gemPrice),
                onBuyClick: () =>
                {
                    LawLogicController.Instance.TryUpgradePolicy(type);
                    PopupController.Instance.CloseCurrentPopup();
                });

            PopupController.Instance.Show<UpgradePolicyPopup>(popup => popup.Initialize(data));
        }

        #endregion
    }
}