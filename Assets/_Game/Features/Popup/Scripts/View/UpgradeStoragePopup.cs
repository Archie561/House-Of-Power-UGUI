using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;

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

    public void Initialize(UpgradeStoragePopupData popupData)
    {
        var definition = _resourceLibrary.GetDef(popupData.UpgradeData.StorageType);
        if (definition != null)
        {
            _descriptionLocalizer.StringReference.Arguments = new object[] { definition.LocalizedName.GetLocalizedString() };
            _descriptionLocalizer.RefreshString();
        }

        _currentStorageCapacity.text = popupData.UpgradeData.CurrentCapacity.ToString();
        _upgradedStorageCapacity.text = popupData.UpgradeData.NextCapacity.ToString();

        _resourceCostButton.Initialize(
            popupData.UpgradeData.StorageType,
            popupData.UpgradeData.DefaultCostAmount,
            popupData.CanAffordDefault,
            popupData.OnDefaultClick
        );

        _gemCostButton.Initialize(
            ResourceType.Gems,
            popupData.UpgradeData.PremiumCostAmount,
            popupData.CanAffordPremium,
            popupData.OnPremiumClick
        );
    }
}
