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

        // --- State ---
        private List<LawData> _availableLaws = new List<LawData>();
        private LawData _activeLaw;
        private int _lawsLeftToExecute;
        private static readonly ResourceType[] _policyTypes = Enum.GetValues(typeof(ResourceType))
            .Cast<ResourceType>()
            .Where(t => t.IsPolicyValue())
            .ToArray();

        // --- Dependencies ---
        private LawCooldownTimer _cooldownTimer;
        private PolicyUpgradeCalculator _policyUpgradeCalculator;

        // --- Events ---
        public event Action OnDataReady;
        public event Action<ResourceType> OnPolicyAmountChanged;
        public event Action<int> OnLawsCountChanged;
        public event Action<int> OnTimerSecondsTick;

        // --- Properties ---
        public bool IsDataReady { get; private set; }
        public int MaxLawsCount => _lawConfig.MaxAvailableLaws;
        public int CurrentLawsCount => _lawsLeftToExecute;

        #region Unity Lifecycle & Initialization

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _cooldownTimer = new LawCooldownTimer();
            _policyUpgradeCalculator = new PolicyUpgradeCalculator(_lawConfig, PlayerDataService.Instance.GetResourceAmount);
        }

        private void Start()
        {
            InitializeLaws();
            InitializeTimer();

            RegisterPolicyHandlers();
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

            UnregisterPolicyHandlers();
            UnregisterTimerEvents();

            if (PlayerDataService.Instance != null)
                PlayerDataService.Instance.OnResourceChanged -= HandleResourceChange;
        }

        #endregion

        #region Policies Logic

        /// <summary>
        /// Retrieves a read-only collection of policy resources associated with the player.
        /// </summary>
        public IReadOnlyList<ResourceAmount> GetPlayerPolicies()
        {
            var resources = new List<ResourceAmount>(_policyTypes.Length);

            foreach (var type in _policyTypes)
                resources.Add(new ResourceAmount(type, PlayerDataService.Instance.GetResourceAmount(type)));

            return resources.AsReadOnly();
        }

        /// <summary>
        /// Returns all the necessary data to display the current progress of a policy.
        /// </summary>
        public PolicyProgressData GetPolicyProgressData(ResourceType type) => _policyUpgradeCalculator.GetPolicyProgressData(type);

        /// <summary>
        /// Attempts to upgrade the specified policy by spending gems. If the player has enough gems, it applies the transaction,
        /// adds the necessary experience points to reach the next level, and returns true. Otherwise, it returns false
        /// </summary>
        public bool TryUpgradePolicy(ResourceType type)
        {
            if (!type.IsPolicyValue()) return false;

            var policyProgressData = GetPolicyProgressData(type);

            var transaction = TransactionOperation.Spend(ResourceType.Gems, policyProgressData.UpgradeCostGems);
            if (TransactionService.Instance.TryApplyTransaction(transaction))
            {
                TransactionService.Instance.TryApplyTransaction(TransactionOperation.Add(type, policyProgressData.MissingXp));
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if the player has enough resource to pay the cost.
        /// </summary>
        public bool CanAfford(ResourceType type, int cost)
        {
            return TransactionService.Instance.CanApplyTransaction(TransactionOperation.Spend(type, cost));
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
        /// Returns the data necessary to display the current replenishment status of laws.
        /// </summary>
        public LawsReplenishData GetLawsReplenishData()
        {
            int lawsToReplenish = _lawConfig.MaxAvailableLaws - _lawsLeftToExecute;
            int totalCost = lawsToReplenish * _lawConfig.LawReplenishCost;

            DateTime totalReplenishTime = _cooldownTimer.IsRunning
                ? _cooldownTimer.TargetTime.AddSeconds((lawsToReplenish - 1) * _lawConfig.ReplenishCooldownSeconds)
                : DateTime.UtcNow;

            return new LawsReplenishData(totalReplenishTime, totalCost);
        }

        /// <summary>
        /// Attempts to replenish the player's available laws to the maximum by spending gems.
        /// If the player has enough gems, it applies the transaction and updates the laws count to maximum
        /// </summary>
        public bool TryReplenishLaws()
        {
            if (_lawsLeftToExecute >= _lawConfig.MaxAvailableLaws) return false;

            int cost = (_lawConfig.MaxAvailableLaws - _lawsLeftToExecute) * _lawConfig.LawReplenishCost;
            var transaction = TransactionOperation.Spend(ResourceType.Gems, cost);

            if (TransactionService.Instance.TryApplyTransaction(transaction))
            {
                UpdateLawsCount(_lawConfig.MaxAvailableLaws);
                _cooldownTimer.Stop();

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
            if (_activeLaw == null && _lawsLeftToExecute > 0)
            {
                ILawDataWriter writer = PlayerDataService.Instance;

                // If there are no available laws left, we reset the used laws and refill the available laws list
                if (_availableLaws.Count == 0)
                {
                    writer.ResetUsedLaws();
                    _availableLaws.AddRange(_lawConfig.GetAllLaws());
                }

                int randomIndex = UnityEngine.Random.Range(0, _availableLaws.Count);
                _activeLaw = _availableLaws[randomIndex];

                _availableLaws.RemoveAt(randomIndex);

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
    
            ILawDataWriter writer = PlayerDataService.Instance;
            writer.MarkLawAsUsed(law.Id);

            var effects = accepted ? law.OnAcceptEffects : law.OnRejectEffects;

            var transaction = new List<TransactionOperation>(effects.Count);
            foreach (var effect in effects)
                transaction.Add(new TransactionOperation(effect.Type, effect.Amount, forceApply: true));

            TransactionService.Instance.TryApplyTransaction(transaction);
            UpdateLawsCount(-1);

            // if the player had maximum laws before executing this one, we start the cooldown for replenishment
            if (_lawsLeftToExecute == _lawConfig.MaxAvailableLaws - 1)
            {
                _cooldownTimer.Start(DateTime.UtcNow.AddSeconds(_lawConfig.ReplenishCooldownSeconds));
                writer.SetNextLawRefreshTime(_cooldownTimer.TargetTime);
            }
        }

        private void UpdateLawsCount(int amount)
        {
            _lawsLeftToExecute = Mathf.Clamp(_lawsLeftToExecute + amount, 0, _lawConfig.MaxAvailableLaws);

            ILawDataWriter writer = PlayerDataService.Instance;
            writer.SetAvailableLawsCount(_lawsLeftToExecute);

            OnLawsCountChanged?.Invoke(_lawsLeftToExecute);
        }

        // Initializes the list of available laws based on the configuration and game data, and sets the active law if one is already selected.
        private void InitializeLaws()
        {
            _lawsLeftToExecute = PlayerDataService.Instance.GetAvailableLawsCount();

            string savedActiveLawId = PlayerDataService.Instance.GetActiveLawId();
            var usedLawIds = PlayerDataService.Instance.GetUsedLawIds();
            var allLaws = _lawConfig.GetAllLaws();

            _availableLaws = new List<LawData>(allLaws.Count);

            foreach (var law in allLaws)
            {
                if (law.Id == savedActiveLawId)
                {
                    _activeLaw = law;
                }
                else if (!usedLawIds.Contains(law.Id))
                {
                    _availableLaws.Add(law);
                }
            }
        }

        #endregion

        #region Timer Logic

        // Loads the timer state based on saved next refresh time.
        private void InitializeTimer()
        {
            RegisterTimerEvents();

            ProcessOfflineProgress();
        }

        private void RegisterTimerEvents()
        {
            _cooldownTimer.OnTickSeconds += NotifySecondsLeft;
            _cooldownTimer.OnFinished += HandleTimerFinished;
        }

        private void NotifySecondsLeft()
        {
            int secondsLeft = Mathf.CeilToInt((float)_cooldownTimer.RemainingTime.TotalSeconds);
            OnTimerSecondsTick?.Invoke(secondsLeft);
        }

        private void HandleTimerFinished()
        {
            UpdateLawsCount(1);

            // If still hasn't fully replenished, setting the next replenish time based on the cooldown
            if (_lawsLeftToExecute < _lawConfig.MaxAvailableLaws)
            {
                _cooldownTimer.Start(DateTime.UtcNow.AddSeconds(_lawConfig.ReplenishCooldownSeconds));

                ILawDataWriter writer = PlayerDataService.Instance;
                writer.SetNextLawRefreshTime(_cooldownTimer.TargetTime);
            }
        }

        private void UnregisterTimerEvents()
        {
            _cooldownTimer.OnTickSeconds -= NotifySecondsLeft;
            _cooldownTimer.OnFinished -= HandleTimerFinished;
        }

        // Processes the offline progress for law replenishment based on the last saved next replenish time and the current time.
        private void ProcessOfflineProgress()
        {
            if (_lawsLeftToExecute >= _lawConfig.MaxAvailableLaws) return;

            var nextReplenishTime = PlayerDataService.Instance.GetNextLawsRefreshTime();

            // If the next replenish time is still in the future, we just start the timer with that target
            if (DateTime.UtcNow < nextReplenishTime)
            {
                _cooldownTimer.Start(nextReplenishTime);
                return;
            }

            // else - calculating how many replenish cycles have passed since the next replenish time, and updating the laws count accordingly
            TimeSpan passedTime = DateTime.UtcNow - nextReplenishTime;

            // +1 becouse the _nextReplenishTime was already reached, plus the number of full replenish cycles that passed since then
            int lawsToReplenish = 1 + (int)(passedTime.TotalSeconds / _lawConfig.ReplenishCooldownSeconds);
            UpdateLawsCount(lawsToReplenish);

            // If still hasn't fully replenished, setting the next replenish time based on how many laws were recovered
            if (_lawsLeftToExecute < _lawConfig.MaxAvailableLaws)
            {
                _cooldownTimer.Start(nextReplenishTime.AddSeconds(lawsToReplenish * _lawConfig.ReplenishCooldownSeconds));

                ILawDataWriter writer = PlayerDataService.Instance;
                writer.SetNextLawRefreshTime(_cooldownTimer.TargetTime);
            }

        }

        #endregion

        #region IResourceLogicHandler Implementation

        private void RegisterPolicyHandlers()
        {
            if (TransactionService.Instance != null)
            {
                foreach (var type in _policyTypes)
                {
                    TransactionService.Instance.RegisterHandler(type, this);
                }
            }
        }

        private void UnregisterPolicyHandlers()
        {
            if (TransactionService.Instance != null)
            {
                foreach (var type in _policyTypes)
                {
                    TransactionService.Instance.UnregisterHandler(type);
                }
            }
        }

        /// Calculates the new amount for a policy resource after applying a transaction, ensuring that it does not drop below the "floor"
        /// defined by the current level's experience requirements.
        int IResourceLogicHandler.CalculateTransactionOperation(ResourceType type, int currentAmount, int delta)
        {
            if (delta >= 0) return currentAmount + delta;

            int newAmount = currentAmount + delta;

            var progressData = GetPolicyProgressData(type);

            return Mathf.Max(newAmount, progressData.TotalFloorXp);
        }

        // For policies, we allow all transactions, but they will be adjusted in CalculateTransactionOperation
        bool IResourceLogicHandler.CanApplyTransactionOperation(ResourceType type, int currentAmount, int delta)
        {
            return true;
        }

        #endregion
    }
}
