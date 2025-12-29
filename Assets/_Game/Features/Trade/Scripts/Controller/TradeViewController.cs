using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    private void Start()
    {
        _refreshButton.onClick.AddListener(OnRefreshButtonClicked);
        InitializeResourceRows();
    }

    private void OnEnable()
    {
        if (TradeLogicController.Instance == null) return;

        TradeLogicController.Instance.OnTimerTick += UpdateTimer;
        TradeLogicController.Instance.OnRefreshStatusChanged += UpdateHeaderVisuals;
        TradeLogicController.Instance.OnOffersListUpdated += RebuildOffersList;
        TradeLogicController.Instance.OnTradeResourceChanged += UpdateSingleResourceVisual;

        // 2. Примусова синхронізація стану (Force Sync)
        // Це критично важливо, якщо дані змінилися, поки вікно було закрите
        UpdateHeaderVisuals(TradeLogicController.Instance.IsReadyToRefresh());
        RebuildOffersList(TradeLogicController.Instance.GetOffers());
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

    #region Initialization

    private void InitializeResourceRows()
    {
        if (_spawnedRows.Count > 0) return;

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

    #endregion

    #region Event Handlers

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

    #region User Interaction

    private void OnUpgradeClicked(ResourceType type)
    {
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
                TradeLogicController.Instance.UpgradeCapacity(type, premiumPurchase: false);
                PopupController.Instance.CloseCurrentPopup();
            },
            onPremiumClick: () =>
            {
                TradeLogicController.Instance.UpgradeCapacity(type, premiumPurchase: true);
                PopupController.Instance.CloseCurrentPopup();
            }
        );

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

        //додати конструктори у data класи щоб було видно які поля треба ініціалізувати
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
                TradeLogicController.Instance.TryExecuteTrade(offer);
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
