using UnityEngine;
using UnityEngine.Localization;

namespace Game.General
{
    /// <summary>
    /// Configuration asset representing a specific resource (Wood, Gold, etc.).
    /// </summary>
    [CreateAssetMenu(fileName = "Res_New", menuName = "Game/Resources/Definition")]
    public class ResourceDefinition : ScriptableObject
    {
        [Tooltip("Unique identifier for this resource type.")]
        [SerializeField] private ResourceType _type;

        [Tooltip("Localized name of the resource displayed in UI.")]
        [SerializeField] private LocalizedString _localizedName;

        [Tooltip("Icon representing the resource.")]
        [SerializeField] private Sprite _icon;

        public ResourceType Type => _type;
        public LocalizedString LocalizedName => _localizedName;
        public Sprite Icon => _icon;
    }
}