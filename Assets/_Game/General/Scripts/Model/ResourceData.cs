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
}