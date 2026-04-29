using Game.Features.Trade;
using System;
using System.Collections.Generic;

namespace Game.General
{
    /// <summary>
    /// Interface for writing changes to trade-related data.
    /// Implemented by GameDataService to prevent direct access to data modification methods (Interface segregation principle)
    /// </summary>
    public interface ITradeDataWriter
    {
        void SetStorageLevel(ResourceType type, int level);
        void SetActiveOffers(List<TradeOfferData> offers);
        void SetNextTradeRefreshTime(DateTime time);
    }
}