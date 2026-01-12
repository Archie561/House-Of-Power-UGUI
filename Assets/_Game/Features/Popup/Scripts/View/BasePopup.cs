using System;
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

        protected virtual void Awake() => _canvasGroup = GetComponent<CanvasGroup>();

        /// <summary>
        /// Activates the popup and plays the opening animation.
        /// </summary>
        /// <param name="onOpened">Callback invoked when animation finishes.</param>
        public virtual void Open(Action onOpened = null)
        {
            transform.DOKill();
            gameObject.SetActive(true);

            // Make visible but block clicks until animation finishes
            _canvasGroup.blocksRaycasts = false;

            transform.DOScale(Vector3.one, ANIMATION_DURATION)
                .From(Vector3.zero)
                .SetEase(Ease.OutBack)
                .OnComplete(() =>
                {
                    _canvasGroup.blocksRaycasts = true;
                    onOpened?.Invoke();
                });
        }

        /// <summary>
        /// Plays the closing animation and deactivates the popup.
        /// </summary>
        /// <param name="onClosed">Callback invoked when popup is fully closed.</param>
        public virtual void Close(Action onClosed = null)
        {
            transform.DOKill();

            // Immediately block interaction to prevent double-clicks
            _canvasGroup.blocksRaycasts = false;

            transform.DOScale(Vector3.zero, ANIMATION_DURATION)
                .SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    gameObject.SetActive(false);
                    onClosed?.Invoke();
                });
        }
    }
}