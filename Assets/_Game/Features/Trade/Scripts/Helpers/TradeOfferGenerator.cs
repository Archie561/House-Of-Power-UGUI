using Game.General;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Features.Trade
{
    /// <summary>
    /// Generates complex trade offers with multiple resources (2-3 items per side).
    /// Balances offers based on player's storage capacity and random profitability factors.
    /// </summary>
    public class TradeOfferGenerator
    {
        // Item Count Configuration
        private const int MIN_ITEMS_PER_SIDE = 2;
        private const int MAX_ITEMS_PER_SIDE = 3;

        // Capacity Load Factors (How much of the storage we want to fill/empty)
        private const float MIN_LOAD_FACTOR = 0.1f; // 10% of storage
        private const float MAX_LOAD_FACTOR = 0.3f; // 30% of storage

        // Profitability Multipliers (Import / Export ratio)
        private const float RATE_BAD_MIN = 0.6f;
        private const float RATE_BAD_MAX = 0.8f;
        private const float RATE_NORMAL_MIN = 0.9f;
        private const float RATE_NORMAL_MAX = 1.1f;
        private const float RATE_GOOD_MIN = 1.3f;
        private const float RATE_GOOD_MAX = 1.8f;

        // Probabilities (Must sum to 1.0)
        private const float CHANCE_BAD = 0.4f;
        private const float CHANCE_NORMAL = 0.3f;
        // Remaining 0.3 is CHANCE_GOOD

        // Limits
        private const int MIN_TRANSACTION_AMOUNT = 10;

        private readonly List<ResourceType> _tradableResources;
        private readonly List<CountryId> _countries;

        public TradeOfferGenerator()
        {
            _tradableResources = Enum.GetValues(typeof(ResourceType))
                .Cast<ResourceType>()
                .Where(t => !t.IsCurrency())
                .ToList();

            _countries = Enum.GetValues(typeof(CountryId))
                .Cast<CountryId>()
                .ToList();
        }

        public TradeOfferData GenerateOffer()
        {
            var country = GetRandomCountry();

            // 1. Select Resources
            // We need unique resources for export and import so they don't overlap.
            var shuffledRes = _tradableResources.OrderBy(x => UnityEngine.Random.value).ToList();

            int exportCount = UnityEngine.Random.Range(MIN_ITEMS_PER_SIDE, MAX_ITEMS_PER_SIDE + 1); // +1 because upper bound is exclusive
            int importCount = UnityEngine.Random.Range(MIN_ITEMS_PER_SIDE, MAX_ITEMS_PER_SIDE + 1);

            // Validate counts against available definitions
            int maxTotal = shuffledRes.Count;
            if (exportCount + importCount > maxTotal)
            {
                exportCount = maxTotal / 2;
                importCount = maxTotal - exportCount;
            }

            var exportTypes = shuffledRes.Take(exportCount).ToList();
            var importTypes = shuffledRes.Skip(exportCount).Take(importCount).ToList();

            // 2. Determine Profitability Ratio first
            float exchangeRate = GetRandomExchangeRate();

            // 3. Calculate Capacities (The Bottleneck Check)

            // A. Calculate max POTENTIAL export based on player's export storage
            int maxPotentialExport = CalculateTotalCapacityScaled(exportTypes);

            // B. Calculate max POSSIBLE import based on player's import storage
            // If we want to give player 1.5x profit, we must ensure they have space for it.
            int maxPossibleImport = CalculateTotalCapacity(importTypes);

            // 4. Calculate Target Amounts
            // We initially want to trade based on our export capacity potential
            int targetExportAmount = maxPotentialExport;
            int targetImportAmount = Mathf.RoundToInt(targetExportAmount * exchangeRate);

            // 5. Apply "Reverse Scaling" Logic
            // If the calculated import is too huge for the player's small storage,
            // we must scale down the ENTIRE deal to fit the import storage.
            if (targetImportAmount > maxPossibleImport)
            {
                // We are limited by import space.
                // Scale down export to maintain the exchange rate.
                // Formula: Export = Import / Rate
                targetImportAmount = maxPossibleImport;
                targetExportAmount = Mathf.RoundToInt(targetImportAmount / exchangeRate);
            }

            // Ensure we don't go below minimums
            if (targetExportAmount < MIN_TRANSACTION_AMOUNT) targetExportAmount = MIN_TRANSACTION_AMOUNT;
            if (targetImportAmount < MIN_TRANSACTION_AMOUNT) targetImportAmount = MIN_TRANSACTION_AMOUNT;

            // 6. Distribute the calculated totals among the specific resources
            // We use weighted distribution based on individual resource capacity
            var exports = DistributeAmountByCapacity(targetExportAmount, exportTypes);
            var imports = DistributeAmountByCapacity(targetImportAmount, importTypes);

            return new TradeOfferData(country, imports, exports);
        }

        private CountryId GetRandomCountry()
        {
            return _countries[UnityEngine.Random.Range(0, _countries.Count)];
        }

        private float GetRandomExchangeRate()
        {
            float roll = UnityEngine.Random.value;

            if (roll < CHANCE_BAD)
                return UnityEngine.Random.Range(RATE_BAD_MIN, RATE_BAD_MAX);

            if (roll < CHANCE_BAD + CHANCE_NORMAL)
                return UnityEngine.Random.Range(RATE_NORMAL_MIN, RATE_NORMAL_MAX);

            return UnityEngine.Random.Range(RATE_GOOD_MIN, RATE_GOOD_MAX);
        }

        /// <summary>
        /// Calculates sum of max capacities for given types.
        /// </summary>
        private int CalculateTotalCapacity(List<ResourceType> types)
        {
            int total = 0;
            foreach (var type in types)
            {
                total += GameDataService.Instance.GetMaxCapacity(type);
            }
            return total;
        }

        /// <summary>
        /// Calculates sum of capacities multiplied by a random load factor (e.g., 30% of capacity).
        /// Used to determine how much the player "wants" to trade ideally.
        /// </summary>
        private int CalculateTotalCapacityScaled(List<ResourceType> types)
        {
            float total = 0;
            foreach (var type in types)
            {
                int cap = GameDataService.Instance.GetMaxCapacity(type);
                float load = UnityEngine.Random.Range(MIN_LOAD_FACTOR, MAX_LOAD_FACTOR);
                total += cap * load;
            }
            return Mathf.RoundToInt(total);
        }

        /// <summary>
        /// Distributes a total amount across resources, weighted by their storage capacity.
        /// This ensures we don't try to put 500 items into a 100-capacity storage.
        /// </summary>
        private List<ResourceData> DistributeAmountByCapacity(int totalAmountToDistribute, List<ResourceType> types)
        {
            var result = new List<ResourceData>();
            int remainingToDistribute = totalAmountToDistribute;

            // Calculate total capacity of this group to determine weights
            int groupTotalCapacity = CalculateTotalCapacity(types);

            // Prevent division by zero
            if (groupTotalCapacity <= 0) groupTotalCapacity = 1;

            for (int i = 0; i < types.Count; i++)
            {
                var type = types[i];
                int typeCapacity = GameDataService.Instance.GetMaxCapacity(type);

                int amount;

                // If last item, take the rest to fix rounding errors
                if (i == types.Count - 1)
                {
                    amount = remainingToDistribute;
                }
                else
                {
                    // Weight based on capacity size
                    float weight = (float)typeCapacity / groupTotalCapacity;

                    // Add a little randomness to the weight so it's not perfectly linear every time
                    float variance = UnityEngine.Random.Range(0.8f, 1.2f);

                    amount = Mathf.RoundToInt(totalAmountToDistribute * weight * variance);
                }

                // Final clamp to ensure we don't overflow the specific storage OR underflow
                // Max limit: The storage capacity itself
                // Min limit: 1 (to ensure item exists in list)
                amount = Mathf.Clamp(amount, 1, typeCapacity);

                // Also clamp by what's actually remaining to distribute
                amount = Mathf.Min(amount, remainingToDistribute);

                // Add to list
                result.Add(new ResourceData(type, amount));
                remainingToDistribute -= amount;

                // If we ran out of amount early (due to clamping), subsequent items might get 0. 
                // In a real production code, you might want a retry loop or simpler distribution logic,
                // but for this casual balance, it's acceptable.
            }

            // Cleanup: remove any 0-amount entries if distribution failed slightly
            result.RemoveAll(x => x.Amount <= 0);

            return result;
        }
    }
}