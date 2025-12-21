using UnityEngine;
using UnityEngine.UI;

public class PopupController : MonoBehaviour
{
    public static PopupController Instance { get; private set; }

    [Header("Overlay")]
    [SerializeField] private GameObject _overlayBackground; // Чорна напівпрозора панель на весь екран
    [SerializeField] private Button _overlayButton;         // Кнопка на ній же, щоб клік повз вікно закривав його

    [Header("Registered Popups")]
    //[SerializeField] private ConfirmationPopup _confirmationPopup;
    // Тут будуть інші специфічні попапи
    // [SerializeField] private TradeOfferPopup _tradeOfferPopup;
    // [SerializeField] private UpgradePopup _upgradePopup;

    private BasePopup _currentPopup;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _overlayBackground.SetActive(false);
        _overlayButton.onClick.AddListener(OnOverlayClicked);
    }

    // --- PUBLIC API ---

    //public void ShowConfirmation(string title, string body, System.Action onConfirm, System.Action onCancel = null)
    //{
        //_confirmationPopup.Setup(title, body, onConfirm, onCancel);
        //OpenPopup(_confirmationPopup);
    //}

    /* // Приклад для майбутнього кастомного попапа
    public void ShowTradeOffer(TradeOfferData data, Action onAccept)
    {
        _tradeOfferPopup.Setup(data, onAccept);
        OpenPopup(_tradeOfferPopup);
    }
    */

    // --- INTERNAL LOGIC ---

    private void OpenPopup(BasePopup popup)
    {
        if (_currentPopup != null)
            return;

        _currentPopup = popup;
        _overlayBackground.SetActive(true);

        popup.Open();
    }

    public void CloseCurrentPopup()
    {
        if (_currentPopup != null)
        {
            _currentPopup.Close();
            _currentPopup = null;
        }

        _overlayBackground.SetActive(false);
    }

    private void OnOverlayClicked()
    {
        CloseCurrentPopup();
    }
}
