using System;
using System.Collections.Generic;
using UnityEngine;

public class TradeLogicController : MonoBehaviour
{
    public static TradeLogicController Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float _refreshCooldown = 300f;
    [SerializeField] private int _gemSkipCost = 5;
    [SerializeField] private int _capacityUpgradeAmount = 100;

    // --- Events ---
    public event Action<float> OnTimerTick;
    public event Action<bool> OnRefreshStatusChanged;

    public event Action<IReadOnlyList<TradeOfferData>> OnOffersListUpdated;
    public event Action<ResourceData> OnTradeResourceChanged;

    // --- State ---
    private float _currentTimer;
    private int _lastIntTimer = -1;
    private bool _isReadyToRefresh;
    private List<TradeOfferData> _activeOffers = new List<TradeOfferData>();

    #region Lifecycle

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (GameDataService.Instance != null)
            GameDataService.Instance.OnResourceChanged += HandleGlobalResourceChange;

        //TODO: pull from Player Data
        _currentTimer = _refreshCooldown;

        if (_activeOffers.Count == 0)
            GenerateOffers(notify: false);
    }

    private void OnDestroy()
    {
        if (Instance == this && GameDataService.Instance != null)
            GameDataService.Instance.OnResourceChanged -= HandleGlobalResourceChange;
    }

    private void Update()
    {
        if (_isReadyToRefresh) return;

        _currentTimer -= Time.deltaTime;

        int currentIntTimer = Mathf.CeilToInt(_currentTimer);
        if (currentIntTimer != _lastIntTimer)
        {
            _lastIntTimer = currentIntTimer;
            OnTimerTick?.Invoke(_currentTimer);
        }

        if (_currentTimer <= 0)
        {
            _currentTimer = 0;
            _isReadyToRefresh = true;
            OnRefreshStatusChanged?.Invoke(true);
        }
    }

    #endregion

    #region Public API

    public IReadOnlyList<TradeOfferData> GetOffers()
    {
        if (_activeOffers == null || _activeOffers.Count == 0)
        {
            GenerateOffers(notify: false);
        }
        return _activeOffers.AsReadOnly();
    }

    public IReadOnlyList<ResourceData> GetPlayerResources()
    {
        var resources = new List<ResourceData>();
        foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
        {
            if (type.IsCurrency()) continue;

            resources.Add(new ResourceData
            {
                Type = type,
                Amount = GameDataService.Instance.GetAmount(type),
                MaxCapacity = GameDataService.Instance.GetMaxCapacity(type)
            });
        }
        return resources.AsReadOnly();
    }

    public bool IsReadyToRefresh() => _isReadyToRefresh;
    public float GetTimeRemaining() => _currentTimer;
    public int GetGemSkipCost() => _gemSkipCost;

    public void RefreshOffersFree()
    {
        if (!_isReadyToRefresh) return;

        GenerateOffers(notify: true);
        ResetTimer();
    }

    public bool TryRefreshWithGems()
    {
        if (!GameDataService.Instance.TrySpend(ResourceType.Gems, _gemSkipCost))
            return false;

        GenerateOffers(notify: true);
        ResetTimer();
        return true;
    }

    public bool TryExecuteTrade(TradeOfferData offer)
    {
        if (!GameDataService.Instance.TrySpend(offer.Export))
            return false;

        // OPTIONAL: Додати логіку обмеження по максимальній кількості ресурсів
        GameDataService.Instance.AddResources(offer.Import);

        _activeOffers.Remove(offer);
        OnOffersListUpdated?.Invoke(_activeOffers.AsReadOnly());

        return true;
    }

    public UpgradeStorageData GetUpgradeStorageData(ResourceType type)
    {
        // TODO: Винести в окремий калькулятор згодом
        int currentCapacity = GameDataService.Instance.GetMaxCapacity(type);
        int nextCapacity = currentCapacity + _capacityUpgradeAmount;
        int defaultCost = 40;
        int premiumCost = 2;
        return new UpgradeStorageData
        {
            ResourceToUpgrade = type,
            CurrentCapacity = currentCapacity,
            NextCapacity = nextCapacity,
            DefaultCostType = type,
            DefaultCostAmount = defaultCost,
            PremiumCostAmount = premiumCost
        };
    }

    public void UpgradeCapacity(ResourceType type, bool premiumCost = false)
    {

        //int cost = 40; // TODO #7: Винести в окремий калькулятор згодом
        //if (!GameDataService.Instance.TrySpend(ResourceType.Money, cost)) //success = false;

        //GameDataService.Instance.UpgradeCapacity(type, _capacityUpgradeAmount);
        //success = true;
        //onUpgradeComplete?.Invoke(bool success);
    }

    #endregion

    #region Private Helpers

    private void HandleGlobalResourceChange(ResourceType type)
    {
        if (type.IsCurrency()) return;

        int amount = GameDataService.Instance.GetAmount(type);
        int maxCapacity = GameDataService.Instance.GetMaxCapacity(type);

        var data = new ResourceData
        {
            Type = type,
            Amount = amount,
            MaxCapacity = maxCapacity
        };

        OnTradeResourceChanged?.Invoke(data);
    }

    private void ResetTimer()
    {
        _currentTimer = _refreshCooldown;
        _lastIntTimer = -1;
        _isReadyToRefresh = false;
        OnRefreshStatusChanged?.Invoke(false);
    }

    // FIX #3: Це місце (Генерація) в майбутньому винесемо в ITradeOfferGenerator
    private void GenerateOffers(bool notify = true)
    {
        _activeOffers.Clear();
        for (int i = 0; i < 6; i++)
            _activeOffers.Add(CreateRandomOffer());

        if (notify)
        {
            OnOffersListUpdated?.Invoke(_activeOffers.AsReadOnly());
        }
    }

    private TradeOfferData CreateRandomOffer()
    {
        // ... (твоя стара логіка генерації без змін) ...
        // Залишаю код генерації як був у попередньому повідомленні

        var resourcesList = new List<ResourceType>();
        foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
        {
            if (!type.IsCurrency()) resourcesList.Add(type);
        }

        var countryList = new List<CountryId>();
        foreach (CountryId country in Enum.GetValues(typeof(CountryId)))
        {
            countryList.Add(country);
        }

        var imports = GenerateRandomResourceList(resourcesList);
        var exports = GenerateRandomResourceList(resourcesList);

        return new TradeOfferData
        {
            CountryId = countryList[UnityEngine.Random.Range(0, countryList.Count)],
            Import = imports,
            Export = exports
        };
    }

    private List<ResourceData> GenerateRandomResourceList(List<ResourceType> allowedTypes)
    {
        var list = new List<ResourceData>();
        int count = UnityEngine.Random.Range(1, 4);
        for (int i = 0; i < count; i++)
        {
            list.Add(new ResourceData
            {
                Type = allowedTypes[UnityEngine.Random.Range(0, allowedTypes.Count)],
                Amount = UnityEngine.Random.Range(1, 100)
            });
        }
        return list;
    }

    #endregion
}