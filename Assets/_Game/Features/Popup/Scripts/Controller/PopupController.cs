using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.Features.Popup
{
    public class PopupController : MonoBehaviour
    {
        public static PopupController Instance { get; private set; }

        #region Configuration & State

        [Header("Overlay Settings")]
        [SerializeField] private CanvasGroup _overlayCanvasGroup;
        [SerializeField] private Button _overlayButton;
        [SerializeField] private float _overlayFadeDuration = 0.2f;

        [Header("Registry")]
        [SerializeField] private List<BasePopup> _allPopups;

        private Dictionary<Type, BasePopup> _popupRegistry;
        private BasePopup _currentPopup;
        private bool _isBusy;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeRegistry();
            SetupOverlay();
        }

        private void Update()
        {
            // Handle Android back button
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (_currentPopup != null && !_isBusy && _currentPopup.IsCloseOnOverlayAllowed)
                {
                    CloseCurrentPopup();
                }
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Shows a popup of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of popup to show.</typeparam>
        /// <param name="setupAction">Action to configure the popup data before opening.</param>
        public void Show<T>(Action<T> setupAction = null) where T : BasePopup
        {
            if (_isBusy || _currentPopup != null)
            {
                Debug.LogWarning($"[PopupController] System is busy. Cannot show {typeof(T).Name}");
                return;
            }

            var type = typeof(T);
            if (_popupRegistry.TryGetValue(type, out var popupBase))
            {
                var popup = popupBase as T;

                // Lock the controller immediately
                _isBusy = true;
                _currentPopup = popup;

                try
                {
                    setupAction?.Invoke(popup);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[PopupController] Error initializing {type.Name}: {e}");
                    // Reset state if initialization fails
                    _isBusy = false;
                    _currentPopup = null;
                    return;
                }

                FadeOverlay(show: true);

                popup.Open(onOpened: () =>
                {
                    _isBusy = false;
                });
            }
            else
            {
                Debug.LogError($"[PopupController] Popup {type.Name} is not registered!");
            }
        }

        /// <summary>
        /// Closes the currently active popup and hides the overlay.
        /// </summary>
        public void CloseCurrentPopup()
        {
            if (_currentPopup == null) return;

            _isBusy = true;

            _currentPopup.Close(onClosed: () =>
            {
                _currentPopup = null;
                FadeOverlay(show: false, onComplete: () =>
                {
                    _isBusy = false;
                });
            });
        }

        #endregion

        #region Private Methods

        private void InitializeRegistry()
        {
            _popupRegistry = new Dictionary<Type, BasePopup>();
            foreach (var popup in _allPopups)
            {
                if (popup == null) continue;

                var type = popup.GetType();
                if (!_popupRegistry.ContainsKey(type))
                {
                    _popupRegistry.Add(type, popup);
                    popup.gameObject.SetActive(false);
                }
            }
        }

        private void SetupOverlay()
        {
            _overlayCanvasGroup.alpha = 0f;
            _overlayCanvasGroup.blocksRaycasts = false;
            _overlayCanvasGroup.gameObject.SetActive(false);

            _overlayButton.onClick.RemoveAllListeners();
            _overlayButton.onClick.AddListener(OnOverlayClicked);
        }

        private void OnOverlayClicked()
        {
            if (_isBusy || _currentPopup == null) return;

            if (_currentPopup.IsCloseOnOverlayAllowed)
            {
                CloseCurrentPopup();
            }
        }

        private void FadeOverlay(bool show, Action onComplete = null)
        {
            _overlayCanvasGroup.DOKill();

            if (show)
            {
                _overlayCanvasGroup.gameObject.SetActive(true);
                _overlayCanvasGroup.blocksRaycasts = true; // Block clicks on the game world
                _overlayCanvasGroup.DOFade(1f, _overlayFadeDuration);
                onComplete?.Invoke();
            }
            else
            {
                _overlayCanvasGroup.blocksRaycasts = false; // Allow clicks again
                _overlayCanvasGroup.DOFade(0f, _overlayFadeDuration).OnComplete(() =>
                {
                    _overlayCanvasGroup.gameObject.SetActive(false);
                    onComplete?.Invoke();
                });
            }
        }

        #endregion
    }
}