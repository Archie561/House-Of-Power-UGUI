using System.Collections.Generic;
using UnityEngine;

namespace Game.General
{
    /// <summary>
    /// A centralized library that holds references to all ResourceDefinitions.
    /// Provides efficient lookup by ResourceType using a Dictionary cache.
    /// </summary>
    [CreateAssetMenu(fileName = "ResourceLibrary", menuName = "Game/Resources/Library")]
    public class ResourceLibrary : ScriptableObject
    {
        [Tooltip("List of all resources available in the game.")]
        [SerializeField] private List<ResourceDefinition> _definitions;

        private Dictionary<ResourceType, ResourceDefinition> _lookup;

        /// <summary>
        /// Retrieves the resource definition for the specified type.
        /// </summary>
        /// <param name="type">The type of the resource.</param>
        /// <returns>The definition asset, or null if not found.</returns>
        public ResourceDefinition GetDef(ResourceType type)
        {
            // Lazy initialization
            if (_lookup == null || _lookup.Count == 0)
            {
                UpdateLookup();
            }

            if (_lookup.TryGetValue(type, out var def))
            {
                return def;
            }

            Debug.LogError($"[ResourceLibrary] Definition not found for type: {type}");
            return null;
        }

        private void UpdateLookup()
        {
            _lookup = new Dictionary<ResourceType, ResourceDefinition>();

            if (_definitions == null) return;

            foreach (var def in _definitions)
            {
                if (def == null) continue;

                if (_lookup.ContainsKey(def.Type))
                {
                    Debug.LogWarning($"[ResourceLibrary] Duplicate resource type found: {def.Type}. Keeping the first one.");
                    continue;
                }

                _lookup.Add(def.Type, def);
            }
        }
    }
}