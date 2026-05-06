using System;
using System.Collections.Generic;
using Game.Features.Law;

namespace Game.Features.Popup
{
    /// <summary>
    /// Data container for the Law Policies Popup.
    /// </summary>
    public class LawPoliciesPopupData
    {
        public List<DetailedPolicyProgressData> Policies { get; private set; }

        public LawPoliciesPopupData(List<DetailedPolicyProgressData> policies)
        {
            Policies = policies;
        }
    }
}