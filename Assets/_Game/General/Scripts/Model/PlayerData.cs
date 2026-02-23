using Game.Features.Trade;
using System;
using System.Collections.Generic;

namespace Game.General
{
    /// <summary>
    /// The main data container for the player's progress.
    /// Used for saving and loading game state.
    /// </summary>
    [Serializable]
    public class PlayerData
    {
        // General
        public Dictionary<ResourceType, int> Resources;
        public bool IsFirstSession;

        // Trade Module
        public Dictionary<ResourceType, int> StorageLevels;
        public List<TradeOfferData> ActiveTradeOffers;
        public DateTime NextTradeRefreshTime;

        // Law Module
        public HashSet<string> UsedLawIds;
        public string ActiveLawId;
        public DateTime NextLawsRefreshTime;

        /// <summary>
        /// Creates a new instance of PlayerData.
        /// </summary>
        public PlayerData(
            Dictionary<ResourceType, int> resources,
            bool isFirstSession,
            Dictionary<ResourceType, int> storageLevels,
            List<TradeOfferData> activeTradeOffers,
            DateTime nextTradeRefreshTime,
            string activeLawId,
            DateTime nextLawsRefreshTime)
        {
            Resources = resources;
            IsFirstSession = isFirstSession;

            StorageLevels = storageLevels;
            ActiveTradeOffers = activeTradeOffers;
            NextTradeRefreshTime = nextTradeRefreshTime;

            UsedLawIds = new HashSet<string>();
            ActiveLawId = activeLawId;
            NextLawsRefreshTime = nextLawsRefreshTime;
        }
    }
}