using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

public class PopupController : MonoBehaviour
{
    public static PopupController Instance { get; private set; }

    [Header("Overlay")]
    [SerializeField] private CanvasGroup _overlayCanvasGroup;
    [SerializeField] private Button _overlayButton;
    [SerializeField] private float _overlayFadeDuration = 0.2f;

    [Header("Registered Popups")]
    [SerializeField] private UpgradeStoragePopup _upgradeStoragePopup;

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

        SetupOverlay();
    }

    private void SetupOverlay()
    {
        _overlayCanvasGroup.alpha = 0f;
        _overlayCanvasGroup.blocksRaycasts = false;
        _overlayCanvasGroup.gameObject.SetActive(false);

        _overlayButton.onClick.AddListener(CloseCurrentPopup);
    }

    public void ShowUpgradeStoragePopup(UpgradeStorageData data, Action onDefaultCostClick, Action onPremiumCostClick)
    {
        _upgradeStoragePopup.Initialize(data, onDefaultCostClick, onPremiumCostClick);
        OpenPopupInternal(_upgradeStoragePopup);
    }

    private void OpenPopupInternal(BasePopup popupToOpen)
    {
        if (_isBusy || _currentPopup != null)
        {
            Debug.LogWarning("Cannot open popup: Controller is busy or another popup is open.");
            return;
        }

        _isBusy = true;
        _currentPopup = popupToOpen;

        _overlayCanvasGroup.gameObject.SetActive(true);
        _overlayCanvasGroup.blocksRaycasts = true;
        _overlayCanvasGroup.DOFade(1f, _overlayFadeDuration);

        popupToOpen.Open(onOpened: () =>
        {
            _isBusy = false;
        });
    }

    private void CloseCurrentPopup()
    {
        if (_isBusy || _currentPopup == null) return;

        _isBusy = true;

        _currentPopup.Close(onClosed: () =>
        {
            _currentPopup = null;

            _overlayCanvasGroup.DOFade(0f, _overlayFadeDuration).OnComplete(() =>
            {
                _overlayCanvasGroup.gameObject.SetActive(false);
                _overlayCanvasGroup.blocksRaycasts = false;
                _isBusy = false;
            });
        });
    }
}