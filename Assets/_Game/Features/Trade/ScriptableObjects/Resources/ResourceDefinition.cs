using System;
using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(fileName = "Res_New", menuName = "Game/Resources/Definition")]
public class ResourceDefinition : ScriptableObject
{
    public ResourceType Type;
    public LocalizedString LocalizedName;
    public Sprite Icon;
}