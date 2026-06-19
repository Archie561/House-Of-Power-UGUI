using System;
using DG.Tweening;
using Game.General;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Game.Features.Law
{
    /// <summary>
    /// Represents a UI law object. Contains the necessary methods for dragging the card and playing animations
    /// </summary>
    [RequireComponent(typeof(CanvasGroup), typeof(RectTransform))]
    public class LawCardView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("UI References")]
        [SerializeField] private Image _documentImage;
        [SerializeField] private LocalizeStringEvent _lawText;
        [SerializeField] private LocalizeStringEvent _authorText;

        [Header("Swipe Config")]
        [SerializeField] private float _swipeThreshold = 200f;
        [SerializeField] private float _tiltMultiplier = 0.05f;
        [SerializeField] private float _animationDuration = 0.3f;

        [Header("Config")]
        [SerializeField] private DocumentLibrary _documentLibrary;

        public event Action<bool> OnSwipeDecided;
        public event Action OnHideAnimationFinished;

        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        private Canvas _parentCanvas;

        private Vector2 _startPosition;
        private Vector3 _startRotation;
        private bool _isInteractable = false;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();
            _parentCanvas = GetComponentInParent<Canvas>();

            _startPosition = _rectTransform.anchoredPosition;
            _startRotation = _rectTransform.eulerAngles;
        }

        // Initializes the card with content
        private void Initialize(LawData data)
        {
            var definition = _documentLibrary.GetDef(data.Type);
            if (definition != null)
            {
                _documentImage.sprite = definition.Sprite;
                _authorText.StringReference = definition.LocalizedAuthor;
            }

            _lawText.StringReference.TableEntryReference = data.Id;
        }

        // Resets the card to its original position and rotation
        private void ResetCardState()
        {
            _rectTransform.DOKill();
            _canvasGroup.DOKill();
            _rectTransform.anchoredPosition = _startPosition;
            _rectTransform.rotation = Quaternion.Euler(_startRotation);
        }

        /// <summary>
        /// Shows initialized law card with optional animation
        /// </summary>
        public void Show(LawData data, bool playAnimation = true)
        {
            gameObject.SetActive(true);
            _isInteractable = false;

            Initialize(data);
            ResetCardState();

            if (playAnimation)
            {
                _rectTransform.localScale = Vector3.zero;
                _rectTransform.DOScale(Vector3.one, _animationDuration)
                    .SetEase(Ease.OutBack)
                    .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                    .OnComplete(() => _isInteractable = true);
            }
            else
            {
                _rectTransform.localScale = Vector3.one;
                _isInteractable = true;
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!_isInteractable) return;
            _rectTransform.DOKill();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isInteractable) return;

            float deltaX = eventData.delta.x / _parentCanvas.scaleFactor;
            _rectTransform.anchoredPosition += new Vector2(deltaX, 0f);

            float tiltAngle = -_rectTransform.anchoredPosition.x * _tiltMultiplier;
            _rectTransform.rotation = Quaternion.Euler(0, 0, tiltAngle);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isInteractable) return;

            float xOffset = _rectTransform.anchoredPosition.x - _startPosition.x;

            if (xOffset > _swipeThreshold)
            {
                AcceptLaw();
            }
            else if (xOffset < -_swipeThreshold)
            {
                RejectLaw();
            }
            else 
            {
                CancelSwipe();
            }
        }

        private void AcceptLaw()
        {
            _isInteractable = false;
            OnSwipeDecided?.Invoke(true);
            AnimateOut(isAccepted: true);
        }

        private void RejectLaw()
        {
            _isInteractable = false;
            OnSwipeDecided?.Invoke(false);
            AnimateOut(isAccepted: false);
        }

        private void CancelSwipe()
        {
            _rectTransform.DOAnchorPos(_startPosition, _animationDuration).SetEase(Ease.OutBack);
            _rectTransform.DORotate(_startRotation, _animationDuration).SetEase(Ease.OutBack);
        }

        private void AnimateOut(bool isAccepted)
        {
            // Calculating the off-screen offset based on the parent canvas size and card size
            RectTransform parentRect = (RectTransform)_rectTransform.parent;
            var offScreenOffset = (parentRect.rect.width / 2f + _rectTransform.rect.width / 2f) * 1.2f;

            _rectTransform.DOAnchorPosX(isAccepted ? offScreenOffset : -offScreenOffset, _animationDuration)
                .SetEase(Ease.OutQuart)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(() =>
                {
                    gameObject.SetActive(false);
                    OnHideAnimationFinished.Invoke();
                });
        }
    } 
}