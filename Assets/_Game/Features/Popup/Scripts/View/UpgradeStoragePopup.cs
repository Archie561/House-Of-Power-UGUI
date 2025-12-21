using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

public class UpgradeStoragePopup : BasePopup
{
    [SerializeField] private ResourceLibrary _resourceLibrary;
    [SerializeField] private LocalizeStringEvent _descriptionLocalizer;

    private void Start()
    {
        
        Setup(ResourceType.Wood);
    }

    private void Setup(ResourceType type)
    {
        // 1. Отримуємо Definition ресурсу
        var def = _resourceLibrary.GetDef(type);

        _descriptionLocalizer.StringReference.Arguments = new object[] { def.LocalizedName.GetLocalizedString() };

        _descriptionLocalizer.RefreshString();
    }
}
