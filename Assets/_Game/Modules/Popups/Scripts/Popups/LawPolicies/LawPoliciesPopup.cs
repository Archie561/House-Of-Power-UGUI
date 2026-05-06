using System.Collections.Generic;
using Game.Features.Law;
using Game.General;
using UnityEngine;

namespace Game.Features.Popup
{
    /// <summary>
    /// View for the Law Policies Popup. Displays data and forwards results of user interactions via callbacks.
    /// </summary>
    public class LawPoliciesPopup : BasePopup
    {
        [Header("UI References")]
        [SerializeField] private Transform _contentContainer;
        [SerializeField] private DetailedPolicyProgressView _policyProgressPrefab;

        private Dictionary<ResourceType, DetailedPolicyProgressView> _spawnedPolicies = new();

        /// <summary>
        /// Configures the popup with the specific data.
        /// </summary>
        public void Initialize(LawPoliciesPopupData data)
        {
            foreach (var policy in data.Policies)
            {
                // If we haven't spawned a view for this policy type yet, do it now.
                if (!_spawnedPolicies.ContainsKey(policy.Type))
                {
                    var newItem = Instantiate(_policyProgressPrefab, _contentContainer);

                    newItem.Initialize(policy);

                    _spawnedPolicies.Add(policy.Type, newItem);
                }

                // Update the visuals for this policy's view with the current data.
                UpdateVisuals(policy.Type, policy.Level, policy.CurrentXp, policy.RequiredXp);
            }
        }

        public void UpdateVisuals(ResourceType type, int level, int currentXp, int requiredXp)
        {
            if (_spawnedPolicies.TryGetValue(type, out var view))
            {
                view.UpdateVisuals(level, currentXp, requiredXp);
            }
            else
            {
                Debug.LogWarning($"[LawPoliciesPopup] Trying to update visuals for policy type {type} but no view is spawned for it.");
            }
        }
    }
}