using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class GameDataService : MonoBehaviour
{
    public static GameDataService Instance { get; private set; }
    
    private PlayerData _playerData;
    private string _saveFilePath;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _saveFilePath = Path.Combine(Application.persistentDataPath, "playerdata.json");
        LoadPlayerData();
    }

    private void LoadPlayerData()
    {
        if (!File.Exists(_saveFilePath))
        {
            _playerData = GenerateNewPlayerData();
            SavePlayerData();
        }
        else
        {
            try
            {
                string json = File.ReadAllText(_saveFilePath);
                _playerData = JsonConvert.DeserializeObject<PlayerData>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load player data: {e.Message}");
                _playerData = GenerateNewPlayerData();
            }
        }     
    }

    private PlayerData GenerateNewPlayerData()
    {
        List<string> unlockedStates = new List<string> { "state_1", "state_2", "state_3" };
        List<string> purchasedCities = new List<string> { "city_1", "city_2", "city_3" };

        List<ResourceData> resources = new List<ResourceData>();
        foreach (ResourceType resource in Enum.GetValues(typeof(ResourceType)))
        {
            resources.Add(new ResourceData(resource, 50, resource.IsCurrency() ? int.MaxValue : 100));
        }

        return new PlayerData
        (
            resources,
            unlockedStates,
            purchasedCities,
            isFirstSession: true
        );
    }

    public void SavePlayerData()
    {
        try
        {
            string json = JsonConvert.SerializeObject(_playerData, Formatting.Indented);
            File.WriteAllText(_saveFilePath, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save player data: {e.Message}");
        }
    }

    //ECONOMY SECTION//
    private Dictionary<ResourceType, ResourceData> _resourceLookup;

    public event Action<ResourceType> OnResourceChanged;

    private ResourceData GetResourceData(ResourceType type)
    {
        if (_resourceLookup == null || _resourceLookup.Count == 0)
        {
            _resourceLookup = new Dictionary<ResourceType, ResourceData>();
            foreach (var res in _playerData.Resources)
                _resourceLookup[res.Type] = res;
        }

        if (_resourceLookup.TryGetValue(type, out var resource))
        {
            return resource;
        }
        else
        {
            Debug.LogError($"Resource type {type} not found in player data.");
            return null;
        }
    }

    public int GetAmount(ResourceType type)
    {
        return GetResourceData(type).Amount;
    }

    public int GetMaxCapacity(ResourceType type)
    {
        return GetResourceData(type).MaxCapacity;
    }

    public bool CanAfford(ResourceType type, int amount)
    {
        return GetAmount(type) >= amount;
    }

    public bool TrySpend(List<ResourceData> cost)
    {
        foreach (var item in cost)
            if (!CanAfford(item.Type, item.Amount)) return false;

        foreach (var item in cost)
        {
            GetResourceData(item.Type).Amount -= item.Amount;
            OnResourceChanged?.Invoke(item.Type);
        }

        return true;
    }

    public bool TrySpend(ResourceType type, int amount)
    {
        if (!CanAfford(type, amount)) return false;

        var resource = GetResourceData(type);
        resource.Amount -= amount;
        OnResourceChanged?.Invoke(type);

        return true;
    }

    public void AddResources(List<ResourceData> income)
    {
        foreach (var item in income)
        {
            var resource = GetResourceData(item.Type);
            resource.Amount = Math.Min(resource.Amount + item.Amount, resource.MaxCapacity);
            OnResourceChanged?.Invoke(item.Type);
        }
    }

    public void AddResources(ResourceType type, int amount)
    {
        var resource = GetResourceData(type);
        resource.Amount = Math.Min(resource.Amount + amount, resource.MaxCapacity);
        OnResourceChanged?.Invoke(type);
    }

    public void UpgradeCapacity(ResourceType type, int additionalCapacity)
    {
        if (type.IsCurrency()) return;

        var resource = GetResourceData(type);
        resource.MaxCapacity += additionalCapacity;
        OnResourceChanged?.Invoke(type);
    }
}
