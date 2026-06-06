using Game.General;
using System;
using UnityEngine;

namespace Game.Features.Trade
{
    public class TradeCooldownTimer : BaseCooldownTimer
    {
        public event Action OnTickMinutes;
        private int _lastIntMinute = -1;

        public override void Tick()
        {
            base.Tick();

            // Calculate remaining minutes and invoke event if changed
            int currentMinutes = Mathf.CeilToInt((float)RemainingTime.TotalMinutes);
            if (currentMinutes != _lastIntMinute)
            {
                _lastIntMinute = currentMinutes;
                OnTickMinutes?.Invoke();
            }
        }
    }
}