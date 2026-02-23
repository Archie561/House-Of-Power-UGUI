using Game.General;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Features.Law
{
    /// <summary>
    /// Provides centralized management and retrieval of player policy information within the game.
    /// Acts as a bridge between DataService and View
    /// </summary>
    public class LawLogicController : MonoBehaviour, IResourceLogicHandler
    {
        public static LawLogicController Instance { get; private set; }

        [Header("Config")]
        [SerializeField] private LawConfig _lawConfig;

        // --- Events ---
        public event Action<ResourceType> OnPolicyAmountChanged;

        // --- State ---
        private List<LawData> _availableLaws = new List<LawData>();
        private LawData _activeLaw;
        private int _lawsLeftToExecute;

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            // Register this controller as a handler for policy-related resource types
            RegisterHandler();
        }

        private void Start()
        {
            InitializeAvailableLaws();
            LoadActiveLaw();
        }

        private void InitializeAvailableLaws()
        {
            var usedLawIds = GameDataService.Instance.GetUsedLawIds();
            _availableLaws = _lawConfig.GetAllLaws().Where(law => !usedLawIds.Contains(law.Id)).ToList();
        }

        private void LoadActiveLaw()
        {
            _activeLaw = _availableLaws.FirstOrDefault(law => law.Id == GameDataService.Instance.GetActiveLawId());
             if (_activeLaw == null)
             {
                _activeLaw = GetRandomLaw();
            }
        }

        #endregion

        #region Policies Logic

        /// <summary>
        /// Retrieves a read-only collection of policy resources associated with the player.
        /// </summary>
        public IReadOnlyList<ResourceAmount> GetPlayerPolicies()
        {
            List<ResourceAmount> policies = new List<ResourceAmount>();

            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                if (type.IsPolicyValue())
                {
                    policies.Add(new ResourceAmount(type, GameDataService.Instance.GetAmount(type)));
                }
            }

            return policies.AsReadOnly();
        }

        /// <summary>
        /// Returns the policy level for the specified policy based on accumulated experience points.
        /// </summary>
        public int GetPolicyLevel(ResourceType type)
        {
            int amount = GameDataService.Instance.GetAmount(type);
            return (amount / _lawConfig.BaseRequiredXpForLevel) + 1;
        }

        /// <summary>
        /// Returns the policy amount for the specified resource type based on the current game data.
        /// </summary>
        public int GetCalculatedPolicyAmount(ResourceType type)
        {
            int amount = GameDataService.Instance.GetAmount(type);
            return amount % _lawConfig.BaseRequiredXpForLevel;
        }

        /// <summary>
        /// Returns the maximum policy value for the specified resource type.
        /// </summary>
        public int GetCalculatedPolicyMaxValue(ResourceType type)
        {
            return _lawConfig.BaseRequiredXpForLevel;
        }

        #endregion

        #region Laws Logic

        public LawData GetActiveLaw()
        {
            if (_activeLaw == null)
            {
                InitializeAvailableLaws();
                LoadActiveLaw();
            }

            return _activeLaw;
        }

        public void MakeDecision(bool accepted)
        {
            if (_activeLaw == null) return;

            var law = _activeLaw;
            _activeLaw = null;

            _lawsLeftToExecute--;
            _availableLaws.Remove(law);
            GameDataService.Instance.SaveUsedLawId(law.Id);
            // ...
        }

        public bool TryGetNextLaw(out LawData law)
        {
            if (_activeLaw != null)
            {
                law = _activeLaw;
                return true;
            }

            if (_lawsLeftToExecute <= 0)
            {
                 law = null;
                 return false;
            }

            law = GetRandomLaw();
            _activeLaw = law;

            GameDataService.Instance.SaveActiveLawId(law.Id);

            return true;
        }

        private LawData GetRandomLaw()
        {
            return _availableLaws[UnityEngine.Random.Range(0, _availableLaws.Count)];
        }

        #endregion

        #region IResourceLogicHandler Implementation

        private void RegisterHandler()
        {
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                if (type.IsPolicyValue())
                {
                    GameDataService.Instance.RegisterHandler(type, this);
                }
            }
        }

        public int CalculateTransactionOperation(ResourceType type, int currentAmount, int delta)
        {
            throw new NotImplementedException();
        }

        public bool CanApplyTransactionOperation(ResourceType type, int currentAmount, int delta)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
