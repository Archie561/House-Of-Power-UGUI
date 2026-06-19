using Game.General;
using Game.Features.Trade;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
using TMPro;

namespace Game.Features.Popup
{
    /// <summary>
    /// View for the Accept Offer Popup. Displays trade details, availability information and forwards results of user interactions via callbacks.
    /// </summary>
    public class AcceptOfferPopup : BasePopup
    {
        [Header("UI References")]
        [SerializeField] private LocalizeStringEvent _descriptionLocalizer;
        [SerializeField] private Image _flagIcon;
        [SerializeField] private Transform _exportTransform;
        [SerializeField] private Transform _importTransform;
        [SerializeField] private TextMeshProUGUI _storageOverflowWarningText;
        [SerializeField] private TextMeshProUGUI _cantAffordOfferWarningText;
        [SerializeField] private Button _acceptButton;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private CanvasGroup _acceptButtonCanvasGroup;

        [Header("Config")]
        [SerializeField] private CountryLibrary _countryLibrary;
        [SerializeField] private ResourceAmountView _resourceAmountPrefab;
        [SerializeField, Range(0f, 1f)] private float _disableAlpha = 0.6f;

        /// <summary>
        /// Initializes the popup with offer details, confirm and cancel buttons.
        /// </summary>
        public void Initialize(AcceptOfferPopupData popupData)
        {
            // Localization
            var definition = _countryLibrary.GetDef(popupData.OfferData.CountryId);
            if (definition != null)
            {
                _flagIcon.sprite = definition.FlagIcon;

                _descriptionLocalizer.StringReference.Arguments = new object[] { definition.LocalizedName.GetLocalizedString() };
                _descriptionLocalizer.RefreshString();
            }

            // Clear old views
            ClearContainer(_importTransform);
            ClearContainer(_exportTransform);

            // Populate new views
            AddResourcesView(popupData.OfferData.Import, isExport: false);
            AddResourcesView(popupData.OfferData.Export, isExport: true);

            // Set cant afford warning visibility
            _cantAffordOfferWarningText.gameObject.SetActive(!popupData.CanAfford);

            // Set storage overflow warning visibility
            _storageOverflowWarningText.gameObject.SetActive(popupData.DisplayOverflowStorageWarning);

            // Update Button State
            _acceptButtonCanvasGroup.blocksRaycasts = popupData.CanAfford;
            _acceptButtonCanvasGroup.alpha = popupData.CanAfford ? 1f : _disableAlpha;

            // Setup Buttons
            _acceptButton.onClick.RemoveAllListeners();
            _cancelButton.onClick.RemoveAllListeners();

            if (popupData.CanAfford)
            {
                _acceptButton.onClick.AddListener(() => popupData.OnConfirmClick?.Invoke());
            }

            _cancelButton.onClick.AddListener(() => popupData.OnCancelClick?.Invoke());
        }

        private void ClearContainer(Transform container)
        {
            foreach (Transform child in container)
            {
                Destroy(child.gameObject);
            }
        }

        private void AddResourcesView(IReadOnlyList<ResourceAmount> resources, bool isExport)
        {
            if (resources == null) return;

            foreach (var resource in resources)
            {
                var resourceView = Instantiate(_resourceAmountPrefab, isExport ? _exportTransform : _importTransform);
                resourceView.Initialize(resource, isExport);
            }
        }
    }
}