using System;
using System.Collections.Generic;
using Game.Features.Law;

namespace Game.Features.Popup
{
    /// <summary>
    /// Observable Model for the Law Policies Popup.
    /// Holds the data and uses the Observer pattern (OnDataUpdated event) 
    /// to notify subscribers (Presenters/Containers) when data changes, 
    /// keeping the data logic decoupled from the UI.
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