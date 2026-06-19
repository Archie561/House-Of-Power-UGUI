using System;
using UnityEngine;

namespace Game.General
{
    /// <summary>
    /// Universal cooldown timer with built-in optimizations and extensibility hooks.
    /// </summary>
    public abstract class BaseCooldownTimer
    {
        private DateTime _targetTime;
        private TimeSpan _remainingTime;
        private bool _isRunning;
        private int _lastIntSecond = -1;

        // Universal events, which are needed by all
        public event Action OnTickSeconds;
        public event Action OnFinished;

        public bool IsRunning => _isRunning;
        public DateTime TargetTime => _targetTime;
        public TimeSpan RemainingTime => _remainingTime;

        /// <summary>
        /// Starts or resumes the timer until the specified time.
        /// </summary>
        public virtual void Start(DateTime targetTime)
        {
            _targetTime = targetTime;
            _isRunning = true;
            _lastIntSecond = -1; // Resetting cache
            Tick(); // Calculating initial state immediately
        }

        /// <summary>
        /// Stops the timer and prevents any further events until restarted.
        /// </summary>
        public virtual void Stop()
        {
            _isRunning = false;
        }

        /// <summary>
        /// The main update method. Called from Update.
        /// </summary>
        public virtual void Tick()
        {
            if (!_isRunning) return;

            _remainingTime = _targetTime - DateTime.UtcNow;

            if (_remainingTime.TotalSeconds <= 0)
            {
                _isRunning = false;
                OnFinished?.Invoke();
            }

            // Optimization for events: invoke only if the integer second has changed
            int currentSeconds = Mathf.CeilToInt((float)_remainingTime.TotalSeconds);
            if (currentSeconds != _lastIntSecond)
            {
                _lastIntSecond = currentSeconds;
                OnTickSeconds?.Invoke();
            }
        }
    }
}