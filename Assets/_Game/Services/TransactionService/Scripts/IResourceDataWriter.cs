using UnityEngine;

namespace Game.General
{
    /// <summary>
    /// Interface for writing changes to resource data.
    /// Implemented by PlayerDataService to prevent direct access to data modification methods (Interface segregation principle)
    /// </summary>
    public interface IResourceDataWriter
    {
        void ApplyResourceChange(ResourceType type, int oldValue, int newValue);
        int GetResourceAmount(ResourceType type);
    }
}
