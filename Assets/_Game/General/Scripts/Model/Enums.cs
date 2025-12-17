using System;

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
public static class ResourceTypeExtensions
{
    public static bool IsCurrency(this ResourceType type)
    {
        return (int)type < 10;
    }
}

[Serializable]
public enum CountryId
{
    Ukraine,
    China,
    UnitedKingdom,
    Germany,
    France
}

public enum TabType
{
    Laws,
    Map,
    Economy,
    Shop,
    Trades
}