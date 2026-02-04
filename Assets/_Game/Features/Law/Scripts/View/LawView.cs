using Game.General;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Game.Features.Law
{
    /// <summary>
    /// Represents a law document in the UI.
    /// Displays the localized law text, sprite, and handles the click event.
    /// </summary>
    public class LawView : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image _documentImage;
        [SerializeField] private TextMeshProUGUI _lawText;
        [SerializeField] private LocalizeStringEvent _authorText;

        [Header("Config")]
        [SerializeField] private DocumentLibrary _documentLibrary;

        /// <summary>
        /// Configure the law view with the specific document type and localized content.
        /// </summary>
        /// <param name="type">Type of document (e.g. healthcare, economy, etc.)</param>
        /// <param name="localizedContent">Localized law text.</param>
        public void Initialize(DocumentType type, string localizedContent)
        {
            var definition = _documentLibrary.GetDef(type);
            if (definition != null)
            {
                _documentImage.sprite = definition.Sprite;
                _authorText.StringReference = definition.LocalizedAuthor;
            }

            _lawText.text = localizedContent;
        }
    }
}
