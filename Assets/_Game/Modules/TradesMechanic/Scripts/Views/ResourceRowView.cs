using DG.Tweening;
using Game.General;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Game.Features.Trade
{
    /// <summary>
    /// Displays a full row for a resource in the storage list.
    /// Includes icon, name, capacity bar, and an upgrade button.
    /// </summary>
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
        [SerializeField] private float _fillAnimationDuration = 0.5f;

        /// <summary>
        /// Initializes the row with static data and sets up the upgrade button callback.
        /// </summary>
        public void Initialize(ResourceType type, Action<ResourceType> onAddCallback)
        {
            var definition = _library.GetDef(type);
            if (definition == null) return;

            _image.sprite = definition.Icon;
            _nameLocalizer.StringReference = definition.LocalizedName;

            _addButton.onClick.RemoveAllListeners();
            _addButton.onClick.AddListener(() => onAddCallback?.Invoke(type));
        }

        /// <summary>
        /// Updates dynamic data (amount and progress bar) without re-initializing the whole view.
        /// </summary>
        public void UpdateView(int amount, int maxCapacity)
        {
            _amount.text = $"{amount}/{maxCapacity}";

            // Prevent division by zero
            float fillAmount = maxCapacity > 0 ? (float)amount / maxCapacity : 0f;
            fillAmount = Mathf.Clamp01(fillAmount);

            _fillBar.DOKill();
            _fillBar.DOValue(fillAmount, _fillAnimationDuration).SetEase(Ease.OutCubic);
        }
    }
}