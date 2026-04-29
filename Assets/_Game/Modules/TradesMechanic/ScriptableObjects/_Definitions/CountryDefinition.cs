using UnityEngine;
using UnityEngine.Localization;

namespace Game.General
{
    /// <summary>
    /// Configuration asset representing a specific country in the game.
    /// </summary>
    [CreateAssetMenu(fileName = "Country_New", menuName = "Game/Trade/Country Definition")]
    public class CountryDefinition : ScriptableObject
    {
        [Tooltip("Unique identifier for this country.")]
        [SerializeField] private CountryId _id;

        [Tooltip("Localized name of the country displayed in UI.")]
        [SerializeField] private LocalizedString _localizedName;

        [Tooltip("Sprite used for the country's flag.")]
        [SerializeField] private Sprite _flagIcon;

        public CountryId Id => _id;
        public LocalizedString LocalizedName => _localizedName;
        public Sprite FlagIcon => _flagIcon;
    }
}