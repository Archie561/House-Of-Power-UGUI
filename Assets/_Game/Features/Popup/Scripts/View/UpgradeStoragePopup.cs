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

    public void Initialize(UpgradeStorageData data, Action onDefaultCostClickCallback, Action onPremiumCostClickCallback)
    {
        //локалізоване імя ресурсу який буде апргрейдитись
        var definition = _resourceLibrary.GetDef(data.ResourceToUpgrade);
        if (definition != null)
        {
            _descriptionLocalizer.StringReference.Arguments = new object[] { definition.LocalizedName.GetLocalizedString() };
            _descriptionLocalizer.RefreshString();
        }

        _currentStorageCapacity.text = data.CurrentCapacity.ToString();
        _upgradedStorageCapacity.text = data.NextCapacity.ToString();

        _resourceCostButton.Initialize(data.DefaultCostType, data.DefaultCostAmount, onDefaultCostClickCallback);
        _gemCostButton.Initialize(ResourceType.Gems, data.PremiumCostAmount, onPremiumCostClickCallback);
    }
}
