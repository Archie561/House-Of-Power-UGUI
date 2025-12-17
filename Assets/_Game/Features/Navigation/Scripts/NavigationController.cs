using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NavigationController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private TabType _startTab = TabType.Map;
    [SerializeField] private Sprite _defaultButtonSprite;
    [SerializeField] private Sprite _activeButtonSprite;

    [Header("References")]
    [SerializeField] private List<NavigationTab> _tabs;

    private TabType _currentTab;

    public event Action<TabType> OnTabChanged;

    private void Start()
    {
        InitializeButtons();
        SwitchToTab(_startTab);
    }

    private void InitializeButtons()
    {
        foreach (var tab in _tabs)
        {
            tab.Button.onClick.AddListener(() => SwitchToTab(tab.Type));
        }
    }

    public void SwitchToTab(TabType type)
    {
        _currentTab = type;

        foreach (var tab in _tabs)
        {
            bool isActive = (tab.Type == type);

            if (tab.ContentScreen != null)
            {
                tab.ContentScreen.SetActive(isActive);
            }

            tab.Button.image.sprite = isActive ? _activeButtonSprite : _defaultButtonSprite;
        }

        OnTabChanged?.Invoke(type);
    }
}

[Serializable]
public class NavigationTab
{
    public TabType Type;
    public Button Button;
    public GameObject ContentScreen;
}