using System.Collections.Generic;
using UnityEngine;

namespace Game.Features.Law
{
    [CreateAssetMenu(fileName = "LawConfig", menuName = "Game/Law/Law Config")]
    public class LawConfig : ScriptableObject
    {
        [Header("General")]
        [SerializeField] private List<LawData> _allLaws = new List<LawData>();
        [SerializeField] private int _baseLawCount = 8;
        [SerializeField] private int _baseRequiredXpForLevel = 100;
        [SerializeField] private int _xpGrowthPerLevel = 10;

        public IReadOnlyList<LawData> GetAllLaws() => _allLaws;
        public int BaseLawCount => _baseLawCount;
        public int BaseRequiredXpForLevel => _baseRequiredXpForLevel;
        public int XpGrowthPerLevel => _xpGrowthPerLevel;

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