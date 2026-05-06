using System;
using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Game.Features.Popup
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class BasePopup : MonoBehaviour
    {
        /// <summary>
        /// Determines if clicking the overlay (or back button) closes this popup.
        /// </summary>
        public bool IsCloseOnOverlayAllowed { get; protected set; } = true;
        /// <summary>
        /// Event invoked after the popup has fully closed. This is invoked after the closing animation completes and the popup is deactivated.
         // </summary>
        public Action OnPopupClosed { get; set; }

        protected const float ANIMATION_DURATION = 0.3f;
        private CanvasGroup _canvasGroup;

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

            OnPopupClosed?.Invoke();

            OnPopupClosed = null; // Clear subscribers to prevent memory leaks
            gameObject.SetActive(false);
        }
    }
}