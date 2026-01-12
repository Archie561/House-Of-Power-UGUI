using System;

namespace Game.General
{
    /// <summary>
    /// Identifies the type of resource used in the game.
    /// IDs 0-9 are reserved for currencies, 10+ for trade goods.
    /// </summary>
    [Serializable]
    public enum ResourceType
    {
        Money = 0,
        Gems = 1,
        Reputation = 2,

        Stone = 10,
        Minerals = 11,
        Iron = 12,
        Chemicals = 13,
        Oil = 14,
        Wood = 15
    }

    /// <summary>
    /// Extension methods for ResourceType logic.
    /// </summary>
    public static class ResourceTypeExtensions
    {
        /// <summary>
        /// Checks if the resource is considered a currency (Money, Gems, Reputation).
        /// </summary>
        public static bool IsCurrency(this ResourceType type)
        {
            // Currencies are defined as IDs less than 10 based on current enum structure
            return (int)type < 10;
        }
    }

    /// <summary>
    /// Unique identifiers for trade partner countries.
    /// </summary>
    [Serializable]
    public enum CountryId
    {
        Ukraine,
        China,
        UnitedKingdom,
        Germany,
        France
    }

    /// <summary>
    /// Identifies the main navigation tabs in the bottom bar.
    /// </summary>
    public enum TabType
    {
        Laws,
        Map,
        Economy,
        Shop,
        Trades
    }
}