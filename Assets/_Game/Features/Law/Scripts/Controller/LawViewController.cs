using Game.General;
using System;
using System.Collections.Generic;
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

        [Header("Law Panel")]
        [SerializeField] private LawView _lawView;
        [SerializeField] private Button _confirmLawButton;
        [SerializeField] private Button _declineLawButton;

        private Dictionary<ResourceType, PolicyProgressView> _spawnedPolicies = new Dictionary<ResourceType, PolicyProgressView>();

        #region Unity Lifecycle

        private void Start()
        {
            _confirmLawButton.onClick.AddListener(OnAcceptLawClicked);
            _declineLawButton.onClick.AddListener(OnDeclineLawClicked);
        }

        private void OnEnable()
        {
            if (LawLogicController.Instance == null) return;

            // Subscribe to events
            LawLogicController.Instance.OnPolicyAmountChanged += RefreshPolicyProgress;

            // Force Sync (Update UI to match current logic state immediately)
            InitializePoliciesProgress();
            InitializeLaw(LawLogicController.Instance.GetActiveLaw());

        }

        private void OnDisable()
        {
            if (LawLogicController.Instance == null) return;

            // Unsubscribe from events
            LawLogicController.Instance.OnPolicyAmountChanged -= RefreshPolicyProgress;
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
                int level = LawLogicController.Instance.GetPolicyLevel(type);
                int amount = LawLogicController.Instance.GetCalculatedPolicyAmount(type);
                int maxValue = LawLogicController.Instance.GetCalculatedPolicyMaxValue(type);
                progressView.UpdateView(level, amount, maxValue);
            }
        }

        private void InitializeLaw(LawData data)
        {
            _lawView.Initialize(data.Type, data.Id);
        }

        #endregion

        #region User Interaction Handlers

        private void OnAcceptLawClicked()
        {
            Debug.Log("Law accepted!");
        }

        private void OnDeclineLawClicked()
        {
            Debug.Log("Law declined!");
        }

        #endregion
    }
}
