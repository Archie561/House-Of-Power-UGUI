using System.Collections.Generic;
using UnityEngine;

namespace Game.General
{
    /// <summary>
    /// A centralized library that holds references to all CountryDefinitions.
    /// Provides efficient lookup by CountryId using a Dictionary cache.
    /// </summary>
    [CreateAssetMenu(fileName = "CountryLibrary", menuName = "Game/Trade/Country Library")]
    public class CountryLibrary : ScriptableObject
    {
        [Tooltip("List of all countries available in the game.")]
        [SerializeField] private List<CountryDefinition> _definitions;

        private Dictionary<CountryId, CountryDefinition> _lookup;

        /// <summary>
        /// Retrieves the country definition for the specified ID.
        /// </summary>
        /// <param name="id">The unique ID of the country.</param>
        /// <returns>The definition asset, or null if not found.</returns>
        public CountryDefinition GetDef(CountryId id)
        {
            // Lazy initialization of the dictionary
            if (_lookup == null || _lookup.Count == 0)
            {
                UpdateLookup();
            }

            if (_lookup.TryGetValue(id, out var def))
            {
                return def;
            }

            Debug.LogError($"[CountryLibrary] Definition not found for id: {id}");
            return null;
        }

        private void UpdateLookup()
        {
            _lookup = new Dictionary<CountryId, CountryDefinition>();

            if (_definitions == null) return;

            foreach (var def in _definitions)
            {
                if (def == null) continue;

                if (_lookup.ContainsKey(def.Id))
                {
                    Debug.LogWarning($"[CountryLibrary] Duplicate country ID found: {def.Id}. Keeping the first one.");
                    continue;
                }

                _lookup.Add(def.Id, def);
            }
        }
    }
}