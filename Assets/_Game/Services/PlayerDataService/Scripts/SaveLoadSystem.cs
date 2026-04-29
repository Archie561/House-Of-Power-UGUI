using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace Game.General
{
    /// <summary>
    /// Handles saving and loading of player data to and from persistent storage.
    /// </summary>
    public class SaveLoadSystem
    {
        private PlayerConfig _playerConfig;
        private string _saveFilePath;

        public SaveLoadSystem(PlayerConfig config)
        {
            _playerConfig = config;
            _saveFilePath = Path.Combine(Application.persistentDataPath, "playerdata.json");
        }

        /// <summary>
        /// Loads player data from persistent storage. If no save file exists or if loading fails, generates new player data with default values.
        /// </summary>
        public PlayerData LoadPlayerData()
        {
            // If no save file exists, return new player data with default values
            if (!File.Exists(_saveFilePath)) return GenerateNewPlayerData();

            try
            {
                string json = File.ReadAllText(_saveFilePath);
                var data = JsonConvert.DeserializeObject<PlayerData>(json);

                // Validate data integrity (in case save file is old version)
                if (data == null) throw new Exception("Deserialized data is null");
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveLoadSystem] Failed to load data: {e.Message}. Creating new.");
                return GenerateNewPlayerData();
            }
        }

        private PlayerData GenerateNewPlayerData()
        {
            // Initial Game State Configuration
            Dictionary<ResourceType, int> resources = _playerConfig.GetInitialResources();
            Dictionary<ResourceType, int> storageLevels = _playerConfig.GetInitialStorageLevels();

            DateTime nextTradeRefresh = DateTime.UtcNow.AddSeconds(_playerConfig.GetTradeInitialRefreshTime());
            DateTime nextLawRefresh = DateTime.UtcNow.AddSeconds(_playerConfig.GetLawsInitialRefreshTime());

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
                _playerConfig.IsFirstGameSession(),
                storageLevels,
                _playerConfig.GetInitialOffers(),
                nextTradeRefresh,
                _playerConfig.GetInitialLawId(),
                _playerConfig.GetInitialLawsCount(),
                nextLawRefresh);
        }

        /// <summary>
        /// Saves the provided player data to persistent storage in JSON format. If saving fails, logs an error message.
        /// </summary>
        public void SavePlayerData(PlayerData data)
        {
            try
            {
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(_saveFilePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveLoadSystem] Failed to save data: {e.Message}");
            }
        }
    }
}