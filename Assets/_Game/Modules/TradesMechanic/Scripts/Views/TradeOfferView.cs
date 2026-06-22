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

        private readonly List<ResourceAmountView> _importViewPool = new List<ResourceAmountView>(4);
        private readonly List<ResourceAmountView> _exportViewPool = new List<ResourceAmountView>(4);

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

            // Deactivate all pooled views before reuse
            DeactivatePool(_importViewPool);
            DeactivatePool(_exportViewPool);

            // Add new resources using pooled views
            PopulateResourceViews(data.Import, _importTransform, _importViewPool, isExport: false);
            PopulateResourceViews(data.Export, _exportTransform, _exportViewPool, isExport: true);

            // Setup Click
            _cardButton.onClick.RemoveAllListeners();
            _cardButton.onClick.AddListener(() => onOfferClickCallback?.Invoke(data));
        }

        private void DeactivatePool(List<ResourceAmountView> pool)
        {
            for (int i = 0; i < pool.Count; i++)
            {
                pool[i].gameObject.SetActive(false);
            }
        }

        private void PopulateResourceViews(IReadOnlyList<ResourceAmount> resources, Transform container,
            List<ResourceAmountView> pool, bool isExport)
        {
            if (resources == null) return;

            for (int i = 0; i < resources.Count; i++)
            {
                ResourceAmountView view;

                if (i < pool.Count)
                {
                    // Reuse existing pooled view
                    view = pool[i];
                    view.gameObject.SetActive(true);
                }
                else
                {
                    // Pool exhausted — instantiate new view and add to pool
                    view = Instantiate(_resourceAmountPrefab, container);
                    pool.Add(view);
                }

                view.Initialize(resources[i], isExport);
            }

            // Deactivate any excess pool items beyond what's needed
            for (int i = resources.Count; i < pool.Count; i++)
            {
                pool[i].gameObject.SetActive(false);
            }
        }
    }
}