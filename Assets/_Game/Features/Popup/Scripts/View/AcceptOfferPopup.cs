using Game.General;
using Game.Features.Trade;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Game.Features.Popup
{
    /// <summary>
    /// Popup that displays trade details (import/export resources) and allows the player to accept or reject the offer.
    /// </summary>
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

        /// <summary>
        /// Configures the popup with the specific trade offer data.
        /// </summary>
        /// <param name="popupData">Data containing the offer, costs, and callbacks.</param>
        public void Initialize(AcceptOfferPopupData popupData)
        {
            if (popupData?.OfferData == null)
            {
                Debug.LogError("[AcceptOfferPopup] Data is missing!");
                return;
            }

            // Localization
            var definition = _countryLibrary.GetDef(popupData.OfferData.CountryId);
            if (definition != null)
            {
                _descriptionLocalizer.StringReference.Arguments = new object[] { definition.LocalizedName.GetLocalizedString() };
                _descriptionLocalizer.RefreshString();
            }

            // Clear old views
            ClearContainer(_importTransform);
            ClearContainer(_exportTransform);

            // Populate new views
            AddResourcesView(popupData.OfferData.Import, true);
            AddResourcesView(popupData.OfferData.Export, false);

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

        private void AddResourcesView(IReadOnlyList<ResourceData> resources, bool isImport)
        {
            if (resources == null) return;

            foreach (var resource in resources)
            {
                var resourceView = Instantiate(_resourceAmountPrefab, isImport ? _importTransform : _exportTransform);
                resourceView.Initialize(resource, isImport);
            }
        }
    }
}