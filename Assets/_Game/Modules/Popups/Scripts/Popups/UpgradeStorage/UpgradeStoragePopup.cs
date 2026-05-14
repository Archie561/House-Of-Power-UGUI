using Game.General;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;

namespace Game.Features.Popup
{
    /// <summary>
    /// View for the Upgrade Storage Popup. Displays information about current and upgraded storage capacity, cost of the upgrade in resources and gems,
    /// affordability and buttons to confirm the purchase. Forwards results of user interactions via callbacks.
    /// </summary>
    public class UpgradeStoragePopup : BasePopup
    {
        [Header("UI References")]
        [SerializeField] private LocalizeStringEvent _descriptionLocalizer;
        [SerializeField] private TextMeshProUGUI _currentStorageCapacity;
        [SerializeField] private TextMeshProUGUI _upgradedStorageCapacity;
        [SerializeField] private PopupCostButton _resourceCostButton;
        [SerializeField] private PopupCostButton _gemCostButton;

        [Header("Config")]
        [SerializeField] private ResourceLibrary _resourceLibrary;

        /// <summary>
        /// Initializes the popup with current and next capacity values and cost options.
        /// </summary>
        public void Initialize(UpgradeStoragePopupData popupData)
        {
            if (popupData == null)
            {
                Debug.LogError("[UpgradeStoragePopup] Data is missing!");
                return;
            }

            // Localization
            var definition = _resourceLibrary.GetDef(popupData.StorageType);
            if (definition != null)
            {
                _descriptionLocalizer.StringReference.Arguments = new object[] { definition.LocalizedName.GetLocalizedString() };
                _descriptionLocalizer.RefreshString();
            }

            // Capacity Texts
            _currentStorageCapacity.text = popupData.CurrentCapacity.ToString();
            _upgradedStorageCapacity.text = popupData.UpgradedCapacity.ToString();

            // Initialize Buttons
            _resourceCostButton.Initialize(
                popupData.StorageType,
                popupData.DefaultCost,
                popupData.CanAffordDefault,
                popupData.OnDefaultClick
            );

            _gemCostButton.Initialize(
                ResourceType.Gems,
                popupData.PremiumCost,
                popupData.CanAffordPremium,
                popupData.OnPremiumClick
            );
        }
    }
}
