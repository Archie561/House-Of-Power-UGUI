using System.Collections.Generic;
using UnityEngine;

namespace Game.General
{
    /// <summary>
    /// A centralized library that holds references to all DocumentDefinitions.
    /// Provides efficient lookup by DocumentType using a Dictionary cache.
    /// </summary>
    [CreateAssetMenu(fileName = "DocumentLibrary", menuName = "Game/Law/DocumentLibrary")]
    public class DocumentLibrary : ScriptableObject
    {
        [Tooltip("List of all documents available in the game.")]
        [SerializeField] private List<DocumentDefinition> _definitions;

        private Dictionary<DocumentType, DocumentDefinition> _lookup;

        /// <summary>
        /// Retrieves the document definition for the specified type.
        /// </summary>
        /// <param name="type">The type of the document.</param>
        /// <returns>The definition asset, or null if not found.</returns>
        public DocumentDefinition GetDef(DocumentType type)
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

            Debug.LogError($"[DocumentLibrary] Definition not found for type: {type}");
            return null;
        }

        private void UpdateLookup()
        {
            _lookup = new Dictionary<DocumentType, DocumentDefinition>();

            if (_definitions == null) return;

            foreach (var def in _definitions)
            {
                if (def == null) continue;

                if (_lookup.ContainsKey(def.Type))
                {
                    Debug.LogWarning($"[DocumentLibrary] Duplicate document type found: {def.Type}. Keeping the first one.");
                    continue;
                }

                _lookup.Add(def.Type, def);
            }
        }
    }
}
