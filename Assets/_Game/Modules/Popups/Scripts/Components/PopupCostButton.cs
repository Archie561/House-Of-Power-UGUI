using Game.General;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Features.Popup
{
    /// <summary>
    /// A reusable button component that displays a resource icon, cost, and handles click events.
    /// Handles visual state (alpha) based on affordability via CanvasGroup.
    /// </summary>
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

        /// <summary>
        /// Configures the button visuals and logic.
        /// </summary>
        /// <param name="type">The resource type (icon).</param>
        /// <param name="cost">The numerical cost to display.</param>
        /// <param name="canAfford">If true, button is fully opaque and clickable.</param>
        /// <param name="onClickCallback">Action to execute on click.</param>
        public void Initialize(ResourceType type, int cost, bool canAfford, Action onClickCallback)
        {
            // Setup Icon
            var definition = _resourceLibrary.GetDef(type);
            if (definition != null)
            {
                _resourceImage.sprite = definition.Icon;
            }

            // Setup Text
            _costText.text = cost.ToString();

            // Setup Visual State
            _canvasGroup.blocksRaycasts = canAfford;
            _canvasGroup.alpha = canAfford ? 1f : _disableAlpha;

            // Setup Interaction
            _button.onClick.RemoveAllListeners();

            if (canAfford)
            {
                _button.onClick.AddListener(() => onClickCallback?.Invoke());
            }
        }
    }
}
