using System;
using Game.General;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Features.Navigation
{
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
