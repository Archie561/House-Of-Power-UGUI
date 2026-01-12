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
        public List<ResourceData> Resources;
        public List<string> UnlockedStateIds;
        public List<string> PurchasedCityIds;
        public bool IsFirstSession;

        public DateTime NextTradeRefreshTime;
        public List<TradeOfferData> ActiveTradeOffers;

        /// <summary>
        /// Creates a new instance of PlayerData.
        /// Automatically initializes lists to empty if null is passed.
        /// </summary>
        public PlayerData(
            List<ResourceData> resources,
            List<string> unlockedStateIds,
            List<string> purchasedCityIds,
            bool isFirstSession,
            DateTime nextTradeRefreshTime,
            List<TradeOfferData> activeTradeOffers)
        {
            // Ensure lists are never null to prevent NullReferenceException logic errors later
            Resources = resources ?? new List<ResourceData>();
            UnlockedStateIds = unlockedStateIds;
            PurchasedCityIds = purchasedCityIds;

            IsFirstSession = isFirstSession;
            NextTradeRefreshTime = nextTradeRefreshTime;
            ActiveTradeOffers = activeTradeOffers;
        }
    }
}