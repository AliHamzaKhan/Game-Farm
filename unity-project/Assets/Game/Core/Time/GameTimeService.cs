using System;
using UnityEngine;

namespace FarmQuest.Core.Time
{
    /// <summary>
    /// Centralized game clock (§9). ALL timed activity (crops, animals, production,
    /// buildings, missions, events) must use this service — never independent timers.
    /// Offline progression: activities store UTC timestamps; elapsed time is computed
    /// on demand, so closing the game simply lets time pass.
    /// </summary>
    public interface ITimeService
    {
        DateTime UtcNow { get; }
        double GetElapsedSeconds(DateTime fromUtc);
        double GetElapsedSeconds(long fromTicksUtc);
        /// <summary>Raised once per second by TimeTicker for UI/event polling.</summary>
        event Action SecondTick;
        /// <summary>Raised when the UTC calendar day rolls over (bank interest,
        /// market days, seasons). Phase 5.</summary>
        event Action DayChanged;
    }

    public class GameTimeService : ITimeService
    {
        public DateTime UtcNow => DateTime.UtcNow;

        public double GetElapsedSeconds(DateTime fromUtc)
        {
            var elapsed = (UtcNow - fromUtc).TotalSeconds;
            // Clamp absurd clock jumps (device clock changed / shared devices):
            // crops simply become ready, nothing is punished.
            if (elapsed < 0) return 0;
            const double maxSaneSeconds = 60 * 60 * 24 * 30; // 30 days
            return Math.Min(elapsed, maxSaneSeconds);
        }

        public double GetElapsedSeconds(long fromTicksUtc)
        {
            return GetElapsedSeconds(new DateTime(fromTicksUtc, DateTimeKind.Utc));
        }

        public event Action SecondTick;
        public event Action DayChanged;

        private DateTime _lastDay = DateTime.UtcNow.Date;

        internal void RaiseSecondTick()
        {
            SecondTick?.Invoke();
            var today = DateTime.UtcNow.Date;
            if (today != _lastDay)
            {
                _lastDay = today;
                DayChanged?.Invoke();
            }
        }
    }

    /// <summary>
    /// Single MonoBehaviour driving the per-second tick. Attach once in the
    /// bootstrap scene. This is the ONLY Update() in core services (§49).
    /// </summary>
    public class TimeTicker : MonoBehaviour
    {
        private GameTimeService _time;
        private float _accumulator;

        private void Awake()
        {
            _time = new GameTimeService();
            FarmQuest.Core.Services.ServiceLocator.Register<ITimeService>(_time);
        }

        private void Update()
        {
            _accumulator += UnityEngine.Time.deltaTime;
            if (_accumulator >= 1f)
            {
                _accumulator -= 1f;
                _time.RaiseSecondTick();
            }
        }
    }
}
