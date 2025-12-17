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

    public event Action<float> OnTimerTick;           
    public event Action<bool> OnRefreshStatusChanged;
    public event Action<List<TradeOfferData>> OnOffersListUpdated;
    public event Action<ResourceData> OnTradeResourceChanged;

    private float _currentTimer;
    private bool _isReadyToRefresh;
    private List<TradeOfferData> _activeOffers = new List<TradeOfferData>();

    //перевірити чи GameDataService існує на момент виклику Awake
    private void Awake()
    {
        Instance = this;

        // Тут можна завантажити збережений стан таймера з PlayerData
        _currentTimer = _refreshCooldown;
        GenerateOffers();
    }

    private void Start()
    {
        if (GameDataService.Instance != null)
            GameDataService.Instance.OnResourceChanged += HandleResourceChange;
    }

    private void OnDestroy()
    {
        if (GameDataService.Instance != null)
            GameDataService.Instance.OnResourceChanged -= HandleResourceChange;
    }

    private void HandleResourceChange(ResourceType type)
    {
        if (type.IsCurrency()) return;

        int amount = GameDataService.Instance.GetAmount(type);
        int maxCapacity = GameDataService.Instance.GetMaxCapacity(type);

        OnTradeResourceChanged?.Invoke(new ResourceData
        {
            Type = type,
            Amount = amount,
            MaxCapacity = maxCapacity
        });
    }

    private void Update()
    {
        if (_isReadyToRefresh) return;

        _currentTimer -= Time.deltaTime;
        OnTimerTick?.Invoke(_currentTimer);

        if (_currentTimer <= 0)
        {
            _currentTimer = 0;
            _isReadyToRefresh = true;
            OnRefreshStatusChanged?.Invoke(true);
        }
    }

    public List<TradeOfferData> GetOffers() => _activeOffers;
    public bool IsReadyToRefresh() => _isReadyToRefresh;
    public float GetTimeRemaining() => _currentTimer;
    public int GetGemSkipCost() => _gemSkipCost;

    public List<ResourceData> GetPlayerResources()
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

        return resources;
    }

    public void RefreshOffersFree()
    {
        if (!_isReadyToRefresh) return;

        GenerateOffers();
        ResetTimer();
    }

    public bool TryRefreshWithGems()
    {
        if (!GameDataService.Instance.TrySpend(ResourceType.Gems, _gemSkipCost))
            return false;

        GenerateOffers();
        ResetTimer();
        return true;
    }

    public bool TryExecuteTrade(TradeOfferData offer)
    {
        if (!GameDataService.Instance.TrySpend(offer.Export))
            return false;

        GameDataService.Instance.AddResources(offer.Import);
        _activeOffers.Remove(offer);
        OnOffersListUpdated?.Invoke(_activeOffers);

        return true;
    }

    public bool TryUpgradeCapacity(ResourceType type)
    {
        int cost = 40; //CalculateUpgradeCost(type);
        if (!GameDataService.Instance.TrySpend(type, cost)) return false;

        GameDataService.Instance.UpgradeCapacity(type, _capacityUpgradeAmount);
        return true;
    }

    private void ResetTimer()
    {
        _currentTimer = _refreshCooldown;
        _isReadyToRefresh = false;
        OnRefreshStatusChanged?.Invoke(false);
    }

    private void GenerateOffers()
    {
        _activeOffers.Clear();

        for (int i = 0; i < 6; i++)
            _activeOffers.Add(CreateRandomOffer());

        OnOffersListUpdated?.Invoke(_activeOffers);
    }

    private TradeOfferData CreateRandomOffer()
    {
        var resourcesList = new List<ResourceType>();
        foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
        {
            if (type.IsCurrency()) continue;
            resourcesList.Add(type);
        }

        var countryList = new List<CountryId>();
        foreach (CountryId country in Enum.GetValues(typeof(CountryId)))
        {
            countryList.Add(country);
        }

        var imports = new List<ResourceData>();
        var randomAmount = UnityEngine.Random.Range(1, 4);
        for (int i = 0; i < randomAmount; i++)
        {
            imports.Add(new ResourceData
            {
                Type = resourcesList[UnityEngine.Random.Range(0, resourcesList.Count)],
                Amount = UnityEngine.Random.Range(1, 100)
            });
        }

        var exports = new List<ResourceData>();
        randomAmount = UnityEngine.Random.Range(1, 4);
        for (int i = 0; i < randomAmount; i++)
        {
            exports.Add(new ResourceData
            {
                Type = resourcesList[UnityEngine.Random.Range(0, resourcesList.Count)],
                Amount = UnityEngine.Random.Range(1, 100)
            });
        }

        return new TradeOfferData
        {
            CountryId = countryList[UnityEngine.Random.Range(0, countryList.Count)],
            Import = imports,
            Export = exports
        };
    }
}