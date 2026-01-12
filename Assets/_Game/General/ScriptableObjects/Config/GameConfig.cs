using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.General
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Game/General/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Initial Player State")]
        public List<ResourceInitData> InitialResources;
    }

    [Serializable] // Helper class to serialize resource initialization data in the inspector
    public struct ResourceInitData
    {
        public ResourceType Type;
        public int StartAmount;
        public int StartCapacity;
    }
}
