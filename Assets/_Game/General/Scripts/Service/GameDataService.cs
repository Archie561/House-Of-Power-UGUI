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
    /// and core economy logic (Transactions, Resource adjustments).
    /// Acts as a Single Source of Truth for the game state.
    /// </summary>
    public class GameDataService : MonoBehaviour
    {
        public static GameDataService Instance { get; private set; }

        #region Configuration & State

        [Header("Configuration")]
        [SerializeField] private GameConfig _gameConfig;

        private const string SAVE_FILE_NAME = "playerdata.json";

        private PlayerData _playerData;
        private string _saveFilePath;

        // Lookup cache for O(1) resource access
        private Dictionary<ResourceType, ResourceData> _resourceLookup;

        #endregion

        #region Events

        public event Action<ResourceType> OnResourceChanged;

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

        // Critical for mobile: Save when user minimizes the app
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SavePlayerData();
            }
        }

        private void OnApplicationQuit()
        {
            SavePlayerData();
        }

        #endregion

        #region Save / Load System

        private void LoadPlayerData()
        {
            if (!File.Exists(_saveFilePath))
            {
                _playerData = GenerateNewPlayerData();
                InitializeLookup(); // Build cache
                SavePlayerData();
            }
            else
            {
                try
                {
                    string json = File.ReadAllText(_saveFilePath);
                    _playerData = JsonConvert.DeserializeObject<PlayerData>(json);

                    // Validate data integrity (in case save file is old version)
                    if (_playerData == null) throw new Exception("Deserialized data is null");

                    InitializeLookup(); // Build cache
                }
                catch (Exception e)
                {
                    Debug.LogError($"[GameDataService] Failed to load data: {e.Message}. Creating new.");
                    _playerData = GenerateNewPlayerData();
                    InitializeLookup();
                    SavePlayerData(); // Overwrite corrupted file
                }
            }
        }

        public void SavePlayerData()
        {
            if (_playerData == null) return;

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
            List<ResourceData> resources = new List<ResourceData>();

            if (_gameConfig.InitialResources != null)
            {
                foreach (var initData in _gameConfig.InitialResources)
                {
                    resources.Add(new ResourceData(initData.Type, initData.StartAmount, initData.StartCapacity));
                }
            }

            // Ensure all enums exist (safety check)
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                if (!resources.Exists(r => r.Type == type))
                {
                    int cap = type.IsCurrency() ? int.MaxValue : 100;
                    resources.Add(new ResourceData(type, 0, cap));
                }
            }

            List<string> unlockedStates = new List<string>();
            List<string> purchasedCities = new List<string>();

            DateTime nextRefresh = DateTime.Now;

            return new PlayerData(
                resources,
                unlockedStates,
                purchasedCities,
                isFirstSession: true,
                nextRefresh,
                activeTradeOffers: null
            );
        }

        private void InitializeLookup()
        {
            _resourceLookup = new Dictionary<ResourceType, ResourceData>();
            if (_playerData?.Resources == null) return;

            foreach (var res in _playerData.Resources)
            {
                if (!_resourceLookup.ContainsKey(res.Type))
                {
                    _resourceLookup.Add(res.Type, res);
                }
            }
        }

        #endregion

        #region Economy Public API

        public int GetAmount(ResourceType type)
        {
            return TryGetResourceData(type, out var data) ? data.Amount : 0;
        }

        public int GetMaxCapacity(ResourceType type)
        {
            return TryGetResourceData(type, out var data) ? data.MaxCapacity : 0;
        }

        public bool CanAfford(ResourceType type, int amount)
        {
            if (TryGetResourceData(type, out var data))
            {
                return data.Amount >= amount;
            }
            return false;
        }

        public bool CanAfford(IReadOnlyList<ResourceData> cost)
        {
            if (cost == null || cost.Count == 0) return true;

            foreach (var item in cost)
            {
                if (!CanAfford(item.Type, item.Amount)) return false;
            }
            return true;
        }

        /// <summary>
        /// Attempts to spend a single resource. Returns true if successful.
        /// </summary>
        public bool TrySpend(ResourceType type, int amount)
        {
            if (!TryGetResourceData(type, out var data)) return false;
            if (data.Amount < amount) return false;

            data.Amount -= amount;
            OnResourceChanged?.Invoke(type);

            // Optional: Auto-save on critical currency spending? 
            // Better to rely on OnPause for performance.

            return true;
        }

        /// <summary>
        /// Transactional spend: Either ALL costs are paid, or NONE.
        /// Prevents partial state updates if player can afford item A but not item B.
        /// </summary>
        public bool TrySpend(IReadOnlyList<ResourceData> cost)
        {
            if (!CanAfford(cost)) return false;

            foreach (var item in cost)
            {
                // We utilize the private method or direct access since we already checked affordability
                if (TryGetResourceData(item.Type, out var data))
                {
                    data.Amount -= item.Amount;
                    OnResourceChanged?.Invoke(item.Type);
                }
            }

            return true;
        }

        public void AddResource(ResourceType type, int amount)
        {
            if (amount <= 0) return;

            if (TryGetResourceData(type, out var data))
            {
                // Cap at MaxCapacity
                data.Amount = Math.Min(data.Amount + amount, data.MaxCapacity);
                OnResourceChanged?.Invoke(type);
            }
        }

        public void AddResources(IReadOnlyList<ResourceData> income)
        {
            if (income == null) return;

            foreach (var item in income)
            {
                AddResource(item.Type, item.Amount);
            }
        }

        public void UpgradeCapacity(ResourceType type, int additionalCapacity)
        {
            if (type.IsCurrency()) return; // Currencies usually don't have caps

            if (TryGetResourceData(type, out var data))
            {
                data.MaxCapacity += additionalCapacity;
                OnResourceChanged?.Invoke(type);

                // Immediately save after an upgrade is a good practice
                SavePlayerData();
            }
        }

        public DateTime GetNextTradeRefreshTime()
        {
            return _playerData.NextTradeRefreshTime;
        }

        public void SetNextTradeRefreshTime(DateTime time)
        {
            _playerData.NextTradeRefreshTime = time;
            SavePlayerData();
        }

        public List<TradeOfferData> GetActiveOffers()
        {
            return _playerData.ActiveTradeOffers;
        }

        public void SaveActiveOffers(List<TradeOfferData> offers)
        {
            _playerData.ActiveTradeOffers = new List<TradeOfferData>(offers);
            SavePlayerData();
        }

        #endregion

        #region Helpers

        private bool TryGetResourceData(ResourceType type, out ResourceData data)
        {
            if (_resourceLookup != null && _resourceLookup.TryGetValue(type, out data))
            {
                return true;
            }

            Debug.LogError($"[GameDataService] Resource {type} not found in lookup!");
            data = null;
            return false;
        }

        #endregion
    }
}
