using System;
using FarmQuest.Core.Time;

namespace FarmQuest.Tests
{
    /// <summary>Deterministic clock for tests (§58).</summary>
    public class FakeTimeService : ITimeService
    {
        public DateTime Now { get; set; } =
            new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        public DateTime UtcNow => Now;

        public double GetElapsedSeconds(DateTime fromUtc) =>
            Math.Max(0, (Now - fromUtc).TotalSeconds);

        public double GetElapsedSeconds(long fromTicksUtc) =>
            GetElapsedSeconds(new DateTime(fromTicksUtc, DateTimeKind.Utc));

        public event Action SecondTick;
        public event Action DayChanged;
        public void RaiseTick() => SecondTick?.Invoke();
        public void RaiseDayChanged() => DayChanged?.Invoke();
        public void AdvanceSeconds(double seconds) => Now = Now.AddSeconds(seconds);
    }
}
