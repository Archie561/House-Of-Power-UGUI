using DG.Tweening;
using Game.General;
using System;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Game.Features.Law
{
    /// <summary>
    /// Represents a UI object that displays the progress of a policy. Shows current level, progress bar, icon, etc.
    /// </summary>
    public class PolicyProgressView : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Slider _fillBar;
        [SerializeField] private Image _policyIcon;
        [SerializeField] private LocalizeStringEvent _nameLocalizer;

        [Header("Config")]
        [SerializeField] private ResourceLibrary _library;
        [SerializeField] private float _fillAnimationDuration = 0.5f;

        /// <summary>
        /// Initializes the view with static data.
        /// </summary>
        public void Initialize(ResourceType type)
        {
            var definition = _library.GetDef(type);
            if (definition == null) return;

            _policyIcon.sprite = definition.Icon;
            _nameLocalizer.StringReference = definition.LocalizedName;
        }

        /// <summary>
        /// Updates dynamic data (amount and progress bar) without re-initializing the whole view.
        /// </summary>
        public void UpdateView(int amount, int maxValue)
        {
            // Prevent division by zero
            float fillAmount = maxValue > 0 ? (float)amount / maxValue : 0f;
            fillAmount = Mathf.Clamp01(fillAmount);

            _fillBar.DOKill();
            _fillBar.DOValue(fillAmount, _fillAnimationDuration).SetEase(Ease.OutCubic);
        }
    }
}