using Game.General;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Features.Trade
{
    /// <summary>
    /// Orchestrates the trading system mechanics: timer, offer lifecycle, and upgrades.
    /// Acts as a bridge between DataService and View.
    /// </summary>
    public class TradeLogicController : MonoBehaviour
    {
        public static TradeLogicController Instance { get; private set; }

        [Header("Config")]
        [SerializeField] private float _refreshTradesCooldown = 600f;
        [SerializeField] private int _skipRefreshGemCost = 5;
        [SerializeField] private int _offersToGenerate = 8;

        public event Action<float> OnTimerTick;
        public event Action<bool> OnRefreshStatusChanged;
        public event Action<IReadOnlyList<TradeOfferData>> OnOffersListUpdated;
        public event Action<ResourceData> OnTradeResourceChanged;

        private float _currentTimerSeconds; // Visual timer only;
        private int _lastIntTimer = -1;
        private bool _isReadyToRefresh;
        private bool _isDataLoaded = false;

        private List<TradeOfferData> _activeOffers = new List<TradeOfferData>();

        // Dependencies
        private TradeOfferGenerator _offerGenerator;
        private StorageUpgradeCalculator _upgradeCalculator;

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Initialize Logic Modules
            _offerGenerator = new TradeOfferGenerator();
            _upgradeCalculator = new StorageUpgradeCalculator();
        }

        private void Start()
        {
            if (GameDataService.Instance != null)
                GameDataService.Instance.OnResourceChanged += HandleGlobalResourceChange;

            RestoreTimerState();
        }

        private void OnDestroy()
        {
            if (Instance == this && GameDataService.Instance != null)
                GameDataService.Instance.OnResourceChanged -= HandleGlobalResourceChange;
        }

        private void Update()
        {
            if (_isReadyToRefresh) return;

            _currentTimerSeconds -= Time.deltaTime;

            if (_currentTimerSeconds <= 0)
            {
                _currentTimerSeconds = 0;
                if (DateTime.Now >= GameDataService.Instance.GetNextTradeRefreshTime())
                {
                    SetReadyState(true);
                }
            }

            // Optimize Event Calls: Only fire when integer second changes
            int currentIntTimer = Mathf.CeilToInt(_currentTimerSeconds);
            if (currentIntTimer != _lastIntTimer)
            {
                _lastIntTimer = currentIntTimer;
                OnTimerTick?.Invoke(_currentTimerSeconds);
            }
        }

        #endregion

        #region Public API - Offers

        public IReadOnlyList<TradeOfferData> GetOffers()
        {
            if (!_isDataLoaded)
            {
                RestoreOffersState();
            }
            return _activeOffers.AsReadOnly();
        }

        public bool IsReadyToRefresh() => _isReadyToRefresh;

        public DateTime GetNextRefreshTime() => GameDataService.Instance.GetNextTradeRefreshTime();

        // TODO: In the future, this could scale with the timer
        public int GetGemSkipCost() => _skipRefreshGemCost;

        public void RefreshOffersFree()
        {
            if (!_isReadyToRefresh) return;
            GenerateOffers(notify: true);
            ResetTimer();
        }

        public bool TryRefreshWithGems()
        {
            int cost = GetGemSkipCost();

            if (!GameDataService.Instance.TrySpend(ResourceType.Gems, cost))
                return false;

            GenerateOffers(notify: true);
            ResetTimer();
            return true;
        }

        public bool TryExecuteOffer(TradeOfferData offer)
        {
            if (offer == null) return false;

            // Check if we have resources to export
            if (!GameDataService.Instance.CanAfford(offer.Export)) return false;

            // Transaction: Spend Export, Gain Import
            GameDataService.Instance.TrySpend(offer.Export);
            GameDataService.Instance.AddResources(offer.Import);

            _activeOffers.Remove(offer);
            GameDataService.Instance.SaveActiveOffers(_activeOffers);
            OnOffersListUpdated?.Invoke(_activeOffers.AsReadOnly());

            return true;
        }

        #endregion

        #region Public API - Resources & Upgrades

        public IReadOnlyList<ResourceData> GetPlayerResources()
        {
            var resources = new List<ResourceData>();
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                if (type.IsCurrency()) continue;

                resources.Add(new ResourceData(
                    type,
                    GameDataService.Instance.GetAmount(type),
                    GameDataService.Instance.GetMaxCapacity(type)
                ));
            }
            return resources.AsReadOnly();
        }

        public UpgradeStorageData GetUpgradeStorageData(ResourceType type)
        {
            int currentCap = GameDataService.Instance.GetMaxCapacity(type);
            return _upgradeCalculator.CalculateNextUpgrade(type, currentCap);
        }

        public bool TryUpgradeCapacity(ResourceType type, bool premiumPurchase)
        {
            var data = GetUpgradeStorageData(type);

            // Determine cost type and amount
            ResourceType costType = premiumPurchase ? ResourceType.Gems : type;
            int amount = premiumPurchase ? data.PremiumCostAmount : data.DefaultCostAmount;

            if (!GameDataService.Instance.TrySpend(costType, amount))
            {
                return false;
            }

            GameDataService.Instance.UpgradeCapacity(type, _upgradeCalculator.GetCapacityStep());
            return true;
        }

        // Forwarding simple checks to DataService to keep encapsulation
        public bool CanAfford(ResourceType type, int cost) => GameDataService.Instance.CanAfford(type, cost);

        public bool CanAfford(IReadOnlyList<ResourceData> costs) => GameDataService.Instance.CanAfford(costs);

        #endregion

        #region Private Methods

        private void RestoreTimerState()
        {
            DateTime targetTime = GameDataService.Instance.GetNextTradeRefreshTime();
            TimeSpan diff = targetTime - DateTime.Now;

            if (diff.TotalSeconds <= 0)
            {
                // Time has already passed (e.g. while game was closed)
                SetReadyState(true);
            }
            else
            {
                // Timer is still running
                _currentTimerSeconds = (float)diff.TotalSeconds;
                SetReadyState(false);
            }
        }

        private void SetReadyState(bool isReady)
        {
            _isReadyToRefresh = isReady;
            OnRefreshStatusChanged?.Invoke(isReady);
            if (isReady) _currentTimerSeconds = 0;
            OnTimerTick?.Invoke(_currentTimerSeconds);
        }

        private void ResetTimer()
        {
            DateTime newTarget = DateTime.Now.AddSeconds(_refreshTradesCooldown);
            GameDataService.Instance.SetNextTradeRefreshTime(newTarget);

            _currentTimerSeconds = _refreshTradesCooldown;
            SetReadyState(false);
        }

        private void RestoreOffersState()
        {
            _isDataLoaded = true;

            // Try to load from GameDataService
            var savedOffers = GameDataService.Instance.GetActiveOffers();

            if (savedOffers != null && savedOffers.Count > 0)
            {
                // Found saved offers, use them
                _activeOffers = new List<TradeOfferData>(savedOffers);
            }
            else
            {
                //If first time or no saved offers, generate new ones
                if (GameDataService.Instance.GetActiveOffers() == null)
                {
                    GenerateOffers(notify: false);
                }
                else
                {
                    _activeOffers.Clear();
                }
            }

            OnOffersListUpdated?.Invoke(_activeOffers.AsReadOnly());
        }

        private void GenerateOffers(bool notify)
        {
            _activeOffers.Clear();
            for (int i = 0; i < _offersToGenerate; i++)
            {
                _activeOffers.Add(_offerGenerator.GenerateOffer());
            }
            GameDataService.Instance.SaveActiveOffers(_activeOffers);

            if (notify)
            {
                OnOffersListUpdated?.Invoke(_activeOffers.AsReadOnly());
            }
        }

        private void HandleGlobalResourceChange(ResourceType type)
        {
            if (type.IsCurrency()) return;

            // Notify UI only about the changed resource to update the row
            int amount = GameDataService.Instance.GetAmount(type);
            int maxCapacity = GameDataService.Instance.GetMaxCapacity(type);

            OnTradeResourceChanged?.Invoke(new ResourceData(type, amount, maxCapacity));
        }

        #endregion
    }
}