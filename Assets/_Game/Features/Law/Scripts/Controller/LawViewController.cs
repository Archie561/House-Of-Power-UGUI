using DG.Tweening;
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

        [Header("Refresh Info Panel")]
        [SerializeField] private TextMeshProUGUI _lawsCount;
        [SerializeField] private TextMeshProUGUI _timeToNextLawReplenish;
        [SerializeField] private Button _replenishLawButton;

        [Header("Law Panel")]
        [SerializeField] private LawView _lawView;
        [SerializeField] private Button _confirmLawButton;
        [SerializeField] private Button _declineLawButton;
        [SerializeField] private float _lawAnimationDuration = 0.5f;

        private Dictionary<ResourceType, PolicyProgressView> _spawnedPolicies = new Dictionary<ResourceType, PolicyProgressView>();

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
            LawLogicController.Instance.OnLawsCountChanged += UpdateLawsCount;

            // Force Sync (Update UI to match current logic state immediately)
            InitializePoliciesProgress();
            if (LawLogicController.Instance.TryGetActiveLaw(out var activeLaw))
            {
                InitializeLaw(activeLaw);
                _lawView.gameObject.SetActive(true); // Animation is not needed here to prevent playing it every time the player switches to the Law screen. It will only play when a new law is executed
            }
            else
            {
                // show no law text
            }
        }

        private void UpdateTimer(int time)
        {
            _timeToNextLawReplenish.gameObject.SetActive(true);

            if (time < 0) time = 0;

            int m = time / 60;
            int s = time % 60;
            _timeToNextLawReplenish.text = $"{m:00}:{s:00}";
        }

        private void UpdateLawsCount(int count)
        {
            _lawsCount.text = $"{count}/8";
            if (count == 8)
            {
                _timeToNextLawReplenish.gameObject.SetActive(false);
            }
        }

        private void OnDisable()
        {
            if (LawLogicController.Instance == null) return;

            // Unsubscribe from events
            LawLogicController.Instance.OnPolicyAmountChanged -= RefreshPolicyProgress;
            LawLogicController.Instance.OnTimerTick -= UpdateTimer;
            LawLogicController.Instance.OnLawsCountChanged -= UpdateLawsCount;
        }

        #endregion

        #region View Initialization & Updates

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

        private void RefreshPolicyProgress(ResourceType type)
        {
            if (_spawnedPolicies.TryGetValue(type, out var progressView))
            {
                var data = LawLogicController.Instance.GetPolicyLevelData(type);
                progressView.UpdateView(data.level, data.currentXp, data.requiredXp);
            }
        }

        private void InitializeLaw(LawData data)
        {
            _lawView.Initialize(data.Type, data.Id);
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

            var rectTransform = _lawView.GetComponent<RectTransform>();

            rectTransform.DOAnchorPosX(targetX, _lawAnimationDuration).SetEase(Ease.OutQuart).OnComplete(() =>
            {
                _lawView.gameObject.SetActive(false);

                if (LawLogicController.Instance.TryGetActiveLaw(out var nextLaw))
                {
                    InitializeLaw(nextLaw);
                    PlayLawAppearingAnimation();
                }
                else
                {
                    // Show "No More Laws" popup
                }
            });
        }

        private void PlayLawAppearingAnimation()
        {
            _lawView.gameObject.SetActive(true);
            _lawView.transform.DOKill(complete: true);

            _lawView.transform.DOScale(Vector2.one, _lawAnimationDuration).From(Vector2.zero).SetEase(Ease.OutBack).OnComplete(() =>
            {
                _confirmLawButton.interactable = true;
                _declineLawButton.interactable = true;
            });
        }

        #endregion
    }
}
