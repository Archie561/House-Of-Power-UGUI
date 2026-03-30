using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Game.Features.Law
{
    [CreateAssetMenu(fileName = "LawConfig", menuName = "Game/Law/Law Config")]
    public class LawConfig : ScriptableObject
    {
        [Header("Laws Config")]
        [SerializeField] private List<LawData> _allLaws = new List<LawData>();
        [SerializeField] private int _maxAvailableLaws = 8;
        [SerializeField] private int _replenishCooldownSeconds = 60;
        [SerializeField] private int _lawReplenishCost = 2;
        [SerializeField] private int _costPer10Xp = 1;

        [Header("Policy Leveling")]
        [SerializeField] private int _baseRequiredXpForLevel = 100;
        [SerializeField] private int _xpIncreasePerLevel = 50;      

        public IReadOnlyList<LawData> GetAllLaws() => _allLaws;
        public int MaxAvailableLaws => _maxAvailableLaws;
        public int ReplenishCooldownSeconds => _replenishCooldownSeconds;
        public int LawReplenishCost => _lawReplenishCost;
        public int CostPer10Xp => _costPer10Xp;
        public int BaseRequiredXpForLevel => _baseRequiredXpForLevel;
        public int XpIncreasePerLevel => _xpIncreasePerLevel;

        /// <summary>
        /// Method to set the list of laws imported from an TSV file.
        /// </summary>
        public void SetLawsFromImport(List<LawData> importedLaws)
        {
            _allLaws = importedLaws;

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }
}