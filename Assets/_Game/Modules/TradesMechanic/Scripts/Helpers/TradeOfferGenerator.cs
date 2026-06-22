using Game.General;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Features.Trade
{
    /// <summary>
    /// Generates complex trade offers with multiple resources (2-3 items per side).
    /// Balances offers based on player's storage capacity and random profitability factors.
    /// </summary>
    public class TradeOfferGenerator
    {
        private OfferGenerationSettings _settings;
        private readonly Func<ResourceType, int> _capacityProvider;
        private readonly List<ResourceType> _tradableResources;
        private readonly List<CountryId> _countries;

        private readonly List<ResourceType> _shuffleBuffer;
        private readonly List<ResourceType> _exportTypesBuffer;
        private readonly List<ResourceType> _importTypesBuffer;

        public TradeOfferGenerator(OfferGenerationSettings settings, Func<ResourceType, int> capacityProvider)
        {
            _settings = settings;

            _capacityProvider = capacityProvider;

            Array resourceValues = Enum.GetValues(typeof(ResourceType));
            _tradableResources = new List<ResourceType>(resourceValues.Length);
            foreach (object value in resourceValues)
            {
                ResourceType type = (ResourceType)value;
                if (type.IsTradeGood())
                    _tradableResources.Add(type);
            }

            Array countryValues = Enum.GetValues(typeof(CountryId));
            _countries = new List<CountryId>(countryValues.Length);
            foreach (object value in countryValues)
            {
                _countries.Add((CountryId)value);
            }

            _shuffleBuffer = new List<ResourceType>(_tradableResources.Capacity);
            _exportTypesBuffer = new List<ResourceType>(_tradableResources.Capacity);
            _importTypesBuffer = new List<ResourceType>(_tradableResources.Capacity);
        }

        public TradeOfferData GenerateOffer()
        {
            var country = GetRandomCountry();

            // 1. Select Resources
            // We need unique resources for export and import so they don't overlap.
            _shuffleBuffer.Clear();
            for (int i = 0; i < _tradableResources.Count; i++)
                _shuffleBuffer.Add(_tradableResources[i]);

            // Fisher-Yates in-place shuffle
            for (int i = _shuffleBuffer.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                ResourceType temp = _shuffleBuffer[i];
                _shuffleBuffer[i] = _shuffleBuffer[j];
                _shuffleBuffer[j] = temp;
            }

            int exportCount = UnityEngine.Random.Range(_settings.MinItemsPerSide, _settings.MaxItemsPerSide + 1); // +1 because upper bound is exclusive
            int importCount = UnityEngine.Random.Range(_settings.MinItemsPerSide, _settings.MaxItemsPerSide + 1);

            // Validate counts against available definitions
            int maxTotal = _shuffleBuffer.Count;
            if (exportCount + importCount > maxTotal)
            {
                exportCount = maxTotal / 2;
                importCount = maxTotal - exportCount;
            }

            _exportTypesBuffer.Clear();
            for (int i = 0; i < exportCount; i++)
                _exportTypesBuffer.Add(_shuffleBuffer[i]);

            _importTypesBuffer.Clear();
            for (int i = exportCount; i < exportCount + importCount; i++)
                _importTypesBuffer.Add(_shuffleBuffer[i]);

            // 2. Determine Profitability Ratio first
            float exchangeRate = GetRandomExchangeRate();

            // 3. Calculate Capacities (The Bottleneck Check)

            // A. Calculate max POTENTIAL export based on player's export storage
            int maxPotentialExport = CalculateTotalCapacityScaled(_exportTypesBuffer);

            // B. Calculate max POSSIBLE import based on player's import storage
            // If we want to give player 1.5x profit, we must ensure they have space for it.
            int maxPossibleImport = CalculateTotalCapacity(_importTypesBuffer);

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
            if (targetExportAmount < _settings.MinTransactionAmount) targetExportAmount = _settings.MinTransactionAmount;
            if (targetImportAmount < _settings.MinTransactionAmount) targetImportAmount = _settings.MinTransactionAmount;

            // 6. Distribute the calculated totals among the specific resources
            // We use weighted distribution based on individual resource capacity
            var exports = DistributeAmountByCapacity(targetExportAmount, _exportTypesBuffer);
            var imports = DistributeAmountByCapacity(targetImportAmount, _importTypesBuffer);

            return new TradeOfferData(country, imports, exports);
        }

        private CountryId GetRandomCountry()
        {
            return _countries[UnityEngine.Random.Range(0, _countries.Count)];
        }

        private float GetRandomExchangeRate()
        {
            float roll = UnityEngine.Random.value;

            if (roll < _settings.ChanceBad)
                return UnityEngine.Random.Range(_settings.RateBadMin, _settings.RateBadMax);

            if (roll < _settings.ChanceBad + _settings.ChanceNormal)
                return UnityEngine.Random.Range(_settings.RateNormalMin, _settings.RateNormalMax);

            return UnityEngine.Random.Range(_settings.RateGoodMin, _settings.RateGoodMax);
        }

        /// <summary>
        /// Calculates sum of max capacities for given types.
        /// </summary>
        private int CalculateTotalCapacity(List<ResourceType> types)
        {
            int total = 0;
            foreach (var type in types)
            {
                total += _capacityProvider(type);
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
                int cap = _capacityProvider(type);
                float load = UnityEngine.Random.Range(_settings.MinLoadFactor, _settings.MaxLoadFactor);
                total += cap * load;
            }
            return Mathf.RoundToInt(total);
        }

        /// <summary>
        /// Distributes a total amount across resources, weighted by their storage capacity.
        /// This ensures we don't try to put 500 items into a 100-capacity storage.
        /// </summary>
        private List<ResourceAmount> DistributeAmountByCapacity(int totalAmountToDistribute, List<ResourceType> types)
        {
            var result = new List<ResourceAmount>();
            int remainingToDistribute = totalAmountToDistribute;

            // Calculate total capacity of this group to determine weights
            int groupTotalCapacity = CalculateTotalCapacity(types);

            // Prevent division by zero
            if (groupTotalCapacity <= 0) groupTotalCapacity = 1;

            for (int i = 0; i < types.Count; i++)
            {
                var type = types[i];
                int typeCapacity = _capacityProvider(type);

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
                result.Add(new ResourceAmount(type, amount));
                remainingToDistribute -= amount;

                // If we ran out of amount early (due to clamping), subsequent items might get 0. 
                // In a real production code, you might want a retry loop or simpler distribution logic,
                // but for this casual balance, it's acceptable.
            }

            // Cleanup: remove any 0-amount entries if distribution failed slightly
            for (int i = result.Count - 1; i >= 0; i--)
            {
                if (result[i].Amount <= 0)
                    result.RemoveAt(i);
            }

            return result;
        }
    }
}