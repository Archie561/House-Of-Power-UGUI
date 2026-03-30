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
        private Stack<BasePopup> _activePopups = new Stack<BasePopup>();
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
            // if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            // {
            //     if (_currentPopup != null && !_isBusy && _currentPopup.IsCloseOnOverlayAllowed)
            //     {
            //         CloseCurrentPopup();
            //     }
            // }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Shows a popup of the specified type. Stacks on top of existing ones.
        /// </summary>
        /// <typeparam name="T">The type of popup to show.</typeparam>
        /// <param name="setupAction">Action to configure the popup data before opening.</param>
        public void Show<T>(Action<T> setupAction = null) where T : BasePopup
        {
            if (_isBusy)
            {
                Debug.LogWarning($"[PopupController] System is busy. Cannot show {typeof(T).Name}");
                return;
            }

            var type = typeof(T);
            if (_popupRegistry.TryGetValue(type, out var popupBase))
            {
                var popup = popupBase as T;

                // Захист від подвійного відкриття одного і того ж попапу підряд
                if (_activePopups.Count > 0 && _activePopups.Peek() == popup)
                {
                    Debug.LogWarning($"[PopupController] Popup {type.Name} is already on top.");
                    return;
                }

                _isBusy = true;

                try
                {
                    setupAction?.Invoke(popup);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[PopupController] Error initializing {type.Name}: {e}");
                    _isBusy = false;
                    return;
                }

                // Додаємо в стек
                _activePopups.Push(popup);

                _overlayCanvasGroup.transform.SetAsLastSibling();

                popup.transform.SetAsLastSibling();

                if (_activePopups.Count == 1)
                {
                    FadeOverlay(show: true);
                }

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
            if (_activePopups.Count == 0 || _isBusy) return;

            _isBusy = true;

            var popupToClose = _activePopups.Pop();

            popupToClose.Close(onClosed: () =>
            {
                // Якщо після закриття стек пустий - вимикаємо затемнення
                if (_activePopups.Count == 0)
                {
                    FadeOverlay(show: false, onComplete: () =>
                    {
                        _isBusy = false;
                    });
                }
                else
                {
                    // Якщо під ним ще є попапи, переміщуємо затемнення під новий верхній попап
                    var newTopPopup = _activePopups.Peek();
                    _overlayCanvasGroup.transform.SetAsLastSibling();
                    newTopPopup.transform.SetAsLastSibling();
                    _isBusy = false;
                }
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
            if (_isBusy || _activePopups.Count == 0) return;

            var topPopup = _activePopups.Peek();
            if (topPopup.IsCloseOnOverlayAllowed)
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