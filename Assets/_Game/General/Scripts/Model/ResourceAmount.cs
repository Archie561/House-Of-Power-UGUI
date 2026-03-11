using Newtonsoft.Json;
using System;
using UnityEngine;

namespace Game.General
{
    /// <summary>
    /// Immutable representation of a resource quantity.
    /// Used for Logic, UI arguments, Trade Offers, etc.
    /// </summary>
    [System.Serializable]
    public struct ResourceAmount
    {
        [SerializeField] private ResourceType _type;
        [SerializeField] private int _amount;

        public ResourceType Type => _type;
        public int Amount => _amount;

        [JsonConstructor]
        public ResourceAmount(ResourceType type, int amount)
        {
            _type = type;
            _amount = amount;
        }
    }
}