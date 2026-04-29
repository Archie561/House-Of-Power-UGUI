using UnityEngine;

namespace Game.Features.Trade
{
    /// <summary>
    /// Calculates storage upgrade costs using a "Fill Rate" strategy.
    /// Ensures resource cost never exceeds storage capacity.
    /// </summary>
    public class StorageUpgradeCalculator
    {
        private StorageUpgradeSettings _settings;

        /// <summary>
        /// Initializes a new instance of the StorageUpgradeCalculator class using the specified upgrade settings.
        /// </summary>
        public StorageUpgradeCalculator(StorageUpgradeSettings settings)
        {
            _settings = settings;
        }

        /// <summary>
        /// Calculates the storage capacity for THIS level.
        /// </summary>
        public int GetCapacity(int level)
        {
            if (level <= 1) return _settings.BaseCapacity;

            // Base * (Multiplier ^ (Level - 1))
            float cap = _settings.BaseCapacity * Mathf.Pow(_settings.CapacityMultiplier, level - 1);

            // Rounding (152 -> 150, 157 -> 160)
            return RoundToSignificant(cap);
        }

        /// <summary>
        /// Calculates the default cost required to upgrade to the THIS level.
        /// </summary>
        public int GetDefaultCost(int nextLevel)
        {
            int currentLevel = nextLevel - 1;
            int currentCapacity = GetCapacity(currentLevel);

            float cost = currentCapacity * _settings.UpgradeCostFillRate;

            return RoundToSignificant(cost);
        }

        /// <summary>
        /// Calculates the premium cost required to upgrade to the THIS level.
        /// </summary>
        public int GetPremiumCost(int nextLevel)
        {
            float cost = _settings.BasePremiumCost * Mathf.Pow(_settings.PremiumCostMultiplier, nextLevel - 2);
            return Mathf.CeilToInt(cost);
        }

        private int RoundToSignificant(float value)
        {
            int intVal = Mathf.RoundToInt(value);
            if (intVal < 100) return intVal;
            if (intVal < 1000) return (intVal / 10) * 10;
            return (intVal / 100) * 100;
        }
    }
}