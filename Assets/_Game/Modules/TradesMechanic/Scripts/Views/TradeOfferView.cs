using Game.General;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Features.Trade
{
    /// <summary>
    /// Represents a single trade offer card in the UI.
    /// Displays the country flag, import/export resources, and handles the click event.
    /// </summary>
    public class TradeOfferView : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button _cardButton;
        [SerializeField] private Image _flagIcon;
        [SerializeField] private Transform _importTransform;
        [SerializeField] private Transform _exportTransform;

        [Header("Config")]
        [SerializeField] private CountryLibrary _countryLibrary;
        [SerializeField] private ResourceAmountView _resourceAmountPrefab;

        /// <summary>
        /// Configures the card with offer data.
        /// </summary>
        public void Initialize(TradeOfferData data, Action<TradeOfferData> onOfferClickCallback)
        {
            if (data == null) return;

            // Setup Country Flag
            var definition = _countryLibrary.GetDef(data.CountryId);
            if (definition != null)
            {
                _flagIcon.sprite = definition.FlagIcon;
            }

            // Clear previous resources (Crucial for object reuse)
            ClearContainer(_importTransform);
            ClearContainer(_exportTransform);

            // Add new resources
            AddResourcesView(data.Import, isExport: false);
            AddResourcesView(data.Export, isExport: true);

            // Setup Click
            _cardButton.onClick.RemoveAllListeners();
            _cardButton.onClick.AddListener(() => onOfferClickCallback?.Invoke(data));
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