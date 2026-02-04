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
        public Dictionary<ResourceType, int> Resources;
        public Dictionary<ResourceType, int> StorageLevels;
        public bool IsFirstSession;

        public DateTime NextTradeRefreshTime;
        public List<TradeOfferData> ActiveTradeOffers;

        /// <summary>
        /// Creates a new instance of PlayerData.
        /// Automatically initializes lists to empty if null is passed.
        /// </summary>
        public PlayerData(
            Dictionary<ResourceType, int> resources,
            Dictionary<ResourceType, int> storageLevels,
            bool isFirstSession,
            DateTime nextTradeRefreshTime,
            List<TradeOfferData> activeTradeOffers)
        {
            Resources = resources;
            StorageLevels = storageLevels;

            IsFirstSession = isFirstSession;
            NextTradeRefreshTime = nextTradeRefreshTime;
            ActiveTradeOffers = activeTradeOffers;
        }
    }
}