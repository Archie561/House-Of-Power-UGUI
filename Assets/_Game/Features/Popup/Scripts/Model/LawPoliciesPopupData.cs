using System;
using System.Collections.Generic;
using Game.Features.Law;
using Game.General;
using UnityEngine.Localization.Tables;

namespace Game.Features.Popup
{
    /// <summary>
    /// Data class for law policies popup. Contains all necessary information to display the popup, such as policy type, current level, progress, etc.
    /// </summary>
    [Serializable]
    public class LawPoliciesPopupData
    {
        public List<DetailedPolicyProgressData> Policies { get; private set; }
        public Action OnDataUpdated {get; set; }

        public LawPoliciesPopupData(List<DetailedPolicyProgressData> policies)
        {
            Policies = policies;
        }

        public void UpdatePolicies(List<DetailedPolicyProgressData> updatedPolicies)
        {
            Policies = updatedPolicies;
            OnDataUpdated?.Invoke();
        }
    }
}