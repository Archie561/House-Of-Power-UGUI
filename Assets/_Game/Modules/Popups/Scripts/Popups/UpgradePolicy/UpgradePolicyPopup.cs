using System;
using Game.General;
using UnityEngine;
using UnityEngine.Localization.Components;

namespace Game.Features.Popup
{
    /// <summary>
    /// Passive View for the Upgrade Policy Popup. Displays data and forwards results of user interactions via callbacks.
    /// </summary>
    public class UpgradePolicyPopup : BasePopup
    {
        [Header("UI References")]
        [SerializeField] private LocalizeStringEvent _descriptionLocalizer;
        [SerializeField] private PopupCostButton _buyButton;

        [Header("Config")]
        [SerializeField] private ResourceLibrary _resourceLibrary;
        
        /// <summary>
        /// Initializes the popup with cost and description data.
        /// </summary>
        /// <param name="data"></param>
        public void Initialize(UpgradePolicyPopupData data)
        {
            if (data == null)
            {
                Debug.LogError("[UpgradePolicyPopup] Data is missing!");
                return;
            }

            var definition = _resourceLibrary.GetDef(data.Type);
            if (definition != null)
            {
                _descriptionLocalizer.StringReference.Arguments = new object[] { definition.LocalizedName.GetLocalizedString() };
                _descriptionLocalizer.RefreshString();
            }

            _buyButton.Initialize(ResourceType.Gems, data.GemPrice, data.CanAfford, data.OnBuyClick);
        }
    }
}