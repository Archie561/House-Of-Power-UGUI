using Game.General;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

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
        private static readonly ResourceType[] _tradeGoodTypes = Enum.GetValues(typeof(ResourceType))
            .Cast<ResourceType>()
            .Where(t => t.IsTradeGood())
            .ToArray();

        // --- Dependencies ---
        private TradeOfferGenerator _offerGenerator;
        private StorageUpgradeCalculator _storageUpgradeCalculator;
        private TradeCooldownTimer _cooldownTimer;

        // --- Events ---
        public event Action OnDataReady;
        public event Action<int> OnTimerSecondsTick;
        public event Action<bool> OnFreeTradesRefreshStatusChanged;
        public event Action<int> OnPremiumTradesRefreshCostChanged;
        public event Action<IReadOnlyList<TradeOfferData>> OnOffersListUpdated;
        public event Action<ResourceType> OnTradeGoodChanged;

        // --- Properties ---
        public bool IsDataReady { get; private set; }
        public bool IsFreeTradesRefreshAvailable => !_cooldownTimer.IsRunning;
        public int PremiumTradesRefreshCost => Mathf.CeilToInt((float)_cooldownTimer.RemainingTime.TotalMinutes) * _config.SkipRefreshGemCostPerMinute;
        public DateTime NextTradeRefreshTime => _cooldownTimer.TargetTime;

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
            _cooldownTimer = new TradeCooldownTimer();
            _storageUpgradeCalculator = new StorageUpgradeCalculator(settings: _config.StorageSettings);
            _offerGenerator = new TradeOfferGenerator(settings: _config.GenerationSettings, capacityProvider: GetResourceCapacity);
        }

        private void Start()
        {
            LoadTradeOffers();
            InitializeTimer();

            RegisterTradeGoodHandlers();
            if (PlayerDataService.Instance != null)
                PlayerDataService.Instance.OnResourceChanged += HandleResourceChange;

            IsDataReady = true;
            OnDataReady?.Invoke();
        }

        private void Update()
        {
            _cooldownTimer.Tick();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;

            UnregisterTimerEvents();
            UnregisterTradeGoodHandlers();

            if (PlayerDataService.Instance != null)
                PlayerDataService.Instance.OnResourceChanged -= HandleResourceChange;
        }

        #endregion

        #region Offers Logic

        /// <summary>
        /// Retrieves a read-only list of all currently active trade offers.
        /// </summary>
        public IReadOnlyList<TradeOfferData> GetActiveOffers()
        {
            return _activeOffersCache.AsReadOnly();
        }

        /// <summary>
        /// Updates offers list with new generated offers if the timer allows it or if paid with gems.
        /// </summary>
        /// <param name="isPremium">Indicates if the refresh is paid with gems (true) or free (false).</param>
        public bool TryRefreshOffers(bool isPremium)
        {
            if (isPremium)
            {
                var cost = TransactionOperation.Spend(ResourceType.Gems, PremiumTradesRefreshCost);
                if (!TransactionService.Instance.TryApplyTransaction(cost)) return false;
            }
            else if (!IsFreeTradesRefreshAvailable)
            {
                return false;
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
            if (offer == null || !_activeOffersCache.Contains(offer)) return false;

            int operationsCount = offer.Export.Count + offer.Import.Count;
            var transaction = new List<TransactionOperation>(operationsCount);

            foreach (var resource in offer.Export)
                transaction.Add(TransactionOperation.Spend(resource.Type, resource.Amount));

            // Add operations are enforced. An overflow warning (if any) is displayed in the UI before the transaction is confirmed.
            foreach (var resource in offer.Import)
                transaction.Add(TransactionOperation.Add(resource.Type, resource.Amount, forceApply: true));

            if (!TransactionService.Instance.TryApplyTransaction(transaction)) return false;

            _activeOffersCache.Remove(offer);

            ITradeDataWriter writer = PlayerDataService.Instance;
            writer.SetActiveOffers(_activeOffersCache);

            OnOffersListUpdated?.Invoke(_activeOffersCache.AsReadOnly());

            return true;
        }

        /// <summary>
        /// Checks if the player has enaugh export resources of the given trade offer.
        /// </summary>
        public bool CanAffordOffer(TradeOfferData offer)
        {
            var transaction = new List<TransactionOperation>(offer.Export.Count);

            foreach (var resource in offer.Export)
                transaction.Add(TransactionOperation.Spend(resource.Type, resource.Amount));

            return TransactionService.Instance.CanApplyTransaction(transaction);
        }

        // Loads saved offers from PlayerDataService 
        private void LoadTradeOffers()
        {
            var savedOffers = PlayerDataService.Instance.GetActiveOffers();
            _activeOffersCache = savedOffers != null
                ? new List<TradeOfferData>(savedOffers)
                : new List<TradeOfferData>(_config.OffersToGenerate);
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

        #region Resources & StorageLogic

        /// <summary>
        /// Returns the player's trade goods resources for UI display.
        /// </summary>
        public IReadOnlyList<ResourceAmount> GetPlayerResources()
        {
            var resources = new List<ResourceAmount>(_tradeGoodTypes.Length);

            foreach (var type in _tradeGoodTypes)
                resources.Add(new ResourceAmount(type, PlayerDataService.Instance.GetResourceAmount(type)));

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
        /// Gets the current max storage capacity for the specified resource type
        /// </summary>
        public int GetResourceCapacity(ResourceType type)
        {
            int currentLevel = PlayerDataService.Instance.GetStorageLevel(type);
            return _storageUpgradeCalculator.GetCapacityForLevel(currentLevel);
        }

        /// <summary>
        /// Checks if the player has enough resource to pay the cost.
        /// </summary>
        public bool CanAffordCost(ResourceType type, int cost)
        {
            return TransactionService.Instance.CanApplyTransaction(TransactionOperation.Spend(type, cost));
        }

        /// <summary>
        /// Gets the storage upgrade data for specified resource type for UI display
        /// </summary>
        public StorageUpgradeData GetStorageUpgradeData(ResourceType type)
        {
            int currentLevel = PlayerDataService.Instance.GetStorageLevel(type);
            int nextLevel = currentLevel + 1;

            int currentCapacity = _storageUpgradeCalculator.GetCapacityForLevel(currentLevel);
            int upgradedCapacity = _storageUpgradeCalculator.GetCapacityForLevel(nextLevel);
            int defaultCost = _storageUpgradeCalculator.GetDefaultCostForLevel(nextLevel);
            int premiumCost = _storageUpgradeCalculator.GetPremiumCostForLevel(nextLevel);

            return new StorageUpgradeData(currentCapacity, upgradedCapacity, defaultCost, premiumCost);
        }

        /// <summary>
        /// Attempts to upgrade storage for the specified resource type with premium or default cost option.
        /// </summary>
        public bool TryUpgradeCapacity(ResourceType type, bool premiumPurchase)
        {
            int nextLevel = PlayerDataService.Instance.GetStorageLevel(type) + 1;

            int cost = premiumPurchase
                ? _storageUpgradeCalculator.GetPremiumCostForLevel(nextLevel)
                : _storageUpgradeCalculator.GetDefaultCostForLevel(nextLevel);

            ResourceType costType = premiumPurchase ? ResourceType.Gems : type;

            if (!TransactionService.Instance.TryApplyTransaction(TransactionOperation.Spend(costType, cost)))
                return false;

            ITradeDataWriter writer = PlayerDataService.Instance;
            writer.SetStorageLevel(type, nextLevel);

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

        #region Timer Logic

        private void InitializeTimer()
        {
            RegisterTimerEvents();

            var savedRefreshTime = PlayerDataService.Instance.GetNextTradeRefreshTime();

            // Start the timer only if the saved refresh time is in the future.
            if (savedRefreshTime > DateTime.UtcNow)
                _cooldownTimer.Start(savedRefreshTime);
        }

        // Resets the timer to the configured cooldown and updates the next refresh time in PlayerDataService.
        private void ResetTimer()
        {
            var nextRefreshTime = DateTime.UtcNow.AddSeconds(_config.RefreshTradesCooldown);

            _cooldownTimer.Start(nextRefreshTime);

            ITradeDataWriter writer = PlayerDataService.Instance;
            writer.SetNextTradeRefreshTime(nextRefreshTime);

            OnFreeTradesRefreshStatusChanged?.Invoke(IsFreeTradesRefreshAvailable);
        }

        private void RegisterTimerEvents()
        {
            UnregisterTimerEvents();

            _cooldownTimer.OnTickSeconds += NotifySecondsLeft;
            _cooldownTimer.OnTickMinutes += NotifyPremiumTradesRefreshCostChanged;
            _cooldownTimer.OnFinished += NotifyFreeTradesRefreshAvailable;
        }

        private void UnregisterTimerEvents()
        {
            if (_cooldownTimer == null) return;

            _cooldownTimer.OnTickSeconds -= NotifySecondsLeft;
            _cooldownTimer.OnTickMinutes -= NotifyPremiumTradesRefreshCostChanged;
            _cooldownTimer.OnFinished -= NotifyFreeTradesRefreshAvailable;
        }

        private void NotifySecondsLeft()
        {
            int secondsLeft = Mathf.CeilToInt((float)_cooldownTimer.RemainingTime.TotalSeconds);
            OnTimerSecondsTick?.Invoke(secondsLeft);
        }

        private void NotifyPremiumTradesRefreshCostChanged()
        {
            OnPremiumTradesRefreshCostChanged?.Invoke(PremiumTradesRefreshCost);
        }

        private void NotifyFreeTradesRefreshAvailable()
        {
            OnFreeTradesRefreshStatusChanged?.Invoke(IsFreeTradesRefreshAvailable);
        }

        #endregion

        #region IResourceLogicHandler Implementation

        private void RegisterTradeGoodHandlers()
        {
            foreach (var type in _tradeGoodTypes)
            {
                TransactionService.Instance.RegisterHandler(type, this);
            }
        }

        private void UnregisterTradeGoodHandlers()
        {
            if (TransactionService.Instance == null) return;

            foreach (var type in _tradeGoodTypes)
            {
                TransactionService.Instance.UnregisterHandler(type);
            }
        }

        bool IResourceLogicHandler.CanApplyTransactionOperation(ResourceType type, int currentAmount, int delta)
        {
            int newAmount = currentAmount + delta;

            if (newAmount < 0) return false;

            if (delta > 0)
            {
                int capacity = GetResourceCapacity(type);
                if (newAmount > capacity) return false;
            }

            return true;
        }

        int IResourceLogicHandler.CalculateTransactionOperation(ResourceType type, int currentAmount, int delta)
        {
            int capacity = GetResourceCapacity(type);
            return Mathf.Clamp(currentAmount + delta, 0, capacity);
        }

        #endregion
    }
}