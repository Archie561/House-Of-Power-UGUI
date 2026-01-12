using Game.General;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;

namespace Game.Features.Popup
{
    /// <summary>
    /// Popup that allows the player to upgrade resource storage capacity using different currencies.
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
        /// Sets up the popup with current and next capacity values and cost options.
        /// </summary>
        public void Initialize(UpgradeStoragePopupData popupData)
        {
            if (popupData?.UpgradeData == null)
            {
                Debug.LogError("[UpgradeStoragePopup] Data is missing!");
                return;
            }

            var data = popupData.UpgradeData;

            // Localization
            var definition = _resourceLibrary.GetDef(data.StorageType);
            if (definition != null)
            {
                _descriptionLocalizer.StringReference.Arguments = new object[] { definition.LocalizedName.GetLocalizedString() };
                _descriptionLocalizer.RefreshString();
            }

            // Capacity Texts
            _currentStorageCapacity.text = data.CurrentCapacity.ToString();
            _upgradedStorageCapacity.text = data.NextCapacity.ToString();

            // Initialize Buttons
            _resourceCostButton.Initialize(
                data.StorageType,
                data.DefaultCostAmount,
                popupData.CanAffordDefault,
                popupData.OnDefaultClick
            );

            _gemCostButton.Initialize(
                ResourceType.Gems,
                data.PremiumCostAmount,
                popupData.CanAffordPremium,
                popupData.OnPremiumClick
            );
        }
    }
}
