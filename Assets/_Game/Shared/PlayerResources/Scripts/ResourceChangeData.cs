namespace Game.General
{
    /// <summary>
    /// Represents a change in a resource, including the resource type, previous value, new value, and the amount of change.
    /// </summary>
    /// <remarks>Use this struct to track or report changes to resources, such as when updating resource values in a
    /// system. The Delta field indicates the difference between the new and old values, and may be positive or negative
    /// depending on whether the resource increased or decreased.</remarks>
    public readonly struct ResourceChangeData
    {
        public readonly ResourceType Type;
        public readonly int OldValue;
        public readonly int NewValue;
        public readonly int Delta;

        public ResourceChangeData(ResourceType type, int oldVal, int newVal)
        {
            Type = type;
            OldValue = oldVal;
            NewValue = newVal;
            Delta = newVal - oldVal;
        }
    }
}
