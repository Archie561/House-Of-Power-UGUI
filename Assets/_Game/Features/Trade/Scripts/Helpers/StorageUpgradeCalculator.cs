using Game.General;
using UnityEngine;

namespace Game.Features.Trade
{
    /// <summary>
    /// Calculates storage upgrade costs using a "Fill Rate" strategy.
    /// Ensures resource cost never exceeds storage capacity.
    /// </summary>
    public class StorageUpgradeCalculator
    {
        private const int CAPACITY_STEP = 100;

        // --- Resource Cost Settings ---
        // At Level 1, cost is 50% of storage (Easy start)
        private const float START_FILL_RATIO = 0.5f;

        // At Max Level (or high level), cost is 90% of storage (Hard grind)
        private const float MAX_FILL_RATIO = 0.90f;

        // The level at which we reach maximum difficulty (e.g., Level 20)
        private const float DIFFICULTY_PEAK_LEVEL = 20f;


        // --- Gem Cost Settings ---
        private const int BASE_GEM_COST = 5;
        // Determines how fast gem price grows (Exponential-like)
        private const float GEM_GROWTH_MULTIPLIER = 1.2f;

        public UpgradeStorageData CalculateNextUpgrade(ResourceType type, int currentCapacity)
        {
            int nextCapacity = currentCapacity + CAPACITY_STEP;

            // 1. Calculate Level (1, 2, 3...)
            int currentLevel = Mathf.Max(1, currentCapacity / CAPACITY_STEP);

            // 2. Calculate Resource Cost (Safe "Fill Rate" Algorithm)
            // We calculate a ratio from 0.5 to 0.90 based on level progression.
            // Using Mathf.InverseLerp allows us to map Level 1->10 to Ratio 0.5->0.90
            float difficultyProgress = Mathf.Clamp01((currentLevel - 1) / DIFFICULTY_PEAK_LEVEL);

            // Interpolate (Lerp) between Easy and Hard ratio
            float currentFillRatio = Mathf.Lerp(START_FILL_RATIO, MAX_FILL_RATIO, difficultyProgress);

            // Calculate exact cost
            int resourceCost = Mathf.RoundToInt(currentCapacity * currentFillRatio);

            // 3. Calculate Gem Cost (Value-based Growth)
            // Formula: Base * (Multiplier ^ (Level - 1))
            // This creates a nice curve where early upgrades are cheap, but later ones become premium.
            float gemMultiplier = Mathf.Pow(GEM_GROWTH_MULTIPLIER, currentLevel - 1);
            int gemCost = Mathf.RoundToInt(BASE_GEM_COST * gemMultiplier);

            return new UpgradeStorageData(
                type,
                resourceCost,
                gemCost,
                currentCapacity,
                nextCapacity
            );
        }

        public int GetCapacityStep() => CAPACITY_STEP;
    }
}