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
        private List<TradeOfferView> _offersPool = new List<TradeOfferView>();

        #region Unity Lifecycle

        private void Start()
        {
            _refreshButton.onClick.RemoveAllListeners();
            _refreshButton.onClick.AddListener(OnRefreshButtonClicked);
        }

        private void OnEnable()
        {
            if (TradeLogicController.Instance == null) return;

            // Subscribe to events
            TradeLogicController.Instance.OnTimerSecondsTick += UpdateTimer;
            TradeLogicController.Instance.OnFreeTradesRefreshStatusChanged += UpdateHeaderVisuals;
            TradeLogicController.Instance.OnTradeGoodChanged += RefreshResourceRow;
            TradeLogicController.Instance.OnOffersListUpdated += RebuildOffersList;

            // Force Sync (Update UI to match current logic state immediately)
            if (TradeLogicController.Instance.IsDataReady) RestoreViewState();
            else TradeLogicController.Instance.OnDataReady += RestoreViewState;
        }

        private void OnDisable()
        {
            if (TradeLogicController.Instance == null) return;

            TradeLogicController.Instance.OnTimerSecondsTick -= UpdateTimer;
            TradeLogicController.Instance.OnFreeTradesRefreshStatusChanged -= UpdateHeaderVisuals;
            TradeLogicController.Instance.OnTradeGoodChanged -= RefreshResourceRow;
            TradeLogicController.Instance.OnOffersListUpdated -= RebuildOffersList;
            TradeLogicController.Instance.OnDataReady -= RestoreViewState;
        }

        #endregion

        #region View Initialization & Updates

        private void RestoreViewState()
        {
            UpdateHeaderVisuals(TradeLogicController.Instance.IsFreeTradesRefreshAvailable);
            RebuildOffersList(TradeLogicController.Instance.GetActiveOffers());
            InitializeResourceRows();
        }

        private void UpdateHeaderVisuals(bool isReady)
        {
            _timerIconObject.SetActive(!isReady);
            _refreshTimerText.gameObject.SetActive(!isReady);
            _refreshButton.image.sprite = isReady ? _activeButtonSprite : _waitingButtonSprite;
        }

        private void RebuildOffersList(IReadOnlyList<TradeOfferData> offers)
        {
            for (int i = 0; i < offers.Count; i++)
            {
                TradeOfferView offerView;

                // Reuse from pool or instantiate new
                if (i < _offersPool.Count)
                {
                    offerView = _offersPool[i];
                    offerView.gameObject.SetActive(true);
                }
                else
                {
                    offerView = Instantiate(_offerPrefab, _offersContainer);
                    _offersPool.Add(offerView);
                }

                offerView.Initialize(offers[i], OnOfferClicked);
            }

            // Deactivate any unused offer views in the pool
            for (int i = offers.Count; i < _offersPool.Count; i++)
            {
                _offersPool[i].gameObject.SetActive(false);
            }
        }

        private void InitializeResourceRows()
        {
            var resources = TradeLogicController.Instance.GetPlayerResources();

            foreach (var resource in resources)
            {
                if (!_spawnedRows.ContainsKey(resource.Type))
                {
                    var row = Instantiate(_resourceRowPrefab, _resourcesContainer);
                    row.Initialize(resource.Type, OnStorageUpgradeClicked);
                    _spawnedRows.Add(resource.Type, row);
                }

                RefreshResourceRow(resource.Type);
            }
        }

        private void RefreshResourceRow(ResourceType type)
        {
            if (_spawnedRows.TryGetValue(type, out var row))
            {
                var amount = TradeLogicController.Instance.GetResourceAmount(type);
                var capacity = TradeLogicController.Instance.GetStorageCapacity(type);
                row.UpdateView(amount, capacity);
            }
        }

        private void UpdateTimer(int time)
        {
            if (time < 0) time = 0;

            int m = time / 60;
            int s = time % 60;
            _refreshTimerText.text = $"{m:00}:{s:00}";
        }

        #endregion

        #region User Interaction Handlers

        private void OnStorageUpgradeClicked(ResourceType type)
        {
            // Prepare Data using Logic Controller
            var data = TradeLogicController.Instance.GetStorageUpgradeData(type);

            var canAffordDefault = TradeLogicController.Instance.CanAffordCost(type, data.DefaultCost);
            var canAffordPremium = TradeLogicController.Instance.CanAffordCost(ResourceType.Gems, data.PremiumCost);

            var popupData = new UpgradeStoragePopupData
            (
                type,
                data.CurrentCapacity,
                data.UpgradedCapacity,
                data.DefaultCost,
                data.PremiumCost,
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
            bool isReady = TradeLogicController.Instance.IsFreeTradesRefreshAvailable;

            if (isReady)
            {
                TradeLogicController.Instance.TryRefreshOffers(isPremium: false);
                return;
            }

            DateTime targetTime = TradeLogicController.Instance.NextTradeRefreshTime;
            var skipCost = TradeLogicController.Instance.PremiumTradesRefreshCost;
            var canAfford = TradeLogicController.Instance.CanAffordCost(ResourceType.Gems, skipCost);

            var popupData = new RefreshTradesPopupData(
                targetTime: targetTime,
                costType: ResourceType.Gems,
                costAmount: skipCost,
                canAfford: canAfford,
                onConfirmClick: () =>
                {
                    TradeLogicController.Instance.TryRefreshOffers(isPremium: true);
                    PopupController.Instance.CloseCurrentPopup();
                }
            );

            PopupController.Instance.Show<RefreshTradesPopup>(popup =>
            {
                popup.Initialize(popupData);

                void OnPremiumTradesRefreshCostChanged(int newCost)
                {
                    if (newCost <= 0)
                    {
                        PopupController.Instance.CloseCurrentPopup();
                    }
                    else
                    {
                        var canAfford = TradeLogicController.Instance.CanAffordCost(ResourceType.Gems, newCost);
                        popup.UpdateCostVisuals(newCost, canAfford);
                    }
                }

                TradeLogicController.Instance.OnPremiumTradesRefreshCostChanged += OnPremiumTradesRefreshCostChanged;

                // Unsubscribe from the event when the popup is closed to prevent memory leaks
                popup.OnPopupClosed += ()  => TradeLogicController.Instance.OnPremiumTradesRefreshCostChanged -= OnPremiumTradesRefreshCostChanged;
            });
        }

        private void OnOfferClicked(TradeOfferData offer)
        {
            var canAfford = TradeLogicController.Instance.CanAffordOffer(offer, out bool hasStorageOverflow);

            // show warning about storage overflow only if the player can afford the offer, to avoid confusion with the "can't afford" warning
            bool displayOverflowStorageWarning = hasStorageOverflow && canAfford;

            var popupData = new AcceptOfferPopupData
            (
                offer,
                canAfford,
                displayOverflowStorageWarning,
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