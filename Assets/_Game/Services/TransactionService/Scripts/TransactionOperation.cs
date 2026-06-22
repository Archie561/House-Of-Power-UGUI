using System;
using UnityEngine;

namespace Game.General
{
    [Serializable]
    /// <summary> Struct representing a transaction operation for resources. </summary>
    public readonly struct TransactionOperation
    {
        public readonly ResourceType Type;
        public readonly int Amount;
        public readonly bool ForceApply;

        /// <summary>
        /// Initializes a new instance of the TransactionOperation struct with the specified resource type, amount, and optional force apply flag.
        /// </summary>
        /// <param name="type">The type of resource involved in the transaction.</param>
        /// <param name="signedAmount">The amount of the resource to be transacted. Negative values ​​are subtracted, positive values ​​are added.</param>
        /// <param name="forceApply">True to force the transaction to be applied regardless of validation; false by default.</param>
        public TransactionOperation(ResourceType type, int signedAmount, bool forceApply = false)
        {
            Type = type;
            Amount = signedAmount;
            ForceApply = forceApply;
        }

        /// <summary>
        /// Creates an operation to add resources.
        /// </summary>
        public static TransactionOperation Add(ResourceType type, int amount, bool forceApply = false)
        {
            return new TransactionOperation(type, Mathf.Abs(amount), forceApply);
        }

        /// <summary>
        /// Creates an operation to spend resources.
        /// </summary>
        public static TransactionOperation Spend(ResourceType type, int cost, bool forceApply = false)
        {
            return new TransactionOperation(type, -Mathf.Abs(cost), forceApply);
        }
    }
}
