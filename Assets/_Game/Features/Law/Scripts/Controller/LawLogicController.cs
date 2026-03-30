using Game.General;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Features.Law
{
    /// <summary>
    /// Provides centralized management and retrieval of player policy information within the game.
    /// Acts as a bridge between DataService and View
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
                Destroy(this);
                return;
            }
            Instance = this;

            // Register this controller as a handler for policy-related resource types
            RegisterHandler();
        }

        private void Start()
        {
            GameDataService.Instance.OnResourceChanged += HandleResourceChange;
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
            GameDataService.Instance.OnResourceChanged -= HandleResourceChange;
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
            var usedLawIds = new HashSet<string>(GameDataService.Instance.GetUsedLawIds());
            _availableLaws = _lawConfig.GetAllLaws().Where(law => !usedLawIds.Contains(law.Id)).ToList();

            _lawsLeftToExecute = GameDataService.Instance.GetAvailableLawsCount();

            _activeLaw = _availableLaws.FirstOrDefault(law => law.Id == GameDataService.Instance.GetActiveLawId());
        }

        // Loads the timer state based on saved next refresh time.
        private void InitializeTimer()
        {
            _nextReplenishTime = GameDataService.Instance.GetNextLawsRefreshTime();
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
                    policies.Add(new ResourceAmount(type, GameDataService.Instance.GetAmount(type)));
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
            int totalXp = GameDataService.Instance.GetAmount(type);

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

        public bool TryUpgradePolicy(ResourceType type)
        {
            if (!type.IsPolicyValue()) return false;

            var transaction = TransactionOperation.Spend(ResourceType.Gems, GetPolicyXpUpgradeCost(type));
            if (GameDataService.Instance.TryApplyTransaction(transaction))
            {
                var levelData = GetPolicyLevelData(type);
                var xpToNextLevel = levelData.requiredXp - levelData.currentXp;
                GameDataService.Instance.TryApplyTransaction(TransactionOperation.Add(type, xpToNextLevel));
                return true;
            }

            return false;
        }

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

        public DateTime GetTotalLawsReplenishTime()
        {
            EnsureInitialized();

            int lawsToReplenish = _lawConfig.MaxAvailableLaws - _lawsLeftToExecute;
            return _nextReplenishTime.AddSeconds((lawsToReplenish - 1) * _lawConfig.ReplenishCooldownSeconds);
        }

        public int GetTotalLawsReplenishCost()
        {
            EnsureInitialized();

            int lawsToReplenish = _lawConfig.MaxAvailableLaws - _lawsLeftToExecute;
            return lawsToReplenish * _lawConfig.LawReplenishCost;
        }

        public void TryReplenishLaws()
        {
            EnsureInitialized();

            if (_lawsLeftToExecute >= _lawConfig.MaxAvailableLaws) return;

            var transaction = TransactionOperation.Spend(ResourceType.Gems, GetTotalLawsReplenishCost());
            if (GameDataService.Instance.TryApplyTransaction(transaction))
            {
                UpdateLawsCount(_lawConfig.MaxAvailableLaws);
                _nextReplenishTime = DateTime.MinValue;
                GameDataService.Instance.SetNextLawsRefreshTime(_nextReplenishTime);
            }
        }

        /// <summary>
        /// Returns the active law if the player has laws left available to execute. Otherwise, returns null.
        /// </summary>
        public bool TryGetActiveLaw(out LawData law)
        {
            EnsureInitialized();

            if (_activeLaw == null && _lawsLeftToExecute > 0)
            {
                _activeLaw = GetRandomLaw();
                GameDataService.Instance.SaveActiveLawId(_activeLaw.Id);
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

            var law = _activeLaw;
            _activeLaw = null;

            _availableLaws.Remove(law);
            GameDataService.Instance.SaveUsedLawId(law.Id);

            var effects = accepted ? law.OnAcceptEffects : law.OnRejectEffects;
            var transaction = effects.Select(e => new TransactionOperation(e.Type, e.Amount, forceApply: true)).ToList();
            GameDataService.Instance.TryApplyTransaction(transaction);

            if (_lawsLeftToExecute == _lawConfig.MaxAvailableLaws)
            {
                _nextReplenishTime = DateTime.UtcNow.AddSeconds(_lawConfig.ReplenishCooldownSeconds);
                GameDataService.Instance.SetNextLawsRefreshTime(_nextReplenishTime);
            }

            UpdateLawsCount(-1);
        }

        /// <summary>
        /// Checks if the player has enough resource to pay the cost.
        /// </summary>
        public bool CanAfford(ResourceType type, int cost)
        {
            return GameDataService.Instance.CanApplyTransaction(TransactionOperation.Spend(type, cost));
        }

        private LawData GetRandomLaw()
        {
            if (_availableLaws.Count == 0)
            {
                GameDataService.Instance.ResetUsedLawIds();
                _availableLaws = _lawConfig.GetAllLaws().ToList();
            }

            return _availableLaws[UnityEngine.Random.Range(0, _availableLaws.Count)];
        }

        private void UpdateLawsCount(int amount)
        {
            _lawsLeftToExecute = Mathf.Clamp(_lawsLeftToExecute + amount, 0, _lawConfig.MaxAvailableLaws);

            GameDataService.Instance.UpdateAvailableLawsCount(_lawsLeftToExecute);
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
                    GameDataService.Instance.SetNextLawsRefreshTime(_nextReplenishTime);
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

                if (_lawsLeftToExecute < _lawConfig.MaxAvailableLaws)
                {
                    _nextReplenishTime = _nextReplenishTime.AddSeconds(_lawConfig.ReplenishCooldownSeconds);
                    GameDataService.Instance.SetNextLawsRefreshTime(_nextReplenishTime);
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
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                if (type.IsPolicyValue())
                {
                    GameDataService.Instance.RegisterHandler(type, this);
                }
            }
        }

        public int CalculateTransactionOperation(ResourceType type, int currentAmount, int delta)
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

        public bool CanApplyTransactionOperation(ResourceType type, int currentAmount, int delta)
        {
            return true; // For policies, we allow all transactions, but they will be adjusted in CalculateTransactionOperation
        }

        #endregion
    }
}
