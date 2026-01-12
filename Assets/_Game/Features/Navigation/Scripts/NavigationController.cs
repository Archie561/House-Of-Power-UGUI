using Game.General;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Features.Navigation
{
    /// <summary>
    /// Manages the bottom navigation bar, switching between different content screens.
    /// </summary>
    public class NavigationController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private TabType _startTab = TabType.Map;
        [SerializeField] private Sprite _defaultButtonSprite;
        [SerializeField] private Sprite _activeButtonSprite;

        [Header("References")]
        [SerializeField] private List<NavigationTab> _tabs;

        public event Action<TabType> OnTabChanged;

        private TabType _currentTab;

        private void Start()
        {
            InitializeButtons();

            // Force switch to start tab immediately
            SwitchToTab(_startTab, force: true);
        }

        /// <summary>
        /// Activates the specified tab and hides others.
        /// </summary>
        /// <param name="type">The target tab type.</param>
        /// <param name="force">If true, updates the UI even if the tab is already active.</param>
        public void SwitchToTab(TabType type, bool force = false)
        {
            if (!force && _currentTab == type) return;

            _currentTab = type;

            foreach (var tab in _tabs)
            {
                bool isActive = (tab.Type == type);

                if (tab.ContentScreen != null)
                {
                    tab.ContentScreen.SetActive(isActive);
                }

                if (tab.Button != null)
                {
                    tab.Button.image.sprite = isActive ? _activeButtonSprite : _defaultButtonSprite;
                }
            }

            OnTabChanged?.Invoke(type);
        }

        private void InitializeButtons()
        {
            foreach (var tab in _tabs)
            {
                if (tab.Button == null) continue;

                // Remove old listeners to prevent duplicates if initialized multiple times
                tab.Button.onClick.RemoveAllListeners();
                tab.Button.onClick.AddListener(() => SwitchToTab(tab.Type));
            }
        }
    }

    /// <summary>
    /// Helper class to link a TabType with its UI components.
    /// </summary>
    [Serializable]
    public class NavigationTab
    {
        public TabType Type;
        public Button Button;
        public GameObject ContentScreen;
    }
}