namespace Game.General
{
    /// <summary>
    /// Defines methods for evaluating resource value according to custom logic.
    /// </summary>
    public interface IResourceLogicHandler
    {
        /// <summary>
        /// Calculates the result of an operation on a resource using custom logic for operation safety.
        /// </summary>
        /// <param name="type">Type of resource involved in the operation.</param>
        /// <param name="currentAmount">Current amount of the specified resource available before the operation.</param>
        /// <param name="delta">Change in resource amount required for the operation. Positive values indicate a gain; negative values indicate a cost.</param>
        /// <returns>Operation result (already calculated by internal logic).</returns>
        int CalculateTransactionOperation(ResourceType type, int currentAmount, int delta);

        /// <summary>
        /// Determines whether a operation involving the specified resource type can be successfully applied based on the current amount and the proposed change.
        /// </summary>
        /// <param name="type">Type of resource involved in the operation.</param>
        /// <param name="currentAmount">Current amount of the specified resource available before the operation.</param>
        /// <param name="delta">Change in resource amount required for the operation. Positive values indicate a gain; negative values indicate a cost.</param>
        /// <returns>True if the transaction can be successfully applied with the current amount and delta; otherwise, false.</returns>
        bool CanApplyTransactionOperation(ResourceType type, int currentAmount, int delta);
    }
}
