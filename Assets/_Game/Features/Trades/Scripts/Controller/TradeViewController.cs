using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TradeViewController : MonoBehaviour
{
    [Header("ResourcesPanel")]
    [SerializeField] private Transform _resourcesContainer;
    [SerializeField] private ResourceRowView _resourceRowPrefab;

    [Header("Offers Panel")]
    [SerializeField] private Transform _offersContainer;
    [SerializeField] private TradeOfferView _offerPrefab;
    [SerializeField] private Button _refreshButton;
    [SerializeField] private TextMeshProUGUI _refreshTimerText;     
    [SerializeField] private GameObject _timerIconObject;
    [SerializeField] private Sprite _activeButtonSprite;
    [SerializeField] private Sprite _waitingButtonSprite;

    private Dictionary<ResourceType, ResourceRowView> _spawnedRows = new Dictionary<ResourceType, ResourceRowView>();

    private void Start()
    {
        _refreshButton.onClick.AddListener(OnRefreshButtonClicked);
        InitializeResourceRows();
    }

    private void OnEnable()
    {
        if (TradeLogicController.Instance == null) return;

        // Підписки
        TradeLogicController.Instance.OnTimerTick += UpdateTimer;
        TradeLogicController.Instance.OnRefreshStatusChanged += UpdateHeaderVisuals;
        TradeLogicController.Instance.OnOffersListUpdated += RebuildOffersList;
        TradeLogicController.Instance.OnTradeResourceChanged += UpdateResourceVisuals;

        // Примусове оновлення при відкритті
        RebuildOffersList(TradeLogicController.Instance.GetOffers());
        UpdateHeaderVisuals(TradeLogicController.Instance.IsReadyToRefresh());
        UpdateTimer(TradeLogicController.Instance.GetTimeRemaining());
    }

    private void UpdateResourceVisuals(ResourceData resource)
    {
        if (_spawnedRows.TryGetValue(resource.Type, out var row))
        {
            row.UpdateView(resource.Amount, resource.MaxCapacity);
        }
        else
        {
            Debug.LogWarning($"No ResourceRowView found for resource type: {resource.Type}");
        }
    }

    private void OnDisable()
    {
        if (TradeLogicController.Instance == null) return;

        TradeLogicController.Instance.OnTimerTick -= UpdateTimer;
        TradeLogicController.Instance.OnRefreshStatusChanged -= UpdateHeaderVisuals;
        TradeLogicController.Instance.OnOffersListUpdated -= RebuildOffersList;
        TradeLogicController.Instance.OnTradeResourceChanged -= UpdateResourceVisuals;
    }

    private void OnRefreshButtonClicked()
    {
        bool isReady = TradeLogicController.Instance.IsReadyToRefresh();

        if (isReady)
        {
            TradeLogicController.Instance.RefreshOffersFree();
        }
        else
        {
            int cost = TradeLogicController.Instance.GetGemSkipCost();
            Debug.Log($"Open Popup: Refresh now for {cost} gems?");

            // PopupManager.ShowConfirmation($"Pay {cost} Gems?", () => TradeService.Instance.TryRefreshWithGems());
        }
    }

    private void UpdateTimer(float time)
    {
        if (time < 0)
            return;

        int m = Mathf.FloorToInt(time / 60F);
        int s = Mathf.FloorToInt(time % 60F);
        _refreshTimerText.text = $"{m:00}:{s:00}";
    }

    private void UpdateHeaderVisuals(bool isReady)
    {
        _timerIconObject.SetActive(!isReady);
        _refreshTimerText.gameObject.SetActive(!isReady);
        _refreshButton.image.sprite = isReady ? _activeButtonSprite : _waitingButtonSprite;
    }

    private void InitializeResourceRows()
    {
        foreach (Transform child in _resourcesContainer) Destroy(child.gameObject);
        _spawnedRows.Clear();

        var resources = TradeLogicController.Instance.GetPlayerResources();
        foreach (var resource in resources)
        {
            var row = Instantiate(_resourceRowPrefab, _resourcesContainer);
            row.Initialize(resource, OnUpgradeClicked);
            _spawnedRows.Add(resource.Type, row);
        }
    }

    private void OnUpgradeClicked(ResourceType type)
    {
        Debug.Log($"Open Popup: Upgrade capacity for {type}?");
        // PopupManager.ShowUpgradePopup(type, () => TradeService.Instance.UpgradeCapacity(type));
    }

    private void RebuildOffersList(List<TradeOfferData> offers)
    {
        foreach (Transform child in _offersContainer) Destroy(child.gameObject);

        foreach (var offer in offers)
        {
            var item = Instantiate(_offerPrefab, _offersContainer);
            item.Initialize(offer, OnOfferClicked);
        }
    }

    private void OnOfferClicked(TradeOfferData offer)
    {
        Debug.Log($"Open Popup: Accept trade with offer?");
        TradeLogicController.Instance.TryExecuteTrade(offer);
        // PopupManager.ShowTradeConfirm(offer, () => TradeService.Instance.TryExecuteTrade(offer));
    }
}
