using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

public class AcceptOfferPopup : BasePopup
{
    [Header("UI References")]
    [SerializeField] private LocalizeStringEvent _descriptionLocalizer;
    [SerializeField] private Transform _exportTransform;
    [SerializeField] private Transform _importTransform;
    [SerializeField] private Button _acceptButton;
    [SerializeField] private Button _cancelButton;
    [SerializeField] private CanvasGroup _acceptButtonCanvasGroup;

    [Header("Config")]
    [SerializeField] private CountryLibrary _countryLibrary;
    [SerializeField] private ResourceAmountView _resourceAmountPrefab;
    [SerializeField, Range(0f, 1f)] private float _disableAlpha = 0.6f;

    public void Initialize(AcceptOfferPopupData popupData)
    {
        var definition = _countryLibrary.GetDef(popupData.OfferData.CountryId);
        if (definition != null)
        {
            _descriptionLocalizer.StringReference.Arguments = new object[] { definition.LocalizedName.GetLocalizedString() };
            _descriptionLocalizer.RefreshString();
        }

        foreach (Transform child in _importTransform) Destroy(child.gameObject);
        foreach (Transform child in _exportTransform) Destroy(child.gameObject);
        AddResourcesView(popupData.OfferData.Import, true);
        AddResourcesView(popupData.OfferData.Export, false);

        _acceptButtonCanvasGroup.blocksRaycasts = popupData.CanAfford;
        _acceptButtonCanvasGroup.alpha = popupData.CanAfford ? 1 : _disableAlpha;

        _acceptButton.onClick.RemoveAllListeners();
        _cancelButton.onClick.RemoveAllListeners();

        if (popupData.CanAfford) _acceptButton.onClick.AddListener(() => popupData.OnConfirmClick?.Invoke());
        _cancelButton.onClick.AddListener(() => popupData.OnCancelClick?.Invoke());
    }

    private void AddResourcesView(List<ResourceData> resources, bool isImport)
    {
        foreach (var resource in resources)
        {
            var resourceView = Instantiate(_resourceAmountPrefab, isImport ? _importTransform : _exportTransform);
            resourceView.Initialize(resource, isImport);
        }
    }
}
