using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.General
{
    /// <summary>
    /// Displays a single resource icon and its amount (e.g., "+5 Wood" or "-10 Gold").
    /// </summary>
    public class ResourceAmountView : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image _image;
        [SerializeField] private TextMeshProUGUI _amountText;

        [Header("Config")]
        [SerializeField] private ResourceLibrary _library;
        [SerializeField] private Color _incomeColor = Color.green;
        [SerializeField] private Color _expenseColor = Color.red;

        /// <summary>
        /// Configures the view with data and determines color based on import/export status.
        /// </summary>
        public void Initialize(ResourceAmount res, bool isExpense)
        {
            var definition = _library.GetDef(res.Type);
            if (definition != null)
            {
                _image.sprite = definition.Icon;
            }

            _amountText.text = isExpense ? $"-{res.Amount}" : $"+{res.Amount}";
            _amountText.color = isExpense ? _expenseColor : _incomeColor;
        }
    }
}
