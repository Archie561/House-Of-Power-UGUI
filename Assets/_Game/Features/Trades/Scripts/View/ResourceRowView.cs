using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

public class ResourceRowView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image _image;
    [SerializeField] private LocalizeStringEvent _nameLocalizer;
    [SerializeField] private TextMeshProUGUI _amount;
    [SerializeField] private Slider _fillBar;
    [SerializeField] private Button _addButton;

    [Header("Config")]
    [SerializeField] private ResourceLibrary _library;

    public void Initialize(ResourceData data, Action<ResourceType> onAddCallback)
    {
        var definition = _library.GetDef(data.Type);
        if (definition != null)
        {
            _image.sprite = definition.Icon;
            _nameLocalizer.StringReference = definition.LocalizedName;
        }

        UpdateView(data.Amount, data.MaxCapacity);

        _addButton.onClick.RemoveAllListeners();
        _addButton.onClick.AddListener(() => onAddCallback?.Invoke(data.Type));
    }

    public void UpdateView(int amount, int maxCapacity)
    {
        _amount.text = $"{amount}/{maxCapacity}";
        _fillBar.value = Mathf.Clamp01((float)amount / maxCapacity);
    }
}