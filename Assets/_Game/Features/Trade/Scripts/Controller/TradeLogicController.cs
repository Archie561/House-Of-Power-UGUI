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
    public class TradeLogicController : MonoBehaviour, IResourceLogicHandler
    {
        public static TradeLogicController Instance { get; private set; }

        [Header("Config")]
        [SerializeField] private TradeConfig _config;

        // --- State ---
        private List<TradeOfferData> _activeOffersCache = new List<TradeOfferData>();
        private float _timerSecondsRemaining;
        private bool _isReadyToRefresh;
        private int _lastIntTimer = -1; // For UI events optimization

        // --- Dependencies ---
        private TradeOfferGenerator _offerGenerator;
        private StorageUpgradeCalculator _storageUpgradeCalculator;

        // --- Events ---
        public event Action<float> OnTimerTick;
        public event Action<bool> OnRefreshStatusChanged;
        public event Action<IReadOnlyList<TradeOfferData>> OnOffersListUpdated;
        public event Action<ResourceType> OnTradeGoodChanged;

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
            _offerGenerator = new TradeOfferGenerator(settings: _config.GenerationSettings, capacityProvider: GetCurrentResourceCapacity);
            _storageUpgradeCalculator = new StorageUpgradeCalculator(settings: _config.StorageSettings);

            // Register as trade goods Logic Handler
            RegisterHandler();
        }

        private void Start()
        {
            GameDataService.Instance.OnResourceChanged += HandleResourceChange;

            LoadActiveOffers();
            InitializeTimer();
        }

        private void Update()
        {
            if (_isReadyToRefresh) return;

            HandleTimerTick();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            GameDataService.Instance.OnResourceChanged -= HandleResourceChange;
        }

        #endregion

        #region Offers Logic

        /// <summary>
        /// Retrieves a read-only list of all currently active trade offers.
        /// </summary>
        public IReadOnlyList<TradeOfferData> GetActiveOffers()
        {
            if (_activeOffersCache == null)
            {
                LoadActiveOffers();
            }
            return _activeOffersCache.AsReadOnly();
        }

        /// <summary>
        /// Updates offers list with new generated offers if the timer allows it or if paid with gems.
        /// </summary>
        /// <param name="isPremium">Indicates if the refresh is paid with gems (true) or free (false).</param>
        public bool TryRefreshOffers(bool isPremium)
        {
            if (!isPremium)
            {
                if (!_isReadyToRefresh) return false;
            }
            else
            {
                var cost = TransactionOperation.Spend(ResourceType.Gems, _config.SkipRefreshGemCost);
                if (!GameDataService.Instance.TryApplyTransaction(cost)) return false;
            }

            GenerateOffers();
            ResetTimer();

            return true;
        }

        /// <summary>
        /// Attempts to execute the given trade offer. If successful, updates resources and removes the offer.
        /// </summary>
        public bool TryExecuteOffer(TradeOfferData offer)
        {
            if (offer == null || !_activeOffersCache.Contains(offer))
                return false;

            List<TransactionOperation> transaction = new List<TransactionOperation>();

            foreach (var resource in offer.Export)
                transaction.Add(TransactionOperation.Spend(resource.Type, resource.Amount));

            // Add operations are enforced. An overflow warning (if any) is displayed in the UI before the transaction is confirmed.
            foreach (var resource in offer.Import)
                transaction.Add(TransactionOperation.Add(resource.Type, resource.Amount, forceApply: true));

            if (!GameDataService.Instance.TryApplyTransaction(transaction))
                return false;

            _activeOffersCache.Remove(offer);
            GameDataService.Instance.SaveActiveOffers(_activeOffersCache.AsReadOnly());
            OnOffersListUpdated?.Invoke(_activeOffersCache.AsReadOnly());

            return true;
        }

        /// <summary>
        /// Checks if the player has enough resource to pay the cost.
        /// </summary>
        public bool CanAfford(ResourceType type, int cost)
        {
            return GameDataService.Instance.CanApplyTransaction(TransactionOperation.Spend(type, cost));
        }

        /// <summary>
        /// Checks if the player has enaugh export resources of the given trade offer.
        /// </summary>
        public bool CanAfford(TradeOfferData offer)
        {
            List<TransactionOperation> transaction = new List<TransactionOperation>();

            foreach (var resource in offer.Export)
                transaction.Add(TransactionOperation.Spend(resource.Type, resource.Amount));

            if (!GameDataService.Instance.CanApplyTransaction(transaction))
                return false;

            return true;
        }

        // Loads saved offers from GameDataService. If none exist, creates an empty list.
        private void LoadActiveOffers()
        {
            var savedOffers = GameDataService.Instance.GetActiveOffers();
            _activeOffersCache = savedOffers != null ? new List<TradeOfferData>(savedOffers) : new List<TradeOfferData>();
        }

        // Generates new trade offers and updates the GameDataService. Notifies listeners.
        private void GenerateOffers()
        {
            _activeOffersCache.Clear();
            for (int i = 0; i < _config.OffersToGenerate; i++)
            {
                _activeOffersCache.Add(_offerGenerator.GenerateOffer());
            }

            GameDataService.Instance.SaveActiveOffers(_activeOffersCache.AsReadOnly());
            OnOffersListUpdated?.Invoke(_activeOffersCache.AsReadOnly());
        }

        #endregion

        #region Timer Logic

        public bool IsReadyToRefresh() => _isReadyToRefresh;

        public DateTime GetNextRefreshTime() => GameDataService.Instance.GetNextTradeRefreshTime();

        public int GetSkipRefreshGemCost() => _config.SkipRefreshGemCost;

        // Loads the timer state based on saved next refresh time.
        private void InitializeTimer()
        {
            DateTime targetTime = GameDataService.Instance.GetNextTradeRefreshTime();
            TimeSpan diff = targetTime - DateTime.Now;

            if (diff.TotalSeconds <= 0)
            {
                _timerSecondsRemaining = 0;
                SetTimerState(isReady: true);
            }
            else
            {
                _timerSecondsRemaining = (float)diff.TotalSeconds;
                SetTimerState(isReady: false);
            }
        }

        // Handles the countdown timer logic and triggers events on tick.
        private void HandleTimerTick()
        {
            _timerSecondsRemaining -= Time.deltaTime;

            if (_timerSecondsRemaining <= 0)
            {
                _timerSecondsRemaining = 0;
                if (DateTime.Now >= GameDataService.Instance.GetNextTradeRefreshTime())
                {
                    SetTimerState(true);
                }
            }

            // Optimize Event Calls: Only fire when integer second changes
            int currentIntTimer = Mathf.CeilToInt(_timerSecondsRemaining);
            if (currentIntTimer != _lastIntTimer)
            {
                _lastIntTimer = currentIntTimer;
                OnTimerTick?.Invoke(_timerSecondsRemaining);
            }
        }

        // Resets the timer to the configured cooldown and updates the next refresh time in GameDataService.
        private void ResetTimer()
        {
            DateTime newTarget = DateTime.Now.AddSeconds(_config.RefreshTradesCooldown);
            GameDataService.Instance.SetNextTradeRefreshTime(newTarget);

            _timerSecondsRemaining = _config.RefreshTradesCooldown;
            SetTimerState(false);
        }

        // Updates the timer state and triggers the refresh status changed event.
        private void SetTimerState(bool isReady)
        {
            _isReadyToRefresh = isReady;
            OnRefreshStatusChanged?.Invoke(isReady);
        }

        #endregion

        #region Resources Logic

        /// <summary>
        /// Returns the player's trade goods resources for UI display.
        /// </summary>
        public IReadOnlyList<ResourceAmount> GetPlayerResources()
        {
            var resources = new List<ResourceAmount>();
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                if (type.IsTradeGood())
                {
                    resources.Add(new ResourceAmount(type, GameDataService.Instance.GetAmount(type)));
                }   
            }

            return resources.AsReadOnly();
        }

        /// <summary>
        /// Gets the current amount of the specified resource type.
        /// </summary>
        public int GetResourceAmount(ResourceType type)
        {
            return GameDataService.Instance.GetAmount(type);
        }

        /// <summary>
        /// Returns CURRENT storage capacity for the specified resource type.
        /// </summary>
        public int GetCurrentResourceCapacity(ResourceType type)
        {
            int level = GameDataService.Instance.GetStorageLevel(type);
            return _storageUpgradeCalculator.GetCapacity(level);
        }

        /// <summary>
        /// Calculates the storage capacity for the next upgrade level of the specified resource type.
        /// </summary>
        public int GetNextResourceCapacity(ResourceType type)
        {
            int currentLevel = GameDataService.Instance.GetStorageLevel(type);
            int nextLevel = currentLevel + 1;
            return _storageUpgradeCalculator.GetCapacity(nextLevel);
        }

        /// <summary>
        /// Calculates the cost to upgrade specific resource storage to the NEXT level.
        /// </summary>
        public int GetStorageUpgradeCost(ResourceType type, bool isPremium)
        {
            int currentLevel = GameDataService.Instance.GetStorageLevel(type);
            int nextLevel = currentLevel + 1;

            if (isPremium)
                return _storageUpgradeCalculator.GetPremiumCost(nextLevel);
            else
                return _storageUpgradeCalculator.GetDefaultCost(nextLevel);
        }

        /// <summary>
        /// Attempts to upgrade storage.
        /// </summary>
        public bool TryUpgradeCapacity(ResourceType type, bool premiumPurchase)
        {
            int cost = GetStorageUpgradeCost(type, premiumPurchase);
            ResourceType costType = premiumPurchase ? ResourceType.Gems : type;

            if (!GameDataService.Instance.TryApplyTransaction(TransactionOperation.Spend(costType, cost)))
                return false;

            int newLevel = GameDataService.Instance.GetStorageLevel(type) + 1;
            GameDataService.Instance.SetStorageLevel(type, newLevel);

            OnTradeGoodChanged?.Invoke(type);
            return true;
        }

        private void HandleResourceChange(ResourceChangeData data)
        {
            if (data.Type.IsTradeGood())
            {
                OnTradeGoodChanged?.Invoke(data.Type);
            }
        }

        #endregion

        #region IResourceLogicHandler Implementation

        private void RegisterHandler()
        {
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                if (type.IsTradeGood())
                {
                    GameDataService.Instance.RegisterHandler(type, this);
                }
            }
        }

        public bool CanApplyTransactionOperation(ResourceType type, int currentAmount, int delta)
        {
            int newAmount = currentAmount + delta;

            if (newAmount < 0) return false;

            if (delta > 0)
            {
                int capacity = GetCurrentResourceCapacity(type);
                if (newAmount > capacity) return false;
            }

            return true;
        }

        public int CalculateTransactionOperation(ResourceType type, int currentAmount, int delta)
        {
            int capacity = GetCurrentResourceCapacity(type);
            return Mathf.Clamp(currentAmount + delta, 0, capacity);
        }

        #endregion
    }
}