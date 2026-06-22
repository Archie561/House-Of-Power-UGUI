namespace Game.Features.Law
{
    /// <summary>
    /// Data transfer object representing the progress of a policy in the law mechanic.
    /// </summary>
    public readonly struct PolicyProgressData
    {
        public int Level { get; }
        public int CurrentXp { get; }
        public int RequiredXp { get; }
        public int MissingXp { get; }
        public int UpgradeCostGems { get; }
        public int TotalFloorXp { get; }

        public PolicyProgressData(int level, int currentXp, int requiredXp, int upgradeCostGems, int totalFloorXp)
        {
            Level = level;
            CurrentXp = currentXp;
            RequiredXp = requiredXp;
            MissingXp = requiredXp - currentXp;
            UpgradeCostGems = upgradeCostGems;
            TotalFloorXp = totalFloorXp;
        }
    }
}