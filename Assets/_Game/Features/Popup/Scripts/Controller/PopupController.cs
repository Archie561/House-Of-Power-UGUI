using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PopupController : MonoBehaviour
{
    public static PopupController Instance { get; private set; }

    [Header("Overlay")]
    [SerializeField] private CanvasGroup _overlayCanvasGroup;
    [SerializeField] private Button _overlayButton;
    [SerializeField] private float _overlayFadeDuration = 0.2f;

    [Header("Popups Register")]
    [SerializeField] private List<BasePopup> _allPopups;

    private Dictionary<Type, BasePopup> _popupRegistry;
    private BasePopup _currentPopup;
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
        _overlayCanvasGroup.gameObject.SetActive(false);
        _overlayButton.onClick.AddListener(OnOverlayClicked);
    }

    private void OnOverlayClicked()
    {
        if (_isBusy || _currentPopup == null) return;
        if (_currentPopup.IsCloseOnOverlayAllowed) CloseCurrentPopup();
    }

    // --- Public API ---

    public void Show<T>(Action<T> setupAction = null) where T : BasePopup
    {
        if (_isBusy || _currentPopup != null)
        {
            Debug.LogWarning($"[PopupController] Busy. Cannot show {typeof(T).Name}");
            return;
        }

        var type = typeof(T);
        if (_popupRegistry.TryGetValue(type, out var popupBase))
        {
            var popup = popupBase as T;
            _isBusy = true;
            _currentPopup = popup;

            try
            {
                setupAction?.Invoke(popup);
            }
            catch (Exception e)
            {
                Debug.LogError($"[PopupController] Error initializing {type.Name}: {e}");
                _isBusy = false;
                _currentPopup = null;
                return;
            }

            FadeOverlay(true); // Оверлей з'являється паралельно або трохи раніше

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

    public void CloseCurrentPopup()
    {
        if (_currentPopup == null) return;

        _isBusy = true;

        _currentPopup.Close(onClosed: () =>
        {
            _currentPopup = null;
            FadeOverlay(false, () =>
            {
                _isBusy = false;
            });
        });
    }

    // --- Internal Logic ---

    private void FadeOverlay(bool show, Action onComplete = null)
    {
        _overlayCanvasGroup.DOKill();

        if (show)
        {
            _overlayCanvasGroup.gameObject.SetActive(true);
            _overlayCanvasGroup.DOFade(1f, _overlayFadeDuration);
        }
        else
        {
            _overlayCanvasGroup.DOFade(0f, _overlayFadeDuration).OnComplete(() =>
            {
                _overlayCanvasGroup.gameObject.SetActive(false);
                onComplete?.Invoke();
            });
        }
    }
}