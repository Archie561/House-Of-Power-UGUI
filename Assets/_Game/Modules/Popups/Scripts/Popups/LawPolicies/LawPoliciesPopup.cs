using Game.General;
using Game.Features.Law;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Features.Popup
{
    /// <summary>
    /// View for the Law Policies Popup. Displays information about all policies, their levels and progress.
    /// Forwards results of user interactions via callbacks. Contains a method for updating the visuals of a single policy.
    /// It is recommended to subscribe to the policy change event in the popup's setup method (via PopupController)
    /// to keep the visuals updated without reinitializing the entire popup.
    /// </summary>
    public class LawPoliciesPopup : BasePopup
    {
        [Header("UI References")]
        [SerializeField] private Transform _contentContainer;
        [SerializeField] private DetailedPolicyProgressView _policyProgressPrefab;

        private Dictionary<ResourceType, DetailedPolicyProgressView> _spawnedPolicies = new();

        /// <summary>
        /// Configures the popup with all the policies that should be displayed, their current levels and progress.
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
                UpdatePolicyVisuals(policy.Type, policy.Level, policy.CurrentXp, policy.RequiredXp);
            }
        }

        /// <summary>
        /// Updates the visuals of a specific policy type without reinitializing the entire popup.
        /// </summary>
        public void UpdatePolicyVisuals(ResourceType type, int level, int currentXp, int requiredXp)
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