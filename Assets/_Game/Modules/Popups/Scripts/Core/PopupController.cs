using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.Features.Popup
{
    /// <summary>
    /// Centralized controller for managing popups in the game. Handles showing, stacking, and closing of popups.
    /// </summary>
    public class PopupController : MonoBehaviour
    {
        public static PopupController Instance { get; private set; }

        [Header("Overlay Settings")]
        [SerializeField] private CanvasGroup _overlayCanvasGroup;
        [SerializeField] private Button _overlayButton;
        [SerializeField] private float _overlayFadeDuration = 0.2f;

        [Header("Registry")]
        [SerializeField] private List<BasePopup> _allPopups;

        private Dictionary<Type, BasePopup> _popupRegistry;
        private Stack<BasePopup> _activePopups = new Stack<BasePopup>();
        private bool _isBusy;

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
                if (_activePopups.Count > 0 && !_isBusy && _activePopups.Peek().IsCloseOnOverlayAllowed)
                {
                    CloseCurrentPopup();
                }
            }
        }

        #region Public API

        /// <summary>
        /// Shows a popup of the specified type. Stacks on top of existing ones.
        /// </summary>
        /// <typeparam name="T">The type of popup to show.</typeparam>
        /// <param name="setupAction">Action to configure the popup data before opening.</param>
        public async void Show<T>(Action<T> setupAction) where T : BasePopup
        {
            if (_isBusy)
            {
                Debug.LogWarning($"[PopupController] System is busy. Cannot show {typeof(T).Name}");
                return;
            }

            if (!_popupRegistry.TryGetValue(typeof(T), out var popupBase))
            {
                Debug.LogError($"[PopupController] Popup {typeof(T).Name} is not registered!");
                return;
            }

            var popup = popupBase as T;

            // Protection against opening the same popup twice in a row
            if (_activePopups.Count > 0 && _activePopups.Peek() == popup)
            {
                Debug.LogWarning($"[PopupController] Popup {typeof(T).Name} is already on top.");
                return;
            }

            _isBusy = true;

            try
            {
                // Configure the popup with provided data before opening and pushing it on top of the stack
                setupAction?.Invoke(popup);
                _activePopups.Push(popup);

                // Ensure the overlay is just below the top popup
                _overlayCanvasGroup.transform.SetAsLastSibling();
                popup.transform.SetAsLastSibling();

                // If this is the first popup, fade in the overlay
                Task fadeTask = _activePopups.Count == 1 ? FadeOverlayAsync(show: true) : Task.CompletedTask;
                Task openTask = popup.OpenAsync();

                // Wait for both the popup to close and the overlay to fade (if applicable)
                await Task.WhenAll(openTask, fadeTask);
            }
            catch (Exception e)
            {
                Debug.LogError($"[PopupController] Error while opening {typeof(T).Name} popup: {e.Message}");
                // If something goes wrong during opening, ensure we disable and pop the failed popup from the stack to prevent blocking
                if (_activePopups.Count > 0 && _activePopups.Peek() == popup)
                {
                    _activePopups.Pop();
                    popup.gameObject.SetActive(false);
                }
            }
            finally
            {
                _isBusy = false;
            }
        }

        /// <summary>
        /// Closes the currently active popup and hides the overlay.
        /// </summary>
        public async void CloseCurrentPopup()
        {
            if (_activePopups.Count == 0 || _isBusy) return;

            _isBusy = true;
            var popupToClose = _activePopups.Pop();

            try
            {
                Task closeTask = popupToClose.CloseAsync();
                // If after closing the popup there are no more popups left, fade out the overlay
                Task fadeTask = _activePopups.Count == 0 ? FadeOverlayAsync(show: false) : Task.CompletedTask;

                // Wait for both the popup to close and the overlay to fade (if applicable)
                await Task.WhenAll(closeTask, fadeTask);

                // If there are still popups left, ensure the overlay is just below the new top popup
                if (_activePopups.Count > 0)
                {
                    var newTopPopup = _activePopups.Peek();
                    _overlayCanvasGroup.transform.SetAsLastSibling();
                    newTopPopup.transform.SetAsLastSibling();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[PopupController] Error while closing popup {popupToClose.GetType().Name}: {e.Message}");
                // If something goes wrong during closing, ensure we disable the failed popup to prevent blocking
                if (_activePopups.Count > 0 && _activePopups.Peek() == popupToClose)
                {
                    popupToClose.gameObject.SetActive(false);
                }
            }
            finally
            {
                _isBusy = false;
            }
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

        // Sets up the overlay to block interactions with the game world and handle clicks for closing popups
        private void SetupOverlay()
        {
            _overlayCanvasGroup.alpha = 0f;
            _overlayCanvasGroup.blocksRaycasts = false;
            _overlayCanvasGroup.gameObject.SetActive(false);

            _overlayButton.onClick.RemoveAllListeners();
            _overlayButton.onClick.AddListener(OnOverlayClicked);
        }

        // Closes the top popup if the overlay is clicked and the top popup allows closing on overlay click
        private void OnOverlayClicked()
        {
            if (_isBusy || _activePopups.Count == 0) return;

            var topPopup = _activePopups.Peek();
            if (topPopup.IsCloseOnOverlayAllowed)
            {
                CloseCurrentPopup();
            }
        }

        // Handles fading in/out the overlay when the first popup is shown or the last popup is closed
        private async Task FadeOverlayAsync(bool show)
        {
            _overlayCanvasGroup.DOKill();

            if (show)
            {
                _overlayCanvasGroup.gameObject.SetActive(true);
                _overlayCanvasGroup.blocksRaycasts = true; // Block clicks on the game world

                await _overlayCanvasGroup.DOFade(1f, _overlayFadeDuration).AsyncWaitForCompletion();
            }
            else
            {
                await _overlayCanvasGroup.DOFade(0f, _overlayFadeDuration).AsyncWaitForCompletion();

                _overlayCanvasGroup.blocksRaycasts = false; // Allow clicks again
                _overlayCanvasGroup.gameObject.SetActive(false);
            }
        }

        #endregion
    }
}