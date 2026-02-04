using Game.General;
using System.Collections.Generic;

namespace Game.Features.Law
{
    /// <summary>
    /// Represents a specific law proposal with its effects upon acceptance or rejection.
    /// </summary>
    public class LawData
    {
        /// <summary>
        /// Unique identifier for the law. Also serves as the key for law content in localization table.
        /// </summary>
        public string Id { get; }
        public DocumentType Type { get; }
        public IReadOnlyList<ResourceAmount> OnAcceptEffects { get; }
        public IReadOnlyList<ResourceAmount> OnRejectEffects { get; }

        public LawData(string id, DocumentType type, List<ResourceAmount> onAcceptEffects, List<ResourceAmount> onRejectEffects)
        {
            Id = id;
            Type = type;
            OnAcceptEffects = onAcceptEffects.AsReadOnly();
            OnRejectEffects = onRejectEffects.AsReadOnly();
        }
    }
}
