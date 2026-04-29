using System;
using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Game.Features.Popup
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class BasePopup : MonoBehaviour
    {
        protected const float ANIMATION_DURATION = 0.3f;

        private CanvasGroup _canvasGroup;

        /// <summary>
        /// Determines if clicking the overlay (or back button) closes this popup.
        /// </summary>
        public bool IsCloseOnOverlayAllowed { get; protected set; } = true;
        public Action OnOpened { get; set; }
        public Action OnClosed { get; set; }

        protected virtual void Awake() => _canvasGroup = GetComponent<CanvasGroup>();

        /// <summary>
        /// Activates the popup and plays the opening animation.
        /// </summary>
        public virtual async Task OpenAsync()
        {
            transform.DOKill();
            if (!gameObject.activeSelf) gameObject.SetActive(true);

            // Make visible but block clicks until animation finishes
            _canvasGroup.blocksRaycasts = false;

            await transform.DOScale(Vector3.one, ANIMATION_DURATION).From(Vector3.zero).SetEase(Ease.OutBack).AsyncWaitForCompletion();

            _canvasGroup.blocksRaycasts = true;
            OnOpened?.Invoke();
        }

        /// <summary>
        /// Plays the closing animation and deactivates the popup.
        /// </summary>
        public virtual async Task CloseAsync()
        {
            transform.DOKill();

            // Immediately block interaction to prevent double-clicks
            _canvasGroup.blocksRaycasts = false;

            await transform.DOScale(Vector3.zero, ANIMATION_DURATION).SetEase(Ease.InBack).AsyncWaitForCompletion();

            gameObject.SetActive(false);
            OnClosed?.Invoke();

            // Clear subscribers to prevent memory leaks
            OnOpened = null;
            OnClosed = null; 
        }
    }
}