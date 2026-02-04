using Newtonsoft.Json;

namespace Game.General
{
    /// <summary>
    /// Immutable representation of a resource quantity.
    /// Used for Logic, UI arguments, Trade Offers, etc.
    /// </summary>
    [System.Serializable]
    public readonly struct ResourceAmount
    {
        public ResourceType Type { get; }
        public int Amount { get; }

        [JsonConstructor]
        public ResourceAmount(ResourceType type, int amount)
        {
            Type = type;
            Amount = amount;
        }
    }
}