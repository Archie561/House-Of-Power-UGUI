using Game.Features.Trade;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.General
{
    /// <summary>
    /// Represents the main configuration asset for initializing game state, including starting resources, storage
    /// levels, available trade offers, refresh timing, and session flag, etc.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Game/General/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Initial Resources")]
        [SerializeField] private List<ResourceConfigEntry> _initialResources;

        [Header("Is First Game Session")]
        [SerializeField] private bool _isFirstGameSession = true;

        [Header("Initial Storages")]
        [SerializeField] private List<ResourceConfigEntry> _initialStorageLevels;

        [Header("Initial Offers")]
        [SerializeField] private List<TradeOfferConfigEntry> _initialOffers;

        [Header("Next Trade Refresh Time (in seconds)")]
        [SerializeField] private float _initialTradeRefreshTime = 600f;

        [Header("Initial Law Id")]
        [SerializeField] private string _initialLawId = "law_0001";

        [Header("Initial Laws Count")]
        [SerializeField] private int _initialLawsCount = 8;

        [Header("Next Laws Refresh Time")]
        [SerializeField] private float _initialLawsRefreshTime = 600f;

        // --- Runtime Accessors (Convertors) ---
        public Dictionary<ResourceType, int> GetInitialResources()
        {
            var dict = new Dictionary<ResourceType, int>();
            if (_initialResources == null) return dict;

            foreach (var entry in _initialResources)
            {
                if (!dict.ContainsKey(entry.Type))
                {
                    dict.Add(entry.Type, entry.Amount);
                }
            }
            return dict;
        }

        public bool IsFirstGameSession()
        {
            return _isFirstGameSession;
        }

        public Dictionary<ResourceType, int> GetInitialStorageLevels()
        {
            var dict = new Dictionary<ResourceType, int>();
            if (_initialStorageLevels == null) return dict;

            foreach (var entry in _initialStorageLevels)
            {
                if (!dict.ContainsKey(entry.Type))
                {
                    dict.Add(entry.Type, entry.Amount); // Here Amount acts as Level
                }
            }
            return dict;
        }

        public List<TradeOfferData> GetInitialOffers()
        {
            if (_initialOffers == null) return new List<TradeOfferData>();
            return _initialOffers.Select(x => x.ToRuntime()).ToList();
        }

        public float GetTradeInitialRefreshTime()
        {
            return _initialTradeRefreshTime;
        }

        public string GetInitialLawId()
        {
            return _initialLawId;
        }

        public int GetInitialLawsCount()
        {
            return _initialLawsCount;
        }

        public float GetLawsInitialRefreshTime()
        {
            return _initialLawsRefreshTime;
        }
    }

    [Serializable]
    public struct ResourceConfigEntry
    {
        public ResourceType Type;
        public int Amount;

        public ResourceAmount ToRuntime() => new ResourceAmount(Type, Amount);
    }

    [Serializable]
    public class TradeOfferConfigEntry
    {
        public CountryId Country;
        public List<ResourceConfigEntry> Import;
        public List<ResourceConfigEntry> Export;

        public TradeOfferData ToRuntime()
        {
            var importList = Import.Select(x => x.ToRuntime()).ToList();
            var exportList = Export.Select(x => x.ToRuntime()).ToList();

            return new TradeOfferData(Country, importList, exportList);
        }
    }
}
