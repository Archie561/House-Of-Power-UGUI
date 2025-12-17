using UnityEngine;

[CreateAssetMenu(fileName = "Country_New", menuName = "Game/Trade/Country Definition")]
public class CountryDefinition : ScriptableObject
{
    public CountryId Id;
    public Sprite FlagIcon;
}