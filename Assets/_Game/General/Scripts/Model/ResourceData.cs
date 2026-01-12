using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Game.General
{
    /// <summary>
    /// A simple data container representing a quantity of a specific resource type.
    /// Used for inventory, trade costs, and rewards.
    /// </summary>
    [System.Serializable]
    public class ResourceData
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ResourceType Type; // Public field needed for Json serialization usually, or use [JsonProperty] on getter

        public int Amount { get; set; }
        public int MaxCapacity { get; set; }

        public ResourceData(ResourceType type, int amount, int maxCapacity = int.MaxValue)
        {
            Type = type;
            Amount = amount;
            MaxCapacity = maxCapacity;
        }
    }
}