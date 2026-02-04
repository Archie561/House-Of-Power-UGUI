using Game.Features.Trade;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Game.General
{
    /// <summary>
    /// The central service responsible for persistent data management (Save/Load) 
    /// and core economy logic (Transactions, stats adjustments).
    /// Acts as a Single Source of Truth for the game state.
    /// </summary>
    [DefaultExecutionOrder(-100)] // Ensure this initializes early
    public class GameDataService : MonoBehaviour
    {
        public static GameDataService Instance { get; private set; }

        #region Configuration & State

        [Header("Configuration")]
        [SerializeField] private GameConfig _gameConfig;

        private const string SAVE_FILE_NAME = "playerdata.json";

        private PlayerData _playerData;
        private string _saveFilePath;
        private bool _isDirty;

        // Handlers that determine the logic of operations on resource values
        private Dictionary<ResourceType, IResourceLogicHandler> _logicHandlers;

        #endregion

        #region Events

        public event Action<ResourceChangeData> OnResourceChanged;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _saveFilePath = Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);

            LoadPlayerData();
        }

        private void LateUpdate()
        {
            if (_isDirty)
            {
                SavePlayerData();
                _isDirty = false;
            }
        }

        // Critical for mobile: Save when user minimizes the app
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && _isDirty)
            {
                SavePlayerData();
            }
        }

        private void OnApplicationQuit()
        {
            if (_isDirty)
            {
                SavePlayerData();
            }
        }

        #endregion

        #region Save / Load System

        private void LoadPlayerData()
        {
            if (!File.Exists(_saveFilePath))
            {
                _playerData = GenerateNewPlayerData();
                _isDirty = true;
            }
            else
            {
                try
                {
                    string json = File.ReadAllText(_saveFilePath);
                    _playerData = JsonConvert.DeserializeObject<PlayerData>(json);

                    // Validate data integrity (in case save file is old version)
                    if (_playerData == null) throw new Exception("Deserialized data is null");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[GameDataService] Failed to load data: {e.Message}. Creating new.");
                    _playerData = GenerateNewPlayerData();
                    _isDirty = true;
                }
            }
        }

        private void SavePlayerData()
        {
            try
            {
                string json = JsonConvert.SerializeObject(_playerData, Formatting.Indented);
                File.WriteAllText(_saveFilePath, json);
                Debug.Log("[GameDataService] Game Saved.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameDataService] Failed to save data: {e.Message}");
            }
        }

        private PlayerData GenerateNewPlayerData()
        {
            // Initial Game State Configuration
            Dictionary<ResourceType, int> resources = _gameConfig.GetInitialResources();
            Dictionary<ResourceType, int> storageLevels = _gameConfig.GetInitialStorageLevels();
            List<TradeOfferData> activeTradeOffers = _gameConfig.GetInitialOffers();

            DateTime nextRefresh = DateTime.Now.AddSeconds(_gameConfig.GetInitialRefreshTime());
            bool isFirstSession = _gameConfig.IsFirstGameSession();

            // Ensure all enums exist (safety check)
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                if (!resources.ContainsKey(type))
                {
                    resources.Add(type, 0); // Default amount
                }

                if (type.IsTradeGood() && !storageLevels.ContainsKey(type))
                {
                    storageLevels.Add(type, 1); // Default level
                }
            }

            return new PlayerData(
                resources,
                storageLevels,
                isFirstSession,
                nextRefresh,
                activeTradeOffers
            );
        }

        #endregion

        #region Economy Public API

        /// <summary>
        /// Registers handler that defines custom logic for resource modifications.
        /// </summary>
        public void RegisterHandler(ResourceType type, IResourceLogicHandler handler)
        {
            if (_logicHandlers == null)
            {
                _logicHandlers = new Dictionary<ResourceType, IResourceLogicHandler>();
            }

            // There is no duplicate check here, because when the scene is reloaded, the controllers will be re-registered.
            _logicHandlers[type] = handler;
        }

        /// <summary>
        /// Determines whether the specified transaction operation can be successfully applied after checking conditions by handlers, if any.
        /// If operation.ForceApply equals true, skips validation for that operation.
        /// </summary>
        /// <param name="operations">The transaction operations to apply. Determines the type of resource affected, the amount to change, and
        /// whether to force the application of the transaction.</param>
        /// <returns>true if the transaction can be applied; otherwise, false.</returns>
        public bool CanApplyTransaction(IReadOnlyList<TransactionOperation> operations)
        {
            var simulationCache = new Dictionary<ResourceType, int>();

            foreach (var op in operations)
            {
                if (!simulationCache.ContainsKey(op.Type))
                    simulationCache[op.Type] = GetAmount(op.Type);

                int currentSimulated = simulationCache[op.Type];

                if (!op.ForceApply)
                {
                    if (!CanApplyTransactionOperation(op.Type, currentSimulated, op.Amount))
                        return false;
                }

                simulationCache[op.Type] = CalculateTransactionOperation(op.Type, currentSimulated, op.Amount);
            }

            return true;
        }

        // Method overloading for single parameter
        public bool CanApplyTransaction(TransactionOperation operation)
        {
            if (operation.ForceApply) return true;

            return CanApplyTransactionOperation(operation.Type, GetAmount(operation.Type), operation.Amount);
        }

        /// <summary>
        /// Attempts to complete the transaction after checking conditions by handlers.
        /// </summary>
        /// <param name="operations">The transaction operations to apply. Determines the type of resource affected, the amount to change, and
        /// whether to force the application of the transaction.</param>
        /// <returns>true if the transaction was successfully applied; otherwise, false.</returns>
        public bool TryApplyTransaction(IReadOnlyList<TransactionOperation> operations)
        {
            if (!CanApplyTransaction(operations))
                return false;

            foreach (var op in operations)
            {
                int currentAmount = GetAmount(op.Type);
                int newAmount = CalculateTransactionOperation(op.Type, currentAmount, op.Amount);
                ApplyResourceChange(op.Type, currentAmount, newAmount);
            }

            return true;
        }

        // Method overloading for single parameter
        public bool TryApplyTransaction(TransactionOperation operation)
        {
            if (!CanApplyTransaction(operation)) return false;

            int currentAmount = GetAmount(operation.Type);
            int newAmount = CalculateTransactionOperation(operation.Type, currentAmount, operation.Amount);
            ApplyResourceChange(operation.Type, currentAmount, newAmount);

            return true;
        }

        /// <summary>
        /// Returns the current amount of the specified resource type.
        /// </summary>
        public int GetAmount(ResourceType type)
        {
            try
            {
                int amount = _playerData.Resources[type];
                return amount;
            }
            catch (Exception ex)
            {
                throw new Exception($"[GameDataService] Failed to get amount for resource type {type}: {ex.Message}");
            }
        }

        /// <summary>
        /// Returns the storage capacity level for the specified trade good resource type. Returns int.MaxValue for non-trade goods.
        /// </summary>
        public int GetStorageLevel(ResourceType type)
        {
            if (!type.IsTradeGood())
            {
                Debug.LogWarning($"[GameDataService] Requested storage level for non-trade good type {type}. Returning int.MaxValue.");
                return int.MaxValue;
            }

            try
            {
                int level = _playerData.StorageLevels[type];
                return level;

            }
            catch (Exception ex)
            {
                throw new Exception($"[GameDataService] Failed to get storage level for resource type {type}: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets the storage capacity level for the specified trade good resource type. Performs no action if the type is not a trade good.
        /// </summary>
        /// <param name="type">Trade good type for which the level needs to be set.</param>
        /// <param name="level">New storage level.</param>
        public void SetStorageLevel(ResourceType type, int level)
        {
            if (!type.IsTradeGood())
            {
                Debug.LogWarning($"[GameDataService] Attempted to set storage level for non-trade good type {type}.");
                return;
            }

            try
            {
                _playerData.StorageLevels[type] = level;
                _isDirty = true;

            }
            catch (Exception ex)
            {
                throw new Exception($"[GameDataService] Failed to set storage level for resource type {type}: {ex.Message}");
            }
        }

        /// <summary>
        /// Returns the scheduled date and time for the next trade refresh.
        /// </summary>
        public DateTime GetNextTradeRefreshTime()
        {
            return _playerData.NextTradeRefreshTime;
        }

        /// <summary>
        /// Sets the next scheduled time when trades will be refreshed.
        /// </summary>
        public void SetNextTradeRefreshTime(DateTime time)
        {
            _playerData.NextTradeRefreshTime = time;
            _isDirty = true;
        }

        /// <summary>
        /// Retrieves a list of all active trade offers for the current player.
        /// </summary>
        /// <returns>A list of <see cref="TradeOfferData"/> objects representing the player's active trade offers.
        public IReadOnlyList<TradeOfferData> GetActiveOffers()
        {
            return _playerData.ActiveTradeOffers.AsReadOnly();
        }

        /// <summary>
        /// Saves the specified list of active trade offers
        /// </summary>
        public void SaveActiveOffers(IReadOnlyList<TradeOfferData> offers)
        {
            _playerData.ActiveTradeOffers = new List<TradeOfferData>(offers);
            _isDirty = true;
        }

        /// <summary>
        /// Determines if its first session for the player.
        /// </summary>
        public bool IsFirstSession() => _playerData.IsFirstSession;

        #endregion

        #region Helpers

        // The only method that changes the value of the player's resources
        private void ApplyResourceChange(ResourceType type, int oldValue, int newValue)
        {
            if (oldValue == newValue) return;

            try
            {
                _playerData.Resources[type] = newValue;
            }
            catch (Exception ex)
            {
                throw new Exception($"[GameDataService] Failed to apply resource change for type {type}: {ex.Message}");
            }

            var changeData = new ResourceChangeData(type, oldValue, newValue);
            OnResourceChanged?.Invoke(changeData);

            _isDirty = true;
        }

        // Private method that determines whether a single transaction operation is possible for a given type and amount. Delegates logic to handlers or applies default
        private bool CanApplyTransactionOperation(ResourceType type, int currentAmount, int delta)
        {
            // Use handler logic if exists
            if (_logicHandlers.TryGetValue(type, out var handler))
                return handler.CanApplyTransactionOperation(type, currentAmount, delta);

            // Default logic
            return delta < 0 ? currentAmount >= Mathf.Abs(delta) : true;
        }

        // Private method that calculates single transaction operation for a given type and amount. Delegates logic to handlers or applies default
        private int CalculateTransactionOperation(ResourceType type, int currentAmount, int delta)
        {
            // Use handler logic if exists
            if (_logicHandlers.TryGetValue(type, out var handler))
                return handler.CalculateTransactionOperation(type, currentAmount, delta);

            // Default logic
            return Mathf.Max(currentAmount + delta, 0);
        }

        #endregion
    }
}
