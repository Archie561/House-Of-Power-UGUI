using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupCostButton : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button _button;
    [SerializeField] private Image _resourceImage;
    [SerializeField] private TextMeshProUGUI _costText;

    [Header("Config")]
    [SerializeField] private ResourceLibrary _resourceLibrary;

    public void Initialize(ResourceType type, int cost, Action onClickCallback)
    {
        var definition = _resourceLibrary.GetDef(type);
        if (definition != null)
        {
            _resourceImage.sprite = definition.Icon;
        }

        _costText.text = cost.ToString();

        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(() => onClickCallback?.Invoke());
    }
}
