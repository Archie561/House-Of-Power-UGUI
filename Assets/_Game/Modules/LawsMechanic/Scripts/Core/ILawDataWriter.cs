using System;

namespace Game.General
{
    /// <summary>
    /// Interface for writing changes to law data.
    /// Implemented by PlayerDataService to prevent direct access to data modification methods (Interface segregation principle)
    /// </summary>
    public interface ILawDataWriter
    {
        void SetActiveLawId(string id);
        void SetAvailableLawsCount(int count);
        void MarkLawAsUsed(string id);
        void ResetUsedLaws();
        void SetNextLawRefreshTime(DateTime time);
    }
}