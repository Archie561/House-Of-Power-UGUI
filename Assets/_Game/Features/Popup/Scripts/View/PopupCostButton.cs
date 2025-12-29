using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupCostButton : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Button _button;
    [SerializeField] private Image _resourceImage;
    [SerializeField] private TextMeshProUGUI _costText;

    [Header("Config")]
    [SerializeField] private ResourceLibrary _resourceLibrary;
    [SerializeField, Range(0f, 1f)] private float _disableAlpha = 0.6f;

    public void Initialize(ResourceType type, int cost, bool canAfford, Action onClickCallback)
    {
        var definition = _resourceLibrary.GetDef(type);
        if (definition != null) _resourceImage.sprite = definition.Icon;

        _costText.text = cost.ToString();

        _canvasGroup.blocksRaycasts = canAfford;
        _canvasGroup.alpha = canAfford ? 1 : _disableAlpha;

        _button.onClick.RemoveAllListeners();
        if (canAfford) _button.onClick.AddListener(() => onClickCallback?.Invoke());
    }
}
