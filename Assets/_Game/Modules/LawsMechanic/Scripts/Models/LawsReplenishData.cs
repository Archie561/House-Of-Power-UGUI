using System;

namespace Game.Features.Law
{
    /// <summary>
    /// Data transfer object representing the replenishment data for all laws
    /// </summary>
    public readonly struct LawsReplenishData
    {
        public DateTime TargetTime { get; }
        public int TotalCost { get; }

        public LawsReplenishData(DateTime targetTime, int totalCost)
        {
            TargetTime = targetTime;
            TotalCost = totalCost;
        }
    }
}