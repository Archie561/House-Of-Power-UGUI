using Game.General;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Features.Law
{
    /// <summary>
    /// Controls the core logic for the Law system, including managing available laws, handling policy levels and experience
    /// and processing law execution and replenishment. Acts as a bridge between PlayerDataService and View
    /// </summary>
    public class LawLogicController : MonoBehaviour, IResourceLogicHandler
    {
        public static LawLogicController Instance { get; private set; }

        [Header("Config")]
        [SerializeField] private LawConfig _lawConfig;

        // --- Events ---
        public event Action<ResourceType> OnPolicyAmountChanged;
        public event Action<int> OnLawsCountChanged;
        public event Action<int> OnTimerTick;

        // --- State ---
        private List<LawData> _availableLaws = new List<LawData>();
        private bool _isInitialized = false;
        private LawData _activeLaw;
        private int _lawsLeftToExecute;
        private DateTime _nextReplenishTime;
        private int _lastIntTimer = -1; // For UI events optimization

        #region Unity Lifecycle & Initialization

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
            // Register this controller as a handler for policy-related resource types
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
            UnregisterHandler();

            PlayerDataService.Instance.OnResourceChanged -= HandleResourceChange;
            if (Instance == this) Instance = null;
        }

        private void EnsureInitialized()
        {
            if (_isInitialized) return;

            InitializeLaws();
            InitializeTimer();

            _isInitialized = true;
        }

        // Initializes the list of available laws based on the configuration and game data, and sets the active law if one is already selected.
        private void InitializeLaws()
        {
            var usedLawIds = new HashSet<string>(PlayerDataService.Instance.GetUsedLawIds());
            _availableLaws = _lawConfig.GetAllLaws().Where(law => !usedLawIds.Contains(law.Id)).ToList();

            _lawsLeftToExecute = PlayerDataService.Instance.GetAvailableLawsCount();

            _activeLaw = _availableLaws.FirstOrDefault(law => law.Id == PlayerDataService.Instance.GetActiveLawId());
        }

        // Loads the timer state based on saved next refresh time.
        private void InitializeTimer()
        {
            _nextReplenishTime = PlayerDataService.Instance.GetNextLawsRefreshTime();
            ProcessOfflineProgress();
            HandleTimerTick(); // To update the timer state immediately after processing offline progress
            UpdateLawsCount(0); // To trigger the UI update for laws count based on the loaded state
        }

        #endregion

        #region Policies Logic

        /// <summary>
        /// Retrieves a read-only collection of policy resources associated with the player.
        /// </summary>
        public IReadOnlyList<ResourceAmount> GetPlayerPolicies()
        {
            List<ResourceAmount> policies = new List<ResourceAmount>();

            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                if (type.IsPolicyValue())
                {
                    policies.Add(new ResourceAmount(type, PlayerDataService.Instance.GetResourceAmount(type)));
                }
            }

            return policies.AsReadOnly();
        }

        /// <summary>
        /// Calculates the current level, accumulated experience points, and required experience points for the next
        /// level for a specified resource type.
        /// </summary>
        public (int level, int currentXp, int requiredXp) GetPolicyLevelData(ResourceType type)
        {
            int totalXp = PlayerDataService.Instance.GetResourceAmount(type);

            int currentLevel = 1;
            int requiredXp = _lawConfig.BaseRequiredXpForLevel;

            // Subtracting the necessary experience until there are enough total points
            while (totalXp >= requiredXp)
            {
                totalXp -= requiredXp;
                currentLevel++;
                requiredXp += _lawConfig.XpIncreasePerLevel;
            }

            // What is left in totalXp after all subtractions is the current level progress
            return (currentLevel, totalXp, requiredXp);
        }

        /// <summary>
        /// Calculates the cost in gems to upgrade the specified policy to the next level,
        /// based on the experience points needed and the cost per 10 XP defined in the configuration.
        /// </summary>
        public int GetPolicyXpUpgradeCost(ResourceType type)
        {
            if (!type.IsPolicyValue()) return 0;

            var levelData = GetPolicyLevelData(type);
            var xpToNextLevel = levelData.requiredXp - levelData.currentXp;

            if (xpToNextLevel <= 0) return 0;

            // most efficient way to round up to the nearest 10 and calculate the cost based on that
            int packsNeeded = (xpToNextLevel + 9) / 10;
            return packsNeeded * _lawConfig.CostPer10Xp;
        }

        /// <summary>
        /// Attempts to upgrade the specified policy by spending gems. If the player has enough gems, it applies the transaction,
        /// adds the necessary experience points to reach the next level, and returns true. Otherwise, it returns false
        /// </summary>
        public bool TryUpgradePolicy(ResourceType type)
        {
            if (!type.IsPolicyValue()) return false;

            var transaction = TransactionOperation.Spend(ResourceType.Gems, GetPolicyXpUpgradeCost(type));
            if (TransactionService.Instance.TryApplyTransaction(transaction))
            {
                var levelData = GetPolicyLevelData(type);
                var xpToNextLevel = levelData.requiredXp - levelData.currentXp;
                TransactionService.Instance.TryApplyTransaction(TransactionOperation.Add(type, xpToNextLevel));
                return true;
            }

            return false;
        }

        /// Handles changes to resources, specifically looking for changes in policy values to trigger the appropriate events for UI updates.
        private void HandleResourceChange(ResourceChangeData data)
        {
            if (data.Type.IsPolicyValue())
            {
                OnPolicyAmountChanged?.Invoke(data.Type);
            }
        }

        #endregion

        #region Laws Logic

        /// <summary>
        /// Gets the number of laws remaining to be executed.
        /// </summary>
        public int GetCurrentLawsCount()
        {
            EnsureInitialized();
            return _lawsLeftToExecute;
        }

        /// <summary>
        /// Returns the number of maximum available laws
        /// </summary>
        public int GetMaxLawsCount() => _lawConfig.MaxAvailableLaws;

        /// <summary>
        /// Calculates the total time required to fully replenish the player's available laws
        /// </summary>
        public DateTime GetTotalLawsReplenishTime()
        {
            EnsureInitialized();
            int lawsToReplenish = _lawConfig.MaxAvailableLaws - _lawsLeftToExecute;
            return _nextReplenishTime.AddSeconds((lawsToReplenish - 1) * _lawConfig.ReplenishCooldownSeconds);
        }

        /// <summary>
        /// Calculates the total cost in gems to fully replenish the player's available laws
        /// </summary>
        public int GetTotalLawsReplenishCost()
        {
            EnsureInitialized();
            int lawsToReplenish = _lawConfig.MaxAvailableLaws - _lawsLeftToExecute;
            return lawsToReplenish * _lawConfig.LawReplenishCost;
        }

        /// <summary>
        /// Attempts to replenish the player's available laws to the maximum by spending gems.
        /// If the player has enough gems, it applies the transaction and updates the laws count to maximum
        /// </summary>
        public bool TryReplenishLaws()
        {
            EnsureInitialized();

            if (_lawsLeftToExecute >= _lawConfig.MaxAvailableLaws) return false;

            var transaction = TransactionOperation.Spend(ResourceType.Gems, GetTotalLawsReplenishCost());
            if (TransactionService.Instance.TryApplyTransaction(transaction))
            {
                UpdateLawsCount(_lawConfig.MaxAvailableLaws);
                _nextReplenishTime = DateTime.MinValue;
                
                ILawDataWriter writer = PlayerDataService.Instance;
                writer.SetNextLawRefreshTime(_nextReplenishTime);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Attemps to get the currently active law. If there is no active law but there are laws left to execute,
        /// it randomly selects a new active law from the available laws.
        /// </summary>
        public bool TryGetActiveLaw(out LawData law)
        {
            EnsureInitialized();

            if (_activeLaw == null && _lawsLeftToExecute > 0)
            {
                _activeLaw = GetRandomLaw();
                
                ILawDataWriter writer = PlayerDataService.Instance;
                writer.SetActiveLawId(_activeLaw.Id);
            }

            law = _activeLaw;
            return law != null;
        }

        /// <summary>
        /// Executes the currently active law, applying its effects and updating the list of available laws.
        /// </summary>
        public void ExecuteActiveLaw(bool accepted)
        {
            if (_activeLaw == null) return;

            // Nulling the active law to prevent double execution
            var law = _activeLaw;
            _activeLaw = null;

            // Marking this law as used and removing it from the available pool
            _availableLaws.Remove(law);
            
            ILawDataWriter writer = PlayerDataService.Instance;
            writer.MarkLawAsUsed(law.Id);

            var effects = accepted ? law.OnAcceptEffects : law.OnRejectEffects;
            var transaction = effects.Select(e => new TransactionOperation(e.Type, e.Amount, forceApply: true)).ToList();
            TransactionService.Instance.TryApplyTransaction(transaction);

            UpdateLawsCount(-1);

            // if the player had maximum laws before executing this one, we start the cooldown for replenishment
            if (_lawsLeftToExecute == _lawConfig.MaxAvailableLaws - 1)
            {
                _nextReplenishTime = DateTime.UtcNow.AddSeconds(_lawConfig.ReplenishCooldownSeconds);
                writer.SetNextLawRefreshTime(_nextReplenishTime);
            }
        }

        /// <summary>
        /// Checks if the player has enough resource to pay the cost.
        /// </summary>
        public bool CanAfford(ResourceType type, int cost)
        {
            return TransactionService.Instance.CanApplyTransaction(TransactionOperation.Spend(type, cost));
        }

        private LawData GetRandomLaw()
        {
            // Reset the available laws pool if all laws have been used, allowing them to be drawn again
            if (_availableLaws.Count == 0)
            {
                ILawDataWriter writer = PlayerDataService.Instance;
                writer.ResetUsedLaws();

                _availableLaws = _lawConfig.GetAllLaws().ToList();
            }

            return _availableLaws[UnityEngine.Random.Range(0, _availableLaws.Count)];
        }

        private void UpdateLawsCount(int amount)
        {
            _lawsLeftToExecute = Mathf.Clamp(_lawsLeftToExecute + amount, 0, _lawConfig.MaxAvailableLaws);

            ILawDataWriter writer = PlayerDataService.Instance;
            writer.SetAvailableLawsCount(_lawsLeftToExecute);

            OnLawsCountChanged?.Invoke(_lawsLeftToExecute);
        }

        #endregion

        #region Timer Logic

        // Processes the offline progress for law replenishment based on the last saved next replenish time and the current time.
        private void ProcessOfflineProgress()
        {
            if (_lawsLeftToExecute >= _lawConfig.MaxAvailableLaws) return;

            if (DateTime.UtcNow >= _nextReplenishTime)
            {
                TimeSpan passedTime = DateTime.UtcNow - _nextReplenishTime;

                // +1 becouse the _nextReplenishTime was already reached, plus the number of full replenish cycles that passed since then
                int lawsToRecover = 1 + (int)(passedTime.TotalSeconds / _lawConfig.ReplenishCooldownSeconds);
                UpdateLawsCount(lawsToRecover);

                // If still hasn't fully replenished, setting the next replenish time based on how many laws was recovered
                if (_lawsLeftToExecute < _lawConfig.MaxAvailableLaws)
                {
                    _nextReplenishTime = _nextReplenishTime.AddSeconds(lawsToRecover * _lawConfig.ReplenishCooldownSeconds);

                    ILawDataWriter writer = PlayerDataService.Instance;
                    writer.SetNextLawRefreshTime(_nextReplenishTime);
                }
            }
        }

        // Handles the countdown timer logic and triggers events on tick.
        private void HandleTimerTick()
        {
            if (_lawsLeftToExecute >= _lawConfig.MaxAvailableLaws) return;

            TimeSpan diff = _nextReplenishTime - DateTime.UtcNow;

            if (diff.TotalSeconds <= 0)
            {
                UpdateLawsCount(1);

                // If still hasn't fully replenished, setting the next replenish time based on the cooldown
                if (_lawsLeftToExecute < _lawConfig.MaxAvailableLaws)
                {
                    _nextReplenishTime = _nextReplenishTime.AddSeconds(_lawConfig.ReplenishCooldownSeconds);
                    
                    ILawDataWriter writer = PlayerDataService.Instance;
                    writer.SetNextLawRefreshTime(_nextReplenishTime);
                }
            }

            // UI-optimization: Only trigger the timer tick event when the integer value changes
            int currentIntTimer = Mathf.CeilToInt((float)diff.TotalSeconds);
            if (currentIntTimer != _lastIntTimer)
            {
                _lastIntTimer = currentIntTimer;
                OnTimerTick?.Invoke(currentIntTimer);
            }
        }

        #endregion

        #region IResourceLogicHandler Implementation

        private void RegisterHandler()
        {
            if (TransactionService.Instance != null)
            {
                foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
                {
                    if (type.IsPolicyValue())
                    {
                        TransactionService.Instance.RegisterHandler(type, this);
                    }
                }
            }
        }

        private void UnregisterHandler()
        {
            if (TransactionService.Instance != null)
            {
                foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
                {
                    if (type.IsPolicyValue())
                    {
                        TransactionService.Instance.UnregisterHandler(type);
                    }
                }
            }
        }

        /// Calculates the new amount for a policy resource after applying a transaction, ensuring that it does not drop below the "floor"
        /// defined by the current level's experience requirements.
        int IResourceLogicHandler.CalculateTransactionOperation(ResourceType type, int currentAmount, int delta)
        {
            if (delta >= 0) return currentAmount + delta;

            int newAmount = currentAmount + delta;

            // Calculating the "floor" for current level
            int floorXp = 0;
            int requiredXp = _lawConfig.BaseRequiredXpForLevel;
            int tempXp = currentAmount;

            while (tempXp >= requiredXp)
            {
                tempXp -= requiredXp;
                floorXp += requiredXp;
                requiredXp += _lawConfig.XpIncreasePerLevel;
            }

            return Mathf.Max(newAmount, floorXp);
        }

        // For policies, we allow all transactions, but they will be adjusted in CalculateTransactionOperation
        bool IResourceLogicHandler.CanApplyTransactionOperation(ResourceType type, int currentAmount, int delta)
        {
            return true;
        }

        #endregion
    }
}
