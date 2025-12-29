using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

[Serializable]
public class ResourceData
{
    [JsonConverter(typeof(StringEnumConverter))]
    public ResourceType Type;
    public int Amount;
    public int MaxCapacity;

    public ResourceData(ResourceType type, int amount, int maxCapacity = int.MaxValue)
    {
        Type = type;
        Amount = amount;
        MaxCapacity = maxCapacity;
    }
}