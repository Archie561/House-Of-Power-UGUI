using Game.General;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Features.Law
{
    /// <summary>
    /// Data Model representing a specific law proposal.
    /// Acts as a read-only container for static data parsed from configuration tables,
    /// which can be passed to Views for display.
    /// </summary>
    [Serializable]
    public class LawData
    {
        [SerializeField] private string _id;
        [SerializeField] private DocumentType _type;
        [SerializeField] private List<ResourceAmount> _onAcceptEffects;
        [SerializeField] private List<ResourceAmount> _onRejectEffects;

        /// <summary>
        /// Unique identifier for the law. Also serves as the key for law content in localization table.
        /// </summary>
        public string Id => _id;
        public DocumentType Type => _type;
        public IReadOnlyList<ResourceAmount> OnAcceptEffects => _onAcceptEffects;
        public IReadOnlyList<ResourceAmount> OnRejectEffects => _onRejectEffects;

        public LawData(string id, DocumentType type, List<ResourceAmount> onAcceptEffects, List<ResourceAmount> onRejectEffects)
        {
            _id = id;
            _type = type;
            _onAcceptEffects = onAcceptEffects;
            _onRejectEffects = onRejectEffects;
        }
    }
}
