using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TradeOfferView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button _cardButton;
    [SerializeField] private Image _flagIcon;
    [SerializeField] private Transform _importTransform;
    [SerializeField] private Transform _exportTransform;

    [Header("Config")]
    [SerializeField] private CountryLibrary _countryLibrary;
    [SerializeField] private ResourceAmountView _ResourceAmountPrefab;

    public void Initialize(TradeOfferData data, Action<TradeOfferData> onOfferClickCallback)
    {
        var definition = _countryLibrary.GetDef(data.CountryId);

        if (definition != null)
        {
            _flagIcon.sprite = definition.FlagIcon;
        }

        AddResourcesView(_importTransform, data.Import, true);
        AddResourcesView(_exportTransform, data.Export, false);

        _cardButton.onClick.RemoveAllListeners();
        _cardButton.onClick.AddListener(() => onOfferClickCallback?.Invoke(data));
    }

    private void AddResourcesView(Transform parent, List<ResourceData> resources, bool isImport)
    {
        foreach (var resource in resources)
        {
            var resourceView = Instantiate(_ResourceAmountPrefab, parent);
            resourceView.Initialize(resource, isImport);
        }
    }
}