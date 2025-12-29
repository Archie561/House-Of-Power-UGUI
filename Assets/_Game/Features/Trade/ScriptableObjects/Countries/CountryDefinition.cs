using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(fileName = "Country_New", menuName = "Game/Trade/Country Definition")]
public class CountryDefinition : ScriptableObject
{
    public CountryId Id;
    public LocalizedString LocalizedName;
    public Sprite FlagIcon;
}