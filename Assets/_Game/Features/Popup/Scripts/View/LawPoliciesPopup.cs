using System;
using System.Collections.Generic;
using Game.Features.Law;
using UnityEngine;

namespace Game.Features.Popup
{
    /// <summary>
    /// Data class for law policies popup. Contains all necessary information to display the popup, such as policy type, current level, progress, etc.
    /// </summary>
    public class LawPoliciesPopup : BasePopup
    {
        [Header("UI References")]
        [SerializeField] private Transform _contentContainer;
        [SerializeField] private DetailedPolicyProgressView _policyProgressPrefab;

        private LawPoliciesPopupData _popupData;
        private List<DetailedPolicyProgressView> _spawnedPolicies = new List<DetailedPolicyProgressView>();

        public void Initialize(LawPoliciesPopupData data)
        {
            if (_popupData != null) _popupData.OnDataUpdated -= RefreshList;

            _popupData = data;
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
            // 1. Створюємо префаби тільки один раз
            if (_spawnedPolicies.Count == 0)
            {
                foreach (var policy in _popupData.Policies)
                {
                    var newItem = Instantiate(_policyProgressPrefab, _contentContainer);
                    _spawnedPolicies.Add(newItem);
                }
            }

            // 2. Оновлюємо дані (передаємо їх у PolicyDetailItemView))
            for (int i = 0; i < _popupData.Policies.Count; i++)
            {
                _spawnedPolicies[i].Initialize(_popupData.Policies[i]);
            }
        }
    }
}