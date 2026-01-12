using Game.General;
using Game.Features.Popup;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Features.Trade
{
    /// <summary>
    /// Manages the visual representation of the Trade screen.
    /// Acts as an Orchestrator: listens to Logic events and updates the View, 
    /// and maps View input events to Logic commands or Popups.
    /// </summary>
    public class TradeViewController : MonoBehaviour
    {
        [Header("Resources Panel")]
        [SerializeField] private Transform _resourcesContainer;
        [SerializeField] private ResourceRowView _resourceRowPrefab;

        [Header("Offers Panel")]
        [SerializeField] private Transform _offersContainer;
        [SerializeField] private TradeOfferView _offerPrefab;

        [Header("Refresh Controls")]
        [SerializeField] private Button _refreshButton;
        [SerializeField] private TextMeshProUGUI _refreshTimerText;
        [SerializeField] private GameObject _timerIconObject;
        [SerializeField] private Sprite _activeButtonSprite;
        [SerializeField] private Sprite _waitingButtonSprite;

        private Dictionary<ResourceType, ResourceRowView> _spawnedRows = new Dictionary<ResourceType, ResourceRowView>();

        #region Unity Lifecycle

        private void Start()
        {
            _refreshButton.onClick.AddListener(OnRefreshButtonClicked);

            // Initialize once on startup
            InitializeResourceRows();
        }

        private void OnEnable()
        {
            if (TradeLogicController.Instance == null) return;

            // 1. Subscribe to events
            TradeLogicController.Instance.OnTimerTick += UpdateTimer;
            TradeLogicController.Instance.OnRefreshStatusChanged += UpdateHeaderVisuals;
            TradeLogicController.Instance.OnOffersListUpdated += RebuildOffersList;
            TradeLogicController.Instance.OnTradeResourceChanged += UpdateSingleResourceVisual;

            // 2. Force Sync (Update UI to match current logic state immediately)
            UpdateHeaderVisuals(TradeLogicController.Instance.IsReadyToRefresh());
            RebuildOffersList(TradeLogicController.Instance.GetOffers());

            // Ensure resource rows are created if OnEnable fires before Start (rare but possible)
            if (_spawnedRows.Count == 0) InitializeResourceRows();
            RefreshAllResourcesVisuals();
        }

        private void OnDisable()
        {
            if (TradeLogicController.Instance == null) return;

            TradeLogicController.Instance.OnTimerTick -= UpdateTimer;
            TradeLogicController.Instance.OnRefreshStatusChanged -= UpdateHeaderVisuals;
            TradeLogicController.Instance.OnOffersListUpdated -= RebuildOffersList;
            TradeLogicController.Instance.OnTradeResourceChanged -= UpdateSingleResourceVisual;
        }

        #endregion

        #region View Initialization & Updates

        private void InitializeResourceRows()
        {
            if (_spawnedRows.Count > 0) return;

            // Clean up placeholders if any
            foreach (Transform child in _resourcesContainer) Destroy(child.gameObject);
            _spawnedRows.Clear();

            var resources = TradeLogicController.Instance.GetPlayerResources();
            foreach (var resource in resources)
            {
                var row = Instantiate(_resourceRowPrefab, _resourcesContainer);
                row.Initialize(resource, OnUpgradeClicked);
                _spawnedRows.Add(resource.Type, row);
            }
        }

        private void RefreshAllResourcesVisuals()
        {
            if (_spawnedRows.Count == 0) return;

            var resources = TradeLogicController.Instance.GetPlayerResources();
            foreach (var res in resources)
            {
                if (_spawnedRows.TryGetValue(res.Type, out var row))
                {
                    row.UpdateView(res.Amount, res.MaxCapacity);
                }
            }
        }

        private void UpdateSingleResourceVisual(ResourceData resource)
        {
            if (_spawnedRows.TryGetValue(resource.Type, out var row))
            {
                row.UpdateView(resource.Amount, resource.MaxCapacity);
            }
        }

        private void RebuildOffersList(IReadOnlyList<TradeOfferData> offers)
        {
            foreach (Transform child in _offersContainer) Destroy(child.gameObject);

            foreach (var offer in offers)
            {
                var item = Instantiate(_offerPrefab, _offersContainer);
                item.Initialize(offer, OnOfferClicked);
            }
        }

        private void UpdateTimer(float time)
        {
            if (time < 0) time = 0;

            int m = Mathf.FloorToInt(time / 60F);
            int s = Mathf.FloorToInt(time % 60F);
            _refreshTimerText.text = $"{m:00}:{s:00}";
        }

        private void UpdateHeaderVisuals(bool isReady)
        {
            _timerIconObject.SetActive(!isReady);
            _refreshTimerText.gameObject.SetActive(!isReady);
            _refreshButton.image.sprite = isReady ? _activeButtonSprite : _waitingButtonSprite;
        }

        #endregion

        #region User Interaction Handlers

        private void OnUpgradeClicked(ResourceType type)
        {
            // Prepare Data using Logic Controller
            var upgradeData = TradeLogicController.Instance.GetUpgradeStorageData(type);
            var canAffordDefault = TradeLogicController.Instance.CanAfford(upgradeData.StorageType, upgradeData.DefaultCostAmount);
            var canAffordPremium = TradeLogicController.Instance.CanAfford(ResourceType.Gems, upgradeData.PremiumCostAmount);

            var popupData = new UpgradeStoragePopupData
            (
                upgradeData,
                canAffordDefault,
                canAffordPremium,
                onDefaultClick: () =>
                {
                    TradeLogicController.Instance.TryUpgradeCapacity(type, premiumPurchase: false);
                    PopupController.Instance.CloseCurrentPopup();
                },
                onPremiumClick: () =>
                {
                    TradeLogicController.Instance.TryUpgradeCapacity(type, premiumPurchase: true);
                    PopupController.Instance.CloseCurrentPopup();
                }
            );

            // Show Popup
            PopupController.Instance.Show<UpgradeStoragePopup>(popup =>
            {
                popup.Initialize(popupData);
            });
        }

        private void OnRefreshButtonClicked()
        {
            bool isReady = TradeLogicController.Instance.IsReadyToRefresh();

            if (isReady)
            {
                TradeLogicController.Instance.RefreshOffersFree();
                return;
            }

            DateTime targetTime = TradeLogicController.Instance.GetNextRefreshTime();
            var skipCost = TradeLogicController.Instance.GetGemSkipCost();
            var canAfford = TradeLogicController.Instance.CanAfford(ResourceType.Gems, skipCost);

            var popupData = new RefreshTradesPopupData
            (
                targetTime,
                skipCost,
                canAfford,
                onSkipClick: () =>
                {
                    TradeLogicController.Instance.TryRefreshWithGems();
                    PopupController.Instance.CloseCurrentPopup();
                },
                onTimerVisuallyFinished: () =>
                {
                    // Auto-close popup when timer hits 00:00 while popup is open
                    if (TradeLogicController.Instance.IsReadyToRefresh())
                    {
                        PopupController.Instance.CloseCurrentPopup();
                    }
                }
            );

            PopupController.Instance.Show<RefreshTradesPopup>(popup =>
            {
                popup.Initialize(popupData);
            });
        }

        private void OnOfferClicked(TradeOfferData offer)
        {
            var canAfford = TradeLogicController.Instance.CanAfford(offer.Export);

            var popupData = new AcceptOfferPopupData
            (
                offer,
                canAfford,
                onConfirmClick: () =>
                {
                    TradeLogicController.Instance.TryExecuteOffer(offer);
                    PopupController.Instance.CloseCurrentPopup();
                },
                onCancelClick: () =>
                {
                    PopupController.Instance.CloseCurrentPopup();
                }
            );

            PopupController.Instance.Show<AcceptOfferPopup>(popup =>
            {
                popup.Initialize(popupData);
            });
        }

        #endregion
    }
}