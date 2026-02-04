using UnityEngine;
using UnityEngine.Localization;

namespace Game.General
{
    /// <summary>
    /// Configuration asset representing a law document definition.
    /// </summary>
    [CreateAssetMenu(fileName = "Document_new", menuName = "Game/Law/DocumentDefinition")]
    public class DocumentDefinition : ScriptableObject
    {
        [Tooltip("Unique identifier for this document type.")]
        [SerializeField] private DocumentType _type;

        [Tooltip("Localized author name displayed in UI.")]
        [SerializeField] private LocalizedString _localizedAuthor;

        [Tooltip("Sprite representing this document.")]
        [SerializeField] private Sprite _sprite;

        public DocumentType Type => _type;
        public LocalizedString LocalizedAuthor => _localizedAuthor;
        public Sprite Sprite => _sprite;
    }
}
