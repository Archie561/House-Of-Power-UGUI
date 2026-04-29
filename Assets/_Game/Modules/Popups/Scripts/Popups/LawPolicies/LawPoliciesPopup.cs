using System.Collections.Generic;
using Game.Features.Law;
using UnityEngine;

namespace Game.Features.Popup
{
    /// <summary>
    /// Container View / Presenter for the Law Policies Popup.
    /// Acts as a "Smart Component" that listens to the Observable Model.
    /// </summary>
    public class LawPoliciesPopup : BasePopup
    {
        [Header("UI References")]
        [SerializeField] private Transform _contentContainer;
        [SerializeField] private DetailedPolicyProgressView _policyProgressPrefab;

        private LawPoliciesPopupData _popupData;
        private List<DetailedPolicyProgressView> _spawnedPolicies = new List<DetailedPolicyProgressView>();

        /// <summary>
        /// Initializes the popup with policies data.
        /// </summary>
        /// <param name="data"></param>
        public void Initialize(LawPoliciesPopupData data)
        {
            if (_popupData != null) _popupData.OnDataUpdated -= RefreshList;

            _popupData = data;

            // Subscribe to data updates to refresh the list when policies change
            _popupData.OnDataUpdated += RefreshList;

            RefreshList();
        }

        private void OnDisable()
        {
            if (_popupData != null)
            {
                _popupData.OnDataUpdated -= RefreshList;
            }
        }

        private void RefreshList()
        {
            // Spawn all the policies if we haven't already (we keep them around and just update them for performance)
            if (_spawnedPolicies.Count == 0)
            {
                foreach (var policy in _popupData.Policies)
                {
                    var newItem = Instantiate(_policyProgressPrefab, _contentContainer);
                    _spawnedPolicies.Add(newItem);
                }
            }

            // Update all the policies with the latest data
            for (int i = 0; i < _popupData.Policies.Count; i++)
            {
                _spawnedPolicies[i].Initialize(_popupData.Policies[i]);
            }
        }
    }
}