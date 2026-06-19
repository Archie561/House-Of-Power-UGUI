using Game.General;
using System;

namespace Game.Features.Law
{
    /// <summary>
    /// Calculates all necessary parameters related to the policy level based on total policy xp.
    /// </summary>
    public class PolicyUpgradeCalculator
    {
        private LawConfig _config;
        private readonly Func<ResourceType, int> _totalXpProvider;

        public PolicyUpgradeCalculator(LawConfig config, Func<ResourceType, int> totalXpProvider)
        {
            _config = config;
            _totalXpProvider = totalXpProvider;
        }

        /// <summary>
        /// Calculates the current level, XP progress, and upgrade cost for a given policy type based on total XP.
        /// </summary>
        public PolicyProgressData GetPolicyProgressData(ResourceType type)
        {
            int totalXp = _totalXpProvider(type);
            int level = 1;
            int requiredXp = _config.BaseRequiredXpForLevel;
            int totalFloorXp = 0;

            // Substracting until we can't level up anymore
            while (totalXp >= requiredXp)
            {
                totalXp -= requiredXp;
                totalFloorXp += requiredXp;
                level++;
                requiredXp += _config.XpIncreasePerLevel;
            }

            int currentXp = totalXp;
            int missingXp = requiredXp - currentXp;

            // Calculation of the cost in gems
            int upgradeCostGems = 0;
            if (missingXp > 0)
            {
                int packsNeeded = (missingXp + 9) / 10;
                upgradeCostGems = packsNeeded * _config.CostPer10Xp;
            }

            return new PolicyProgressData(
                level: level,
                currentXp: currentXp,
                requiredXp: requiredXp,
                upgradeCostGems: upgradeCostGems,
                totalFloorXp: totalFloorXp
            );
        }
    }
}