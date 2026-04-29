using Game.General;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Game.Features.Law
{
    /// <summary>
    /// Represents the detailed view for a policy's progress, used in Law Policies Popup.
    /// Displays the current level, XP progress, description, and provides a buy button for upgrades.
    /// </summary>
    public class DetailedPolicyProgressView : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private ResourceLibrary _library;

        [Header("UI References")]
        [SerializeField] private PolicyProgressView _progressView;
        [SerializeField] private LocalizeStringEvent _descriptionTextLocalizer;
        [SerializeField] private TextMeshProUGUI _xpText;
        [SerializeField] private Button _buyButton;

        /// <summary>
        /// Initializes the view with static data.
        /// </summary>
        public void Initialize(DetailedPolicyProgressData data)
        {
            _progressView.Initialize(data.Type);
            _progressView.UpdateView(data.Level, data.CurrentXp, data.RequiredXp);

            var definition = _library.GetDef(data.Type);
            if (definition != null)
            {
                _descriptionTextLocalizer.StringReference = definition.LocalizedDescription;
            }

            _xpText.text = $"{data.CurrentXp}/{data.RequiredXp}";

            _buyButton.onClick.RemoveAllListeners();
            _buyButton.onClick.AddListener(() => data.OnBuyClick?.Invoke());
        }
    }
}
