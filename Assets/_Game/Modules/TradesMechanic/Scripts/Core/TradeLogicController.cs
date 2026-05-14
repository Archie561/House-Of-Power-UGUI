using Game.General;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Features.Trade
{
    /// <summary>
    /// Controls the logic of the trading system mechanics: timer, offer lifecycle, and upgrades.
    /// Acts as a bridge between PlayerDataService and View.
    /// </summary>
    public class TradeLogicController : MonoBehaviour, IResourceLogicHandler
    {
        public static TradeLogicController Instance { get; private set; }

        [Header("Config")]
        [SerializeField] private TradeConfig _config;

        // --- State ---
        private List<TradeOfferData> _activeOffersCache = new List<TradeOfferData>();
        private DateTime _nextRefreshTime;
        private bool _isReadyToRefresh;
        private int _skipRefreshGemCost; // Cached cost for skipping refresh, calculated based on remaining time and config. Updated on timer tick.
        private int _lastIntTimer = -1; // For UI events optimization
        private bool _isInitialized = false;

        // --- Dependencies ---
        private TradeOfferGenerator _offerGenerator;
        private StorageUpgradeCalculator _storageUpgradeCalculator;

        // --- Events ---
        public event Action<int> OnTimerTick;
        public event Action<bool> OnRefreshStatusChanged;
        public event Action<int> OnSkipCostChanged;
        public event Action<IReadOnlyList<TradeOfferData>> OnOffersListUpdated;
        public event Action<ResourceType> OnTradeGoodChanged;

        #region Unity Lifecycle & Initialization

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
        }

        private void Start()
        {
            // Register as trade goods Logic Handler
            RegisterHandler();
            
            PlayerDataService.Instance.OnResourceChanged += HandleResourceChange;

            EnsureInitialized();
        }

        private void Update()
        {
            if (!_isInitialized) return;

            HandleTimerTick();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            PlayerDataService.Instance.OnResourceChanged -= HandleResourceChange;

            if (TransactionService.Instance != null)
            {
                foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
                {
                    if (type.IsTradeGood())
                    {
                        TransactionService.Instance.UnregisterHandler(type);
                    }
                }
            }
        }

        private void EnsureInitialized()
        {
            if (_isInitialized) return;

            InitializeOffers();
            InitializeTimer();

            _isInitialized = true;
        }

        // Loads saved offers from PlayerDataService. If none exist, creates an empty list.
        private void InitializeOffers()
        {
            var savedOffers = PlayerDataService.Instance.GetActiveOffers();
            _activeOffersCache = savedOffers != null ? new List<TradeOfferData>(savedOffers) : new List<TradeOfferData>();
        }

        // Loads the timer state based on saved next refresh time.
        private void InitializeTimer()
        {
            _nextRefreshTime = PlayerDataService.Instance.GetNextTradeRefreshTime();
            HandleTimerTick(); // To initialize the timer state immediately on load
        }

        #endregion

        #region Offers Logic

        /// <summary>
        /// Retrieves a read-only list of all currently active trade offers.
        /// </summary>
        public IReadOnlyList<TradeOfferData> GetActiveOffers()
        {
            EnsureInitialized();
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
                var cost = TransactionOperation.Spend(ResourceType.Gems, _skipRefreshGemCost);
                if (!TransactionService.Instance.TryApplyTransaction(cost)) return false;
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

            if (!TransactionService.Instance.TryApplyTransaction(transaction))
                return false;

            _activeOffersCache.Remove(offer);
            
            ITradeDataWriter writer = PlayerDataService.Instance;
            writer.SetActiveOffers(_activeOffersCache);

            OnOffersListUpdated?.Invoke(_activeOffersCache.AsReadOnly());

            return true;
        }

        /// <summary>
        /// Checks if the player has enough resource to pay the cost.
        /// </summary>
        public bool CanAfford(ResourceType type, int cost)
        {
            return TransactionService.Instance.CanApplyTransaction(TransactionOperation.Spend(type, cost));
        }

        /// <summary>
        /// Checks if the player has enaugh export resources of the given trade offer.
        /// </summary>
        public bool CanAfford(TradeOfferData offer)
        {
            List<TransactionOperation> transaction = new List<TransactionOperation>();

            foreach (var resource in offer.Export)
                transaction.Add(TransactionOperation.Spend(resource.Type, resource.Amount));

            if (!TransactionService.Instance.CanApplyTransaction(transaction))
                return false;

            return true;
        }

        // Generates new trade offers and updates the PlayerDataService. Notifies listeners.
        private void GenerateOffers()
        {
            _activeOffersCache.Clear();
            for (int i = 0; i < _config.OffersToGenerate; i++)
            {
                _activeOffersCache.Add(_offerGenerator.GenerateOffer());
            }

            ITradeDataWriter writer = PlayerDataService.Instance;
            writer.SetActiveOffers(_activeOffersCache);

            OnOffersListUpdated?.Invoke(_activeOffersCache.AsReadOnly());
        }

        #endregion

        #region Timer Logic

        public bool IsReadyToRefresh() => _isReadyToRefresh;

        public DateTime GetNextRefreshTime() => PlayerDataService.Instance.GetNextTradeRefreshTime();

        public int GetSkipRefreshGemCost() => _skipRefreshGemCost;

        // Handles the countdown timer logic and triggers events on tick.
        private void HandleTimerTick()
        {
            if (_isReadyToRefresh) return;

            // Calculating the difference between the target refresh time and the current time to determine how much time is left until the next refresh.
            TimeSpan diff = _nextRefreshTime - DateTime.UtcNow;

            // =================================================Calculate the skip refresh gem cost based on the remaining time TEMPORARY
            var newSkipCost = Mathf.CeilToInt((float)diff.TotalMinutes * _config.SkipRefreshGemCostPerMinute);
            if (newSkipCost != _skipRefreshGemCost)
            {
                _skipRefreshGemCost = newSkipCost;
                OnSkipCostChanged?.Invoke(_skipRefreshGemCost);
            }
            //============================================================================================================================

            if (diff.TotalSeconds <= 0)
            {
                SetTimerState(isReady: true);
                return;
            }

            // UI-optimization: Only trigger the timer tick event when the integer value changes
            int currentIntTimer = Mathf.CeilToInt((float)diff.TotalSeconds);
            if (currentIntTimer != _lastIntTimer)
            {
                _lastIntTimer = currentIntTimer;
                OnTimerTick?.Invoke(currentIntTimer);
            }
        }

        // Resets the timer to the configured cooldown and updates the next refresh time in PlayerDataService.
        private void ResetTimer()
        {
            _nextRefreshTime = DateTime.UtcNow.AddSeconds(_config.RefreshTradesCooldown);

            ITradeDataWriter writer = PlayerDataService.Instance;
            writer.SetNextTradeRefreshTime(_nextRefreshTime);

            SetTimerState(isReady: false);
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
                    resources.Add(new ResourceAmount(type, PlayerDataService.Instance.GetResourceAmount(type)));
                }   
            }

            return resources.AsReadOnly();
        }

        /// <summary>
        /// Gets the current amount of the specified resource type.
        /// </summary>
        public int GetResourceAmount(ResourceType type)
        {
            return PlayerDataService.Instance.GetResourceAmount(type);
        }

        /// <summary>
        /// Returns CURRENT storage capacity for the specified resource type.
        /// </summary>
        public int GetCurrentResourceCapacity(ResourceType type)
        {
            int level = PlayerDataService.Instance.GetStorageLevel(type);
            return _storageUpgradeCalculator.GetCapacity(level);
        }

        /// <summary>
        /// Calculates the storage capacity for the next upgrade level of the specified resource type.
        /// </summary>
        public int GetNextResourceCapacity(ResourceType type)
        {
            int currentLevel = PlayerDataService.Instance.GetStorageLevel(type);
            int nextLevel = currentLevel + 1;
            return _storageUpgradeCalculator.GetCapacity(nextLevel);
        }

        /// <summary>
        /// Calculates the cost to upgrade specific resource storage to the NEXT level.
        /// </summary>
        public int GetStorageUpgradeCost(ResourceType type, bool isPremium)
        {
            int currentLevel = PlayerDataService.Instance.GetStorageLevel(type);
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

            if (!TransactionService.Instance.TryApplyTransaction(TransactionOperation.Spend(costType, cost)))
                return false;

            int newLevel = PlayerDataService.Instance.GetStorageLevel(type) + 1;
            
            ITradeDataWriter writer = PlayerDataService.Instance;
            writer.SetStorageLevel(type, newLevel);

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
                    TransactionService.Instance.RegisterHandler(type, this);
                }
            }
        }

        
        bool IResourceLogicHandler.CanApplyTransactionOperation(ResourceType type, int currentAmount, int delta)
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

        int IResourceLogicHandler.CalculateTransactionOperation(ResourceType type, int currentAmount, int delta)
        {
            int capacity = GetCurrentResourceCapacity(type);
            return Mathf.Clamp(currentAmount + delta, 0, capacity);
        }

        #endregion
    }
}