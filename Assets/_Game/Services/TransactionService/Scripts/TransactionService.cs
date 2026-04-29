using System.Collections.Generic;
using UnityEngine;

namespace Game.General
{
    public class TransactionService : MonoBehaviour
    {
        public static TransactionService Instance { get; private set; }

        // Handlers that determine the logic of operations on resource values
        private Dictionary<ResourceType, IResourceLogicHandler> _logicHandlers = new Dictionary<ResourceType, IResourceLogicHandler>();

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Registers handler that defines custom logic for resource modifications.
        /// </summary>
        public void RegisterHandler(ResourceType type, IResourceLogicHandler handler)
        {
            if (!_logicHandlers.ContainsKey(type))
            {
                _logicHandlers.Add(type, handler);
            }
        }

        /// <summary>
        /// Unregisters handler for the specified resource type, if it exists.
        /// </summary>
        public void UnregisterHandler(ResourceType type)
        {
            if (_logicHandlers.ContainsKey(type))
            {
                _logicHandlers.Remove(type);
            }
        }

        /// <summary>
        /// Determines whether the specified transaction operation can be successfully applied after checking conditions by handlers, if any.
        /// If operation.ForceApply equals true, skips validation for that operation.
        /// </summary>
        /// <param name="operations">The transaction operations to apply. Determines the type of resource affected, the amount to change, and
        /// whether to force the application of the transaction.</param>
        /// <returns>true if the transaction can be applied; otherwise, false.</returns>
        public bool CanApplyTransaction(IReadOnlyList<TransactionOperation> operations)
        {
            var simulationCache = new Dictionary<ResourceType, int>();

            foreach (var op in operations)
            {
                if (!simulationCache.ContainsKey(op.Type))
                    simulationCache[op.Type] = PlayerDataService.Instance.GetResourceAmount(op.Type);

                int currentSimulated = simulationCache[op.Type];

                if (!op.ForceApply)
                {
                    if (!CanApplyTransactionOperation(op.Type, currentSimulated, op.Amount))
                        return false;
                }

                simulationCache[op.Type] = CalculateTransactionOperation(op.Type, currentSimulated, op.Amount);
            }

            return true;
        }

        // Method overloading for single parameter
        public bool CanApplyTransaction(TransactionOperation operation)
        {
            if (operation.ForceApply) return true;

            return CanApplyTransactionOperation(operation.Type, PlayerDataService.Instance.GetResourceAmount(operation.Type), operation.Amount);
        }

        /// <summary>
        /// Attempts to complete the transaction after checking conditions by handlers.
        /// </summary>
        /// <param name="operations">The transaction operations to apply. Determines the type of resource affected, the amount to change, and
        /// whether to force the application of the transaction.</param>
        /// <returns>true if the transaction was successfully applied; otherwise, false.</returns>
        public bool TryApplyTransaction(IReadOnlyList<TransactionOperation> operations)
        {
            if (!CanApplyTransaction(operations))
                return false;

            foreach (var op in operations)
            {
                int currentAmount = PlayerDataService.Instance.GetResourceAmount(op.Type);
                int newAmount = CalculateTransactionOperation(op.Type, currentAmount, op.Amount);

                IResourceDataWriter writer = PlayerDataService.Instance;
                writer.ApplyResourceChange(op.Type, currentAmount, newAmount);
            }

            return true;
        }

        // Method overloading for single parameter
        public bool TryApplyTransaction(TransactionOperation operation)
        {
            if (!CanApplyTransaction(operation)) return false;

            int currentAmount = PlayerDataService.Instance.GetResourceAmount(operation.Type);
            int newAmount = CalculateTransactionOperation(operation.Type, currentAmount, operation.Amount);

            IResourceDataWriter writer = PlayerDataService.Instance;
            writer.ApplyResourceChange(operation.Type, currentAmount, newAmount);

            return true;
        }

        // Private method that determines whether a single transaction operation is possible for a given type and amount. Delegates logic to handlers or applies default
        private bool CanApplyTransactionOperation(ResourceType type, int currentAmount, int delta)
        {
            // Use handler logic if exists
            if (_logicHandlers.TryGetValue(type, out var handler))
            {
                if (handler != null)
                    return handler.CanApplyTransactionOperation(type, currentAmount, delta);
                else
                    Debug.LogError($"[TransactionService] Handler for type {type} is null!");
            }

            // Default logic
            return delta < 0 ? currentAmount >= Mathf.Abs(delta) : true;
        }

        // Private method that calculates single transaction operation for a given type and amount. Delegates logic to handlers or applies default
        private int CalculateTransactionOperation(ResourceType type, int currentAmount, int delta)
        {
            // Use handler logic if exists
            if (_logicHandlers.TryGetValue(type, out var handler))
            {
                if (handler != null)
                    return handler.CalculateTransactionOperation(type, currentAmount, delta);
                else
                    Debug.LogError($"[TransactionService] Handler for type {type} is null!");
            }

            // Default logic
            return Mathf.Max(currentAmount + delta, 0);
        }
    }
}