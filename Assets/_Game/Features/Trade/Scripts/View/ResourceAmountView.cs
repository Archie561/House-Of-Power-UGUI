using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResourceAmountView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image _image;
    [SerializeField] private TextMeshProUGUI _amountText;

    [Header("Config")]
    [SerializeField] private ResourceLibrary _library;
    [SerializeField] private Color _incomeColor;
    [SerializeField] private Color _expenseColor;

    public void Initialize(ResourceData data, bool isImport)
    {
        var definition = _library.GetDef(data.Type);
        if (definition != null)
        {
            _image.sprite = definition.Icon;
        }

        _amountText.text = isImport ? $"+{data.Amount}" : $"-{data.Amount}";
        _amountText.color = isImport ? _incomeColor : _expenseColor;
    }
}
