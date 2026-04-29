using Game.Features.Trade;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.General
{
    /// <summary>
    /// A service that stores and provides access to player data. Methods for modifying data are protected through explicit implementation of the interface.
    /// It includes an event that notifies of changes to player data.
    /// </summary>
    [DefaultExecutionOrder(-100)] // Ensure this initializes early
    public class PlayerDataService : MonoBehaviour, IResourceDataWriter, ITradeDataWriter, ILawDataWriter
    {
        public static PlayerDataService Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private PlayerConfig _playerConfig;

        private PlayerData _playerData;
        private SaveLoadSystem _saveLoadSystem;
        private bool _isDirty;

        public event Action<ResourceChangeData> OnResourceChanged;

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

            _saveLoadSystem = new SaveLoadSystem(_playerConfig);
            _playerData = _saveLoadSystem.LoadPlayerData();
        }

        private void LateUpdate()
        {
            if (_isDirty)
            {
                _saveLoadSystem.SavePlayerData(_playerData);
                _isDirty = false;
            }
        }

        // Critical for mobile: Save when user minimizes the app
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && _isDirty)
            {
                _saveLoadSystem.SavePlayerData(_playerData);
                _isDirty = false;
            }
        }

        private void OnApplicationQuit()
        {
            if (_isDirty)
            {
                _saveLoadSystem.SavePlayerData(_playerData);
                _isDirty = false;
            }
        }

        #endregion

        #region General Player Data

        /// <summary>
        /// Returns the current amount of the specified resource type.
        /// </summary>
        public int GetResourceAmount(ResourceType type)
        {
            try
            {
                int amount = _playerData.Resources[type];
                return amount;
            }
            catch (Exception ex)
            {
                throw new Exception($"[PlayerDataService] Failed to get amount for resource type {type}: {ex.Message}");
            }
        }

        // The only method that changes the value of the player's resources
        void IResourceDataWriter.ApplyResourceChange(ResourceType type, int oldValue, int newValue)
        {
            if (oldValue == newValue) return;

            try
            {
                _playerData.Resources[type] = newValue;
            }
            catch (Exception ex)
            {
                throw new Exception($"[PlayerDataService] Failed to apply resource change for type {type}: {ex.Message}");
            }

            // Notify about the change
            var changeData = new ResourceChangeData(type, oldValue, newValue);
            OnResourceChanged?.Invoke(changeData);

            _isDirty = true;
        }

        /// <summary>
        /// Determines if its first session for the player.
        /// </summary>
        public bool IsFirstSession() => _playerData.IsFirstSession;

        // Method to set first session flag.
        private void SetFirstSession(bool isFirst)
        {
            _playerData.IsFirstSession = isFirst;
            _isDirty = true;
        }

        #endregion

        #region Trade Player Data

        /// <summary>
        /// Returns the storage capacity level for the specified trade good resource type. Returns int.MaxValue for non-trade goods.
        /// </summary>
        public int GetStorageLevel(ResourceType type)
        {
            if (!type.IsTradeGood())
            {
                Debug.LogWarning($"[PlayerDataService] Requested storage level for non-trade good type {type}. Returning int.MaxValue.");
                return int.MaxValue;
            }

            try
            {
                int level = _playerData.StorageLevels[type];
                return level;

            }
            catch (Exception ex)
            {
                throw new Exception($"[PlayerDataService] Failed to get storage level for resource type {type}: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets the storage capacity level for the specified trade good resource type. Performs no action if the type is not a trade good.
        /// </summary>
        /// <param name="type">Trade good type for which the level needs to be set.</param>
        /// <param name="level">New storage level.</param>
        void ITradeDataWriter.SetStorageLevel(ResourceType type, int level)
        {
            if (!type.IsTradeGood())
            {
                Debug.LogWarning($"[PlayerDataService] Attempted to set storage level for non-trade good type {type}.");
                return;
            }

            try
            {
                _playerData.StorageLevels[type] = level;
                _isDirty = true;

            }
            catch (Exception ex)
            {
                throw new Exception($"[PlayerDataService] Failed to set storage level for resource type {type}: {ex.Message}");
            }
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
        /// Sets the specified list of active trade offers
        /// </summary>
        void ITradeDataWriter.SetActiveOffers(List<TradeOfferData> offers)
        {
            _playerData.ActiveTradeOffers = new List<TradeOfferData>(offers);
            _isDirty = true;
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
        void ITradeDataWriter.SetNextTradeRefreshTime(DateTime time)
        {
            _playerData.NextTradeRefreshTime = time;
            _isDirty = true;
        }

        #endregion

        #region Laws Player Data

        /// <summary>
        /// Gets the identifier of the currently active law for the player.
        /// </summary>
        public string GetActiveLawId()
        {
            return _playerData.ActiveLawId;
        }

        /// <summary>
        /// Sets the active law identifier for the player data.
        /// </summary>
        void ILawDataWriter.SetActiveLawId(string id)
        {
            _playerData.ActiveLawId = id;
            _isDirty = true;
        }

        /// <summary>
        /// Gets the number of laws available for the player to execute.
        /// </summary>
        public int GetAvailableLawsCount()
        {
            return _playerData.LawsLeftToExecute;
        }

        /// <summary>
        /// Sets the number of laws available for the player to execute.
        /// </summary>
        void ILawDataWriter.SetAvailableLawsCount(int newCount)
        {
            _playerData.LawsLeftToExecute = newCount >= 0 ? newCount : 0;
            _isDirty = true;
        }

        /// <summary>
        /// Gets the collection of identifiers for the laws that was already used by the player.
        /// </summary>
        public IReadOnlyCollection<string> GetUsedLawIds()
        {
            return _playerData.UsedLawIds;
        }

        /// <summary>
        /// Records the specified law identifier as used if it has not already been saved.
        /// </summary>
        void ILawDataWriter.MarkLawAsUsed(string id)
        {
            if (!_playerData.UsedLawIds.Contains(id))
            {
                _playerData.UsedLawIds.Add(id);
                _isDirty = true;
            }
        }

        /// <summary>
        /// Clears all used law identifiers from the player's data. Use only if player has completed all avaliable laws.
        /// </summary>
        void ILawDataWriter.ResetUsedLaws()
        {
            _playerData.UsedLawIds.Clear();
            _isDirty = true;
        }

        /// <summary>
        /// Gets the scheduled date and time for the next refresh of the laws data.
        /// </summary>
        public DateTime GetNextLawsRefreshTime()
        {
            return _playerData.NextLawsRefreshTime;
        }

        /// <summary>
        /// Sets the scheduled time for the next refresh of laws in the player's data.
        /// </summary>
        void ILawDataWriter.SetNextLawRefreshTime(DateTime time)
        {
            _playerData.NextLawsRefreshTime = time;
            _isDirty = true;
        }

        #endregion
    }
}