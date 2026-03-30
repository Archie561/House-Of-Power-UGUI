using System;
using Game.General;

namespace Game.Features.Law
{
    /// <summary>
    /// Data class that holds the current progress of a policy resource type.
    /// </summary>
    [Serializable]
    public class DetailedPolicyProgressData
    {
        public ResourceType Type {get;}
        public int Level {get; }
        public int CurrentXp {get; }
        public int RequiredXp {get;}
        public Action OnBuyClick {get;}

        public DetailedPolicyProgressData(ResourceType type, int level, int currentXp, int requiredXp, Action onBuyClick)
        {
            Type = type;
            Level = level;
            CurrentXp = currentXp;
            RequiredXp = requiredXp;
            OnBuyClick = onBuyClick;
        }
    }
}