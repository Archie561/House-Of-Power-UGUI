using System;

namespace Game.General
{
    /// <summary>
    /// Identifies the type of resource used in the game.
    /// IDs 0-9 are reserved for currencies, 10-19 for trade goods, 20-29 for policy values.
    /// </summary>
    [Serializable]
    public enum ResourceType
    {
        Money = 0,
        Gems = 1,

        Stone = 10,
        Minerals = 11,
        Iron = 12,
        Chemicals = 13,
        Oil = 14,
        Wood = 15,

        Healthcare = 20,
        Infrastructure = 21,
        Science = 22,
        Ecology = 23,
        Education = 24,
        Welfare = 25
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

        /// <summary>
        /// Checks if the specified resource type represents a trade good.
        /// </summary>
        public static bool IsTradeGood(this ResourceType type)
        {
            return (int)type >= 10 && (int)type < 20;
        }

        /// <summary>
        /// Checks if the specified resource type represents a policy value.
        /// </summary>
        public static bool IsPolicyValue(this ResourceType type)
        {
            return (int)type >= 20 && (int)type < 30;
        }
    }

    /// <summary>
    /// Specifies the category of a law blank. Defines its sprite and author.
    /// </summary>
    [Serializable]
    public enum DocumentType
    {
        Healthcare,
        Infrastructure,
        Science,
        Ecology,
        Education,
        Welfare,
        Agriculture,
        Army,
        Economy,
        Court,
        Diplomacy,
        GlobalProjects
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